using SerenityHavenResort.Models;
using System.ComponentModel.DataAnnotations;

namespace SerenityHavenResort.Models
{
    public class Amenities
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? Category { get; set; } // e.g., "Technology", "Comfort", "Entertainment"

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}
