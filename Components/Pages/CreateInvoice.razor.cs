using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Services;

namespace Invoqs.Components.Pages
{
    public partial class CreateInvoice : ComponentBase
    {
        [Parameter] public int CustomerId { get; set; } = 0;

        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private IJobService JobService { get; set; } = default!;
        [Inject] private IInvoiceService InvoiceService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        [SupplyParameterFromQuery] public string? ReturnUrl { get; set; }

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

        // UI state
        protected bool isLoading = true;
        protected bool isSaving = false;
        protected string errorMessage = "";

        protected override async Task OnInitializedAsync()
        {
            InitializeInvoice();
            await LoadData();
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
                CustomerId = CustomerId,
                DueDate = DateTime.Now.AddDays(30), // 30 days from now
                Status = InvoiceStatus.Draft,
                PaymentTermsDays = 30,
                VatRate = 5m // Default to skip rental rate
            };
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

            // Calculate VAT rate based on selected jobs
            newInvoice.CalculateVatRate();
        }

        private async Task HandleCreateInvoice()
        {
            try
            {
                isSaving = true;
                errorMessage = "";

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
                var createdInvoice = await InvoiceService.CreateInvoiceFromJobsAsync(
                    customerId, 
                    selectedJobIds.ToList(), 
                    newInvoice.DueDate
                );

                // Update the created invoice with any custom notes
                if (!string.IsNullOrWhiteSpace(newInvoice.Notes))
                {
                    createdInvoice.Notes = newInvoice.Notes;
                    await InvoiceService.UpdateInvoiceAsync(createdInvoice);
                }

                // Navigate to invoice details or customer jobs page
                Navigation.NavigateTo($"/invoice/{createdInvoice.Id}");
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

        private void HandleInvalidSubmit()
        {
            errorMessage = "Please check the form for errors and try again.";
            StateHasChanged();
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
    }
}