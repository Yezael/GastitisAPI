using FluentValidation;
using Gastitis.Application.DTOs.Expenses;

namespace Gastitis.API.Validation.Expenses
{
	public sealed class UpdateExpenseDTOValidator : AbstractValidator<UpdateExpenseDTO>
	{
		public UpdateExpenseDTOValidator()
		{
			RuleFor(x => x.Value).ValidateExpenseValue();
			RuleFor(x => x.Description).ValidateExpenseDescription();
			RuleFor(x => x.CategoryId).ValidateExpenseAssignedCategoryId();
		}
	}

}
