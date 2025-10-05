using Invoqs.Models;

namespace Invoqs.Interfaces
{
    public interface IInvoiceService
    {
        // Standard CRUD operations
        Task<List<InvoiceModel>> GetAllInvoicesAsync();
        Task<InvoiceModel?> GetInvoiceByIdAsync(int id);
        Task<InvoiceModel> UpdateInvoiceAsync(InvoiceModel invoice);
        Task<bool> DeleteInvoiceAsync(int id);

        // Customer-specific queries
        Task<List<InvoiceModel>> GetInvoicesByCustomerIdAsync(int customerId);

        // Business logic operations
        Task<InvoiceModel> CreateInvoiceFromJobsAsync(int customerId, List<int> jobIds, DateTime? dueDate = null);
        Task<bool> MarkInvoiceAsSentAsync(int invoiceId);
        Task<bool> MarkInvoiceAsPaidAsync(int invoiceId, DateTime paidDate, string? paymentMethod = null, string? paymentReference = null);
        Task<bool> CancelInvoiceAsync(int invoiceId, string? reason = null);

        // Statistics
        Task<decimal> GetTotalOutstandingAsync();
    }
}