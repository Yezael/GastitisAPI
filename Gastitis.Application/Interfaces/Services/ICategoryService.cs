using Gastitis.Application.DTOs.Categories;

namespace Gastitis.Application.Interfaces.Services
{
	public interface ICategoryService
	{
		Task<CategoryResponseDTO> CreateAsync(CreateCategoryDTO createCategory, CancellationToken cancellationToken);
		Task<IReadOnlyList<CategoryResponseDTO>> GetAllAsync(CancellationToken cancellationToken);
		Task<CategoryResponseDTO> GetByIdAsync(int id, CancellationToken cancellationToken);
		Task DeleteAsync(int categoryID, CancellationToken cancellationToken);
	}
}
