namespace TechShop_API_backend_.DTOs.Admin
{
  public class AdminUserOverviewDto
  {
    public int Id { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsEmailVerified { get; set; }
    public string? Name { get; set; }
    public string? Avatar { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public DateTime? Birthday { get; set; }

    public int CartItemCount { get; set; }
    public int WishlistCount { get; set; }
    public int TotalOrders { get; set; }

    public int NotConfirmOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ProcessingOrders { get; set; }
    public int ShippedOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int ConfirmedOrders { get; set; }
    public decimal TotalSpent { get; set; }

    public DateTime? FirstOrderAt { get; set; }
    public DateTime? LastOrderAt { get; set; }
  }
}
