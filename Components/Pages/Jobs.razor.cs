using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Services;

namespace Invoqs.Components.Pages
{
    public partial class Jobs : ComponentBase
    {
        [Inject] private IJobService JobService { get; set; } = default!;
        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        // Component state
        protected string currentUser = "John Doe";
        protected string searchTerm = "";
        protected string customerFilter = "all";
        protected string statusFilter = "all";
        protected string typeFilter = "all";
        protected string sortBy = "startDate";
        protected string viewMode = "grid";
        protected bool isLoading = true;
        protected string errorMessage = "";

        protected List<JobModel> jobs = new();
        protected List<CustomerModel> customers = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                isLoading = true;
                errorMessage = "";

                // Load both jobs and customers in parallel
                var jobsTask = JobService.GetAllJobsAsync();
                var customersTask = CustomerService.GetAllCustomersAsync();

                await Task.WhenAll(jobsTask, customersTask);

                jobs = await jobsTask;
                customers = await customersTask;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading data: {ex.Message}";
                jobs = new List<JobModel>();
                customers = new List<CustomerModel>();
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        protected IEnumerable<JobModel> filteredJobs
        {
            get
            {
                var filtered = jobs.AsEnumerable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    filtered = filtered.Where(j =>
                        j.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        j.Address.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        j.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        GetCustomerName(j.CustomerId).Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                }

                // Apply customer filter
                if (customerFilter != "all" && int.TryParse(customerFilter, out int customerId))
                {
                    filtered = filtered.Where(j => j.CustomerId == customerId);
                }

                // Apply status filter
                if (statusFilter != "all")
                {
                    var status = statusFilter switch
                    {
                        "new" => JobStatus.New,
                        "active" => JobStatus.Active,
                        "completed" => JobStatus.Completed,
                        "cancelled" => JobStatus.Cancelled,
                        _ => JobStatus.New
                    };
                    filtered = filtered.Where(j => j.Status == status);
                }

                // Apply type filter
                if (typeFilter != "all")
                {
                    var type = typeFilter switch
                    {
                        "skip" => JobType.SkipRental,
                        "sand" => JobType.SandDelivery,
                        "cliff" => JobType.FortCliffService,
                        _ => JobType.SkipRental
                    };
                    filtered = filtered.Where(j => j.Type == type);
                }

                // Apply sorting
                filtered = sortBy switch
                {
                    "customer" => filtered.OrderBy(j => GetCustomerName(j.CustomerId)),
                    "title" => filtered.OrderBy(j => j.Title),
                    "status" => filtered.OrderBy(j => j.Status),
                    "price" => filtered.OrderByDescending(j => j.Price),
                    _ => filtered.OrderByDescending(j => j.StartDate)
                };

                return filtered;
            }
        }

        protected string GetCustomerName(int customerId)
        {
            return customers.FirstOrDefault(c => c.Id == customerId)?.Name ?? "Unknown Customer";
        }

        protected void ClearFilters()
        {
            searchTerm = "";
            customerFilter = "all";
            statusFilter = "all";
            typeFilter = "all";
            StateHasChanged();
        }

        // Event handlers
        protected Task HandleLogout()
        {
            Console.WriteLine("Logout clicked");
            return Task.CompletedTask;
        }

        protected async Task HandleStatusChange(JobModel job, JobStatus newStatus)
        {
            try
            {
                var originalStatus = job.Status;
                job.Status = newStatus;

                // Set end date for completed jobs
                if (newStatus == JobStatus.Completed && !job.EndDate.HasValue)
                {
                    job.EndDate = DateTime.Now;
                }
                // Clear end date if moving away from completed
                else if (newStatus != JobStatus.Completed && originalStatus == JobStatus.Completed)
                {
                    job.EndDate = null;
                }

                var success = await JobService.UpdateJobAsync(job);

                if (success)
                {
                    StateHasChanged();
                }
                else
                {
                    // Revert the status change
                    job.Status = originalStatus;
                    errorMessage = "Failed to update job status";
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error updating job status: {ex.Message}";
                StateHasChanged();
            }
        }

        protected Task HandleEditJob(JobModel job)
        {
            Navigation.NavigateTo($"/job/{job.Id}/edit");
            return Task.CompletedTask;
        }

        protected async Task HandleDeleteJob(JobModel job)
        {
            try
            {
                // In a real app, you'd show a confirmation dialog here
                var success = await JobService.DeleteJobAsync(job.Id);

                if (success)
                {
                    await LoadData(); // Refresh all data
                }
                else
                {
                    errorMessage = "Failed to delete job";
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error deleting job: {ex.Message}";
                StateHasChanged();
            }
        }

        protected Task HandleGenerateInvoice(JobModel job)
        {
            Navigation.NavigateTo($"/invoice/generate/{job.Id}");
            return Task.CompletedTask;
        }

        protected Task HandleViewCustomer(JobModel job)
        {
            Navigation.NavigateTo($"/customer/{job.CustomerId}/jobs");
            return Task.CompletedTask;
        }

        protected Task ShowAddJobModal()
        {
            // For a global add job, we might want to go to a customer selection page first
            Navigation.NavigateTo("/job/new");
            return Task.CompletedTask;
        }

        // Method to refresh data
        protected async Task RefreshData()
        {
            await LoadData();
        }
    }
}