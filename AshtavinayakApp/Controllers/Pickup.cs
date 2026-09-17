using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AshtavinayakAPP.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace AshtavinayakAPP.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PickupController : ControllerBase
    {
        private readonly AshtvinayakTravelContext _context;
        private readonly ILogger<PickupController> _logger;

        public PickupController(AshtvinayakTravelContext context, ILogger<PickupController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/PickupPoint/{cityId}
        [HttpGet("city/{cityId}")]
        public async Task<ActionResult<object>> GetPickupsByCity(int cityId)
        {
            try
            {
                // Fetch the PickupPoints based on CityId
                var pickups = await _context.PickupPoints
                    .Include(p => p.City)                    // CRIT-11: eagerly load City to prevent NullReferenceException
                    .Where(x => !x.IsDeleted)
                    .Where(p => p.CityId == cityId)
                    .Select(p => new
                    {
                        PickupPointName = p.PickupPoint1,
                        CityName        = p.City.CityName,
                        PickupPointID   = p.PickupPointId,
                        p.Time,
                    })
                    .ToListAsync();

                // If no results are found, return a not found response
                if (!pickups.Any())
                {
                    return NotFound(new
                    {
                        Message = "No pickup points found for the given CityId.",
                        Data = pickups
                    });
                }

                // Return the response in the desired format
                return Ok(new
                {
                    Message = "Pickup points fetched successfully.",
                    Data = pickups
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPickupsByCity failed for CityId={CityId}", cityId);
                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred. Please try again.",
                    Data = new List<object>()
                });
            }
        }
        // GET: api/Pickups/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetPickup(int id)
        {
            try
            {
                var pickup = await _context.PickupPoints.Where(x => !x.IsDeleted)
                    .Where(p => p.PickupPointId == id)
                    .Select(p => new
                    {
                        PickupPointId = p.PickupPointId,
                        PickupPointName = p.PickupPoint1,
                        CityName=p.City.CityName,
                        p.Time
          
                    })
                    .FirstOrDefaultAsync();

                if (pickup == null)
                {
                    return NotFound("Pickup point not found.");
                }

                return Ok(new
                {
                    Message = "Pickup point fetched successfully.",
                    Data = pickup
                });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "GetPickup failed for PickupPointId={PickupPointId}", id);
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }

        // POST: api/Pickups
        [Authorize(Roles = "Admin")] // MED-02
        [HttpPost]
        public async Task<ActionResult<object>> PostPickup([FromBody] PickupPoint pickupPoint)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _context.PickupPoints.Add(pickupPoint);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Pickup point created successfully.",
                    Data = pickupPoint
                });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "PostPickup failed");
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }

        // PUT: api/Pickups/{id}
        [Authorize(Roles = "Admin")] // MED-02
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPickup(int id, [FromBody] PickupPoint pickupPoint)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (id != pickupPoint.PickupPointId)
                {
                    return BadRequest("Pickup Point ID mismatch.");
                }

                var existingPickup = await _context.PickupPoints.FindAsync(id);
                if (existingPickup == null)
                {
                    return NotFound("Pickup point not found.");
                }

                existingPickup.PickupPoint1 = pickupPoint.PickupPoint1;
                existingPickup.PickupPointId = pickupPoint.PickupPointId;
                existingPickup.CityId = pickupPoint.CityId;

                _context.Entry(existingPickup).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Pickup point updated successfully.",
                    Data = existingPickup
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PickupExists(id))
                {
                    return NotFound("Pickup point not found.");
                }
                else
                {
                    throw;
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "PutPickup failed for PickupPointId={PickupPointId}", id);
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }

        // DELETE: api/Pickups/{id}
        [Authorize(Roles = "Admin")] // MED-02
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePickup(int id)
        {
            try
            {
                var pickup = await _context.PickupPoints.FindAsync(id);
                if (pickup == null)
                {
                    return NotFound("Pickup point not found.");
                }
                pickup.IsDeleted = true;
                _context.PickupPoints.Update(pickup);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Pickup point deleted successfully.",
                    DeletedPickupPointId = id
                });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "DeletePickup failed for PickupPointId={PickupPointId}", id);
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }

        private bool PickupExists(int id)
        {
            return _context.PickupPoints.Any(e => e.PickupPointId == id && !e.IsDeleted);
        }

        // ─── Package-based Pickup Endpoints ───────────────────────────────────────

        // GET: api/Pickup/package/{packageId}
        // Returns all pickup points for a specific package (with time).
        [HttpGet("package/{packageId}")]
        public async Task<ActionResult<object>> GetPickupsByPackage(int packageId)
        {
            try
            {
                var pickups = await _context.PickupPoints
                    .Where(p => p.PackageId == packageId && !p.IsDeleted)
                    .Select(p => new
                    {
                        PickupPointId   = p.PickupPointId,
                        PickupPointName = p.PickupPoint1,
                        Time            = p.Time,
                        CityId          = p.CityId,
                        PackageId       = p.PackageId,
                    })
                    .OrderBy(p => p.Time)
                    .ToListAsync();

                return Ok(new { Message = "Pickup points fetched.", Data = pickups });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPickupsByPackage failed for PackageId={PackageId}", packageId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // POST: api/Pickup/BulkCreate
        // Create multiple pickup points for a package in one call.
        // Body: { "cityId": 1, "packageId": 9, "pickupPoints": [{ "pickupPointName": "Pune", "time": "05:30" }] }
        [Authorize(Roles = "Admin")]
        [HttpPost("BulkCreate")]
        public async Task<ActionResult<object>> BulkCreatePickups([FromBody] BulkPickupRequest request)
        {
            try
            {
                if (request?.PickupPoints == null || !request.PickupPoints.Any())
                    return BadRequest("At least one pickup point is required.");

                var entities = request.PickupPoints.Select(p => new PickupPoint
                {
                    CityId       = request.CityId,
                    PackageId    = request.PackageId,
                    PickupPoint1 = p.PickupPointName,
                    Time         = p.Time.HasValue ? TimeOnly.FromTimeSpan(p.Time.Value) : null,
                    IsDeleted    = false,
                }).ToList();

                _context.PickupPoints.AddRange(entities);
                await _context.SaveChangesAsync();

                return Ok(new { Message = $"{entities.Count} pickup point(s) created.", Data = entities.Select(e => e.PickupPointId) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BulkCreatePickups failed");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // ─── Drop Point Endpoints ──────────────────────────────────────────────────

        // GET: api/Pickup/DropPoints/package/{packageId}
        [HttpGet("DropPoints/package/{packageId}")]
        public async Task<ActionResult<object>> GetDropPointsByPackage(int packageId)
        {
            try
            {
                var drops = await _context.DropUps
                    .Where(d => d.PackageId == packageId && !d.IsDeleted)
                    .Select(d => new
                    {
                        DroppointId = d.DroppointId,
                        DropPoint   = d.DropPoint,
                        Time        = d.Time,
                        CityId      = d.CityId,
                        PackageId   = d.PackageId,
                    })
                    .OrderBy(d => d.Time)
                    .ToListAsync();

                return Ok(new { Message = "Drop points fetched.", Data = drops });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetDropPointsByPackage failed for PackageId={PackageId}", packageId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // POST: api/Pickup/DropPoints/BulkCreate
        // Body: { "cityId": 1, "packageId": 9, "dropPoints": [{ "dropPointName": "Pune", "time": "21:00" }] }
        [Authorize(Roles = "Admin")]
        [HttpPost("DropPoints/BulkCreate")]
        public async Task<ActionResult<object>> BulkCreateDropPoints([FromBody] BulkDropRequest request)
        {
            try
            {
                if (request?.DropPoints == null || !request.DropPoints.Any())
                    return BadRequest("At least one drop point is required.");

                var entities = request.DropPoints.Select(d => new DropUp
                {
                    CityId    = request.CityId,
                    PackageId = request.PackageId,
                    DropPoint = d.DropPointName,
                    Time      = d.Time.HasValue ? TimeOnly.FromTimeSpan(d.Time.Value) : null,
                    IsDeleted = false,
                }).ToList();

                _context.DropUps.AddRange(entities);
                await _context.SaveChangesAsync();

                return Ok(new { Message = $"{entities.Count} drop point(s) created.", Data = entities.Select(e => e.DroppointId) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BulkCreateDropPoints failed");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}

// ─── Request DTOs for bulk pickup/drop creation ────────────────────────────────
public class BulkPickupRequest
{
    public int CityId { get; set; }
    public int PackageId { get; set; }
    public List<PickupPointEntry> PickupPoints { get; set; } = new();
}

public class PickupPointEntry
{
    public string PickupPointName { get; set; } = string.Empty;
    public TimeSpan? Time { get; set; }
}

public class BulkDropRequest
{
    public int CityId { get; set; }
    public int PackageId { get; set; }
    public List<DropPointEntry> DropPoints { get; set; } = new();
}

public class DropPointEntry
{
    public string DropPointName { get; set; } = string.Empty;
    public TimeSpan? Time { get; set; }
}
