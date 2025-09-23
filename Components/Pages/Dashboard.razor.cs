using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Interfaces;

namespace Invoqs.Components.Pages
{
    public partial class Dashboard : ComponentBase
    {
        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private IJobService JobService { get; set; } = default!;
        [Inject] private IInvoiceService InvoiceService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        // [Inject] private IJobService JobService { get; set; } = default!;
        // [Inject] private IInvoiceService InvoiceService { get; set; } = default!;

        protected string currentUser = "John Doe";
        protected bool isLoading = true;
        protected DashboardDataModel dashboardData = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadDashboardData();
        }

        private async Task LoadDashboardData()
        {
            try
            {
                isLoading = true;

                // Load data from all services
                var customers = await CustomerService.GetAllCustomersAsync();
                var jobs = await JobService.GetAllJobsAsync();
                var invoices = await InvoiceService.GetAllInvoicesAsync();

                // Calculate active jobs by type
                var activeJobs = jobs.Where(j => j.Status == JobStatus.Active).ToList();
                var skipRentals = activeJobs.Count(j => j.Type == JobType.SkipRental);
                var sandDeliveries = activeJobs.Count(j => j.Type == JobType.SandDelivery);
                var forkLiftServices = activeJobs.Count(j => j.Type == JobType.ForkLiftService);

                // Calculate jobs scheduled today
                var today = DateTime.Today;
                var jobsToday = jobs.Count(j => j.StartDate.Date == today && j.Status == JobStatus.New);

                // Calculate new customers this week
                var oneWeekAgo = DateTime.Now.AddDays(-7);
                var newCustomersThisWeek = customers.Count(c => c.CreatedDate >= oneWeekAgo);

                // Calculate pending invoices and amount
                var pendingInvoices = invoices.Where(i => i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue).ToList();

                dashboardData = new DashboardDataModel
                {
                    // Revenue metrics - get from invoice service
                    WeekRevenue = await InvoiceService.GetWeeklyRevenueAsync(),
                    RevenueGrowth = 12.5m, // Calculate vs previous week if needed

                    // Job metrics
                    ActiveJobs = activeJobs.Count,
                    JobsScheduledToday = jobsToday,
                    SkipRentals = skipRentals,
                    SandDeliveries = sandDeliveries,
                    ForkLiftServices = forkLiftServices,

                    // Customer metrics
                    TotalCustomers = customers.Count,
                    NewCustomersThisWeek = newCustomersThisWeek,

                    // Invoice metrics
                    PendingInvoices = pendingInvoices.Count,
                    PendingAmount = pendingInvoices.Sum(i => i.Total)
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading dashboard data: {ex.Message}");
                // Load with default/empty data
                dashboardData = new DashboardDataModel();
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        protected string GetTimeOfDay()
        {
            var hour = DateTime.Now.Hour;
            return hour switch
            {
                >= 5 and < 12 => "morning",
                >= 12 and < 17 => "afternoon",
                >= 17 and < 21 => "evening",
                _ => "evening"
            };
        }

        protected void NavigateToPage(string url)
        {
            if (url == "/customer/new")
            {
                var currentUrl = Navigation.Uri;
                Navigation.NavigateTo($"/customer/new?returnUrl={Uri.EscapeDataString(currentUrl)}", forceLoad: true);
            }
            else if (url == "/job/new")
            {
                var currentUrl = Navigation.Uri;
                Navigation.NavigateTo($"/job/new?returnUrl={Uri.EscapeDataString(currentUrl)}", forceLoad: true);
            }
            else if (url == "/invoice/new")
            {
                var currentUrl = Navigation.Uri;
                Navigation.NavigateTo($"/invoice/new?returnUrl={Uri.EscapeDataString(currentUrl)}", forceLoad: true);
            }
            else
            {
                Navigation.NavigateTo(url, forceLoad: true);
            }
        }

        protected Task HandleLogout()
        {
            Console.WriteLine("Logout clicked");
            return Task.CompletedTask;
        }

        // Method to refresh dashboard data
        protected async Task RefreshData()
        {
            await LoadDashboardData();
        }
    }
}