using Invoqs.Interfaces;
using Invoqs.Models;
using System.Text.Json;

namespace Invoqs.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private readonly JsonSerializerOptions _jsonOptions;

        public InvoiceService(HttpClient httpClient, IAuthService authService)
        {
            _httpClient = httpClient;
            _authService = authService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<InvoiceModel>> GetAllInvoicesAsync()
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync("invoices");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return new List<InvoiceModel>();
                }

                response.EnsureSuccessStatusCode();

                var invoices = await response.Content.ReadFromJsonAsync<List<InvoiceModel>>(_jsonOptions);
                return invoices ?? new List<InvoiceModel>();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching invoices: {ex.Message}");
                return new List<InvoiceModel>();
            }
        }

        public async Task<InvoiceModel?> GetInvoiceByIdAsync(int id)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync($"invoices/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return null;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<InvoiceModel>(_jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching invoice {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<(InvoiceModel? Invoice, ApiValidationError? ValidationErrors)> UpdateInvoiceAsync(InvoiceModel invoice)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.PutAsJsonAsync($"invoices/{invoice.Id}", invoice);

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

                var updatedInvoice = await response.Content.ReadFromJsonAsync<InvoiceModel>(_jsonOptions);
                return (updatedInvoice, null);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error updating invoice: {ex.Message}");
            }
        }

        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.DeleteAsync($"invoices/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return false;
                }

                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error deleting invoice {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<List<InvoiceModel>> GetInvoicesByCustomerIdAsync(int customerId)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync($"invoices/customer/{customerId}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return new List<InvoiceModel>();
                }

                response.EnsureSuccessStatusCode();

                var invoices = await response.Content.ReadFromJsonAsync<List<InvoiceModel>>(_jsonOptions);
                return invoices ?? new List<InvoiceModel>();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching invoices for customer {customerId}: {ex.Message}");
                return new List<InvoiceModel>();
            }
        }

        public async Task<(InvoiceModel? Invoice, ApiValidationError? ValidationErrors)> CreateInvoiceFromJobsAsync(
            int customerId,
            List<int> jobIds,
            DateTime? dueDate = null)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var request = new
                {
                    CustomerId = customerId,
                    JobIds = jobIds,
                    VatRate = 0.19m,
                    PaymentTermsDays = 30,
                    Notes = (string?)null
                };

                var response = await _httpClient.PostAsJsonAsync("invoices", request, _jsonOptions);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    throw new UnauthorizedAccessException("Authentication required");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();

                    try
                    {
                        var validationErrors = JsonSerializer.Deserialize<ApiValidationError>(errorContent, _jsonOptions);
                        return (null, validationErrors);
                    }
                    catch (JsonException)
                    {
                        var fallbackError = new ApiValidationError
                        {
                            Error = $"Validation failed: {errorContent}"
                        };
                        return (null, fallbackError);
                    }
                }

                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var createdInvoice = JsonSerializer.Deserialize<InvoiceModel>(responseContent, _jsonOptions);

                return (createdInvoice, null);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error creating invoice from jobs: {ex.Message}");
            }
        }

        public async Task<bool> MarkInvoiceAsSentAsync(int invoiceId)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var content = JsonContent.Create(new { SentDate = DateTime.Today });
                var response = await _httpClient.PostAsync($"invoices/{invoiceId}/send", content);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return false;
                }

                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error marking invoice as sent: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> MarkInvoiceAsPaidAsync(int invoiceId, DateTime paidDate, string? paymentMethod = null, string? paymentReference = null)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var request = new
                {
                    PaymentDate = paidDate,
                    PaymentMethod = paymentMethod ?? "Bank Transfer",
                    PaymentReference = paymentReference
                };

                var response = await _httpClient.PostAsJsonAsync($"invoices/{invoiceId}/payment", request);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return false;
                }

                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error recording payment: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CancelInvoiceAsync(int invoiceId, string? reason = null)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.PostAsync($"invoices/{invoiceId}/cancel", null);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return false;
                }

                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error cancelling invoice: {ex.Message}");
                return false;
            }
        }

        public async Task<decimal> GetTotalOutstandingAsync()
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync("invoices/outstanding");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return 0;
                }

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<decimal>(_jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching total outstanding: {ex.Message}");
                return 0;
            }
        }

        public async Task<byte[]?> DownloadInvoicePdfAsync(int invoiceId)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync($"invoices/{invoiceId}/pdf");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return null;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine($"Invoice {invoiceId} not found for PDF generation");
                    return null;
                }

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error downloading invoice PDF: {ex.Message}");
                return null;
            }
        }
    }
}