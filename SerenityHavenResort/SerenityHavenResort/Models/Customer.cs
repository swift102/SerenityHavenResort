
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SerenityHavenResort.Models
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        // Link to Identity (single source of personal data)
        [Required]
        public string UserID { get; set; } = null!;
        public User User { get; set; } = null!;

        // Hotel-specific customer information only
        public DateTime? DateOfBirth { get; set; }

        [MaxLength(100)]
        public string? Nationality { get; set; }

        // Travel documents
        [MaxLength(50)]
        [PersonalData]
        public string? PassportNumber { get; set; }

        [MaxLength(50)]
        [PersonalData]
        public string? IdNumber { get; set; }

        // Address Information
        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [MaxLength(20)]
        public string? PostalCode { get; set; }

        // Hotel-specific preferences and restrictions
        [MaxLength(1000)]
        public string? DietaryRestrictions { get; set; }

        [MaxLength(1000)]
        public string? RoomPreferences { get; set; }

        // VIP and loyalty information
        public bool IsVip { get; set; } = false;

        [MaxLength(50)]
        public string? LoyaltyTier { get; set; }

        public int LoyaltyPoints { get; set; } = 0;

        [MaxLength(50)]
        public string? LoyaltyMembershipNumber { get; set; }

        // Emergency contact (guest-specific, different from employee emergency contact)
        [MaxLength(100)]
        public string? EmergencyContactName { get; set; }

        [MaxLength(20)]
        public string? EmergencyContactPhone { get; set; }

        [MaxLength(100)]
        public string? EmergencyContactRelationship { get; set; }

        // Company information (for corporate guests)
        [MaxLength(200)]
        public string? CompanyName { get; set; }

        [MaxLength(100)]
        public string? CompanyPosition { get; set; }

        // Guest statistics and behavior
        public int TotalStays { get; set; } = 0;

        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalSpent { get; set; } = 0;

        public DateTime? LastStayDate { get; set; }

        public DateTime? FirstStayDate { get; set; }

        // Special status flags
        public bool IsBlacklisted { get; set; } = false;

        [MaxLength(1000)]
        public string? BlacklistReason { get; set; }

        public DateTime? BlacklistedDate { get; set; }

        [MaxLength(100)]
        public string? BlacklistedBy { get; set; }

        // Guest rating (for VIP status determination)
        public decimal AverageRating { get; set; } = 0;

        public int ReviewCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<CustomerPreference> Preferences { get; set; } = new List<CustomerPreference>();
        public ICollection<CustomerNote> Notes { get; set; } = new List<CustomerNote>();

        // Computed properties for easy access (no data duplication)
        [NotMapped]
        public string FirstName => User?.FirstName ?? "Unknown Guest";
        public string LastName => User?.LastName ?? "Unknown Guest";

        [NotMapped]
        public string Email => User?.Email ?? "";

        [NotMapped]
        public string Phone => User?.ContactNumber ?? "";

        [NotMapped]
        public string DisplayName => IsVip ? $"{FirstName} (VIP)" : FirstName;
    }

}
