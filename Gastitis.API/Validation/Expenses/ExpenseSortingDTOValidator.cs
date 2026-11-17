using FluentValidation;
using Gastitis.Application.DTOs.Expenses;

namespace Gastitis.API.Validation.Expenses
{
	public sealed class ExpenseSortingDTOValidator : AbstractValidator<ExpenseSortingDTO>
	{
		public ExpenseSortingDTOValidator()
		{
			RuleFor(x => x.SortBy).IsInEnum();
			RuleFor(x => x.SortDirection).IsInEnum();
		}
	}

}