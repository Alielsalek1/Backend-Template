using API.ActionFilters;
using API.Extensions;
using Application.Constants;
using Application.Utils;
using Asp.Versioning;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Options;
using Application.Common.Options;
using Infrastructure.Common.Options;

namespace API;

public static class DependencyInjection
{
    public static IServiceCollection AddApiLayer(
        this IServiceCollection services,
        bool isDevelopment = false)
    {
        services.AddAuthenticationAndAuthorization();
        services.AddControllersWithValidation();
        services.AddRateLimiting();
        services.AddApiVersioningConfiguration();
        services.AddActionFilters();
        services.ConfigureForwardedHeaders();
        services.AddHangfireConfiguration();
        
        if (isDevelopment)
        {
            services.AddOpenApiDocumentation();
        }

        return services;
    }

    private static IServiceCollection AddAuthenticationAndAuthorization(this IServiceCollection services)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            // We need to use a post-configure or similar to access IOptions during setup if we don't want to use sp here
            // but for simplicity in Program.cs we could just use a temporary sp or just bind it here
            var sp = services.BuildServiceProvider();
            var jwtOptions = sp.GetRequiredService<IOptions<JwtOptions>>().Value;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
            };
        });

        services.AddAuthorization();
        return services;
    }

    private static IServiceCollection AddControllersWithValidation(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            options.Filters.Add(new AuthorizeFilter(policy));
            options.Filters.Add<ValidationFilter>();
            options.MaxModelBindingCollectionSize = 1000;
        })
        .ConfigureApiBehaviorOptions(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
        });

        services.AddFluentValidationAutoValidation();
        return services;
    }

    private static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1)
                    });
            });

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var response = new FailApiResponse
                {
                    StatusCode = StatusCodes.Status429TooManyRequests,
                    Message = "Too many requests. Please try again later.",
                    Errors = [],
                    ErrorCode = ApiErrorCodes.RateLimitExceededCode,
                    TraceId = context.HttpContext.TraceIdentifier
                };

                await context.HttpContext.Response.WriteAsJsonAsync(response, token);
            };
        });
        return services;
    }

    private static IServiceCollection AddApiVersioningConfiguration(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });
        return services;
    }

    private static IServiceCollection AddActionFilters(this IServiceCollection services)
    {
        services.AddTransient<IdempotencyFilter>();
        return services;
    }

    private static IServiceCollection ConfigureForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    private static IServiceCollection AddHangfireConfiguration(this IServiceCollection services)
    {
        var sp = services.BuildServiceProvider();
        var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(dbOptions.ConnectionString)));

        services.AddHangfireServer();
        return services;
    }
}
