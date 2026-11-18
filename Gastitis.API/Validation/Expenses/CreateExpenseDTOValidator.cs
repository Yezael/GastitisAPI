using FluentValidation;
using Gastitis.Application.DTOs.Expenses;

namespace Gastitis.API.Validation.Expenses
{
	public sealed class CreateExpenseDTOValidator : AbstractValidator<CreateExpenseDTO>
	{
		public CreateExpenseDTOValidator(TimeProvider timeProvider)
		{
			RuleFor(x => x.Value).ValidateExpenseValue();
			RuleFor(x => x.Description).ValidateExpenseDescription();
			RuleFor(x => x.CategoryId).ValidateExpenseAssignedCategoryId();
			RuleFor(x => x.Date).ValidateExpenseDate(timeProvider);
		}
	}
}


