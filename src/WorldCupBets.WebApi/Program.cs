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

await app.ApplyDatabaseMigrationsAsync();
app.UseWebApiPipeline();
app.MapWebApiEndpoints();

app.Run();
