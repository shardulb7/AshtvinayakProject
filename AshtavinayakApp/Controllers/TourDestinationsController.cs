using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AshtavinayakAPP.Models;

namespace AshtavinayakAPP.Controllers
{
    public class TourDestinationsController : Controller
    {
        private readonly AshtvinayakTravelContext _context;

        public TourDestinationsController(AshtvinayakTravelContext context)
        {
            _context = context;
        }

        // CRIT-14: session guard
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userSession = context.HttpContext.Session.GetString("User");
            if (string.IsNullOrEmpty(userSession))
                context.Result = new RedirectToActionResult("Login", "Home", null);
            base.OnActionExecuting(context);
        }

        // GET: TourDestinations
        public async Task<IActionResult> Index()
        {
            return View(await _context.TourDestinations.Where(x=>!x.IsDeleted).ToListAsync());
        }

        // GET: TourDestinations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tourDestination = await _context.TourDestinations
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tourDestination == null)
            {
                return NotFound();
            }

            return View(tourDestination);
        }

        // GET: TourDestinations/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TourDestinations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,DestinationName,Description")] TourDestination tourDestination)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tourDestination);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tourDestination);
        }

        // GET: TourDestinations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tourDestination = await _context.TourDestinations.FindAsync(id);
            if (tourDestination == null)
            {
                return NotFound();
            }
            return View(tourDestination);
        }

        // POST: TourDestinations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DestinationName,Description")] TourDestination tourDestination)
        {
            if (id != tourDestination.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tourDestination);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TourDestinationExists(tourDestination.Id))
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
            return View(tourDestination);
        }

        // GET: TourDestinations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tourDestination = await _context.TourDestinations
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tourDestination == null)
            {
                return NotFound();
            }

            return View(tourDestination);
        }

        // POST: TourDestinations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tourDestination = await _context.TourDestinations.FindAsync(id);
            if (tourDestination != null)
            {
                tourDestination.IsDeleted = true;
                _context.TourDestinations.Update(tourDestination);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TourDestinationExists(int id)
        {
            return _context.TourDestinations.Any(e => e.Id == id && !e.IsDeleted);
        }
    }
}
