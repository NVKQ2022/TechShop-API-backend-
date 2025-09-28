using System.ComponentModel;

namespace TechShop_API_backend_.Models.Api
{
    public class LoginResponse
    {
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? Token { get; set; }
        public bool IsAdmin { get; set; } //Default False
        public int ExpiresIn { get; set; } // in seconds
    }
}
