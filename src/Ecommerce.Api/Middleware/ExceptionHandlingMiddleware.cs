using System.Net;
using System.Text.Json;
using Ecommerce.Domain.Exceptions;

namespace Ecommerce.Api.Middleware;

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

            if (!context.Response.HasStarted &&
                (context.Response.StatusCode == StatusCodes.Status401Unauthorized ||
                 context.Response.StatusCode == StatusCodes.Status403Forbidden) &&
                context.Response.ContentLength is null or 0)
            {
                await WriteErrorAsync(context, (HttpStatusCode)context.Response.StatusCode,
                    context.Response.StatusCode == StatusCodes.Status401Unauthorized
                        ? "Authentication required. Include a valid 'Authorization: Bearer <token>' header."
                        : "You do not have permission to perform this action.");
            }
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var (status, title) = ex switch
        {
            NotFoundException => (HttpStatusCode.NotFound, ex.Message),
            DomainException => (HttpStatusCode.BadRequest, ex.Message),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "You are not allowed to perform this action."),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        if (status == HttpStatusCode.InternalServerError)
            _logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning("Request failed ({Status}): {Message}", (int)status, ex.Message);

        await WriteErrorAsync(context, status, title);
    }

    private async Task WriteErrorAsync(HttpContext context, HttpStatusCode status, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;

        var payload = JsonSerializer.Serialize(new
        {
            status = (int)status,
            error = message
        });

        await context.Response.WriteAsync(payload);
    }
}