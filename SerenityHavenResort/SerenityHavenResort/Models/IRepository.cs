using SerenityHavenResort.Models;
using SerenityHavenResort.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SerenityHavenResort.Models
{

    public interface IRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync(int page = 1, int pageSize = 50);
        Task<T?> GetByIdAsync(int id);
        Task<T> AddAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);
    }

    // Base Generic Repository Implementation
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;
        protected readonly ILogger<Repository<T>> _logger;

        public Repository(AppDbContext context, ILogger<Repository<T>> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = context.Set<T>();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync(int page = 1, int pageSize = 50)
        {
            try
            {
                return await _dbSet
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all {EntityType}", typeof(T).Name);
                throw new RepositoryException($"Failed to retrieve {typeof(T).Name} entities.", ex);
            }
        }

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            try
            {
                return await _dbSet.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving {EntityType} with ID {Id}", typeof(T).Name, id);
                throw new RepositoryException($"Failed to retrieve {typeof(T).Name} with ID {id}.", ex);
            }
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            try
            {
                await _dbSet.AddAsync(entity);
                await _context.SaveChangesAsync();
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding {EntityType}", typeof(T).Name);
                throw new RepositoryException($"Failed to add {typeof(T).Name}.", ex);
            }
        }

        public virtual async Task<T> UpdateAsync(T entity)
        {
            try
            {
                _dbSet.Update(entity);
                await _context.SaveChangesAsync();
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating {EntityType}", typeof(T).Name);
                throw new RepositoryException($"Failed to update {typeof(T).Name}.", ex);
            }
        }

        public virtual async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var entity = await _dbSet.FindAsync(id);
                if (entity == null)
                    return false;

                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting {EntityType} with ID {Id}", typeof(T).Name, id);
                throw new RepositoryException($"Failed to delete {typeof(T).Name} with ID {id}.", ex);
            }
        }

        public virtual async Task<bool> ExistsAsync(int id)
        {
            try
            {
                return await _dbSet.FindAsync(id) != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence of {EntityType} with ID {Id}", typeof(T).Name, id);
                throw new RepositoryException($"Failed to check existence of {typeof(T).Name} with ID {id}.", ex);
            }
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                return await _dbSet.Where(predicate).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding {EntityType} with predicate", typeof(T).Name);
                throw new RepositoryException($"Failed to find {typeof(T).Name} entities.", ex);
            }
        }

        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                return await _dbSet.FirstOrDefaultAsync(predicate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding first {EntityType} with predicate", typeof(T).Name);
                throw new RepositoryException($"Failed to find {typeof(T).Name}.", ex);
            }
        }

        public virtual async Task<int> CountAsync()
        {
            try
            {
                return await _dbSet.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting {EntityType}", typeof(T).Name);
                throw new RepositoryException($"Failed to count {typeof(T).Name} entities.", ex);
            }
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                return await _dbSet.CountAsync(predicate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting {EntityType} with predicate", typeof(T).Name);
                throw new RepositoryException($"Failed to count {typeof(T).Name} entities.", ex);
            }
        }
    }

    // FIXED: Enhanced Room Repository Interface
    public interface IRoomRepository : IRepository<Room>
    {
        // Basic room operations
        Task<IEnumerable<Room>> GetRoomsAsync(); // Added missing method
        Task<Room?> GetRoomByIdAsync(int id); // Added missing method

        // Availability and search operations
        Task<List<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut);
        Task<List<Room>> GetRoomsByTypeAsync(string roomType);
        Task<List<Room>> GetRoomsWithAmenitiesAsync();
        Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut);
        Task<Room?> GetRoomWithDetailsAsync(int id);
        Task<List<Room>> SearchRoomsAsync(RoomSearchCriteria criteria);
    }

    // FIXED: Enhanced Booking Repository Interface
    public interface IBookingRepository : IRepository<Booking>
    {
        Task<List<Booking>> GetBookingsByCustomerAsync(int customerId);
        Task<List<Booking>> GetBookingsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<Booking>> GetActiveBookingsAsync();
        Task<Booking?> GetBookingWithDetailsAsync(int id);
        Task<List<Booking>> GetBookingsByRoomAsync(int roomId);
        Task<List<Booking>> GetBookingsByStatusAsync(BookingStatus status);
        Task<Booking?> GetBookingByIdAsync(int id);
    }

    public interface ICustomerRepository : IRepository<Customer>
    {
        Task<Customer?> GetCustomerByUserIdAsync(string userId);
        Task<Customer?> GetCustomerByEmailAsync(string email);
        Task<List<Customer>> GetVipCustomersAsync();
        Task<Customer?> GetCustomerWithBookingsAsync(int customerId);
    }


    // Specific Repository Implementations
    public class RoomRepository : Repository<Room>, IRoomRepository
    {
        public RoomRepository(AppDbContext context, ILogger<RoomRepository> logger)
             : base(context, logger) { }

        // ADDED: Missing basic methods
        public async Task<IEnumerable<Room>> GetRoomsAsync()
        {
            try
            {
                return await _dbSet
                    .Include(r => r.Amenities)
                    .Include(r => r.Images)
                    .Where(r => r.IsAvailable)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all rooms");
                throw new RepositoryException("Failed to retrieve rooms.", ex);
            }
        }

        public async Task<Room?> GetRoomByIdAsync(int id)
        {
            try
            {
                return await _dbSet
                    .Include(r => r.Amenities)
                    .Include(r => r.Images)
                    .FirstOrDefaultAsync(r => r.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving room with ID {Id}", id);
                throw new RepositoryException($"Failed to retrieve room with ID {id}.", ex);
            }
        }

        // Existing methods remain the same...
        public async Task<List<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut)
        {
            try
            {
                return await _dbSet
                    .Where(r => r.IsAvailable &&
                               !r.Bookings.Any(b =>
                                   b.Status != BookingStatus.Cancelled &&
                                   b.Status != BookingStatus.NoShow &&
                                   ((checkIn >= b.CheckInDate && checkIn < b.CheckOutDate) ||
                                    (checkOut > b.CheckInDate && checkOut <= b.CheckOutDate) ||
                                    (checkIn <= b.CheckInDate && checkOut >= b.CheckOutDate))))
                    .Include(r => r.Amenities)
                    .Include(r => r.Images)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available rooms for {CheckIn} to {CheckOut}", checkIn, checkOut);
                throw new RepositoryException("Failed to get available rooms.", ex);
            }
        }

        public async Task<List<Room>> GetRoomsByTypeAsync(string roomType)
        {
            try
            {
                return await _dbSet
                    .Where(r => r.RoomType == roomType && r.IsAvailable)
                    .Include(r => r.Amenities)
                    .Include(r => r.Images)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rooms by type {RoomType}", roomType);
                throw new RepositoryException($"Failed to get rooms by type {roomType}.", ex);
            }
        }

        public async Task<List<Room>> GetRoomsWithAmenitiesAsync()
        {
            try
            {
                return await _dbSet
                    .Include(r => r.Amenities)
                    .Include(r => r.Images)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rooms with amenities");
                throw new RepositoryException("Failed to get rooms with amenities.", ex);
            }
        }

        public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut)
        {
            try
            {
                var conflictingBookings = await _context.Bookings
                    .Where(b => b.RoomId == roomId &&
                               b.Status != BookingStatus.Cancelled &&
                               b.Status != BookingStatus.NoShow &&
                               ((checkIn >= b.CheckInDate && checkIn < b.CheckOutDate) ||
                                (checkOut > b.CheckInDate && checkOut <= b.CheckOutDate) ||
                                (checkIn <= b.CheckInDate && checkOut >= b.CheckOutDate)))
                    .AnyAsync();

                return !conflictingBookings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking room availability for room {RoomId}", roomId);
                throw new RepositoryException($"Failed to check availability for room {roomId}.", ex);
            }
        }

        public async Task<Room?> GetRoomWithDetailsAsync(int id)
        {
            try
            {
                return await _dbSet
                    .Include(r => r.Amenities)
                    .Include(r => r.Images)
                    .Include(r => r.Bookings.Where(b =>
                        b.Status == BookingStatus.CheckedIn ||
                        b.Status == BookingStatus.Confirmed))
                    .FirstOrDefaultAsync(r => r.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting room details for room {RoomId}", id);
                throw new RepositoryException($"Failed to get room details for room {id}.", ex);
            }
        }

        public async Task<List<Room>> SearchRoomsAsync(RoomSearchCriteria criteria)
        {
            try
            {
                var query = _dbSet.AsQueryable();

                if (criteria.CheckIn.HasValue && criteria.CheckOut.HasValue)
                {
                    query = query.Where(r => r.IsAvailable &&
                        !r.Bookings.Any(b =>
                            b.Status != BookingStatus.Cancelled &&
                            b.Status != BookingStatus.NoShow &&
                            ((criteria.CheckIn >= b.CheckInDate && criteria.CheckIn < b.CheckOutDate) ||
                             (criteria.CheckOut > b.CheckInDate && criteria.CheckOut <= b.CheckOutDate) ||
                             (criteria.CheckIn <= b.CheckInDate && criteria.CheckOut >= b.CheckOutDate))));
                }

                if (!string.IsNullOrEmpty(criteria.RoomType))
                    query = query.Where(r => r.RoomType == criteria.RoomType);

                if (criteria.MinCapacity.HasValue)
                    query = query.Where(r => r.Capacity >= criteria.MinCapacity);

                if (criteria.MaxPrice.HasValue)
                    query = query.Where(r => r.CurrentPrice <= criteria.MaxPrice);

                if (criteria.AmenityIds?.Any() == true)
                    query = query.Where(r => r.Amenities.Any(a => criteria.AmenityIds.Contains(a.Id)));

                return await query
                    .Include(r => r.Amenities)
                    .Include(r => r.Images)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching rooms with criteria");
                throw new RepositoryException("Failed to search rooms.", ex);
            }
        }
    }

    public class BookingRepository : Repository<Booking>, IBookingRepository
    {
        public BookingRepository(AppDbContext context, ILogger<Repository<Booking>> logger)
            : base(context, logger) { }

        public async Task<List<Booking>> GetBookingsByCustomerAsync(int customerId)
        {
            try
            {
                return await _dbSet
                    .Where(b => b.CustomerId == customerId)
                    .Include(b => b.Room)
                    .Include(b => b.Payments)
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bookings for customer {CustomerId}", customerId);
                throw new RepositoryException($"Failed to get bookings for customer {customerId}.", ex);
            }
        }

        public async Task<List<Booking>> GetBookingsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _dbSet
                    .Where(b => b.CheckInDate >= startDate && b.CheckInDate <= endDate)
                    .Include(b => b.Customer)
                        .ThenInclude(c => c.User)
                    .Include(b => b.Room)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bookings for date range {StartDate} to {EndDate}", startDate, endDate);
                throw new RepositoryException("Failed to get bookings for date range.", ex);
            }
        }

        public async Task<List<Booking>> GetActiveBookingsAsync()
        {
            try
            {
                return await _dbSet
                    .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn)
                    .Include(b => b.Customer)
                        .ThenInclude(c => c.User)
                    .Include(b => b.Room)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active bookings");
                throw new RepositoryException("Failed to get active bookings.", ex);
            }
        }

        public async Task<Booking?> GetBookingWithDetailsAsync(int id)
        {
            try
            {
                return await _dbSet
                    .Include(b => b.Customer)
                        .ThenInclude(c => c.User)
                    .Include(b => b.Room)
                        .ThenInclude(r => r.Amenities)
                    .Include(b => b.Room)
                        .ThenInclude(r => r.Images)
                    .Include(b => b.Payments)
                    .Include(b => b.AdditionalServices)
                    .FirstOrDefaultAsync(b => b.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting booking details for booking {BookingId}", id);
                throw new RepositoryException($"Failed to get booking details for booking {id}.", ex);
            }
        }

        public async Task<List<Booking>> GetBookingsByRoomAsync(int roomId)
        {
            try
            {
                return await _dbSet
                    .Where(b => b.RoomId == roomId)
                    .Include(b => b.Customer)
                        .ThenInclude(c => c.User)
                    .OrderByDescending(b => b.CheckInDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bookings for room {RoomId}", roomId);
                throw new RepositoryException($"Failed to get bookings for room {roomId}.", ex);
            }
        }

        public async Task<List<Booking>> GetBookingsByStatusAsync(BookingStatus status)
        {
            try
            {
                return await _dbSet
                    .Where(b => b.Status == status)
                    .Include(b => b.Customer)
                        .ThenInclude(c => c.User)
                    .Include(b => b.Room)
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bookings by status {Status}", status);
                throw new RepositoryException($"Failed to get bookings by status {status}.", ex);
            }
        }


        public async Task<Booking?> GetBookingByIdAsync(int id)
        {
            try
            {
                return await _dbSet
                    .Include(b => b.Customer)
                        .ThenInclude(c => c.User)
                    .Include(b => b.Room)
                    .Include(b => b.Payments)
                    .FirstOrDefaultAsync(b => b.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching booking by ID {BookingId}", id);
                throw new RepositoryException($"Failed to fetch booking by ID {id}.", ex);
            }
        }

    }

    // Supporting Classes
    public class RoomSearchCriteria
    {
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public string? RoomType { get; set; }
        public int? MinCapacity { get; set; }
        public decimal? MaxPrice { get; set; }
        public List<int>? AmenityIds { get; set; }
        public string? ViewType { get; set; }
        public bool? HasBalcony { get; set; }
        public bool? IsAccessible { get; set; }
        public bool? AllowsPets { get; set; }
    }

    public class CustomerRepository : Repository<Customer>, ICustomerRepository
    {
        public CustomerRepository(AppDbContext context, ILogger<Repository<Customer>> logger)
            : base(context, logger) { }

        public async Task<Customer?> GetCustomerByUserIdAsync(string userId)
        {
            try
            {
                return await _dbSet
                    .Include(c => c.User)
                    .Include(c => c.Preferences)
                    .FirstOrDefaultAsync(c => c.UserID == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer by user ID {UserId}", userId);
                throw new RepositoryException($"Failed to get customer by user ID {userId}.", ex);
            }
        }

        public async Task<Customer?> GetCustomerByEmailAsync(string email)
        {
            try
            {
                return await _dbSet
                    .Include(c => c.User)
                    .Include(c => c.Preferences)
                    .FirstOrDefaultAsync(c => c.User.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer by email {Email}", email);
                throw new RepositoryException($"Failed to get customer by email {email}.", ex);
            }
        }

        public async Task<List<Customer>> GetVipCustomersAsync()
        {
            try
            {
                return await _dbSet
                    .Where(c => c.IsVip)
                    .Include(c => c.User)
                    .OrderByDescending(c => c.TotalSpent)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting VIP customers");
                throw new RepositoryException("Failed to get VIP customers.", ex);
            }
        }

        public async Task<Customer?> GetCustomerWithBookingsAsync(int customerId)
        {
            try
            {
                return await _dbSet
                    .Include(c => c.User)
                    .Include(c => c.Bookings)
                        .ThenInclude(b => b.Room)
                    .Include(c => c.Preferences)
                    .Include(c => c.Notes)
                    .FirstOrDefaultAsync(c => c.Id == customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer with bookings for customer {CustomerId}", customerId);
                throw new RepositoryException($"Failed to get customer with bookings for customer {customerId}.", ex);
            }
        }
    }

    // Unit of Work Pattern
    public interface IUnitOfWork : IDisposable
    {
        IRoomRepository Rooms { get; }
        IBookingRepository Bookings { get; }
        ICustomerRepository Customers { get; }
        IRepository<Payment> Payments { get; }
        IRepository<Amenities> Amenities { get; }
        IRepository<Image> Images { get; }
        IRepository<UserProfile> UserProfiles { get; }
        IRepository<Employee> Employees { get; }

        Task<int> SaveChangesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }

    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UnitOfWork> _logger;

        public IRoomRepository Rooms { get; private set; }
        public IBookingRepository Bookings { get; private set; }
        public ICustomerRepository Customers { get; private set; }
        public IRepository<Payment> Payments { get; private set; }
        public IRepository<Amenities> Amenities { get; private set; }
        public IRepository<Image> Images { get; private set; }
        public IRepository<UserProfile> UserProfiles { get; private set; }
        public IRepository<Employee> Employees { get; private set; }

        public UnitOfWork(AppDbContext context, ILogger<UnitOfWork> logger, ILoggerFactory loggerFactory)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize repositories
            Rooms = new RoomRepository(_context, loggerFactory.CreateLogger<RoomRepository>());
            Bookings = new BookingRepository(_context, loggerFactory.CreateLogger<Repository<Booking>>());
            Customers = new CustomerRepository(_context, loggerFactory.CreateLogger<Repository<Customer>>());
            Payments = new Repository<Payment>(_context, loggerFactory.CreateLogger<Repository<Payment>>());
            Amenities = new Repository<Amenities>(_context, loggerFactory.CreateLogger<Repository<Amenities>>());
            Images = new Repository<Image>(_context, loggerFactory.CreateLogger<Repository<Image>>());
            UserProfiles = new Repository<UserProfile>(_context, loggerFactory.CreateLogger<Repository<UserProfile>>());
            Employees = new Repository<Employee>(_context, loggerFactory.CreateLogger<Repository<Employee>>());
        }

        public async Task<int> SaveChangesAsync()
        {
            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes to database");
                throw new RepositoryException("Failed to save changes to database.", ex);
            }
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            try
            {
                return await _context.Database.BeginTransactionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting database transaction");
                throw new RepositoryException("Failed to start database transaction.", ex);
            }
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }

    // Exception Classes
    public class RepositoryException : Exception
    {
        public RepositoryException(string message) : base(message) { }
        public RepositoryException(string message, Exception innerException) : base(message, innerException) { }
    }
}
