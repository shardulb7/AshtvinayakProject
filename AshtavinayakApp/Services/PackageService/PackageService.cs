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
                // When isCarType=true, find packages whose Category has IsCar=true.
                // This is resilient to DB ID mismatches — the client may pass any ID
                // (e.g. 2) but the actual car category ID in DB may differ (e.g. 7).
                return await _context.Packages
                    .Include(p => p.Category)
                    .Where(p => p.Category != null
                             && p.Category.IsCar
                             && !p.Category.IsDeleted
                             && !p.IsDeleted)
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
