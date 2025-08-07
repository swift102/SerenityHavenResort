using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SerenityHavenResort.Models
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Token { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        public bool IsRevoked { get; set; } = false;

        public DateTime? RevokedAt { get; set; }

        [MaxLength(255)]
        public string? RevokedByIp { get; set; }

        [MaxLength(255)]
        public string? CreatedByIp { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } 

        [NotMapped]
        public bool IsActive => !IsRevoked && DateTime.UtcNow <= ExpiryDate;
    }

}
