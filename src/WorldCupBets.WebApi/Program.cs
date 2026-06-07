using JasperFx.CodeGeneration.Model;
using WorldCupBets.Application.DependencyInjection;
using WorldCupBets.Application.Features.Auth;
using WorldCupBets.Infrastructure.DependencyInjection;
using WorldCupBets.WebApi.Extensions;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWolverine(options =>
{
    options.ServiceLocationPolicy = ServiceLocationPolicy.AllowedButWarn;
    options.Discovery.IncludeAssembly(typeof(GoogleLoginHandler).Assembly);
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddWebApiServices(builder.Configuration);

var app = builder.Build();

// TEMP DIAGNOSTIC — remove after confirming env var propagation. Logs variable NAMES only, never values
// (these logs may be visible in shared dashboards — printing secret values would leak them).
var envVarNames = Environment.GetEnvironmentVariables().Keys.Cast<string>().OrderBy(name => name);
app.Logger.LogInformation(
    "config-check env={Environment} ApiSportsFootball:ApiKey present={Present} length={Length}",
    app.Environment.EnvironmentName,
    !string.IsNullOrWhiteSpace(app.Configuration["ApiSportsFootball:ApiKey"]),
    app.Configuration["ApiSportsFootball:ApiKey"]?.Length ?? 0);
app.Logger.LogInformation("config-check env var names visible to the process: {Names}", string.Join(", ", envVarNames));

await app.ApplyDatabaseMigrationsAsync();
app.UseWebApiPipeline();
app.MapWebApiEndpoints();

app.Run();
