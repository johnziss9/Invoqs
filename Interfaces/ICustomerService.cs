using Invoqs.Models;

namespace Invoqs.Interfaces
{
    public interface ICustomerService
    {
        Task<List<CustomerModel>> GetAllCustomersAsync();
        Task<CustomerModel?> GetCustomerByIdAsync(int id);
        Task<(CustomerModel? Customer, ApiValidationError? ValidationErrors)> CreateCustomerAsync(CustomerModel customer);
        Task<(CustomerModel? Customer, ApiValidationError? ValidationErrors)> UpdateCustomerAsync(CustomerModel customer);
        Task<bool> DeleteCustomerAsync(int id);
    }
}