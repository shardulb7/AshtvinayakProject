using AshtavinayakAPP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace AshtvinayakAPP.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly AshtvinayakTravelContext _context;

        public NotificationController(AshtvinayakTravelContext context)
        {
            _context = context;
        }

        [HttpPost("SendNotificationsForTrip/{tripId}")]
        public async Task<ActionResult<object>> SendNotificationsForTrip(int tripId)
        {
            try
            {
                // Step 1: Find all users who booked this trip
                var bookings = await _context.Bookings.Where(x => !x.IsDeleted)
                    .Where(b => b.TripId == tripId)
                    .Include(b => b.User) // Fetch user details
                    .Include(b => b.Trip) // Fetch trip details
                    .ToListAsync();

                if (!bookings.Any())
                {
                    return NotFound(new { Message = "No users have booked this trip." });
                }

                // Step 2: Create notifications for each user
                var notifications = new List<Notification>();

                foreach (var booking in bookings)
                {
                    var notification = new Notification
                    {
                        TripId = tripId,
                        UserId = booking.User.UserId,
                        NotificationMessage = $"Hello {booking.User.UserName}, your trip '{booking.Trip.TourName}' is confirmed!",
                        NotificationDate = DateTime.UtcNow
                    };

                    notifications.Add(notification);
                }

                // Step 3: Save all notifications in the database
                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync();

                // Step 4: Return success response with sent notifications
                var response = notifications.Select(n => new
                {
                    NotificationId = n.NotificationId,
                    TripId = n.TripId,
                    NotificationMessage = n.NotificationMessage,
                    NotificationDate = n.NotificationDate,
                    UserId = n.UserId
                }).ToList();

                return Ok(new
                {
                    Message = "Notifications sent successfully.",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal server error.", Error = ex.Message });
            }
        }



        [HttpGet("GetUserNotifications/{userId}")]
        public async Task<ActionResult<object>> GetUserNotifications(int userId)
        {
            try
            {
                // Step 1: Fetch all trips booked by this UserId
                var trips = await _context.Bookings.Where(x => !x.IsDeleted)
                    .Where(b => b.UserId == userId)
                    .Select(b => b.TripId)
                    .Distinct()
                    .ToListAsync();

                // Step 2: If no trips exist for this user, return a message
                if (!trips.Any())
                {
                    return NotFound(new { Message = "No notifications for this user." });
                }

                // Step 3: Fetch all notifications related to these trips
                var notifications = await _context.Notifications.Where(x => !x.IsDeleted)
                    .Where(n => n.UserId == userId || trips.Contains(n.TripId)) // Fetch by UserId or related TripId
                    .Include(n => n.Vehicle) // Include vehicle info if available
                    .Select(n => new
                    {
                        n.NotificationId,
                        n.NotificationMessage,
                        n.NotificationDate,
                        Vehicle = n.VehicleId != null ? new
                        {
                            n.Vehicle.VehicleId,
                            n.Vehicle.VehicleName,
                            n.Vehicle.VehicleNumber,
                            n.Vehicle.VehicleType,
                            n.Vehicle.DriverName,
                            n.Vehicle.DriverContact
                        } : null
                    })
                    .ToListAsync();

                // Step 4: If no notifications exist, return a message
                if (!notifications.Any())
                {
                    return NotFound(new { Message = "No notifications for this user." });
                }

                return Ok(new
                {
                    Message = "User notifications fetched successfully.",
                    UserId = userId,
                    Notifications = notifications
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal server error.", Error = ex.Message });
            }
        }


    }
}