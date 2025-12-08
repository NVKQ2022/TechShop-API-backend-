namespace TechShop_API_backend_.DTOs.Admin
{
  public class AdminTopCustomerDto
  {
    public int UserId { get; set; }
    public string? Email { get; set; }
    public string? Username { get; set; }
    public string? Name { get; set; }
    public string? Avatar { get; set; }

    public int TotalOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public int CancelledOrders { get; set; }

    public decimal TotalSpent { get; set; }   // tổng tiền các đơn Delivered
  }
}
