using Microsoft.AspNetCore.Components;
using Invoqs.Models;

namespace Invoqs.Components.Pages
{
    public partial class Customers : ComponentBase
    {
        // Sample data
        protected string currentUser = "John Doe";
        protected string searchTerm = "";
        protected string sortBy = "name";
        protected string filterBy = "all";
        protected string viewMode = "grid";
        
        protected List<CustomerModel> customers = new();

        protected override void OnInitialized()
        {
            LoadFakeData();
        }

        private void LoadFakeData()
        {
            customers = new List<CustomerModel>
            {
                new CustomerModel { Id = 1, Name = "ABC Construction Ltd", Email = "contact@abcconstruction.co.uk", Phone = "01234 567890", ActiveJobs = 3, CompletedJobs = 12, TotalRevenue = 15500m, CreatedDate = DateTime.Now.AddMonths(-6) },
                new CustomerModel { Id = 2, Name = "Smith & Sons Builders", Email = "info@smithbuilders.com", Phone = "01234 567891", ActiveJobs = 1, CompletedJobs = 8, TotalRevenue = 9200m, CreatedDate = DateTime.Now.AddMonths(-3) },
                new CustomerModel { Id = 3, Name = "Green Landscapes", Email = "hello@greenlandscapes.co.uk", Phone = "01234 567892", ActiveJobs = 0, CompletedJobs = 5, TotalRevenue = 3400m, CreatedDate = DateTime.Now.AddMonths(-2) },
                new CustomerModel { Id = 4, Name = "Urban Development Corp", Email = "projects@urbandevelopment.com", Phone = "01234 567893", ActiveJobs = 5, CompletedJobs = 18, TotalRevenue = 28900m, CreatedDate = DateTime.Now.AddMonths(-8) },
                new CustomerModel { Id = 5, Name = "Residential Renovations", Email = "bookings@resrenovations.co.uk", Phone = "01234 567894", ActiveJobs = 2, CompletedJobs = 7, TotalRevenue = 7800m, CreatedDate = DateTime.Now.AddMonths(-4) },
                new CustomerModel { Id = 6, Name = "Coastal Builders", Email = "team@coastalbuilders.com", Phone = "01234 567895", ActiveJobs = 1, CompletedJobs = 4, TotalRevenue = 5200m, CreatedDate = DateTime.Now.AddMonths(-1) }
            };
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

        // StateHasChanged forcing re-render
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
            // TODO Implement logout logic
            Console.WriteLine("Logout clicked");
            return Task.CompletedTask;
        }

        protected Task HandleViewJobs(CustomerModel customer)
        {
            // TODO Navigate to customer jobs page
            Console.WriteLine($"View jobs for {customer.Name}");
            // TODO NavigationManager.NavigateTo($"/customer/{customer.Id}/jobs");
            return Task.CompletedTask;
        }

        protected Task HandleEditCustomer(CustomerModel customer)
        {
            // TODO Navigate to edit customer page
            Console.WriteLine($"Edit customer {customer.Name}");
            // TODO NavigationManager.NavigateTo($"/customer/{customer.Id}/edit");
            return Task.CompletedTask;
        }

        protected Task HandleDeleteCustomer(CustomerModel customer)
        {
            // TODO Show confirmation dialog and delete
            Console.WriteLine($"Delete customer {customer.Name}");
            // TODO Add confirmation dialog logic here
            return Task.CompletedTask;
        }

        protected Task ShowAddCustomerModal()
        {
            // TODO Show add customer modal
            Console.WriteLine("Add new customer");
            // TODO NavigationManager.NavigateTo("/customer/new");
            return Task.CompletedTask;
        }
    }
}