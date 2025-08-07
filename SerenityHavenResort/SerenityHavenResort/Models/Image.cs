using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SerenityHavenResort.Models
{
    public class Image
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(255)]
        public string? Name { get; set; }

        [Required]
        [MaxLength(500)]
        public string ImagePath { get; set; } // Store relative path

        [MaxLength(255)]
        public string? AltText { get; set; } // For accessibility

        [MaxLength(100)]
        public string? Caption { get; set; }

        public bool IsPrimary { get; set; } = false; // Main image for the room

        public int DisplayOrder { get; set; } = 0; // For sorting images

        public long FileSize { get; set; } // In bytes

        [MaxLength(50)]
        public string? ContentType { get; set; } // MIME type

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int RoomID { get; set; } // Fixed naming convention

        // Navigation property
        public Room Room { get; set; }

        // Computed property for full URL
        [NotMapped]
        public string FullImageUrl => $"/images/rooms/{ImagePath}";
    }

}
