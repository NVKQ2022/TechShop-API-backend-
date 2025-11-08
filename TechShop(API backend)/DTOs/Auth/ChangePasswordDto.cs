namespace TechShop_API_backend_.DTOs.Auth
{
    public class ChangePasswordDto
    {
        public string NewPassword { get; set; } 

        public string CurrentPassword { get; set; }
    }
}
