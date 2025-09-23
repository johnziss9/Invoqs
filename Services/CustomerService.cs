using Invoqs.Interfaces;
using Invoqs.Models;
using System.Text.Json;

namespace Invoqs.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public CustomerService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<CustomerModel>> GetAllCustomersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("customers");
                response.EnsureSuccessStatusCode();
                
                var customers = await response.Content.ReadFromJsonAsync<List<CustomerModel>>(_jsonOptions);
                return customers ?? new List<CustomerModel>();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching customers: {ex.Message}");
                return new List<CustomerModel>();
            }
        }

        public async Task<CustomerModel?> GetCustomerByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"customers/{id}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;
                    
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CustomerModel>(_jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching customer {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<CustomerModel> CreateCustomerAsync(CustomerModel customer)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("customers", customer);
                response.EnsureSuccessStatusCode();
                
                var createdCustomer = await response.Content.ReadFromJsonAsync<CustomerModel>(_jsonOptions);
                return createdCustomer!;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error creating customer: {ex.Message}");
            }
        }

        public async Task<CustomerModel> UpdateCustomerAsync(CustomerModel customer)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"customers/{customer.Id}", customer);
                response.EnsureSuccessStatusCode();
                
                var updatedCustomer = await response.Content.ReadFromJsonAsync<CustomerModel>(_jsonOptions);
                return updatedCustomer!;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error updating customer: {ex.Message}");
            }
        }

        public async Task<bool> DeleteCustomerAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"customers/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error deleting customer {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CustomerExistsAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"customers/{id}/exists");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<bool>(_jsonOptions);
                }
                return false;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error checking customer existence {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<List<CustomerModel>> SearchCustomersAsync(string searchTerm)
        {
            try
            {
                var response = await _httpClient.GetAsync($"customers/search?term={Uri.EscapeDataString(searchTerm)}");
                response.EnsureSuccessStatusCode();
                
                var customers = await response.Content.ReadFromJsonAsync<List<CustomerModel>>(_jsonOptions);
                return customers ?? new List<CustomerModel>();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error searching customers: {ex.Message}");
                return new List<CustomerModel>();
            }
        }
    }
}