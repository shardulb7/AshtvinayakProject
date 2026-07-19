using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AshtavinayakAPP.Models;
using OfficeOpenXml;
using System.IO;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AshtavinayakAPP.Controllers
{
    public class BookingsController : Controller
    {
        private readonly AshtvinayakTravelContext _context;


       

        public BookingsController(AshtvinayakTravelContext context)
        {
            _context = context;
        }

        // GET: Bookings
        //public async Task<IActionResult> Index()
        //{
        //    var ashtvinayakTravelAppContext = _context.Bookings.Include(b => b.PickupPoint).Include(b => b.Trip).Include(b => b.User).OrderByDescending(b => b.BookingId);
        //    return View(await ashtvinayakTravelAppContext.ToListAsync());
        //}

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

        public async Task<IActionResult> Index()
        {
            var ashtvinayakTravelAppContext = _context.Bookings.Where(x=>!x.IsDeleted&&x.TripId!=null)
                .Include(b => b.PickupPoint)
                .Include(b => b.Trip)
                .Include(b => b.User)
                .OrderByDescending(b => b.BookingId);

            ViewBag.Trips = await _context.Trips.Where(x=>!x.IsDeleted).ToListAsync();  // Get list of trips for the dropdown filter

            return View(await ashtvinayakTravelAppContext.ToListAsync());
        }


        // GET: Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.PickupPoint)
                .Include(b => b.Trip)
                .Include(b => b.User)
                .FirstOrDefaultAsync(m => m.BookingId == id);
            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // GET: Bookings/Create
        public IActionResult Create()
        {
            ViewData["PickupPointId"] = new SelectList(_context.PickupPoints, "PickupPointId", "PickupPoint1");
            ViewData["TripId"] = new SelectList(_context.Trips.Where(x => !x.IsDeleted), "TripId", "TourName");
            ViewData["UserId"] = new SelectList(_context.Users.Where(x => !x.IsDeleted), "UserId", "UserName");
            return View();
        }

        // POST: Bookings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BookingId,UserId,TripId,PickupPointId,BookingDate,Status,TotalPayment,Advance,BookingCode,Droppoint")] Booking booking)
        {
            if (ModelState.IsValid)
            {
                _context.Add(booking);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PickupPointId"] = new SelectList(_context.PickupPoints.Where(x=>!x.IsDeleted), "PickupPointId", "PickupPoint1");
            ViewData["TripId"] = new SelectList(_context.Trips.Where(x => !x.IsDeleted), "TripId", "TourName");
            ViewData["UserId"] = new SelectList(_context.Users.Where(x => !x.IsDeleted), "UserId", "UserName");
            return View(booking);
        }

        // GET: Bookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }
            ViewData["PickupPointId"] = new SelectList(_context.PickupPoints, "PickupPointId", "PickupPoint1");
            ViewData["TripId"] = new SelectList(_context.Trips.Where(x => !x.IsDeleted), "TripId", "TourName");
            ViewData["UserId"] = new SelectList(_context.Users.Where(x => !x.IsDeleted), "UserId", "UserName");
            return View(booking);
        }

        // POST: Bookings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BookingId,UserId,TripId,PickupPointId,BookingDate,Status,TotalPayment,Advance,BookingCode,Droppoint")] Booking booking)
        {
            if (id != booking.BookingId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingExists(booking.BookingId))
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
            ViewData["PickupPointId"] = new SelectList(_context.PickupPoints, "PickupPointId", "PickupPoint1");
            ViewData["TripId"] = new SelectList(_context.Trips.Where(x => !x.IsDeleted), "TripId", "TourName");
            ViewData["UserId"] = new SelectList(_context.Users.Where(x => !x.IsDeleted), "UserId", "UserName");
            return View(booking);
        }

        // GET: Bookings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.PickupPoint)
                .Include(b => b.Trip)
                .Include(b => b.User)
                .FirstOrDefaultAsync(m => m.BookingId == id);
            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // POST: Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                booking.IsDeleted = true;
                _context.Bookings.Update(booking);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ExportToPDF()
        {
            var bookings = await _context.Bookings
                .Include(b => b.PickupPoint) // Include related PickupPoint
                .Include(b => b.Trip) // Include related Trip
                .Include(b => b.User) // Include related User
                .ToListAsync(); // Get all records, ignoring pagination

            // Transform the bookings data into a simpler form for the PDF generation
            var bookingData = bookings.Select(b => new
            {
                bookingDate = b.BookingDate?.ToString("yyyy-MM-dd") ?? "",
                totalPayment = b.TotalPayment.ToString(),
                advance = b.Advance.ToString(),
                pickupPoint = b.PickupPoint?.PickupPoint1 ?? "",
                droppoint = b.Droppoint ?? "",
                tourName = b.Trip?.TourName ?? "",
                username = b.User?.UserName ?? ""
            }).ToList();

            return Json(bookingData); // Return the data as JSON to the client
        }


        
        public async Task<IActionResult> ExportToExcel(string tripFilter)
        {
            // Fetch bookings based on the selected filter
            IQueryable<Booking> bookingsQuery = _context.Bookings.Where(x=>!x.IsDeleted)
                                                         .Include(b => b.PickupPoint)
                                                         .Include(b => b.Trip)
                                                         .Include(b => b.User);

            if (!string.IsNullOrEmpty(tripFilter))
            {
                bookingsQuery = bookingsQuery.Where(b => b.Trip.TourName == tripFilter); // Filter by selected trip
            }

            var bookings = await bookingsQuery.ToListAsync();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Bookings");

                // Add header row
                worksheet.Cells[1, 1].Value = "Serial No";
                worksheet.Cells[1, 2].Value = "Booking Date";
                worksheet.Cells[1, 3].Value = "Total Payment";
                worksheet.Cells[1, 4].Value = "Advance";
                worksheet.Cells[1, 5].Value = "Pickup Point";
                worksheet.Cells[1, 6].Value = "Droppoint";
                worksheet.Cells[1, 7].Value = "Tour Name";
                worksheet.Cells[1, 8].Value = "Username";

                // Fill data
                int row = 2;
                int serialNumber = 1;

                foreach (var item in bookings)
                {
                    worksheet.Cells[row, 1].Value = serialNumber++;
                    worksheet.Cells[row, 2].Value = item.BookingDate?.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 3].Value = item.TotalPayment;
                    worksheet.Cells[row, 4].Value = item.Advance;
                    worksheet.Cells[row, 5].Value = item.PickupPoint;
                    worksheet.Cells[row, 6].Value = item.Droppoint;
                    worksheet.Cells[row, 7].Value = item.Trip?.TourName;
                    worksheet.Cells[row, 8].Value = item.User?.UserName;

                    row++;
                }

                // Save the Excel file to memory stream
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                // Return the Excel file as download
                string fileName = "Bookings.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }



        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.BookingId == id && !e.IsDeleted);
        }
    }
}