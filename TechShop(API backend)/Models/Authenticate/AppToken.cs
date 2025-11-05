namespace TechShop_API_backend_.Models.Authenticate
{
    public class AppToken
    {
        public int UserId { get; set; }
        public string Token { get; set; }
        public string UserName { get; set; }

        public bool IsAdmin { get; set; }

        public DateTime ExpiresIn { get; set; }
    }
}
