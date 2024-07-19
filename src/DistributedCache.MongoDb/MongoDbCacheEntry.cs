using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace DistributedCache.MongoDb;

/// <summary>
/// Class that is persisted on the database to store an entry
/// </summary>
internal class MongoDbCacheEntry
{
    /// <summary>
    /// Simple constructor to create a minimal version of cache object
    /// </summary>
    public MongoDbCacheEntry(string id, byte[] value)
    {
        Id = id;
        Value = value;
    }

    /// <summary>
    /// This is the key of the entry that can clearly be used
    /// as _id field in mongodb
    /// </summary>
    public string Id { get; private set; }

    /// <summary>
    /// We will use short name in documents to avoid wasting space
    /// </summary>
    [BsonElement("v")]
    public byte[]? Value { get; private set; }

    /// <summary>
    /// This is the expiration time of the entry, if present and not
    /// null the entry is considered expired and mongodb will remove
    /// it automatically with index TTL
    /// </summary>
    [BsonElement("e")]
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>
    /// Since we do not want to store sliding expiration as timespan
    /// we will store in milliseconds
    /// </summary>
    [BsonElement("s")]
    public long? SlidingExpiration { get; }
}
