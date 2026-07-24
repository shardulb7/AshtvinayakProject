using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AshtavinayakAPP.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace AshtavinayakAPP.Controllers
{
    [Authorize]               // GET endpoints open to all authenticated users
    [Route("api/[controller]")]
    [ApiController]
    public class SeatController : ControllerBase
    {
        private readonly AshtvinayakTravelContext _context;
        private readonly ILogger<SeatController> _logger;

        public SeatController(AshtvinayakTravelContext context, ILogger<SeatController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Seats
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetSeats()
        {
            try
            {
                var seats = await _context.Seats.Where(x => !x.IsDeleted)
                    .Include(s => s.Package)
                    .Select(s => new
                    {
                        SeatId = s.SeatId,
                        PackageName = s.Package.PackageName,
                        SeatNumber = s.SeatNumber,
                        IsAvailable = s.IsAvailable
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Message = "Seats fetched successfully.",
                    Data = seats
                });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "GetSeats failed");
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }

        // GET: api/Seats/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetSeat(int id)
        {
            try
            {
                var seat = await _context.Seats.Where(x => !x.IsDeleted)
                    .Include(s => s.Package)
                    .Where(s => s.SeatId == id)
                    .Select(s => new
                    {
                        SeatId = s.SeatId,
                        PackageName = s.Package.PackageName,
                        SeatNumber = s.SeatNumber,
                        IsAvailable = s.IsAvailable
                    })
                    .FirstOrDefaultAsync();

                if (seat == null)
                {
                    return NotFound("Seat not found.");
                }

                return Ok(new
                {
                    Message = "Seat fetched successfully.",
                    Data = seat
                });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "GetSeat failed for SeatId={SeatId}", id);
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }

        // POST: api/Seats
        [Authorize(Roles = "Admin")] // MED-02
        [HttpPost]
        public async Task<ActionResult<object>> PostSeat([FromBody] Seat seat)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var packageExists = await _context.Packages.AnyAsync(p => p.PackageId == seat.PackageId);
                if (!packageExists)
                {
                    return BadRequest("Invalid PackageId.");
                }

                _context.Seats.Add(seat);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Seat created successfully.",
                });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "PostSeat failed for PackageId={PackageId}", seat.PackageId);
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }

        // PUT: api/Seats/{id}
        [Authorize(Roles = "Admin")] // MED-02
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSeat(int id, [FromBody] Seat seat)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (id != seat.SeatId)
                {
                    return BadRequest("Seat ID mismatch.");
                }

                var existingSeat = await _context.Seats.FindAsync(id);
                if (existingSeat == null)
                {
                    return NotFound("Seat not found.");
                }

                existingSeat.PackageId = seat.PackageId;
                existingSeat.SeatNumber = seat.SeatNumber;
                existingSeat.IsAvailable = seat.IsAvailable;

                _context.Entry(existingSeat).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Seat updated successfully.",
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SeatExists(id))
                {
                    return NotFound("Seat not found.");
                }
                else
                {
                    throw;
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "PutSeat failed for SeatId={SeatId}", id);
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }

        // DELETE: api/Seats/{id}
        [Authorize(Roles = "Admin")] // MED-02
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSeat(int id)
        {
            try
            {
                var seat = await _context.Seats.FindAsync(id);
                if (seat == null)
                {
                    return NotFound("Seat not found.");
                }
                seat.IsDeleted = true;
                _context.Seats.Update(seat);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Seat deleted successfully.",
                    DeletedSeatId = id
                });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "DeleteSeat failed for SeatId={SeatId}", id);
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }

        private bool SeatExists(int id)
        {
            return _context.Seats.Any(s => s.SeatId == id && !s.IsDeleted);
        }
    }
}

