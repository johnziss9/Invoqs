using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Invoqs.Interfaces;
using Microsoft.JSInterop;

namespace Invoqs.Components.Pages
{
    public partial class CustomerReceipts : ComponentBase
    {
        [Parameter] public int CustomerId { get; set; }

        [Inject] private IReceiptService ReceiptService { get; set; } = default!;
        [Inject] private ICustomerService CustomerService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        protected CustomerModel? customer;
        protected List<ReceiptModel> receipts = new();
        protected bool isLoading = true;
        protected string errorMessage = "";

        // Filter properties
        protected string searchTerm = "";
        protected string sortBy = "date";
        protected string filterBy = "all";

        // Delete modal properties
        private bool showDeleteConfirmation = false;
        private bool isDeleting = false;
        private ReceiptModel? receiptToDelete = null;

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (CustomerId > 0)
            {
                await LoadData();
            }
        }

        private async Task LoadData()
        {
            try
            {
                isLoading = true;
                errorMessage = "";

                // Load customer and receipts in parallel
                var customerTask = CustomerService.GetCustomerByIdAsync(CustomerId);
                var receiptsTask = ReceiptService.GetReceiptsByCustomerIdAsync(CustomerId);

                await Task.WhenAll(customerTask, receiptsTask);

                customer = customerTask.Result;
                receipts = receiptsTask.Result.OrderByDescending(r => r.CreatedDate).ToList();

                if (customer == null)
                {
                    errorMessage = "Customer not found.";
                    Navigation.NavigateTo("/customers");
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading data: {ex.Message}";
                Console.WriteLine($"Error loading receipts: {ex.Message}");
                receipts = new List<ReceiptModel>();
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        protected IEnumerable<ReceiptModel> filteredReceipts
        {
            get
            {
                var filtered = receipts.AsEnumerable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    filtered = filtered.Where(r =>
                        r.ReceiptNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                }

                // Apply category filter
                filtered = filterBy switch
                {
                    "sent" => filtered.Where(r => r.IsSent),
                    "notSent" => filtered.Where(r => !r.IsSent),
                    _ => filtered
                };

                // Apply sorting
                filtered = sortBy switch
                {
                    "amount" => filtered.OrderByDescending(r => r.TotalAmount),
                    "created" => filtered.OrderByDescending(r => r.CreatedDate),
                    _ => filtered.OrderByDescending(r => r.CreatedDate)
                };

                return filtered;
            }
        }

        protected void ViewReceipt(int receiptId)
        {
            Navigation.NavigateTo($"/receipt/{receiptId}", true);
        }

        private async Task DownloadReceipt(int receiptId, string receiptNumber)
        {
            try
            {
                var pdfBytes = await ReceiptService.DownloadReceiptPdfAsync(receiptId);

                if (pdfBytes != null)
                {
                    var fileName = $"{receiptNumber}.pdf";
                    await JSRuntime.InvokeVoidAsync("downloadFile", fileName, "application/pdf", pdfBytes);
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error downloading receipt: {ex.Message}";
                Console.WriteLine($"Error downloading receipt: {ex.Message}");
                StateHasChanged();
            }
        }

        private void ShowDeleteConfirmation(ReceiptModel receipt)
        {
            receiptToDelete = receipt;
            showDeleteConfirmation = true;
            StateHasChanged();
        }

        private void HideDeleteConfirmation()
        {
            showDeleteConfirmation = false;
            receiptToDelete = null;
            StateHasChanged();
        }

        private async Task ConfirmDelete()
        {
            if (receiptToDelete == null) return;

            try
            {
                isDeleting = true;
                StateHasChanged();

                var success = await ReceiptService.DeleteReceiptAsync(receiptToDelete.Id);
                
                if (success)
                {
                    receipts = receipts.Where(r => r.Id != receiptToDelete.Id).ToList();
                    showDeleteConfirmation = false;
                    receiptToDelete = null;
                }
                else
                {
                    errorMessage = "Failed to delete receipt.";
                    showDeleteConfirmation = false;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error deleting receipt: {ex.Message}";
                showDeleteConfirmation = false;
                Console.WriteLine($"Error deleting receipt: {ex.Message}");
            }
            finally
            {
                isDeleting = false;
                StateHasChanged();
            }
        }
    }
}