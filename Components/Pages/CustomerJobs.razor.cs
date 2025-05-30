using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Services;

namespace Invoqs.Components.Pages
{
    public partial class CustomerJobs : ComponentBase
    {
        [Parameter] public int CustomerId { get; set; }

        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private IJobService JobService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        // Component state
        protected string currentUser = "John Doe";
        protected string searchTerm = "";
        protected string statusFilter = "all";
        protected string typeFilter = "all";
        protected string sortBy = "startDate";
        protected bool isLoading = true;
        protected string errorMessage = "";

        protected CustomerModel? customer;
        protected string customerName = "Loading...";
        protected List<JobModel> jobs = new();
        protected List<AddressJobGroup> addressGroups = new();

        // Track expanded address sections
        protected HashSet<string> expandedAddresses = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadCustomerData();
            await LoadJobs();
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

                // Load individual jobs
                jobs = await JobService.GetJobsByCustomerIdAsync(CustomerId);

                // Load address groups
                addressGroups = await JobService.GetJobsGroupedByAddressAsync(CustomerId);
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading jobs: {ex.Message}";
                jobs = new List<JobModel>();
                addressGroups = new List<AddressJobGroup>();
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
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

        protected bool IsAddressExpanded(string address)
        {
            return expandedAddresses.Contains(address);
        }

        protected void ToggleAddressExpansion(string address)
        {
            if (expandedAddresses.Contains(address))
            {
                expandedAddresses.Remove(address);
            }
            else
            {
                expandedAddresses.Add(address);
            }
            StateHasChanged();
        }

        // Get most relevant jobs for address preview (max 10 total)
        protected List<JobModel> GetRelevantJobsForAddress(List<JobModel> addressJobs)
        {
            var relevant = new List<JobModel>();

            // Add active jobs (max 5)
            relevant.AddRange(addressJobs.Where(j => j.Status == JobStatus.Active).Take(5));

            // Add new jobs if we have space (max 3)
            var remainingSpace = 10 - relevant.Count;
            if (remainingSpace > 0)
            {
                relevant.AddRange(addressJobs.Where(j => j.Status == JobStatus.New).Take(Math.Min(3, remainingSpace)));
            }

            // Fill remaining space with recent completed jobs
            remainingSpace = 10 - relevant.Count;
            if (remainingSpace > 0)
            {
                relevant.AddRange(addressJobs
                    .Where(j => j.Status == JobStatus.Completed)
                    .OrderByDescending(j => j.EndDate ?? j.UpdatedDate)
                    .Take(remainingSpace));
            }

            return relevant;
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
                    // Refresh data to update stats and groupings
                    await LoadJobs();
                    await LoadCustomerData();
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
                    await LoadJobs();
                    await LoadCustomerData(); // Refresh customer stats
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
            Navigation.NavigateTo($"/invoice/generate/{job.Id}");
            return Task.CompletedTask;
        }

        protected Task HandleGenerateInvoiceForAddress(string address)
        {
            // Navigate to invoice generation for all completed jobs at this address
            Navigation.NavigateTo($"/invoice/generate/address/{CustomerId}/{Uri.EscapeDataString(address)}");
            return Task.CompletedTask;
        }

        protected Task HandleViewFullAddress(string address)
        {
            Navigation.NavigateTo($"/customer/{CustomerId}/address/{Uri.EscapeDataString(address)}");
            return Task.CompletedTask;
        }

        protected Task ShowAddJobModal()
        {
            Navigation.NavigateTo($"/customer/{CustomerId}/job/new");
            return Task.CompletedTask;
        }

        // Method to refresh data
        protected async Task RefreshData()
        {
            await LoadCustomerData();
            await LoadJobs();
        }
    }
}