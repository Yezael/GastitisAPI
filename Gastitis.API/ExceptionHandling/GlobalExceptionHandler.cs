using Gastitis.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Gastitis.API.ExceptionHandling
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext context,
            Exception exception,
            CancellationToken cancellationToken
            )
        {

            ProblemDetails problemDetail = null;
            string messageForLogger = exception.Message;

            switch  (exception)
            {
                case CategoryNotFoundException notFoundException:
                    problemDetail = new ProblemDetails()
                    {
                        Status = StatusCodes.Status404NotFound,
                        Title = "Category not found.",
                        Detail = notFoundException.Message
                    };
                    break;
                default:
                    problemDetail = new ProblemDetails()
                    {
                        Status = StatusCodes.Status500InternalServerError,
                        Title = "An unexpected error occurred.",
                        Detail = "An unexpected error occurred while processing the request."
                    };
                    _logger.LogError(exception, $"An unhandled error occurred: {exception.Message}");
                    break;
            }


            context.Response.StatusCode = problemDetail.Status.Value;
            await context.Response.WriteAsJsonAsync(problemDetail, cancellationToken);

            return true;
        }
    }
}
