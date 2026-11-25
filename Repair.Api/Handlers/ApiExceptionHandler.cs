using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Repair.Api.Exceptions;

namespace Repair.Api.Handlers;

public class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext,
        Exception exception, CancellationToken cancellationToken)
    {
        int statusCode = exception switch
        {
            EntityNotFoundException => StatusCodes.Status404NotFound,
            EntityConcurrencyException => StatusCodes.Status409Conflict,
            EntityValidationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError,
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled API exception.");

        httpContext.Response.StatusCode = statusCode;
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(statusCode),
            Detail = statusCode == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred." : exception.Message,
        };

        await httpContext.Response.WriteAsJsonAsync(problem,
            options: null, contentType: "application/problem+json",
            cancellationToken: cancellationToken);
        return true;
    }
}
