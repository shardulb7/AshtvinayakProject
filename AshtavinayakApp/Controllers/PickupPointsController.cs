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
    public class PickupPointsController : Controller
    {
        private readonly AshtvinayakTravelContext _context;

        public PickupPointsController(AshtvinayakTravelContext context)
        {
            _context = context;
        }

        // GET: PickupPoints
        //public async Task<IActionResult> Index()
        //{
        //    var ashtvinayakTravelAppContext = _context.PickupPoints.Include(p => p.City).Include(p => p.Package);
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
            int totalRecords = await _context.PickupPoints.Where(x => !x.IsDeleted).CountAsync(); // Get total number of pickup points
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize); // Calculate total pages

            var pickupPoints = await _context.PickupPoints.Where(x => !x.IsDeleted)
                                             .Include(p => p.City)
                                             .Include(p => p.Package)
                                             .OrderByDescending(p => p.PickupPointId) // Ordering by PickupPointId in descending order
                                             .Skip((page - 1) * pageSize) // Skip records for previous pages
                                             .Take(pageSize) // Take only the records for the current page
                                             .ToListAsync();

            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;

            return View(pickupPoints);
        }

        // GET: PickupPoints/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pickupPoint = await _context.PickupPoints
                .Include(p => p.City)
                .Include(p => p.Package)
                .FirstOrDefaultAsync(m => m.PickupPointId == id);
            if (pickupPoint == null)
            {
                return NotFound();
            }

            return View(pickupPoint);
        }

        // GET: PickupPoints/Create — now renders multi-add form
        public IActionResult Create()
        {
            ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName");
            ViewData["PackageId"] = new SelectList(_context.Packages.Where(x => !x.IsDeleted), "PackageId", "PackageName");
            return View();
        }

        // POST: PickupPoints/BulkCreate — saves multiple pickup points in one submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkCreate(int cityId, int packageId,
            [FromForm] List<string> points, [FromForm] List<string> times)
        {
            if (cityId == 0 || packageId == 0 || points == null || !points.Any(p => !string.IsNullOrWhiteSpace(p)))
            {
                TempData["Error"] = "City, Package and at least one pickup point are required.";
                ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName");
                return View("Create");
            }

            for (int i = 0; i < points.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(points[i])) continue;
                var pp = new PickupPoint
                {
                    CityId       = cityId,
                    PackageId    = packageId,
                    PickupPoint1 = points[i].Trim(),
                    Time         = (i < times.Count && TimeOnly.TryParse(times[i], out var t)) ? t : null,
                    IsDeleted    = false
                };
                _context.PickupPoints.Add(pp);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: PickupPoints/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pickupPoint = await _context.PickupPoints.FindAsync(id);
            if (pickupPoint == null)
            {
                return NotFound();
            }
            ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName", pickupPoint.CityId);
            ViewData["PackageId"] = new SelectList(_context.Packages.Where(x => !x.IsDeleted), "PackageId", "PackageName", pickupPoint.PackageId);
            return View(pickupPoint);
        }

        // POST: PickupPoints/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PickupPointId,CityId,PickupPoint1,Time,PackageId")] PickupPoint pickupPoint)
        {
            if (id != pickupPoint.PickupPointId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pickupPoint);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PickupPointExists(pickupPoint.PickupPointId))
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
            ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName", pickupPoint.CityId);
            ViewData["PackageId"] = new SelectList(_context.Packages.Where(x => !x.IsDeleted), "PackageId", "PackageName", pickupPoint.PackageId);
            return View(pickupPoint);
        }

        // GET: PickupPoints/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pickupPoint = await _context.PickupPoints
                .Include(p => p.City)
                .Include(p => p.Package)
                .FirstOrDefaultAsync(m => m.PickupPointId == id);
            if (pickupPoint == null)
            {
                return NotFound();
            }

            return View(pickupPoint);
        }

        // POST: PickupPoints/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pickupPoint = await _context.PickupPoints.FindAsync(id);
            if (pickupPoint != null)
            {
                pickupPoint.IsDeleted = true;
                _context.PickupPoints.Update(pickupPoint);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PickupPointExists(int id)
        {
            return _context.PickupPoints.Any(e => e.PickupPointId == id && !e.IsDeleted);
        }
    }
}
