namespace TechShop_API_backend_.DTOs.Admin
{
  public class AdminUserListItemDto
  {
    public int Id { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsEmailVerified { get; set; }
  }
}
