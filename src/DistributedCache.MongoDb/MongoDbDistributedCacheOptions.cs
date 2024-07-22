using MongoDB.Driver;
using System;

namespace DistributedCache.MongoDb;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
public class MongoDbDistributedCacheOptions 
{
    /// <summary>
    /// Connection string used to connect to MongoDb, it is ignored
    /// if <see cref="MongoClientFactory"/> is set.
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    /// Name of the database
    /// </summary>
    public string DatabaseName { get; set; }

    /// <summary>
    /// Name of the collection used to store the cache entries
    /// </summary>
    public string CollectionName { get; set; } = "distributed-cache";

    /// <summary>
    /// To connect to Mongodb we can use the connection string or the
    /// caller can directly configure a factory that create Mongo client
    /// with the desired configuration. This is especially useful to create
    /// a single <see cref="IMongoClient"/> for the entire application.
    /// If this value is different from null, <see cref="ConnectionString"/>
    /// is not used anymore.
    /// </summary>
    public Func<IMongoClient> MongoClientFactory { get; set; }

    /// <summary>
    /// If not specified the absolute duration of the cache.
    /// </summary>
    public double DefaultCacheDurationsInMinutes { get; set; } = 30;

    internal IMongoClient CreateClient()
    {
        if (MongoClientFactory != null)
        {
            return MongoClientFactory();
        }

        if (ConnectionString == null)
        {
            throw new InvalidOperationException("ConnectionString or MongoClientFactory must be set");
        }

        return new MongoClient(ConnectionString);
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
