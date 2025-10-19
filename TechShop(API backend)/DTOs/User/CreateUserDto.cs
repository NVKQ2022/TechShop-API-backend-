using System.ComponentModel.DataAnnotations;

namespace TechShop_API_backend_.DTOs.User
{
    public class CreateUserDto
    {

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, MinLength(3)]
        public string Username { get; set; }

        [Required, MinLength(8)]
        public string Password { get; set; }

        [Required, MinLength(8)]
        public string ConfirmPassword { get; set; }


    }
}
