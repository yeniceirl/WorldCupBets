using WorldCupBets.Application.DependencyInjection;
using WorldCupBets.Infrastructure.DependencyInjection;
using WorldCupBets.WebApi.Extensions;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWolverine();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddWebApiServices(builder.Configuration);

var app = builder.Build();

await app.ApplyDatabaseMigrationsAsync();
app.UseWebApiPipeline();
app.MapWebApiEndpoints();

app.Run();
