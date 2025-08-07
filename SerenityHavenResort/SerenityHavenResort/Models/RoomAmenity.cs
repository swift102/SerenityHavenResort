using SerenityHavenResort.Models;

namespace SerenityHavenResort.Models
{
    public class RoomAmenity
    {
        public int RoomId { get; set; }
        public int AmenityId { get; set; }
        public DateTime AddedDate { get; set; } = DateTime.UtcNow;

        public Room Room { get; set; }
        public Amenities Amenity { get; set; }
    }
}
