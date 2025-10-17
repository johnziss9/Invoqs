using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Interfaces;
using Microsoft.JSInterop;

namespace Invoqs.Components.Pages
{
    public partial class Customers : ComponentBase
    {
        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        // Component state
        protected string currentUser = "John Doe";
        protected string searchTerm = "";
        protected string sortBy = "name";
        protected string filterBy = "all";
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