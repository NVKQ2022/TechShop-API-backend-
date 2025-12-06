public class CollectionStatsDto
{
  public string Name { get; set; } = null!;
  public long Count { get; set; }
  public long Size { get; set; }
  public long StorageSize { get; set; }
  public long AvgObjSize { get; set; }
  public long TotalIndexSize { get; set; }
  public int Indexes { get; set; }
}
