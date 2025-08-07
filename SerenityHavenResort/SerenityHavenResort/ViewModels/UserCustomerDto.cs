using System.ComponentModel.DataAnnotations;

namespace SerenityHavenResort.ViewModels
{
    public class UserCustomerDto
    {
        public class CustomerDto
        {
            public int Id { get; set; }
            public string UserID { get; set; } = null!;

            // Read-only User properties (from User entity)
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public string Email { get; set; } = "";
            public string Phone { get; set; } = "";

            // Customer-specific properties
            public DateTime? DateOfBirth { get; set; }
            public string? Nationality { get; set; }
            public string? PassportNumber { get; set; }
            public string? IdNumber { get; set; }
            public string? Address { get; set; }
            public string? City { get; set; }
            public string? Country { get; set; }
            public string? PostalCode { get; set; }
            public string? DietaryRestrictions { get; set; }
            public string? RoomPreferences { get; set; }
            public bool IsVip { get; set; }
            public string? LoyaltyTier { get; set; }
            public int LoyaltyPoints { get; set; }
            public string? LoyaltyMembershipNumber { get; set; }
            public string? EmergencyContactName { get; set; }
            public string? EmergencyContactPhone { get; set; }
            public string? EmergencyContactRelationship { get; set; }
            public string? CompanyName { get; set; }
            public string? CompanyPosition { get; set; }
            public int TotalStays { get; set; }
            public decimal TotalSpent { get; set; }
            public DateTime? LastStayDate { get; set; }
            public DateTime? FirstStayDate { get; set; }
            public bool IsBlacklisted { get; set; }
            public string? BlacklistReason { get; set; }
            public DateTime? BlacklistedDate { get; set; }
            public string? BlacklistedBy { get; set; }
            public decimal AverageRating { get; set; }
            public int ReviewCount { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
            public string DisplayName { get; set; } = "";
        }

        public class CreateCustomerDto
        {
            [Required]
            public string UserID { get; set; } = null!;

            // Optional Customer-specific properties for creation
            public DateTime? DateOfBirth { get; set; }
            public string? Nationality { get; set; }
            public string? PassportNumber { get; set; }
            public string? IdNumber { get; set; }
            public string? Address { get; set; }
            public string? City { get; set; }
            public string? Country { get; set; }
            public string? PostalCode { get; set; }
            public string? DietaryRestrictions { get; set; }
            public string? RoomPreferences { get; set; }
            public bool IsVip { get; set; } = false;
            public string? LoyaltyTier { get; set; }
            public int LoyaltyPoints { get; set; } = 0;
            public string? LoyaltyMembershipNumber { get; set; }
            public string? EmergencyContactName { get; set; }
            public string? EmergencyContactPhone { get; set; }
            public string? EmergencyContactRelationship { get; set; }
            public string? CompanyName { get; set; }
            public string? CompanyPosition { get; set; }
        }

        public class UpdateCustomerDto
        {
            [Required]
            public int Id { get; set; }

            // Only Customer-specific properties that can be updated
            public DateTime? DateOfBirth { get; set; }
            public string? Nationality { get; set; }
            public string? PassportNumber { get; set; }
            public string? IdNumber { get; set; }
            public string? Address { get; set; }
            public string? City { get; set; }
            public string? Country { get; set; }
            public string? PostalCode { get; set; }
            public string? DietaryRestrictions { get; set; }
            public string? RoomPreferences { get; set; }
            public bool IsVip { get; set; }
            public string? LoyaltyTier { get; set; }
            public int LoyaltyPoints { get; set; }
            public string? LoyaltyMembershipNumber { get; set; }
            public string? EmergencyContactName { get; set; }
            public string? EmergencyContactPhone { get; set; }
            public string? EmergencyContactRelationship { get; set; }
            public string? CompanyName { get; set; }
            public string? CompanyPosition { get; set; }

            // Administrative fields (optional)
            public bool? IsBlacklisted { get; set; }
            public string? BlacklistReason { get; set; }
            public string? BlacklistedBy { get; set; }
        }
    }
}
