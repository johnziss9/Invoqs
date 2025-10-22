using Microsoft.AspNetCore.Components;
using Invoqs.Models;

namespace Invoqs.Components.UI
{
    public partial class CustomerCard : ComponentBase
    {
        [Parameter] public CustomerModel Customer { get; set; } = new();
        [Parameter] public EventCallback<CustomerModel> OnViewJobs { get; set; }
        [Parameter] public EventCallback<CustomerModel> OnDelete { get; set; }

        [Inject] private NavigationManager Navigation { get; set; } = default!;

        private bool showDeleteConfirmation = false;
        private bool isDeleting = false;
        private string? errorMessage;

        protected string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "?";

            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();

            return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
        }

        private void EditCustomer(int customerId)
        {
            var currentUrl = Navigation.Uri;
            Navigation.NavigateTo($"/customer/{customerId}/edit?returnUrl={Uri.EscapeDataString(currentUrl)}", true);
        }

        private void ShowDeleteConfirmation()
        {
            if (Customer.ActiveJobs > 0)
            {
                errorMessage = "Cannot delete a customer with active jobs. Please complete or cancel all active jobs first.";
                StateHasChanged();
                return;
            }

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
            isDeleting = true;
            StateHasChanged();

            try
            {
                await OnDelete.InvokeAsync(Customer);
                showDeleteConfirmation = false;
            }
            finally
            {
                isDeleting = false;
                StateHasChanged();
            }
        }

        private string GetDeleteButtonText()
        {
            if (Customer.ActiveJobs > 0)
                return "Cannot Delete (Active Jobs)";
            
            return "Delete Customer";
        }

        private string GetDeleteDisabledReason()
        {
            if (Customer.ActiveJobs > 0)
                return "Cannot delete customers with active jobs. Complete or cancel all active jobs first.";

            return "";
        }
    }
}