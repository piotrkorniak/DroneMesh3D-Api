using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace DroneMesh3D.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException)
        {
            logger.LogInformation("Request was cancelled.");
            httpContext.Response.StatusCode = 499; // Client Closed Request
            return true;
        }

        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .Select(e => e.ErrorMessage)
                .ToList();

            logger.LogWarning(
                validationException,
                "Validation failed: {Errors}",
                string.Join("; ", errors));

            httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            httpContext.Response.ContentType = "application/json";

            var response = new
            {
                message = "Validation failed.",
                errors
            };

            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
            return true;
        }

        logger.LogError(
            exception,
            "An unhandled exception occurred: {Message}",
            exception.Message);

        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        httpContext.Response.ContentType = "application/json";

        var errorResponse = new
        {
            message = "An unexpected error occurred."
        };

        await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);
        return true;
    }
}
