using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using WorldCupBets.WebApi.Configuration;

namespace WorldCupBets.WebApi.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddWebApiServices(IConfiguration configuration)
        {
            _ = services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
            _ = services.Configure<GoogleOptions>(configuration.GetSection("Google"));
            _ = services.Configure<AuthOptions>(configuration.GetSection("Auth"));
            _ = services.AddWebApiTelemetry(configuration);

            var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
            jwtOptions.Validate();

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter a valid JWT bearer token."
                });

                options.AddSecurityRequirement(d => new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, d),
                        []
                    }
                });
            });
            services.AddHealthChecks();

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = true,
                        ValidateIssuer = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidAudience = jwtOptions.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
                options.AddPolicy("Bettor", policy => policy.RequireRole("Admin", "Bettor"));
            });

            return services;
        }

        private IServiceCollection AddWebApiTelemetry(IConfiguration configuration)
        {
            var serviceName = configuration["OTEL_SERVICE_NAME"] ?? "WorldCupBets.WebApi";
            var hasOtlpEndpoint = !string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

            services.AddLogging(logging =>
            {
                logging.AddOpenTelemetry(options =>
                {
                    options.IncludeFormattedMessage = true;
                    options.IncludeScopes = true;
                    options.ParseStateValues = true;
                    options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));

                    if (hasOtlpEndpoint)
                    {
                        options.AddOtlpExporter();
                    }
                });
            });

            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(serviceName))
                .WithTracing(tracing =>
                {
                    tracing.AddAspNetCoreInstrumentation();
                    tracing.AddHttpClientInstrumentation();

                    if (hasOtlpEndpoint)
                    {
                        tracing.AddOtlpExporter();
                    }
                })
                .WithMetrics(metrics =>
                {
                    metrics.AddAspNetCoreInstrumentation();
                    metrics.AddHttpClientInstrumentation();
                    metrics.AddRuntimeInstrumentation();

                    if (hasOtlpEndpoint)
                    {
                        metrics.AddOtlpExporter();
                    }
                });

            return services;
        }
    }
}
