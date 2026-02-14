using Application.Services;
using Application.Services.Interfaces;
using Application.Services.Implementations;
using Application.Validators.Auth;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Mail;
using System.Net;
using MassTransit;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        Dictionary<string, string> emailConfig,
        string redisConnectionString,
        string rabbitMqHost,
        string rabbitMqPort,
        string rabbitMqUsername,
        string rabbitMqPassword)
    {
        services.AddValidation();
        services.AddEmailServices(emailConfig);
        services.AddCaching(redisConnectionString);
        services.AddApplicationServices();
        services.AddMessageBroker(rabbitMqHost, rabbitMqPort, rabbitMqUsername, rabbitMqPassword);

        return services;
    }

    private static IServiceCollection AddValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterRequestDtoValidator>();
        return services;
    }

    private static IServiceCollection AddEmailServices(this IServiceCollection services, Dictionary<string, string> emailConfig)
    {
        services
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
                Timeout = 20000
            });
        services.AddScoped<IEmailService, EmailService>();
        return services;
    }

    private static IServiceCollection AddCaching(this IServiceCollection services, string redisConnectionString)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "MyBackendTemplate_";
        });
        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Cache Services
        services.AddScoped<ConfirmationTokenCacheService>();

        // Auth Services
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IInternalAuthService, InternalAuthService>();
        services.AddScoped<IInternalAuthFacadeService, InternalAuthFacadeService>();
        services.AddScoped<IUserConfirmationService, UserConfirmationService>();
        services.AddScoped<IJwtTokenProvider, JwtTokenProvider>();
        services.AddScoped<IGoogleAuthValidator, GoogleAuthValidator>();
        services.AddScoped<IExternalAuthService, ExternalAuthService>();

        // User Services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserFacadeService, UserFacadeService>();

        return services;
    }

    private static IServiceCollection AddMessageBroker(
        this IServiceCollection services,
        string rabbitMqHost,
        string rabbitMqPort,
        string rabbitMqUsername,
        string rabbitMqPassword)
    {
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqHost, ushort.TryParse(rabbitMqPort, out var port) ? port : (ushort)5672, "/", h =>
                {
                    h.Username(rabbitMqUsername);
                    h.Password(rabbitMqPassword);
                });

                cfg.ConfigureEndpoints(context);
            });
        });
        return services;
    }
}
