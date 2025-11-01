namespace TechShop_API_backend_.DTOs.Auth
{
    public class ChangePasswordDto
    {
        public string Email { get; set; }
        public string NewPassword { get; set; } 

        public string CurrentPassword { get; set; }

        public string OTP { get; set; }
    }
}
