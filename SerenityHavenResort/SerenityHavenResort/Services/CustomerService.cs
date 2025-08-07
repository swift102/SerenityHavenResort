using AngleSharp.Text;
using SerenityHavenResort.DTOs;
using SerenityHavenResort.Models;
using SerenityHavenResort.Controllers;
using SerenityHavenResort.Data;
using SerenityHavenResort.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;


namespace SerenityHavenResort.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CustomerService> _logger;
        private readonly UserManager<User> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<Customer> _repository;


        public CustomerService(AppDbContext context, ILogger<CustomerService> logger, UserManager<User> userManager, IUnitOfWork unitOfWork, IRepository<Customer> repository) 
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Customer> GetCustomerByIdAsync(int id)
        {
            try
            {
                var customer = await _context.Customers
                    .Include(c => c.User) // Include User for computed properties
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for ID {CustomerId}", id);
                    throw new CustomerNotFoundException($"Customer with ID {id} not found.");
                }
                return customer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customer for ID {CustomerId}", id);
                throw new CustomerServiceException($"Failed to fetch customer for ID {id}.", ex);
            }
        }

        public async Task<Customer> GetCustomerByUserIdAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("Invalid UserID provided for customer lookup.");
                    throw new CustomerValidationException("UserId is required.");
                }

                var customer = await _context.Customers
                    .Include(c => c.User) // Include User for computed properties
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for UserID {UserId}", userId);
                    throw new CustomerNotFoundException($"Customer with UserId {userId} not found.");
                }

                return customer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customer for UserID {UserId}", userId);
                throw new CustomerServiceException($"Failed to fetch customer for UserID {userId}.", ex);
            }
        }

        public async Task<Customer> GetCustomerByUserProfileIdAsync(int userProfileId)
        {
            try
            {
                var customer = await _context.Customers
                    .Include(c => c.User) // Include User for computed properties
                    .FirstOrDefaultAsync(c => c.Id == userProfileId);

                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for ID {UserProfileID}", userProfileId);
                    throw new CustomerNotFoundException($"Customer with UserProfileID {userProfileId} not found.");
                }

                return customer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customer for UserProfileID {UserProfileID}", userProfileId);
                throw new CustomerServiceException($"Failed to fetch customer for UserProfileID {userProfileId}.", ex);
            }
        }

        public async Task<Customer> GetCustomerByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("Invalid email provided for customer lookup.");
                    throw new CustomerValidationException("Email is required.");
                }

                var customer = await _context.Customers
                    .Include(c => c.User) // Include the User navigation property
                    .FirstOrDefaultAsync(c => c.User.Email == email); // Query through User entity

                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for email {Email}", email);
                    return null;
                }
                return customer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customer for email {Email}", email);
                throw new CustomerServiceException($"Failed to fetch customer for email {email}.", ex);
            }
        }

        public async Task<IEnumerable<Customer>> GetCustomersAsync(int page, int pageSize)
        {
            try
            {
                if (page < 1 || pageSize < 1)
                {
                    _logger.LogWarning("Invalid pagination parameters: page={Page}, pageSize={PageSize}", page, pageSize);
                    throw new CustomerValidationException("Page and page size must be positive.");
                }

                var customers = await _context.Customers
                    .Include(c => c.User) // Include User for computed properties
                    .OrderBy(c => c.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                return customers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customers, page {Page}, pageSize {PageSize}", page, pageSize);
                throw new CustomerServiceException($"Failed to fetch customers for page {page}.", ex);
            }
        }

        public async Task<Customer> AddCustomerAsync(Customer customer)
        {
            try
            {
                if (customer == null)
                {
                    _logger.LogWarning("Attempted to add null customer.");
                    throw new ArgumentNullException(nameof(customer));
                }

                // Validate that User exists and has required data
                var user = await _context.Users.FindAsync(customer.UserID);
                if (user == null)
                {
                    _logger.LogWarning("User not found for UserID {UserId}", customer.UserID);
                    throw new CustomerValidationException($"User with ID {customer.UserID} not found.");
                }

                // Validate User has required data
                if (string.IsNullOrWhiteSpace(user.FirstName) ||
                    string.IsNullOrWhiteSpace(user.LastName) ||
                    string.IsNullOrWhiteSpace(user.Email) ||
                    string.IsNullOrWhiteSpace(user.ContactNumber))
                {
                    _logger.LogWarning("User data incomplete for UserID {UserId}", customer.UserID);
                    throw new CustomerValidationException("User must have first name, last name, email, and phone number.");
                }

                if (!IsValidEmail(user.Email))
                {
                    _logger.LogWarning("Invalid email address for UserID {UserId}: {Email}", customer.UserID, user.Email);
                    throw new CustomerValidationException("User has invalid email address.");
                }

                // Check for existing customer with this UserID
                var existingCustomer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserID == customer.UserID);
                if (existingCustomer != null)
                {
                    _logger.LogWarning("Customer already exists for UserId {UserId}", customer.UserID);
                    throw new DuplicateCustomerException($"Customer with UserId {customer.UserID} already exists.");
                }

                await _context.Customers.AddAsync(customer);
                await _context.SaveChangesAsync();

                // Reload with User for computed properties
                var savedCustomer = await _context.Customers
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == customer.Id);

                _logger.LogInformation("Customer created: Id={CustomerId}, Email={Email}",
                    savedCustomer!.Id, savedCustomer.Email);

                return savedCustomer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding customer with UserID {UserID}", customer?.UserID);
                throw new CustomerServiceException($"Failed to add customer.", ex);
            }
        }

        public async Task<Customer> UpdateCustomerAsync(Customer customer)
        {
            try
            {
                if (customer == null)
                {
                    _logger.LogWarning("Attempted to update null customer.");
                    throw new ArgumentNullException(nameof(customer));
                }

                var existing = await _context.Customers
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == customer.Id);

                if (existing == null)
                {
                    _logger.LogWarning("Customer not found for Id {CustomerId}", customer.Id);
                    throw new CustomerNotFoundException($"Customer with ID {customer.Id} not found.");
                }

                // Update only Customer-specific properties (NOT User properties)
                existing.DateOfBirth = customer.DateOfBirth;
                existing.Nationality = customer.Nationality;
                existing.PassportNumber = customer.PassportNumber;
                existing.IdNumber = customer.IdNumber;
                existing.Address = customer.Address;
                existing.City = customer.City;
                existing.Country = customer.Country;
                existing.PostalCode = customer.PostalCode;
                existing.DietaryRestrictions = customer.DietaryRestrictions;
                existing.RoomPreferences = customer.RoomPreferences;
                existing.IsVip = customer.IsVip;
                existing.LoyaltyTier = customer.LoyaltyTier;
                existing.LoyaltyPoints = customer.LoyaltyPoints;
                existing.LoyaltyMembershipNumber = customer.LoyaltyMembershipNumber;
                existing.EmergencyContactName = customer.EmergencyContactName;
                existing.EmergencyContactPhone = customer.EmergencyContactPhone;
                existing.EmergencyContactRelationship = customer.EmergencyContactRelationship;
                existing.CompanyName = customer.CompanyName;
                existing.CompanyPosition = customer.CompanyPosition;
                existing.UpdatedAt = DateTime.UtcNow;

                // DO NOT update UserID to prevent reassociation

                _context.Customers.Update(existing);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Customer updated: Id={CustomerId}, Email={Email}", existing.Id, existing.Email);
                return existing;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer with ID {CustomerId}", customer.Id);
                throw new CustomerServiceException($"Failed to update customer with ID {customer.Id}.", ex);
            }
        }

        public async Task DeleteCustomerAsync(int id)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(id);
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for Id {CustomerId}", id);
                    throw new CustomerNotFoundException($"Customer with ID {id} not found.");
                }

                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Customer deleted: Id={CustomerId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer with ID {CustomerId}", id);
                throw new CustomerServiceException($"Failed to delete customer with ID {id}.", ex);
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // Helper methods to work with your DTOs
        public CustomerReadDTO MapToReadDTO(Customer customer)
        {
            return new CustomerReadDTO
            {
                Id = customer.Id,
                FirstName = customer.FirstName, // Uses computed property
                LastName = customer.LastName,   // Uses computed property
                Email = customer.Email,         // Uses computed property
                Phone = customer.Phone,         // Uses computed property
                UserID = customer.UserID,
                IsVip = customer.IsVip,
                LoyaltyTier = customer.LoyaltyTier,
                LoyaltyPoints = customer.LoyaltyPoints,
                CreatedAt = customer.CreatedAt
            };
        }

        public Customer MapFromCreateDTO(CustomerCreateDTO dto)
        {
            return new Customer
            {
                UserID = dto.UserID,
                DateOfBirth = dto.DateOfBirth,
                Nationality = dto.Nationality,
                Address = dto.Address,
                City = dto.City,
                Country = dto.Country,
                IsVip = dto.IsVip,
                LoyaltyTier = dto.LoyaltyTier,
                LoyaltyPoints = dto.LoyaltyPoints,
                DietaryRestrictions = dto.DietaryRestrictions,
                RoomPreferences = dto.RoomPreferences,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

      
            public async Task<Customer> CreateCustomerWithUserAsync(CreateDTO customerDto)
            {
                // First create the User
                var user = new User
                {
                    FirstName = customerDto.FirstName,
                    LastName = customerDto.LastName,
                    Email = customerDto.Email,
                    UserName = customerDto.Email, // Use email as username
                    ContactNumber = customerDto.Phone,
                    UserType = UserType.Guest,
                    EmailConfirmed = true 
                };

                // Use UserManager to create user (assuming you have access to UserManager)
                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    throw new CustomerServiceException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                // Now create the Customer with the User's ID
                var customer = new Customer
                {
                    UserID = user.Id,
                    // Don't set computed properties - they come from the User
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Save customer to database
                return await _repository.AddAsync(customer);
            }
        
    }

    // Exception classes
    public class CustomerServiceException : Exception
    {
        public CustomerServiceException(string message) : base(message) { }
        public CustomerServiceException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class CustomerValidationException : Exception
    {
        public CustomerValidationException(string message) : base(message) { }
    }

    public class DuplicateCustomerException : Exception
    {
        public DuplicateCustomerException(string message) : base(message) { }
    }

    public class CustomerNotFoundException : Exception
    {
        public CustomerNotFoundException(string message) : base(message) { }
    }

    public class CreateDTO
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string UserID { get; set; } = string.Empty;
    }
}