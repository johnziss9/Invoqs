using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Services;

namespace Invoqs.Components.Pages
{
    public partial class JobDetailsBase : ComponentBase
    {
        [Parameter] public int JobId { get; set; }
        [SupplyParameterFromQuery] public string? ReturnUrl { get; set; }

        [Inject] protected IJobService JobService { get; set; } = default!;
        [Inject] protected ICustomerService CustomerService { get; set; } = default!;
        [Inject] protected NavigationManager Navigation { get; set; } = default!;

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
                    job.Status = JobStatus.Active;

                    await JobService.UpdateJobAsync(job);
                }
                
                StateHasChanged();
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
                    job.Status = JobStatus.Completed;
                    job.EndDate = DateTime.Now;

                    await JobService.UpdateJobAsync(job);
                }

                StateHasChanged();
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
                Navigation.NavigateTo($"/customer/{job.CustomerId}/invoice/new?preselectedJobId={JobId}");
            }
        }

        protected void ViewInvoice(int? invoiceId)
        {
            if (invoiceId.HasValue)
            {
                Navigation.NavigateTo($"/invoice/{invoiceId.Value}");
            }
        }

        protected Task HandleLogout()
        {
            Console.WriteLine("Logout clicked");
            return Task.CompletedTask;
        }
    }
}