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
    public class FamilyBookingsController : Controller
    {
        private readonly AshtvinayakTravelContext _context;

        public FamilyBookingsController(AshtvinayakTravelContext context)
        {
            _context = context;
        }

        // GET: FamilyBookings
        //public async Task<IActionResult> Index()
        //{
        //    return View(await _context.FamilyBookings.ToListAsync());
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
            int pageSize = 10;
            int totalRecords = await _context.FamilyBookings.Where(x => !x.IsDeleted).CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            var familyBookings = await _context.FamilyBookings.Include(x=>x.User).Include(x=>x.Package)
    .Where(f => !f.IsDeleted)
    .OrderByDescending(f => f.FamilyId)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();


            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;

            return View(familyBookings);
        }


        // GET: FamilyBookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var familyBooking = await _context.FamilyBookings
                .FirstOrDefaultAsync(m => m.FamilyId == id);
            if (familyBooking == null)
            {
                return NotFound();
            }

            return View(familyBooking);
        }

        // GET: FamilyBookings/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users.Where(x => !x.IsDeleted), "UserId", "UserName");
            return View();
        }

        // POST: FamilyBookings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FamilyId,CarType,Date,Time,BookingId,UserId")] FamilyBooking familyBooking)
        {
            if (ModelState.IsValid)
            {
                _context.Add(familyBooking);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["UserId"] = new SelectList(_context.Users.Where(x => !x.IsDeleted), "UserId", "UserName");

            return View(familyBooking);
        }

        // GET: FamilyBookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var familyBooking = await _context.FamilyBookings.FindAsync(id);
            if (familyBooking == null)
            {
                return NotFound();
            }
            return View(familyBooking);
        }

        // POST: FamilyBookings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("FamilyId,CarType,Date,Time,BookingId,UserId")] FamilyBooking familyBooking)
        {
            if (id != familyBooking.FamilyId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(familyBooking);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FamilyBookingExists(familyBooking.FamilyId))
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
            return View(familyBooking);
        }

        // GET: FamilyBookings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var familyBooking = await _context.FamilyBookings
                .FirstOrDefaultAsync(m => m.FamilyId == id);
            if (familyBooking == null)
            {
                return NotFound();
            }

            return View(familyBooking);
        }

        // POST: FamilyBookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var familyBooking = await _context.FamilyBookings.FindAsync(id);
            if (familyBooking != null)
            {
                familyBooking.IsDeleted = true;
                _context.FamilyBookings.Update(familyBooking);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FamilyBookingExists(int id)
        {
            return _context.FamilyBookings.Any(e => e.FamilyId == id);
        }
    }
}
