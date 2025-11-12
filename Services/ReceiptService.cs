using Invoqs.Interfaces;
using Invoqs.Models;
using System.Text.Json;

namespace Invoqs.Services
{
    public class ReceiptService : IReceiptService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private readonly JsonSerializerOptions _jsonOptions;

        public ReceiptService(HttpClient httpClient, IAuthService authService)
        {
            _httpClient = httpClient;
            _authService = authService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<ReceiptModel>> GetAllReceiptsAsync()
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync("receipts");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return new List<ReceiptModel>();
                }

                response.EnsureSuccessStatusCode();

                var receipts = await response.Content.ReadFromJsonAsync<List<ReceiptModel>>(_jsonOptions);
                return receipts ?? new List<ReceiptModel>();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching receipts: {ex.Message}");
                return new List<ReceiptModel>();
            }
        }

        public async Task<ReceiptModel?> GetReceiptByIdAsync(int id)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync($"receipts/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return null;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ReceiptModel>(_jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching receipt {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<List<ReceiptModel>> GetReceiptsByCustomerIdAsync(int customerId)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync($"receipts/customer/{customerId}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return new List<ReceiptModel>();
                }

                response.EnsureSuccessStatusCode();

                var receipts = await response.Content.ReadFromJsonAsync<List<ReceiptModel>>(_jsonOptions);
                return receipts ?? new List<ReceiptModel>();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching receipts for customer {customerId}: {ex.Message}");
                return new List<ReceiptModel>();
            }
        }

        public async Task<ReceiptModel?> CreateReceiptAsync(CreateReceiptModel model)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.PostAsJsonAsync("receipts", model, _jsonOptions);

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

                var receipt = await response.Content.ReadFromJsonAsync<ReceiptModel>(_jsonOptions);
                return receipt;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error creating receipt: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeleteReceiptAsync(int id)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.DeleteAsync($"receipts/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return false;
                }

                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error deleting receipt {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<byte[]?> DownloadReceiptPdfAsync(int receiptId, string userFirstName, string userLastName)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var url = $"receipts/{receiptId}/pdf?userFirstName={Uri.EscapeDataString(userFirstName)}&userLastName={Uri.EscapeDataString(userLastName)}";
                var response = await _httpClient.GetAsync(url);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return null;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine($"Receipt {receiptId} not found for PDF generation");
                    return null;
                }

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error downloading receipt PDF: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> SendReceiptAsync(int receiptId)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.PostAsync($"receipts/{receiptId}/send", null);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return false;
                }

                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error sending receipt {receiptId}: {ex.Message}");
                return false;
            }
        }
    }
}