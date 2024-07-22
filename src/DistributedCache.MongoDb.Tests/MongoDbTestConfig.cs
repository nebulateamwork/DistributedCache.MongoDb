using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedCache.MongoDb.Tests;

internal static class MongoDbTestConfig
{
    public static MongoDbDistributedCache CreateCacheInstance(string cacheName) 
    {
        var envConnectionString = Environment.GetEnvironmentVariable("MONGODB_TEST_CONNECTION_STRING");
        
        if (string.IsNullOrWhiteSpace(envConnectionString))
        {
            envConnectionString = "mongodb://admin:123456##@localhost:27017";
        }

        return new MongoDbDistributedCache(new MongoDbDistributedCacheOptions
        {
            ConnectionString = envConnectionString,
            DatabaseName = $"test-{cacheName}",
            CollectionName = "test-cache",
        });
    }
}
