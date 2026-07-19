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
    public class HistoriesController : Controller
    {
        private readonly AshtvinayakTravelContext _context;

        public HistoriesController(AshtvinayakTravelContext context)
        {
            _context = context;
        }

        // GET: Histories
        //public async Task<IActionResult> Index()
        //{
        //    var ashtvinayakTravelAppContext = _context.Histories.Include(h => h.Booking).Include(h => h.Category).Include(h => h.City).Include(h => h.Package);
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
            int totalRecords = await _context.Histories.Where(x => !x.IsDeleted).CountAsync(); // Get total number of history records
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize); // Calculate total pages

            var histories = await _context.Histories.Where(x => !x.IsDeleted)
                                          .Include(h => h.Booking)
                                          .Include(h => h.Category)
                                          .Include(h => h.City)
                                          .Include(h => h.Package)
                                          .OrderByDescending(h => h.HistoryId) // Ordering by HistoryId in descending order
                                          .Skip((page - 1) * pageSize) // Skip records for previous pages
                                          .Take(pageSize) // Take only the records for the current page
                                          .ToListAsync();

            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;

            return View(histories);
        }


        // GET: Histories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var history = await _context.Histories
                .Include(h => h.Booking)
                .Include(h => h.Category)
                .Include(h => h.City)
                .Include(h => h.Package)
                .FirstOrDefaultAsync(m => m.HistoryId == id);
            if (history == null)
            {
                return NotFound();
            }

            return View(history);
        }

        // GET: Histories/Create
        public IActionResult Create()
        {
            ViewData["BookingId"] = new SelectList(_context.Bookings.Where(x => !x.IsDeleted), "BookingId", "BookingId");
            ViewData["CategoryId"] = new SelectList(_context.Categories.Where(x => !x.IsDeleted), "CategoryId", "CategoryName");
            ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName");
            ViewData["PackageId"] = new SelectList(_context.Packages.Where(x => !x.IsDeleted), "PackageId", "PackageName");
            return View();
        }

        // POST: Histories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CityId,PackageId,CategoryId,BookingId,HistoryId")] History history)
        {
            if (ModelState.IsValid)
            {
                _context.Add(history);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BookingId"] = new SelectList(_context.Bookings.Where(x => !x.IsDeleted), "BookingId", "BookingId", history.BookingId);
            ViewData["CategoryId"] = new SelectList(_context.Categories.Where(x => !x.IsDeleted), "CategoryId", "CategoryName", history.CategoryId);
            ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName", history.CityId);
            ViewData["PackageId"] = new SelectList(_context.Packages.Where(x => !x.IsDeleted), "PackageId", "PackageName", history.PackageId);
            return View(history);
        }

        // GET: Histories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var history = await _context.Histories.FindAsync(id);
            if (history == null)
            {
                return NotFound();
            }
            ViewData["BookingId"] = new SelectList(_context.Bookings.Where(x => !x.IsDeleted), "BookingId", "BookingId", history.BookingId);
            ViewData["CategoryId"] = new SelectList(_context.Categories.Where(x => !x.IsDeleted), "CategoryId", "CategoryName", history.CategoryId);
            ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName", history.CityId);
            ViewData["PackageId"] = new SelectList(_context.Packages.Where(x => !x.IsDeleted), "PackageId", "PackageName", history.PackageId);
            return View(history);
        }

        // POST: Histories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CityId,PackageId,CategoryId,BookingId,HistoryId")] History history)
        {
            if (id != history.HistoryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(history);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HistoryExists(history.HistoryId))
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
            ViewData["BookingId"] = new SelectList(_context.Bookings.Where(x => !x.IsDeleted), "BookingId", "BookingId", history.BookingId);
            ViewData["CategoryId"] = new SelectList(_context.Categories.Where(x => !x.IsDeleted), "CategoryId", "CategoryName", history.CategoryId);
            ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName", history.CityId);
            ViewData["PackageId"] = new SelectList(_context.Packages.Where(x => !x.IsDeleted), "PackageId", "PackageName", history.PackageId);
            return View(history);
        }

        // GET: Histories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var history = await _context.Histories
                .Include(h => h.Booking)
                .Include(h => h.Category)
                .Include(h => h.City)
                .Include(h => h.Package)
                .FirstOrDefaultAsync(m => m.HistoryId == id);
            if (history == null)
            {
                return NotFound();
            }

            return View(history);
        }

        // POST: Histories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var history = await _context.Histories.FindAsync(id);
            if (history != null)
            {
                history.IsDeleted = true;
                _context.Histories.Update(history);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool HistoryExists(int id)
        {
            return _context.Histories.Any(e => e.HistoryId == id);
        }
    }
}
