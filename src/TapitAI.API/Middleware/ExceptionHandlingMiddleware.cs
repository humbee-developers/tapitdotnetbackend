using System.Text.Json;
using TapitAI.Application.Common.Exceptions;
using TapitAI.Domain.Exceptions;

namespace TapitAI.API.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, response) = exception switch
        {
            ValidationException ve => (StatusCodes.Status422UnprocessableEntity,
                new ErrorResponse("Validation failed", ve.Errors)),

            NotFoundException nfe => (StatusCodes.Status404NotFound,
                new ErrorResponse(nfe.Message)),

            DomainException de => (StatusCodes.Status400BadRequest,
                new ErrorResponse(de.Message)),

            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized,
                new ErrorResponse("Unauthorized")),

            _ => (StatusCodes.Status500InternalServerError,
                new ErrorResponse("An unexpected error occurred."))
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private record ErrorResponse(string Message, object? Details = null);
}
