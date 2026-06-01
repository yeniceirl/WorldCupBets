using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Domain.Repositories;
using WorldCupBets.Infrastructure.Authentication;
using WorldCupBets.Infrastructure.Caching;
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
        services.AddAuthenticationAdapters();
        services.AddMessaging(configuration);
        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=worldcupbets;Username=app;Password=placeholder";

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<IMatchBetRepository, MatchBetRepository>();
        services.AddScoped<IChampionBetRepository, ChampionBetRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
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
