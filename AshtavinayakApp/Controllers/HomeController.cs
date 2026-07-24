using AshtavinayakApp.Models;
using AshtavinayakAPP.Models;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace AshtavinayakApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AshtvinayakTravelContext _context;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, AshtvinayakTravelContext context, IConfiguration configuration)
        {
            _logger        = logger;
            _context       = context;
            _configuration = configuration;
        }


        // Session guard: redirect unauthenticated requests to Login.
        // Must use `override` — without it, the MVC pipeline never calls this method.
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Login and Logout actions must be exempt — otherwise we create an infinite redirect
            var action = context.ActionDescriptor.RouteValues["action"];
            if (action == "Login" || action == "Logout")
            {
                base.OnActionExecuting(context);
                return;
            }

            var userSession = context.HttpContext.Session.GetString("User");
            if (string.IsNullOrEmpty(userSession))
            {
                context.Result = new RedirectToActionResult("Login", "Home", null);
                return;
            }
            base.OnActionExecuting(context);
        }
        public IActionResult Login()
        {
            return View();
        }



        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var adminUsername = _configuration["Admin:Username"];
            var adminPasswordHash = _configuration["Admin:PasswordHash"];

            bool isValid = !string.IsNullOrWhiteSpace(adminUsername)
                        && !string.IsNullOrWhiteSpace(adminPasswordHash)
                        && username == adminUsername
                        && BCrypt.Net.BCrypt.Verify(password, adminPasswordHash);

            if (isValid)
            {
                HttpContext.Session.SetString("User", username);
                return RedirectToAction("Index", "Home");
            }

            _logger.LogWarning("Failed admin login attempt for username: {Username}", username);
            ViewBag.Error = "Invalid username or password.";
            return View();
        }


        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Clear session
            return RedirectToAction("Login", "Home"); // Redirect to Login page after logout
        }



        public async Task<IActionResult> Index()
        {
            var userSession = HttpContext.Session.GetString("User");
            if (string.IsNullOrEmpty(userSession))
            {
                return RedirectToAction("Login");
            }

            // Fix 9: use async EF calls — synchronous .Count()/.ToList() block the thread pool
            var dashData = new Dashdata
            {
                TotalBooking      = await _context.Bookings.Where(x => !x.IsDeleted).CountAsync(),
                TotalTrips        = await _context.Trips.Where(x => !x.IsDeleted).CountAsync(),
                TotalPackages     = await _context.Packages.Where(x => !x.IsDeleted).CountAsync(),
                TotalTransactions = await _context.Transactions.Where(x => !x.IsDeleted).CountAsync()
            };

            var recentPackages = await _context.Packages.Where(x => !x.IsDeleted)
                         .Include(p => p.City)
                         .Include(p => p.Category)
                         .OrderByDescending(p => p.PackageId)
                         .Take(3)
                         .ToListAsync();

            var recentBookings = await _context.Bookings.Where(x => !x.IsDeleted)
                          .OrderByDescending(b => b.BookingId)
                          .Take(6)
                          .Include(b => b.User)
                          .Include(b => b.Trip)
                          .Include(b => b.PickupPoint)
                          .ToListAsync();

            var recentTrips = await _context.Trips.Where(x => !x.IsDeleted)
                .OrderByDescending(t => t.TripId)
                .Take(6)
                .Include(t => t.Package)
                .ToListAsync();

            ViewBag.RecentTrips    = recentTrips;
            ViewBag.RecentBookings = recentBookings;
            ViewBag.RecentPackages = recentPackages;

            return View(dashData);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}