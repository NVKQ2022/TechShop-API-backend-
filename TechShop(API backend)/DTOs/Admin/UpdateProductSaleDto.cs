namespace TechShop_API_backend_.DTOs.Admin
{
  public class UpdateProductSaleDto
  {
    public double Percent { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
  }
}
