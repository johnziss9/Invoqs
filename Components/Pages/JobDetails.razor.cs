using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Interfaces;
using Microsoft.JSInterop;

namespace Invoqs.Components.Pages
{
    public partial class JobDetailsBase : ComponentBase
    {
        [Parameter] public int JobId { get; set; }
        [SupplyParameterFromQuery] public string? ReturnUrl { get; set; }

        [Inject] protected IJobService JobService { get; set; } = default!;
        [Inject] protected ICustomerService CustomerService { get; set; } = default!;
        [Inject] protected NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        protected bool isCustomerDeleted = false;
        protected bool isLoading = true;
        protected string currentUser = "John Doe"; // Replace with actual user service
        protected string? errorMessage;
        protected bool showDeleteJobConfirmation = false;
        protected bool isDeletingJob = false;

        protected JobModel? job;
        protected CustomerModel? customer;

        protected override async Task OnInitializedAsync()
        {
            await LoadJobDetailsAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (JobId > 0)
            {
                await LoadJobDetailsAsync();
            }
        }

        private string GetReturnUrl()
        {
            return !string.IsNullOrEmpty(ReturnUrl) ? ReturnUrl : "/jobs";
        }

        private async Task LoadJobDetailsAsync()
        {
            try
            {
                isLoading = true;
                errorMessage = null;
                isCustomerDeleted = false;

                job = await JobService.GetJobByIdAsync(JobId);

                if (job != null)
                {
                    customer = await CustomerService.GetCustomerByIdAsync(job.CustomerId);

                    // Check if customer is soft deleted (when API supports it)
                    if (customer != null && customer.IsDeleted)
                    {
                        isCustomerDeleted = true;
                    }
                    else if (customer == null)
                    {
                        // Fallback for hard delete (current mock behavior)
                        isCustomerDeleted = true;
                        customer = new CustomerModel
                        {
                            Id = job.CustomerId,
                            Name = "[Deleted Customer]",
                            Email = "",
                            Phone = ""
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading job details: {ex.Message}";
                job = null;
                customer = null;
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        protected void ShowDeleteJobConfirmation()
        {
            showDeleteJobConfirmation = true;
            StateHasChanged();
        }

        protected void HideDeleteJobConfirmation()
        {
            showDeleteJobConfirmation = false;
            StateHasChanged();
        }

        protected async Task ConfirmDeleteJob()
        {
            if (job == null) return;

            try
            {
                isDeletingJob = true;
                StateHasChanged();

                var success = await JobService.DeleteJobAsync(job.Id);

                if (success)
                {
                    Navigation.NavigateTo(GetReturnUrl(), true);
                }
                else
                {
                    errorMessage = "Failed to delete job.";
                    showDeleteJobConfirmation = false;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error deleting job: {ex.Message}";
                showDeleteJobConfirmation = false;
            }
            finally
            {
                isDeletingJob = false;
                StateHasChanged();
            }
        }

        protected async Task StartJob(int jobId)
        {
            try
            {
                if (job != null)
                {
                    var (success, errors) = await JobService.UpdateJobStatusAsync(job.Id, JobStatus.Active);

                    if (success)
                    {
                        job.Status = JobStatus.Active;
                        StateHasChanged();
                    }
                    else if (errors != null)
                    {
                        errorMessage = string.Join(", ", errors.GetAllErrors());
                    }
                    else
                    {
                        errorMessage = "Failed to start job";
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error starting job: {ex.Message}";
            }
        }

        protected async Task CompleteJob(int jobId)
        {
            try
            {
                if (job != null)
                {
                    var (success, errors) = await JobService.UpdateJobStatusAsync(job.Id, JobStatus.Completed);

                    if (success)
                    {
                        job.Status = JobStatus.Completed;
                        job.EndDate = DateTime.Now;
                        StateHasChanged();
                    }
                    else if (errors != null)
                    {
                        errorMessage = string.Join(", ", errors.GetAllErrors());
                    }
                    else
                    {
                        errorMessage = "Failed to complete job";
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error completing job: {ex.Message}";
            }
        }

        protected void GoBack()
        {
            if (!string.IsNullOrWhiteSpace(ReturnUrl))
            {
                Navigation.NavigateTo(ReturnUrl);
            }
            else
            {
                Navigation.NavigateTo("/jobs");
            }
        }

        protected void CreateInvoice()
        {
            if (job != null)
            {
                var currentUrl = Navigation.Uri;
                Navigation.NavigateTo($"/customer/{job.CustomerId}/invoice/new?preselectedJobId={JobId}&returnUrl={Uri.EscapeDataString(currentUrl)}", true);
            }
        }

        protected void ViewInvoice(int? invoiceId)
        {
            if (invoiceId.HasValue)
            {
                Navigation.NavigateTo($"/invoice/{invoiceId.Value}", true);
            }
        }

        protected async Task HandleLogout()
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
                await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "currentUser");
            }
            catch (JSDisconnectedException)
            {
                // Circuit disconnected, ignore
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
            {
                // Prerendering, ignore localStorage calls
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing localStorage during logout: {ex.Message}");
            }

            // Always redirect to login, even if localStorage clearing fails
            Navigation.NavigateTo("/login", true);
        }
    }
}