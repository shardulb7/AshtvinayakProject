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
    public class TripsController : Controller
    {
        private readonly AshtvinayakTravelContext _context;

        public TripsController(AshtvinayakTravelContext context)
        {
            _context = context;
        }

        // GET: Trips
        //public async Task<IActionResult> Index()
        //{
        //    var ashtvinayakTravelAppContext = _context.Trips.Include(t => t.Package);
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

        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 10; // Number of records per page
            int totalRecords = await _context.Trips.Where(x => !x.IsDeleted).CountAsync(); // Get total number of trips
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize); // Calculate total pages

            var trips = await _context.Trips.Where(x => !x.IsDeleted)
                                      .Include(t => t.Package) // Include related Package data
                                      .Include(t => t.Category)
                                      .OrderByDescending(t => t.TripId) // Ordering by TripId in descending order
                                      .Skip((page - 1) * pageSize) // Skip records for previous pages
                                      .Take(pageSize) // Take only the records for the current page
                                      .ToListAsync();

            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;

            return View(trips);
        }


        // GET: Trips/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trip = await _context.Trips
                .Include(t => t.Package)
                 .Include(t => t.Category)
                .FirstOrDefaultAsync(m => m.TripId == id);
            if (trip == null)
            {
                return NotFound();
            }

            return View(trip);
        }

        // GET: Trips/Create
        public IActionResult Create()
        {
            ViewData["PackageId"] = new SelectList(_context.Packages.Where(x => !x.IsDeleted), "PackageId", "PackageName");

            ViewData["CategoryId"] = new SelectList(_context.Categories.Where(x => !x.IsDeleted), "CategoryId", "CategoryName");
            return View();
        }

        // POST: Trips/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Trip trip)
        {
            if (ModelState.IsValid)
            {
                _context.Add(trip);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PackageId"] = new SelectList(_context.Packages.Where(x => !x.IsDeleted), "PackageId", "PackageName", trip.PackageId);

            ViewData["CategoryId"] = new SelectList(_context.Categories.Where(x => !x.IsDeleted), "CategoryId", "CategoryName");


            return View(trip);
        }

        // GET: Trips/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trip = await _context.Trips.FindAsync(id);
            if (trip == null)
            {
                return NotFound();
            }
            ViewData["PackageId"] = new SelectList(_context.Packages.Where(x => !x.IsDeleted), "PackageId", "PackageName", trip.TripId);
            ViewData["CategoryId"] = new SelectList(_context.Categories.Where(x => !x.IsDeleted), "CategoryId", "CategoryName", trip.TripId);


            return View(trip);
        }

        // POST: Trips/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,Trip trip)
        {
            if (id != trip.TripId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(trip);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TripExists(trip.TripId))
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
            ViewData["PackageId"] = new SelectList(_context.Packages.Where(x => !x.IsDeleted), "PackageId", "PackageName", trip.PackageId);
            ViewData["CategoryId"] = new SelectList(_context.Categories.Where(x => !x.IsDeleted), "CategoryId", "CategoryName", trip.CategoryId);


            return View(trip);
        }

        // GET: Trips/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trip = await _context.Trips
                .Include(t => t.Package)
                .FirstOrDefaultAsync(m => m.TripId == id);
            if (trip == null)
            {
                return NotFound();
            }

            return View(trip);
        }

        // POST: Trips/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip != null)
            {
                trip.IsDeleted = true;
                _context.Trips.Update(trip);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TripExists(int id)
        {
            return _context.Trips.Any(e => e.TripId == id);
        }
    }
}
