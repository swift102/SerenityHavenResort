using SerenityHavenResort.Services;
using SerenityHavenResort.Data;
using SerenityHavenResort.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Cryptography;

namespace SerenityHavenResort.Services
{
    public class BookingService : IBookingService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BookingService> _logger;
        private readonly RoomService _roomService;
        private readonly PaymentService _paymentService;
        private readonly IEmailSender _emailSender;

        public BookingService(
            AppDbContext context,
            ILogger<BookingService> logger,
            RoomService roomService,
            PaymentService paymentService,
            IEmailSender emailSender)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _roomService = roomService ?? throw new ArgumentNullException(nameof(roomService));
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        }

        public async Task<Booking> CreateBookingAsync(CreateBookingRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate room availability
                var isAvailable = await _roomService.IsRoomAvailableAsync(
                    request.RoomId, request.CheckInDate, request.CheckOutDate);

                if (!isAvailable)
                {
                    throw new BookingValidationException("Room is not available for the selected dates.");
                }

                // Validate room capacity
                await _roomService.ValidateRoomCapacityAsync(request.RoomId, request.GuestCount, false);

                // Calculate pricing
                var totalPrice = await _roomService.CalculatePrice(request.RoomId, request.CheckInDate, request.CheckOutDate);

                var booking = new Booking
                {
                    RoomId = request.RoomId,
                    CustomerId = request.CustomerId,
                    CheckInDate = request.CheckInDate,
                    CheckOutDate = request.CheckOutDate,
                    TotalPrice = totalPrice,
                    BasePrice = totalPrice, // Simplified - you might want to break this down
                    GuestCount = request.GuestCount,
                    ChildrenCount = request.ChildrenCount,
                    SpecialRequests = request.SpecialRequests,
                    BookingReference = GenerateBookingReference(),
                    Status = BookingStatus.Pending
                };

                await _context.Bookings.AddAsync(booking);
                await _context.SaveChangesAsync();

                // Send confirmation email
                var customer = await _context.Customers
                    .Include(c => c.User)
                    .FirstAsync(c => c.Id == request.CustomerId);

                var room = await _context.Rooms.FindAsync(request.RoomId);

                await _emailSender.SendBookingConfirmationEmailAsync(customer.User.Email, booking, room);

                await transaction.CommitAsync();

                _logger.LogInformation("Booking created successfully: {BookingId}", booking.Id);
                return booking;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating booking for room {RoomId}", request.RoomId);
                throw;
            }
        }

        public async Task<Booking> GetBookingByIdAsync(int bookingId)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Customer)
                        .ThenInclude(c => c.User)
                    .Include(b => b.Room)
                        .ThenInclude(r => r.Images)
                    .Include(b => b.Payments)
                    .Include(b => b.AdditionalServices)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    _logger.LogWarning("Booking not found: {BookingId}", bookingId);
                    throw new BookingNotFoundException($"Booking with ID {bookingId} not found.");
                }

                return booking;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching booking {BookingId}", bookingId);
                throw new BookingServiceException($"Failed to retrieve booking {bookingId}.", ex);
            }
        }

        public async Task<IEnumerable<Booking>> GetBookingsByCustomerAsync(int customerId, int page = 1, int pageSize = 10)
        {
            try
            {
                return await _context.Bookings
                    .Where(b => b.CustomerId == customerId)
                    .Include(b => b.Room)
                        .ThenInclude(r => r.Images)
                    .Include(b => b.Payments)
                    .OrderByDescending(b => b.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bookings for customer {CustomerId}", customerId);
                throw new BookingServiceException($"Failed to retrieve bookings for customer {customerId}.", ex);
            }
        }

        public async Task<Booking> UpdateBookingAsync(int bookingId, UpdateBookingRequest request)
        {
            try
            {
                var booking = await GetBookingByIdAsync(bookingId);

                if (!booking.CanBeCancelled)
                {
                    throw new BookingValidationException("Booking cannot be modified in current status.");
                }

                // Update allowed fields
                booking.SpecialRequests = request.SpecialRequests;
                booking.GuestCount = request.GuestCount;
                booking.ChildrenCount = request.ChildrenCount;
                booking.UpdatedAt = DateTime.UtcNow;

                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Booking updated: {BookingId}", bookingId);
                return booking;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating booking {BookingId}", bookingId);
                throw new BookingServiceException($"Failed to update booking {bookingId}.", ex);
            }
        }

        public async Task CheckInAsync(int bookingId, string staffUserId)
        {
            try
            {
                var booking = await GetBookingByIdAsync(bookingId);

                if (booking.Status != BookingStatus.Confirmed)
                {
                    throw new BookingValidationException("Only confirmed bookings can be checked in.");
                }

                if (booking.CheckInDate.Date > DateTime.Today)
                {
                    throw new BookingValidationException("Cannot check in before check-in date.");
                }

                booking.Status = BookingStatus.CheckedIn;
                booking.UpdatedAt = DateTime.UtcNow;

                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Customer checked in for booking {BookingId} by staff {StaffId}", bookingId, staffUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking in booking {BookingId}", bookingId);
                throw new BookingServiceException($"Failed to check in booking {bookingId}.", ex);
            }
        }

        public async Task CheckOutAsync(int bookingId, string staffUserId)
        {
            try
            {
                var booking = await GetBookingByIdAsync(bookingId);

                if (booking.Status != BookingStatus.CheckedIn)
                {
                    throw new BookingValidationException("Only checked-in bookings can be checked out.");
                }

                booking.Status = BookingStatus.CheckedOut;
                booking.UpdatedAt = DateTime.UtcNow;

                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Customer checked out for booking {BookingId} by staff {StaffId}", bookingId, staffUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking out booking {BookingId}", bookingId);
                throw new BookingServiceException($"Failed to check out booking {bookingId}.", ex);
            }
        }

       


        private static string GenerateBookingReference()
        {
            using var rng = RandomNumberGenerator.Create();
            byte[] bytes = new byte[6];
            rng.GetBytes(bytes);
            return $"BK{DateTime.Now:yyyyMMdd}{Convert.ToHexString(bytes)}";
        }

        public async Task<Booking> CancelBookingAsync(int bookingId, string reason = null)
        {
            try
            {
                var booking = await GetBookingByIdAsync(bookingId);

                if (!booking.CanBeCancelled)
                {
                    throw new BookingValidationException("Booking cannot be cancelled in current status.");
                }

                booking.Status = BookingStatus.Cancelled;
                booking.CancelledAt = DateTime.UtcNow;
                booking.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(reason))
                {
                    booking.InternalNotes = $"Cancelled: {reason}";
                }

                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Booking cancelled: {BookingId}", bookingId);
                return booking;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling booking {BookingId}", bookingId);
                throw new BookingServiceException($"Failed to cancel booking {bookingId}.", ex);
            }
        }

        public async Task<Booking> ConfirmBookingAsync(int bookingId)
        {
            try
            {
                var booking = await GetBookingByIdAsync(bookingId);

                if (booking.Status != BookingStatus.Pending)
                {
                    throw new BookingValidationException("Only pending bookings can be confirmed.");
                }

                booking.Status = BookingStatus.Confirmed;
                booking.UpdatedAt = DateTime.UtcNow;

                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Booking confirmed: {BookingId}", bookingId);
                return booking;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming booking {BookingId}", bookingId);
                throw new BookingServiceException($"Failed to confirm booking {bookingId}.", ex);
            }
        }

        // Essential for hotel operations - get bookings by status
        public async Task<IEnumerable<Booking>> GetBookingsByStatusAsync(BookingStatus status, int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.Bookings
                    .Where(b => b.Status == status)
                    .Include(b => b.Customer)
                        .ThenInclude(c => c.User)
                    .Include(b => b.Room)
                    .OrderBy(b => b.CheckInDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bookings by status {Status}", status);
                throw new BookingServiceException($"Failed to retrieve bookings by status {status}.", ex);
            }
        }

        // Essential for hotel operations - get today's arrivals/departures
        public async Task<IEnumerable<Booking>> GetTodaysCheckInsAsync()
        {
            try
            {
                var today = DateTime.Today;
                return await _context.Bookings
                    .Where(b => b.CheckInDate.Date == today && b.Status == BookingStatus.Confirmed)
                    .Include(b => b.Customer)
                        .ThenInclude(c => c.User)
                    .Include(b => b.Room)
                    .OrderBy(b => b.Room.RoomNumber)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching today's check-ins");
                throw new BookingServiceException("Failed to retrieve today's check-ins.", ex);
            }
        }

        public async Task<IEnumerable<Booking>> GetTodaysCheckOutsAsync()
        {
            try
            {
                var today = DateTime.Today;
                return await _context.Bookings
                    .Where(b => b.CheckOutDate.Date == today && b.Status == BookingStatus.CheckedIn)
                    .Include(b => b.Customer)
                        .ThenInclude(c => c.User)
                    .Include(b => b.Room)
                    .OrderBy(b => b.Room.RoomNumber)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching today's check-outs");
                throw new BookingServiceException("Failed to retrieve today's check-outs.", ex);
            }
        }

        // Essential for hotel operations - get current guests
        public async Task<IEnumerable<Booking>> GetCurrentGuestsAsync()
        {
            try
            {
                return await _context.Bookings
                    .Where(b => b.Status == BookingStatus.CheckedIn)
                    .Include(b => b.Customer)
                        .ThenInclude(c => c.User)
                    .Include(b => b.Room)
                    .OrderBy(b => b.Room.RoomNumber)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching current guests");
                throw new BookingServiceException("Failed to retrieve current guests.", ex);
            }
        }

        // Essential for finding bookings
        public async Task<Booking?> GetBookingByReferenceAsync(string bookingReference)
        {
            try
            {
                return await _context.Bookings
                    .Include(b => b.Customer)
                        .ThenInclude(c => c.User)
                    .Include(b => b.Room)
                        .ThenInclude(r => r.Images)
                    .Include(b => b.Payments)
                    .FirstOrDefaultAsync(b => b.BookingReference == bookingReference);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching booking by reference {Reference}", bookingReference);
                throw new BookingServiceException($"Failed to retrieve booking by reference {bookingReference}.", ex);
            }
        }

    }

    // Request DTOs
    public class CreateBookingRequest
    {
        public int RoomId { get; set; }
        public int CustomerId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int GuestCount { get; set; } = 1;
        public int ChildrenCount { get; set; } = 0;
        public string? SpecialRequests { get; set; }
    }

    public class UpdateBookingRequest
    {
        public int GuestCount { get; set; }
        public int ChildrenCount { get; set; }
        public string? SpecialRequests { get; set; }
    }

    // Interface
    public interface IBookingService
    {
        Task<Booking> CreateBookingAsync(CreateBookingRequest request);
        Task<Booking> GetBookingByIdAsync(int bookingId);
        Task<Booking?> GetBookingByReferenceAsync(string bookingReference);
        Task<IEnumerable<Booking>> GetBookingsByCustomerAsync(int customerId, int page = 1, int pageSize = 10);
        Task<IEnumerable<Booking>> GetBookingsByStatusAsync(BookingStatus status, int page = 1, int pageSize = 20);
        Task<Booking> UpdateBookingAsync(int bookingId, UpdateBookingRequest request);
        Task<Booking> ConfirmBookingAsync(int bookingId);
        Task<Booking> CancelBookingAsync(int bookingId, string reason = null);
        Task CheckInAsync(int bookingId, string staffUserId);
        Task CheckOutAsync(int bookingId, string staffUserId);

        // Hotel operations methods
        Task<IEnumerable<Booking>> GetTodaysCheckInsAsync();
        Task<IEnumerable<Booking>> GetTodaysCheckOutsAsync();
        Task<IEnumerable<Booking>> GetCurrentGuestsAsync();
    }

    // Exceptions
    public class BookingServiceException : Exception
    {
        public BookingServiceException(string message) : base(message) { }
        public BookingServiceException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class BookingValidationException : Exception
    {
        public BookingValidationException(string message) : base(message) { }
    }

    public class BookingNotFoundException : Exception
    {
        public BookingNotFoundException(string message) : base(message) { }
    }
}
