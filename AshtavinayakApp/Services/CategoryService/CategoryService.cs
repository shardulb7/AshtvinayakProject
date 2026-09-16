using AshtavinayakAPP.Models;
using Microsoft.EntityFrameworkCore;

namespace AshtavinayakAPP.Services.CategoryService
{
    public class CategoryService : ICategoryService
    {
        private readonly AshtvinayakTravelContext _context;

        // Constructor to inject the DbContext
        public CategoryService(AshtvinayakTravelContext context)
        {
            _context = context;
        }

        public async Task<List<TourDestination>> GetAllTourDestinaionAsync()
        {
            return await _context.TourDestinations.Where(x=>!x.IsDeleted).ToListAsync();
        }

        // Method to get the list of categories
        public async Task<List<Category>> GetCategoriesList(int cityid, long tourDestinationId)
        {
            return await _context.Categories
                .Where(x => x.CityId == cityid
                         && x.TourDestinationId == tourDestinationId
                         && !x.IsDeleted
                         && !x.IsCar)   // exclude car categories — car booking uses a separate flow
                .ToListAsync();
        }

    }
}
