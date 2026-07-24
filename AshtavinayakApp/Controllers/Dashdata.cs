using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using AshtavinayakAPP.Models;

namespace AshtavinayakAPP.Controllers
{
    [Authorize(Roles = "Admin")]                  // CRIT-15: protect all dashboard endpoints
    [Route("api/[controller]")]
    [ApiController]
    public class DashdataController : ControllerBase
    {
        private readonly AshtvinayakTravelContext _context;
        private readonly ILogger<DashdataController> _logger;

        public DashdataController(AshtvinayakTravelContext context, ILogger<DashdataController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Dashboard
        [HttpGet]
        public async Task<ActionResult<object>> GetDashboardData()
        {
            var dashData = new
            {
                TotalBooking = await _context.Bookings.Where(x => !x.IsDeleted).CountAsync(),
                TotalTrips = await _context.Trips.Where(x => !x.IsDeleted).CountAsync(),
                TotalPackages = await _context.Packages.Where(x => !x.IsDeleted).CountAsync(),
                TotalTransactions = await _context.Transactions.Where(x => !x.IsDeleted).CountAsync()
            };

            return Ok(dashData);
        }

        // GET: api/Dashboard/RecentPackages
        [HttpGet("RecentPackages")]
        public async Task<ActionResult<IEnumerable<object>>> GetRecentPackages()
        {
            try
            {
                // Get the 3 most recent packages ordered by PackageId or another relevant field
                var recentPackages = await _context.Packages.Where(x => !x.IsDeleted)
                    .OrderByDescending(p => p.PackageId)  // Order by PackageId or another field like Date
                    .Take(3)  // Limit to the 3 most recent packages
                    .Include(p => p.City)  // Load related data like City
                    .Include(p => p.Category)  // Load related data like Category
                    .Select(p => new  // Project to a custom object
                    {
                        PackageId = p.PackageId,
                        PackageName = p.PackageName,  // Assuming Package has a Name property
                        CityName = p.City.CityName,  // Assuming City has a Name property
                        CategoryName = p.Category.CategoryName,  // Assuming Category has a Name property
                    })
                    .ToListAsync();  // Asynchronous call to fetch the data

                // Return the custom projections
                return Ok(recentPackages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetRecentPackages failed");
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }

        // GET: api/Dashboard/RecentBookings
        [HttpGet("RecentBookings")]
        public async Task<ActionResult<IEnumerable<object>>> GetRecentBookings()
        {
            try
            {
                // Get the 6 most recent bookings ordered by BookingDate
                var recentBookings = await _context.Bookings.Where(x => !x.IsDeleted)
                    .OrderByDescending(b => b.BookingDate)  // Order by BookingDate or another relevant date field
                    .Take(6)  // Limit to the 6 most recent bookings
                    .Include(b => b.User)  // Load related data like User (if you have a User entity)
                    .Include(b => b.Trip)  // Load related data like Trip (if you have a Trip entity)
                    .Include(b => b.PickupPoint)  // Load related data like PickupPoint (if you have a PickupPoint entity)
                    .Select(b => new  // Project to a custom object
                    {
                        BookingId = b.BookingId,
                        BookingDate = b.BookingDate,
                        UserName  = b.User != null ? b.User.UserName : null,    // CS8602 guard
                        Tridate   = b.Trip != null ? (DateTime?)b.Trip.TripDate : null,    // CS8602 guard
                        Status    = b.Status

                    })
                    .ToListAsync();  // Asynchronous call to fetch the data

                // Return the custom projections
                return Ok(recentBookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetRecentBookings failed");
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }



        // GET: api/Dashboard/RecentTrips
        [HttpGet("RecentTrips")]
        public async Task<ActionResult<IEnumerable<object>>> GetRecentTrips()
        {
            try
            {
                // Get the 6 most recent trips ordered by TripId
                var recentTrips = await _context.Trips.Where(x => !x.IsDeleted)
                    .OrderByDescending(t => t.TripId)  // Order by TripId or another relevant field like TripDate
                    .Take(6)  // Limit to the 6 most recent trips
                    .Include(t => t.Package)  // Load related data like Package (if you have a Package entity)
                    .Select(t => new  // Project to a custom object
                    {
                        TripId = t.TripId,
                        Tripdate=t.TripDate,
                        TotalSeats = t.TotalSeats,  // Assuming Trip has a Name property
                        PackageName = t.Package != null ? t.Package.PackageName : null, // CS8602 guard
                        AvalaibleSeats = t.AvailableSeats
                        // Assuming Package has a Name property
                    })
                    .ToListAsync();  // Asynchronous call to fetch the data

                // Return the custom projections
                return Ok(recentTrips);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetRecentTrips failed");
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }


    }
}

