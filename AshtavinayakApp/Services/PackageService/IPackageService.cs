using AshtavinayakAPP.Models;

namespace AshtavinayakAPP.Services.PackageService  // LOW-01: fixed typo (was PakageService)
{
    public interface IPackageService
    {
        Task<List<Package>>GetpakageByCategoryId(int categoryId, bool isCartype, int? destinationId = null);
        Task<List<DropUp>> GetDropPoitByCityId(int cityId);
    }
}
