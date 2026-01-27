using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Interfaces;

namespace Invoqs.Components.UI
{
    public partial class JobCard : ComponentBase
    {
        [Parameter] public JobModel Job { get; set; } = new();
        [Parameter] public CustomerModel? Customer { get; set; }
        [Parameter] public EventCallback<JobModel> OnEdit { get; set; }
        [Parameter] public EventCallback<JobModel> OnDelete { get; set; }
        [Parameter] public EventCallback<JobModel> OnGenerateInvoice { get; set; }

        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJobService JobService { get; set; } = default!;
        [Inject] private ILogger<JobCard> Logger { get; set; } = default!;

        private bool showDeleteConfirmation = false;
        private bool isDeleting = false;
        private string? errorMessage;

        private void ShowDeleteConfirmation()
        {
            if (Job.IsInvoiced)
            {
                errorMessage = "Cannot delete a job that has been invoiced. Please remove it from the invoice first.";
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
                await OnDelete.InvokeAsync(Job);
                showDeleteConfirmation = false;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error deleting job: {ex.Message}";
                Logger.LogError(ex, "Error deleting job {JobId}", Job.Id);
            }
            finally
            {
                isDeleting = false;
                StateHasChanged();
            }
        }

        private void ViewDetails(int jobId)
        {
            var currentUrl = Navigation.Uri;
            Navigation.NavigateTo($"/job/{jobId}?returnUrl={Uri.EscapeDataString(currentUrl)}", true);
        }

        private void HandleGenerateInvoiceClick()
        {
            var currentUrl = Navigation.Uri;
            var returnUrl = Uri.EscapeDataString(currentUrl);
            Navigation.NavigateTo($"/customer/{Job.CustomerId}/invoice/new?preselectedJobId={Job.Id}&returnUrl={returnUrl}", true);
        }

        private void HandleEditJob(int jobId)
        {
            var currentUrl = Navigation.Uri;
            Navigation.NavigateTo($"/job/{jobId}/edit?returnUrl={Uri.EscapeDataString(currentUrl)}", true);
        }

        private string GetDeleteButtonText()
        {
            if (Job.IsInvoiced)
                return "Cannot Delete (Invoiced)";
            
            return "Delete Job";
        }

        private string GetDeleteDisabledReason()
        {
            if (Job.IsInvoiced)
                return "Cannot delete invoiced jobs. Remove from invoice first.";
            
            return "";
        }
    }
}