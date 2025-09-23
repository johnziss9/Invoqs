using Invoqs.Models;

namespace Invoqs.Interfaces
{
    public interface IInvoiceService
    {
        // Standard CRUD operations
        Task<List<InvoiceModel>> GetAllInvoicesAsync();
        Task<InvoiceModel?> GetInvoiceByIdAsync(int id);
        Task<InvoiceModel> CreateInvoiceAsync(InvoiceModel invoice);
        Task<InvoiceModel> UpdateInvoiceAsync(InvoiceModel invoice);
        Task<bool> DeleteInvoiceAsync(int id);
        Task<bool> InvoiceExistsAsync(int id);

        // Customer-specific queries
        Task<List<InvoiceModel>> GetInvoicesByCustomerIdAsync(int customerId);
        Task<List<InvoiceModel>> GetInvoicesGroupedByCustomerAsync();

        // Status-based queries
        Task<List<InvoiceModel>> GetInvoicesByStatusAsync(InvoiceStatus status);
        Task<List<InvoiceModel>> GetDraftInvoicesAsync();
        Task<List<InvoiceModel>> GetSentInvoicesAsync();
        Task<List<InvoiceModel>> GetPaidInvoicesAsync();
        Task<List<InvoiceModel>> GetOverdueInvoicesAsync();
        Task<List<InvoiceModel>> GetCancelledInvoicesAsync();

        // Date-based queries
        Task<List<InvoiceModel>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<InvoiceModel>> GetInvoicesDueByDateAsync(DateTime dueDate);
        Task<List<InvoiceModel>> GetInvoicesCreatedThisMonthAsync();
        Task<List<InvoiceModel>> GetInvoicesCreatedThisWeekAsync();

        // Business logic operations
        Task<InvoiceModel> CreateInvoiceFromJobsAsync(int customerId, List<int> jobIds, DateTime? dueDate = null);
        Task<bool> MarkInvoiceAsSentAsync(int invoiceId);
        Task<bool> MarkInvoiceAsPaidAsync(int invoiceId, DateTime paidDate, string? paymentMethod = null, string? paymentReference = null);
        Task<bool> MarkInvoiceAsOverdueAsync(int invoiceId);
        Task<bool> CancelInvoiceAsync(int invoiceId, string? reason = null);

        // Search and filtering
        Task<List<InvoiceModel>> SearchInvoicesAsync(string searchTerm);
        Task<List<InvoiceModel>> GetInvoicesWithFiltersAsync(int? customerId = null, InvoiceStatus? status = null, DateTime? startDate = null, DateTime? endDate = null);

        // Statistics and reporting
        Task<decimal> GetTotalRevenueAsync();
        Task<decimal> GetTotalRevenueByCustomerAsync(int customerId);
        Task<decimal> GetTotalOutstandingAsync();
        Task<decimal> GetTotalOverdueAsync();
        Task<int> GetInvoiceCountByStatusAsync(InvoiceStatus status);
        
        // Dashboard metrics
        Task<decimal> GetWeeklyRevenueAsync();
        Task<decimal> GetMonthlyRevenueAsync();
        Task<int> GetPendingInvoicesCountAsync();
        Task<decimal> GetPendingInvoicesAmountAsync();

        // Invoice numbering
        Task<string> GenerateNextInvoiceNumberAsync();
        Task<bool> IsInvoiceNumberUniqueAsync(string invoiceNumber);

        // Line item management
        Task<InvoiceLineItemModel> AddLineItemAsync(int invoiceId, InvoiceLineItemModel lineItem);
        Task<bool> RemoveLineItemAsync(int invoiceId, int lineItemId);
        Task<InvoiceModel> UpdateLineItemsAsync(int invoiceId, List<InvoiceLineItemModel> lineItems);

        // Validation helpers
        Task<bool> CanJobsBeInvoicedAsync(List<int> jobIds);
        Task<List<JobModel>> GetUnInvoicedCompletedJobsByCustomerAsync(int customerId);
        Task<List<JobModel>> GetUnInvoicedCompletedJobsByAddressAsync(int customerId, string address);
    }
}