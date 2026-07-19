using AshtavinayakAPP.Models;

namespace AshtavinayakAPP.Services.PakageService
{
    public interface IPackageService
    {
        Task<List<Package>>GetpakageByCategoryId(int  categoryId,bool isCartype);
        Task<List<DropUp>> GetDropPoitByCityId(int cityId);
    }
}
