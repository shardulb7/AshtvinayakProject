using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AshtavinayakAPP.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace AshtavinayakAPP.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TripController : ControllerBase
    {
        private readonly AshtvinayakTravelContext _context;
        private readonly ILogger<TripController> _logger;

        public TripController(AshtvinayakTravelContext context, ILogger<TripController> logger)
        {
            _context = context;
            _logger = logger;
        }

        
        [HttpGet("TripsByPackage/{packageId}")]
        public async Task<IActionResult> GetTripsByPackage(int packageId)
        {
            try
            {
                // Fetch all trips assigned to the given PackageId
                var trips = await _context.Trips.Where(x => !x.IsDeleted)
                    .Where(t => t.PackageId == packageId) // Filter trips by PackageId
                    .Select(t => new
                    {
                        TripId = t.TripId,
                        TripDate = t.TripDate,
                        TotalSeats = t.TotalSeats,
                        AvailableSeats = t.AvailableSeats,
                        TourName = t.TourName
                    })
                    .ToListAsync();

                if (!trips.Any())
                {
                    return NotFound(new
                    {
                        Message = $"No trips found for Package ID: {packageId}."
                    });
                }

                return Ok(new
                {
                    Message = "Trips fetched successfully.",
                    Data = trips
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetTripsByPackage failed for PackageId={PackageId}", packageId);
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }

        [HttpGet("Available/{tripId}")]
        public async Task<IActionResult> GetAvailableSeats(int tripId)
        {
            try
            {
                // Update the filter to use TripId instead of PackageId
                var seats = await _context.Seats.Where(x => !x.IsDeleted)
                    .Where(s => s.TripId == tripId && s.IsAvailable)  // Use TripId here
                    .Select(s => new
                    {
                        SeatId = s.SeatId,
                        SeatNumber = s.SeatNumber,
                        IsAvailable = s.IsAvailable
                    })
                    .ToListAsync();

                if (!seats.Any())
                {
                    return NotFound(new
                    {
                        Message = $"No available seats found for TripId {tripId}."
                    });
                }

                return Ok(new
                {
                    Message = "Available seats fetched successfully.",
                    Data = seats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAvailableSeats failed for TripId={TripId}", tripId);
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }

        [HttpPut("UpdateAvailability")]
        public async Task<IActionResult> UpdateSeatAvailability([FromBody] Seat request)
        {
            try
            {
                // Find the seat by SeatNumber
                var seat = await _context.Seats
                    .FirstOrDefaultAsync(s => s.SeatNumber == request.SeatNumber
                                           && s.TripId == request.TripId); // HIGH-03: scope to correct trip

                // Check if the seat exists
                if (seat == null)
                {
                    return NotFound(new
                    {
                        Message = $"Seat {request.SeatNumber} not found."
                    });
                }

                // Update the seat availability
                seat.IsAvailable = request.IsAvailable;

                // Save the changes in the database
                _context.Seats.Update(seat);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = $"Seat {request.SeatNumber} availability updated to {request.IsAvailable}."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateSeatAvailability failed for SeatNumber={SeatNumber} TripId={TripId}", request.SeatNumber, request.TripId);
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }


        [HttpGet("BookingSeats/{tripId}")]
        public async Task<IActionResult> GetBookingSeatsByTripId(int tripId)
        {
            try
            {
                // Fetch booking seats for the specified TripId
                var bookingSeats = await _context.BookingSeats.Where(x => !x.IsDeleted)
                    .Where(bs => bs.TripId == tripId)
                    .Select(bs => new
                    {
                        BookingSeatId = bs.BookingSeatId,
                        BookingId = bs.BookingId,
                        SeatNumber = bs.SeatNumber,
                        Adults = bs.Adults,
                        ChildWithSeat = bs.Childwithseat,
                        ChildWithoutSeat = bs.Childwithoutseat,
                        TripId = bs.TripId
                    })
                    .ToListAsync();

                // Check if no seats are found for the trip
                if (!bookingSeats.Any())
                {
                    return NotFound(new
                    {
                        Message = $"No booking seats found for Trip ID {tripId}."
                    });
                }

                return Ok(new
                {
                    Message = $"Booking seats for Trip ID {tripId} fetched successfully.",
                    Data = bookingSeats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetBookingSeatsByTripId failed for TripId={TripId}", tripId);
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }

        // GET: api/TripApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetTrips()
        {
            try
            {
                var trips = await _context.Trips.Where(x => !x.IsDeleted)
                    .Include(t => t.Package) // Include the related Package data
                    .Select(t => new
                    {
                        TripId = t.TripId,

                        TripDate = t.TripDate,

                    })
                    .ToListAsync();

                return Ok(new
                {
                    Message = "Trips fetched successfully.",
                    Data = trips
                });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "GetTrips failed");
                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred. Please try again.",
                });
            }
        }

        private bool TripExists(int id)
        {
            return _context.Trips.Any(e => e.TripId == id && !e.IsDeleted);
        }
    }
}