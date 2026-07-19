using AshtavinayakAPP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class HistoryController : ControllerBase
{
    private readonly AshtvinayakTravelContext _context;

    public HistoryController(AshtvinayakTravelContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetHistories()
    {
        try
        {
            var histories = await _context.Histories.Where(x => !x.IsDeleted)
                .Include(h => h.Booking)
                    .ThenInclude(b => b.User) // Include User to get UserName
                .Include(h => h.Booking)
                    .ThenInclude(b => b.Trip) // Include Trip to get TourName
                .Include(h => h.Booking)
                    .ThenInclude(b => b.Droppoint) // Include DropPoint to get DropPointName
                .Include(h => h.Category)
                .Include(h => h.City)
                .Include(h => h.Package)
                .Select(h => new
                {
                    HistoryId = h.HistoryId,
                    BookingId = h.Booking.BookingId,
                    UserName = h.Booking.User.UserName, // Fetch UserName based on UserId
                    TourName = h.Booking.Trip.TourName, // Fetch TourName based on TripId
                    /*DropPoint = h.Booking.Droppoint.DropPoint,*/ // Fetch DropPoint based on DropPointId
                    CategoryName = h.Category.CategoryName,
                    CityName = h.City.CityName,
                    PackageName = h.Package.PackageName,
                    Total = h.Booking.TotalPayment,
                    Advance = h.Booking.Advance,
                    Status = h.Booking.Status,

                    // Fetch booked seat numbers for the specific BookingId
                    BookedSeatNumbers = _context.BookingSeats
                        .Where(bs => bs.BookingId == h.Booking.BookingId)
                        .Select(bs => bs.SeatNumber) // Get seat numbers for that booking
                        .ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                Message = "Histories fetched successfully.",
                Data = histories
            });
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, "Internal server error: " + ex.Message);
        }
    }


    // GET: api/Histories/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetHistory(int id)
    {
        try
        {
            var history = await _context.Histories.Where(x => !x.IsDeleted)
                .Include(h => h.Booking)
                .Include(h => h.Category)
                .Include(h => h.City)
                .Include(h => h.Package)
                .Where(h => h.HistoryId == id)
                .Select(h => new
                {
                    HistoryId = h.HistoryId,
                    BookingId = h.Booking.BookingId,
                    CategoryName = h.Category.CategoryName,
                    CityName = h.City.CityName,
                    PackageName = h.Package.PackageName
                })
                .FirstOrDefaultAsync();

            if (history == null)
            {
                return NotFound("History not found.");
            }

            return Ok(new
            {
                Message = "History fetched successfully.",
                Data = history
            });
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, "Internal server error: " + ex.Message);
        }
    }

    // POST: api/Histories
    [HttpPost]
    public async Task<ActionResult<object>> PostHistory([FromBody] History history)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Histories.Add(history);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "History created successfully.",
                Data = history
            });
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, "Internal server error: " + ex.Message);
        }
    }

    // PUT: api/Histories/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> PutHistory(int id, [FromBody] History history)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != history.HistoryId)
            {
                return BadRequest("History ID mismatch.");
            }

            var existingHistory = await _context.Histories.FindAsync(id);
            if (existingHistory == null)
            {
                return NotFound("History not found.");
            }

            existingHistory.BookingId = history.BookingId;
            existingHistory.CategoryId = history.CategoryId;
            existingHistory.CityId = history.CityId;
            existingHistory.PackageId = history.PackageId;

            _context.Entry(existingHistory).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "History updated successfully.",
                Data = existingHistory
            });
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, "Internal server error: " + ex.Message);
        }
    }

    // DELETE: api/Histories/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHistory(int id)
    {
        try
        {
            var history = await _context.Histories.FindAsync(id);
            if (history == null)
            {
                return NotFound("History not found.");
            }
            history.IsDeleted = true;
            _context.Histories.Remove(history);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "History deleted successfully.",
                DeletedHistoryId = id
            });
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, "Internal server error: " + ex.Message);
        }
    }

    private bool HistoryExists(int id)
    {
        return _context.Histories.Any(e => e.HistoryId == id);
    }
}
