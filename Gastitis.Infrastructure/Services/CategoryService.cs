using Gastitis.Application.DTOs;
using Gastitis.Application.Exceptions;
using Gastitis.Application.Interfaces;
using Gastitis.Domain.Entities;
using Gastitis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gastitis.Infrastructure.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly GastitisDBContext _dbContext;

        public CategoryService(GastitisDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<CategoryResponseDTO>> GetAllAsync()
        {
            return await _dbContext.Categories
                .AsNoTracking()
                .Select(c => new CategoryResponseDTO()
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();
        }

        public async Task<CategoryResponseDTO> GetByIdAsync(int id)
        {
            var categoryEntity = await _dbContext.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (categoryEntity == null)
            {
                throw new CategoryNotFoundException(id);
            }

            var response = new CategoryResponseDTO()
            {
                Id = categoryEntity.Id,
                Name = categoryEntity.Name
            };
            return response;
        }

        public async Task<CategoryResponseDTO> CreateAsync(CreateCategoryDTO categoryRequest)
        {
            var newCategoryEntity = new CategoryEntity()
            {
                Name = categoryRequest.Name
            };

            _dbContext.Categories.Add(newCategoryEntity);
            await _dbContext.SaveChangesAsync();

            var response = new CategoryResponseDTO()
            {
                Id = newCategoryEntity.Id,
                Name = newCategoryEntity.Name
            };
            return response;
        }
    }
}


