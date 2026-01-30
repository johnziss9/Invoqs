using Microsoft.AspNetCore.Components;
using Invoqs.Models;

namespace Invoqs.Components.UI
{
    public partial class DuplicateEmailModal : ComponentBase
    {
        [Parameter] public bool IsVisible { get; set; }
        [Parameter] public List<DuplicateCustomerModel> DuplicateCustomers { get; set; } = new();
        [Parameter] public EventCallback OnCancel { get; set; }
        [Parameter] public EventCallback OnContinue { get; set; }

        private async Task Cancel()
        {
            await OnCancel.InvokeAsync();
        }

        private async Task ContinueAnyway()
        {
            await OnContinue.InvokeAsync();
        }
    }
}