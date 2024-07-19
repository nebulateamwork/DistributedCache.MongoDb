using Microsoft.Extensions.Caching.Distributed;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Linq;
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
        var bsonDoc = _collection
            .Find(x => x.Id == key && x.ExpiresAt > DateTimeOffset.UtcNow)
            .Project(
                Builders<MongoDbCacheEntry>.Projection
                    .Include("v")
                    .Include("s"))
            .FirstOrDefault();

        if (bsonDoc?.Names.Contains("s") == true
            && bsonDoc["s"] != BsonNull.Value)
        { 
            //We have sliding time in milliseconds
            var milliseconds = bsonDoc["s"].AsInt64;
            var newExpiration = DateTimeOffset.UtcNow.AddMilliseconds(milliseconds);
            _collection.UpdateOne(
                Builders<MongoDbCacheEntry>.Filter.Eq(x => x.Id, key),
                Builders<MongoDbCacheEntry>.Update.Set(x => x.ExpiresAt, newExpiration));
        }

        return bsonDoc?.Names.Contains("v") == true ? bsonDoc["v"].AsByteArray : null;
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        var bsonDoc = await _collection
                   .Find(x => x.Id == key && x.ExpiresAt > DateTimeOffset.UtcNow)
                   .Project(
                       Builders<MongoDbCacheEntry>.Projection
                           .Include("v")
                           .Include("s"))
                   .FirstOrDefaultAsync();

        if (bsonDoc?.Names.Contains("s") == true 
            && bsonDoc["s"] != BsonNull.Value)
        {
            //We have sliding time in milliseconds
            var milliseconds = bsonDoc["s"].AsInt64;
            var newExpiration = DateTimeOffset.UtcNow.AddMilliseconds(milliseconds);
            await _collection.UpdateOneAsync(
                Builders<MongoDbCacheEntry>.Filter.Eq(x => x.Id, key),
                Builders<MongoDbCacheEntry>.Update.Set(x => x.ExpiresAt, newExpiration));
        }

        return bsonDoc?.Names.Contains("v") == true ? bsonDoc["v"].AsByteArray : null;
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
        
        //rule1: subsecond expiration, cache should expire immediately
        if (expireAt == null)
        {
            return;
        }

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

    private (DateTimeOffset? ExpireAt, double? SlidingExpiration) GetExpirations(DistributedCacheEntryOptions options)
    {
        var now = DateTimeOffset.UtcNow;
        var expireAt = options?.AbsoluteExpiration;
        if (options?.AbsoluteExpirationRelativeToNow != null)
        {
            //this is another way to set absolute expiration
            expireAt = now.Add(options.AbsoluteExpirationRelativeToNow.Value);
        }

        //ok now expireAt is null if we have sliding expiration
        var slidingExpiration = options?.SlidingExpiration?.TotalMilliseconds;
        if (slidingExpiration != null)
        {
            expireAt = now.AddMilliseconds(slidingExpiration.Value);
        }

        if (expireAt == null)
        {
            //Add a default expiration
            expireAt = now.AddMinutes(_mongoDbDistributedCacheOptions.DefaultCacheDurationsInMinutes);
        }

        if (expireAt < now)
        {
            throw new ArgumentOutOfRangeException(
                "AbsoluteExpiration",
                expireAt,
                "The absolute expiration value must be in the future.");
        }

        //subsecond cache should be immediately expired
        if (expireAt < now.AddSeconds(1))
        {
            expireAt = DateTimeOffset.UtcNow;
        }

        return (expireAt, slidingExpiration);
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

        if (expireAt == null)
        { 
            return Task.CompletedTask;
        }

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
