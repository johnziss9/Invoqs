using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Interfaces;
using Microsoft.JSInterop;

namespace Invoqs.Components.Pages
{
    public partial class CustomerAddressJobsBase : ComponentBase
    {
        [Parameter] public int CustomerId { get; set; }
        [Parameter] public string Address { get; set; } = string.Empty;

        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private IJobService JobService { get; set; } = default!;
        [Inject] protected NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        // Component state
        protected string searchTerm = "";
        protected string statusFilter = "all";
        protected string typeFilter = "all";
        protected string sortBy = "startDate";
        protected bool isLoading = true;
        protected string errorMessage = "";

        protected CustomerModel? customer;
        protected string customerName = "Loading...";
        protected List<JobModel> jobs = new();

        protected override async Task OnParametersSetAsync()
        {
            if (CustomerId > 0 && !string.IsNullOrEmpty(Address))
            {
                Address = Uri.UnescapeDataString(Address);
                await LoadCustomerData();
                await LoadJobs();
            }
        }

        private async Task LoadCustomerData()
        {
            try
            {
                customer = await CustomerService.GetCustomerByIdAsync(CustomerId);
                if (customer != null)
                {
                    customerName = customer.Name;
                }
                else
                {
                    errorMessage = "Customer not found";
                    Navigation.NavigateTo("/customers");
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading customer: {ex.Message}";
            }
        }

        private async Task LoadJobs()
        {
            try
            {
                isLoading = true;
                errorMessage = "";

                var allCustomerJobs = await JobService.GetJobsByCustomerIdAsync(CustomerId);

                // Filter to only jobs at this specific address
                jobs = allCustomerJobs.Where(j => j.Address.Equals(Address, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading jobs: {ex.Message}";
                jobs = new List<JobModel>();
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

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    filtered = filtered.Where(j =>
                        j.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        j.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        j.TypeDisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                }

                if (statusFilter != "all")
                {
                    if (Enum.TryParse<JobStatus>(statusFilter, true, out var status))
                    {
                        filtered = filtered.Where(j => j.Status == status);
                    }
                }

                if (typeFilter != "all")
                {
                    if (Enum.TryParse<JobType>(typeFilter, out var type))
                    {
                        filtered = filtered.Where(j => j.Type == type);
                    }
                }

                filtered = sortBy switch
                {
                    "startDate" => filtered.OrderByDescending(j => j.StartDate),
                    "endDate" => filtered.OrderByDescending(j => j.EndDate ?? DateTime.MinValue),
                    "status" => filtered.OrderBy(j => j.Status),
                    "type" => filtered.OrderBy(j => j.Type),
                    "price" => filtered.OrderByDescending(j => j.Price),
                    _ => filtered.OrderByDescending(j => j.StartDate)
                };

                return filtered;
            }
        }

        protected string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "?";

            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();

            return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
        }
        // Event handlers
        protected async Task HandleStatusChange(JobModel job, JobStatus newStatus)
        {
            try
            {
                var originalStatus = job.Status;

                var (success, errors) = await JobService.UpdateJobStatusAsync(job.Id, newStatus);

                if (success)
                {
                    job.Status = newStatus;

                    if (newStatus == JobStatus.Completed && !job.EndDate.HasValue)
                    {
                        job.EndDate = DateTime.Now;
                    }
                    else if (newStatus != JobStatus.Completed && originalStatus == JobStatus.Completed)
                    {
                        job.EndDate = null;
                    }

                    // Refresh data to update stats
                    await LoadJobs();
                    StateHasChanged();
                }
                else
                {
                    if (errors != null)
                    {
                        errorMessage = "Validation error: " + string.Join(", ", errors.GetAllErrors());
                    }
                    else
                    {
                        errorMessage = "Failed to update job status";
                    }

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
            var currentUrl = Navigation.Uri;
            Navigation.NavigateTo($"/job/{job.Id}/edit?returnUrl={Uri.EscapeDataString(currentUrl)}", true);
            return Task.CompletedTask;
        }

        protected async Task HandleDeleteJob(JobModel job)
        {
            try
            {
                var success = await JobService.DeleteJobAsync(job.Id);

                if (success)
                {
                    await LoadJobs();
                }
                else
                {
                    errorMessage = "Failed to delete job";
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
            var currentUrl = Navigation.Uri;
            var returnUrl = Uri.EscapeDataString(currentUrl);
            Navigation.NavigateTo($"/customer/{CustomerId}/invoice/new?preselectedJobId={job.Id}&returnUrl={returnUrl}", true);
            return Task.CompletedTask;
        }

        protected void ClearFilters()
        {
            searchTerm = "";
            statusFilter = "all";
            typeFilter = "all";
            StateHasChanged();
        }
    }
}