public class MongoOverviewDto
{
  public string DatabaseName { get; set; } = null!;
  public long Collections { get; set; }
  public long Objects { get; set; }
  public long DataSize { get; set; }
  public long StorageSize { get; set; }
  public long Indexes { get; set; }
  public long IndexSize { get; set; }
  public int CurrentConnections { get; set; }
  public int AvailableConnections { get; set; }
}
