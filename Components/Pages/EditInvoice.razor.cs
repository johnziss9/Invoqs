using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Interfaces;

namespace Invoqs.Components.Pages
{
    public partial class EditInvoice : ComponentBase
    {
        [Parameter] public int InvoiceId { get; set; }

        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private IJobService JobService { get; set; } = default!;
        [Inject] private IInvoiceService InvoiceService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        [SupplyParameterFromQuery] public string? ReturnUrl { get; set; }

        // Component state
        protected string currentUser = "John Doe";
        protected CustomerModel? selectedCustomer;
        protected InvoiceModel? invoice;
        protected List<JobModel> availableJobs = new();
        protected List<JobModel> filteredJobs = new();
        protected List<JobModel> originalSelectedJobs = new();
        protected HashSet<int> selectedJobIds = new();

        // Filter state
        protected DateTime? filterStartDate;
        protected DateTime? filterEndDate;
        protected string filterJobType = "";
        protected string filterAddress = "";

        // UI state
        protected bool isLoading = true;
        protected bool isSaving = false;
        protected string errorMessage = "";
        protected string successMessage = "";
        
        private ApiValidationError? validationErrors;

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                isLoading = true;
                errorMessage = "";

                // Load existing invoice
                invoice = await InvoiceService.GetInvoiceByIdAsync(InvoiceId);
                if (invoice == null)
                {
                    errorMessage = "Invoice not found.";
                    return;
                }

                // Check if invoice can be edited
                if (invoice.Status != InvoiceStatus.Draft)
                {
                    return; // Let the UI handle the warning
                }

                // Load customer
                selectedCustomer = await CustomerService.GetCustomerByIdAsync(invoice.CustomerId);
                if (selectedCustomer == null)
                {
                    errorMessage = "Customer not found.";
                    return;
                }

                // Load jobs that were originally in the invoice
                await LoadOriginalInvoiceJobs();

                // Load available jobs for this customer (completed and uninvoiced)
                await LoadAvailableJobs();

                // Pre-select jobs from original invoice
                foreach (var lineItem in invoice.LineItems)
                {
                    selectedJobIds.Add(lineItem.JobId);
                }

                // Apply initial filters
                FilterJobs();
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading invoice: {ex.Message}";
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task LoadOriginalInvoiceJobs()
        {
            if (invoice!.LineItems.Any())
            {
                // Use existing method to get jobs by invoice ID
                originalSelectedJobs = await JobService.GetJobsByInvoiceIdAsync(invoice.Id);
            }
        }

        private async Task LoadAvailableJobs()
        {
            // Get completed uninvoiced jobs for the customer
            var uninvoicedJobs = await JobService.GetCompletedUninvoicedJobsByCustomerAsync(selectedCustomer!.Id);
            
            // Combine with original invoice jobs (in case they need to be removed)
            availableJobs = uninvoicedJobs.Union(originalSelectedJobs).ToList();
            filteredJobs = availableJobs.ToList();
        }

        private string GetReturnUrl()
        {
            return !string.IsNullOrEmpty(ReturnUrl) ? ReturnUrl : $"/invoice/{InvoiceId}";
        }

        protected void FilterJobs()
        {
            filteredJobs = availableJobs.ToList();

            // Apply date range filter
            if (filterStartDate.HasValue)
            {
                filteredJobs = filteredJobs.Where(j => j.StartDate.Date >= filterStartDate.Value.Date).ToList();
            }

            if (filterEndDate.HasValue)
            {
                filteredJobs = filteredJobs.Where(j => j.StartDate.Date <= filterEndDate.Value.Date).ToList();
            }

            // Apply job type filter
            if (!string.IsNullOrEmpty(filterJobType) && Enum.TryParse<JobType>(filterJobType, out var jobType))
            {
                filteredJobs = filteredJobs.Where(j => j.Type == jobType).ToList();
            }

            // Apply address filter
            if (!string.IsNullOrEmpty(filterAddress))
            {
                filteredJobs = filteredJobs.Where(j => j.Address == filterAddress).ToList();
            }

            filteredJobs = filteredJobs.OrderBy(j => j.StartDate).ToList();
            StateHasChanged();
        }

        protected void ToggleJobSelection(int jobId)
        {
            if (selectedJobIds.Contains(jobId))
            {
                selectedJobIds.Remove(jobId);
            }
            else
            {
                selectedJobIds.Add(jobId);
            }

            RecalculateTotals();
            StateHasChanged();
        }

        protected void SelectAllVisible()
        {
            foreach (var job in filteredJobs)
            {
                selectedJobIds.Add(job.Id);
            }
            RecalculateTotals();
            StateHasChanged();
        }

