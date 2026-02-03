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

        // Address autocomplete properties
        protected string addressInputValue = "";
        protected List<string> addressSuggestions = new();
        protected bool showAddressSuggestions = false;
        protected bool isSearchingAddresses = false;
        private Timer? addressSearchTimer;

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
                JobDate = DateTime.Today, // Default to today
                Type = JobType.SkipRental, // Default type
                CreatedDate = DateTime.Now
            };

            // Reset address input value
            addressInputValue = "";

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

        protected async Task OnAddressFocus()
        {
            // Show all customer addresses on focus
            if (newJob.CustomerId > 0)
            {
                await LoadCustomerAddresses();
            }
        }

        private async Task LoadCustomerAddresses()
        {
            try
            {
                isSearchingAddresses = true;
                StateHasChanged();

                // Pass empty query and customer ID to get all customer addresses
                addressSuggestions = await JobService.SearchAddressesAsync("", newJob.CustomerId);
                showAddressSuggestions = addressSuggestions.Any();

                // Update job address with current input value
                newJob.Address = addressInputValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading addresses: {ex.Message}");
                addressSuggestions.Clear();
                showAddressSuggestions = false;
            }
            finally
            {
                isSearchingAddresses = false;
                StateHasChanged();
            }
        }

        private void UpdateJobTypeDisplay()
        {
            jobTypeIcon = newJob.Type switch
            {
                JobType.SkipRental => "icon-skip",
                JobType.SandDelivery => "icon-delivery-truck",
                JobType.ForkLiftService => "icon-cliff",
                JobType.Transfer => "icon-transfer",
                _ => ""
            };

            jobTypeDisplayName = newJob.Type switch
            {
                JobType.SkipRental => "Skip Rental",
                JobType.SandDelivery => "Sand Delivery",
                JobType.ForkLiftService => "Fork Lift Service",
                JobType.Transfer => "Transfer",
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

        protected string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "?";

            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();

            return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
        }

        protected void OnAddressInput(ChangeEventArgs e)
        {
            var value = e.Value?.ToString() ?? "";

            // Update local input value immediately (prevents input clearing)
            addressInputValue = value;

            // Cancel previous timer
            addressSearchTimer?.Dispose();

            if (string.IsNullOrWhiteSpace(value))
            {
                // Clear the job address when input is empty
                newJob.Address = "";

                // Show all customer addresses if empty
                if (newJob.CustomerId > 0)
                {
                    addressSearchTimer = new Timer(async _ =>
                    {
                        await InvokeAsync(async () =>
                        {
                            await LoadCustomerAddresses();
                        });
                    }, null, 300, Timeout.Infinite);
                }
                else
                {
                    showAddressSuggestions = false;
                    addressSuggestions.Clear();
                }
                return;
            }

            // Debounce: wait 300ms before searching
            addressSearchTimer = new Timer(async _ =>
            {
                await InvokeAsync(async () =>
                {
                    await SearchAddresses(value);
                });
            }, null, 300, Timeout.Infinite);
        }

        private async Task SearchAddresses(string query)
        {
            try
            {
                isSearchingAddresses = true;
                StateHasChanged();

                addressSuggestions = await JobService.SearchAddressesAsync(query, newJob.CustomerId);
                showAddressSuggestions = addressSuggestions.Any();

                // Update job address with current input value
                newJob.Address = addressInputValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching addresses: {ex.Message}");
                addressSuggestions.Clear();
                showAddressSuggestions = false;
            }
            finally
            {
                isSearchingAddresses = false;
                StateHasChanged();
            }
        }

        protected void SelectAddress(string address)
        {
            // Update both the input value and the model
            addressInputValue = address;
            newJob.Address = address;
            showAddressSuggestions = false;
            addressSuggestions.Clear();
            StateHasChanged();
        }

        protected void HideAddressSuggestions()
        {
            // Delay hiding to allow click events to register
            Task.Delay(200).ContinueWith(_ =>
            {
                InvokeAsync(() =>
                {
                    showAddressSuggestions = false;
                    StateHasChanged();
                });
            });
        }

        public void Dispose()
        {
            addressSearchTimer?.Dispose();
        }
    }
}