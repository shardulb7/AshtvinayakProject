using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AshtavinayakAPP.Models;

namespace AshtavinayakAPP.Controllers
{
    public class PackagesController : Controller
    {
        private readonly AshtvinayakTravelContext _context;

        public PackagesController(AshtvinayakTravelContext context)
        {
            _context = context;
        }

        // GET: Packages
        //public async Task<IActionResult> Index()
        //{
        //    var ashtvinayakTravelAppContext = _context.Packages.Include(p => p.Category).Include(p => p.City);
        //    return View(await ashtvinayakTravelAppContext.ToListAsync());
        //}

        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 10; // Number of records per page
            int totalRecords = await _context.Packages.Where(x => !x.IsDeleted).CountAsync(); // Get total number of packages
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize); // Calculate total pages

            var packages = await _context.Packages.Where(x => !x.IsDeleted)
                                         .Include(p => p.Category) // Include related Category
                                         .Include(p => p.City)
                                         .OrderBy(p => p.PackageName) // Sort by PackageName (modify if needed)
                                         .Skip((page - 1) * pageSize) // Skip records for previous pages
                                         .Take(pageSize) // Take only the records for the current page
                                         .ToListAsync();

            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;

            return View(packages);
        }

        // GET: Packages/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var package = await _context.Packages.Where(x => !x.IsDeleted)
                .Include(p => p.Category)
                .Include(p => p.City)
                .FirstOrDefaultAsync(m => m.PackageId == id);
            if (package == null)
            {
                return NotFound();
            }

            return View(package);
        }

        // GET: Packages/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories.Where(x => !x.IsDeleted), "CategoryId", "CategoryName");
            ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName");
            ViewData["DestinationId"] = new SelectList(_context.TourDestinations.Where(x => !x.IsDeleted), "Id", "DestinationName");
            return View();
        }

        // POST: Packages/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PackageId,PackageName,Duration,CategoryId,CityId,Inclusions,Exclusions,AdultPrice,Child3To8YrswithSeat,Child3To8YrsWithoutSeat,PkgPersonCount,IsCar,CarType,Itinerary,CarTotalSeat,CarPackagePrice,FamilyRoomChargePerPerson,SingleSharingChargePerPerson,DoubleSharingChargePerPerson,TripleSharingChargePerPerson,DestinationId")] Package package)
        {
            if (ModelState.IsValid)
            {
                _context.Add(package);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories.Where(x => !x.IsDeleted), "CategoryId", "CategoryName", package.CategoryId);
            ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName", package.CityId);
            ViewData["DestinationId"] = new SelectList(_context.TourDestinations.Where(x => !x.IsDeleted), "Id", "DestinationName", package.DestinationId);
            return View(package);
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var package = await _context.Packages.FindAsync(id);
            if (package == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories.Where(x => !x.IsDeleted), "CategoryId", "CategoryName", package.CategoryId);
            ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName", package.CityId);
            ViewData["DestinationId"] = new SelectList(_context.TourDestinations.Where(x => !x.IsDeleted), "Id", "DestinationName", package.DestinationId);
            return View(package);
        }
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PackageId,PackageName,Duration,CategoryId,CityId,Inclusions,Exclusions,AdultPrice,Child3To8YrswithSeat,Child3To8YrsWithoutSeat,PkgPersonCount,IsCar,CarType,Itinerary,CarTotalSeat,CarPackagePrice,FamilyRoomChargePerPerson,SingleSharingChargePerPerson,DoubleSharingChargePerPerson,TripleSharingChargePerPerson,DestinationId")] Package package)
        {
            if (id != package.PackageId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(package);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PackageExists(package.PackageId))
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
            ViewData["CategoryId"] = new SelectList(_context.Categories.Where(x => !x.IsDeleted), "CategoryId", "CategoryName", package.CategoryId);
            ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName", package.CityId);
            ViewData["DestinationId"] = new SelectList(_context.TourDestinations.Where(x => !x.IsDeleted), "Id", "DestinationName", package.DestinationId);
            return View(package);
        }

        // GET: Packages/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var package = await _context.Packages
                .Include(p => p.Category)
                .Include(p => p.City)
                .FirstOrDefaultAsync(m => m.PackageId == id);
            if (package == null)
            {
                return NotFound();
            }

            return View(package);
        }

        // POST: Packages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var package = await _context.Packages.FindAsync(id);
            if (package != null)
            {
                package.IsDeleted = true;
                _context.Packages.Update(package);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PackageExists(int id)
        {
            return _context.Packages.Any(e => e.PackageId == id && !e.IsDeleted);
        }
    }
}
