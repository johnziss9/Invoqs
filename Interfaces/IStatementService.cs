using Invoqs.Models;

namespace Invoqs.Interfaces
{
    public interface IStatementService
    {
        Task<List<StatementModel>> GetAllStatementsAsync();
        Task<StatementModel?> GetStatementByIdAsync(int id);
        Task<StatementModel?> CreateStatementAsync(CreateStatementModel model);
        Task<bool> DeleteStatementAsync(int id);
        Task<byte[]?> DownloadStatementPdfAsync(int statementId);
        Task<bool> SendStatementAsync(int statementId, List<string> recipientEmails);
        Task<bool> MarkStatementAsDeliveredAsync(int statementId);
    }
}
