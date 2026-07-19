using AshtavinayakAPP.Models;

namespace AshtavinayakAPP.Services.CategoryService
{
    /// <summary>
    /// Gets the list of categories for a specific city.
    /// </summary>
    /// <param name="cityid">ID of the city.</param>
    /// <param name="isTulsapur">Whether the request is for Tulsapur.</param>
    /// <returns>List of categories.</returns>
    public interface ICategoryService
    {
        Task<List<Category>> GetCategoriesList(int cityid, long tourDestinationId);
        Task<List<TourDestination>> GetAllTourDestinaionAsync();
    }
}
