using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets<Program>(optional: true);

var dbPassword = builder.AddParameter(
    "db-password",
    GetRequiredConfiguration(builder.Configuration, "DB_PASSWORD"),
    publishValueAsDefault: false,
    secret: true);
var dbUsername = builder.AddParameter(
    "db-username",
    builder.Configuration["DB_USERNAME"] ?? "app",
    publishValueAsDefault: true,
    secret: false);
var jwtSecret = GetRequiredConfiguration(builder.Configuration, "JWT_SECRET");

var googleClientId = builder.Configuration["GOOGLE_CLIENT_ID"] ?? string.Empty;
var enableDevLogin = builder.Configuration["ENABLE_DEV_LOGIN"] ?? "true";

var postgres = builder.AddPostgres("postgres", password: dbPassword, userName: dbUsername, port: 5432)
    .WithDataVolume("pgdata");
var database = postgres.AddDatabase("worldcupbets");

var redis = builder.AddRedis("redis", port: 6379)
    .WithDataVolume("redisdata");

var api = builder.AddProject("api", "../WorldCupBets.WebApi/WorldCupBets.WebApi.csproj")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Jwt__Secret", jwtSecret)
    .WithEnvironment("Google__ClientId", googleClientId)
    .WithReference(database, connectionName: "DefaultConnection")
    .WithReference(redis, connectionName: "Redis")
    .WaitFor(database)
    .WaitFor(redis)
    .WithHttpEndpoint(port: 5000, name: "http")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

builder.AddNpmApp("frontend", "../../frontend")
    .WithEnvironment("ASPIRE_API_URL", api.GetEndpoint("http"))
    .WithEnvironment("GOOGLE_CLIENT_ID", googleClientId)
    .WithEnvironment("ENABLE_DEV_LOGIN", enableDevLogin)
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(port: 4200, env: "PORT", name: "http")
    .WithExternalHttpEndpoints();

builder.Build().Run();
return;

static string GetRequiredConfiguration(IConfiguration configuration, string key)
{
    return configuration[key]
        ?? throw new InvalidOperationException(
            $"Missing required configuration value '{key}'. Set it before starting the Aspire AppHost.");
}
