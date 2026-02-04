using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Invoqs.Models;
using Invoqs.Interfaces;

namespace Invoqs.Components.Pages
{
    public partial class EditJob : ComponentBase
    {
        [Parameter] public int JobId { get; set; }

        [Inject] private IJobService JobService { get; set; } = default!;
        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        [SupplyParameterFromQuery] public string? ReturnUrl { get; set; }

        protected JobModel? job;
        protected CustomerModel? customer;
        protected bool isLoading = true;
        protected bool isSaving = false;
        protected bool isDeleting = false;
        protected bool showDeleteConfirmation = false;
        protected bool isInvoiced = false;
        protected bool canDeleteJob = true;
        protected bool isCustomerDeleted = false;
        protected string errorMessage = "";
        protected string successMessage = "";

        private ApiValidationError? validationErrors;

        // Address autocomplete properties
        protected string addressInputValue = "";
        protected List<string> addressSuggestions = new();
        protected bool showAddressSuggestions = false;
        protected bool isSearchingAddresses = false;
        private System.Threading.Timer? addressSearchTimer;

        protected override async Task OnInitializedAsync()
        {
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

                await JSRuntime.InvokeVoidAsync("eval", $"document.title = 'Επεξεργασία Εργασίας - {job.Title.Replace("'", "\\'")} - Invoqs'");

                isInvoiced = job.IsInvoiced;
                isCustomerDeleted = job.CustomerIsDeleted;

                // Initialize address input value with job's address
                addressInputValue = job.Address ?? "";

                // Determine if job can be deleted
                // Cannot delete invoiced jobs
                canDeleteJob = !isInvoiced;

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

        private void OnSkipTypeChanged()
        {
            if (job == null) return;
            
            // Clear skip number if switching to Hook
            if (job.SkipType == "Hook")
            {
                job.SkipNumber = null;
            }
            StateHasChanged();
        }

        private void OnJobTypeChanged()
        {
            if (job == null) return;
            
            // Clear ALL type-specific fields when job type changes
            job.SkipType = null;
            job.SkipNumber = null;
            job.SandMaterialType = null;
            job.SandDeliveryMethod = null;
            job.ForkliftSize = null;
            
            StateHasChanged();
        }

        private async Task HandleValidSubmit()
        {
            if (job == null) return;

            try
            {
                isSaving = true;
                errorMessage = "";
                successMessage = "";
                validationErrors = null;

                // Sync the address from input to model before submitting
                job.Address = addressInputValue;

                var (success, errors) = await JobService.UpdateJobAsync(job);

                if (success)
                {
                    successMessage = "Job updated successfully!";
                    StateHasChanged();

                    await Task.Delay(1500);
                    Navigation.NavigateTo($"/job/{job.Id}", forceLoad: true);
                }
                else if (errors != null)
                {
                    validationErrors = errors;
                    errorMessage = "Please correct the validation errors below.";
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

        private void ShowDeleteConfirmation()
        {
            if (job == null) return;

            // Validate job can be deleted
            if (isInvoiced)
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
            if (job == null) return;

            try
            {
                isDeleting = true;
                StateHasChanged();

                var success = await JobService.DeleteJobAsync(job.Id);

                if (success)
                {
                    showDeleteConfirmation = false;
                    successMessage = "Job deleted successfully!";
                    StateHasChanged();

                    // Navigate back after brief delay to show success message
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(1500);
                        await InvokeAsync(() => GoBack());
                    });
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
            if (!string.IsNullOrEmpty(ReturnUrl))
            {
                Navigation.NavigateTo(ReturnUrl, true);
            }
            else if (customer != null)
            {
                // Go to customer jobs page
                Navigation.NavigateTo($"/customer/{customer.Id}/jobs", true);
            }
            else
            {
                // Fallback to all jobs page
                Navigation.NavigateTo("/jobs", true);
            }
        }

        private string GetReturnUrl()
        {
            return !string.IsNullOrEmpty(ReturnUrl) ? ReturnUrl : "/jobs";
        }

        private string GetFieldErrorClass(string fieldName)
        {
            if (validationErrors?.GetFieldErrors(fieldName).Any() == true)
                return "form-control is-invalid";
            return "form-control";
        }

        private void GenerateInvoice()
        {
            if (job != null)
            {
                var currentUrl = Navigation.Uri;
                var returnUrl = Uri.EscapeDataString(currentUrl);
                Navigation.NavigateTo($"/customer/{job.CustomerId}/invoice/new?preselectedJobId={job.Id}&returnUrl={returnUrl}", true);
            }
        }

        protected async Task OnAddressFocus()
        {
            if (job == null) return;
            
            // Show all customer addresses on focus
            if (job.CustomerId > 0 && !isInvoiced)
            {
                await LoadCustomerAddresses();
            }
        }

        private async Task LoadCustomerAddresses()
        {
            if (job == null) return;

            try
            {
                isSearchingAddresses = true;

                // Pass empty query and customer ID to get all customer addresses
                addressSuggestions = (await JobService.SearchAddressesAsync("", job.CustomerId)).ToList();
                showAddressSuggestions = addressSuggestions.Any();
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

        private string GetDeleteButtonText()
        {
            if (job == null) return "Delete Job";
            
            if (isInvoiced)
                return "Cannot Delete (Invoiced)";
            
            return "Delete Job";
        }

        private string GetDeleteDisabledReason()
        {
            if (job == null) return "";
            
            if (isInvoiced)
                return "Cannot delete invoiced jobs. Remove from invoice first.";
            
            return "";
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
            if (job == null) return;

            var value = e.Value?.ToString() ?? "";

            // Update local input value immediately (prevents input clearing)
            addressInputValue = value;

            // Cancel previous timer
            addressSearchTimer?.Dispose();

            if (string.IsNullOrWhiteSpace(value))
            {
                // Clear the job address when input is empty
                job.Address = "";

                // Show all customer addresses if empty
                if (job.CustomerId > 0 && !isInvoiced)
                {
                    addressSearchTimer = new System.Threading.Timer(async _ =>
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
            addressSearchTimer = new System.Threading.Timer(async _ =>
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

                addressSuggestions = (await JobService.SearchAddressesAsync(query, job?.CustomerId)).ToList();
                showAddressSuggestions = addressSuggestions.Any();
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
            if (job == null) return;

            // Update both the input value and the model
            addressInputValue = address;
            job.Address = address;
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