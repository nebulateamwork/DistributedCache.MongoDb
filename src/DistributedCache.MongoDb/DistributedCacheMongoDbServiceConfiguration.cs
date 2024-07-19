using System;
using DistributedCache.MongoDb;
using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up Redis distributed cache related services in an <see cref="IServiceCollection" />.
/// </summary>
public static class StackExchangeRedisCacheServiceCollectionExtensions
{
    /// <summary>
    /// Adds Redis distributed caching services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="setupAction">An <see cref="Action{RedisCacheOptions}"/> to configure the provided
    /// <see cref="RedisCacheOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddMongoDbDistributedCache(
        this IServiceCollection services, 
        Action<MongoDbDistributedCacheOptions> setupAction)
    {
        services.AddSingleton(setupAction);
        services.Add(
            ServiceDescriptor.Singleton<IDistributedCache, MongoDbDistributedCache>());

        return services;
    }
}