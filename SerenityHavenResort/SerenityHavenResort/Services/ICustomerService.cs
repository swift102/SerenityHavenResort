using SerenityHavenResort.Models;
using System.Threading.Tasks;


namespace SerenityHavenResort.Services
{
    public interface ICustomerService
    {
        Task<Customer> GetCustomerByIdAsync(int id);
        Task<Customer> GetCustomerByEmailAsync(string email);
        Task<IEnumerable<Customer>> GetCustomersAsync(int page, int pageSize);
        Task<Customer> AddCustomerAsync(Customer customer);
        Task<Customer> UpdateCustomerAsync(Customer customer);
        Task DeleteCustomerAsync(int id);
        Task<Customer> GetCustomerByUserProfileIdAsync(int userProfileId);
        Task<Customer> GetCustomerByUserIdAsync(string userId);
    }
}
