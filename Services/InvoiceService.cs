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

        // Standard CRUD operations
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

        public async Task<InvoiceModel> CreateInvoiceAsync(InvoiceModel invoice)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.PostAsJsonAsync("invoices", invoice);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    throw new UnauthorizedAccessException("Authentication required");
                }

                response.EnsureSuccessStatusCode();

                var createdInvoice = await response.Content.ReadFromJsonAsync<InvoiceModel>(_jsonOptions);
                return createdInvoice!;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error creating invoice: {ex.Message}");
            }
        }

        public async Task<InvoiceModel> UpdateInvoiceAsync(InvoiceModel invoice)
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

                response.EnsureSuccessStatusCode();

                var updatedInvoice = await response.Content.ReadFromJsonAsync<InvoiceModel>(_jsonOptions);
                return updatedInvoice!;
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

        public async Task<bool> InvoiceExistsAsync(int id)
        {
            try
            {
                var invoice = await GetInvoiceByIdAsync(id);
                return invoice != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking invoice existence {id}: {ex.Message}");
                return false;
            }
        }

        // Customer-specific queries
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

        public async Task<List<InvoiceModel>> GetInvoicesGroupedByCustomerAsync()
        {
            // This returns all invoices - grouping is done in the UI
            return await GetAllInvoicesAsync();
        }

        // Status-based queries
        public async Task<List<InvoiceModel>> GetInvoicesByStatusAsync(InvoiceStatus status)
        {
            try
            {
                var allInvoices = await GetAllInvoicesAsync();
                return allInvoices.Where(i => i.Status == status).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error filtering invoices by status: {ex.Message}");
                return new List<InvoiceModel>();
            }
        }

        public async Task<List<InvoiceModel>> GetDraftInvoicesAsync()
        {
            return await GetInvoicesByStatusAsync(InvoiceStatus.Draft);
        }

        public async Task<List<InvoiceModel>> GetSentInvoicesAsync()
        {
            return await GetInvoicesByStatusAsync(InvoiceStatus.Sent);
        }

        public async Task<List<InvoiceModel>> GetPaidInvoicesAsync()
        {
            return await GetInvoicesByStatusAsync(InvoiceStatus.Paid);
        }

        public async Task<List<InvoiceModel>> GetOverdueInvoicesAsync()
        {
            return await GetInvoicesByStatusAsync(InvoiceStatus.Overdue);
        }

        public async Task<List<InvoiceModel>> GetCancelledInvoicesAsync()
        {
            return await GetInvoicesByStatusAsync(InvoiceStatus.Cancelled);
        }

        // Date-based queries
        public async Task<List<InvoiceModel>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var allInvoices = await GetAllInvoicesAsync();
                return allInvoices.Where(i => i.CreatedDate >= startDate && i.CreatedDate <= endDate).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error filtering invoices by date range: {ex.Message}");
                return new List<InvoiceModel>();
            }
        }

        public async Task<List<InvoiceModel>> GetInvoicesDueByDateAsync(DateTime dueDate)
        {
            try
            {
                var allInvoices = await GetAllInvoicesAsync();
                return allInvoices.Where(i => i.DueDate.Date == dueDate.Date && i.Status != InvoiceStatus.Paid).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error filtering invoices by due date: {ex.Message}");
                return new List<InvoiceModel>();
            }
        }

        public async Task<List<InvoiceModel>> GetInvoicesCreatedThisMonthAsync()
        {
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            return await GetInvoicesByDateRangeAsync(startOfMonth, DateTime.Now);
        }

        public async Task<List<InvoiceModel>> GetInvoicesCreatedThisWeekAsync()
        {
            var oneWeekAgo = DateTime.Now.AddDays(-7);
            return await GetInvoicesByDateRangeAsync(oneWeekAgo, DateTime.Now);
        }

        // Business logic operations
        public async Task<InvoiceModel> CreateInvoiceFromJobsAsync(int customerId, List<int> jobIds, DateTime? dueDate = null)
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

                var response = await _httpClient.PostAsJsonAsync("invoices", request);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    throw new UnauthorizedAccessException("Authentication required");
                }

                response.EnsureSuccessStatusCode();

                var createdInvoice = await response.Content.ReadFromJsonAsync<InvoiceModel>(_jsonOptions);
                return createdInvoice!;
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

                var response = await _httpClient.PostAsync($"invoices/{invoiceId}/send", null);
                
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

        public async Task<bool> MarkInvoiceAsOverdueAsync(int invoiceId)
        {
            // This is typically handled automatically by the API based on due dates
            // No specific endpoint needed - status is computed
            return true;
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

        // Search and filtering
        public async Task<List<InvoiceModel>> SearchInvoicesAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return await GetAllInvoicesAsync();

                var allInvoices = await GetAllInvoicesAsync();
                var searchLower = searchTerm.ToLower();

                return allInvoices.Where(i =>
                    i.InvoiceNumber.ToLower().Contains(searchLower) ||
                    i.Customer?.Name.ToLower().Contains(searchLower) == true ||
                    i.Notes?.ToLower().Contains(searchLower) == true
                ).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching invoices: {ex.Message}");
                return new List<InvoiceModel>();
            }
        }

        public async Task<List<InvoiceModel>> GetInvoicesWithFiltersAsync(int? customerId = null, InvoiceStatus? status = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var allInvoices = await GetAllInvoicesAsync();
                var filteredInvoices = allInvoices.AsQueryable();

                if (customerId.HasValue)
                    filteredInvoices = filteredInvoices.Where(i => i.CustomerId == customerId.Value);

                if (status.HasValue)
                    filteredInvoices = filteredInvoices.Where(i => i.Status == status.Value);

                if (startDate.HasValue)
                    filteredInvoices = filteredInvoices.Where(i => i.CreatedDate >= startDate.Value);

                if (endDate.HasValue)
                    filteredInvoices = filteredInvoices.Where(i => i.CreatedDate <= endDate.Value);

                return filteredInvoices.OrderByDescending(i => i.CreatedDate).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error filtering invoices: {ex.Message}");
                return new List<InvoiceModel>();
            }
        }

        // Statistics and reporting
        public async Task<decimal> GetTotalRevenueAsync()
        {
            try
            {
                var paidInvoices = await GetPaidInvoicesAsync();
                return paidInvoices.Sum(i => i.Total);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating total revenue: {ex.Message}");
                return 0;
            }
        }

        public async Task<decimal> GetTotalRevenueByCustomerAsync(int customerId)
        {
            try
            {
                var customerInvoices = await GetInvoicesByCustomerIdAsync(customerId);
                return customerInvoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.Total);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating revenue for customer {customerId}: {ex.Message}");
                return 0;
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

        public async Task<decimal> GetTotalOverdueAsync()
        {
            try
            {
                var overdueInvoices = await GetOverdueInvoicesAsync();
                return overdueInvoices.Sum(i => i.Total);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating total overdue: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> GetInvoiceCountByStatusAsync(InvoiceStatus status)
        {
            try
            {
                var invoices = await GetInvoicesByStatusAsync(status);
                return invoices.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error counting invoices by status: {ex.Message}");
                return 0;
            }
        }

        // Dashboard metrics
        public async Task<decimal> GetWeeklyRevenueAsync()
        {
            try
            {
                var oneWeekAgo = DateTime.Now.AddDays(-7);
                var paidInvoices = await GetPaidInvoicesAsync();
                return paidInvoices.Where(i => i.PaidDate >= oneWeekAgo).Sum(i => i.Total);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating weekly revenue: {ex.Message}");
                return 0;
            }
        }

        public async Task<decimal> GetMonthlyRevenueAsync()
        {
            try
            {
                var oneMonthAgo = DateTime.Now.AddDays(-30);
                var paidInvoices = await GetPaidInvoicesAsync();
                return paidInvoices.Where(i => i.PaidDate >= oneMonthAgo).Sum(i => i.Total);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating monthly revenue: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> GetPendingInvoicesCountAsync()
        {
            try
            {
                var allInvoices = await GetAllInvoicesAsync();
                return allInvoices.Count(i => i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error counting pending invoices: {ex.Message}");
                return 0;
            }
        }

        public async Task<decimal> GetPendingInvoicesAmountAsync()
        {
            try
            {
                return await GetTotalOutstandingAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating pending invoices amount: {ex.Message}");
                return 0;
            }
        }

        // Invoice numbering
        public async Task<string> GenerateNextInvoiceNumberAsync()
        {
            try
            {
                var year = DateTime.Now.Year;
                var allInvoices = await GetAllInvoicesAsync();
                var maxNumber = allInvoices
                    .Where(i => i.InvoiceNumber.StartsWith($"INV-{year}"))
                    .Select(i =>
                    {
                        var parts = i.InvoiceNumber.Split('-');
                        return parts.Length == 3 && int.TryParse(parts[2], out var num) ? num : 0;
                    })
                    .DefaultIfEmpty(0)
                    .Max();

                return $"INV-{year}-{(maxNumber + 1):D4}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating invoice number: {ex.Message}");
                return $"INV-{DateTime.Now.Year}-0001";
            }
        }

        public async Task<bool> IsInvoiceNumberUniqueAsync(string invoiceNumber)
        {
            try
            {
                var allInvoices = await GetAllInvoicesAsync();
                return !allInvoices.Any(i => i.InvoiceNumber.Equals(invoiceNumber, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking invoice number uniqueness: {ex.Message}");
                return false;
            }
        }

        // Line item management (these use the main invoice endpoints)
        public async Task<InvoiceLineItemModel> AddLineItemAsync(int invoiceId, InvoiceLineItemModel lineItem)
        {
            // This would typically be handled through invoice updates
            throw new NotImplementedException("Line items are managed through invoice updates");
        }

        public async Task<bool> RemoveLineItemAsync(int invoiceId, int lineItemId)
        {
            // This would typically be handled through invoice updates
            throw new NotImplementedException("Line items are managed through invoice updates");
        }

        public async Task<InvoiceModel> UpdateLineItemsAsync(int invoiceId, List<InvoiceLineItemModel> lineItems)
        {
            // This would typically be handled through invoice updates
            throw new NotImplementedException("Line items are managed through invoice updates");
        }

        // Validation helpers
        public async Task<bool> CanJobsBeInvoicedAsync(List<int> jobIds)
        {
            // This logic is handled by the JobService
            return true; // Simplified for now
        }

        public async Task<List<JobModel>> GetUnInvoicedCompletedJobsByCustomerAsync(int customerId)
        {
            // This is handled by JobService.GetCompletedUninvoicedJobsByCustomerAsync
            return new List<JobModel>(); // Simplified for now
        }

        public async Task<List<JobModel>> GetUnInvoicedCompletedJobsByAddressAsync(int customerId, string address)
        {
            // This is handled by JobService.GetCompletedUninvoicedJobsByAddressAsync
            return new List<JobModel>(); // Simplified for now
        }
    }
}