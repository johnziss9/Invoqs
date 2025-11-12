using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Interfaces;
using Microsoft.JSInterop;

namespace Invoqs.Components.Pages
{
    public partial class Dashboard : ComponentBase
    {
        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private IJobService JobService { get; set; } = default!;
        [Inject] private IInvoiceService InvoiceService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        protected string currentUser = "User";
        protected bool isLoading = true;
        protected DashboardDataModel dashboardData = new();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    var token = await GetStoredTokenAsync();

                    if (string.IsNullOrEmpty(token))
                    {
                        Navigation.NavigateTo("/login", true);
                        return;
                    }

                    await LoadDashboardData();
                    StateHasChanged();
                }
                catch (JSDisconnectedException)
                {
                    // Circuit disconnected, ignore
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during dashboard initialization: {ex.Message}");
                    Navigation.NavigateTo("/login", true);
                }
            }
        }

        private void HandleUserLoaded(string userName)
        {
            currentUser = userName;
            StateHasChanged();
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
                var pendingInvoices = invoices.Where(i => 
                    i.Status == InvoiceStatus.Sent || 
                    i.Status == InvoiceStatus.Delivered || 
                    i.Status == InvoiceStatus.Overdue).ToList();

                // Calculate this week's revenue
                var twoWeeksAgo = DateTime.Now.AddDays(-14);

                var thisWeekInvoices = invoices.Where(i => 
                    i.Status == InvoiceStatus.Paid && 
                    i.PaymentDate.HasValue && 
                    i.PaymentDate >= oneWeekAgo).ToList();

                var lastWeekInvoices = invoices.Where(i => 
                    i.Status == InvoiceStatus.Paid && 
                    i.PaymentDate.HasValue && 
                    i.PaymentDate >= twoWeeksAgo && 
                    i.PaymentDate < oneWeekAgo).ToList();

                var weekRevenue = thisWeekInvoices.Sum(i => i.Total);
                var lastWeekRevenue = lastWeekInvoices.Sum(i => i.Total);

                // Calculate growth percentage
                decimal revenueGrowth = 0;
                if (lastWeekRevenue > 0)
                {
                    revenueGrowth = ((weekRevenue - lastWeekRevenue) / lastWeekRevenue) * 100;
                }

                dashboardData = new DashboardDataModel
                {
                    WeekRevenue = weekRevenue,
                    RevenueGrowth = revenueGrowth,

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

        private async Task<string?> GetStoredTokenAsync()
        {
            try
            {
                return await JSRuntime.InvokeAsync<string?>("localStorage.getItem", "authToken");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving auth token: {ex.Message}");
                return null;
            }
        }

        protected string GetTimeOfDay()
        {
            var hour = DateTime.Now.Hour;
            return hour switch
            {
                >= 5 and < 12 => "Καλημέρα",
                >= 12 and < 17 => "Καλό απόγευμα",
                >= 17 and < 21 => "Καλησπέρα",
                _ => "Καλησπέρα"
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

        // Method to refresh dashboard data
        protected async Task RefreshData()
        {
            await LoadDashboardData();
        }
    }
}