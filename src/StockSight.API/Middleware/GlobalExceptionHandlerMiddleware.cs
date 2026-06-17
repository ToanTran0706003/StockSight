using System.Net;
using System.Text.Json;

namespace StockSight.API.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
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
        catch (UnauthorizedAccessException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.Unauthorized, "Unauthorized", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.BadRequest, "Invalid operation", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled request failed.");
            await WriteProblemAsync(context, HttpStatusCode.InternalServerError, "Unexpected error", "The request could not be completed.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, HttpStatusCode statusCode, string title, string detail)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            type = "https://tools.ietf.org/html/rfc9110",
            title,
            status = (int)statusCode,
            detail,
            traceId = context.TraceIdentifier
        }));
    }
}
