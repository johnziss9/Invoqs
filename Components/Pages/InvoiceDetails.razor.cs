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
    private bool showSendConfirmation = false;
    private bool showMarkAsPaidConfirmation = false;
    private bool isMarkingPaid = false;
    private DateTime paymentDate = DateTime.Today;
    private string paymentMethod = "Bank Transfer";
    private string paymentReference = string.Empty;
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
                // Create customer object from invoice's flat properties
                // This works even when the customer is soft-deleted
                customer = new CustomerModel
                {
                    Id = invoice.CustomerId,
                    Name = invoice.CustomerName ?? "[Unknown Customer]",
                    Email = invoice.CustomerEmail ?? "",
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

    private async Task ConfirmMarkAsPaid()
    {
        if (invoice == null) return;

        try
        {
            isMarkingPaid = true;

            var utcPaymentDate = DateTime.SpecifyKind(paymentDate.Date, DateTimeKind.Utc);

            var success = await InvoiceService.MarkInvoiceAsPaidAsync(
                invoice.Id,
                utcPaymentDate,
                paymentMethod,
                paymentReference);

            if (success)
            {
                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidDate = utcPaymentDate;
                invoice.PaymentMethod = paymentMethod;
                invoice.PaymentReference = paymentReference;

                showMarkAsPaidConfirmation = false;
                successMessage = "Invoice marked as paid successfully.";

                // Clear the message after 3 seconds
                _ = Task.Delay(3000).ContinueWith(_ =>
                {
                    successMessage = string.Empty;
                    InvokeAsync(StateHasChanged);
                });
            }
            else
            {
                errorMessage = "Failed to mark invoice as paid.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error marking invoice as paid: {ex.Message}";
        }
        finally
        {
            isMarkingPaid = false;
            StateHasChanged();
        }
    }

    private void HideMarkAsPaidConfirmation()
    {
        showMarkAsPaidConfirmation = false;
        // Reset payment details
        paymentDate = DateTime.Today;
        paymentMethod = "Bank Transfer";
        paymentReference = string.Empty;
        StateHasChanged();
    }

    private async Task CancelInvoice()
    {
        if (invoice == null) return;

        try
        {
            isProcessing = true;

            var success = await InvoiceService.CancelInvoiceAsync(invoice.Id);

            if (success)
            {
                invoice.Status = InvoiceStatus.Cancelled;

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

        try
        {
            isProcessing = true;
            showSendConfirmation = false;
            StateHasChanged();

            // Check if customer has email
            if (string.IsNullOrWhiteSpace(invoice.CustomerEmail))
            {
                errorMessage = "Cannot send invoice: Customer has no email address. Please update customer details first.";
                return;
            }

            // Determine if this is a resend based on current status
            bool isResend = invoice.Status == InvoiceStatus.Sent;

            var success = await InvoiceService.MarkInvoiceAsSentAsync(invoice.Id);

            if (success)
            {
                if (!isResend)
                {
                    invoice.Status = InvoiceStatus.Sent;
                }

                invoice.IsSent = true;
                invoice.SentDate = DateTime.UtcNow;

                successMessage = isResend ?
                    $"Invoice resent successfully to {invoice.CustomerEmail}" :
                    $"Invoice sent successfully to {invoice.CustomerEmail}";

                // Clear the message after 5 seconds
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
            // Parse error message for better user feedback
            var errorMsg = ex.Message;

            if (errorMsg.Contains("no email address"))
            {
                errorMessage = "Cannot send invoice: Customer has no email address.";
            }
            else if (errorMsg.Contains("Failed to send email"))
            {
                errorMessage = "Failed to send invoice email. Please check your email configuration and try again.";
            }
            else
            {
                errorMessage = $"Error sending invoice: {errorMsg}";
            }

            Console.WriteLine($"Error sending invoice: {ex.Message}");
        }
        finally
        {
            isProcessing = false;
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
}