using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Interfaces;
using Microsoft.JSInterop;

namespace Invoqs.Components.Pages
{
    public partial class CreateReceipt : ComponentBase
    {
        [Inject] private IReceiptService ReceiptService { get; set; } = default!;
        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private IInvoiceService InvoiceService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        [SupplyParameterFromQuery(Name = "customerId")]
        public int? PreselectedCustomerId { get; set; }

        // Customer and Address data
        protected List<CustomerModel> customers = new();
        protected int selectedCustomerId = 0;
        protected List<string> customerAddresses = new();
        protected string selectedAddress = "";

        // Invoice data
        protected List<InvoiceModel> allInvoices = new();
        protected List<InvoiceModel> filteredInvoices = new();
        protected HashSet<int> selectedInvoiceIds = new();
        protected bool isLoadingInvoices = false;

        // UI state
        protected bool isCreating = false;
        protected string? errorMessage = null;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                await LoadCustomers();

                // If customer is pre-selected via query parameter, select and load invoices
                if (PreselectedCustomerId.HasValue && PreselectedCustomerId.Value > 0)
                {
                    selectedCustomerId = PreselectedCustomerId.Value;
                    await OnCustomerSelected();
                }
            }
            catch (Microsoft.AspNetCore.Components.NavigationException)
            {
                // User being redirected to login - this is expected
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Initialization error: {ex.GetType().Name} - {ex.Message}");
                errorMessage = $"Error loading page: {ex.Message}";
            }
        }

        private async Task LoadCustomers()
        {
            try
            {
                customers = await CustomerService.GetAllCustomersAsync();
            }
            catch (NavigationException)
            {
                Console.WriteLine("NavigationException - user being redirected");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading customers: {ex.Message}");
                errorMessage = $"Error loading customers: {ex.Message}";
            }
        }

        protected async Task OnCustomerSelected()
        {
            // Reset state
            selectedInvoiceIds.Clear();
            allInvoices.Clear();
            filteredInvoices.Clear();
            customerAddresses.Clear();
            selectedAddress = "";
            errorMessage = null;

            if (selectedCustomerId <= 0)
                return;

            // Load paid invoices for selected customer
            isLoadingInvoices = true;
            try
            {
                allInvoices = await InvoiceService.GetInvoicesByCustomerIdAsync(selectedCustomerId);

                // Filter to only paid invoices
                allInvoices = allInvoices
                    .Where(i => i.Status == InvoiceStatus.Paid)
                    .OrderByDescending(i => i.CreatedDate)
                    .ToList();

                // Extract unique addresses
                customerAddresses = allInvoices
                    .Select(i => i.Address ?? "")
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .Distinct()
                    .OrderBy(a => a)
                    .ToList();

                // Initially show all invoices
                filteredInvoices = allInvoices;
            }
            catch (Microsoft.AspNetCore.Components.NavigationException)
            {
                Console.WriteLine("NavigationException in OnCustomerSelected - ignoring");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading invoices: {ex.Message}");
                errorMessage = $"Error loading invoices: {ex.Message}";
            }
            finally
            {
                isLoadingInvoices = false;
            }
        }

        protected void FilterInvoices()
        {
            if (string.IsNullOrEmpty(selectedAddress))
            {
                filteredInvoices = allInvoices;
            }
            else
            {
                filteredInvoices = allInvoices
                    .Where(i => i.Address == selectedAddress)
                    .ToList();
            }

            // Remove selections no longer in filtered list
            selectedInvoiceIds.RemoveWhere(id => !filteredInvoices.Any(i => i.Id == id));
        }

        protected void ToggleInvoiceSelection(int invoiceId)
        {
            if (selectedInvoiceIds.Contains(invoiceId))
                selectedInvoiceIds.Remove(invoiceId);
            else
                selectedInvoiceIds.Add(invoiceId);

            StateHasChanged();
        }

        protected void SelectAllInvoices()
        {
            foreach (var invoice in filteredInvoices)
            {
                selectedInvoiceIds.Add(invoice.Id);
            }
            StateHasChanged();
        }

        protected void DeselectAllInvoices()
        {
            selectedInvoiceIds.Clear();
            StateHasChanged();
        }

        protected decimal CalculateTotalAmount()
        {
            return allInvoices
                .Where(i => selectedInvoiceIds.Contains(i.Id))
                .Sum(i => i.Total);
        }

        protected string GetSelectedCustomerName()
        {
            return customers.FirstOrDefault(c => c.Id == selectedCustomerId)?.Name ?? "";
        }

        protected void UpdatePreview()
        {
            // This method is called after each field update to refresh the preview
            StateHasChanged();
        }

        protected async Task HandleCreateReceipt()
        {
            errorMessage = null;

            if (selectedCustomerId <= 0)
            {
                errorMessage = "Please select a customer.";
                return;
            }

            if (!selectedInvoiceIds.Any())
            {
                errorMessage = "Please select at least one invoice.";
                return;
            }

            isCreating = true;

            try
            {
                var createModel = new CreateReceiptModel
                {
                    CustomerId = selectedCustomerId,
                    InvoiceIds = selectedInvoiceIds.ToList()
                };

                var receipt = await ReceiptService.CreateReceiptAsync(createModel);

                if (receipt != null)
                {
                    // Download PDF
                    var pdfBytes = await ReceiptService.DownloadReceiptPdfAsync(receipt.Id);

                    if (pdfBytes != null)
                    {
                        await JSRuntime.InvokeVoidAsync("downloadFile",
                            $"{receipt.ReceiptNumber}.pdf",
                            "application/pdf",
                            pdfBytes);
                    }

                    // Navigate to receipts list
                    Navigation.NavigateTo("/receipts");
                }
                else
                {
                    errorMessage = "Failed to create receipt. Please try again.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating receipt: {ex.Message}");
                errorMessage = $"Error creating receipt: {ex.Message}";
            }
            finally
            {
                isCreating = false;
                StateHasChanged();
            }
        }

    }
}