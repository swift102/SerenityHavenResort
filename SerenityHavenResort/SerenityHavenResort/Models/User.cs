using SerenityHavenResort.Models;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SerenityHavenResort.Models

{
    public class User : IdentityUser
    {
        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        [MaxLength(20)]
        public string? ContactNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Verification fields
        public string? VerificationCode { get; set; }
        public DateTime? VerificationCodeExpiration { get; set; }

        // Navigation properties
        public UserProfile? UserProfile { get; set; }
        public Customer? Customer { get; set; }
        public Employee? Employee { get; set; }

        public UserType UserType { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }

    // User types for role-based logic
    public enum UserType
    {
        Guest = 1,
        Employee = 2,
        Manager = 3,
        Admin = 4
    }
}

