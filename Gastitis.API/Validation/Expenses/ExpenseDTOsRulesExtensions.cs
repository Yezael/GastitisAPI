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
	}
}