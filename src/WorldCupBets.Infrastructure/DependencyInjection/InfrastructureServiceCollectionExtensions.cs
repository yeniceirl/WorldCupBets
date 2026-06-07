using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Application.Features.FootballData;
using WorldCupBets.Domain.Repositories;
using WorldCupBets.Infrastructure.Authentication;
using WorldCupBets.Infrastructure.Caching;
using WorldCupBets.Infrastructure.ExternalFootball;
using WorldCupBets.Infrastructure.Messaging;
using WorldCupBets.Infrastructure.Persistence;
using WorldCupBets.Infrastructure.Persistence.Repositories;

namespace WorldCupBets.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddCaching(configuration);
        services.AddExternalFootballData(configuration);
        services.AddAuthenticationAdapters();
        services.AddMessaging(configuration);
        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=worldcupbets;Username=app;Password=placeholder";

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IApplicationTransactionFactory, EfApplicationTransactionFactory>();
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<IMatchBetRepository, MatchBetRepository>();
        services.AddScoped<ITournamentPickRepository, TournamentPickRepository>();
        services.AddScoped<ITournamentSettlementRepository, TournamentSettlementRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserInvitationRepository, UserInvitationRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IExternalFootballDataRepository, ExternalFootballDataRepository>();
        services.AddScoped<IExternalFootballPlayerRepository, ExternalFootballPlayerRepository>();
        return services;
    }

    private static IServiceCollection AddExternalFootballData(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("ExternalFootballData");
        var options = new ExternalFootballDataOptions
        {
            Provider = section["Provider"] ?? "worldcup26",
            BaseUrl = section["BaseUrl"] ?? "https://worldcup26.ir"
        };
        var apiSportsSection = configuration.GetSection("ApiSportsFootball");
        var includedTeamNames = apiSportsSection.GetSection("IncludedTeamNames")
            .GetChildren()
            .Select(section => section.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();
        var apiSportsOptions = new ApiSportsFootballOptions
        {
            ApiKey = apiSportsSection["ApiKey"] ?? string.Empty,
            BaseUrl = apiSportsSection["BaseUrl"] ?? "https://v3.football.api-sports.io",
            IncludedTeamNames = includedTeamNames.Length > 0
                ? new HashSet<string>(includedTeamNames, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(ApiSportsFootballOptions.DefaultIncludedTeamNames, StringComparer.OrdinalIgnoreCase),
        };

        services.AddSingleton(options);
        services.AddSingleton(apiSportsOptions);
        services.AddSingleton(new ApiSportsFootballSyncOptions(apiSportsOptions.ApiKey, apiSportsOptions.IncludedTeamNames));
        services.AddSingleton<IFootballDataProvider>(serviceProvider =>
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(options.BaseUrl)
            };

            return new WorldCup26FootballDataProvider(httpClient, serviceProvider.GetRequiredService<ExternalFootballDataOptions>());
        });
        services.AddScoped<IPlayerSearchProvider>(serviceProvider =>
        {
            if (string.IsNullOrWhiteSpace(apiSportsOptions.ApiKey))
            {
                return new EmptyPlayerSearchProvider();
            }

            return new ApiSportsFootballPlayerSearchProvider(
                serviceProvider.GetRequiredService<IExternalFootballPlayerRepository>());
        });
        services.AddScoped<IPlayerSquadProvider>(_ => new ApiSportsPlayerSquadProvider(
            new HttpClient { BaseAddress = new Uri(apiSportsOptions.BaseUrl) },
            apiSportsOptions));

        return services;
    }

    private static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? string.Empty;

        services.AddHybridCache();
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
        });
        services.AddScoped<ILookupCacheService, LookupCacheService>();
        services.AddSingleton(new RedisTransportOptions
        {
            ConnectionString = redisConnectionString
        });
        return services;
    }

    private static IServiceCollection AddAuthenticationAdapters(this IServiceCollection services)
    {
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        return services;
    }

    private static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(new RedisConnectionFactory(new RedisTransportOptions
        {
            ConnectionString = configuration.GetConnectionString("Redis") ?? string.Empty
        }));
        return services;
    }
}
