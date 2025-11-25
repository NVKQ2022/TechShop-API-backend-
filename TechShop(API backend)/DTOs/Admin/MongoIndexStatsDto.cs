namespace TechShop_API_backend_.Models
{
  public class MongoIndexStatsDto
  {
    public string Collection { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Key { get; set; } = null!;
    public long AccessesOps { get; set; }
    public DateTime Since { get; set; }
    public bool IsTTL { get; set; }
  }
}
