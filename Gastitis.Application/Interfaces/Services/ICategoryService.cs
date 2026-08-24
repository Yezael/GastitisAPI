
using Gastitis.Application.DTOs;
namespace Gastitis.Application.Interfaces
{
	public interface ICategoryService
	{
		Task<CategoryResponseDTO> CreateAsync(CreateCategoryDTO createCategory);
		Task<IReadOnlyList<CategoryResponseDTO>> GetAllAsync();
		Task<CategoryResponseDTO> GetByIdAsync(int id);
	}
}
