using Gastitis.API.Validation.Expenses;
using Gastitis.Application.DTOs.Expenses;
using FluentValidation.TestHelper;

namespace Gastitis.API.Tests.Unit.Expenses
{
	public sealed class ExpenseSortingDTOValidatorTests
	{
		private readonly ExpenseSortingDTOValidator _validator;
		
		public ExpenseSortingDTOValidatorTests()
		{
			_validator = new ExpenseSortingDTOValidator();
		}

		[Fact]
		public void ValidateExpenseSortingDTO_WithCorrectParameters_Passes()
		{
			var testDTO = GetValidDTO();

			var result = _validator.TestValidate(testDTO);
			result.ShouldNotHaveAnyValidationErrors();
		}

		[Fact]
		public void ValidateExpenseSortingDTO_WithInvalidSortBy_Fails()
		{
			var testDTO = GetValidDTO();
			testDTO.SortBy = (SortBy)999; 

			var result = _validator.TestValidate(testDTO);
			result.ShouldHaveValidationErrorFor(x => x.SortBy);
		}

		[Fact]
		public void ValidateExpenseSortingDTO_WithInvalidSortDirection_Fails()
		{
			var testDTO = GetValidDTO();
			testDTO.SortDirection = (SortDirection)999; 

			var result = _validator.TestValidate(testDTO);
			result.ShouldHaveValidationErrorFor(x => x.SortDirection);
		}

		private static ExpenseSortingDTO GetValidDTO() => new ExpenseSortingDTO()
		{
			SortBy = SortBy.Date,
			SortDirection = SortDirection.Desc
		};
	}
}
