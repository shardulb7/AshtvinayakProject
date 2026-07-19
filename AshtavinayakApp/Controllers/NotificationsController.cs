using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AshtavinayakAPP.Models;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AshtavinayakAPP.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly AshtvinayakTravelContext _context;

        public NotificationsController(AshtvinayakTravelContext context)
        {
            _context = context;
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

        // GET: Notifications
        //public async Task<IActionResult> Index()
        //{
        //    var ashtvinayakTravelAppContext = _context.Notifications.Include(n => n.Trip).Include(n => n.Vehicle);
        //    return View(await ashtvinayakTravelAppContext.ToListAsync());
        //}

        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 10; // Number of records per page
            int totalRecords = await _context.Notifications.Where(x => !x.IsDeleted).CountAsync(); // Get total number of notifications
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize); // Calculate total pages

            var notifications = await _context.Notifications.Where(x => !x.IsDeleted)
                                              .Include(n => n.Trip)
                                              .Include(n => n.Vehicle)
                                              .OrderByDescending(n => n.NotificationId) // Ordering by NotificationId in descending order
                                              .Skip((page - 1) * pageSize) // Skip records for previous pages
                                              .Take(pageSize) // Take only the records for the current page
                                              .ToListAsync();

            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;

            return View(notifications);
        }


        // GET: Notifications/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notification = await _context.Notifications.Where(x => !x.IsDeleted)
                .Include(n => n.Trip)
                .Include(n => n.Vehicle)
                .FirstOrDefaultAsync(m => m.NotificationId == id);
            if (notification == null)
            {
                return NotFound();
            }

            return View(notification);
        }

        // GET: Notifications/Create
        public IActionResult Create()
        {
            ViewData["TripId"] = new SelectList(_context.Trips.Where(x => !x.IsDeleted), "TripId", "TourName");
            ViewData["VehicleId"] = new SelectList(_context.Vehicles.Where(x => !x.IsDeleted), "VehicleId", "VehicleNumber");


          

            return View();
        }

        // POST: Notifications/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NotificationId,TripId,NotificationMessage,NotificationDate,CreatedAt,UpdatedAt,VehicleId")] Notification notification)
        {
            if (ModelState.IsValid)
            {
                _context.Add(notification);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["TripId"] = new SelectList(_context.Trips.Where(x => !x.IsDeleted), "TripId", "TourName", notification.TripId);
            ViewData["VehicleId"] = new SelectList(_context.Vehicles.Where(x => !x.IsDeleted), "VehicleId", "VehicleNumber", notification.VehicleId);
            return View(notification);
        }

        // GET: Notifications/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return NotFound();
            }
            ViewData["TripId"] = new SelectList(_context.Trips.Where(x => !x.IsDeleted), "TripId", "TourName", notification.TripId);
            ViewData["VehicleId"] = new SelectList(_context.Vehicles.Where(x => !x.IsDeleted), "VehicleId", "VehicleNumber", notification.VehicleId);




            return View(notification);
        }

        // POST: Notifications/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("NotificationId,TripId,NotificationMessage,NotificationDate,CreatedAt,UpdatedAt,VehicleId")] Notification notification)
        {
            if (id != notification.NotificationId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(notification);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NotificationExists(notification.NotificationId))
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
            ViewData["TripId"] = new SelectList(_context.Trips.Where(x => !x.IsDeleted), "TripId", "TourName", notification.TripId);
            ViewData["VehicleId"] = new SelectList(_context.Vehicles.Where(x => !x.IsDeleted), "VehicleId", "VehicleId", notification.VehicleId);
            return View(notification);
        }

        // GET: Notifications/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notification = await _context.Notifications
                .Include(n => n.Trip)
                .Include(n => n.Vehicle)
                .FirstOrDefaultAsync(m => m.NotificationId == id);
            if (notification == null)
            {
                return NotFound();
            }

            return View(notification);
        }

        // POST: Notifications/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                notification.IsDeleted = true;
                _context.Notifications.Update(notification);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool NotificationExists(int id)
        {
            return _context.Notifications.Any(e => e.NotificationId == id);
        }
    }
}
