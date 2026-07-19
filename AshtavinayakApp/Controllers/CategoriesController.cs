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
    public class CategoriesController : Controller
    {
        private readonly AshtvinayakTravelContext _context;

        public CategoriesController(AshtvinayakTravelContext context)
        {
            _context = context;
        }



        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 10; // Number of records per page
            int totalRecords = await _context.Categories.CountAsync(); // Get total number of categories
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize); // Calculate total pages


            var categories = await _context.Categories.Where(c => !c.IsDeleted)
                .Include(x => x.City)
                .Include(x => x.TourDestination)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            //var categories = await _context.Categories.Where(x=>!x.IsDeleted)
            //                               .Include(c => c.City)
            //                               .Include(c=>c.TourDestinationId)
            //                               .OrderBy(c => c.CategoryName) // Sort by CategoryName (modify as needed)
            //                               .Skip((page - 1) * pageSize) // Skip records for previous pages
            //                               .Take(pageSize) // Take only the records for the current page
            //                               .ToListAsync();

            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;

            return View(categories);
        }


        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.City)
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName");
            ViewData["TourDestinationId"] = new SelectList(_context.TourDestinations.Where(x => !x.IsDeleted), "Id", "DestinationName");
            return View();
        }

        // POST: Categories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryId,CategoryName,CityId,TourDestinationId")] Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName", category.CityId);
            ViewData["TourDestinationId"] = new SelectList(_context.TourDestinations.Where(x => !x.IsDeleted), "Id", "DestinationName",category.TourDestinationId);


            return View(category);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            //ViewData["CityId"] = new SelectList(_context.Cities, "CityId", "CityId", category.CityId);
            ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName", category.CityId);
            ViewData["TourDestinationId"] = new SelectList(_context.TourDestinations.Where(x => !x.IsDeleted), "Id", "DestinationName", category.TourDestinationId);


            return View(category);
        }

        // POST: Categories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryId,CategoryName,CityId,TourDestinationId")] Category category)
        {
            if (id != category.CategoryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.CategoryId))
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
            //ViewData["CityId"] = new SelectList(_context.Cities, "CityId", "CityId", category.CityId);
            ViewData["CityId"] = new SelectList(_context.Cities.Where(x => !x.IsDeleted), "CityId", "CityName", category.CityId);
            ViewData["TourDestinationId"] = new SelectList(_context.TourDestinations.Where(x => !x.IsDeleted), "Id", "DestinationName", category.TourDestinationId);

            return View(category);
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.City)
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                category.IsDeleted = true;
                _context.Categories.Update(category);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.CategoryId == id && !e.IsDeleted);
        }
    }
}
