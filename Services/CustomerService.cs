using Invoqs.Interfaces;
using Invoqs.Models;
using System.Text.Json;

namespace Invoqs.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private readonly JsonSerializerOptions _jsonOptions;

        public CustomerService(HttpClient httpClient, IAuthService authService)
        {
            _httpClient = httpClient;
            _authService = authService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<CustomerModel>> GetAllCustomersAsync()
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync("customers");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return new List<CustomerModel>();
                }

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
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync($"customers/{id}");
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return null;
                }

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
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.PostAsJsonAsync("customers", customer);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    throw new UnauthorizedAccessException("Authentication required");
                }

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
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.PutAsJsonAsync($"customers/{customer.Id}", customer);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    throw new UnauthorizedAccessException("Authentication required");
                }

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
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.DeleteAsync($"customers/{id}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return false;
                }

                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error deleting customer {id}: {ex.Message}");
                return false;
            }
        }
    }
}