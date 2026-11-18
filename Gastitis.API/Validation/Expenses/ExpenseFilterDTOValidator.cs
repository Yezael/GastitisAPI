using FluentValidation;
using Gastitis.Application.DTOs.Expenses;
using Gastitis.Domain.Constants;

namespace Gastitis.API.Validation.Expenses
{
	public sealed class ExpenseFilterDTOValidator : AbstractValidator<ExpenseFilterDTO>
	{
		public ExpenseFilterDTOValidator(TimeProvider timeProvider)
		{
			RuleFor(x => x.Year).NotNull()
				.When(x => x.Month.HasValue)
				.WithMessage("'Year' must be provided when 'Month' is specified.");

			RuleFor(x => x.Year)
				.Must(x => x >= 2000 && x <= timeProvider.GetUtcNow().Year + 1)
				.When(x => x.Year.HasValue);

			RuleFor(x => x.Month)
				.InclusiveBetween(1, 12)
				.When(x => x.Month.HasValue);

			RuleFor(x => x.CategoryID)
				.NotEqual(0)
				.When(x => x.CategoryID.HasValue);

			RuleFor(x => x.Keyword)
				.MaximumLength(ExpenseConstraints.ExpenseFilterKeywordMaxLength);

			RuleFor(x => x.Page).GreaterThan(0);
			RuleFor(x => x.PageSize).InclusiveBetween(1, PagedResultConstraints.PagedResultMaxPageSize);
		}
	}

}