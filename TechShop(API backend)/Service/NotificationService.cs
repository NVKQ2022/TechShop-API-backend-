namespace TechShop_API_backend_.Service
{

    public class NotificationService
    {
    }
    public class DeviceToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
