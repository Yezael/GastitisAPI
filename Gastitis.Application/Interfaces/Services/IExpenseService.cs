using Gastitis.Application.DTOs;
using Gastitis.Application.DTOs.Categories;
using Gastitis.Application.DTOs.Expenses;
using Gastitis.Application.DTOs.Summaries;

namespace Gastitis.Application.Interfaces.Services
{
	public interface IExpenseService
	{
		Task<ExpenseResponseDTO> CreateAsync(CreateExpenseDTO createExpense, CancellationToken cancellationToken);
		Task<ExpenseResponseDTO> UpdateAsync(int id, UpdateExpenseDTO expenseToUpdate, CancellationToken cancellationToken);
		Task DeleteAsync(int expenseId, CancellationToken cancellationToken);
		Task<PagedResponseDTO<ExpenseResponseDTO>> GetAllAsync(ExpenseFilterDTO filter, ExpenseSortingDTO sorting, CancellationToken cancellationToken);
		Task<ExpenseResponseDTO> GetByIdAsync(int id, CancellationToken cancellationToken);
		Task<ExpensesSummaryResponseDTO> GetSummaryAsync(ExpenseFilterDTO filter, CancellationToken cancellationToken);
		Task<PagedResponseDTO<ExpensesCategorySummaryResponseDTO>> GetCategorySummaryAsync(ExpenseFilterDTO filter, CancellationToken cancellationToken);
	}
}
