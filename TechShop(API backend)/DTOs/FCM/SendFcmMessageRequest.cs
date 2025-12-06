namespace TechShop_API_backend_.DTOs.FCM
{
    public class SendFcmMessageRequest
    {
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        // Optional data payload
        public Dictionary<string, string>? Data { get; set; }
    }
}
