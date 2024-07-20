# DistributedCache MongoDb

This is an opionated implementation of the classic [IDistributedCache](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.distributed.idistributedcache?view=net-8.0) on MongoDb database. The aim is to minimize call to the datatabse and using most up-to-date driver as possible.

The package is published on Nuget - [TeamNebula - Mongodb Distributed cache](https://www.nuget.org/packages/TeamNebula.DistributedCache.MongoDb/0.2.0-alpha0009)

## Usage

Usage is really simple, you just configure with the corresponding extensions methods.

```csharp
services.AddMongoDbDistributedCache(options => 
{
    options.ConnectionString = "mongodb://...";
    options.DatabaseName = "my-database-name";
    options.CollectionName = "my-collection-name";
    options.DefaultCacheDurationsInMinutes = 10;
});
```

Also options has a special property called `MongoClientFactory` that allows the extension to call your function to obtain an instance of a IMongoClient. This allows for your application to manage all possible options in a centralized way (intercepting etc). Also allows you to have a single instance of the IMongoClient for your porcess (a well known best practice in MongoDb).