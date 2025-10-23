namespace TechShop_API_backend_.Models.Authenticate
{
    public class OTPCode
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public int UserId { get; set; }

        public string Email { get; set; }

        public string OTPCodeValue { get; set; }

        public DateTime ExpiresAt { get; set; }

        public bool IsVerified { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Optional: navigation property to User
        public User User { get; set; }
    }
}
