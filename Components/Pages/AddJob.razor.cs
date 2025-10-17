using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Interfaces;
using Microsoft.JSInterop;

namespace Invoqs.Components.Pages
{
    public partial class AddJob : ComponentBase
    {
        [Parameter] public int CustomerId { get; set; } = 0;

        [Inject] private IJobService JobService { get; set; } = default!;
        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        [SupplyParameterFromQuery] public string? ReturnUrl { get; set; }

        protected string currentUser = "John Doe";
        protected JobModel newJob = new();
        protected List<CustomerModel> customers = new();
        protected CustomerModel? selectedCustomer;
        protected bool isLoading = true;
        protected bool isSaving = false;
        protected string errorMessage = "";
        protected string successMessage = "";

        // Job type display properties
        protected string jobTypeIcon = "";
        protected string jobTypeDisplayName = "";

        private ApiValidationError? validationErrors;

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        protected override async Task OnParametersSetAsync()
        {
            // Re-initialize job when customer ID changes
            InitializeNewJob();
            await LoadSelectedCustomer();
        }

        private async Task LoadData()
        {
            try
            {
                isLoading = true;
                errorMessage = "";

                // Load all customers
                customers = await CustomerService.GetAllCustomersAsync();

                // Initialize the new job
                InitializeNewJob();

                // Load selected customer if CustomerId is provided
                await LoadSelectedCustomer();
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading data: {ex.Message}";
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private void InitializeNewJob()
        {
            newJob = new JobModel
            {
                CustomerId = CustomerId,
                StartDate = DateTime.Today.AddDays(1), // Default to tomorrow
                Status = JobStatus.New, // Default to New status
                Type = JobType.SkipRental, // Default type
                CreatedDate = DateTime.Now
            };

            UpdateJobTypeDisplay();
        }

        private async Task LoadSelectedCustomer()
        {
            if (CustomerId > 0)
            {
                try
                {
                    selectedCustomer = await CustomerService.GetCustomerByIdAsync(CustomerId);
                    newJob.CustomerId = CustomerId;
                }
                catch (Exception ex)
                {
                    errorMessage = $"Error loading customer: {ex.Message}";
                }
            }
            else
            {
                selectedCustomer = null;
            }
        }

        private void OnCustomerChanged()
        {
            selectedCustomer = customers.FirstOrDefault(c => c.Id == newJob.CustomerId);
            StateHasChanged();
        }

        private void UpdateJobTypeDisplay()
        {
            jobTypeIcon = newJob.Type switch
            {
                JobType.SkipRental => "icon-skip",
                JobType.SandDelivery => "icon-delivery-truck",
                JobType.ForkLiftService => "icon-cliff",
                _ => ""
            };

            jobTypeDisplayName = newJob.Type switch
            {
                JobType.SkipRental => "Skip Rental",
                JobType.SandDelivery => "Sand Delivery",
                JobType.ForkLiftService => "Fork Lift Service",
                _ => ""
            };

            // Clear type-specific fields when job type changes
            ClearTypeSpecificFields();
            StateHasChanged();
        }

        private void OnSkipTypeChanged()
        {
            // Clear skip number if switching to Hook
            if (newJob.SkipType == "Hook")
            {
                newJob.SkipNumber = null;
            }
            StateHasChanged();
        }

        private void ClearTypeSpecificFields()
        {
            // Clear all type-specific fields when type changes
            newJob.SkipType = null;
            newJob.SkipNumber = null;
            newJob.SandMaterialType = null;
            newJob.SandDeliveryMethod = null;
            newJob.ForkliftSize = null;
        }


        private async Task HandleValidSubmit()
        {
            try
            {
                isSaving = true;
                errorMessage = "";
                successMessage = "";
                validationErrors = null;

                if (newJob.CustomerId == 0)
                {
                    errorMessage = "Please select a customer.";
                    return;
                }

                var (createdJob, errors) = await JobService.CreateJobAsync(newJob);

                if (createdJob != null)
                {
                    successMessage = "Job created successfully!";
                    StateHasChanged();

                    await Task.Delay(1500);
                    Navigation.NavigateTo($"/job/{createdJob.Id}", forceLoad: true);
                }
                else if (errors != null)
                {
                    validationErrors = errors;
                    errorMessage = "Please correct the validation errors below.";
                }
                else
                {
                    errorMessage = "Failed to create job.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error creating job: {ex.Message}";
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

        private string GetReturnUrl()
        {
            if (!string.IsNullOrEmpty(ReturnUrl))
                return ReturnUrl;
                
            // Context-specific fallbacks
            if (CustomerId > 0)
                return $"/customer/{CustomerId}/jobs";
            else
                return "/jobs";
        }

        private string GetFieldErrorClass(string fieldName)
        {
            if (validationErrors?.GetFieldErrors(fieldName).Any() == true)
                return "form-control is-invalid";
            return "form-control";
        }

        private void GoBack()
        {
            Navigation.NavigateTo(GetReturnUrl(), true);
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

        protected string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "?";

            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();

            return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
        }

        protected string GetStatusColor(JobStatus status)
        {
            return status switch
            {
                JobStatus.New => "secondary",
                JobStatus.Active => "primary",
                JobStatus.Completed => "success",
                JobStatus.Cancelled => "danger",
                _ => "secondary"
            };
        }

        protected string GetStatusIcon(JobStatus status)
        {
            return status switch
            {
                JobStatus.New => "bi-clock",
                JobStatus.Active => "bi-play-circle",
                JobStatus.Completed => "bi-check-circle",
                JobStatus.Cancelled => "bi-x-circle",
                _ => "bi-clock"
            };
        }
    }
}