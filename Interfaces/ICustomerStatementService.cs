using Invoqs.Models;

namespace Invoqs.Interfaces
{
    public interface ICustomerStatementService
    {
        Task<List<CustomerStatementModel>> GetAllCustomerStatementsAsync();
        Task<List<CustomerStatementModel>> GetCustomerStatementsAsync(int customerId);
        Task<CustomerStatementModel?> GetCustomerStatementByIdAsync(int id);
        Task<CustomerStatementModel?> CreateCustomerStatementAsync(CreateCustomerStatementModel model);
        Task<bool> DeleteCustomerStatementAsync(int id);
        Task<byte[]?> DownloadCustomerStatementPdfAsync(int statementId);
        Task<bool> SendCustomerStatementAsync(int statementId, List<string> recipientEmails);
        Task<bool> MarkCustomerStatementAsDeliveredAsync(int statementId);
    }
}
