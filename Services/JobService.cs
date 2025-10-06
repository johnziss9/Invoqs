using Invoqs.Interfaces;
using Invoqs.Models;
using System.Text.Json;

namespace Invoqs.Services
{
    public class JobService : IJobService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private readonly JsonSerializerOptions _jsonOptions;

        public JobService(HttpClient httpClient, IAuthService authService)
        {
            _httpClient = httpClient;
            _authService = authService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<JobModel>> GetAllJobsAsync()
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync("jobs");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return new List<JobModel>();
                }

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
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync($"jobs/customer/{customerId}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return new List<JobModel>();
                }

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
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync($"jobs/{jobId}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return null;
                }

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
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.PostAsJsonAsync("jobs", job);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    throw new UnauthorizedAccessException("Authentication required");
                }

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
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.PutAsJsonAsync($"jobs/{job.Id}", job);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return false;
                }

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
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.DeleteAsync($"jobs/{jobId}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return false;
                }

                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error deleting job {jobId}: {ex.Message}");
                return false;
            }
        }

        public async Task<List<AddressJobGroup>> GetJobsGroupedByAddressAsync(int customerId)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync($"jobs/customer/{customerId}/grouped-by-address");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return new List<AddressJobGroup>();
                }

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

        // Invoice integration methods
        public async Task<List<JobModel>> GetCompletedUninvoicedJobsByCustomerAsync(int customerId)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync($"jobs/customer/{customerId}/uninvoiced");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return new List<JobModel>();
                }

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

        public async Task<List<JobModel>> GetJobsByInvoiceIdAsync(int invoiceId)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync($"jobs/invoice/{invoiceId}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return new List<JobModel>();
                }

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
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var request = new { JobIds = jobIds, InvoiceId = invoiceId };
                var response = await _httpClient.PostAsJsonAsync("jobs/mark-as-invoiced", request);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return false;
                }

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
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var request = new { JobIds = jobIds };
                var response = await _httpClient.PostAsJsonAsync("jobs/remove-from-invoice", request);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return false;
                }

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
    }
}