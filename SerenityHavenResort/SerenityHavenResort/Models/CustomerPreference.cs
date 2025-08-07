
using Microsoft.EntityFrameworkCore;
using System;   
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SerenityHavenResort.Models
{
    public class CustomerPreference
    {
        [Key]
        public int PreferenceId { get; set; }

        [Required]
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string PreferenceCategory { get; set; } = null!; // "Room", "Food", "Service", "Accessibility"

        [Required]
        [MaxLength(100)]
        public string PreferenceType { get; set; } = null!; // "BedType", "FloorLevel", "Dietary", etc.

        [Required]
        [MaxLength(500)]
        public string PreferenceValue { get; set; } = null!;

        // Priority and status
        public PreferencePriority Priority { get; set; } = PreferencePriority.Medium;

        public bool IsActive { get; set; } = true;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum PreferencePriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4 // For accessibility or medical needs
    }


}
