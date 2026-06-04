using System.Net;
using System.Text.Json;
using Serilog;

namespace MultiVendor.Ecommerce.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await WriteProblemAsync(context, ex);
        }
    }

    private static Task WriteProblemAsync(HttpContext context, Exception ex)
    {
        var (statusCode, title) = ex switch
        {
            KeyNotFoundException        => (HttpStatusCode.NotFound,            "Resource not found."),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden,           "Access denied."),
            InvalidOperationException e when e.Message.Contains("SKU")
                                        => (HttpStatusCode.Conflict,            "Conflict."),
            InvalidOperationException   => (HttpStatusCode.BadRequest,          "Bad request."),
            _                           => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        switch (statusCode)
        {
            case HttpStatusCode.NotFound:
                Log.Warning("Resource not found: {Message}", ex.Message);
                break;
            case HttpStatusCode.Forbidden:
                Log.Warning("Unauthorized access attempt: {Message}", ex.Message);
                break;
            case HttpStatusCode.Conflict:
            case HttpStatusCode.BadRequest:
                Log.Warning("Bad request: {Message}", ex.Message);
                break;
            default:
                Log.Error(ex, "Unhandled exception occurred");
                break;
        }

        context.Response.StatusCode  = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type    = $"https://httpstatuses.com/{(int)statusCode}",
            title,
            status  = (int)statusCode,
            detail  = ex.Message,
            traceId = context.TraceIdentifier
        };

        return context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }
}
