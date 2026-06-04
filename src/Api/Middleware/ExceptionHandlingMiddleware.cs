using System.Net;
using System.Text.Json;

namespace MultiVendor.Ecommerce.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await WriteProblemAsync(context, ex);
        }
    }

    private static Task WriteProblemAsync(HttpContext context, Exception ex)
    {
        var (statusCode, title) = ex switch
        {
            KeyNotFoundException      => (HttpStatusCode.NotFound,            "Resource not found."),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden,         "Access denied."),
            InvalidOperationException => (HttpStatusCode.BadRequest,          "Bad request."),
            _                         => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type   = $"https://httpstatuses.com/{(int)statusCode}",
            title,
            status = (int)statusCode,
            detail = ex.Message,
            traceId = context.TraceIdentifier
        };

        return context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }
}
