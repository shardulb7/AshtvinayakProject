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
    public class SeatsController : Controller
    {
        private readonly AshtvinayakTravelContext _context;

        public SeatsController(AshtvinayakTravelContext context)
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

        // GET: Seats
        public async Task<IActionResult> Index()
        {
            var ashtvinayakTravelAppContext = _context.Seats.Where(x => !x.IsDeleted).Include(s => s.Package).Include(s=>s.Trip);
            return View(await ashtvinayakTravelAppContext.ToListAsync());
        }

        // GET: Seats/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var seat = await _context.Seats
                .Include(s => s.Package)
                .FirstOrDefaultAsync(m => m.SeatId == id);
            if (seat == null)
            {
                return NotFound();
            }

            return View(seat);
        }

        // GET: Seats/Create
        public IActionResult Create()
        {
            // Set ViewData to include PackageId as the value and PackageName as the display text
            ViewData["PackageId"] = new SelectList(_context.Packages.Where(x => !x.IsDeleted), "PackageId", "PackageName");
            ViewData["TripId"] = new SelectList(_context.Trips.Where(x => !x.IsDeleted), "TripId", "TourName");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Seat seat)
        {
            if (ModelState.IsValid)
            {
                _context.Add(seat);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            // Make sure to pass the selected value to the dropdown on POST
            ViewData["PackageId"] = new SelectList(_context.Packages.Where(x => !x.IsDeleted), "PackageId", "PackageName", seat.PackageId);
            ViewData["TripId"] = new SelectList(_context.Trips.Where(x => !x.IsDeleted), "TripId", "TourName", seat.TripId);

            return View(seat);
        }


        // GET: Seats/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var seat = await _context.Seats.FindAsync(id);
            if (seat == null)
            {
                return NotFound();
            }

            // Update the SelectList to display PackageName instead of PackageId
            ViewData["PackageId"] = new SelectList(
                _context.Packages.Where(x => !x.IsDeleted),
                "PackageId",
                "PackageName", // Use "PackageName" as the display text
                seat.PackageId
            );
            ViewData["TripId"] = new SelectList(_context.Trips.Where(x => !x.IsDeleted), "TripId", "TourName", seat.TripId);

            return View(seat);
        }

        // POST: Seats/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,Seat seat)
        {
            if (id != seat.SeatId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(seat);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SeatExists(seat.SeatId))
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

            // Update the SelectList on POST as well
            ViewData["PackageId"] = new SelectList(
                _context.Packages.Where(x => !x.IsDeleted),
                "PackageId",
                "PackageName", // Use "PackageName" as the display text
                seat.PackageId
            );
            ViewData["TripId"] = new SelectList(_context.Trips.Where(x => !x.IsDeleted), "TripId", "TourName", seat.TripId);

            return View(seat);
        }


        // GET: Seats/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var seat = await _context.Seats
                .Include(s => s.Package)
                .FirstOrDefaultAsync(m => m.SeatId == id);
            if (seat == null)
            {
                return NotFound();
            }

            return View(seat);
        }

        // POST: Seats/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var seat = await _context.Seats.FindAsync(id);
            if (seat != null)
            {
                seat.IsDeleted = true;
                _context.Seats.Update(seat);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SeatExists(int id)
        {
            return _context.Seats.Any(e => e.SeatId == id && !e.IsDeleted);
        }
    }
}
