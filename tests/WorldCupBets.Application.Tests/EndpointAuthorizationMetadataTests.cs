using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Routing;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Domain.Repositories;
using Wolverine;
using WorldCupBets.WebApi.Extensions;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class EndpointAuthorizationMetadataTests
{
    private static readonly string[] AdminRoutePatterns =
    [
        "/api/matches/{id:int}/result",
        "/api/bets/champion/settlement",
        "/api/challenges/{id:int}/settlement",
        "/api/challenges/{id:int}/void",
        "/api/challenges/{id:int}/expire",
        "/api/admin/audit/balances",
        "/api/admin/audit/users/{userId:int}",
        "/api/football-data/sync",
        "/api/football-data/players/sync",
        "/api/football-data/fixtures/group-stage/import"
    ];

    private static readonly string[] AnonymousApiRoutePatterns =
    [
        "/api/auth/google",
        "/api/auth/dev-login"
    ];

    [Fact]
    public async Task Admin_Endpoints_Require_Admin_Policy()
    {
        var endpointsByRoute = (await CreateApiEndpointsAsync())
            .GroupBy(endpoint => endpoint.RoutePattern, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        foreach (var routePattern in AdminRoutePatterns)
        {
            Assert.True(
                endpointsByRoute.TryGetValue(routePattern, out var endpoints),
                $"Endpoint '{routePattern}' was not mapped. Available routes: {string.Join(", ", endpointsByRoute.Keys.OrderBy(route => route, StringComparer.Ordinal))}");
            Assert.Contains(endpoints, endpoint => endpoint.AuthorizeData.Any(authorizeData => string.Equals(authorizeData.Policy, "Admin", StringComparison.Ordinal)));
        }
    }

    [Fact]
    public async Task Api_Endpoints_Require_Authorization_Unless_Explicitly_Anonymous()
    {
        foreach (var endpoint in (await CreateApiEndpointsAsync()).Where(endpoint => endpoint.RoutePattern.StartsWith("/api/", StringComparison.Ordinal)))
        {
            if (AnonymousApiRoutePatterns.Contains(endpoint.RoutePattern, StringComparer.Ordinal))
            {
                Assert.True(endpoint.AllowAnonymous, $"Endpoint '{endpoint.RoutePattern}' must explicitly allow anonymous access.");
                continue;
            }

            Assert.False(endpoint.AllowAnonymous, $"Endpoint '{endpoint.RoutePattern}' unexpectedly allows anonymous access.");
            Assert.NotEmpty(endpoint.AuthorizeData);
        }
    }

    private static async Task<IReadOnlyList<ApiEndpointMetadata>> CreateApiEndpointsAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.Host.UseWolverine();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddHealthChecks();
        builder.Services.AddScoped<IUserRepository>(_ => throw new NotSupportedException("Endpoint metadata tests do not execute handlers."));
        builder.Services.AddScoped<IUserInvitationRepository>(_ => throw new NotSupportedException("Endpoint metadata tests do not execute handlers."));
        builder.Services.AddScoped<IRoleRepository>(_ => throw new NotSupportedException("Endpoint metadata tests do not execute handlers."));
        builder.Services.AddScoped<IMatchChallengeRepository>(_ => throw new NotSupportedException("Endpoint metadata tests do not execute handlers."));
        builder.Services.AddScoped<IJwtTokenGenerator>(_ => throw new NotSupportedException("Endpoint metadata tests do not execute handlers."));
        var app = builder.Build();

        app.MapWebApiEndpoints();

        await app.StartAsync();
        await app.StopAsync();

        return app.Services.GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Select(endpoint => new ApiEndpointMetadata(
                NormalizeRoutePattern(endpoint.RoutePattern.RawText ?? string.Empty),
                endpoint.Metadata.OfType<IAuthorizeData>().ToArray(),
                endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null))
            .ToArray();
    }

    private static string NormalizeRoutePattern(string routePattern)
    {
        return routePattern.StartsWith("/", StringComparison.Ordinal)
            ? routePattern
            : $"/{routePattern}";
    }

    private sealed record ApiEndpointMetadata(
        string RoutePattern,
        IReadOnlyList<IAuthorizeData> AuthorizeData,
        bool AllowAnonymous);
}
