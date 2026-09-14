using Gastitis.Application.DTOs;
using Gastitis.Application.DTOs.Summaries;

namespace Gastitis.Application.Interfaces
{
	public interface IExpenseService
	{
		Task<ExpenseResponseDTO> CreateAsync(CreateExpenseDTO createExpense);
		Task<ExpenseResponseDTO> UpdateAsync(int id, UpdateExpenseDTO expenseToUpdate);
		Task DeleteAsync(int expenseId);
		Task<PagedResponseDTO<ExpenseResponseDTO>> GetAllAsync(ExpenseFilterDTO filter, ExpenseSortingDTO sorting);
		Task<ExpenseResponseDTO> GetByIdAsync(int id);
		Task<ExpensesSummaryResponseDTO> GetSummaryAsync(ExpenseFilterDTO filter);
		Task<PagedResponseDTO<ExpensesCategorySummaryResponseDTO>> GetCategorySummaryAsync(ExpenseFilterDTO filter);
	}
}
