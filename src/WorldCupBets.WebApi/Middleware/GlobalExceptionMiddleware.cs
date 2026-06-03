using System.Text.Json;
using WorldCupBets.Application.Abstractions;

namespace WorldCupBets.WebApi.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (PersistenceConflictException)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/problem+json";

            var payload = new
            {
                title = "Persistence conflict",
                status = StatusCodes.Status409Conflict,
                detail = "The requested operation conflicted with another concurrent update. Please retry."
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
        catch (Exception exception)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var payload = new
            {
                title = "Unhandled exception",
                status = StatusCodes.Status500InternalServerError,
                detail = context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
                    ? exception.Message
                    : "An unexpected server error occurred."
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
