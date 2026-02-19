using API.ActionFilters;
using Application.Constants;
using Application.Utils;
using Asp.Versioning;
using FluentValidation.AspNetCore;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;

namespace API;

public static class DependencyInjection
{
    public static IServiceCollection AddApiLayer(
        this IServiceCollection services,
        string jwtKey,
        string jwtIssuer,
        string jwtAudience,
        bool isDevelopment = false)
    {
        services.AddAuthenticationAndAuthorization(jwtKey, jwtIssuer, jwtAudience);
        services.AddControllersWithValidation();
        services.AddRateLimiting();
        services.AddApiVersioningConfiguration();
        services.AddActionFilters();
        services.ConfigureForwardedHeaders();
        if (isDevelopment)
        {
            services.AddSwaggerDocumentation();
        }

        return services;
    }

    private static IServiceCollection AddAuthenticationAndAuthorization(
        this IServiceCollection services,
        string jwtKey,
        string jwtIssuer,
        string jwtAudience)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256] // Strictly enforce the algorithm
            };
        });

        services.AddAuthorization();
        return services;
    }

    private static IServiceCollection AddControllersWithValidation(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            // authorize all endpoints by default, individual endpoints can override with [AllowAnonymous]
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            options.Filters.Add(new AuthorizeFilter(policy));

            options.Filters.Add<ValidationFilter>();
            
            // Request size limits to prevent DoS attacks
            options.MaxModelBindingCollectionSize = 1000; // Max items in a collection
        })
        .ConfigureApiBehaviorOptions(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        // Global request size limit (30MB default, 10MB for most APIs)
        services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB
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

    private static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Backend Template API",
                Version = "1.0",
                Description = "The Architect's Forge - A high-performance, resilient backend template with Clean Architecture",
                Contact = new OpenApiContact
                {
                    Name = "Development Team",
                    Email = "dev@example.com"
                },
                License = new OpenApiLicense
                {
                    Name = "MIT"
                }
            });

            // Add JWT Bearer token support in Swagger
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter your JWT token in the format: Bearer {token}"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] { }
                }
            });

            // Include XML comments if available
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (System.IO.File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }

    private static IServiceCollection ConfigureForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            
            // In a production environment, you should be more specific about known proxies.
            // For this template, we clear them to allow the Nginx container to be recognized.
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }
}
