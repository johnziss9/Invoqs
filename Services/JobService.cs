using Invoqs.Interfaces;
using Invoqs.Models;
using System.Text.Json;

namespace Invoqs.Services
{
    public class JobService : IJobService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public JobService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<JobModel>> GetAllJobsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("jobs");
                response.EnsureSuccessStatusCode();

                var jobs = await response.Content.ReadFromJsonAsync<List<JobModel>>(_jsonOptions);
                return jobs ?? new List<JobModel>();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching jobs: {ex.Message}");
                return new List<JobModel>();
            }
        }

        public async Task<List<JobModel>> GetJobsByCustomerIdAsync(int customerId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"jobs/customer/{customerId}");
                response.EnsureSuccessStatusCode();

                var jobs = await response.Content.ReadFromJsonAsync<List<JobModel>>(_jsonOptions);
                return jobs ?? new List<JobModel>();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching jobs for customer {customerId}: {ex.Message}");
                return new List<JobModel>();
            }
        }

        public async Task<JobModel?> GetJobByIdAsync(int jobId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"jobs/{jobId}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<JobModel>(_jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching job {jobId}: {ex.Message}");
                return null;
            }
        }

        public async Task<JobModel> CreateJobAsync(JobModel job)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("jobs", job);
                response.EnsureSuccessStatusCode();

                var createdJob = await response.Content.ReadFromJsonAsync<JobModel>(_jsonOptions);
                return createdJob!;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error creating job: {ex.Message}");
            }
        }

        public async Task<bool> UpdateJobAsync(JobModel job)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"jobs/{job.Id}", job);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error updating job: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteJobAsync(int jobId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"jobs/{jobId}");
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error deleting job {jobId}: {ex.Message}");
                return false;
            }
        }

        public async Task<List<JobModel>> GetJobsByStatusAsync(JobStatus status)
        {
            try
            {
                var allJobs = await GetAllJobsAsync();
                return allJobs.Where(j => j.Status == status).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error filtering jobs by status: {ex.Message}");
                return new List<JobModel>();
            }
        }

        public async Task<List<JobModel>> GetJobsByTypeAsync(JobType type)
        {
            try
            {
                var allJobs = await GetAllJobsAsync();
                return allJobs.Where(j => j.Type == type).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error filtering jobs by type: {ex.Message}");
                return new List<JobModel>();
            }
        }

        public async Task<List<JobModel>> GetJobsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var allJobs = await GetAllJobsAsync();
                return allJobs.Where(j => j.StartDate >= startDate && j.StartDate <= endDate).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error filtering jobs by date range: {ex.Message}");
                return new List<JobModel>();
            }
        }

        public async Task<decimal> GetTotalRevenueByCustomerAsync(int customerId)
        {
            try
            {
                var jobs = await GetJobsByCustomerIdAsync(customerId);
                return jobs.Where(j => j.Status == JobStatus.Completed).Sum(j => j.Price);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating revenue for customer {customerId}: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> GetActiveJobCountByCustomerAsync(int customerId)
        {
            try
            {
                var jobs = await GetJobsByCustomerIdAsync(customerId);
                return jobs.Count(j => j.Status == JobStatus.Active);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error counting active jobs for customer {customerId}: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> GetCompletedJobCountByCustomerAsync(int customerId)
        {
            try
            {
                var jobs = await GetJobsByCustomerIdAsync(customerId);
                return jobs.Count(j => j.Status == JobStatus.Completed);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error counting completed jobs for customer {customerId}: {ex.Message}");
                return 0;
            }
        }

        public async Task<List<AddressJobGroup>> GetJobsGroupedByAddressAsync(int customerId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"jobs/customer/{customerId}/grouped-by-address");
                response.EnsureSuccessStatusCode();

                var groupedJobs = await response.Content.ReadFromJsonAsync<Dictionary<string, List<JobModel>>>(_jsonOptions);

                return groupedJobs?.Select(g => new AddressJobGroup
                {
                    Address = g.Key,
                    Jobs = g.Value
                }).OrderBy(g => g.Address).ToList() ?? new List<AddressJobGroup>();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching grouped jobs for customer {customerId}: {ex.Message}");
                return new List<AddressJobGroup>();
            }
        }

        public async Task<List<JobModel>> GetJobsByAddressAsync(int customerId, string address)
        {
            try
            {
                var allJobs = await GetJobsByCustomerIdAsync(customerId);
                return allJobs.Where(j => j.Address.Equals(address, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching jobs by address: {ex.Message}");
                return new List<JobModel>();
            }
        }

        public async Task<AddressJobGroup?> GetAddressJobGroupAsync(int customerId, string address)
        {
            try
            {
                var jobs = await GetJobsByAddressAsync(customerId, address);
                if (!jobs.Any()) return null;

                return new AddressJobGroup
                {
                    Address = address,
                    Jobs = jobs
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating address job group: {ex.Message}");
                return null;
            }
        }

        // Invoice integration methods
        public async Task<List<JobModel>> GetCompletedUninvoicedJobsAsync()
        {
            try
            {
                var allJobs = await GetAllJobsAsync();
                return allJobs.Where(j => j.CanBeInvoiced).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching completed uninvoiced jobs: {ex.Message}");
                return new List<JobModel>();
            }
        }

        public async Task<List<JobModel>> GetCompletedUninvoicedJobsByCustomerAsync(int customerId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"jobs/customer/{customerId}/uninvoiced");
                response.EnsureSuccessStatusCode();

                var jobs = await response.Content.ReadFromJsonAsync<List<JobModel>>(_jsonOptions);
                return jobs ?? new List<JobModel>();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching uninvoiced jobs for customer {customerId}: {ex.Message}");
                return new List<JobModel>();
            }
        }

        public async Task<List<JobModel>> GetCompletedUninvoicedJobsByAddressAsync(int customerId, string address)
        {
            try
            {
                var jobs = await GetCompletedUninvoicedJobsByCustomerAsync(customerId);
                return jobs.Where(j => j.Address.Equals(address, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching uninvoiced jobs by address: {ex.Message}");
                return new List<JobModel>();
            }
        }

        public async Task<List<JobModel>> GetJobsByInvoiceIdAsync(int invoiceId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"jobs/invoice/{invoiceId}");
                response.EnsureSuccessStatusCode();

                var jobs = await response.Content.ReadFromJsonAsync<List<JobModel>>(_jsonOptions);
                return jobs ?? new List<JobModel>();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching jobs for invoice {invoiceId}: {ex.Message}");
                return new List<JobModel>();
            }
        }

        public async Task<bool> MarkJobsAsInvoicedAsync(List<int> jobIds, int invoiceId)
        {
            try
            {
                var request = new { JobIds = jobIds, InvoiceId = invoiceId };
                var response = await _httpClient.PostAsJsonAsync("jobs/mark-as-invoiced", request);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error marking jobs as invoiced: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveJobsFromInvoiceAsync(List<int> jobIds)
        {
            try
            {
                var request = new { JobIds = jobIds };
                var response = await _httpClient.PostAsJsonAsync("jobs/remove-from-invoice", request);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error removing jobs from invoice: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CanJobBeInvoicedAsync(int jobId)
        {
            try
            {
                var job = await GetJobByIdAsync(jobId);
                return job?.CanBeInvoiced ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking if job can be invoiced: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CanJobsBeInvoicedAsync(List<int> jobIds)
        {
            try
            {
                foreach (var jobId in jobIds)
                {
                    if (!await CanJobBeInvoicedAsync(jobId))
                        return false;
                }
                return jobIds.Any();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking if jobs can be invoiced: {ex.Message}");
                return false;
            }
        }

        public async Task<List<AddressJobGroup>> GetUninvoicedJobsGroupedByAddressAsync(int customerId)
        {
            try
            {
                var jobs = await GetCompletedUninvoicedJobsByCustomerAsync(customerId);
                var groupedJobs = jobs.GroupBy(j => j.Address)
                    .Select(g => new AddressJobGroup
                    {
                        Address = g.Key,
                        Jobs = g.ToList()
                    })
                    .Where(g => g.HasJobsToInvoice)
                    .OrderBy(g => g.Address)
                    .ToList();

                return groupedJobs;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching uninvoiced jobs grouped by address: {ex.Message}");
                return new List<AddressJobGroup>();
            }
        }

        public async Task<AddressJobGroup?> GetUninvoicedAddressJobGroupAsync(int customerId, string address)
        {
            try
            {
                var jobs = await GetCompletedUninvoicedJobsByAddressAsync(customerId, address);
                if (!jobs.Any()) return null;

                return new AddressJobGroup
                {
                    Address = address,
                    Jobs = jobs
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating uninvoiced address job group: {ex.Message}");
                return null;
            }
        }
    }
}