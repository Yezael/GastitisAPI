using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Gastitis.API.Validation
{
	public sealed class FluentValidationActionFilter : IAsyncActionFilter
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ProblemDetailsFactory _problemDetailsFactory;

		public FluentValidationActionFilter(IServiceProvider serviceProvider, ProblemDetailsFactory problemDetailsFactory)
		{
			_serviceProvider = serviceProvider;
			_problemDetailsFactory = problemDetailsFactory;
		}

		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
			var cancellationToken = context.HttpContext.RequestAborted;

			foreach (var argument in context.ActionArguments.Values)
			{
				if (argument is null) continue;

				var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
				if (_serviceProvider.GetService(validatorType) is not IValidator validator) continue;

				var result = await validator.ValidateAsync(new ValidationContext<object>(argument), cancellationToken);

				foreach (var error in result.Errors)
				{
					context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
				}
			}

			if (!context.ModelState.IsValid)
			{
				var problem = _problemDetailsFactory.CreateValidationProblemDetails(
					context.HttpContext,
					context.ModelState,
					StatusCodes.Status400BadRequest);

				context.Result = new ObjectResult(problem)
				{
					StatusCode = StatusCodes.Status400BadRequest,
					ContentTypes = { "application/problem+json" }
				};
				return;
			}

			await next();
		}
	}
}
