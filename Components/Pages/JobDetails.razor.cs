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

        protected string currentUser = "John Doe"; // Replace with actual user service
        protected JobModel? job;
        protected CustomerModel? customer;
        protected bool isLoading = true;
        protected string? errorMessage;

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

        private async Task LoadJobDetailsAsync()
        {
            try
            {
                isLoading = true;
                errorMessage = null;

                job = await JobService.GetJobByIdAsync(JobId);
                
                if (job != null)
                {
                    customer = await CustomerService.GetCustomerByIdAsync(job.CustomerId);
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