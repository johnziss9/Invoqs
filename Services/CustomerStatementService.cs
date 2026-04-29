using Invoqs.Interfaces;
using Invoqs.Models;
using System.Text.Json;

namespace Invoqs.Services
{
    public class CustomerStatementService : ICustomerStatementService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private readonly JsonSerializerOptions _jsonOptions;

        public CustomerStatementService(HttpClient httpClient, IAuthService authService)
        {
            _httpClient = httpClient;
            _authService = authService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<CustomerStatementModel>> GetAllCustomerStatementsAsync()
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync("customerstatements");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return new List<CustomerStatementModel>();
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<CustomerStatementModel>>(_jsonOptions)
                    ?? new List<CustomerStatementModel>();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching customer statements: {ex.Message}");
                return new List<CustomerStatementModel>();
            }
        }

        public async Task<List<CustomerStatementModel>> GetCustomerStatementsAsync(int customerId)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync($"customerstatements/customer/{customerId}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return new List<CustomerStatementModel>();
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<CustomerStatementModel>>(_jsonOptions)
                    ?? new List<CustomerStatementModel>();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching customer statements for customer {customerId}: {ex.Message}");
                return new List<CustomerStatementModel>();
            }
        }

        public async Task<CustomerStatementModel?> GetCustomerStatementByIdAsync(int id)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync($"customerstatements/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return null;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CustomerStatementModel>(_jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching customer statement {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<CustomerStatementModel?> CreateCustomerStatementAsync(CreateCustomerStatementModel model)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.PostAsJsonAsync("customerstatements", model, _jsonOptions);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return null;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Validation error: {errorContent}");
                    return null;
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CustomerStatementModel>(_jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error creating customer statement: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeleteCustomerStatementAsync(int id)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.DeleteAsync($"customerstatements/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return false;
                }

                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error deleting customer statement {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<byte[]?> DownloadCustomerStatementPdfAsync(int statementId)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync($"customerstatements/{statementId}/pdf");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return null;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error downloading customer statement PDF: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> SendCustomerStatementAsync(int statementId, List<string> recipientEmails)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var content = JsonContent.Create(new { RecipientEmails = recipientEmails });
                var response = await _httpClient.PostAsync($"customerstatements/{statementId}/send", content);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return false;
                }

                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error sending customer statement {statementId}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> MarkCustomerStatementAsDeliveredAsync(int statementId)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.PutAsync($"customerstatements/{statementId}/mark-as-delivered", null);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return false;
                }

                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error marking customer statement {statementId} as delivered: {ex.Message}");
                return false;
            }
        }
    }
}
