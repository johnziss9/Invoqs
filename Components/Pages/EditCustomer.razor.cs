using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Invoqs.Models;
using Invoqs.Services;

namespace Invoqs.Components.Pages
{
    public partial class EditCustomer : ComponentBase
    {
        [Parameter] public int CustomerId { get; set; }

        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        protected string currentUser = "John Doe"; // Replace with actual user service
        protected CustomerModel? customer;
        protected bool isLoading = true;
        protected bool isSaving = false;
        protected bool isDeleting = false;
        protected bool showDeleteConfirmation = false;
        protected string errorMessage = "";
        protected string successMessage = "";

        protected override async Task OnInitializedAsync()
        {
            await LoadCustomer();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (CustomerId > 0)
            {
                await LoadCustomer();
            }
        }

        private async Task LoadCustomer()
        {
            try
            {
                isLoading = true;
                errorMessage = "";

                customer = await CustomerService.GetCustomerByIdAsync(CustomerId);

                if (customer == null)
                {
                    errorMessage = "Customer not found.";
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

        private async Task HandleValidSubmit()
        {
            if (customer == null) return;

            try
            {
                isSaving = true;
                errorMessage = "";
                successMessage = "";

                var updatedCustomer = await CustomerService.UpdateCustomerAsync(customer);
                customer = updatedCustomer; // Update with returned data

                successMessage = "Customer updated successfully!";

                // Auto-hide success message after 3 seconds
                _ = Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    successMessage = "";
                    await InvokeAsync(StateHasChanged);
                });
            }
            catch (Exception ex)
            {
                errorMessage = $"Error saving customer: {ex.Message}";
            }
            finally
            {
                isSaving = false;
                StateHasChanged();
            }
        }

        private void HandleInvalidSubmit()
        {
            errorMessage = "Please check the form for errors and try again.";
            successMessage = "";
            StateHasChanged();
        }

        private void ShowDeleteConfirmation()
        {
            showDeleteConfirmation = true;
            StateHasChanged();
        }

        private void HideDeleteConfirmation()
        {
            showDeleteConfirmation = false;
            StateHasChanged();
        }

        private async Task ConfirmDelete()
        {
            if (customer == null) return;

            try
            {
                isDeleting = true;

                var success = await CustomerService.DeleteCustomerAsync(customer.Id);

                if (success)
                {
                    // Navigate back to customers list
                    Navigation.NavigateTo("/customers");
                }
                else
                {
                    errorMessage = "Failed to delete customer.";
                    showDeleteConfirmation = false;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error deleting customer: {ex.Message}";
                showDeleteConfirmation = false;
            }
            finally
            {
                isDeleting = false;
                StateHasChanged();
            }
        }

        private Task HandleLogout()
        {
            Console.WriteLine("Logout clicked");
            return Task.CompletedTask;
        }

        private void CreateNewJob()
        {
            // Navigate to create job page for this customer
            Console.WriteLine($"Create new job for customer {customer?.Name}");
            Navigation.NavigateTo($"/customer/{customer?.Id}/jobs/new");
        }
    }
}