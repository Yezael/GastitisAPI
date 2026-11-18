using FluentValidation;
using Gastitis.Domain.Constants;

namespace Gastitis.API.Validation.Expenses
{

	public static class ExpenseDTOsRulesExtensions
	{
		public static IRuleBuilderOptions<T, decimal> ValidateExpenseValue<T>(
			this IRuleBuilder<T, decimal> ruleInitial)
		{
			return ruleInitial
				.GreaterThan(0)
				.PrecisionScale(
					ExpenseConstraints.ExpenseValuePrecision, 
					ExpenseConstraints.ExpenseValueScale, 
					ignoreTrailingZeros: true);
		}

		public static IRuleBuilderOptions<T, string> ValidateExpenseDescription<T>(
			this IRuleBuilder<T, string> ruleInitial)
		{
			return ruleInitial.NotEmpty().MaximumLength(ExpenseConstraints.ExpenseDescriptionMaxLength);
		}

		public static IRuleBuilderOptions<T, int> ValidateExpenseAssignedCategoryId<T>(
			this IRuleBuilder<T, int> ruleInitial)
		{
			return ruleInitial.NotEqual(0);
		}

		public static IRuleBuilderOptions<T, DateTime> ValidateExpenseDate<T>(
			this IRuleBuilder<T, DateTime> ruleInitial, TimeProvider timeProvider)
		{
			return ruleInitial
				.Must(d => d.Kind == DateTimeKind.Utc)
					.WithMessage("'Date' must be a UTC timestamp ending in 'Z', e.g. 2026-11-16T10:00:00Z.")
				.Must(d => 
				d >= new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc) 
				&& d <= timeProvider.GetUtcNow().AddYears(1).UtcDateTime)
					.WithMessage("'Date' must be between the year 2000 and the next year.");
		}
	}
}