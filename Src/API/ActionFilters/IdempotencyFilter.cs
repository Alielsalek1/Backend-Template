using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace API.ActionFilters;
public class IdempotencyFilter : IAsyncActionFilter
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<IdempotencyFilter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public IdempotencyFilter(IDistributedCache cache, ILogger<IdempotencyFilter> logger, IOptions<JsonOptions> jsonOptions)
    {
        _cache = cache;
        _logger = logger;
        _jsonOptions = jsonOptions.Value.JsonSerializerOptions;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        _logger.LogInformation("Executing IdempotencyFilter for {Path}", context.HttpContext.Request.Path);
        // 1. Check for Header
        if (!context.HttpContext.Request.Headers.TryGetValue("Idempotency-Key", out var keyValues) || string.IsNullOrWhiteSpace(keyValues.ToString()))
        {
            var response = new Application.Utils.FailApiResponse
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "Idempotency-Key header is missing or empty",
                ErrorCode = Application.Constants.ApiErrorCodes.ValidationErrorCode,
                TraceId = context.HttpContext.TraceIdentifier
            };
            context.Result = new BadRequestObjectResult(response);
            return;
        }
        
        string key = keyValues.ToString();
        string cacheKey = $"idempotency:{key}";

        // 2. Hash the Request Body (Safety Check)
        string requestHash = await ComputeBodyHashAsync(context.HttpContext.Request);

        // 3. Check Cache
        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedData))
        {
            var record = JsonSerializer.Deserialize<IdempotencyRecord>(cachedData, _jsonOptions);

            // VALIDATION: Ensure the key isn't being reused for a different request
            if (record.RequestHash != requestHash)
            {
                context.Result = new ConflictObjectResult(new { error = "Idempotency key reused for different request parameters" });
                return;
            }

            _logger.LogInformation("Idempotency key hit for {Path}", context.HttpContext.Request.Path);

            // SUCCESS: Return cached response immediately
            context.Result = new ObjectResult(record.ResponseBody) { StatusCode = record.StatusCode };
            return;
        }

        _logger.LogInformation("Idempotency key miss for {Path}. Executing action.", context.HttpContext.Request.Path);

        // 4. Execute Controller
        var executedContext = await next();

        // 5. Cache the Result (Only if successful)
        if (executedContext.Result is ObjectResult result && result.StatusCode >= 200 && result.StatusCode < 300)
        {
            var record = new IdempotencyRecord
            {
                RequestHash = requestHash,
                StatusCode = result.StatusCode ?? 200,
                ResponseBody = result.Value
            };

            await _cache.SetStringAsync(
                cacheKey, 
                JsonSerializer.Serialize(record, _jsonOptions), 
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) }
            );
        }
    }

    private async Task<string> ComputeBodyHashAsync(HttpRequest request)
    {
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(body)));
    }

    private class IdempotencyRecord
    {
        public string RequestHash { get; set; }
        public int StatusCode { get; set; }
        public object ResponseBody { get; set; }
    }
}