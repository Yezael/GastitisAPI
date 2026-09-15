using Gastitis.Application.DTOs;
using Gastitis.Application.Exceptions;
using Gastitis.Application.Interfaces;
using Gastitis.Domain.Entities;
using Gastitis.Domain.Enums;
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

        public async Task DeleteAsync(int categoryId)
        {
            //Using transaction here because we are both updating and deleting as two separate async operations, if throw, transaction rollback changes
            //automatically.
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            var categoryEntity = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == categoryId);

            if (categoryEntity == null)
            {
                throw new CategoryNotFoundException(categoryId);
            }

            if (categoryEntity.SystemCode.HasValue)
            {
                throw new SystemCategoryCannotBeDeletedException(categoryId);
            }

            var noneCategoryId = await _dbContext.Categories
                .Where(c => c.SystemCode == SystemCategoryCode.None)
                .Select(c => c.Id)
                .SingleAsync();

            await _dbContext.Expenses
                .Where(e => e.CategoryId == categoryId)
                .ExecuteUpdateAsync(update =>
                    update.SetProperty(e => e.CategoryId, noneCategoryId));

            _dbContext.Categories.Remove(categoryEntity);
            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
		}
	}
}


