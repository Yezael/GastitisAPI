using Gastitis.Application.DTOs;
using Gastitis.Application.Exceptions;
using Gastitis.Application.Interfaces;
using Gastitis.Domain.Entities;
using Gastitis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Gastitis.Application.DTOs.Summaries;

namespace Gastitis.Infrastructure.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly GastitisDBContext _dbContext;

        public ExpenseService(GastitisDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ExpenseResponseDTO> CreateAsync(CreateExpenseDTO expenseRequest)
        {
            var categoryExist = await _dbContext.Categories.AnyAsync(c => c.Id == expenseRequest.CategoryId);

            if (!categoryExist)
            {
                throw new NotFoundException("Category not found");
            }

            var newExpenseEntity = new ExpenseEntity()
            {
                Value = expenseRequest.Value,
                Description = expenseRequest.Description,
                CategoryId = expenseRequest.CategoryId,
                Date = expenseRequest.date
            };

            _dbContext.Expenses.Add(newExpenseEntity);
            await _dbContext.SaveChangesAsync();

            var response = new ExpenseResponseDTO()
            {
                Id = newExpenseEntity.Id,
                Value = newExpenseEntity.Value,
                Description = newExpenseEntity.Description,
                CategoryId = newExpenseEntity.CategoryId,
                Date = newExpenseEntity.Date
            };

            return response;
        }

        public async Task<PagedResponseDTO<ExpenseResponseDTO>> GetAllAsync(ExpenseFilterDTO filter, ExpenseSortingDTO sorting)
        {
            ValidatePaginationFilter(filter);

            IQueryable<ExpenseEntity> query =
                _dbContext.Expenses
                    .AsNoTracking();

            query = ApplyDateFilter(query, filter);
            query = ApplyCategoryFilter(query, filter);
            query = ApplySorting(query, sorting);

            
            var paginationMetaData = await GetPaginationMetaData(query, filter);


            query = ApplyPaginationFilter(query, filter);
            var items = await GetExpenseResponseDTOs(query);

            var pagedResult = new PagedResponseDTO<ExpenseResponseDTO>()
            {
                Items = items,
                Page = filter.Page,
                PageSize = items.Count,
                TotalPages = paginationMetaData.totalPages,
                TotalCount = paginationMetaData.totalCount
            };

            return pagedResult;
        }

        private static async Task<(int totalCount,int totalPages)> GetPaginationMetaData(
            IQueryable<ExpenseEntity> query, 
            ExpenseFilterDTO filter)
        {
            var totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);
            return (totalCount, totalPages);
        }

        private async Task<IReadOnlyList<ExpenseResponseDTO>> GetExpenseResponseDTOs(IQueryable<ExpenseEntity> query)
        {
            return await query
                .Select(e => new ExpenseResponseDTO()
                {
                    Id = e.Id,
                    Value = e.Value,
                    Description = e.Description,
                    CategoryId = e.CategoryId,
                    Date = e.Date
                })
                .ToListAsync();
        }

        private async Task<decimal> SumExpenses(IQueryable<ExpenseEntity> query)
        {
            return await query.
                SumAsync(e => e.Value);
        }

        private static IQueryable<ExpenseEntity> ApplyDateFilter(IQueryable<ExpenseEntity> query, ExpenseFilterDTO filter)
        {
            if (filter.Month.HasValue && !filter.Year.HasValue)
            {
                throw new ArgumentException(
                    "Month filter requires year.");
            }

            if (!filter.Year.HasValue) return query;


            DateTime startDate;
            DateTime endDate;
            if (filter.Month.HasValue)
            {
                startDate = new DateTime(filter.Year.Value, filter.Month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
                endDate = startDate.AddMonths(1);

            }
            else
            {
                startDate = new DateTime(filter.Year.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                endDate = startDate.AddYears(1);

            }

            return query.Where(e => e.Date >= startDate && e.Date < endDate);
        }

        private static IQueryable<ExpenseEntity> ApplyCategoryFilter(IQueryable<ExpenseEntity> query, ExpenseFilterDTO filter)
        {
            if (!filter.CategoryID.HasValue) return query;
            return query.Where(e => e.CategoryId == filter.CategoryID.Value);
        }

        private static void ValidatePaginationFilter(ExpenseFilterDTO filter)
        {
            if(filter.Page < 1)
            {
                throw new ArgumentException("Page must be greater than 0");
            }

            if (filter.PageSize < 1)
            {
                throw new ArgumentException(
                    "PageSize must be greater than 0.");
            }
        }

        private static IQueryable<ExpenseEntity> ApplySorting(IQueryable<ExpenseEntity> query, ExpenseSortingDTO sorting)
        {
            //then by ID is Important so that the order is deterministic even with same date expenses
            return sorting.SortBy switch
            {
                SortBy.Date => sorting.SortDirection == SortDirection.Asc
                ? query.OrderBy(e => e.Date).ThenBy(e => e.Id)
                : query.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id),
                SortBy.Value => sorting.SortDirection ==SortDirection.Asc
                ? query.OrderBy(e => e.Value).ThenBy(e => e.Id)
                : query.OrderByDescending(e => e.Value).ThenByDescending(e => e.Id),
                _ => throw new ArgumentException("Sorting by type not supported: " + sorting.SortBy)
            };
        }

        private static IQueryable<ExpenseEntity> ApplyPaginationFilter(IQueryable<ExpenseEntity> query, ExpenseFilterDTO filter)
        {
            int skip = (filter.Page - 1) * filter.PageSize;
            return query.Skip(skip).Take(filter.PageSize);
        }

        public async Task<ExpenseResponseDTO> GetByIdAsync(int id)
        {
            var expenseEntity = await _dbContext.Expenses
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (expenseEntity == null)
            {
                throw new NotFoundException("Expense not found");
            }

            var response = new ExpenseResponseDTO()
            {
                Id = expenseEntity.Id,
                Value = expenseEntity.Value,
                Description = expenseEntity.Description,
                CategoryId = expenseEntity.CategoryId,
                Date = expenseEntity.Date
            };
            return response;
        }

        public async Task<ExpensesSummaryResponseDTO> GetSummaryAsync(ExpenseFilterDTO filter)
        {
            IQueryable<ExpenseEntity> query =
                _dbContext.Expenses
                    .AsNoTracking();

            query = ApplyDateFilter(query, filter);
            query = ApplyCategoryFilter(query, filter);

            return new ExpensesSummaryResponseDTO()
            {
                TotalAmount = await query.SumAsync(e => e.Value),
                ExpensesCount = await query.CountAsync()
            }; 
        }

        public async Task<ExpenseResponseDTO> UpdateAsync(int id, UpdateExpenseDTO expenseToUpdate)
        {
            var expenseEntity = await _dbContext.Expenses
                .FirstOrDefaultAsync(e => e.Id == id);
            if (expenseEntity == null)
            {
                throw new NotFoundException("Expense not found");
            }
            var categoryExist = await _dbContext.Categories.AnyAsync(c => c.Id == expenseToUpdate.CategoryId);
            if (!categoryExist)
            {
                throw new NotFoundException("Category not found");
            }
            expenseEntity.Value = expenseToUpdate.Value;
            expenseEntity.Description = expenseToUpdate.Description;
            expenseEntity.CategoryId = expenseToUpdate.CategoryId;
            await _dbContext.SaveChangesAsync();
            var response = new ExpenseResponseDTO()
            {
                Id = expenseEntity.Id,
                Value = expenseEntity.Value,
                Description = expenseEntity.Description,
                CategoryId = expenseEntity.CategoryId,
                Date = expenseEntity.Date
            };
            return response;
        }


        public async Task<bool> DeleteAsync(int expenseId)
        {
            var expenseEntity = await _dbContext.Expenses
                .FirstOrDefaultAsync(e => e.Id == expenseId);
            if (expenseEntity == null) return false;
           
            _dbContext.Expenses.Remove(expenseEntity);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}


