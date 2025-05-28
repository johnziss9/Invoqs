using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Services;

namespace Invoqs.Components.Pages
{
    public partial class Dashboard : ComponentBase
    {
        [Inject] private ICustomerService CustomerService { get; set; } = default!;
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

                // Load data from various services
                var customers = await CustomerService.GetAllCustomersAsync();

                // Simulate loading other data - replace with actual service calls
                await Task.Delay(800);

                dashboardData = new DashboardDataModel
                {
                    // Revenue metrics
                    WeekRevenue = 12450m,
                    RevenueGrowth = 12.5m,

                    // Job metrics
                    ActiveJobs = 23,
                    JobsScheduledToday = 5,
                    SkipRentals = 14,
                    SandDeliveries = 6,
                    FortCliffServices = 3,

                    // Customer metrics
                    TotalCustomers = customers.Count,
                    NewCustomersThisWeek = 3,

                    // Invoice metrics
                    PendingInvoices = 8,
                    PendingAmount = 12450m
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
            Navigation.NavigateTo(url, forceLoad: true);
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