using System.Collections.Generic;
using SerenityHavenResort.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;


namespace SerenityHavenResort.Models
{
    public class Employee
    {
        [Key]
        public int EmployeeId { get; set; }

        // Link to Identity (single source of personal data)
        [Required]
        public string UserId { get; set; } = null!;
        public User User { get; set; } = null!;

        // Employee-specific information only
        [Required]
        [MaxLength(20)]
        public string EmployeeNumber { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Department { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Position { get; set; } = null!;

        public DateTime HireDate { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Salary { get; set; }

        [MaxLength(100)]
        public string? Supervisor { get; set; }

        // Employment details
        [MaxLength(50)]
        public string EmploymentStatus { get; set; } = "Full-time"; // "Full-time", "Part-time", "Contract"

        [MaxLength(50)]
        public string? WorkSchedule { get; set; } // "Day", "Night", "Rotating"

        [MaxLength(50)]
        public string? EmployeeType { get; set; } // "Receptionist", "Housekeeper", "Manager", etc.

        // Status
        public bool IsActive { get; set; } = true;

        // Emergency contact (work-specific)
        [MaxLength(100)]
        public string? EmergencyContactName { get; set; }

        [MaxLength(20)]
        public string? EmergencyContactPhone { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<CustomerNote> CreatedNotes { get; set; } = new List<CustomerNote>();

        // Computed properties for easy access
        [NotMapped]
        public string FullName => User?.FirstName ?? "Unknown Employee";

        [NotMapped]
        public string DisplayName => $"{FullName} - {Position}";
    }
}
