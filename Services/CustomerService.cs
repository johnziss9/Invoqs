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

        public async Task<(CustomerModel? Customer, ApiValidationError? ValidationErrors)> CreateCustomerAsync(CustomerModel customer)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                // Convert CustomerModel to API request format
                var requestBody = new
                {
                    name = customer.Name,
                    emails = customer.Emails.Select(e => e.Email).ToList(),
                    phone = customer.Phone,
                    companyRegistrationNumber = customer.CompanyRegistrationNumber,
                    vatNumber = customer.VatNumber,
                    notes = customer.Notes
                };

                var response = await _httpClient.PostAsJsonAsync("customers", requestBody);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    throw new UnauthorizedAccessException("Authentication required");
                }

                // Handle validation errors
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var validationErrors = JsonSerializer.Deserialize<ApiValidationError>(errorContent, _jsonOptions);
                    return (null, validationErrors);
                }

                response.EnsureSuccessStatusCode();

                var createdCustomer = await response.Content.ReadFromJsonAsync<CustomerModel>(_jsonOptions);
                return (createdCustomer, null);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error creating customer: {ex.Message}");
            }
        }

        public async Task<(CustomerModel? Customer, ApiValidationError? ValidationErrors)> UpdateCustomerAsync(CustomerModel customer)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                // Convert CustomerModel to API request format
                var requestBody = new
                {
                    name = customer.Name,
                    emails = customer.Emails.Select(e => e.Email).ToList(),
                    phone = customer.Phone,
                    companyRegistrationNumber = customer.CompanyRegistrationNumber,
                    vatNumber = customer.VatNumber,
                    notes = customer.Notes
                };

                var response = await _httpClient.PutAsJsonAsync($"customers/{customer.Id}", requestBody);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    throw new UnauthorizedAccessException("Authentication required");
                }

                // Handle validation errors
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var validationErrors = JsonSerializer.Deserialize<ApiValidationError>(errorContent, _jsonOptions);
                    return (null, validationErrors);
                }

                response.EnsureSuccessStatusCode();

                var updatedCustomer = await response.Content.ReadFromJsonAsync<CustomerModel>(_jsonOptions);
                return (updatedCustomer, null);
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

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new InvalidOperationException(errorContent);
                }

                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error deleting customer {id}: {ex.Message}");
                throw;
            }
        }

        public async Task<DuplicateCheckResponse> CheckEmailDuplicatesAsync(List<string> emails, int? excludeCustomerId = null)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var request = new DuplicateCheckRequest
                {
                    Emails = emails,
                    ExcludeCustomerId = excludeCustomerId
                };

                var response = await _httpClient.PostAsJsonAsync("customers/check-duplicates", request);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    throw new UnauthorizedAccessException("Authentication required");
                }

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<DuplicateCheckResponse>(_jsonOptions);
                return result ?? new DuplicateCheckResponse();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error checking duplicate emails: {ex.Message}");
                throw new Exception($"Error checking duplicate emails: {ex.Message}");
            }
        }
    }
}