using Microsoft.AspNetCore.Components;
using Invoqs.Models;

namespace Invoqs.Components.UI
{
    public partial class JobCard : ComponentBase
    {
        [Parameter] public JobModel Job { get; set; } = new();
        [Parameter] public CustomerModel? Customer { get; set; }
        [Parameter] public EventCallback<(JobModel job, JobStatus status)> OnStatusChange { get; set; }
        [Parameter] public EventCallback<JobModel> OnEdit { get; set; }
        [Parameter] public EventCallback<JobModel> OnDelete { get; set; }
        [Parameter] public EventCallback<JobModel> OnGenerateInvoice { get; set; }

        [Inject] private NavigationManager Navigation { get; set; } = default!;

        private void ViewDetails(int jobId)
        {
            var currentUrl = Navigation.Uri;
            Navigation.NavigateTo($"/job/{jobId}?returnUrl={Uri.EscapeDataString(currentUrl)}");
        }

        private async Task HandleGenerateInvoiceClick()
        {
            if (OnGenerateInvoice.HasDelegate)
            {
                await OnGenerateInvoice.InvokeAsync(Job);
            }
            else
            {
                var currentUrl = Navigation.Uri;
                var returnUrl = Uri.EscapeDataString(currentUrl);
                Navigation.NavigateTo($"/customer/{Job.CustomerId}/invoice/new?preselectedJobId={Job.Id}&returnUrl={returnUrl}");
            }
        }
    }
}