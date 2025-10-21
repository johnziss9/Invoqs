using Invoqs.Models;

namespace Invoqs.Interfaces
{
    public interface IReceiptService
    {
        Task<List<ReceiptModel>> GetAllReceiptsAsync();
        Task<ReceiptModel?> GetReceiptByIdAsync(int id);
        Task<List<ReceiptModel>> GetReceiptsByCustomerIdAsync(int customerId);
        Task<ReceiptModel?> CreateReceiptAsync(CreateReceiptModel model);
        Task<bool> DeleteReceiptAsync(int id);
        Task<byte[]?> DownloadReceiptPdfAsync(int receiptId);
        Task<bool> SendReceiptAsync(int receiptId);
    }
}