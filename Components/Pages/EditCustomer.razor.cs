using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Invoqs.Models;
using Invoqs.Interfaces;

namespace Invoqs.Components.Pages
{
    public partial class EditCustomer : ComponentBase
    {
        [Parameter] public int CustomerId { get; set; }

        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        [SupplyParameterFromQuery] public string? ReturnUrl { get; set; }

        protected CustomerModel? customer;
        protected bool isLoading = true;
        protected bool isSaving = false;
        protected bool isDeleting = false;
        protected bool showDeleteConfirmation = false;
        protected string errorMessage = "";
        protected string successMessage = "";
        protected List<string> customerEmails = new();
        
        private ApiValidationError? validationErrors;
        protected bool showDuplicateModal = false;
        protected List<DuplicateCustomerModel> duplicateCustomers = new();
        private List<string> originalEmails = new();
        private bool skipDuplicateCheck = false;

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

        private string GetReturnUrl()
        {
            return !string.IsNullOrEmpty(ReturnUrl) ? ReturnUrl : "/customers";
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
                else
                {
                    // Load emails into the tag input component
                    customerEmails = customer.Emails.Select(e => e.Email).ToList();
                    originalEmails = new List<string>(customerEmails); // Store original emails
                    
                    await JSRuntime.InvokeVoidAsync("eval", $"document.title = 'Επεξεργασία Πελάτη - {customer.Name.Replace("'", "\\'")} - Invoqs'");
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
                validationErrors = null;

                // Validate emails
                if (!customerEmails.Any())
                {
                    errorMessage = "At least one email address is required.";
                    isSaving = false;
                    StateHasChanged();
                    return;
                }

                // Check for duplicates only for NEW emails (not in originalEmails)
                if (!skipDuplicateCheck)
                {
                    var newEmails = customerEmails.Except(originalEmails, StringComparer.OrdinalIgnoreCase).ToList();
                    
                    if (newEmails.Any())
                    {
                        var duplicateCheck = await CustomerService.CheckEmailDuplicatesAsync(newEmails, customer.Id);
                        
                        if (duplicateCheck.HasDuplicates)
                        {
                            duplicateCustomers = duplicateCheck.DuplicateCustomers;
                            showDuplicateModal = true;
                            isSaving = false;
                            StateHasChanged();
                            return;
                        }
                    }
                }

                // Reset skip flag for next save attempt
                skipDuplicateCheck = false;

                // Update customer emails
                customer.Emails = customerEmails.Select(e => new EmailModel 
                { 
                    Email = e,
                    CreatedDate = customer.Emails.FirstOrDefault(x => x.Email == e)?.CreatedDate ?? DateTime.Now
                }).ToList();

                var (updatedCustomer, errors) = await CustomerService.UpdateCustomerAsync(customer);

                if (updatedCustomer != null)
                {
                    customer = updatedCustomer;

                    successMessage = "Customer updated successfully!";
                    StateHasChanged();

                    await Task.Delay(1500);
                    Navigation.NavigateTo($"/customer/{customer.Id}", forceLoad: true);
                }
                else if (errors != null)
                {
                    validationErrors = errors;
                    errorMessage = "Please correct the validation errors below.";
                }
                else
                {
                    errorMessage = "Failed to update customer.";
                }
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
                    Navigation.NavigateTo("/customers", true);
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

        private string GetFieldErrorClass(string fieldName)
        {
            if (validationErrors?.GetFieldErrors(fieldName).Any() == true)
                return "form-control is-invalid";
            return "form-control";
        }

        private void CreateNewJob()
        {
            var returnUrl = $"/customer/{CustomerId}";
            Navigation.NavigateTo($"/customer/{CustomerId}/job/new?returnUrl={Uri.EscapeDataString(returnUrl)}", true);
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