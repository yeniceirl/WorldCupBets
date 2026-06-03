using Microsoft.EntityFrameworkCore;
using WorldCupBets.Infrastructure.Persistence;
using WorldCupBets.WebApi.Endpoints;
using WorldCupBets.WebApi.Middleware;

namespace WorldCupBets.WebApi.Extensions;

public static class WebApplicationExtensions
{
    public static async Task<WebApplication> ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
        return app;
    }

    public static WebApplication UseWebApiPipeline(this WebApplication app)
    {
        app.UseMiddleware<GlobalExceptionMiddleware>();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    public static WebApplication MapWebApiEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health").AllowAnonymous();
        app.MapAuthEndpoints();
        app.MapMeEndpoints();
        app.MapMatchesEndpoints();
        app.MapBetsEndpoints();
        app.MapLeaderboardEndpoints();
        app.MapFootballDataEndpoints();
        return app;
    }
}
