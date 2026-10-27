using Gastitis.Application.DTOs;
using Gastitis.Application.Exceptions;
using Gastitis.Application.Interfaces.Services;
using Gastitis.Domain.Entities;
using Gastitis.Domain.Enums;
using Gastitis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gastitis.Infrastructure.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly GastitisDBContext _dbContext;
        private readonly ICurrentUserService _currentUserservice;

        public CategoryService(GastitisDBContext dbContext, ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserservice = currentUserService;
        }

        public async Task<IReadOnlyList<CategoryResponseDTO>> GetAllAsync()
        {
            var currUserId = await _currentUserservice.GetUserIdAsync();

            return await _dbContext.Categories
                .AsNoTracking()
                .Where(c => 
                    c.SystemCode.HasValue 
                    || c.OwnerUserId == currUserId
                )
                .Select(c => new CategoryResponseDTO()
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();
        }

        public async Task<CategoryResponseDTO> GetByIdAsync(int id)
        {
            var currUserId = await _currentUserservice.GetUserIdAsync();

            var categoryEntity = await _dbContext.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                c.Id == id
                && (c.SystemCode.HasValue || c.OwnerUserId == currUserId));

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
            var currUserId = await _currentUserservice.GetUserIdAsync();

            var newCategoryEntity = new CategoryEntity()
            {
                Name = categoryRequest.Name,
                OwnerUserId = currUserId
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
            var currUserId = await _currentUserservice.GetUserIdAsync();

            //Using transaction here because we are both updating and deleting as two separate async operations, if throw, transaction rollback changes
            //automatically.
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            var categoryEntity = await _dbContext.Categories.FirstOrDefaultAsync(c => 
                c.Id == categoryId
                && (c.SystemCode.HasValue || c.OwnerUserId == currUserId));

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


