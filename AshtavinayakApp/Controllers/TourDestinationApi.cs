using AshtavinayakAPP.Services.CategoryService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AshtavinayakAPP.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TourDestinationApi : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        public TourDestinationApi(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }
        [HttpGet("GetTourDestinationList")]
        public async Task<IActionResult> GetTourDestinationList()
        {
            var data=await _categoryService.GetAllTourDestinaionAsync();
            if (data != null) 
            {
                return Ok(data);
            }
            return Ok(null);
        }
    }
}
