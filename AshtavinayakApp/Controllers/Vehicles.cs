using AshtavinayakAPP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AshtavinayakAPP.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleController : ControllerBase
    {
        private readonly AshtvinayakTravelContext _context;
        private readonly ILogger<VehicleController> _logger;

        public VehicleController(AshtvinayakTravelContext context, ILogger<VehicleController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/vehicles
        [HttpGet]
        public async Task<IActionResult> GetAllVehicles()
        {
            try
            {
                // Fetch all vehicles from the Vehicles table
                var vehicles = await _context.Vehicles.Where(x => !x.IsDeleted)
                    .Select(v => new
                    {
                        VehicleId = v.VehicleId,
                        VehicleType = v.VehicleType,
                        VehicleName = v.VehicleName,
                        VehicleNumber = v.VehicleNumber,
                        TotalSeats = v.TotalSeats,
                        DriverName = v.DriverName,
                        DriverContact = v.DriverContact,
                        TripId = v.TripId,

                    })
                    .ToListAsync();

                // Check if no vehicles are found
                if (!vehicles.Any())
                {
                    return NotFound(new
                    {
                        Message = "No vehicles found."
                    });
                }

                return Ok(new
                {
                    Message = "Vehicles fetched successfully.",
                    Data = vehicles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllVehicles failed");
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }

        // GET: api/vehicles/{vehicleId}
        [HttpGet("{vehicleId}")]
        public async Task<IActionResult> GetVehicleById(int vehicleId)
        {
            try
            {
                // Fetch vehicle by VehicleId
                var vehicle = await _context.Vehicles.Where(x => !x.IsDeleted)
                    .Where(v => v.VehicleId == vehicleId)
                    .Select(v => new
                    {
                        VehicleId = v.VehicleId,
                        VehicleType = v.VehicleType,
                        VehicleName = v.VehicleName,
                        VehicleNumber = v.VehicleNumber,
                        TotalSeats = v.TotalSeats,
                        DriverName = v.DriverName,
                        DriverContact = v.DriverContact,
                        TripId = v.TripId,

                    })
                    .FirstOrDefaultAsync();

                // Check if the vehicle is found
                if (vehicle == null)
                {
                    return NotFound(new
                    {
                        Message = $"Vehicle with ID {vehicleId} not found."
                    });
                }

                return Ok(new
                {
                    Message = "Vehicle fetched successfully.",
                    Data = vehicle
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetVehicleById failed for VehicleId={VehicleId}", vehicleId);
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }
    }
}
