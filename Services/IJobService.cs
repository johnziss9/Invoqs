using Invoqs.Models;

namespace Invoqs.Services
{
    public interface IJobService
    {
        // Existing CRUD operations
        Task<List<JobModel>> GetAllJobsAsync();
        Task<List<JobModel>> GetJobsByCustomerIdAsync(int customerId);
        Task<JobModel?> GetJobByIdAsync(int jobId);
        Task<JobModel> CreateJobAsync(JobModel job);
        Task<bool> UpdateJobAsync(JobModel job);
        Task<bool> DeleteJobAsync(int jobId);
        Task<List<JobModel>> GetJobsByStatusAsync(JobStatus status);
        Task<List<JobModel>> GetJobsByTypeAsync(JobType type);
        Task<List<JobModel>> GetJobsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalRevenueByCustomerAsync(int customerId);
        Task<int> GetActiveJobCountByCustomerAsync(int customerId);
        Task<int> GetCompletedJobCountByCustomerAsync(int customerId);

        // Existing address grouping methods
        Task<List<AddressJobGroup>> GetJobsGroupedByAddressAsync(int customerId);
        Task<List<JobModel>> GetJobsByAddressAsync(int customerId, string address);
        Task<AddressJobGroup?> GetAddressJobGroupAsync(int customerId, string address);

        // Invoice integration methods
        Task<List<JobModel>> GetCompletedUninvoicedJobsAsync();
        Task<List<JobModel>> GetCompletedUninvoicedJobsByCustomerAsync(int customerId);
        Task<List<JobModel>> GetCompletedUninvoicedJobsByAddressAsync(int customerId, string address);
        Task<List<JobModel>> GetJobsByInvoiceIdAsync(int invoiceId);
        Task<bool> MarkJobsAsInvoicedAsync(List<int> jobIds, int invoiceId);
        Task<bool> RemoveJobsFromInvoiceAsync(List<int> jobIds);
        Task<bool> CanJobBeInvoicedAsync(int jobId);
        Task<bool> CanJobsBeInvoicedAsync(List<int> jobIds);

        // Enhanced address grouping for invoicing
        Task<List<AddressJobGroup>> GetUninvoicedJobsGroupedByAddressAsync(int customerId);
        Task<AddressJobGroup?> GetUninvoicedAddressJobGroupAsync(int customerId, string address);
    }
}