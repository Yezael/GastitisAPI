using Gastitis.API.Validation.Expenses;
using Gastitis.Application.DTOs.Expenses;
using Microsoft.Extensions.Time.Testing;
using FluentValidation.TestHelper;

namespace Gastitis.API.Tests.Unit.Expenses
{
	public sealed class CreateExpenseDTOValidatorTests 
	{
		private readonly CreateExpenseDTOValidator _validator;
		private readonly FakeTimeProvider _timeProvider;

		public static TheoryData<decimal> InvalidValues => new() { 0m, -100m, 10.123m };

		public CreateExpenseDTOValidatorTests()
		{
			_timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
			_validator = new CreateExpenseDTOValidator(_timeProvider);

		}

		[Fact]
		public void ValidateCreateExpenseDTO_WithCorrectParameters_Passes()
		{
			var testDTO = GetValidDTO();

			var result = _validator.TestValidate(testDTO);
			result.ShouldNotHaveAnyValidationErrors();
		}

		[Theory]
		[MemberData(nameof(InvalidValues))]
		public void ValidateCreateExpenseDTO_WithIncorrectValue_Fails(decimal invalidValue)
		{
			var testDTO = GetValidDTO();
			testDTO.Value = invalidValue;

			var result = _validator.TestValidate(testDTO);
			result.ShouldHaveValidationErrorFor(x => x.Value);
		}

		CreateExpenseDTO GetValidDTO() => new CreateExpenseDTO()
		{
			CategoryId = 1,
			Value = 500,
			Description = "Test Description",
			Date = _timeProvider.GetUtcNow().UtcDateTime
		};

	}
}
