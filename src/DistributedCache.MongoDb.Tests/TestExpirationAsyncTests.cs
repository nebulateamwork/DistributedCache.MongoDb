using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace DistributedCache.MongoDb.Tests;

/// <summary>
/// Copied from the standard test used by redis implementation
/// in aspnetcore repository 
/// https://raw.githubusercontent.com/dotnet/aspnetcore/662d200bc4feb11a01895e675fbfee8517c6fe2a/src/Caching/StackExchangeRedis/test/TimeExpirationAsyncTests.cs
/// </summary>
public class TimeExpirationAsyncTests
{
    // async twin to ExceptionAssert.ThrowsArgumentOutOfRange
    static async Task ThrowsArgumentOutOfRangeAsync(Func<Task> test, string paramName, string message, object actualValue)
    {
        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(test);
        if (paramName is not null)
        {
            Assert.Equal(paramName, ex.ParamName);
        }
        if (message is not null)
        {
            Assert.StartsWith(message, ex.Message); // can have "\r\nParameter name:" etc
        }
        if (actualValue is not null)
        {
            Assert.Equal(actualValue, ex.ActualValue);
        }
    }

    [Fact]
    public async Task AbsoluteExpirationInThePastThrows()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = await GetNameAndReset(cache);
        var value = new byte[1];

        var expected = DateTimeOffset.Now - TimeSpan.FromMinutes(1);
        await ThrowsArgumentOutOfRangeAsync(
            async () =>
            {
                await cache.SetAsync(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(expected));
            },
            nameof(DistributedCacheEntryOptions.AbsoluteExpiration),
            "The absolute expiration value must be in the future.",
            expected);
    }

    [Fact]
    public async Task AbsoluteExpirationExpires()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = await GetNameAndReset(cache);
        var value = new byte[1];

        await cache.SetAsync(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(1)));

        byte[]? result = await cache.GetAsync(key);
        Assert.Equal(value, result);

        for (int i = 0; i < 4 && result != null; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            result = await cache.GetAsync(key);
        }

        Assert.Null(result);
    }

    [Fact]
    public async Task AbsoluteSubSecondExpirationExpiresImmediately()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = await GetNameAndReset(cache);
        var value = new byte[1];

        await cache.SetAsync(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(0.25)));

        var result = await cache.GetAsync(key);
        Assert.Null(result);
    }

    [Fact]
    public async Task NegativeRelativeExpirationThrows()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = await GetNameAndReset(cache);
        var value = new byte[1];

        await ThrowsArgumentOutOfRangeAsync(async () =>
        {
            await cache.SetAsync(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(-1)));
        },
        nameof(DistributedCacheEntryOptions.AbsoluteExpirationRelativeToNow),
        "The relative expiration value must be positive.",
        TimeSpan.FromMinutes(-1));
    }

    [Fact]
    public async Task ZeroRelativeExpirationThrows()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = await GetNameAndReset(cache);
        var value = new byte[1];

        await ThrowsArgumentOutOfRangeAsync(async () =>
        {
            await cache.SetAsync(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.Zero));
        },
            nameof(DistributedCacheEntryOptions.AbsoluteExpirationRelativeToNow),
            "The relative expiration value must be positive.",
            TimeSpan.Zero);
    }

    [Fact]
    public async Task RelativeExpirationExpires()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = await GetNameAndReset(cache);
        var value = new byte[1];

        await cache.SetAsync(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(1)));

        var result = await cache.GetAsync(key);
        Assert.Equal(value, result);

        for (int i = 0; i < 4 && result != null; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            result = await cache.GetAsync(key);
        }
        Assert.Null(result);
    }

    [Fact]
    public async Task RelativeSubSecondExpirationExpiresImmediately()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = await GetNameAndReset(cache);
        var value = new byte[1];

        await cache.SetAsync(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(0.25)));

        var result = await cache.GetAsync(key);
        Assert.Null(result);
    }

    [Fact]
    public async Task NegativeSlidingExpirationThrows()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = await GetNameAndReset(cache);
        var value = new byte[1];

        await ThrowsArgumentOutOfRangeAsync(async () =>
        {
            await cache.SetAsync(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(-1)));
        }, nameof(DistributedCacheEntryOptions.SlidingExpiration), "The sliding expiration value must be positive.", TimeSpan.FromMinutes(-1));
    }

    [Fact]
    public async Task ZeroSlidingExpirationThrows()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = await GetNameAndReset(cache);
        var value = new byte[1];

        await ThrowsArgumentOutOfRangeAsync(async () =>
        {
            await cache.SetAsync(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.Zero));
        },
        nameof(DistributedCacheEntryOptions.SlidingExpiration),
        "The sliding expiration value must be positive.",
        TimeSpan.Zero);
    }

    [Fact]
    public async Task SlidingExpirationExpiresIfNotAccessed()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = await GetNameAndReset(cache);
        var value = new byte[1];

        await cache.SetAsync(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(1)));

        var result = await cache.GetAsync(key);
        Assert.Equal(value, result);

        await Task.Delay(TimeSpan.FromSeconds(3.5));

        result = await cache.GetAsync(key);
        Assert.Null(result);
    }

    [Fact]
    public async Task SlidingSubSecondExpirationExpiresImmediately()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = await GetNameAndReset(cache);
        var value = new byte[1];

        await cache.SetAsync(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(0.25)));

        var result = await cache.GetAsync(key);
        Assert.Null(result);
    }

    [Fact]
    public async Task SlidingExpirationRenewedByAccess()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = await GetNameAndReset(cache);
        var value = new byte[1];

        await cache.SetAsync(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(1)));

        var result = await cache.GetAsync(key);
        Assert.Equal(value, result);

        for (int i = 0; i < 5; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(0.5));

            result = await cache.GetAsync(key);
            Assert.Equal(value, result);
        }

        await Task.Delay(TimeSpan.FromSeconds(3));
        result = await cache.GetAsync(key);
        Assert.Null(result);
    }

    [Fact]
    public async Task SlidingExpirationRenewedByAccessUntilAbsoluteExpiration()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = await GetNameAndReset(cache);
        var value = new byte[1];

        await cache.SetAsync(key, value, new DistributedCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromSeconds(1))
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(3)));

        var setTime = DateTime.Now;
        var result = await cache.GetAsync(key);
        Assert.Equal(value, result);

        for (int i = 0; i < 5; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(0.5));

            result = await cache.GetAsync(key);
            Assert.NotNull(result);
            Assert.Equal(value, result);
        }

        while ((DateTime.Now - setTime).TotalSeconds < 4)
        {
            await Task.Delay(TimeSpan.FromSeconds(0.5));
        }

        result = await cache.GetAsync(key);
        Assert.Null(result);
    }

    static async Task<string> GetNameAndReset(IDistributedCache cache, [CallerMemberName] string caller = "")
    {
        await cache.RemoveAsync(caller);
        return caller;
    }
}