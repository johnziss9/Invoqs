using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Interfaces;
using Microsoft.JSInterop;

namespace Invoqs.Components.Pages
{
    public partial class CreateInvoice : ComponentBase
    {
        [Parameter] public int CustomerId { get; set; } = 0;

        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private IJobService JobService { get; set; } = default!;
        [Inject] private IInvoiceService InvoiceService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        [SupplyParameterFromQuery] public string? ReturnUrl { get; set; }
        [SupplyParameterFromQuery] public int? PreselectedJobId { get; set; }

        // Component state
        protected string currentUser = "John Doe";
        protected CustomerModel? selectedCustomer;
        protected List<CustomerModel> customers = new();
        protected List<JobModel> availableJobs = new();
        protected List<JobModel> filteredJobs = new();
        protected HashSet<int> selectedJobIds = new();
        protected InvoiceModel newInvoice = new();

        // Filter state
        protected DateTime? filterStartDate;
        protected DateTime? filterEndDate;
        protected string filterJobType = "";
        protected string filterAddress = "";

        // UI state
        protected bool isLoading = true;
        protected bool isSaving = false;
        protected string errorMessage = "";

        private ApiValidationError? validationErrors;

        protected override async Task OnInitializedAsync()
        {
            InitializeInvoice();
            await LoadData();

            // Handle job preselection
            if (PreselectedJobId.HasValue && availableJobs.Any())
            {
                var jobToPreselect = availableJobs.FirstOrDefault(j => j.Id == PreselectedJobId.Value);
                if (jobToPreselect != null && jobToPreselect.CanBeInvoiced)
                {
                    selectedJobIds.Add(PreselectedJobId.Value);
                    RecalculateTotals();
                    StateHasChanged();
                }
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            // Only reload if CustomerId changes
            await LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                isLoading = true;
                errorMessage = "";

                // Load all customers
                customers = await CustomerService.GetAllCustomersAsync();

                // Load selected customer if CustomerId is provided
                await LoadSelectedCustomer();

                // Load jobs if customer is selected
                if (selectedCustomer != null)
                {
                    await LoadCustomerJobs();
                }
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

        private async Task LoadSelectedCustomer()
        {
            if (CustomerId > 0)
            {
                selectedCustomer = await CustomerService.GetCustomerByIdAsync(CustomerId);
                if (selectedCustomer != null)
                {
                    newInvoice.CustomerId = CustomerId;
                }
            }
        }

        private async Task LoadCustomerJobs()
        {
            var customerId = selectedCustomer?.Id ?? newInvoice.CustomerId;
            if (customerId > 0)
            {
                availableJobs = await JobService.GetCompletedUninvoicedJobsByCustomerAsync(customerId);
                filteredJobs = availableJobs.ToList();
                FilterJobs(); // Apply any existing filters
            }
        }

        private string GetReturnUrl()
        {
            return !string.IsNullOrEmpty(ReturnUrl) ? ReturnUrl : "/invoices";
        }

        protected async void OnCustomerChanged()
        {
            selectedCustomer = customers.FirstOrDefault(c => c.Id == newInvoice.CustomerId);
            selectedJobIds.Clear(); // Clear any previously selected jobs
            filterAddress = "";  // Clear address filter when customer changes

            if (selectedCustomer != null)
            {
                await LoadCustomerJobs();
            }
            else
            {
                availableJobs.Clear();
                filteredJobs.Clear();
            }

            RecalculateTotals();
            StateHasChanged();
        }

        private void InitializeInvoice()
        {
            newInvoice = new InvoiceModel
            {
                CustomerId = CustomerId > 0 ? CustomerId : 0,
                DueDate = DateTime.Now.AddDays(30), // 30 days from now
                Status = InvoiceStatus.Draft,
                PaymentTermsDays = 30,
                VatRate = 0m,
                InvoiceNumber = string.Empty
            };
        }

        protected void FilterJobs()
        {
            filteredJobs = availableJobs.ToList();

            if (filterStartDate.HasValue)
            {
                filteredJobs = filteredJobs.Where(j => j.StartDate.Date >= filterStartDate.Value.Date).ToList();
            }

            if (filterEndDate.HasValue)
            {
                filteredJobs = filteredJobs.Where(j => j.StartDate.Date <= filterEndDate.Value.Date).ToList();
            }

            if (!string.IsNullOrEmpty(filterJobType) && Enum.TryParse<JobType>(filterJobType, out var jobType))
            {
                filteredJobs = filteredJobs.Where(j => j.Type == jobType).ToList();
            }

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
            if (!selectedJobIds.Any())
            {
                newInvoice.Subtotal = 0;
                return;
            }

            var selectedJobs = availableJobs.Where(j => selectedJobIds.Contains(j.Id)).ToList();

            // Calculate subtotal
            newInvoice.Subtotal = selectedJobs.Sum(j => j.Price);

            // Create temporary line items for VAT calculation
            newInvoice.LineItems.Clear();
            foreach (var job in selectedJobs)
            {
                newInvoice.LineItems.Add(new InvoiceLineItemModel
                {
                    JobId = job.Id,
                    Job = job,
                    Description = job.GetInvoiceDescription(),
                    UnitPrice = job.Price,
                    Quantity = 1
                });
            }
        }

        private async Task HandleCreateInvoice()
        {
            try
            {
                isSaving = true;
                errorMessage = "";
                validationErrors = null;

                if (!selectedJobIds.Any())
                {
                    errorMessage = "Please select at least one job to invoice.";
                    return;
                }

                // Verify jobs can still be invoiced
                var canInvoice = await JobService.CanJobsBeInvoicedAsync(selectedJobIds.ToList());
                if (!canInvoice)
                {
                    errorMessage = "One or more selected jobs cannot be invoiced. Please refresh and try again.";
                    return;
                }

                // Create invoice through service
                var customerId = selectedCustomer?.Id ?? newInvoice.CustomerId;
                var (createdInvoice, errors) = await InvoiceService.CreateInvoiceFromJobsAsync(
                    customerId,
                    selectedJobIds.ToList(),
                    newInvoice.DueDate,
                    newInvoice.VatRate,
                    newInvoice.PaymentTermsDays,
                    newInvoice.Notes
                );

                if (createdInvoice != null)
                {
                    Navigation.NavigateTo($"/invoice/{createdInvoice.Id}", true);
                }
                else if (errors != null)
                {
                    validationErrors = errors;
                    errorMessage = "Please correct the validation errors below.";
                }
                else
                {
                    errorMessage = "Failed to create invoice.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error creating invoice: {ex.Message}";
            }
            finally
            {
                isSaving = false;
                StateHasChanged();
            }
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