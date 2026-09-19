using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AshtavinayakAPP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AshtavinayakAPP.Services.PackageService; // LOW-01: fixed typo (was PakageService)
using Microsoft.AspNetCore.Authorization;

namespace AshtavinayakAPP.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PackageController : ControllerBase
    {
        private readonly AshtvinayakTravelContext _context;
        private readonly IPackageService _packageService;
        private readonly ILogger<PackageController> _logger;
        public PackageController(AshtvinayakTravelContext context, IPackageService packageService, ILogger<PackageController> logger)
        {
            _context = context;
            _packageService = packageService;
            _logger = logger;
        }
        [HttpGet("GetPackageByCateGoryId")]
        public async Task<IActionResult>GetPackageByCateGoryId(int id,bool isCarType=false)
        {
            var data=await _packageService.GetpakageByCategoryId(id,isCarType);
            return Ok(data);
        }

        [HttpGet("GetPackagePrice/{cityId}/{categoryId}/{packageId}")]
        public async Task<ActionResult<object>> GetPackagePrice(int cityId, int categoryId, int packageId)
        {
            try
            {
                if (cityId <= 0 || categoryId <= 0 || packageId <= 0)
                {
                    return BadRequest("CityId, CategoryId, and PackageId must be valid positive integers.");
                }

                var package = await _context.Packages.Where(x => !x.IsDeleted)
                    .Include(p => p.Category)
                    .Include(p => p.City)
                    .FirstOrDefaultAsync(p => p.PackageId == packageId &&
                                              p.CityId == cityId &&
                                              p.CategoryId == categoryId);

                if (package == null)
                {
                    return NotFound("No package found for the selected criteria.");
                }

                var routes = await _context.TripRoutes.Where(x => !x.IsDeleted)
                    .Where(tr => tr.PackageId == packageId)
                    .GroupBy(tr => tr.Day)
                    .Select(g => new
                    {
                        Day = g.Key,
                        Points = g.Select(tr => tr.PointName).ToArray()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    PackageId = package.PackageId,
                    PackageName = package.PackageName,
                    Duration = package.Duration,
                    AdultPrice = package.AdultPrice,
                    Child3To8YrsWithSeat = package.Child3To8YrswithSeat,
                    Child3To8YrsWithoutSeat = package.Child3To8YrsWithoutSeat,
                    FamilyRoomChargePerPerson = package.FamilyRoomChargePerPerson,
                    // Room-type sharing charges (per person, on top of AdultPrice)
                    SingleSharingChargePerPerson = package.SingleSharingChargePerPerson,
                    DoubleSharingChargePerPerson = package.DoubleSharingChargePerPerson,
                    TripleSharingChargePerPerson = package.TripleSharingChargePerPerson,
                    IsCar = package.IsCar,
                    CarPackagePrice = package.CarPackagePrice,
                    PkgPersonCount = package.PkgPersonCount,
                    Category = package.Category?.CategoryName,
                    City = package.City?.CityName,
                    Inclusions = package.Inclusions,
                    Exclusions = package.Exclusions,
                    Routes = routes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPackagePrice failed for CityId={CityId} CategoryId={CategoryId} PackageId={PackageId}", cityId, categoryId, packageId);
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }

        [HttpGet("GetPackages/{cityId}/{categoryId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetPackages(int cityId, int categoryId)
        {
            try
            {
                if (cityId <= 0 || categoryId <= 0)
                {
                    return BadRequest("CityId and CategoryId must be valid positive integers.");
                }

                var packages = await _context.Packages.Where(x => !x.IsDeleted)
                    .Where(p => p.CityId == cityId && p.CategoryId == categoryId)
                    .Include(p => p.Category)
                    .Include(p => p.City)
                    .Select(p => new
                    {
                        PackageId = p.PackageId,
                        PackageName = p.PackageName,
                        Duration = p.Duration,
                        CategoryName = p.Category.CategoryName,
                        CityName = p.City.CityName,
                        AdultPrice = p.AdultPrice,
                        Child3To8YrsWithSeat = p.Child3To8YrswithSeat,
                        Child3To8YrsWithoutSeat = p.Child3To8YrsWithoutSeat,
                        FamilyRoomChargePerPerson = p.FamilyRoomChargePerPerson,
                        IsCar = p.IsCar,
                        CarPackagePrice = p.CarPackagePrice,
                        PkgPersonCount = p.PkgPersonCount
                    })
                    .ToListAsync();

                if (!packages.Any())
                {
                    return NotFound($"No packages found for city ID: {cityId} and category ID: {categoryId}");
                }

                return Ok(packages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPackages failed for CityId={CityId} CategoryId={CategoryId}", cityId, categoryId);
                return StatusCode(500, "An unexpected error occurred. Please try again.");
            }
        }
        [HttpGet("GetDropPointByCityIdAsync/{cityId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetDropPointByCityIdAsync(int cityId)
        {
            var data= await _packageService.GetDropPoitByCityId(cityId);
            return data;
        }

        // GET: api/Package/SharingCharges/{packageId}
        // Returns the 3 sharing charge values + familyRoom for a specific package (diagnostic + FE use).
        [HttpGet("SharingCharges/{packageId}")]
        public async Task<IActionResult> GetSharingCharges(int packageId)
        {
            var pkg = await _context.Packages
                .Where(p => p.PackageId == packageId && !p.IsDeleted)
                .Select(p => new
                {
                    PackageId                   = p.PackageId,
                    PackageName                 = p.PackageName,
                    IsCar                       = p.IsCar,
                    FamilyRoomChargePerPerson   = p.FamilyRoomChargePerPerson,
                    SingleSharingChargePerPerson = p.SingleSharingChargePerPerson,
                    DoubleSharingChargePerPerson = p.DoubleSharingChargePerPerson,
                    TripleSharingChargePerPerson = p.TripleSharingChargePerPerson,
                })
                .FirstOrDefaultAsync();

            if (pkg == null) return NotFound(new { Message = $"Package {packageId} not found." });
            return Ok(pkg);
        }

        // PUT: api/Package/SharingCharges/{packageId}
        // Updates sharing charges directly via API (Admin role required).
        // Body: { "singleSharingChargePerPerson": 500, "doubleSharingChargePerPerson": 300, "tripleSharingChargePerPerson": 200 }
        [Authorize(Roles = "Admin")]
        [HttpPut("SharingCharges/{packageId}")]
        public async Task<IActionResult> UpdateSharingCharges(int packageId, [FromBody] SharingChargeUpdateDto dto)
        {
            var pkg = await _context.Packages.FirstOrDefaultAsync(p => p.PackageId == packageId && !p.IsDeleted);
            if (pkg == null) return NotFound(new { Message = $"Package {packageId} not found." });

            pkg.SingleSharingChargePerPerson = dto.SingleSharingChargePerPerson;
            pkg.DoubleSharingChargePerPerson = dto.DoubleSharingChargePerPerson;
            pkg.TripleSharingChargePerPerson = dto.TripleSharingChargePerPerson;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Sharing charges updated successfully.",
                PackageId = pkg.PackageId,
                PackageName = pkg.PackageName,
                pkg.SingleSharingChargePerPerson,
                pkg.DoubleSharingChargePerPerson,
                pkg.TripleSharingChargePerPerson,
            });
        }

        private bool PackageExists(int id)
        {
            return _context.Packages.Any(e => e.PackageId == id && !e.IsDeleted);
        }
    }
}

public class SharingChargeUpdateDto
{
    public int? SingleSharingChargePerPerson { get; set; }
    public int? DoubleSharingChargePerPerson { get; set; }
    public int? TripleSharingChargePerPerson { get; set; }
}
