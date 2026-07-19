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

        public BookingSeatController(AshtvinayakTravelContext context)
        {
            _context = context;
            ;
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
                return StatusCode(500, new
                {
                    Message = "Internal server error",
                    Error = ex.Message
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



        [HttpPost("BookSeats")]
        public async Task<ActionResult> BookSeats([FromBody] BookingRequestDto request)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // *Step 1: Validate Selected Seats*
                    var seats = await _context.Seats
                        .Where(s => request.SeatNumbers.Contains(s.SeatNumber) && s.TripId == request.TripId && s.IsAvailable)
                        .ToListAsync();

                    if (seats.Count != request.SeatNumbers.Count)
                    {
                        return BadRequest("Some seats are already booked or unavailable.");
                    }

                    // *Step 2: Create Booking Record*
                    var booking = new Booking
                    {
                        UserId = request.UserId,
                        TripId = request.TripId,
                        PickupPointId = request.PickupPointId,
                        BookingDate = DateTime.UtcNow,
                        Status = "Confirmed",
                        TotalPayment = request.TotalPayment,
                        Advance = request.Advance,
                        BookingCode = Guid.NewGuid().ToString()
                    };

                    _context.Bookings.Add(booking);
                    await _context.SaveChangesAsync(); // ✅ Now we have booking.BookingId

                    // *Step 3: Save BookingSeats*
                    var bookingSeats = request.SeatNumbers.Select(seatNumber => new BookingSeat
                    {
                        BookingId = booking.BookingId, // ✅ Assign the generated BookingId
                        SeatNumber = seatNumber,
                        Adults = request.Adults,
                        Childwithseat = request.Childwithseat,
                        Childwithoutseat = request.Childwithoutseat,
                        TripId = request.TripId,
                        UserId = request.UserId,
                    }).ToList();

                    _context.BookingSeats.AddRange(bookingSeats);
                    await _context.SaveChangesAsync();

                    // *Step 4: Mark Selected Seats as Unavailable*
                    foreach (var seat in seats)
                    {
                        seat.IsAvailable = false;
                    }
                    await _context.SaveChangesAsync(); // ✅ Update seat availability

                    // *Step 5: Commit Transaction*
                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        Message = "Booking successful!",
                        BookingId = booking.BookingId,
                        BookingSeats = bookingSeats
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
                }
            }
        }

    }
}