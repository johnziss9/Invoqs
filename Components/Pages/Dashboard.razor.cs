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

        protected string currentUser = "John Doe";
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

        protected async Task HandleLogout()
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
                await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "currentUser");
            }
            catch (JSDisconnectedException)
            {
                // Circuit disconnected, ignore
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
            {
                // Prerendering, ignore localStorage calls
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing localStorage during logout: {ex.Message}");
            }

            // Always redirect to login, even if localStorage clearing fails
            Navigation.NavigateTo("/login", true);
        }

        // Method to refresh dashboard data
        protected async Task RefreshData()
        {
            await LoadDashboardData();
        }
    }
}