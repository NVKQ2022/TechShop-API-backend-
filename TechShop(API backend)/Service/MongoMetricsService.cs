using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using TechShop_API_backend_.Models;

namespace TechShop_API_backend_.Service
{
  public class MongoMetricsService
  {
    private readonly IMongoClient _client;
    private readonly IMongoDatabase _db;
    private readonly IMongoDatabase _adminDb;

    public MongoMetricsService(IOptions<MongoDbSettings> settings)
    {
      _client = new MongoClient(settings.Value.ConnectionString);
      _db = _client.GetDatabase(settings.Value.DatabaseName);
      _adminDb = _client.GetDatabase("admin");
    }

    public async Task<MongoOverviewDto> GetOverviewAsync()
    {
      // dbStats
      var dbStatsCmd = new BsonDocument { { "dbStats", 1 } };
      var dbStats = await _db.RunCommandAsync<BsonDocument>(dbStatsCmd);

      // serverStatus
      var serverStatusCmd = new BsonDocument { { "serverStatus", 1 } };
      var serverStatus = await _adminDb.RunCommandAsync<BsonDocument>(serverStatusCmd);

      var connections = serverStatus.GetValue("connections").AsBsonDocument;

      var dto = new MongoOverviewDto
      {
        DatabaseName = _db.DatabaseNamespace.DatabaseName,
        Collections = dbStats.GetValue("collections", 0).ToInt64(),
        Objects = dbStats.GetValue("objects", 0).ToInt64(),
        DataSize = dbStats.GetValue("dataSize", 0).ToInt64(),
        StorageSize = dbStats.GetValue("storageSize", 0).ToInt64(),
        Indexes = dbStats.GetValue("indexes", 0).ToInt64(),
        IndexSize = dbStats.GetValue("indexSize", 0).ToInt64(),
        CurrentConnections = connections.GetValue("current", 0).ToInt32(),
        AvailableConnections = connections.GetValue("available", 0).ToInt32()
      };

      return dto;
    }

    // stats cho từng collection
    public async Task<CollectionStatsDto> GetCollectionStatsAsync(string collectionName)
    {
      var cmd = new BsonDocument
        {
            { "collStats", collectionName },
            { "scale", 1024 }
        };

      var stats = await _db.RunCommandAsync<BsonDocument>(cmd);

      return new CollectionStatsDto
      {
        Name = stats.GetValue("ns", "").AsString,
        Count = stats.GetValue("count", 0).ToInt64(),
        Size = stats.GetValue("size", 0).ToInt64(),
        StorageSize = stats.GetValue("storageSize", 0).ToInt64(),
        AvgObjSize = stats.GetValue("avgObjSize", 0).ToInt64(),
        TotalIndexSize = stats.GetValue("totalIndexSize", 0).ToInt64(),
        Indexes = stats.GetValue("nindexes", 0).ToInt32()
      };
    }

    public async Task<MongoServerMetricsDto> GetServerMetricsAsync()
    {
      var cmd = new BsonDocument { { "serverStatus", 1 } };
      var doc = await _adminDb.RunCommandAsync<BsonDocument>(cmd);

      // connections
      var connectionsDoc = doc.GetValue("connections", new BsonDocument()).AsBsonDocument;

      // opcounters
      var opcountersDoc = doc.GetValue("opcounters", new BsonDocument()).AsBsonDocument;

      // network
      var networkDoc = doc.GetValue("network", new BsonDocument()).AsBsonDocument;

      // wiredTiger cache (nếu có)
      BsonDocument? cacheDoc = null;
      if (doc.TryGetValue("wiredTiger", out var wtValue) && wtValue.IsBsonDocument)
      {
        var wtDoc = wtValue.AsBsonDocument;
        if (wtDoc.TryGetValue("cache", out var cacheValue) && cacheValue.IsBsonDocument)
        {
          cacheDoc = cacheValue.AsBsonDocument;
        }
      }

      long cacheBytes = 0;
      long cacheDirtyBytes = 0;
      if (cacheDoc != null)
      {
        cacheBytes = cacheDoc.GetValue("bytes currently in the cache", 0).ToInt64();
        cacheDirtyBytes = cacheDoc.GetValue("tracked dirty bytes in the cache", 0).ToInt64();
      }

      var dto = new MongoServerMetricsDto
      {
        CurrentConnections = connectionsDoc.GetValue("current", 0).ToInt32(),
        AvailableConnections = connectionsDoc.GetValue("available", 0).ToInt32(),

        Inserts = opcountersDoc.GetValue("insert", 0).ToInt64(),
        Queries = opcountersDoc.GetValue("query", 0).ToInt64(),
        Updates = opcountersDoc.GetValue("update", 0).ToInt64(),
        Deletes = opcountersDoc.GetValue("delete", 0).ToInt64(),
        Commands = opcountersDoc.GetValue("command", 0).ToInt64(),

        BytesIn = networkDoc.GetValue("bytesIn", 0).ToInt64(),
        BytesOut = networkDoc.GetValue("bytesOut", 0).ToInt64(),
        NumRequests = networkDoc.GetValue("numRequests", 0).ToInt64(),

        CacheBytes = cacheBytes,
        CacheDirtyBytes = cacheDirtyBytes
      };

      return dto;
    }

    public async Task<List<MongoIndexStatsDto>> GetIndexStatsForCollectionAsync(string collectionName)
    {
      var collection = _db.GetCollection<BsonDocument>(collectionName);

      var pipeline = new[]
      {
    new BsonDocument("$indexStats", new BsonDocument())
  };

      using var cursor = await collection.AggregateAsync<BsonDocument>(pipeline);
      var docs = await cursor.ToListAsync();

      var result = new List<MongoIndexStatsDto>();

      foreach (var doc in docs)
      {
        var accesses = doc.GetValue("accesses", new BsonDocument()).AsBsonDocument;
        var spec = doc.GetValue("spec", new BsonDocument()).AsBsonDocument;

        var name = doc.GetValue("name", "").AsString;

        string keyString;
        if (spec.TryGetValue("key", out var keyVal) && keyVal.IsBsonDocument)
        {
          var keyDoc = keyVal.AsBsonDocument;
          keyString = string.Join(", ", keyDoc.Elements.Select(e => $"{e.Name}: {e.Value}"));
        }
        else
        {
          keyString = "{}";
        }

        bool isTtl = spec.Contains("expireAfterSeconds");

        var sinceVal = accesses.GetValue("since", BsonNull.Value);
        var since = sinceVal.IsBsonDateTime
          ? sinceVal.ToUniversalTime()
          : DateTime.MinValue;

        result.Add(new MongoIndexStatsDto
        {
          Collection = collectionName,
          Name = name,
          Key = keyString,
          AccessesOps = accesses.GetValue("ops", 0).ToInt64(),
          Since = since,
          IsTTL = isTtl,
        });
      }

      return result;
    }

  }
}