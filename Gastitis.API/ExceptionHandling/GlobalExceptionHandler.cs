using Gastitis.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Gastitis.API.ExceptionHandling
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IProblemDetailsService _problemDetailsService;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetailsService)
        {
            _logger = logger;
            _problemDetailsService = problemDetailsService;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext context,
            Exception exception,
            CancellationToken cancellationToken
            )
        {

            if(exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
            {
                _logger.LogInformation("Request {Method} {Path} was cancelled by the client.", context.Request.Method, context.Request.Path);
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
                }
                return true;
            }


            ProblemDetails problemDetail = exception switch
            {
                ApplicationValidationException validation => new ValidationProblemDetails(
                    validation.Errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred.",
                },
                CategoryNotFoundException e => Problem(StatusCodes.Status404NotFound, "Category not found.", e.Message),
                ExpenseNotFoundException e => Problem(StatusCodes.Status404NotFound, "Expense not found.", e.Message),
				SystemCategoryCannotBeDeletedException e => Problem(StatusCodes.Status409Conflict, "System category cannot be deleted.", e.Message),
                _ => Problem(StatusCodes.Status500InternalServerError, "An unexpected error occurred.", "An unexpected error occurred while processing the request.")
			};

            if(problemDetail.Status == StatusCodes.Status500InternalServerError)
			{
				_logger.LogError(exception, "An unhandled error occurred: {Method} {Path}", context.Request.Method, context.Request.Path);
			}
            context.Response.StatusCode = problemDetail.Status!.Value;

            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
			{
				HttpContext = context,
				ProblemDetails = problemDetail,
                Exception = exception
			});
        }

        private static ProblemDetails Problem(int status, string title, string detail) =>
            new() { Status = status, Title = title, Detail = detail };
    }
}
