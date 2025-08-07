using SerenityHavenResort.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using SerenityHavenResort.Data;

namespace SerenityHavenResort.Services
{
    public class RoomService
    {
        private readonly AppDbContext _context;
        private readonly IRoomRepository _roomRepository; 
        private readonly IDistributedCache _cache;
        private readonly ILogger<RoomService> _logger;
        private readonly PricingService _pricingService;
        private readonly IEmailSender _emailSender;

        public RoomService(
            AppDbContext context,
            IRoomRepository roomRepository, 
            IDistributedCache cache,
            ILogger<RoomService> logger,
            PricingService pricingService,
            IEmailSender emailSender)
        {
            _context = context;
            _roomRepository = roomRepository ?? throw new ArgumentNullException(nameof(roomRepository));
            _cache = cache;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pricingService = pricingService;
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        }

        public async Task<IEnumerable<Room>> GetRooms()
        {
            try
            {
                var cacheKey = "rooms_all";
                var cachedRooms = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedRooms))
                {
                    _logger.LogInformation("Cache hit for rooms");
                    return JsonSerializer.Deserialize<IEnumerable<Room>>(cachedRooms);
                }

                var rooms = await _roomRepository.GetRoomsAsync(); 
                var serializedRooms = JsonSerializer.Serialize(rooms);
                await _cache.SetStringAsync(cacheKey, serializedRooms, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });

