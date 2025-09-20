using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Invoqs.Models;
using Invoqs.Services;

namespace Invoqs.Components.Pages
{
    public partial class CustomerDetails : ComponentBase
    {
        [Parameter] public int CustomerId { get; set; }

        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private IJobService JobService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        protected string currentUser = "John Doe"; // Replace with actual user service
        protected CustomerModel? customer;
        protected List<JobModel> recentJobs = new();
        protected CustomerStats customerStats = new();
        protected bool isLoading = true;
        protected string errorMessage = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            await LoadCustomerData();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (CustomerId > 0)
            {
                await LoadCustomerData();
            }
        }

        private async Task LoadCustomerData()
        {
            try
            {
                isLoading = true;
                errorMessage = string.Empty;

                // Load customer
                customer = await CustomerService.GetCustomerByIdAsync(CustomerId);
                
                if (customer != null)
                {
                    // Load customer's jobs
                    var allCustomerJobs = await JobService.GetJobsByCustomerIdAsync(CustomerId);
                    
                    // Get recent jobs (last 10, ordered by date)
                    recentJobs = allCustomerJobs
                        .OrderByDescending(j => j.StartDate)
                        .Take(10)
                        .ToList();

                    // Calculate customer statistics
                    CalculateCustomerStats(allCustomerJobs);
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading customer: {ex.Message}";
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private void CalculateCustomerStats(List<JobModel> jobs)
        {
            try
            {
                customerStats = new CustomerStats
                {
                    TotalJobs = jobs.Count,
                    ActiveJobs = jobs.Count(j => j.Status == JobStatus.Active),
                    NewJobs = jobs.Count(j => j.Status == JobStatus.New),
                    CompletedJobs = jobs.Count(j => j.Status == JobStatus.Completed),
                    CancelledJobs = jobs.Count(j => j.Status == JobStatus.Cancelled),
                    TotalRevenue = jobs.Where(j => j.Status == JobStatus.Completed).Sum(j => j.Price)
                };

                // Calculate outstanding amount from unpaid invoices
                // This would come from invoice service in a real implementation
                customerStats.OutstandingAmount = 0; // TODO: Calculate from invoices
            }
            catch (Exception ex)
            {
                // Log error but don't fail the page load
                Console.WriteLine($"Error calculating customer stats: {ex.Message}");
            }
        }

        private Task HandleLogout()
        {
            Console.WriteLine("Logout clicked");
            return Task.CompletedTask;
        }
    }

    public class CustomerStats
    {
        public int TotalJobs { get; set; }
        public int ActiveJobs { get; set; }
        public int NewJobs { get; set; }
        public int CompletedJobs { get; set; }
        public int CancelledJobs { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal OutstandingAmount { get; set; }
    }
}