
using Gastitis.Application.DTOs;

namespace Gastitis.Application.Interfaces.Services
{
	public interface ICategoryService
	{
		Task<CategoryResponseDTO> CreateAsync(CreateCategoryDTO createCategory);
		Task<IReadOnlyList<CategoryResponseDTO>> GetAllAsync();
		Task<CategoryResponseDTO> GetByIdAsync(int id);
		Task DeleteAsync(int categoryID);
	}
}
