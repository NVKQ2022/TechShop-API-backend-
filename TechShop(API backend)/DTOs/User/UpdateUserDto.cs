using System.ComponentModel.DataAnnotations;

namespace TechShop_API_backend_.DTOs.User
{
    public class UpdateUserDto
    {
        public string? Email { get; set; }

        public string? Username { get; set; }

        
        public string? Password { get; set; }
    }
}
