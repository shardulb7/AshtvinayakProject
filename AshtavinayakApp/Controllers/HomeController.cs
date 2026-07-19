using AshtavinayakApp.Models;
using AshtavinayakAPP.Models;
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


        public HomeController(ILogger<HomeController> logger, AshtvinayakTravelContext context)
        {
            _logger = logger;
            _context = context;
        }

        //public override void OnActionExecuting(ActionExecutingContext context)
        //{
        //    var userSession = context.HttpContext.Session.GetString("User");
        //    if (string.IsNullOrEmpty(userSession))
        //    {
        //        context.Result = new RedirectToActionResult("Login", "Home", null); // Redirect to login if no session
        //    }
        //    base.OnActionExecuting(context);
        //}

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Check if the session contains the "User" key
            var userSession = context.HttpContext.Session.GetString("User");

            if (string.IsNullOrEmpty(userSession))
            {
                // If no session exists, redirect to the login page
                context.Result = new RedirectToActionResult("Login", "Home", null);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // This can be left empty for now as we don't need to do anything after the action executes.
        }
        public IActionResult Login()
        {
            return View();
        }



        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if (username == "admin" && password == "Admin@123")
            {
                HttpContext.Session.SetString("User", username); // Store session
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewBag.Error = "Invalid username or password.";
                return View();
            }
        }


        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Clear session
            return RedirectToAction("Login", "Home"); // Redirect to Login page after logout
        }



        public IActionResult Index()
        {
            var userSession = HttpContext.Session.GetString("User");
            if (string.IsNullOrEmpty(userSession))
            {
                return RedirectToAction("Login");
            }

            var dashData = new Dashdata
            {
                TotalBooking = _context.Bookings.Where(x => !x.IsDeleted)?.Count() ?? 0,
                TotalTrips = _context.Trips.Where(x => !x.IsDeleted)?.Count() ?? 0,
                TotalPackages = _context.Packages.Where(x => !x.IsDeleted)?.Count() ?? 0,
                TotalTransactions = _context.Transactions.Where(x => !x.IsDeleted)?.Count() ?? 0
            };
            var recentPackages = _context.Packages.Where(x => !x.IsDeleted)
                         .Include(p => p.City) // Ensure City is loaded
                         .Include(p => p.Category) // Ensure Category is loaded
                         .OrderByDescending(p => p.PackageId)  // Or use a Date field if available
                         .Take(3)
                         .ToList();


            var recentBookings = _context.Bookings.Where(x => !x.IsDeleted)
                          .OrderByDescending(b => b.BookingId)  // Or use a Date field if available
                          .Take(6)
                          .Include(b => b.User)  // Optional: to load related data like User
                          .Include(b => b.Trip)  // Optional: to load related data like Trip
                          .Include(b => b.PickupPoint)  // Optional: to load related data like PickupPoint
                          .ToList();


            var recentTrips = _context.Trips.Where(x => !x.IsDeleted)
      .OrderByDescending(t => t.TripId)
      .Take(6)
      .Include(t => t.Package) // Include the navigation property only
      .ToList();

            ViewBag.RecentTrips = recentTrips;


            ViewBag.RecentBookings = recentBookings;

            // Passing both dashboard data and recent packages to the view
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