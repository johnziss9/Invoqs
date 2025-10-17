using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Interfaces;

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
        [Inject] private IJobService JobService { get; set; } = default!;
        [Inject] private ILogger<JobCard> Logger { get; set; } = default!;

        private bool showDeleteConfirmation = false;
        private bool isDeleting = false;
        private string? errorMessage;

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
            isDeleting = true;
            StateHasChanged();

            try
            {
                await OnDelete.InvokeAsync(Job);
                showDeleteConfirmation = false;
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

        private async Task HandleStatusChange(JobStatus newStatus)
        {
            try
            {
                var oldStatus = Job.Status;

                var result = await JobService.UpdateJobStatusAsync(Job.Id, newStatus);

                if (result.Success)
                {
                    Job.Status = newStatus;

                    if (newStatus == JobStatus.Completed && !Job.EndDate.HasValue)
                    {
                        Job.EndDate = DateTime.Now;
                    }
                    else if (newStatus != JobStatus.Completed && oldStatus == JobStatus.Completed)
                    {
                        Job.EndDate = null;
                    }
                }
                else if (result.ValidationErrors != null)
                {
                    errorMessage = string.Join(", ", result.ValidationErrors.GetAllErrors());
                }
                else
                {
                    errorMessage = "Failed to update job status";
                }

                StateHasChanged();
            }
            catch (Exception ex)
            {
                errorMessage = $"Error updating job status: {ex.Message}";
                StateHasChanged();
            }
        }
    }
}