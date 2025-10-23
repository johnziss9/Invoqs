using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Interfaces;
using Microsoft.JSInterop;

namespace Invoqs.Components.Pages
{
    public partial class AddCustomer : ComponentBase
    {
        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        [SupplyParameterFromQuery] public string? ReturnUrl { get; set; }

        protected CustomerModel newCustomer = new();
        protected bool isSaving = false;
        protected string errorMessage = "";
        protected string successMessage = "";
        
        private ApiValidationError? validationErrors;

        protected override void OnInitialized()
        {
            // Initialize with default values
            newCustomer = new CustomerModel
            {
                Name = "",
                Email = "",
                Phone = "",
                CompanyRegistrationNumber = "",
                VatNumber = "",
                Notes = "",
                CreatedDate = DateTime.Now,
                ActiveJobs = 0,
                CompletedJobs = 0,
                TotalRevenue = 0
            };
        }

        private async Task HandleValidSubmit()
        {
            try
            {
                isSaving = true;
                errorMessage = "";
                successMessage = "";
                validationErrors = null;

                var (createdCustomer, errors) = await CustomerService.CreateCustomerAsync(newCustomer);

                if (createdCustomer != null)
                {
                    successMessage = $"Customer '{createdCustomer.Name}' created successfully!";
                    StateHasChanged();

                    // Show success message briefly, then navigate to customer details
                    await Task.Delay(1500);
                    Navigation.NavigateTo($"/customer/{createdCustomer.Id}", forceLoad: true);
                }
                else if (errors != null)
                {
                    validationErrors = errors;
                    errorMessage = "Please correct the validation errors below.";
                }
                else
                {
                    errorMessage = "Failed to create customer.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error creating customer: {ex.Message}";
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

        private string GetFieldErrorClass(string fieldName)
        {
            if (validationErrors?.GetFieldErrors(fieldName).Any() == true)
                return "form-control is-invalid";
            return "form-control";
        }

        protected string GetReturnUrl()
        {
            return string.IsNullOrWhiteSpace(ReturnUrl) ? "/customers" : ReturnUrl;
        }

        protected string GetReturnText()
        {
            return string.IsNullOrWhiteSpace(ReturnUrl) ? "Customers" : "Previous Page";
        }

        protected string GetCustomerInitials()
        {
            if (string.IsNullOrWhiteSpace(newCustomer.Name))
                return "?";

            var parts = newCustomer.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();

            return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
        }

        protected void UpdatePreview()
        {
            // This method is called after each field update to refresh the preview
            StateHasChanged();
        }

    }
}