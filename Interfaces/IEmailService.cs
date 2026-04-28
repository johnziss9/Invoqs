using Invoqs.Models;

namespace Invoqs.Interfaces
{
    public interface IEmailService
    {
        Task<BulkEmailResult> SendBulkEmailAsync(BulkEmailRequest request);
    }
}
