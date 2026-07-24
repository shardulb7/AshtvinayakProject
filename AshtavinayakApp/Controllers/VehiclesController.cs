using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AshtavinayakAPP.Models;
using AshtavinayakAPP.Services.BookingSrc;
using AshtavinayakAPP.Services.SmsService;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AshtavinayakAPP.Controllers
{
    public class VehiclesController : Controller
    {
        private readonly AshtvinayakTravelContext _context;
        private readonly IBookingService _bookingService;
        private readonly ISmsService _smsService;
        private readonly IConfiguration _configuration;

        public VehiclesController(
            AshtvinayakTravelContext context,
            IBookingService bookingService,
            ISmsService smsService,
            IConfiguration configuration)
        {
            _context        = context;
            _bookingService = bookingService;
            _smsService     = smsService;
            _configuration  = configuration;
        }



        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userSession = context.HttpContext.Session.GetString("User");
            if (string.IsNullOrEmpty(userSession))
            {
                // If no session exists, redirect to the login page
                context.Result = new RedirectToActionResult("Login", "Home", null);
            }
            base.OnActionExecuting(context);
        }

        // GET: Vehicles
        //public async Task<IActionResult> Index()
        //{
        //    var ashtvinayakTravelAppContext = _context.Vehicles.Include(v => v.Trip);
        //    return View(await ashtvinayakTravelAppContext.ToListAsync());
        //}

        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 10; // Number of records per page
            int totalRecords = await _context.Vehicles.Where(x => !x.IsDeleted).CountAsync(); // Get total number of vehicles
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize); // Calculate total pages

            var vehicles = await _context.Vehicles.Where(x => !x.IsDeleted)
                                         .Include(v => v.Trip) // Include related Trip data
                                         .OrderByDescending(v => v.VehicleId) // Ordering by VehicleId in descending order
                                         .Skip((page - 1) * pageSize) // Skip records for the previous pages
                                         .Take(pageSize) // Take only the records for the current page
                                         .ToListAsync();

            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;

            return View(vehicles);
        }


        // GET: Vehicles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _context.Vehicles
                .Include(v => v.Trip)
                .FirstOrDefaultAsync(m => m.VehicleId == id);
            if (vehicle == null)
            {
                return NotFound();
            }

            return View(vehicle);
        }

        // GET: Vehicles/Create
        public IActionResult Create()
        {
            ViewData["TripId"] = new SelectList(_context.Trips.Where(x => !x.IsDeleted), "TripId", "TourName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("VehicleId,VehicleType,VehicleName,VehicleNumber,TotalSeats,DriverName,DriverContact,TripId")] Vehicle vehicle)
        {

            if (ModelState.IsValid)   // HIGH-04: was incorrectly `|| vehicle.Trip == null` which always short-circuited validation
            {

                _context.Add(vehicle);
                await _context.SaveChangesAsync();

                var bokkingData = await _bookingService.GetBookingByTripIdAsync(tripId: vehicle.TripId);

                var tripName = _context.Trips.Where(x => x.TripId == vehicle.TripId).Select(x => x.TourName).FirstOrDefault();

                foreach (var booking in bokkingData)
                {
                    var phone = booking.User?.PhoneNumber;
                    var userName = booking.User?.UserName ?? "Customer";

                    if (!string.IsNullOrEmpty(phone))
                    {
                        var message = $"Hello {booking.User.UserName}, Vehicle and driver details for your {tripName} trip are as below- Vehicle Reg No- {vehicle.VehicleNumber} Driver Name - {vehicle.DriverName + "-" + vehicle.DriverContact} HAPPY JOURNEY..!! -iTas";

                        var vehicleTemplateId = _configuration["SmsGateway:VehicleTemplateId"] ?? string.Empty;
                        await _smsService.SendAsync(phone, message, vehicleTemplateId);
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            // Populate TripId dropdown if validation fails
            ViewData["TripId"] = new SelectList(_context.Trips.Where(x => !x.IsDeleted), "TripId", "TourName", vehicle.TripId);
            return View(vehicle);
        }


        // GET: Vehicles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
            {
                return NotFound();
            }
            ViewData["TripId"] = new SelectList(_context.Trips, "TripId", "TourName", vehicle.TripId);
            return View(vehicle);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("VehicleId,VehicleType,VehicleName,VehicleNumber,TotalSeats,DriverName,DriverContact,TripId,CreatedAt,UpdatedAt")] Vehicle vehicle)
        {
            if (id != vehicle.VehicleId)
            {
                return NotFound();
            }

            // Check if the vehicle number already exists in the database, but exclude the current vehicle (so it can be updated)
            var existingVehicle = await _context.Vehicles
                                                 .FirstOrDefaultAsync(v => v.VehicleNumber == vehicle.VehicleNumber && v.VehicleId != vehicle.VehicleId);

            if (existingVehicle != null)
            {
                // Add a model state error for the VehicleNumber field if duplicate exists
                ModelState.AddModelError("VehicleNumber", "Vehicle number should be unique.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vehicle);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VehicleExists(vehicle.VehicleId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["TripId"] = new SelectList(_context.Trips.Where(x => !x.IsDeleted), "TripId", "TourName", vehicle.TripId);
            return View(vehicle);
        }



        // GET: Vehicles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _context.Vehicles
                .Include(v => v.Trip)
                .FirstOrDefaultAsync(m => m.VehicleId == id);
            if (vehicle == null)
            {
                return NotFound();
            }

            return View(vehicle);
        }

        // POST: Vehicles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle != null)
            {
                vehicle.IsDeleted = true;
                _context.Vehicles.Update(vehicle);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VehicleExists(int id)
        {
            return _context.Vehicles.Any(e => e.VehicleId == id && !e.IsDeleted);
        }


    }
}
