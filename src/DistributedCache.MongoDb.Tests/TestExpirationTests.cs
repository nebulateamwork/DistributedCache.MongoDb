using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Runtime.CompilerServices;

namespace DistributedCache.MongoDb.Tests;

/// <summary>
/// Copied from the standard test used by redis implementation
/// in aspnetcore repository 
/// https://raw.githubusercontent.com/dotnet/aspnetcore/662d200bc4feb11a01895e675fbfee8517c6fe2a/src/Caching/StackExchangeRedis/test/TimeExpirationTests.cs
/// </summary>
public class TimeExpirationTests
{
    // async twin to ExceptionAssert.ThrowsArgumentOutOfRange
    static void ThrowsArgumentOutOfRange(Action test, string paramName, string message, object actualValue)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(test);
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
    public void AbsoluteExpirationInThePastThrows()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        var expected = DateTimeOffset.Now - TimeSpan.FromMinutes(1);
            ThrowsArgumentOutOfRange(
            () =>
            {
                cache.Set(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(expected));
            },
            nameof(DistributedCacheEntryOptions.AbsoluteExpiration),
            "The absolute expiration value must be in the future.",
            expected);
    }

    [Fact]
    public void AbsoluteExpirationExpires()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        cache.Set(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(1)));

        byte[] result = cache.Get(key);
        Assert.Equal(value, result);

        for (int i = 0; i < 4 && result != null; i++)
        {
            Thread.Sleep(TimeSpan.FromSeconds(0.5));
            result = cache.Get(key);
        }

        Assert.Null(result);
    }

    [Fact]
    public void AbsoluteSubSecondExpirationExpiresImmediately()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        cache.Set(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(0.25)));

        var result = cache.Get(key);
        Assert.Null(result);
    }

    [Fact]
    public void NegativeRelativeExpirationThrows()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        ThrowsArgumentOutOfRange(() =>
        {
            cache.Set(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(-1)));
        },
        nameof(DistributedCacheEntryOptions.AbsoluteExpirationRelativeToNow),
        "The relative expiration value must be positive.",
        TimeSpan.FromMinutes(-1));
    }

    [Fact]
    public void ZeroRelativeExpirationThrows()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

           ThrowsArgumentOutOfRange(
            () =>
            {
                cache.Set(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.Zero));
            },
            nameof(DistributedCacheEntryOptions.AbsoluteExpirationRelativeToNow),
            "The relative expiration value must be positive.",
            TimeSpan.Zero);
    }

    [Fact]
    public void RelativeExpirationExpires()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        cache.Set(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(1)));

        var result = cache.Get(key);
        Assert.Equal(value, result);

        for (int i = 0; i < 4 && result != null; i++)
        {
            Thread.Sleep(TimeSpan.FromSeconds(0.5));
            result = cache.Get(key);
        }
        Assert.Null(result);
    }

    [Fact]
    public void RelativeSubSecondExpirationExpiresImmediately()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        cache.Set(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(0.25)));

        var result = cache.Get(key);
        Assert.Null(result);
    }

    [Fact]
    public void NegativeSlidingExpirationThrows()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        ThrowsArgumentOutOfRange(() =>
        {
            cache.Set(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(-1)));
        }, nameof(DistributedCacheEntryOptions.SlidingExpiration), "The sliding expiration value must be positive.", TimeSpan.FromMinutes(-1));
    }

    [Fact]
    public void ZeroSlidingExpirationThrows()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        ThrowsArgumentOutOfRange(
            () =>
            {
                cache.Set(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.Zero));
            },
            nameof(DistributedCacheEntryOptions.SlidingExpiration),
            "The sliding expiration value must be positive.",
            TimeSpan.Zero);
    }

    [Fact]
    public void SlidingExpirationExpiresIfNotAccessed()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        cache.Set(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(1)));

        var result = cache.Get(key);
        Assert.Equal(value, result);

        Thread.Sleep(TimeSpan.FromSeconds(3.5));

        result = cache.Get(key);
        Assert.Null(result);
    }

    [Fact]
    public void SlidingSubSecondExpirationExpiresImmediately()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        cache.Set(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(0.25)));

        var result = cache.Get(key);
        Assert.Null(result);
    }

    [Fact]
    public void SlidingExpirationRenewedByAccess()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        cache.Set(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(1)));

        var result = cache.Get(key);
        Assert.Equal(value, result);

        for (int i = 0; i < 5; i++)
        {
            Thread.Sleep(TimeSpan.FromSeconds(0.5));

            result = cache.Get(key);
            Assert.Equal(value, result);
        }

        Thread.Sleep(TimeSpan.FromSeconds(3));
        result = cache.Get(key);
        Assert.Null(result);
    }

    [Fact]
    public void SlidingExpirationRenewedByRefresh()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        cache.Set(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(1)));

        var result = cache.Get(key);
        Assert.Equal(value, result);

        for (int i = 0; i < 5; i++)
        {
            Thread.Sleep(TimeSpan.FromSeconds(0.5));

            cache.Refresh(key);
        }

        result = cache.Get(key);
        Assert.NotNull(result);

        Thread.Sleep(TimeSpan.FromSeconds(3));
        result = cache.Get(key);
        Assert.Null(result);
    }

    [Fact]
    public void SlidingExpirationRenewedByAccessUntilAbsoluteExpiration()
    {
        var cache = MongoDbTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        cache.Set(key, value, new DistributedCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromSeconds(1))
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(3)));

        var setTime = DateTime.Now;
        var result = cache.Get(key);
        Assert.Equal(value, result);

        for (int i = 0; i < 5; i++)
        {
            Thread.Sleep(TimeSpan.FromSeconds(0.5));

            result = cache.Get(key);
            Assert.NotNull(result);
            Assert.Equal(value, result);
        }

        while ((DateTime.Now - setTime).TotalSeconds < 4)
        {
            Thread.Sleep(TimeSpan.FromSeconds(0.5));
        }

        result = cache.Get(key);
        Assert.Null(result);
    }

    static string GetNameAndReset(IDistributedCache cache, [CallerMemberName] string caller = "")
    {
        cache.Remove(caller);
        return caller;
    }
}