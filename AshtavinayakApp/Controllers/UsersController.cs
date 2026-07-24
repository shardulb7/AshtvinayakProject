using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AshtavinayakAPP.Models;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AshtavinayakAPP.Controllers
{
    public class UsersController : Controller
    {
        private readonly AshtvinayakTravelContext _context;

        public UsersController(AshtvinayakTravelContext context)
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

        // GET: Users
        //public async Task<IActionResult> Index()
        //{
        //    return View(await _context.Users.ToListAsync());
        //}

        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 10; // Number of records per page
            int totalRecords = await _context.Users.Where(x => !x.IsDeleted).CountAsync(); // Get total number of users
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize); // Calculate total pages

            var users = await _context.Users.Where(x => !x.IsDeleted)
                                      .OrderByDescending(u => u.UserId) // Ordering by UserId in descending order
                                      .Skip((page - 1) * pageSize) // Skip records for previous pages
                                      .Take(pageSize) // Take only the records for the current page
                                      .ToListAsync();

            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;

            return View(users);
        }


        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,UserName,Email,PhoneNumber,PasswordHash,Role")] User user)
        {
            if (ModelState.IsValid)
            {
                // HIGH-11: Hash the plain-text password before persisting
                // This aligns MVC admin-created users with the API registration flow
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }


		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, [Bind("UserId,UserName,Email,PhoneNumber,PasswordHash,Role")] User user)
		{
			if (id != user.UserId)
			{
				return NotFound();
			}

			// Check if the email already exists in the database for other users, excluding the current user
			var existingUser = await _context.Users
				.FirstOrDefaultAsync(u => u.Email == user.Email && u.UserId != user.UserId);

			if (existingUser != null)
			{
				// If the email exists, add a model error
				ModelState.AddModelError("Email", "The email address is already in use by another user.");
				return View(user);
			}

			if (ModelState.IsValid)
			{
				try
				{
					// HIGH-11: Re-hash only when admin provides a new plain-text password.
					// A BCrypt hash always starts with '$2'; if the submitted value does not,
					// treat it as a new password and hash it. If it already looks like a hash
					// (admin left the field unchanged), preserve the existing DB hash.
					if (!string.IsNullOrWhiteSpace(user.PasswordHash) &&
					    !user.PasswordHash.StartsWith("$2", StringComparison.Ordinal))
					{
						user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
					}
					else if (string.IsNullOrWhiteSpace(user.PasswordHash))
					{
						// Admin left password blank — restore existing hash from DB
						var dbUser = await _context.Users.AsNoTracking()
						    .FirstOrDefaultAsync(u => u.UserId == user.UserId);
						if (dbUser != null) user.PasswordHash = dbUser.PasswordHash;
					}

					_context.Update(user);
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!UserExists(user.UserId))
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

			return View(user);
		}





		// GET: Users/Delete/5
		public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsDeleted = true;
                _context.Users.Update(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id && !e.IsDeleted);
        }
    }
}
