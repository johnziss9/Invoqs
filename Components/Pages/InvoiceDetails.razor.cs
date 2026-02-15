using Microsoft.AspNetCore.Components;
using Invoqs.Models;
using Microsoft.JSInterop;
using Invoqs.Interfaces;

namespace Invoqs.Components.Pages;

public partial class InvoiceDetails
{
    [Parameter] public int Id { get; set; }
    [Inject] private IInvoiceService InvoiceService { get; set; } = default!;
    [Inject] private ICustomerService CustomerService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private InvoiceModel? invoice;
    private CustomerModel? customer;
    private bool isLoading = true;
    private bool isProcessing = false;
    private string successMessage = string.Empty;
    private string errorMessage = string.Empty;
    private bool showCancelModal = false;
    private string cancellationReason = "";
    private string customCancellationReason = "";
    private bool showSendConfirmation = false;
    private bool showEmailSelection = false;
    private List<string>? selectedEmailsForSending = null;
    private bool showMarkAsPaidConfirmation = false;
    private bool isMarkingPaid = false;
    private DateTime paymentDate = DateTime.Today;
    private string paymentMethod = "Bank Transfer";
    private string paymentReference = string.Empty;
    private bool isFullPayment = true;
    private decimal paymentAmount = 0;
    private string paymentAmountError = string.Empty;
    private string paymentNotes = string.Empty;
    private bool showDeleteModal = false;
    private bool isDeleting = false;
    private bool showDeliveredConfirmation = false;
    
