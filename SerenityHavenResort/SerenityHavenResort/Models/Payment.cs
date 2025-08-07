using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace SerenityHavenResort.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } // "PayFast", "Stripe", "Cash", "Card"

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [MaxLength(500)]
        public string StatusMessage { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string TransactionId { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "ZAR"; // Default to South African Rand

        [MaxLength(255)]
        public string? StripePaymentIntentId { get; set; }

        [MaxLength(255)]
        public string? PayFastPaymentId { get; set; }

        // Additional payment details
        [Column(TypeName = "decimal(18,2)")]
        public decimal? RefundAmount { get; set; }

        public DateTime? RefundDate { get; set; }

        [MaxLength(500)]
        public string? RefundReason { get; set; }

        [MaxLength(255)]
        public string? ProcessedBy { get; set; } // Staff member who processed

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Booking Booking { get; set; }
        public Customer Customer { get; set; }

    }

    public enum PaymentStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled,
        Refunded,
        PartiallyRefunded,
        Succeeded
    }
}