using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Testcontainers.RabbitMq;
using Tests.MailHog;

namespace Tests.Common.TestContainerDependencies;

public class ContainerOrchestrator : IAsyncDisposable
{
    private PostgreSqlContainer? _dbContainer;
    private IContainer? _mailhogContainer;
    private RedisContainer? _redisContainer;
    private RabbitMqContainer? _rabbitMqContainer;

    private RedisProvider? _redisProvider;
    private MailhogProvider? _mailhogProvider;
    private DatabaseProvider? _databaseProvider;
    private RespawnerProvider? _respawnerProvider;
    private RabbitMqProvider? _rabbitMqProvider;

    public RedisProvider? RedisProvider => _redisProvider;
    public MailhogProvider? MailhogProvider => _mailhogProvider;
    public DatabaseProvider? DatabaseProvider => _databaseProvider;
    public RespawnerProvider? RespawnerProvider => _respawnerProvider;
    public RabbitMqProvider? RabbitMqProvider => _rabbitMqProvider;

    // Called after the application service provider is available. Ensures migrations are applied
    // and initializes the respawner used to reset the database between tests.
    public async Task InitializeRespawnerAsync(IServiceProvider services)
    {
        if (_databaseProvider == null) throw new InvalidOperationException("DatabaseProvider is not initialized.");

        // Ensure EF migrations have been applied using the app's service provider
        await _databaseProvider.EnsureDatabaseMigratedAsync(services);

        // Create and initialize respawner now that tables exist
        _respawnerProvider = new RespawnerProvider(_dbContainer!.GetConnectionString());
        await _respawnerProvider.InitializeAsync();
    }

    public async Task StartAsync()
    {
        // Create containers
        _dbContainer = ContainerFactory.CreatePostgreSqlContainer();
        _mailhogContainer = ContainerFactory.CreateMailhogContainer();
        _redisContainer = ContainerFactory.CreateRedisContainer();
        _rabbitMqContainer = ContainerFactory.CreateRabbitMqContainer();

        await Task.WhenAll(
            _dbContainer.StartAsync(),
            _mailhogContainer.StartAsync(),
            _redisContainer.StartAsync(),
            _rabbitMqContainer.StartAsync()
        );

        // Create providers that operate on containers/resources
        _mailhogProvider = new MailhogProvider(_mailhogContainer);
        _redisProvider = new RedisProvider(_redisContainer!);
        _databaseProvider = new DatabaseProvider(_dbContainer!.GetConnectionString());
        _respawnerProvider = new RespawnerProvider(_dbContainer.GetConnectionString());
        _rabbitMqProvider = new RabbitMqProvider(_rabbitMqContainer!);

        // Do NOT initialize respawner here — database migrations must run first
        // Respawner will be initialized later once the application services are available.

        // Configure environment variables for the application to use
        // (Keep environment wiring inside the orchestrator so factory remains focused)
        var smtpPort = _mailhogContainer.GetMappedPublicPort(1025);
        TestEnvironment.SetEmailEnvironmentVariables(smtpPort);

        TestEnvironment.SetDatabaseEnvironmentVariables(_dbContainer.GetConnectionString());

        TestEnvironment.SetRedisEnvironmentVariables(_redisContainer.GetConnectionString());

        TestEnvironment.SetRabbitMqEnvironmentVariables(_rabbitMqProvider.GetConnectionString());

        TestEnvironment.SetAspNetCoreEnvironment();

        TestEnvironment.SetSeqUrl();
    }

    public async ValueTask DisposeAsync()
    {
        var tasks = new List<Task>();
        if (_respawnerProvider != null) tasks.Add(_respawnerProvider.DisposeAsync().AsTask());
        if (_mailhogContainer != null) tasks.Add(_mailhogContainer.DisposeAsync().AsTask());
        if (_dbContainer != null) tasks.Add(_dbContainer.DisposeAsync().AsTask());
        if (_redisContainer != null) tasks.Add(_redisContainer.DisposeAsync().AsTask());
        if (_rabbitMqContainer != null) tasks.Add(_rabbitMqContainer.DisposeAsync().AsTask());
        await Task.WhenAll(tasks);
    }
}
