using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Interfaces;
using Microsoft.JSInterop;

namespace Invoqs.Components.Pages
{
    public partial class Jobs : ComponentBase
    {
        [Inject] private IJobService JobService { get; set; } = default!;
        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        // Component state
        protected string searchTerm = "";
        protected string customerFilter = "all";
        protected string invoiceFilter = "all";
        protected string typeFilter = "all";
        protected string sortBy = "jobDate";
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

        private void CreateInvoice()
        {
            var currentUrl = Navigation.Uri;
            Navigation.NavigateTo($"/invoice/new?returnUrl={Uri.EscapeDataString(currentUrl)}", forceLoad: true);
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

                // Apply type filter
                if (typeFilter != "all")
                {
                    var type = typeFilter switch
                    {
                        "skip" => JobType.SkipRental,
                        "sand" => JobType.SandDelivery,
                        "cliff" => JobType.ForkLiftService,
                        "transfer" => JobType.Transfer,
                        "sellforklift" => JobType.SellForklift,
                        _ => JobType.SkipRental
                    };
                    filtered = filtered.Where(j => j.Type == type);
                }

                // Apply invoice filter
                if (invoiceFilter != "all")
                {
                    filtered = invoiceFilter switch
                    {
                        "invoiced" => filtered.Where(j => j.IsInvoiced),
                        "uninvoiced" => filtered.Where(j => !j.IsInvoiced),
                        _ => filtered
                    };
                }

                // Apply sorting
                filtered = sortBy switch
                {
                    "createdDate" => filtered.OrderByDescending(j => j.CreatedDate),
                    "customer" => filtered.OrderBy(j => GetCustomerName(j.CustomerId)),
                    "title" => filtered.OrderBy(j => j.Title),
                    "price" => filtered.OrderByDescending(j => j.Price),
                    _ => filtered.OrderByDescending(j => j.JobDate)
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
            typeFilter = "all";
            invoiceFilter = "all";
            StateHasChanged();
        }

        // Event handlers
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
            var currentUrl = Navigation.Uri;
            var returnUrl = Uri.EscapeDataString(currentUrl);
            Navigation.NavigateTo($"/customer/{job.CustomerId}/invoice/new?preselectedJobId={job.Id}&returnUrl={returnUrl}");
            return Task.CompletedTask;
        }

        protected Task ShowAddJobModal()
        {
            var currentUrl = Navigation.Uri;
            Navigation.NavigateTo($"/job/new?returnUrl={Uri.EscapeDataString(currentUrl)}", true);
            return Task.CompletedTask;
        }

        protected decimal GetMonthJobValue()
        {
            var startOfThisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var thisMonthJobs = jobs.Where(j =>
                j.JobDate >= startOfThisMonth &&
                j.JobDate < startOfThisMonth.AddMonths(1)).ToList();

            return thisMonthJobs.Sum(j => j.Price);
        }

        protected int GetMonthJobCount()
        {
            var startOfThisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            return jobs.Count(j =>
                j.JobDate >= startOfThisMonth &&
                j.JobDate < startOfThisMonth.AddMonths(1));
        }
    }
}