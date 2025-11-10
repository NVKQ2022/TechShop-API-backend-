using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechShop.API.Models
{
    [Table("VERIFICATION_CODES")]
    public class VerificationCode
    {
        [Key]
        [Column("ID")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("USER_ID")]
        public int UserId { get; set; }

        [Required]
        [Column("EMAIL")]
        [MaxLength(255)]
        public string Email { get; set; }

        [Required]
        [Column("CODE")]
        [MaxLength(100)]
        public string Code { get; set; }

        [Required]
        [Column("TYPE")]
        [MaxLength(50)]
        public string Type { get; set; }

        [Required]
        [Column("EXPIRES_AT")]
        public DateTime ExpiresAt { get; set; }

        [Required]
        [Column("IS_USED")]
        public bool IsUsed { get; set; } = false;

        [Required]
        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("USED_AT")]
        public DateTime? UsedAt { get; set; }

        // Optional: Navigation property to User (if you want EF relationship)
        // public User User { get; set; }
    }
}
