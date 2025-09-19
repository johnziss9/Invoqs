using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Services;

namespace Invoqs.Components.Pages
{
    public partial class Customers : ComponentBase
    {
        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        // Component state
        protected string currentUser = "John Doe";
        protected string searchTerm = "";
        protected string sortBy = "name";
        protected string filterBy = "all";
        protected string viewMode = "grid";
        protected bool isLoading = true;
        protected string errorMessage = "";

        protected List<CustomerModel> customers = new();

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

                customers = await CustomerService.GetAllCustomersAsync();
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading customers: {ex.Message}";
                customers = new List<CustomerModel>();
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
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
                    "active" => filtered.Where(c => c.ActiveJobs > 0),
                    "inactive" => filtered.Where(c => c.ActiveJobs == 0),
                    _ => filtered
                };

                // Apply sorting
                filtered = sortBy switch
                {
                    "created" => filtered.OrderByDescending(c => c.CreatedDate),
                    "revenue" => filtered.OrderByDescending(c => c.TotalRevenue),
                    "jobs" => filtered.OrderByDescending(c => c.ActiveJobs + c.CompletedJobs),
                    _ => filtered.OrderBy(c => c.Name)
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

        // Event handlers with StateHasChanged to force re-render
        protected void OnSearchChanged(ChangeEventArgs e)
        {
            searchTerm = e.Value?.ToString() ?? "";
            StateHasChanged();
        }

        protected void OnSortChanged(ChangeEventArgs e)
        {
            sortBy = e.Value?.ToString() ?? "name";
            StateHasChanged();
        }

        protected void OnFilterChanged(ChangeEventArgs e)
        {
            filterBy = e.Value?.ToString() ?? "all";
            StateHasChanged();
        }

        protected void OnViewModeChanged(string newMode)
        {
            viewMode = newMode;
            StateHasChanged();
        }

        protected Task HandleLogout()
        {
            // Implement logout logic
            Console.WriteLine("Logout clicked");
            return Task.CompletedTask;
        }

        protected Task HandleViewJobs(CustomerModel customer)
        {
            // Navigate to customer jobs page
            Console.WriteLine($"View jobs for {customer.Name}");
            Navigation.NavigateTo($"/customer/{customer.Id}/jobs");
            return Task.CompletedTask;
        }

        protected Task HandleEditCustomer(CustomerModel customer)
        {
            // Navigate to edit customer page
            Console.WriteLine($"Edit customer {customer.Name}");
            Navigation.NavigateTo($"/customer/{customer.Id}/edit");
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