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
        public async Task<List<Package>> GetpakageByCategoryId(int categoryId, bool isCartype)
        {
            if (isCartype)
            {
                // Filter by Category.IsCar via subquery — avoids loading the navigation
                // property which causes circular-reference JSON serialization failure.
                return await _context.Packages
                    .Where(p => !p.IsDeleted
                             && _context.Categories.Any(c => c.CategoryId == p.CategoryId
                                                          && c.IsCar
                                                          && !c.IsDeleted))
                    .ToListAsync();
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
