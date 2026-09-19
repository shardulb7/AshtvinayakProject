using AshtavinayakAPP.Models;
using AshtavinayakAPP.Services.PackageService; // LOW-01: fixed typo (was PakageService)
using Microsoft.EntityFrameworkCore;

namespace AshtavinayakAPP.Services.PackageService
{
    public class PackageService : IPackageService
    {
        private readonly AshtvinayakTravelContext _context;

        // Constructor for dependency injection
        public PackageService(AshtvinayakTravelContext context)
        {
            _context = context;
        }

        public async Task<List<DropUp>> GetDropPoitByCityId(int cityId)
        {
            return await _context.DropUps.Where(x=>!x.IsDeleted && x.CityId==cityId).ToListAsync();
        }

        // Get list of packages by category ID
        public async Task<List<Package>> GetpakageByCategoryId(int categoryId, bool isCartype, int? destinationId = null)
        {
            if (isCartype)
            {
                // For car packages: always use Category.IsCar=true (categoryId=7 in DB).
                // Optionally filter by DestinationId if provided.
                var query = _context.Packages
                    .Where(p => !p.IsDeleted
                             && _context.Categories.Any(c => c.CategoryId == p.CategoryId
                                                          && c.IsCar
                                                          && !c.IsDeleted));

                if (destinationId.HasValue)
                    query = query.Where(p => p.DestinationId == destinationId.Value);

                return await query.ToListAsync();
            }
            else
            {
                return await _context.Packages
                    .Where(p => p.CategoryId == categoryId && !p.IsCar && !p.IsDeleted)
                    .ToListAsync();
            }
        }
    }
}
