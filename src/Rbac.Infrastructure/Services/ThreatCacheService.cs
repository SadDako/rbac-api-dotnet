using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Rbac.Application.Interfaces;
using Rbac.Application.Security;
using StackExchange.Redis;

namespace Rbac.Infrastructure.Services;

public sealed class ThreatCacheService : IThreatCacheService
{
    private static readonly ConcurrentDictionary<string, DateTime> LocalLocks = new();
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer? _redis;

    public ThreatCacheService(IDistributedCache cache, IServiceProvider serviceProvider)
    {
        _cache = cache;
        _redis = serviceProvider.GetService<IConnectionMultiplexer>();
    }

    public async Task<ThreatCounterResult> IncrementCounterAsync(string key, TimeSpan window, long limit, CancellationToken cancellationToken)
    {
        key = NormalizeKey(key);
        if (_redis is not null)
        {
            var db = _redis.GetDatabase();
            var count = await db.StringIncrementAsync(key);
            if (count == 1)
            {
                await db.KeyExpireAsync(key, window);
            }

            var ttl = await db.KeyTimeToLiveAsync(key) ?? window;
            return new ThreatCounterResult { Count = count, IsLimitExceeded = count >= limit, Ttl = ttl };
        }

        var current = await _cache.GetStringAsync(key, cancellationToken);
        var parsed = CounterEnvelope.Parse(current, window);
        parsed.Count += 1;
        await _cache.SetStringAsync(key, parsed.Serialize(), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = parsed.ExpiresAtUtc - DateTime.UtcNow
        }, cancellationToken);

        return new ThreatCounterResult
        {
            Count = parsed.Count,
            IsLimitExceeded = parsed.Count >= limit,
            Ttl = parsed.ExpiresAtUtc - DateTime.UtcNow
        };
    }

    public Task ResetCounterAsync(string key, CancellationToken cancellationToken)
    {
        return RemoveAsync(key, cancellationToken);
    }

    public async Task SetFlagAsync(string key, TimeSpan ttl, CancellationToken cancellationToken)
    {
        await _cache.SetStringAsync(NormalizeKey(key), "1", new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl,
            SlidingExpiration = ttl > TimeSpan.FromMinutes(5) ? TimeSpan.FromMinutes(5) : null
        }, cancellationToken);
    }

    public async Task<bool> IsFlaggedAsync(string key, CancellationToken cancellationToken)
    {
        return await _cache.GetStringAsync(NormalizeKey(key), cancellationToken) is not null;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(NormalizeKey(key), cancellationToken);
    }

    public async Task<IAsyncDisposable?> TryAcquireLockAsync(string key, TimeSpan ttl, CancellationToken cancellationToken)
    {
        key = NormalizeKey($"lock:{key}");
        var token = Guid.NewGuid().ToString("N");
        if (_redis is not null)
        {
            var db = _redis.GetDatabase();
            var acquired = await db.StringSetAsync(key, token, ttl, When.NotExists);
            return acquired ? new RedisLock(db, key, token) : null;
        }

        var now = DateTime.UtcNow;
        var expiresAt = now.Add(ttl);
        var acquiredLocal = LocalLocks.AddOrUpdate(key, expiresAt, (_, current) => current <= now ? expiresAt : current) == expiresAt;
        return acquiredLocal ? new LocalLock(key) : null;
    }

    private static string NormalizeKey(string key)
    {
        return $"threat:{key.Trim().ToLowerInvariant()}";
    }

    private sealed class RedisLock : IAsyncDisposable
    {
        private readonly IDatabase _database;
        private readonly RedisKey _key;
        private readonly RedisValue _token;

        public RedisLock(IDatabase database, string key, string token)
        {
            _database = database;
            _key = key;
            _token = token;
        }

        public async ValueTask DisposeAsync()
        {
            const string script = "if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('DEL', KEYS[1]) else return 0 end";
            await _database.ScriptEvaluateAsync(script, new[] { _key }, new[] { _token });
        }
    }

    private sealed class LocalLock : IAsyncDisposable
    {
        private readonly string _key;

        public LocalLock(string key)
        {
            _key = key;
        }

        public ValueTask DisposeAsync()
        {
            LocalLocks.TryRemove(_key, out _);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CounterEnvelope
    {
        public long Count { get; set; }
        public DateTime ExpiresAtUtc { get; set; }

        public static CounterEnvelope Parse(string? serialized, TimeSpan window)
        {
            if (!string.IsNullOrWhiteSpace(serialized))
            {
                try
                {
                    var parsed = System.Text.Json.JsonSerializer.Deserialize<CounterEnvelope>(serialized);
                    if (parsed is not null && parsed.ExpiresAtUtc > DateTime.UtcNow)
                    {
                        return parsed;
                    }
                }
                catch
                {
                    // fall through and create a new counter window
                }
            }

            return new CounterEnvelope { Count = 0, ExpiresAtUtc = DateTime.UtcNow.Add(window) };
        }

        public string Serialize()
        {
            return System.Text.Json.JsonSerializer.Serialize(this);
        }
    }
}
