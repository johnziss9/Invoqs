using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Invoqs.Models;
using Invoqs.Interfaces;
using Invoqs.Services;

namespace Invoqs.Components.Pages
{
    public partial class ReceiptDetails : ComponentBase
    {
        [Parameter] public int ReceiptId { get; set; }

        [Inject] private IReceiptService ReceiptService { get; set; } = default!;
        [Inject] private IAuthService AuthService { get; set; } = default!;
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
                else
                {
                    await JSRuntime.InvokeVoidAsync("eval", $"document.title = 'Λεπτομέρειες Απόδειξης - {receipt.ReceiptNumber.Replace("'", "\\'")} - Invoqs'");
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

                // Get current user info from AuthService
                var userInfo = await AuthService.GetCurrentUserAsync();
                var firstName = userInfo?.FirstName ?? "Unknown";
                var lastName = userInfo?.LastName ?? "User";

                var pdfBytes = await ReceiptService.DownloadReceiptPdfAsync(receiptId, firstName, lastName);

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

                // Check if customer has email
                if (string.IsNullOrWhiteSpace(receipt.CustomerEmail))
                {
                    errorMessage = "Cannot send receipt: Customer has no email address. Please update customer details first.";
                    return;
                }

                // Call service to send receipt
                var success = await ReceiptService.SendReceiptAsync(receipt.Id);

                if (success)
                {
                    receipt.IsSent = true;
                    receipt.SentDate = DateTime.UtcNow;

                    successMessage = $"Receipt sent successfully to {receipt.CustomerEmail}";

                    // Clear the message after 5 seconds
                    _ = Task.Delay(5000).ContinueWith(_ =>
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
                // Parse error message for better user feedback
                var errorMsg = ex.Message;

                if (errorMsg.Contains("no email address"))
                {
                    errorMessage = "Cannot send receipt: Customer has no email address.";
                }
                else if (errorMsg.Contains("Failed to send email"))
                {
                    errorMessage = "Failed to send receipt email. Please check your email configuration and try again.";
                }
                else
                {
                    errorMessage = $"Error sending receipt: {errorMsg}";
                }

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

        private string TranslatePaymentMethod(string? method)
        {
            if (string.IsNullOrEmpty(method)) return "Δεν καθορίστηκε";

            return method switch
            {
                "Bank Transfer" => "Τραπεζική Μεταφορά",
                "Cash" => "Μετρητά",
                "Cheque" => "Επιταγή",
                "Credit Card" => "Πιστωτική Κάρτα",
                "Other" => "Άλλο",
                _ => method // Return as-is if not found
            };
        }
    }
}