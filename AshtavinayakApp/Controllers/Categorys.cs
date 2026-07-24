using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AshtavinayakAPP.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AshtavinayakAPP.Services.CategoryService;
using Microsoft.AspNetCore.Authorization;

namespace AshtavinayakAPP.Controllers
{
    // MED-13: was class-level Admin-only, which blocked GetCategories/GetCategory — pure catalog
    // reads with no sensitive data, needed by the mobile app's browsing flow. Matches the
    // GET-open/mutations-Admin-gated pattern already used in Seats.cs and Pickup.cs.
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CategorysController : ControllerBase
    {
        private readonly AshtvinayakTravelContext _context;
        private readonly ICategoryService _categoryService;

        public CategorysController(AshtvinayakTravelContext context,ICategoryService categoryService)
        {
            _context = context;
            _categoryService = categoryService;
        }

        // GET: api/Categories
        [HttpGet("GetCategories")]
        public async Task<ActionResult<List<Category>>> GetCategories(int cityid, long tourDestinationId)
        {
            var categories = await _categoryService.GetCategoriesList(cityid, tourDestinationId);
            return Ok(categories);
        }


        // GET: api/Categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound(new { Message = "Category not found." });
            }

            return Ok(new { Message = "Category retrieved successfully.", Data = category });
        }

        // POST: api/Categories
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<Category>> PostCategory(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId },

                    new { Message = "Category created successfully.", Data = category });
            }

            return BadRequest(new { Message = "Invalid data.", Errors = ModelState.Values.SelectMany(v => v.Errors) });
        }

        // PUT: api/Categories/5
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCategory(int id, Category category)
        {
            if (id != category.CategoryId)
            {
                return BadRequest(new { Message = "Category ID mismatch." });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Entry(category).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(id))
                    {
                        return NotFound(new { Message = "Category not found." });
                    }
                    else
                    {
                        throw;
                    }
                }

                return Ok(new { Message = "Category updated successfully." ,Data=category});
            }

            return BadRequest(new { Message = "Invalid data.", Errors = ModelState.Values.SelectMany(v => v.Errors) });
        }

        // DELETE: api/Categories/5
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound(new { Message = "Category not found." });
            }

            // CRIT-17: soft-delete — prevent FK constraint violation if packages/history reference this category
            category.IsDeleted = true;
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message       = "Category deleted successfully.",
                DeletedItemId = id
            });
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.CategoryId == id && !e.IsDeleted);
        }
    }
}

