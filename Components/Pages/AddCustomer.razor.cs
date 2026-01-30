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
        protected List<string> customerEmails = new();
        
        private ApiValidationError? validationErrors;
        protected bool showDuplicateModal = false;
        protected List<DuplicateCustomerModel> duplicateCustomers = new();
        private bool skipDuplicateCheck = false;

        protected override void OnInitialized()
        {
            // Initialize with default values
            newCustomer = new CustomerModel
            {
                Name = "",
                Phone = "",
                CompanyRegistrationNumber = "",
                VatNumber = "",
                Notes = "",
                CreatedDate = DateTime.Now,
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

                // Validate emails
                if (!customerEmails.Any())
                {
                    errorMessage = "At least one email address is required.";
                    isSaving = false;
                    return;
                }

                // Check for duplicates if not already skipped
                if (!skipDuplicateCheck)
                {
                    var duplicateCheck = await CustomerService.CheckEmailDuplicatesAsync(customerEmails);
                    
                    if (duplicateCheck.HasDuplicates)
                    {
                        duplicateCustomers = duplicateCheck.DuplicateCustomers;
                        showDuplicateModal = true;
                        isSaving = false;
                        StateHasChanged();
                        return;
                    }
                }

                // Reset skip flag for next save attempt
                skipDuplicateCheck = false;

                // Set emails on customer model
                newCustomer.Emails = customerEmails.Select(e => new EmailModel 
                { 
                    Email = e,
                    CreatedDate = DateTime.Now
                }).ToList();

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

        private void HandleDuplicateCancel()
        {
            showDuplicateModal = false;
            skipDuplicateCheck = false;
            StateHasChanged();
        }

        private async Task HandleDuplicateContinue()
        {
            showDuplicateModal = false;
            skipDuplicateCheck = true;
            StateHasChanged();
            
            // Retry the save
            await HandleValidSubmit();
        }
    }
}