        protected void ClearAllSelected()
        {
            selectedJobIds.Clear();
            RecalculateTotals();
            StateHasChanged();
        }

        protected void RecalculateTotals()
        {
            if (invoice == null) return;

            if (!selectedJobIds.Any())
            {
                invoice.Subtotal = 0;
                invoice.LineItems.Clear();
                return;
            }

            // Get all selected jobs (from both available and original lists)
            var allJobs = availableJobs.Union(originalSelectedJobs).ToList();
            var selectedJobs = allJobs.Where(j => selectedJobIds.Contains(j.Id)).ToList();

            // Calculate subtotal
            invoice.Subtotal = selectedJobs.Sum(j => j.Price);

            // Update line items
            invoice.LineItems.Clear();
            foreach (var job in selectedJobs)
            {
                invoice.LineItems.Add(new InvoiceLineItemModel
                {
                    InvoiceId = invoice.Id,
                    JobId = job.Id,
                    Job = job,
                    Description = job.GetInvoiceDescription(),
                    UnitPrice = job.Price,
                    Quantity = 1
                });
            }

            // Calculate VAT rate based on selected jobs
            invoice.CalculateVatRate();
        }

        private async Task HandleUpdateInvoice()
        {
            try
            {
                isSaving = true;
                errorMessage = "";
                successMessage = "";
                validationErrors = null;

                if (!selectedJobIds.Any())
                {
                    errorMessage = "Please select at least one job for the invoice.";
                    return;
                }

                // Verify jobs can still be invoiced (for newly added jobs)
                var newJobIds = selectedJobIds.Except(originalSelectedJobs.Select(j => j.Id)).ToList();
                if (newJobIds.Any())
                {
                    var canInvoice = await JobService.CanJobsBeInvoicedAsync(newJobIds);
                    if (!canInvoice)
                    {
                        errorMessage = "One or more newly selected jobs cannot be invoiced. Please refresh and try again.";
                        return;
                    }
                }

                // Update the invoice
                var (updatedInvoice, errors) = await InvoiceService.UpdateInvoiceAsync(invoice!);
                if (updatedInvoice == null)
                {
                    if (errors != null)
                    {
                        validationErrors = errors;
                        errorMessage = "Please correct the validation errors below.";
                    }
                    else
                    {
                        errorMessage = "Failed to update invoice.";
                    }
                    return;
                }

                // Update job invoice status
                await UpdateJobInvoiceStatus();

                successMessage = "Invoice updated successfully!";
                StateHasChanged();

                // Navigate back after showing success message
                await Task.Delay(1500);
                Navigation.NavigateTo($"/invoice/{invoice?.Id}", forceLoad: true);
            }
            catch (Exception ex)
            {
                errorMessage = $"Error updating invoice: {ex.Message}";
            }
            finally
            {
                isSaving = false;
                StateHasChanged();
            }
        }

        private async Task UpdateJobInvoiceStatus()
        {
            if (invoice == null) return;

            // Get originally invoiced job IDs
            var originalJobIds = originalSelectedJobs.Select(j => j.Id).ToHashSet();
            
            // Get currently selected job IDs
            var currentJobIds = selectedJobIds.ToHashSet();

            // Jobs to remove from invoice (were originally selected but no longer selected)
            var jobsToRemove = originalJobIds.Except(currentJobIds).ToList();
            if (jobsToRemove.Any())
            {
                await JobService.RemoveJobsFromInvoiceAsync(jobsToRemove);
            }

            // Jobs to add to invoice (newly selected)
            var jobsToAdd = currentJobIds.Except(originalJobIds).ToList();
            if (jobsToAdd.Any())
            {
                await JobService.MarkJobsAsInvoicedAsync(jobsToAdd, invoice.Id);
            }
        }

        private void HandleInvalidSubmit()
        {
            errorMessage = "Please check the form for errors and try again.";
            successMessage = "";
            StateHasChanged();
        }

        private string GetFieldErrorClass(string fieldName)
        {
            if (validationErrors?.GetFieldErrors(fieldName).Any() == true)
                return "form-control is-invalid";
            return "form-control";
        }

        protected string GetCustomerInitials()
        {
            if (selectedCustomer == null || string.IsNullOrWhiteSpace(selectedCustomer.Name))
                return "?";

            var parts = selectedCustomer.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();

            return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
        }

        protected Task HandleLogout()
        {
            Console.WriteLine("Logout clicked");
            return Task.CompletedTask;
        }

        protected List<string> GetUniqueAddresses()
        {
            return availableJobs
                .Select(j => j.Address)
                .Where(addr => !string.IsNullOrEmpty(addr))
                .Distinct()
                .OrderBy(addr => addr)
                .ToList();
        }
    }
}