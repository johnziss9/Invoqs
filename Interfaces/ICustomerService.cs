using Invoqs.Models;

namespace Invoqs.Interfaces
{
    public interface ICustomerService
    {
        Task<List<CustomerModel>> GetAllCustomersAsync();
        Task<CustomerModel?> GetCustomerByIdAsync(int id);
        Task<CustomerModel> CreateCustomerAsync(CustomerModel customer);
        Task<CustomerModel> UpdateCustomerAsync(CustomerModel customer);
        Task<bool> DeleteCustomerAsync(int id);
    }
}