                _logger.LogInformation("Cache miss for rooms, fetched from database");
                return rooms;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching rooms");
                throw new RoomServiceException("Failed to retrieve rooms.", ex);
            }
        }

        public async Task<Room?> GetRoomByIdAsync(int id)
        {
            try
            {
                return await _roomRepository.GetRoomByIdAsync(id); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching room with ID {RoomId}", id);
                throw;
            }
        }

        public async Task<Room> AddRoomAsync(Room room)
        {
            try
            {
                var result = await _roomRepository.AddAsync(room); 
                _logger.LogInformation("Added room with ID {RoomId}", result.Id);

                // Clear cache after adding new room
                await _cache.RemoveAsync("rooms_all");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding room");
                throw;
            }
        }

        public async Task UpdateRoomAsync(Room room)
        {
            try
            {
                await _roomRepository.UpdateAsync(room); 
                _logger.LogInformation("Updated room with ID {RoomId}", room.Id);

                // Clear cache after updating room
                await _cache.RemoveAsync("rooms_all");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating room with ID {RoomId}", room.Id);
                throw;
            }
        }

        public async Task UpdateRoomAvailabilityAsync(int roomId, bool isAvailable)
        {
            var room = await _roomRepository.GetByIdAsync(roomId);
            if (room == null)
            {
                throw new ArgumentException($"Room with ID {roomId} not found.");
            }

            room.IsAvailable = isAvailable;
            await _roomRepository.UpdateAsync(room);

            // Clear cache after updating availability
            await _cache.RemoveAsync("rooms_all");
        }

        public async Task DeleteRoomAsync(int id)
        {
            try
            {
                var deleted = await _roomRepository.DeleteAsync(id); 
                if (deleted)
                {
                    _logger.LogInformation("Deleted room with ID {RoomId}", id);
                    // Clear cache after deleting room
                    await _cache.RemoveAsync("rooms_all");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting room with ID {RoomId}", id);
                throw;
            }
        }

        public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut)
        {
            try
            {
                return await _roomRepository.IsRoomAvailableAsync(roomId, checkIn, checkOut);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking availability for room {RoomId}", roomId);
                throw new RoomServiceException($"Failed to check availability for room {roomId}.", ex);
            }
        }

        public async Task<decimal> CalculatePrice(int roomId, DateTime checkIn, DateTime checkOut)
        {
            try
            {
                var room = await _roomRepository.GetByIdAsync(roomId);
                if (room == null)
                {
                    _logger.LogWarning("Room not found: {RoomId}", roomId);
                    throw new RoomNotFoundException($"Room with ID {roomId} not found.");
                }

                var days = (checkOut - checkIn).Days;
                return (decimal)(room.DynamicPrice * days);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating price for room {RoomId}", roomId);
                throw new RoomServiceException($"Failed to calculate price for room {roomId}.", ex);
            }
        }

        public async Task ValidateRoomCapacityAsync(int roomId, int guestCount, bool isAdmin)
        {
            try
            {
                var room = await _roomRepository.GetByIdAsync(roomId);
                if (room == null)
                {
                    _logger.LogWarning("Room not found: {RoomId}", roomId);
                    throw new RoomNotFoundException($"Room with ID {roomId} not found.");
                }

                if (guestCount > room.Capacity && !isAdmin)
                {
                    _logger.LogWarning("Guest count {GuestCount} exceeds capacity {Capacity} for room {RoomId}", guestCount, room.Capacity, roomId);
                    throw new RoomValidationException($"Guest count {guestCount} exceeds room capacity {room.Capacity}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating capacity for room {RoomId}", roomId);
                throw new RoomServiceException($"Failed to validate capacity for room {roomId}.", ex);
            }
        }

        public async Task<(bool IsCancellable, decimal RefundPercentage)> CanCancelBookingAsync(int bookingId, bool isAdmin)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Room)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);
                if (booking == null)
                {
                    _logger.LogWarning("Booking not found: {BookingId}", bookingId);
                    throw new BookingNotFoundException($"Booking with ID {bookingId} not found.");
                }

                if (booking.Status == BookingStatus.Cancelled)
                {
                    _logger.LogWarning("Booking already cancelled: {BookingId}", bookingId);
                    return (false, 0);
                }

                if (booking.CheckOutDate < DateTime.UtcNow.Date)
                {
                    _logger.LogWarning("Cannot cancel past booking: {BookingId}", bookingId);
                    return (false, 0);
                }

                if (isAdmin)
                {
                    return (true, 100); // Admins can cancel with full refund
                }

                if (!booking.IsRefundable)
                {
                    _logger.LogWarning("Non-refundable booking: {BookingId}", bookingId);
                    return (false, 0);
                }

                var daysUntilCheckIn = (booking.CheckInDate - DateTime.UtcNow.Date).Days;
                if (daysUntilCheckIn >= 7)
                {
                    return (true, 100); // Full refund
                }
                else if (daysUntilCheckIn >= 3)
                {
                    return (true, 50); // Partial refund
                }
                else
                {
                    _logger.LogWarning("Too late to cancel booking: {BookingId}", bookingId);
                    return (false, 0); // No refund
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking cancellation eligibility for booking {BookingId}", bookingId);
                throw new RoomServiceException($"Failed to check cancellation for booking {bookingId}.", ex);
            }
        }

        public async Task CancelBookingAsync(int bookingId, string cancellationReason, string userEmail, bool isAdmin)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Room)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);
                if (booking == null)
                {
                    _logger.LogWarning("Booking not found: {BookingId}", bookingId);
                    throw new BookingNotFoundException($"Booking with ID {bookingId} not found.");
                }

                var (isCancellable, refundPercentage) = await CanCancelBookingAsync(bookingId, isAdmin);
                if (!isCancellable)
                {
                    _logger.LogWarning("Cancellation not allowed for booking: {BookingId}", bookingId);
                    throw new RoomValidationException("Cancellation not allowed per policy.");
                }

                booking.Status = BookingStatus.Cancelled;
                booking.CancelledAt = DateTime.UtcNow;
                booking.RefundPercentage = refundPercentage;
                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();

                // Send cancellation email
                await _emailSender.SendBookingCancellationEmailAsync(userEmail, booking, booking.Room);

                _logger.LogInformation("Booking cancelled: {BookingId}, Refund: {RefundPercentage}%", bookingId, refundPercentage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling booking {BookingId}", bookingId);
                throw new RoomServiceException($"Failed to cancel booking {bookingId}.", ex);
            }
        }

        // Additional methods leveraging the repository pattern
        public async Task<List<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut)
        {
            try
            {
                return await _roomRepository.GetAvailableRoomsAsync(checkIn, checkOut);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available rooms for {CheckIn} to {CheckOut}", checkIn, checkOut);
                throw new RoomServiceException("Failed to get available rooms.", ex);
            }
        }

        public async Task<List<Room>> GetRoomsByTypeAsync(string roomType)
        {
            try
            {
                return await _roomRepository.GetRoomsByTypeAsync(roomType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rooms by type {RoomType}", roomType);
                throw new RoomServiceException($"Failed to get rooms by type {roomType}.", ex);
            }
        }

        public async Task<List<Room>> SearchRoomsAsync(RoomSearchCriteria criteria)
        {
            try
            {
                return await _roomRepository.SearchRoomsAsync(criteria);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching rooms with criteria");
                throw new RoomServiceException("Failed to search rooms.", ex);
            }
        }
    }

    // Custom Exception Classes
    public class RoomServiceException : Exception
    {
        public RoomServiceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public class RoomNotFoundException : Exception
    {
        public RoomNotFoundException(string message) : base(message)
        {
        }
    }

    //public class BookingNotFoundException : Exception
    //{
    //    public BookingNotFoundException(string message) : base(message)
    //    {
    //    }
    //}

    public class RoomValidationException : Exception
    {
        public RoomValidationException(string message) : base(message)
        {
        }
    }
}