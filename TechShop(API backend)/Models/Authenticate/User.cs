using System.ComponentModel.DataAnnotations;

namespace TechShop_API_backend_.Models.Authenticate
{
    public class User
    {

        [Key]
        public int Id { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string Salt { get; set; }

        public bool IsAdmin { get; set; } = false;

        public bool IsEmailVerified { get; set; } = false;

        public string? GOOGLE_ID{ get; set; } = string.Empty;
        public User()
        {

        }

    }
    public class UserId
    {
        public int Id { get; set; }

    }
}
