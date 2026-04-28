using Invoqs.Models;

namespace Invoqs.Interfaces
{
    public interface IBulkEmailLogService
    {
        Task<List<BulkEmailLogModel>> GetAllLogsAsync();
        Task<BulkEmailLogModel?> GetLogByIdAsync(int id);
    }
}
