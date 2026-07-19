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

        public PickupController(AshtvinayakTravelContext context)
        {
            _context = context;
        }

        // GET: api/PickupPoint/{cityId}
        [HttpGet("city/{cityId}")]
        public async Task<ActionResult<object>> GetPickupsByCity(int cityId)
        {
            try
            {
                // Fetch the PickupPoints based on CityId
                var pickups = await _context.PickupPoints.Where(x => !x.IsDeleted)
                    .Where(p => p.CityId == cityId)
                    .Select(p => new
                    {
                        PickupPointName = p.PickupPoint1, // Assuming PickupPoint1 is the name field
                        CityName = p.City.CityName,
                        PickupPointID=p.PickupPointId,
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
                // Return internal server error in case of an exception
                return StatusCode(500, new
                {
                    Message = "Internal server error: " + ex.Message,
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
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        // POST: api/Pickups
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
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        // PUT: api/Pickups/{id}
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
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        // DELETE: api/Pickups/{id}
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
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        private bool PickupExists(int id)
        {
            return _context.PickupPoints.Any(e => e.PickupPointId == id);
        }
    }
}
