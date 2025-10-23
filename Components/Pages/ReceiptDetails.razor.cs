using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Invoqs.Models;
using Invoqs.Interfaces;

namespace Invoqs.Components.Pages
{
    public partial class ReceiptDetails : ComponentBase
    {
        [Parameter] public int ReceiptId { get; set; }

        [Inject] private IReceiptService ReceiptService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        protected ReceiptModel? receipt;
        protected bool isLoading = true;
        protected string errorMessage = string.Empty;

        private bool showDeleteConfirmation = false;
        private bool isDeleting = false;
        private bool isDownloading = false;
        private bool showSendConfirmation = false;
        private bool isSending = false;
        private string successMessage = string.Empty;


        protected override async Task OnInitializedAsync()
        {
            await LoadReceiptData();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (ReceiptId > 0)
            {
                await LoadReceiptData();
            }
        }

        private async Task LoadReceiptData()
        {
            try
            {
                isLoading = true;
                errorMessage = string.Empty;

                receipt = await ReceiptService.GetReceiptByIdAsync(ReceiptId);

                if (receipt == null)
                {
                    errorMessage = "Receipt not found.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading receipt: {ex.Message}";
                Console.WriteLine($"Error loading receipt: {ex.Message}");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        protected async Task DownloadReceipt(int receiptId, string receiptNumber)
        {
            try
            {
                isDownloading = true;
                StateHasChanged();

                var pdfBytes = await ReceiptService.DownloadReceiptPdfAsync(receiptId);

                if (pdfBytes != null)
                {
                    var fileName = $"{receiptNumber}.pdf";
                    await JSRuntime.InvokeVoidAsync("downloadFile", fileName, "application/pdf", pdfBytes);
                }
                else
                {
                    errorMessage = "Failed to download receipt PDF.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error downloading receipt: {ex.Message}";
                Console.WriteLine($"Error downloading receipt: {ex.Message}");
            }
            finally
            {
                isDownloading = false;
                StateHasChanged();
            }
        }

        private void ShowDeleteConfirmation()
        {
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
            if (receipt == null) return;

            try
            {
                isDeleting = true;
                StateHasChanged();

                var success = await ReceiptService.DeleteReceiptAsync(receipt.Id);

                if (success)
                {
                    // Navigate back to receipts list after successful deletion
                    Navigation.NavigateTo("/receipts", true);
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

        private void ShowSendConfirmation()
        {
            showSendConfirmation = true;
            StateHasChanged();
        }

        private void HideSendConfirmation()
        {
            showSendConfirmation = false;
            StateHasChanged();
        }

        private async Task ConfirmSend()
        {
            if (receipt == null) return;

            try
            {
                isSending = true;
                showSendConfirmation = false;
                StateHasChanged();

                // Call service to send receipt
                var success = await ReceiptService.SendReceiptAsync(receipt.Id);

                if (success)
                {
                    receipt.IsSent = true;
                    receipt.SentDate = DateTime.UtcNow;

                    successMessage = $"Receipt sent successfully to {receipt.CustomerEmail}";

                    // Clear the message after 3 seconds
                    _ = Task.Delay(3000).ContinueWith(_ =>
                    {
                        successMessage = string.Empty;
                        InvokeAsync(StateHasChanged);
                    });
                }
                else
                {
                    errorMessage = "Failed to send receipt. Please try again.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error sending receipt: {ex.Message}";
                Console.WriteLine($"Error sending receipt: {ex.Message}");
            }
            finally
            {
                isSending = false;
                StateHasChanged();
            }
        }

        private void ResendReceipt()
        {
            // Show the send confirmation modal for resending
            showSendConfirmation = true;
            StateHasChanged();
        }
    }
}