    protected bool isCustomerDeleted = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadInvoiceAsync();
    }

    private async Task LoadInvoiceAsync()
    {
        try
        {
            isLoading = true;
            errorMessage = string.Empty;
            isCustomerDeleted = false;

            invoice = await InvoiceService.GetInvoiceByIdAsync(Id);

            if (invoice != null)
            {
                await JSRuntime.InvokeVoidAsync("eval", $"document.title = 'Λεπτομέρειες Τιμολογίου - {invoice.InvoiceNumber.Replace("'", "\\'")} - Invoqs'");

                // Create customer object from invoice's flat properties
                // This works even when the customer is soft-deleted
                customer = new CustomerModel
                {
                    Id = invoice.CustomerId,
                    Name = invoice.CustomerName ?? "[Unknown Customer]",
                    Emails = invoice.CustomerEmails?.Select(e => new EmailModel { Email = e, CreatedDate = DateTime.Now }).ToList() 
                        ?? new List<EmailModel>(),
                    Phone = invoice.CustomerPhone ?? "",
                    IsDeleted = invoice.CustomerIsDeleted,
                    CreatedDate = invoice.CustomerCreatedDate,
                    UpdatedDate = invoice.CustomerUpdatedDate
                };

                // Set the deleted flag
                isCustomerDeleted = invoice.CustomerIsDeleted;
            }
            else
            {
                errorMessage = "Invoice not found.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading invoice: {ex.Message}";
            invoice = null;
            customer = null;
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task MarkAsSent()
    {
        if (invoice == null) return;

        try
        {
            isProcessing = true;
            var success = await InvoiceService.MarkInvoiceAsSentAsync(invoice.Id);

            if (success)
            {
                invoice.Status = InvoiceStatus.Sent;
                successMessage = "Invoice marked as sent successfully.";

                // Clear the message after 3 seconds
                _ = Task.Delay(3000).ContinueWith(_ =>
                {
                    successMessage = string.Empty;
                    InvokeAsync(StateHasChanged);
                });
            }
            else
            {
                errorMessage = "Failed to mark invoice as sent.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error marking invoice as sent: {ex.Message}";
        }
        finally
        {
            isProcessing = false;
            StateHasChanged();
        }
    }

    private void ResendInvoice()
    {
        showSendConfirmation = true;
        StateHasChanged();
    }

    private void SelectPaymentType(bool isFull)
    {
        isFullPayment = isFull;
        if (isFull && invoice != null)
        {
            paymentAmount = invoice.RemainingAmount;
        }
        else
        {
            paymentAmount = 0;
        }
        paymentAmountError = string.Empty;
        StateHasChanged();
    }

    private async Task ConfirmPayment()
    {
        if (invoice == null) return;

        // Validate payment amount
        if (paymentAmount <= 0)
        {
            paymentAmountError = "Το ποσό πληρωμής πρέπει να είναι μεγαλύτερο από μηδέν";
            StateHasChanged();
            return;
        }

        if (paymentAmount > invoice.RemainingAmount)
        {
            paymentAmountError = $"Το ποσό πληρωμής δεν μπορεί να υπερβαίνει το υπόλοιπο (€{invoice.RemainingAmount:N2})";
            StateHasChanged();
            return;
        }

        try
        {
            isMarkingPaid = true;
            paymentAmountError = string.Empty;

            var utcPaymentDate = DateTime.SpecifyKind(paymentDate.Date, DateTimeKind.Utc);

            var success = await InvoiceService.RecordPaymentAsync(
                invoice.Id,
                paymentAmount,
                utcPaymentDate,
                paymentMethod,
                paymentReference,
                paymentNotes);

            if (success)
            {
                successMessage = paymentAmount >= invoice.RemainingAmount
                    ? "Το τιμολόγιο σημειώθηκε ως πληρωμένο επιτυχώς!"
                    : $"Η πληρωμή €{paymentAmount:N2} καταγράφηκε επιτυχώς!";
                showMarkAsPaidConfirmation = false;
                await LoadInvoiceAsync();

                // Clear the message after 3 seconds
                _ = Task.Delay(3000).ContinueWith(_ =>
                {
                    successMessage = string.Empty;
                    InvokeAsync(StateHasChanged);
                });
            }
            else
            {
                errorMessage = "Αποτυχία καταγραφής πληρωμής. Παρακαλώ δοκιμάστε ξανά.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Σφάλμα κατά την καταγραφή πληρωμής: {ex.Message}";
        }
        finally
        {
            isMarkingPaid = false;
            StateHasChanged();
        }
    }

    private async Task ConfirmMarkAsPaid()
    {
        // Keep old method for backwards compatibility, but call new method
        await ConfirmPayment();
    }

    private void ShowMarkAsPaidConfirmation()
    {
        if (invoice == null) return;

        showMarkAsPaidConfirmation = true;
        isFullPayment = true;
        paymentAmount = invoice.RemainingAmount;
        paymentDate = DateTime.Today;
        paymentMethod = "Bank Transfer";
        paymentReference = string.Empty;
        paymentNotes = string.Empty;
        paymentAmountError = string.Empty;
        StateHasChanged();
    }

    private void HideMarkAsPaidConfirmation()
    {
        showMarkAsPaidConfirmation = false;
        // Reset payment details
        isFullPayment = true;
        paymentAmount = invoice?.RemainingAmount ?? 0;
        paymentDate = DateTime.Today;
        paymentMethod = "Bank Transfer";
        paymentReference = string.Empty;
        paymentNotes = string.Empty;
        paymentAmountError = string.Empty;
        StateHasChanged();
    }

    private string TranslatePaymentMethod(string? method) => method switch
    {
        "Bank Transfer" => "Τραπεζική Μεταφορά",
        "Cash" => "Μετρητά",
        "Cheque" => "Επιταγή",
        "Credit Card" => "Πιστωτική Κάρτα",
        "Debit Card" => "Χρεωστική Κάρτα",
        "PayPal" => "PayPal",
        "Stripe" => "Stripe",
        "Other" => "Άλλο",
        _ => method ?? "-"
    };

    private async Task CancelInvoice()
    {
        if (invoice == null) return;

        try
        {
            isProcessing = true;

            var finalReason = cancellationReason == "Other" ? customCancellationReason : cancellationReason;
            var notes = cancellationReason == "Other" ? customCancellationReason : null;

            var success = await InvoiceService.CancelInvoiceAsync(invoice.Id, finalReason, notes);

            if (success)
            {
                invoice.Status = InvoiceStatus.Cancelled;
                invoice.CancelledDate = DateTime.UtcNow;
                invoice.CancellationReason = finalReason;
                invoice.CancellationNotes = notes;

                showCancelModal = false;
                successMessage = "Invoice cancelled successfully. Associated jobs are now available for invoicing.";

                // Clear the message after 3 seconds
                _ = Task.Delay(3000).ContinueWith(_ =>
                {
                    successMessage = string.Empty;
                    InvokeAsync(StateHasChanged);
                });
            }
            else
            {
                errorMessage = "Failed to cancel invoice.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error cancelling invoice: {ex.Message}";
        }
        finally
        {
            isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task PrintInvoice()
    {
        if (invoice == null) return;

        try
        {
            isProcessing = true;
            StateHasChanged();

            // Generate the PDF
            var pdfBytes = await InvoiceService.DownloadInvoicePdfAsync(invoice.Id);

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                errorMessage = "Failed to generate PDF for printing. Please try again.";
                return;
            }

            // Open PDF in new window for printing instead of downloading
            await JSRuntime.InvokeVoidAsync("printPdf", pdfBytes);

            successMessage = "PDF opened for printing.";

            // Clear the message after 3 seconds
            _ = Task.Delay(3000).ContinueWith(_ =>
            {
                successMessage = string.Empty;
                InvokeAsync(StateHasChanged);
            });
        }
        catch (Exception ex)
        {
            errorMessage = $"Error generating PDF: {ex.Message}";
        }
        finally
        {
            isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task DownloadPdf()
    {
        if (invoice == null) return;

        try
        {
            isProcessing = true;
            StateHasChanged();

            var pdfBytes = await InvoiceService.DownloadInvoicePdfAsync(invoice.Id);

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                errorMessage = "Failed to generate PDF. Please try again.";
                return;
            }

            // Trigger browser download using JavaScript
            var fileName = $"{invoice.InvoiceNumber}.pdf";
            await JSRuntime.InvokeVoidAsync("downloadFile", fileName, "application/pdf", pdfBytes);

            successMessage = "PDF downloaded successfully.";

            // Clear the message after 3 seconds
            _ = Task.Delay(3000).ContinueWith(_ =>
            {
                successMessage = string.Empty;
                InvokeAsync(StateHasChanged);
            });
        }
        catch (Exception ex)
        {
            errorMessage = $"Error generating PDF: {ex.Message}";
        }
        finally
        {
            isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task ConfirmSend()
    {
        if (invoice == null) return;

        if (invoice.CustomerEmails == null || !invoice.CustomerEmails.Any())
        {
            errorMessage = "Cannot send invoice: Customer has no email address. Please update customer details first.";
            showSendConfirmation = false;
            StateHasChanged();
            return;
        }

        // Multiple emails — show selection modal
        if (invoice.CustomerEmails.Count > 1)
        {
            showSendConfirmation = false;
            showEmailSelection = true;
            StateHasChanged();
            return;
        }

        // Single email — send directly
        showSendConfirmation = false;
        selectedEmailsForSending = new List<string> { invoice.CustomerEmails.First() };
        await SendInvoiceToSelectedEmails();
    }

    private async Task HandleEmailsSelected(List<string> selectedEmails)
    {
        showEmailSelection = false;
        selectedEmailsForSending = selectedEmails;
        await SendInvoiceToSelectedEmails();
    }

    private async Task SendInvoiceToSelectedEmails()
    {
        if (invoice == null || selectedEmailsForSending == null || !selectedEmailsForSending.Any()) return;

        try
        {
            isProcessing = true;
            StateHasChanged();

            bool isResend = invoice.Status == InvoiceStatus.Sent;

            var success = await InvoiceService.MarkInvoiceAsSentAsync(invoice.Id, selectedEmailsForSending);

            if (success)
            {
                if (!isResend)
                    invoice.Status = InvoiceStatus.Sent;

                invoice.IsSent = true;
                invoice.SentDate = DateTime.UtcNow;

                var emailList = string.Join(", ", selectedEmailsForSending);
                successMessage = isResend ?
                    $"Invoice resent successfully to {emailList}" :
                    $"Invoice sent successfully to {emailList}";

                _ = Task.Delay(5000).ContinueWith(_ =>
                {
                    successMessage = string.Empty;
                    InvokeAsync(StateHasChanged);
                });
            }
            else
            {
                errorMessage = "Failed to send invoice. Please try again.";
            }
        }
        catch (Exception ex)
        {
            var errorMsg = ex.Message;

            if (errorMsg.Contains("no email address"))
                errorMessage = "Cannot send invoice: Customer has no email address.";
            else if (errorMsg.Contains("Failed to send email"))
                errorMessage = "Failed to send invoice email. Please check your email configuration and try again.";
            else
                errorMessage = $"Error sending invoice: {errorMsg}";

            Console.WriteLine($"Error sending invoice: {ex.Message}");
        }
        finally
        {
            isProcessing = false;
            selectedEmailsForSending = null;
            StateHasChanged();
        }
    }


    private async Task MarkAsDelivered()
    {
        if (invoice == null) return;

        try
        {
            isProcessing = true;
            var success = await InvoiceService.MarkInvoiceAsDeliveredAsync(invoice.Id);

            if (success)
            {
                invoice.Status = InvoiceStatus.Delivered;
                invoice.IsDelivered = true;
                invoice.DeliveredDate = DateTime.UtcNow;
                successMessage = "Invoice marked as delivered successfully.";

                // Clear the message after 3 seconds
                _ = Task.Delay(3000).ContinueWith(_ =>
                {
                    successMessage = string.Empty;
                    InvokeAsync(StateHasChanged);
                });
            }
            else
            {
                errorMessage = "Failed to mark invoice as delivered.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error marking invoice as delivered: {ex.Message}";
        }
        finally
        {
            isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task ConfirmDeliver()
    {
        showDeliveredConfirmation = false;
        await MarkAsDelivered();
    }

    private async Task DeleteInvoice()
    {
        if (invoice == null) return;

        try
        {
            isDeleting = true;

            var success = await InvoiceService.DeleteInvoiceAsync(invoice.Id);

            if (success)
            {
                Navigation.NavigateTo("/invoices", true);
            }
            else
            {
                errorMessage = "Failed to delete invoice.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error deleting invoice: {ex.Message}";
        }
        finally
        {
            isDeleting = false;
            showDeleteModal = false;
            StateHasChanged();
        }
    }

    private void ShowDeleteConfirmation()
    {
        if (invoice == null) return;

        if (invoice.Status != InvoiceStatus.Draft)
        {
            errorMessage = GetDeleteDisabledReason();
            StateHasChanged();
            return;
        }

        showDeleteModal = true;
        StateHasChanged();
    }
    
    private string GetDeleteButtonText()
    {
        if (invoice == null) return "Delete Invoice";
        
        if (invoice.Status != InvoiceStatus.Draft)
            return "Cannot Delete";
        
        return "Delete Invoice";
    }

    private string GetDeleteDisabledReason()
    {
        if (invoice == null) return "";

        return invoice.Status switch
        {
            InvoiceStatus.Sent => "Cannot delete sent invoices. Cancel the invoice first if needed.",
            InvoiceStatus.Delivered => "Cannot delete delivered invoices. Cancel the invoice first if needed.",
            InvoiceStatus.Paid => "Cannot delete paid invoices. Paid invoices must be preserved for financial records.",
            InvoiceStatus.Overdue => "Cannot delete overdue invoices. Cancel the invoice first if needed.",
            InvoiceStatus.Cancelled => "Cannot delete cancelled invoices. Cancelled invoices must be preserved for audit trail.",
            _ => ""
        };
    }

    private void GoToCreateReceipt()
    {
        Navigation.NavigateTo($"/receipts/new?customerId={customer!.Id}", true);
    }
}