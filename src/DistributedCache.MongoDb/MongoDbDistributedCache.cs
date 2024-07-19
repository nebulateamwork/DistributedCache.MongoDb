using Microsoft.Extensions.Caching.Distributed;
using MongoDB.Driver;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DistributedCache.MongoDb;

public class MongoDbDistributedCache : IDistributedCache
{
    private MongoDbDistributedCacheOptions _mongoDbDistributedCacheOptions;
    private IMongoClient _mongoDbClient;
    private IMongoDatabase _db;
    private IMongoCollection<MongoDbCacheEntry> _collection;

    public MongoDbDistributedCache(
        MongoDbDistributedCacheOptions mongoDbDistributedCacheOptions)
    {
        _mongoDbDistributedCacheOptions = mongoDbDistributedCacheOptions;
        _mongoDbClient = mongoDbDistributedCacheOptions.CreateClient();

        _db = _mongoDbClient.GetDatabase(mongoDbDistributedCacheOptions.DatabaseName);
        _collection = _db.GetCollection<MongoDbCacheEntry>(mongoDbDistributedCacheOptions.CollectionNme);

        //Create expire indes so the index will automatically expire after time 
        //is reached
        _collection.Indexes.CreateOne(
            new CreateIndexModel<MongoDbCacheEntry>(
                Builders<MongoDbCacheEntry>.IndexKeys.Ascending(x => x.ExpiresAt),
                new CreateIndexOptions
                {
                    ExpireAfter = TimeSpan.Zero,
                    Name = "expiring-index"
                }));
    }

    public byte[]? Get(string key)
    {
        return _collection
            .Find(x => x.Id == key)
            .Project(x => x.Value)
            .FirstOrDefault();
    }

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        return _collection
            .Find(x => x.Id == key)
            .Project(x => x.Value)
            .FirstOrDefaultAsync(token);
    }

    public void Refresh(string key)
    {
        throw new NotImplementedException();
    }

    public Task RefreshAsync(string key, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public void Remove(string key)
    {
        _collection.DeleteOne(x => x.Id == key);
    }

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        return _collection.DeleteOneAsync(x => x.Id == key, token);
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var (expireAt, sliding) = GetExpirations(options);

        //proceed with an upsert
        _collection.FindOneAndUpdate(
            Builders<MongoDbCacheEntry>.Filter.Eq(x => x.Id, key),
            Builders<MongoDbCacheEntry>.Update
                .Set(x => x.Value, value)
                .Set(x => x.ExpiresAt, expireAt)
                .Set(x => x.SlidingExpiration, (long?) sliding),
            new FindOneAndUpdateOptions<MongoDbCacheEntry>()
            {
                IsUpsert = true
            });
    }

    private (DateTimeOffset ExpireAt, double? SlidingExpiration) GetExpirations(DistributedCacheEntryOptions options)
    {
        var expireAt = options?.AbsoluteExpiration;
        if (options?.AbsoluteExpirationRelativeToNow != null)
        {
            //this is another way to set absolute expiration
            expireAt = DateTimeOffset.UtcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);
        }

        //ok now expireAt is null if we have sliding expiration
        var slidingExpiration = options?.SlidingExpiration?.TotalMilliseconds;
        if (slidingExpiration != null)
        {
            expireAt = DateTimeOffset.UtcNow.AddMilliseconds(slidingExpiration.Value);
        }

        if (expireAt == null)
        {
            //Add a default expiration
            expireAt = DateTimeOffset.UtcNow.AddMinutes(_mongoDbDistributedCacheOptions.DefaultCacheDurationsInMinutes);
        }

        return (expireAt.Value, slidingExpiration);
    }

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var (expireAt, sliding) = GetExpirations(options);

        //proceed with an upsert
        return _collection.FindOneAndUpdateAsync(
            Builders<MongoDbCacheEntry>.Filter.Eq(x => x.Id, key),
            Builders<MongoDbCacheEntry>.Update
                .Set(x => x.Value, value)
                .Set(x => x.ExpiresAt, expireAt)
                .Set(x => x.SlidingExpiration, (long?)sliding),
            new FindOneAndUpdateOptions<MongoDbCacheEntry>()
            {
                IsUpsert = true
            });
    }
}
