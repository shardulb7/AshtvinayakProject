using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AshtavinayakAPP.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.AspNetCore.Authorization;

namespace AshtavinayakAPP.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BookingSeatController : ControllerBase
    {
        private readonly AshtvinayakTravelContext _context;
        private readonly ILogger<BookingSeatController> _logger;

        public BookingSeatController(AshtvinayakTravelContext context, ILogger<BookingSeatController> logger)
        {
            _context = context;
            _logger = logger;
        }


        //return booked seates 
        [HttpGet("ByTrip/{tripId}")]
        public async Task<ActionResult<object>> GetBookedSeatsByTrip(int tripId)
        {
            try
            {
                var bookedSeats = await _context.BookingSeats
                    .Where(b => b.TripId == tripId && !b.IsDeleted)
                    .Select(bs => bs.SeatNumber) // ✅ Only seat numbers
                    .ToListAsync();

                if (!bookedSeats.Any())
                {
                    return NotFound(new
                    {
                        Message = "No booked seats found for this trip.",
                        Data = new List<string>()
                    });
                }

                return Ok(new
                {
                    Message = "Booked seats fetched successfully.",
                    Data = bookedSeats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetBookedSeatsByTrip failed for TripId={TripId}", tripId);
                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred. Please try again."
                });
            }
        }

        // ✅ 1️⃣ Get Available Seats for a Trip
        [HttpGet("GetAvailableSeats/{tripId}")]
        public async Task<ActionResult<IEnumerable<int>>> GetAvailableSeats(int tripId)
        {
            var availableSeats = await _context.Seats
                .Where(s => s.TripId == tripId && s.IsAvailable && !s.IsDeleted)
                .Select(s => s.SeatNumber)
                .ToListAsync();

            return Ok(availableSeats);
        }



        /// <summary>
        /// ⚠️ DEPRECATED (HIGH-12) — Use POST /api/Booking/CreateBookingWithSeats instead.
        /// This endpoint has been superseded by the canonical booking flow which includes:
        /// SMS confirmation, Transaction record, BookingCode, and seat availability update.
        /// This endpoint will be removed in a future release.
        /// </summary>
        [HttpPost("BookSeats")]
        [Obsolete("Use POST /api/Booking/CreateBookingWithSeats. This endpoint will be removed.")]
        public IActionResult BookSeats([FromBody] BookingRequestDto request)
        {
            return StatusCode(410, new
            {
                Message      = "This endpoint is deprecated. Please migrate to POST /api/Booking/CreateBookingWithSeats.",
                DeprecatedAt = "2026-07-23",
                NewEndpoint  = "POST /api/Booking/CreateBookingWithSeats"
            });
        }

    }
}