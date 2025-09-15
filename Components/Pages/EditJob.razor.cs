using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Invoqs.Models;
using Invoqs.Services;

namespace Invoqs.Components.Pages
{
    public partial class EditJob : ComponentBase
    {
        [Parameter] public int JobId { get; set; }

        [Inject] private IJobService JobService { get; set; } = default!;
        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        [SupplyParameterFromQuery] public string? ReturnUrl { get; set; }

        protected string currentUser = "John Doe"; // Replace with actual user service
        protected JobModel? job;
        protected CustomerModel? customer;
        protected bool isLoading = true;
        protected bool isSaving = false;
        protected bool isDeleting = false;
        protected bool showDeleteConfirmation = false;
        protected string errorMessage = "";
        protected string successMessage = "";
        protected string? returnUrl;

        protected override async Task OnInitializedAsync()
        {
            // Check if we have a return URL in the query string
            var uri = new Uri(Navigation.Uri);
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
            if (query.TryGetValue("returnUrl", out var returnUrlValue))
            {
                returnUrl = returnUrlValue.FirstOrDefault();
            }

            await LoadJob();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (JobId > 0)
            {
                await LoadJob();
            }
        }

        private async Task LoadJob()
        {
            try
            {
                isLoading = true;
                errorMessage = "";

                job = await JobService.GetJobByIdAsync(JobId);

                if (job == null)
                {
                    errorMessage = "Job not found.";
                    return;
                }

                // Load customer information
                customer = await CustomerService.GetCustomerByIdAsync(job.CustomerId);
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading job: {ex.Message}";
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task HandleValidSubmit()
        {
            if (job == null) return;

            try
            {
                isSaving = true;
                errorMessage = "";
                successMessage = "";

                // Auto-set end date when marking as completed
                if (job.Status == JobStatus.Completed && !job.EndDate.HasValue)
                {
                    job.EndDate = DateTime.Now;
                }
                // Clear end date if no longer completed
                else if (job.Status != JobStatus.Completed)
                {
                    job.EndDate = null;
                }

                var success = await JobService.UpdateJobAsync(job);

                if (success)
                {
                    successMessage = "Job updated successfully!";
                    StateHasChanged();

                    await Task.Delay(1500);
                    Navigation.NavigateTo($"/job/{job.Id}", forceLoad: true);
                }
                else
                {
                    errorMessage = "Failed to update job.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error saving job: {ex.Message}";
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

        private async Task QuickStatusChange(JobStatus newStatus)
        {
            if (job == null) return;

            try
            {
                job.Status = newStatus;

                // Auto-set end date when marking as completed
                if (newStatus == JobStatus.Completed && !job.EndDate.HasValue)
                {
                    job.EndDate = DateTime.Now;
                }

                var success = await JobService.UpdateJobAsync(job);

                if (success)
                {
                    successMessage = $"Job status changed to {newStatus}!";

                    // Auto-hide success message
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(2000);
                        successMessage = "";
                        await InvokeAsync(StateHasChanged);
                    });
                }
                else
                {
                    errorMessage = "Failed to update job status.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error updating job status: {ex.Message}";
            }

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
            if (job == null) return;

            try
            {
                isDeleting = true;

                var success = await JobService.DeleteJobAsync(job.Id);

                if (success)
                {
                    // Navigate back to where we came from
                    GoBack();
                }
                else
                {
                    errorMessage = "Failed to delete job.";
                    showDeleteConfirmation = false;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error deleting job: {ex.Message}";
                showDeleteConfirmation = false;
            }
            finally
            {
                isDeleting = false;
                StateHasChanged();
            }
        }

        private void GoBack()
        {
            // Use return URL if available, otherwise intelligent fallback
            if (!string.IsNullOrEmpty(returnUrl))
            {
                Navigation.NavigateTo(returnUrl);
            }
            else if (customer != null)
            {
                // Go to customer jobs page
                Navigation.NavigateTo($"/customer/{customer.Id}/jobs");
            }
            else
            {
                // Fallback to all jobs page
                Navigation.NavigateTo("/jobs");
            }
        }

        private string GetReturnUrl()
        {
            return !string.IsNullOrEmpty(ReturnUrl) ? ReturnUrl : "/jobs";
        }

        private void GenerateInvoice()
        {
            if (job != null)
            {
                Navigation.NavigateTo($"/invoice/generate/{job.Id}");
            }
        }

        private Task HandleLogout()
        {
            Console.WriteLine("Logout clicked");
            return Task.CompletedTask;
        }

        protected string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "?";

            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();

            return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
        }
    }
}