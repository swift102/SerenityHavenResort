using SerenityHavenResort.Models;
using SerenityHavenResort.Services;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SerenityHavenResort.Models
{
    public enum BookingStatus
    {
        Pending,
        Confirmed,
        CheckedIn,
        CheckedOut,
        Cancelled,
        NoShow,
        Refunded
    }

    [CustomValidation(typeof(BookingValidation), "ValidateDates")]
    public class Booking
    {
        [Key]
        public int Id { get; set; }

        // Core booking relationships
        [Required]
        public int RoomId { get; set; }
        [Required]
        public int CustomerId { get; set; } // The guest (Customer record)

        // WHO CREATED THE BOOKING 
        // This could be the guest themselves (if registered) OR staff member
        public string? UserId { get; set; } // User who created the booking (nullable - could be walk-in)
        //public int? UserProfileID { get; set; } // UserProfile of who created booking (nullable)

        // ALTERNATIVE: Staff member who processed the booking
        public int? ProcessedByEmployeeId { get; set; } // If staff member created booking

        // Booking details
        [Required]
        public DateTime CheckInDate { get; set; }
        [Required]
        public DateTime CheckOutDate { get; set; }
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        // Booking metadata
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        [Required]
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        // Guest information
        [Range(1, 10)]
        public int GuestCount { get; set; } = 1;
        public int ChildrenCount { get; set; } = 0;

        // Hotel-specific booking details
        [MaxLength(50)]
        public string BookingSource { get; set; } = "Direct"; // "Direct", "Online", "Phone", "Walk-in"

        // Cancellation and refund policies
        public bool IsRefundable { get; set; } = true;
        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal RefundPercentage { get; set; } = 100;
        public DateTime? RefundDeadline { get; set; }

        // Payment integration
        public string? PaymentIntentId { get; set; }
        public string? BookingReference { get; set; } // Auto-generated unique reference

        // Additional information
        public string? SpecialRequests { get; set; }
        public string? InternalNotes { get; set; } // Staff notes

        // Pricing breakdown for transparency
        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePrice { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal ServiceCharges { get; set; } = 0;

        // Navigation properties
        public Customer Customer { get; set; } = null!; // The guest
        public Room Room { get; set; } = null!;
        public User? CreatedByUser { get; set; } // Who created the booking (optional)
        public Employee? ProcessedByEmployee { get; set; } // Staff member who processed (optional)
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<BookingService> AdditionalServices { get; set; } = new List<BookingService>();

        // Computed properties
        public int NumberOfNights => (CheckOutDate - CheckInDate).Days;
        public bool IsActive => Status == BookingStatus.Confirmed || Status == BookingStatus.CheckedIn;
        public bool CanBeCancelled => Status == BookingStatus.Pending || Status == BookingStatus.Confirmed;
        public bool IsRefundEligible => IsRefundable &&
                                       RefundDeadline.HasValue &&
                                       DateTime.UtcNow <= RefundDeadline.Value;
    }

    public static class BookingValidation
    {
        public static ValidationResult ValidateDates(object value, ValidationContext context)
        {
            var booking = (Booking)context.ObjectInstance;
            if (booking.CheckOutDate <= booking.CheckInDate)
                return new ValidationResult("Check-out date must be after check-in date");
            return ValidationResult.Success!;
        }
    }

    // Add missing ErrorResponse class
    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}


