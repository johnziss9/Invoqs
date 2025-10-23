using Invoqs.Models;

namespace Invoqs.Interfaces
{
    public interface IJobService
    {
        // Existing CRUD operations
        Task<List<JobModel>> GetAllJobsAsync();
        Task<List<JobModel>> GetJobsByCustomerIdAsync(int customerId);
        Task<JobModel?> GetJobByIdAsync(int jobId);
        Task<(JobModel? Job, ApiValidationError? ValidationErrors)> CreateJobAsync(JobModel job);
        Task<(bool Success, ApiValidationError? ValidationErrors)> UpdateJobAsync(JobModel job);
        Task<(bool Success, ApiValidationError? ValidationErrors)> UpdateJobStatusAsync(int jobId, JobStatus newStatus);
        Task<bool> DeleteJobAsync(int jobId);

        // Existing address grouping methods
        Task<List<AddressJobGroup>> GetJobsGroupedByAddressAsync(int customerId);
        Task<List<JobModel>> GetJobsByAddressAsync(int customerId, string address);

        // Invoice integration methods
        Task<List<JobModel>> GetCompletedUninvoicedJobsByCustomerAsync(int customerId);
        Task<List<JobModel>> GetJobsByInvoiceIdAsync(int invoiceId);
        Task<bool> MarkJobsAsInvoicedAsync(List<int> jobIds, int invoiceId);
        Task<bool> RemoveJobsFromInvoiceAsync(List<int> jobIds);
        Task<bool> CanJobBeInvoicedAsync(int jobId);
        Task<bool> CanJobsBeInvoicedAsync(List<int> jobIds);
        Task<List<string>> SearchAddressesAsync(string query);
    }
}