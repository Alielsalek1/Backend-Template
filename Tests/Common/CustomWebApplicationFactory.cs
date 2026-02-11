using Infrastructure.Persistance;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using Tests.Common.TestContainerDependencies;
using Tests.MailHog;

namespace Tests.Common;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private ContainerOrchestrator? _orchestrator;

    /// <summary>
    /// Initializes the test environment and starts the required containers.
    /// </summary>
    public async Task InitializeAsync()
    {
        // hardcode SEQ_URL for testing purposes
        TestEnvironment.SetSeqUrl();
        // hardcode JWT for testing purposes
        TestEnvironment.SetJwtEnvironmentVariables();

        // Set up test environment and start orchestrator (owns containers + providers)
        TestEnvironment.Configure();
        _orchestrator = new ContainerOrchestrator();
        await _orchestrator.StartAsync();
    }

    // Note: Database and Redis operations should be performed via the exposed providers

    // Expose providers so tests and helpers can operate on containers/resources
    public RedisProvider? RedisProvider => _orchestrator?.RedisProvider;
    public DatabaseProvider? DatabaseProvider => _orchestrator?.DatabaseProvider;
    public RespawnerProvider? RespawnerProvider => _orchestrator?.RespawnerProvider;
    public MailhogProvider? MailhogProvider => _orchestrator?.MailhogProvider;
    public RabbitMqProvider? RabbitMqProvider => _orchestrator?.RabbitMqProvider;
    public MailhogClient? MailhogClient => _orchestrator?.MailhogProvider?.CreateClient();

    public new async Task DisposeAsync()
    {
        if (_orchestrator != null) await _orchestrator.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            // allow ContainerOrchestrator's DatabaseProvider to replace DbContext registrations
            if (_orchestrator?.DatabaseProvider != null)
            {
                _orchestrator.DatabaseProvider.ReplaceDbContext(services);
            }
            else
            {
                DatabaseProvider.ReplaceDbContextWithEnvironment(services);
                // Add hosted service to migrate the database for fallback case
                services.AddHostedService<FallbackMigrationHostedService>();
            }

            // Register the orchestrator instance and a hosted service that will run
            // after the app's service provider is available to run migrations and init respawner.
            if (_orchestrator != null)
            {
                services.AddSingleton(_orchestrator);
                services.AddHostedService<OrchestratorHostedService>();
            }
        });
    }
}