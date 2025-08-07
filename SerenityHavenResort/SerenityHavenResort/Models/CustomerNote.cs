
using SerenityHavenResort.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;


namespace SerenityHavenResort.Models
{
    public class CustomerNote
    {
        [Key]
        public int NoteId { get; set; }

        [Required]
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        [Required]
        [MaxLength(2000)]
        public string Note { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string CreatedBy { get; set; } = null!; // Employee name/username

        // Reference to Employee entity
        public int? EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        // Note categorization
        public NoteType NoteType { get; set; } = NoteType.General;

        public NotePriority Priority { get; set; } = NotePriority.Normal;

        // Status and visibility
        public bool IsImportant { get; set; } = false;
        public bool IsVisible { get; set; } = true;
        public bool RequiresAction { get; set; } = false;
        public bool ActionCompleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Action tracking
        public DateTime? ActionDueDate { get; set; }
        public DateTime? ActionCompletedDate { get; set; }

        [MaxLength(100)]
        public string? ActionCompletedBy { get; set; }
    }

    // Note types for better organization
    public enum NoteType
    {
        General = 1,
        Complaint = 2,
        Compliment = 3,
        SpecialRequest = 4,
        Medical = 5,
        Dietary = 6,
        Accessibility = 7,
        Security = 8,
        Billing = 9,
        Maintenance = 10
    }

    // Note priority levels
    public enum NotePriority
    {
        Low = 1,
        Normal = 2,
        High = 3,
        Urgent = 4,
        Critical = 5
    }

}
