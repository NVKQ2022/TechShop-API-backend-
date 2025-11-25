public class MongoServerMetricsDto
{
  public int CurrentConnections { get; set; }
  public int AvailableConnections { get; set; }

  public long Inserts { get; set; }
  public long Queries { get; set; }
  public long Updates { get; set; }
  public long Deletes { get; set; }
  public long Commands { get; set; }

  public long BytesIn { get; set; }
  public long BytesOut { get; set; }
  public long NumRequests { get; set; }

  public long CacheBytes { get; set; }
  public long CacheDirtyBytes { get; set; }
}
