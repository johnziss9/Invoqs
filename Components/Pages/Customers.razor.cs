using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Interfaces;
using Microsoft.JSInterop;

namespace Invoqs.Components.Pages
{
    public partial class Customers : ComponentBase
    {
        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private IJobService JobService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        // Component state
        protected string searchTerm = "";
        protected string sortBy = "name";
        protected string filterBy = "all";
        protected bool isLoading = true;
        protected string errorMessage = "";

        protected List<CustomerModel> customers = new();
        protected List<JobModel> jobs = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadCustomers();
        }

        private async Task LoadCustomers()
        {
            try
            {
                isLoading = true;
                errorMessage = "";

                // Load both customers and jobs
                var customersTask = CustomerService.GetAllCustomersAsync();
                var jobsTask = JobService.GetAllJobsAsync();

                await Task.WhenAll(customersTask, jobsTask);

                customers = await customersTask;
                jobs = await jobsTask;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading customers: {ex.Message}";
                customers = new List<CustomerModel>();
                jobs = new List<JobModel>();
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        protected decimal GetMonthRevenue()
        {
            var startOfThisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var thisMonthJobs = jobs.Where(j =>
                j.JobDate >= startOfThisMonth &&
                j.JobDate < startOfThisMonth.AddMonths(1)).ToList();

            return thisMonthJobs.Sum(j => j.Price);
        }

        protected int GetMonthJobs()
        {
            var startOfThisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            return jobs.Count(j =>
                j.JobDate >= startOfThisMonth &&
                j.JobDate < startOfThisMonth.AddMonths(1));
        }

        protected IEnumerable<CustomerModel> filteredCustomers
        {
            get
            {
                var filtered = customers.AsEnumerable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    filtered = filtered.Where(c =>
                        c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.Phone.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                }

                // Apply category filter
                filtered = filterBy switch
                {
                    "active" => filtered.Where(c => c.UninvoicedJobs > 0),
                    "inactive" => filtered.Where(c => c.UninvoicedJobs == 0),
                    _ => filtered
                };

                // Apply sorting
                filtered = sortBy switch
                {
                    "created" => filtered.OrderByDescending(c => c.CreatedDate),
                    "revenue" => filtered.OrderByDescending(c => c.TotalRevenue),
                    "jobs" => filtered.OrderByDescending(c => c.TotalJobs),
                    _ => filtered.OrderBy(c => c.Name)
                };

                return filtered;
            }
        }

        protected Task HandleViewJobs(CustomerModel customer)
        {
            // Navigate to customer jobs page
            Console.WriteLine($"View jobs for {customer.Name}");
            Navigation.NavigateTo($"/customer/{customer.Id}/jobs");
            return Task.CompletedTask;
        }

        protected async Task HandleDeleteCustomer(CustomerModel customer)
        {
            try
            {
                // Show confirmation dialog and delete
                Console.WriteLine($"Delete customer {customer.Name}");

                // For now, just remove from list - you could add a confirmation dialog here
                var success = await CustomerService.DeleteCustomerAsync(customer.Id);

                if (success)
                {
                    await LoadCustomers(); // Refresh the list
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error deleting customer: {ex.Message}";
                StateHasChanged();
            }
        }

        protected Task ShowAddCustomerModal()
        {
            var currentUrl = Navigation.Uri;
            Navigation.NavigateTo($"/customer/new?returnUrl={Uri.EscapeDataString(currentUrl)}", true);
            return Task.CompletedTask;
        }

        // Method to refresh data (useful for testing)
        protected async Task RefreshData()
        {
            await LoadCustomers();
        }
    }
}