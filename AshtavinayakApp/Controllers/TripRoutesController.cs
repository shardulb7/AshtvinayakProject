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
    public class TripRoutesController : Controller
    {
        private readonly AshtvinayakTravelContext _context;

        public TripRoutesController(AshtvinayakTravelContext context)
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


        // GET: TripRoutes
        //public async Task<IActionResult> Index()
        //{
        //    var ashtvinayakTravelAppContext = _context.TripRoutes.Include(t => t.City).Include(t => t.Package);
        //    return View(await ashtvinayakTravelAppContext.ToListAsync());
        //}

        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 10; // Number of records per page
            int totalRecords = await _context.TripRoutes.Where(x => !x.IsDeleted).CountAsync(); // Get total number of trip routes
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize); // Calculate total pages

            var tripRoutes = await _context.TripRoutes.Where(x => !x.IsDeleted)
                                           .Include(t => t.City)
                                           .Include(t => t.Package)
                                           .OrderByDescending(t => t.Trid) // Ordering by TripRouteId in descending order
                                           .Skip((page - 1) * pageSize) // Skip records for previous pages
                                           .Take(pageSize) // Take only the records for the current page
                                           .ToListAsync();

            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;

            return View(tripRoutes);
        }


        // GET: TripRoutes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tripRoute = await _context.TripRoutes
                .Include(t => t.City)
                .Include(t => t.Package)
                .FirstOrDefaultAsync(m => m.Trid == id);
            if (tripRoute == null)
            {
                return NotFound();
            }

            return View(tripRoute);
        }

        // GET: TripRoutes/Create
        public IActionResult Create()
        {
            ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName");
            ViewData["PackageId"] = new SelectList(_context.Packages.Where(x => !x.IsDeleted), "PackageId", "PackageName");
            return View();
        }

        // POST: TripRoutes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create([Bind("Trid,PackageId,PointName,Day,CityId")] TripRoute tripRoute)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        _context.Add(tripRoute);
        //        await _context.SaveChangesAsync();
        //        return RedirectToAction(nameof(Index));
        //    }
        //    ViewData["CityId"] = new SelectList(_context.Cities, "CityId", "CityName", tripRoute.CityId);
        //    ViewData["PackageId"] = new SelectList(_context.Packages, "PackageId", "PackageName", tripRoute.PackageId);
        //    return View(tripRoute);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create(string PointNames, [Bind("Trid,PackageId,Day,CityId")] TripRoute tripRoute)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        ViewData["CityId"] = new SelectList(_context.Cities, "CityId", "CityName", tripRoute.CityId);
        //        ViewData["PackageId"] = new SelectList(_context.Packages, "PackageId", "PackageName", tripRoute.PackageId);
        //        return View(tripRoute);
        //    }

        //    if (string.IsNullOrEmpty(PointNames))
        //    {
        //        ModelState.AddModelError("PointNames", "Pickup points are required.");
        //        return View(tripRoute);
        //    }

        //    var pickupPoints = PointNames.Split('|', StringSplitOptions.RemoveEmptyEntries);

        //    foreach (var point in pickupPoints)
        //    {
        //        var newTripRoute = new TripRoute
        //        {
        //            CityId = tripRoute.CityId,
        //            PackageId = tripRoute.PackageId,
        //            Day = tripRoute.Day,
        //            PointName = point.Trim()
        //        };

        //        _context.TripRoutes.Add(newTripRoute);
        //    }

        //    await _context.SaveChangesAsync();
        //    return RedirectToAction(nameof(Index));
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string PointNames, [Bind("Trid,PackageId,Day,CityId")] TripRoute tripRoute)
        {


       
            if (!ModelState.IsValid)
            {
                ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName", tripRoute.CityId);
                ViewData["PackageId"] = new SelectList(_context.Packages.Where(x => !x.IsDeleted), "PackageId", "PackageName", tripRoute.PackageId);
                return View(tripRoute);
            }

            if (string.IsNullOrEmpty(PointNames))
            {
                ModelState.AddModelError("PointNames", "Pickup points are required.");
                return View(tripRoute);
            }

            // Store multiple pickup points as a single string separated by "|"
            tripRoute.PointName = PointNames;

            _context.TripRoutes.Add(tripRoute);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }






        [HttpGet]
        public JsonResult GetPackagesByCity(int cityId)
        {
            var packages = _context.Packages
                .Where(p => p.CityId == cityId)
                .Select(p => new { p.PackageId, p.PackageName })
                .ToList();

            return Json(packages);
        }



        // GET: TripRoutes/Edit/5
        //public async Task<IActionResult> Edit(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var tripRoute = await _context.TripRoutes.FindAsync(id);
        //    if (tripRoute == null)
        //    {
        //        return NotFound();
        //    }
        //    ViewData["CityId"] = new SelectList(_context.Cities, "CityId", "CityName", tripRoute.CityId);
        //    ViewData["PackageId"] = new SelectList(_context.Packages, "PackageId", "PackageName", tripRoute.PackageId);
        //    return View(tripRoute);
        //}

        // POST: TripRoutes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, [Bind("Trid,PackageId,PointName,Day,CityId")] TripRoute tripRoute)
        //{
        //    if (id != tripRoute.Trid)
        //    {
        //        return NotFound();
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            _context.Update(tripRoute);
        //            await _context.SaveChangesAsync();
        //        }
        //        catch (DbUpdateConcurrencyException)
        //        {
        //            if (!TripRouteExists(tripRoute.Trid))
        //            {
        //                return NotFound();
        //            }
        //            else
        //            {
        //                throw;
        //            }
        //        }
        //        return RedirectToAction(nameof(Index));
        //    }
        //    ViewData["CityId"] = new SelectList(_context.Cities, "CityId", "CityName", tripRoute.CityId);
        //    ViewData["PackageId"] = new SelectList(_context.Packages, "PackageId", "PackageName", tripRoute.PackageId);
        //    return View(tripRoute);
        //}

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tripRoute = await _context.TripRoutes.FindAsync(id);
            if (tripRoute == null)
            {
                return NotFound();
            }

            ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName", tripRoute.CityId);
            ViewData["PackageId"] = new SelectList(_context.Packages.Where(x => !x.IsDeleted), "PackageId", "PackageName", tripRoute.PackageId);

            return View(tripRoute);  // Ensure PointName is included in the model
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string PointNames, [Bind("Trid,PackageId,PointName,Day,CityId")] TripRoute tripRoute)
        {
            if (id != tripRoute.Trid)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(PointNames))
            {
                ModelState.AddModelError("PointNames", "Pickup points are required.");
                ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName", tripRoute.CityId);
                ViewData["PackageId"] = new SelectList(_context.Packages.Where(x => !x.IsDeleted), "PackageId", "PackageName", tripRoute.PackageId);
                return View(tripRoute);
            }

            // Store multiple pickup points as a single string separated by "|"
            tripRoute.PointName = PointNames;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tripRoute);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TripRouteExists(tripRoute.Trid))
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

            ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName", tripRoute.CityId);
            ViewData["PackageId"] = new SelectList(_context.Packages.Where(x => !x.IsDeleted), "PackageId", "PackageName", tripRoute.PackageId);
            return View(tripRoute);
        }

        // GET: TripRoutes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tripRoute = await _context.TripRoutes
                .Include(t => t.City)
                .Include(t => t.Package)
                .FirstOrDefaultAsync(m => m.Trid == id);
            if (tripRoute == null)
            {
                return NotFound();
            }

            return View(tripRoute);
        }

        // POST: TripRoutes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tripRoute = await _context.TripRoutes.FindAsync(id);
            if (tripRoute != null)
            {
                tripRoute.IsDeleted = true;
                _context.TripRoutes.Update(tripRoute);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TripRouteExists(int id)
        {
            return _context.TripRoutes.Any(e => e.Trid == id);
        }
    }
}
