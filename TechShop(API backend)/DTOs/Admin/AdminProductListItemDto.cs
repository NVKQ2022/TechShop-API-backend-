namespace TechShop_API_backend_.DTOs.Admin
{
  public class AdminProductListItemDto
  {
    public string ProductId { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public int Price { get; set; }
    public int Stock { get; set; }
    public int Sold { get; set; }
    public double RatingAverage { get; set; }
    public bool IsOnSale { get; set; }
    public double? SalePercent { get; set; }
    public List<string> ImageURL { get; set; }
  }

}
