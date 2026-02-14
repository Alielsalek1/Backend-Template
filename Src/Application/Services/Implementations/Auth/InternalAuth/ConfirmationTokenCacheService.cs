using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services.Implementations;

public class ConfirmationTokenCacheService(IDistributedCache cache)
{
    private readonly IDistributedCache _cache = cache;

    public static string GenerateRandomToken()
    {
        return new Random().Next(100000, 999999).ToString();
    }

    public async Task SetTokenAsync(string email, string token, CancellationToken cancellationToken)
    {
        await _cache.SetStringAsync(email, token, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        }, cancellationToken);
    }

    public async Task<string?> GetTokenAsync(string email, CancellationToken cancellationToken)
    {
        return await _cache.GetStringAsync(email, cancellationToken);
    }

    public async Task DeleteTokenAsync(string email, CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(email, cancellationToken);
    }
}
