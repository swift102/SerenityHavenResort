using SerenityHavenResort.Models;
using System.ComponentModel.DataAnnotations;


namespace SerenityHavenResort.Models
{
    public class UserProfile
    {
        [Key]
        public int UserProfileID { get; set; }

        [MaxLength(1000)]
        public string? ProfileDescription { get; set; }

        [MaxLength(255)]
        public string? Avatar { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(100)]
        public string? Nationality { get; set; }

        [MaxLength(10)]
        public string? PreferredLanguage { get; set; } = "en";

        // Communication preferences
        public bool EmailNotifications { get; set; } = true;
        public bool SmsNotifications { get; set; } = false;
        public bool MarketingEmails { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key
        [Required]
        public string UserID { get; set; } = null!;
        public User User { get; set; } = null!;
    }

}
