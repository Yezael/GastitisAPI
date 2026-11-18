using Gastitis.Application.DTOs.Categories;
using Gastitis.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gastitis.API.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory(CreateCategoryDTO categoryRequest, CancellationToken cancellationToken)
        {
            var newCategory = await _categoryService.CreateAsync(categoryRequest, cancellationToken);
            return CreatedAtAction(
                nameof(GetCategoryById),
                new { id = newCategory.Id },
                newCategory);
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
        {
            var allCats = await _categoryService.GetAllAsync(cancellationToken);
            return Ok(allCats);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCategoryById(int id, CancellationToken cancellationToken)
        {
            var category = await _categoryService.GetByIdAsync(id, cancellationToken);
            return Ok(category);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCategoryById(int id, CancellationToken cancellationToken)
        {
            await _categoryService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
