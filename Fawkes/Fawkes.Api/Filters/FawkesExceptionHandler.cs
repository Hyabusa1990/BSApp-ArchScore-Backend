using Fawkes.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Fawkes.Api.Filters
{
    /// <summary>
    /// Translates the domain exceptions from Fawkes.Core into ProblemDetails responses.
    /// Any other exception is left to the default handler, which answers with a generic 500.
    /// </summary>
    public class FawkesExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<FawkesExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is not FawkesException fawkesException)
                return false;

            var (status, title) = fawkesException switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
                AccessDeniedException => (StatusCodes.Status403Forbidden, "Forbidden"),
                ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
                InvalidRequestException => (StatusCodes.Status400BadRequest, "Bad Request"),
                _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
            };

            // Expected client errors: logged once here at information level, without stack trace.
            logger.LogInformation("{Method} {Path} answered with {StatusCode} {ErrorCode}: {Detail}",
                httpContext.Request.Method, httpContext.Request.Path, status, fawkesException.Code, fawkesException.Message);

            httpContext.Response.StatusCode = status;

            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = status,
                    Title = title,
                    Detail = fawkesException.Message,
                    Extensions = { ["code"] = fawkesException.Code }
                }
            });
        }
    }
}
