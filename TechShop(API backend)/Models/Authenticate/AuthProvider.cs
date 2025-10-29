using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TechShop_API_backend_.Models.Authenticate
{
    [Table("auth_providers")]
    public class AuthProvider
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("user_id")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }   // navigation property

        [Required]
        [MaxLength(50)]
        [Column("provider")]
        public string? Provider { get; set; }   // "google" | "facebook" | "apple" | "email"

        [Required]
        [MaxLength(255)]
        [Column("provider_user_id")]
        public string? ProviderUserId { get; set; }

        [MaxLength(255)]
        [Column("provider_email")]
        public string? ProviderEmail { get; set; }

        [Column("access_token")]
        public string? AccessToken { get; set; }

        [Column("refresh_token")]
        public string? RefreshToken { get; set; }

        [Column("token_expires_at")]
        public DateTime? TokenExpiresAt { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    }
}
