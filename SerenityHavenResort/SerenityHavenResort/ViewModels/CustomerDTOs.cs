using System.ComponentModel.DataAnnotations;

namespace SerenityHavenResort.DTOs
{
    public class CustomerReadDTO
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";

        // Add Customer-specific properties you might want to read
        public string UserID { get; set; } = "";
        public bool IsVip { get; set; }
        public string? LoyaltyTier { get; set; }
        public int LoyaltyPoints { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // DTO for adding a customer (write-only)
    public class CustomerCreateDTO
    {
        [Required]
        public string UserID { get; set; } = null!; // Link to existing User

        // Customer-specific properties only
        public DateTime? DateOfBirth { get; set; }
        public string? Nationality { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public bool IsVip { get; set; } = false;
        public string? LoyaltyTier { get; set; }
        public int LoyaltyPoints { get; set; } = 0;
        public string? DietaryRestrictions { get; set; }
        public string? RoomPreferences { get; set; }
    }

    // DTO for updating customer details (write-only)
    public class CustomerUpdateDTO
    {
        public int Id { get; set; }

        // Customer-specific properties only (no User properties)
        public DateTime? DateOfBirth { get; set; }
        public string? Nationality { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public bool IsVip { get; set; }
        public string? LoyaltyTier { get; set; }
        public int LoyaltyPoints { get; set; }
        public string? DietaryRestrictions { get; set; }
        public string? RoomPreferences { get; set; }
    }

}
