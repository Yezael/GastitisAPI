using Gastitis.Application.DTOs;
using Gastitis.Application.Exceptions;
using Gastitis.Application.Interfaces.Services;
using Gastitis.Domain.Entities;
using Gastitis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Gastitis.Application.DTOs.Summaries;
using Gastitis.Application.DTOs.Expenses;
using Gastitis.Application.DTOs.Categories;

namespace Gastitis.Infrastructure.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly GastitisDBContext _dbContext;
        private readonly ICurrentUserService _currUserService;

        public ExpenseService(GastitisDBContext dbContext, ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currUserService = currentUserService;
        }

        public async Task<ExpenseResponseDTO> CreateAsync(CreateExpenseDTO expenseRequest, CancellationToken cancellationToken)
        {
            var userId = await _currUserService.GetUserIdAsync(cancellationToken);

            var categoryExist = await _dbContext.Categories.AnyAsync(c => 
                c.Id == expenseRequest.CategoryId 
                && (c.SystemCode.HasValue || c.OwnerUserId == userId)
                , cancellationToken);

            if (!categoryExist)
            {
                throw new CategoryNotFoundException(expenseRequest.CategoryId);
            }

            var newExpenseEntity = new ExpenseEntity()
            {
                Value = expenseRequest.Value,
                Description = expenseRequest.Description,
                CategoryId = expenseRequest.CategoryId,
                Date = expenseRequest.Date,
                OwnerUserId = userId
            };

            _dbContext.Expenses.Add(newExpenseEntity);
            await _dbContext.SaveChangesAsync(cancellationToken);

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

        public async Task<PagedResponseDTO<ExpenseResponseDTO>> GetAllAsync(ExpenseFilterDTO filter, ExpenseSortingDTO sorting, CancellationToken cancellationToken)
        {
            ValidatePaginationFilter(filter);

            var currUserId = await _currUserService.GetUserIdAsync(cancellationToken);

            IQueryable<ExpenseEntity> query =
                _dbContext.Expenses.AsNoTracking()
                .Where(e => e.OwnerUserId == currUserId);

            query = ApplyDateFilter(query, filter);
            query = ApplyCategoryFilter(query, filter);
            query = ApplyKeywordFilter(query, filter);
            query = ApplySorting(query, sorting);

            var paginationMetaData = await GetPaginationMetaData(query, filter, cancellationToken);

            query = ApplyPaginationFilter(query, filter);
            var items = await GetExpenseResponseDTOs(query, cancellationToken);

            var pagedResult = new PagedResponseDTO<ExpenseResponseDTO>()
            {
                Items = items,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = paginationMetaData.totalPages,
                TotalCount = paginationMetaData.totalCount
            };

            return pagedResult;
        }

        private static async Task<(int totalCount,int totalPages)> GetPaginationMetaData<T>(
            IQueryable<T> query, 
            ExpenseFilterDTO filter
            , CancellationToken cancellationToken)
        {
            var totalCount = await query.CountAsync(cancellationToken);
            int totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);
            return (totalCount, totalPages);
        }

        private async Task<IReadOnlyList<ExpenseResponseDTO>> GetExpenseResponseDTOs(IQueryable<ExpenseEntity> query, CancellationToken cancellationToken)
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
                .ToListAsync(cancellationToken);
        }

        private static IQueryable<ExpenseEntity> ApplyDateFilter(IQueryable<ExpenseEntity> query, ExpenseFilterDTO filter)
        {
            if (filter.Month.HasValue && !filter.Year.HasValue)
            {
                throw new ApplicationValidationException(nameof(filter.Year), "Month filter requires year.");
            }

            if(filter.Month.HasValue && (filter.Month < 1 || filter.Month > 12))
			{
				throw new ApplicationValidationException(nameof(filter.Month), "Month filter must be between 1 and 12.");
			}

            if(filter.Year.HasValue && (filter.Year < 1 || filter.Year > 9999))
			{
				throw new ApplicationValidationException(nameof(filter.Year), "Year filter must be between 1 and 9999.");
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

        private static IQueryable<ExpenseEntity> ApplyKeywordFilter(IQueryable<ExpenseEntity> query, ExpenseFilterDTO filter)
        {
            if (string.IsNullOrWhiteSpace(filter.Keyword)) return query;
            return query.Where(e => EF.Functions.ILike(e.Description,$"%{filter.Keyword}%"));
        }

        private static void ValidatePaginationFilter(ExpenseFilterDTO filter)
        {
            if(filter.Page < 1)
            {
                throw new ApplicationValidationException(nameof(filter.Page), "Page must be greater than 0");
            }

            if (filter.PageSize < 1)
            {
                throw new ApplicationValidationException(nameof(filter.PageSize), "PageSize must be greater than 0.");
            }
        }

        private static IQueryable<ExpenseEntity> ApplySorting(IQueryable<ExpenseEntity> query, ExpenseSortingDTO sorting)
        {
            if (!Enum.IsDefined(sorting.SortDirection))
            {
				throw new ApplicationValidationException(nameof(sorting.SortDirection), "Invalid sort direction: " + sorting.SortDirection);
			}

            //then by ID is Important so that the order is deterministic even with same date expenses
            return sorting.SortBy switch
            {
                SortBy.Date => sorting.SortDirection == SortDirection.Asc
                ? query.OrderBy(e => e.Date).ThenBy(e => e.Id)
                : query.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id),
                SortBy.Value => sorting.SortDirection ==SortDirection.Asc
                ? query.OrderBy(e => e.Value).ThenBy(e => e.Id)
                : query.OrderByDescending(e => e.Value).ThenByDescending(e => e.Id),
                _ => throw new ApplicationValidationException(nameof(sorting.SortBy), "Sorting by type not supported: " + sorting.SortBy)
            };
        }

        private static IQueryable<ExpenseEntity> ApplyPaginationFilter(IQueryable<ExpenseEntity> query, ExpenseFilterDTO filter)
        {
            int skip = (filter.Page - 1) * filter.PageSize;
            return query.Skip(skip).Take(filter.PageSize);
        }

        public async Task<ExpenseResponseDTO> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
			var userId = await _currUserService.GetUserIdAsync(cancellationToken);

			var expenseEntity = await _dbContext.Expenses
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    e => e.Id == id 
                    && e.OwnerUserId == userId
                    , cancellationToken);

            if (expenseEntity == null)
            {
                throw new ExpenseNotFoundException(id);
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

        public async Task<ExpensesSummaryResponseDTO> GetSummaryAsync(ExpenseFilterDTO filter, CancellationToken cancellationToken)
        {
            var currUserId = await _currUserService.GetUserIdAsync(cancellationToken);

            IQueryable<ExpenseEntity> query =
                _dbContext.Expenses
                    .AsNoTracking()
                    .Where(e => e.OwnerUserId == currUserId);

            query = ApplyDateFilter(query, filter);
            query = ApplyCategoryFilter(query, filter);
            query = ApplyKeywordFilter(query, filter);

            return new ExpensesSummaryResponseDTO()
            {
                TotalAmount = await query.SumAsync(e => e.Value, cancellationToken),
                ExpensesCount = await query.CountAsync(cancellationToken)
            }; 
        }

        public async Task<PagedResponseDTO<ExpensesCategorySummaryResponseDTO>> GetCategorySummaryAsync(
            ExpenseFilterDTO filterDTO, CancellationToken cancellationToken)
        {

            var currentUserId = await _currUserService.GetUserIdAsync(cancellationToken);

            ValidatePaginationFilter(filterDTO);

            var query = _dbContext.Expenses.AsNoTracking().Where(e => e.OwnerUserId == currentUserId);

            query = ApplyDateFilter(query, filterDTO);
            query = ApplyCategoryFilter(query, filterDTO);
            query = ApplyKeywordFilter(query, filterDTO);


            var queryByCategory = query.GroupBy(e => new { e.CategoryId, e.Category.Name })
                .Select(g => new ExpensesCategorySummaryResponseDTO()
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.Name,
                    TotalAmount = g.Sum(e => e.Value),
                    ExpenseCount = g.Count()
                })
                .OrderBy(e => e.CategoryId);

            var paginationMetaData = await GetPaginationMetaData(queryByCategory, filterDTO, cancellationToken);

            var items = await queryByCategory
                .Skip((filterDTO.Page - 1) * filterDTO.PageSize)
                .Take(filterDTO.PageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResponseDTO<ExpensesCategorySummaryResponseDTO>()
            {
                Items = items,
                Page = filterDTO.Page,
                PageSize = filterDTO.PageSize,
                TotalPages = paginationMetaData.totalPages,
                TotalCount = paginationMetaData.totalCount
            };

            return pagedResult;
        }

        public async Task<ExpenseResponseDTO> UpdateAsync(int id, UpdateExpenseDTO expenseToUpdate, CancellationToken cancellationToken)
        {
            var currentUserId = await _currUserService.GetUserIdAsync(cancellationToken);

            var expenseEntity = await _dbContext.Expenses
                .FirstOrDefaultAsync(e => 
                    e.Id == id
                    && e.OwnerUserId == currentUserId
                    , cancellationToken);

            if (expenseEntity == null)
            {
                throw new ExpenseNotFoundException(id);
            }
            var categoryExist = await _dbContext.Categories.AnyAsync(
                c => 
                c.Id == expenseToUpdate.CategoryId 
                && (c.SystemCode.HasValue || c.OwnerUserId == currentUserId)
                , cancellationToken);

            if (!categoryExist)
            {
                throw new CategoryNotFoundException(expenseToUpdate.CategoryId);
            }
            expenseEntity.Value = expenseToUpdate.Value;
            expenseEntity.Description = expenseToUpdate.Description;
            expenseEntity.CategoryId = expenseToUpdate.CategoryId;
            await _dbContext.SaveChangesAsync(cancellationToken);
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


        public async Task DeleteAsync(int expenseId, CancellationToken cancellationToken)
        {
            var currentUserId = await _currUserService.GetUserIdAsync(cancellationToken);

            var expenseEntity = await _dbContext.Expenses
                .FirstOrDefaultAsync(e => e.Id == expenseId && e.OwnerUserId == currentUserId, cancellationToken);

            if (expenseEntity == null)
            {
				throw new ExpenseNotFoundException(expenseId);
			}
           
            _dbContext.Expenses.Remove(expenseEntity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}


