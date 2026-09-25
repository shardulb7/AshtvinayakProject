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
    public class DropUpsController : Controller
    {
        private readonly AshtvinayakTravelContext _context;

        public DropUpsController(AshtvinayakTravelContext context)
        {
            _context = context;
        }

        // GET: DropUps
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
            int totalRecords = await _context.DropUps.Where(x => !x.IsDeleted).CountAsync(); // HIGH-09: exclude soft-deleted from count
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize); // Calculate total pages

            var dropUps = await _context.DropUps.Where(x => !x.IsDeleted)
                                         .Include(d => d.City)
                                         .OrderByDescending(d => d.DroppointId) // Ordering by DropUpId in descending order
                                         .Skip((page - 1) * pageSize) // Skip records for previous pages
                                         .Take(pageSize) // Take only the records for the current page
                                         .ToListAsync();

            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;

            return View(dropUps);
        }


        // GET: DropUps/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dropUp = await _context.DropUps
                .Include(d => d.City)
                .FirstOrDefaultAsync(m => m.DroppointId == id);
            if (dropUp == null)
            {
                return NotFound();
            }

            return View(dropUp);
        }

        // GET: DropUps/Create
        public IActionResult Create()
        {
            ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName");
            ViewData["PackageId"] = new SelectList(_context.Packages.Where(x => !x.IsDeleted), "PackageId", "PackageName");
            return View();
        }

        // POST: DropUps/BulkCreate — saves multiple drop points in one submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkCreate(int cityId, int packageId,
            [FromForm] List<string> points)
        {
            if (cityId == 0 || packageId == 0 || points == null || !points.Any(p => !string.IsNullOrWhiteSpace(p)))
            {
                TempData["Error"] = "City, Package and at least one drop point are required.";
                ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName");
                return View("Create");
            }

            foreach (var point in points.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                _context.DropUps.Add(new DropUp
                {
                    CityId    = cityId,
                    PackageId = packageId,
                    DropPoint = point.Trim(),
                    IsDeleted = false
                });
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: DropUps/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dropUp = await _context.DropUps.FindAsync(id);
            if (dropUp == null)
            {
                return NotFound();
            }
            ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName", dropUp.CityId);
            ViewData["PackageId"] = new SelectList(_context.Packages.Where(x => !x.IsDeleted), "PackageId", "PackageName", dropUp.PackageId);
            return View(dropUp);
        }

        // POST: DropUps/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DroppointId,DropPoint,CityId,PackageId")] DropUp dropUp)
        {
            if (id != dropUp.DroppointId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(dropUp);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DropUpExists(dropUp.DroppointId))
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
            ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName", dropUp.CityId);
            ViewData["PackageId"] = new SelectList(_context.Packages.Where(x => !x.IsDeleted), "PackageId", "PackageName", dropUp.PackageId);
            return View(dropUp);
        }

        // GET: DropUps/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dropUp = await _context.DropUps
                .Include(d => d.City)
                .FirstOrDefaultAsync(m => m.DroppointId == id);
            if (dropUp == null)
            {
                return NotFound();
            }

            return View(dropUp);
        }

        // POST: DropUps/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dropUp = await _context.DropUps.FindAsync(id);
            if (dropUp != null)
            {
                dropUp.IsDeleted = true;
                _context.DropUps.Update(dropUp);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DropUpExists(int id)
        {
            return _context.DropUps.Any(e => e.DroppointId == id && !e.IsDeleted);
        }
    }
}

