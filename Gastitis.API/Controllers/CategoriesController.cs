using Gastitis.Application.DTOs;
using Gastitis.Application.Interfaces;
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
        public async Task<IActionResult> CreateCategory(CreateCategoryDTO categoryRequest)
        {
            var newCategory = await _categoryService.CreateAsync(categoryRequest);
            return CreatedAtAction(
                nameof(GetCategories),
                new { id = newCategory.Id },
                newCategory);
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var allCats = await _categoryService.GetAllAsync();
            return Ok(allCats);
        }


        public async Task<IActionResult> GetCategoryById(int id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            return Ok(category);
        }
    }
}
