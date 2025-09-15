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
            Navigation.NavigateTo($"/customer/{customerId}/edit?returnUrl={Uri.EscapeDataString(currentUrl)}");
        }
    }
}