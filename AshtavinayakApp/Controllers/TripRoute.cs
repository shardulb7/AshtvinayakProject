using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AshtavinayakAPP.Models;
using Microsoft.AspNetCore.Authorization;

namespace AshtavinayakAPP.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TripRouteController : ControllerBase
    {
        private readonly AshtvinayakTravelContext _context;

        public TripRouteController(AshtvinayakTravelContext context)
        {
            _context = context;
        }

        // GET: api/TripRoutes
        [HttpGet("{cityId}/{packageId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetTripRoutes(int cityId, int packageId)
        {
            try
            {
                var tripRoutes = await _context.TripRoutes.Where(x => !x.IsDeleted)
                    .Include(t => t.Package)
                    .Include(t => t.City)
                    .Where(t => t.CityId == cityId && t.PackageId == packageId)
                    .Select(t => new
                    {
                        PointName = t.PointName,
                        Day = t.Day
                    })
                    .ToListAsync();

                if (!tripRoutes.Any())
                {
                    return NotFound("No trip routes found for the specified CityId and PackageId.");
                }

                return Ok(new
                {
                    Message = "Trip routes fetched successfully.",
                    Data = tripRoutes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }



        private bool TripRouteExists(int id)
        {
            return _context.TripRoutes.Any(e => e.Trid == id);
        }
    }
}