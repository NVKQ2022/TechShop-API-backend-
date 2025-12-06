using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechShop_API_backend_.Models.Authenticate;
[Table("USER_FCM")]

public class UserFcm
{
    [Key]
    [Column("USER_ID")]
    public int UserId { get; set; }

    [Column("FCM_TOKEN")]
    [Required]
    [MaxLength(255)]
    public string FcmToken { get; set; }
}

