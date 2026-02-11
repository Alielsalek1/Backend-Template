using Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using MyBackendTemplate.API.Middlewares;
using DotNetEnv;
using Application.Services;
using System.Net.Mail;
using System.Net;
using Serilog;
using Microsoft.AspNetCore.RateLimiting;
using Asp.Versioning;
using System.Threading.RateLimiting;
using Application.Utils;
using Application.Constants;
using API.ActionFilters;
using MassTransit;
using Application.Services.Interfaces;
using Application.Services.Implementations;
using Application.Repositories.Interfaces;
using Infrastructure.Repositories.Implementations;
using FluentValidation;
using FluentValidation.AspNetCore;
using Application.Validators.InternalAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;

Env.Load("../../.env");  // Load .env from root

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
var loggerConfig = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day);
if (builder.Environment.IsDevelopment())
{
    loggerConfig = loggerConfig.WriteTo.Seq(Environment.GetEnvironmentVariable("SEQ_URL") ?? 
        throw new InvalidOperationException("SEQ_URL environment variable is not set."));
}
Log.Logger = loggerConfig.CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting web host");

    // 0. JWT Configuration
    var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? 
        throw new InvalidOperationException("JWT_KEY environment variable is not set.");
    var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? 
        throw new InvalidOperationException("JWT_ISSUER environment variable is not set.");
    var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? 
        throw new InvalidOperationException("JWT_AUDIENCE environment variable is not set.");

    builder.Services.AddAuthentication(options =>
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

    builder.Services.AddAuthorization();

    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });

    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestDtoValidator>();

    // Get connection string from environment variable with fallback for migrations
    var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? 
        throw new InvalidOperationException("CONNECTION_STRING environment variable is not set.");

    // naming convention for postgreSQL is snake_case
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString)
               .UseSnakeCaseNamingConvention()
               .UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Enable Built-in Retries
        // This automatically handles "Transient" errors (like network blips).
        // It will retry up to 6 times by default.
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
    }));

    // 1. Email SMTP Configuration
    var emailConfig = new Dictionary<string, string>
    {
        { "Host", Environment.GetEnvironmentVariable("EMAIL_HOST") ?? 
            throw new InvalidOperationException("EMAIL_HOST environment variable is not set.") },
        { "Port", Environment.GetEnvironmentVariable("EMAIL_PORT") ?? 
            throw new InvalidOperationException("EMAIL_PORT environment variable is not set.") },
        { "Username", Environment.GetEnvironmentVariable("EMAIL_USERNAME") ?? 
            throw new InvalidOperationException("EMAIL_USERNAME environment variable is not set.") },
        { "Password", Environment.GetEnvironmentVariable("EMAIL_PASSWORD") ?? 
            throw new InvalidOperationException("EMAIL_PASSWORD environment variable is not set.") },
        { "From", Environment.GetEnvironmentVariable("EMAIL_FROM") ?? 
            throw new InvalidOperationException("EMAIL_FROM environment variable is not set.") }
    };

    // 3. Register FluentEmail with these settings
    builder.Services
        .AddFluentEmail(emailConfig["From"])
        .AddSmtpSender(new SmtpClient(emailConfig["Host"], int.Parse(emailConfig["Port"]))
        {
            EnableSsl = !bool.TryParse(Environment.GetEnvironmentVariable("EMAIL_ENABLE_SSL"), out var enableSsl) || enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(
                emailConfig["Username"], 
                emailConfig["Password"]
            ),
            Timeout = 20000 // 20 seconds timeout
        });
    builder.Services.AddScoped<IEmailService, EmailService>();

    // 4. Redis Caching Configuration
    var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ?? 
        throw new InvalidOperationException("REDIS_CONNECTION_STRING environment variable is not set.");

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "MyBackendTemplate_";
    });

    // Register application services
    builder.Services.AddScoped<IInternalAuthService, InternalAuthService>();

    // 5. MassTransit Configuration
    builder.Services.AddMassTransit(x =>
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            var rabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
            var rabbitMqPort = Environment.GetEnvironmentVariable("RABBITMQ_PORT");
            var username = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? "guest";
            var password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest";

            cfg.Host(rabbitMqHost, ushort.TryParse(rabbitMqPort, out var port) ? port : (ushort)5672, "/", h =>
            {
                h.Username(username);
                h.Password(password);
            });

            cfg.ConfigureEndpoints(context);
        });
    });

    builder.Services.AddRateLimiter(options =>
    {
        // Define a global limiter
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            // Rate limit based on IP Address
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1)
                });
        });
        
        // Handle rate limit rejection
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

    builder.Services.AddApiVersioning(options =>
    {
        // 1. Default to v1.0 if the client doesn't specify one
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        
        // 2. Report supported versions in the "api-supported-versions" header
        options.ReportApiVersions = true;
        
        // 3. Tell .NET to look for the version in the URL Path
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    });

    builder.Services.AddTransient<IdempotencyFilter>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IUserConfirmationService, UserConfirmationService>();
    builder.Services.AddScoped<ITokenProvider, JwtTokenProvider>();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    // Only use rate limiter in non-test environments
    if (!app.Environment.IsEnvironment("Testing"))
    {
        app.UseRateLimiter();
    }

    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // make a healthcheck endpoint
    app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
    // make a healthcheck endpoints to test auth
    app.MapGet("/health/auth", [Authorize] () => Results.Ok(new { status = "Authenticated" }));

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { } // required for tests