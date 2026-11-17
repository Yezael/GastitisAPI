using FluentValidation;
using Gastitis.Application.DTOs.Categories;
using Gastitis.Domain.Constants;

namespace Gastitis.API.Validation.Categories
{
	public sealed class CreateCategoryDTOValidator : AbstractValidator<CreateCategoryDTO>
	{
		public CreateCategoryDTOValidator()
		{
			RuleFor(x => x.Name).NotEmpty().MaximumLength(CategoriesConstraints.CategoryNameMaxLength);
		}
	}
}


