using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Services;

namespace Invoqs.Components.Pages
{
    public partial class AddJob : ComponentBase
    {
        [Parameter] public int CustomerId { get; set; } = 0;

        [Inject] private IJobService JobService { get; set; } = default!;
        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        protected string currentUser = "John Doe";
        protected JobModel newJob = new();
        protected List<CustomerModel> customers = new();
        protected CustomerModel? selectedCustomer;
        protected bool isLoading = true;
        protected bool isSaving = false;
        protected string errorMessage = "";
        protected string successMessage = "";
        protected string? returnUrl;

        // Job type display properties
        protected string jobTypeIcon = "";
        protected string jobTypeDisplayName = "";

        protected override async Task OnInitializedAsync()
        {
            // Check if we have a return URL in the query string
            var uri = new Uri(Navigation.Uri);
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
            if (query.TryGetValue("returnUrl", out var returnUrlValue))
            {
                returnUrl = returnUrlValue.FirstOrDefault();
            }

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
                JobType.FortCliffService => "icon-cliff",
                _ => ""
            };

            jobTypeDisplayName = newJob.Type switch
            {
                JobType.SkipRental => "Skip Rental",
                JobType.SandDelivery => "Sand Delivery",
                JobType.FortCliffService => "Fort Cliff Service",
                _ => ""
            };
        }

        private async Task HandleValidSubmit()
        {
            try
            {
                isSaving = true;
                errorMessage = "";
                successMessage = "";

                // Validate customer selection
                if (newJob.CustomerId == 0)
                {
                    errorMessage = "Please select a customer.";
                    return;
                }

                // Set end date if status is Active (immediate start)
                if (newJob.Status == JobStatus.Active)
                {
                    // Don't set end date - it's active but not completed
                    // End date will be set when job is marked as completed
                }

                // Create the job
                var createdJob = await JobService.CreateJobAsync(newJob);

                if (createdJob != null)
                {
                    successMessage = "Job created successfully!";

                    // Navigate after a short delay to show success message
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(1500);
                        await InvokeAsync(() =>
                        {
                            if (CustomerId > 0)
                            {
                                // Return to customer jobs page
                                Navigation.NavigateTo($"/customer/{CustomerId}/jobs");
                            }
                            else
                            {
                                // Return to global jobs page or specific return URL
                                Navigation.NavigateTo(returnUrl ?? "/jobs");
                            }
                        });
                    });
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

        private void GoBack()
        {
            if (!string.IsNullOrEmpty(returnUrl))
            {
                Navigation.NavigateTo(returnUrl);
            }
            else if (CustomerId > 0)
            {
                // Go back to customer jobs page
                Navigation.NavigateTo($"/customer/{CustomerId}/jobs");
            }
            else
            {
                // Go back to global jobs page
                Navigation.NavigateTo("/jobs");
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