using Gastitis.API.Validation.Expenses;
using Gastitis.Application.DTOs.Expenses;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Time.Testing;
using Gastitis.Domain.Constants;

namespace Gastitis.API.Tests.Unit.Expenses
{
	public sealed class ExpenseFilterDTOValidatorTests
	{
		private readonly ExpenseFilterDTOValidator _validator;
		private static readonly FakeTimeProvider _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));

		public static TheoryData<int> InvalidYears => new()
		{
			1999,
			_timeProvider.GetUtcNow().AddYears(2).Year
		};

		public static TheoryData<int> InvalidPageSizes => new()
		{
			0,
			PagedResultConstraints.PagedResultMaxPageSize + 1
		};

		public ExpenseFilterDTOValidatorTests()
		{
			_validator = new ExpenseFilterDTOValidator(_timeProvider);

		}

		[Fact]
		public void ValidateExpenseFilterDTO_WithCorrectParameters_Passes()
		{
			var testDTO = GetValidDTO();

			var result = _validator.TestValidate(testDTO);
			result.ShouldNotHaveAnyValidationErrors();
		}

		[Fact]
		public void ValidateExpenseFilterDTO_WithMonthWithoutYear_Fails()
		{
			var testDTO = GetValidDTO();
			testDTO.Year = null;

			var result = _validator.TestValidate(testDTO);
			result.ShouldHaveValidationErrorFor(x => x.Year);
		}

		[Theory]
		[MemberData(nameof(InvalidYears))]
		public void ValidateExpenseFilterDTO_WithInvalidYears_Fails(int year)
		{
			var testDTO = GetValidDTO();
			testDTO.Year = year;

			var result = _validator.TestValidate(testDTO);
			result.ShouldHaveValidationErrorFor(x => x.Year);
		}

		[Theory]
		[InlineData(13)]
		[InlineData(0)]
		public void ValidateExpenseFilterDTO_WithInvalidMonth_Fails(int month)
		{
			var testDTO = GetValidDTO();
			testDTO.Month = month;

			var result = _validator.TestValidate(testDTO);
			result.ShouldHaveValidationErrorFor(x => x.Month);
		}

		[Fact]
		public void ValidateExpenseFilterDTO_WithInvalidCategoryId_Fails()
		{
			var testDTO = GetValidDTO();
			testDTO.CategoryID = 0;

			var result = _validator.TestValidate(testDTO);
			result.ShouldHaveValidationErrorFor(x => x.CategoryID);
		}

		[Fact]
		public void ValidateExpenseFilterDTO_WithInvalidKeyword_Fails()
		{
			var testDTO = GetValidDTO();
			testDTO.Keyword = new string('a', ExpenseConstraints.ExpenseFilterKeywordMaxLength + 1);

			var result = _validator.TestValidate(testDTO);
			result.ShouldHaveValidationErrorFor(x => x.Keyword);
		}

		[Fact]
		public void ValidateExpenseFilterDTO_WithInvalidPage_Fails()
		{
			var testDTO = GetValidDTO();
			testDTO.Page = 0;

			var result = _validator.TestValidate(testDTO);
			result.ShouldHaveValidationErrorFor(x => x.Page);
		}

		[Theory]
		[MemberData(nameof(InvalidPageSizes))]
		public void ValidateExpenseFilterDTO_WithInvalidPageSize_Fails(int pageSize)
		{
			var testDTO = GetValidDTO();
			testDTO.PageSize = pageSize;

			var result = _validator.TestValidate(testDTO);
			result.ShouldHaveValidationErrorFor(x => x.PageSize);
		}



		private static ExpenseFilterDTO GetValidDTO() => new ExpenseFilterDTO()
		{
			Year = 2026,
			Month = 6,
			CategoryID = 1,
			Keyword = "keyword",
			Page = 1,
			PageSize = 20
		};
	}
}
