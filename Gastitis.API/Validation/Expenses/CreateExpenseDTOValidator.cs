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

			RuleFor(x => x.Date).Must(d => d.Kind == DateTimeKind.Utc)
				.WithMessage("'Date' must be a UTC timestamp ending in ‘Z’, e.g. 2026 - 11 - 16T10: 00:00Z.");

			RuleFor(x => x.Date)
				.Must(d => d >= new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc) 
					&& d <= timeProvider.GetUtcNow().AddYears(1).UtcDateTime)
				.WithMessage("´Date´ must be between the year 2000 and the next year.");

		}
	}
}


