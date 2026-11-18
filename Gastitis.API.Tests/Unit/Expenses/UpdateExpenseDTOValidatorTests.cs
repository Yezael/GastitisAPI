using Gastitis.API.Validation.Expenses;
using Gastitis.Application.DTOs.Expenses;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Time.Testing;

namespace Gastitis.API.Tests.Unit.Expenses
{
	public sealed class UpdateExpenseDTOValidatorTests
	{
		private readonly UpdateExpenseDTOValidator _validator;
		private static readonly FakeTimeProvider _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));

		public static TheoryData<decimal> InvalidValues => new() { 0m, -100m, 10.123m };

		public static TheoryData<DateTime> InvalidDates => new() {
			new DateTime(1999, 1, 1,0,0,0, DateTimeKind.Utc),
			new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Local),
			new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Unspecified),
			new DateTime(_timeProvider.GetUtcNow().AddYears(2).Year, 1, 1,0,0,0, DateTimeKind.Utc) };

		public static TheoryData<string> InvalidDescriptions => new() {
			"",
			new string('a', Gastitis.Domain.Constants.ExpenseConstraints.ExpenseDescriptionMaxLength + 1),
			null!,
			"   "
		};

		public UpdateExpenseDTOValidatorTests()
		{
			_validator = new UpdateExpenseDTOValidator(_timeProvider);

		}

		[Fact]
		public void ValidateUpdateExpenseDTO_WithCorrectParameters_Passes()
		{
			var testDTO = GetValidDTO();

			var result = _validator.TestValidate(testDTO);
			result.ShouldNotHaveAnyValidationErrors();
		}

		[Theory]
		[MemberData(nameof(InvalidValues))]
		public void ValidateUpdateExpenseDTO_WithIncorrectValue_Fails(decimal invalidValue)
		{
			var testDTO = GetValidDTO();
			testDTO.Value = invalidValue;

			var result = _validator.TestValidate(testDTO);
			result.ShouldHaveValidationErrorFor(x => x.Value);
		}

		[Theory]
		[MemberData(nameof(InvalidDates))]
		public void ValidateUpdateExpenseDTO_WithInvalidDates_Fails(DateTime invalidDate)
		{
			var testDTO = GetValidDTO();
			testDTO.Date = invalidDate;

			var result = _validator.TestValidate(testDTO);
			result.ShouldHaveValidationErrorFor(x => x.Date);
		}

		[Theory]
		[MemberData(nameof(InvalidDescriptions))]
		public void ValidateUpdateExpenseDTO_WithIncorrectDescription_Fails(string invalidDescription)
		{
			var testDTO = GetValidDTO();
			testDTO.Description = invalidDescription;

			var result = _validator.TestValidate(testDTO);
			result.ShouldHaveValidationErrorFor(x => x.Description);
		}

		[Fact]
		public void ValidateUpdateExpenseDTO_WithIncorrectCategoryId_Fails()
		{
			var testDTO = GetValidDTO();
			testDTO.CategoryId = 0;

			var result = _validator.TestValidate(testDTO);
			result.ShouldHaveValidationErrorFor(x => x.CategoryId);
		}
		

		private static UpdateExpenseDTO GetValidDTO() => new UpdateExpenseDTO()
		{
			CategoryId = 1,
			Value = 500,
			Description = "Test Description",
			Date = _timeProvider.GetUtcNow().UtcDateTime
		};
	}
}
