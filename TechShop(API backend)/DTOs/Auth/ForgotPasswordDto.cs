namespace TechShop_API_backend_.DTOs.Auth
{
    public class ForgotPasswordDto
    {
        public string email { get; set; }
        public string newPassword { get; set; }

        public string confirmPassword { get; set; }

        public string Otp  {get; set; }
    }
}
