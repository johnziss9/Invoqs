using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Invoqs.Models;
using Invoqs.Interfaces;

namespace Invoqs.Components.Pages
{
    public partial class CreateCustomerStatement : ComponentBase
    {
        [Inject] private ICustomerStatementService CustomerStatementService { get; set; } = default!;
        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        private CreateCustomerStatementModel model = new();
        private CustomerModel? preselectedCustomer;
        private List<CustomerModel> allCustomers = new();
        private List<CustomerModel> filteredCustomers = new();
        private CustomerModel? selectedCustomer;
        private string customerSearch = string.Empty;
        private bool showDropdown = false;
        private bool isLoadingCustomers = true;
        private bool isSubmitting = false;
        private bool submitAttempted = false;
        private string? errorMessage;

        protected override async Task OnInitializedAsync()
        {
            await LoadCustomers();
            await LoadPreselectedCustomer();
        }

        private async Task LoadPreselectedCustomer()
        {
            try
            {
                var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
                if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("customerId", out var value)
                    && int.TryParse(value, out int customerId) && customerId > 0)
                {
                    preselectedCustomer = await CustomerService.GetCustomerByIdAsync(customerId);
                    if (preselectedCustomer != null)
                    {
                        model.CustomerId = preselectedCustomer.Id;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading preselected customer: {ex.Message}");
            }
        }

        private async Task LoadCustomers()
        {
            try
            {
                isLoadingCustomers = true;
                allCustomers = await CustomerService.GetAllCustomersAsync();
                filteredCustomers = allCustomers;
            }
            catch (Exception ex)
            {
                errorMessage = $"Σφάλμα φόρτωσης πελατών: {ex.Message}";
            }
            finally
            {
                isLoadingCustomers = false;
                StateHasChanged();
            }
        }

        private void OnCustomerSearchInput(ChangeEventArgs e)
        {
            customerSearch = e.Value?.ToString() ?? string.Empty;
            FilterCustomers();
            showDropdown = true;
        }

        private void FilterCustomers()
        {
            if (string.IsNullOrWhiteSpace(customerSearch))
            {
                filteredCustomers = allCustomers;
            }
            else
            {
                var search = customerSearch.ToLower();
                filteredCustomers = allCustomers
                    .Where(c => c.Name.ToLower().Contains(search) || (c.Phone?.Contains(search) ?? false))
                    .ToList();
            }
        }

        private void ShowDropdown()
        {
            if (selectedCustomer == null)
            {
                FilterCustomers();
                showDropdown = true;
            }
        }

        private void SelectCustomer(CustomerModel customer)
        {
            selectedCustomer = customer;
            model.CustomerId = customer.Id;
            customerSearch = customer.Name;
            showDropdown = false;
            StateHasChanged();
        }

        private void ClearCustomerSelection()
        {
            selectedCustomer = null;
            model.CustomerId = null;
            customerSearch = string.Empty;
            showDropdown = false;
            StateHasChanged();
        }

        private async Task HandleValidSubmit()
        {
            // Ensure a customer is selected (preselected or via autocomplete)
            var effectiveCustomerId = preselectedCustomer?.Id ?? model.CustomerId;
            if (effectiveCustomerId == null || effectiveCustomerId == 0)
            {
                errorMessage = "Παρακαλώ επιλέξτε πελάτη.";
                return;
            }

            model.CustomerId = effectiveCustomerId;

            try
            {
                isSubmitting = true;
                errorMessage = null;
                StateHasChanged();

                var result = await CustomerStatementService.CreateCustomerStatementAsync(model);

                if (result != null)
                {
                    Navigation.NavigateTo($"/customer-statements/{result.Id}", forceLoad: true);
                }
                else
                {
                    errorMessage = "Δεν βρέθηκαν τιμολόγια για αυτόν τον πελάτη στην επιλεγμένη περίοδο.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Σφάλμα: {ex.Message}";
            }
            finally
            {
                isSubmitting = false;
                StateHasChanged();
            }
        }
    }
}
