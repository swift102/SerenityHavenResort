using SerenityHavenResort.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SerenityHavenResort.Models
{
    public class Room
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal PricePerNight { get; set; } // Fixed: was int, now decimal

        [Required]
        [MaxLength(50)]
        public string RoomType { get; set; } // "Standard", "Deluxe", "Suite", etc.

        [Range(1, 10)]
        public int Capacity { get; set; } = 2; // Default to 2 guests

        [MaxLength(1000)]
        public string? Features { get; set; }

        [MaxLength(50)]
        public string Category { get; set; } = "Standard";

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DynamicPrice { get; set; } // For seasonal pricing

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePrice { get; set; }

        [Required]
        public int RoomNumber { get; set; }

        public bool IsAvailable { get; set; } = true;

        // Additional room details
        [Column(TypeName = "decimal(8,2)")]
        public decimal? RoomSize { get; set; } // In square meters

        public int Floor { get; set; } = 1;

        [MaxLength(50)]
        public string? ViewType { get; set; } // "Ocean", "Garden", "City", etc.

        public bool HasBalcony { get; set; } = false;

        public bool IsAccessible { get; set; } = false; // Wheelchair accessible

        public bool AllowsSmoking { get; set; } = false;

        public bool AllowsPets { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Amenities> Amenities { get; set; } = new List<Amenities>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Image> Images { get; set; } = new List<Image>();

        // Computed properties
        [NotMapped]
        public decimal CurrentPrice => DynamicPrice ?? BasePrice;

        [NotMapped]
        public Image? PrimaryImage => Images?.FirstOrDefault(i => i.IsPrimary) ?? Images?.FirstOrDefault();

        [NotMapped]
        public bool IsCurrentlyOccupied
        {
            get
            {
                var today = DateTime.Today;
                return Bookings?.Any(b =>
                    b.Status == BookingStatus.CheckedIn &&
                    b.CheckInDate <= today &&
                    b.CheckOutDate > today) ?? false;
            }
        }

    }
}

