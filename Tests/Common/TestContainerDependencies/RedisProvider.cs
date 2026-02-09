using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;
using Testcontainers.Redis;

namespace Tests.Common.TestContainerDependencies;

public class RedisProvider
{
    private readonly RedisContainer _container;

    public RedisProvider(RedisContainer container)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
    }

    public async Task FlushAllAsync()
    {
        await _container.ExecAsync(new[] { "redis-cli", "FLUSHALL" });
    }

    public async Task<string?> GetValueAsync(string key)
    {
        var result = await _container.ExecAsync(new[] { "redis-cli", "GET", key });
        var output = result.Stdout.Trim();
        return string.IsNullOrEmpty(output) || output == "(nil)" ? null : output;
    }

    public async Task<long> GetTTLAsync(string key)
    {
        var result = await _container.ExecAsync(new[] { "redis-cli", "TTL", key });
        if (long.TryParse(result.Stdout.Trim(), out var ttl))
        {
            return ttl;
        }
        return -2;
    }

    public async Task SetValueAsync(string key, string value, int ttlSeconds)
    {
        await _container.ExecAsync(new[] { "redis-cli", "SET", key, value, "EX", ttlSeconds.ToString() });
    }
}
