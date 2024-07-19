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
        return new MongoDbDistributedCache(new MongoDbDistributedCacheOptions
        {
            ConnectionString = "mongodb://admin:123456##@localhost:27017",
            DatabaseName = $"test-mongodb-cache-{cacheName}",
            CollectionNme = "test-cache",
        });
    }
}
