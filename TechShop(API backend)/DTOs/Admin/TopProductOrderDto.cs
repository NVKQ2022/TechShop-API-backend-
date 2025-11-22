using TechShop_API_backend_.Models;

namespace TechShop_API_backend_.DTOs.Admin
{
  public class TopProductOrderDto
  {
    public string ProductId { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public string Image { get; set; } = null!;
    public string Category { get; set; } = null!;
    public double Rating { get; set; }
    public int SelledCount { get; set; }
    public int OrderCount { get; set; }
  }
}
