using Invoqs.Models;

namespace Invoqs.Services
{
    public interface IJobService
    {
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

        // New methods for address grouping
        Task<List<AddressJobGroup>> GetJobsGroupedByAddressAsync(int customerId);
        Task<List<JobModel>> GetJobsByAddressAsync(int customerId, string address);
        Task<AddressJobGroup?> GetAddressJobGroupAsync(int customerId, string address);
    }
}