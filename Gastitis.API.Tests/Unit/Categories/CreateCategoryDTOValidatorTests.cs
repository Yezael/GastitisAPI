using FluentValidation.TestHelper;
using Gastitis.API.Validation.Categories;
using Gastitis.Application.DTOs.Categories;

namespace Gastitis.API.Tests.Unit.Categories
{
	public sealed class CreateCategoryDTOValidatorTests
	{
		public static TheoryData<string> WrongCategoryNames => new TheoryData<string> {
			"",
			new('a', Gastitis.Domain.Constants.CategoriesConstraints.CategoryNameMaxLength + 1),
			null!,
			"   "
		};

		private readonly CreateCategoryDTOValidator _validator;

		public CreateCategoryDTOValidatorTests()
		{
			_validator = new CreateCategoryDTOValidator();
		}


		[Theory]
		[MemberData(nameof(WrongCategoryNames))]
		public void ValidateCreateCategoryDTO_WithInvalidName_Fails(string invalidName)
		{
			var testDTO = GetCreateCategoryDTO(invalidName);
			var result = _validator.TestValidate(testDTO);
			result.ShouldHaveValidationErrorFor(x => x.Name);
		}

		[Fact]
		public void ValidateCreateCategoryDTO_WithValidName_Succeeds()
		{
			var testDTO = GetCreateCategoryDTO();
			var result = _validator.TestValidate(testDTO);
			result.ShouldNotHaveAnyValidationErrors();
		}

		public static CreateCategoryDTO GetCreateCategoryDTO(string customName = "Test Category") 
			=> new CreateCategoryDTO() { Name = customName };

	}
}
