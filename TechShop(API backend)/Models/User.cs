using System.ComponentModel.DataAnnotations;

namespace TechShop_API_backend_.Models
{
    public class User
    {

        [Key]
        public int Id { get; set; }
        [Required]
        public string Email{ get; set; }
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string Salt {  get; set; }

        public bool IsAdmin = false;
        public User()
        {

        }

    }
    public class UserId
    {
        public int Id { get; set; }
       
    }
}
