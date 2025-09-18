using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;
using Invoqs.Models;
using Invoqs.Services;
using Microsoft.JSInterop;

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
    private bool EditMode = false;
    private string successMessage = string.Empty;
    private string errorMessage = string.Empty;
    private bool showPaymentModal = false;
    private bool showCancelModal = false;
    private PaymentInfo paymentInfo = new();

    private string currentUser = "Demo User"; // This should come from authentication service when implemented

    protected override async Task OnInitializedAsync()
    {
        await LoadInvoiceAsync();
    }

    private async Task LoadInvoiceAsync()
    {
        try
        {
            isLoading = true;
            invoice = await InvoiceService.GetInvoiceByIdAsync(Id);

            if (invoice?.CustomerId != null)
            {
                customer = await CustomerService.GetCustomerByIdAsync(invoice.CustomerId);
            }
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

    private async Task ResendInvoice()
    {
        if (invoice == null) return;

        try
        {
            isProcessing = true;

            // Simulate email sending delay
            await Task.Delay(1000);

            successMessage = "Invoice resent successfully.";

            // Clear the message after 3 seconds
            _ = Task.Delay(3000).ContinueWith(_ =>
            {
                successMessage = string.Empty;
                InvokeAsync(StateHasChanged);
            });
        }
        catch (Exception ex)
        {
            errorMessage = $"Error resending invoice: {ex.Message}";
        }
        finally
        {
            isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task RecordPayment()
    {
        if (invoice == null) return;

        try
        {
            isProcessing = true;

            var success = await InvoiceService.MarkInvoiceAsPaidAsync(
                invoice.Id,
                paymentInfo.PaymentDate,
                paymentInfo.PaymentMethod,
                paymentInfo.PaymentReference);

            if (success)
            {
                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidDate = paymentInfo.PaymentDate;
                invoice.PaymentMethod = paymentInfo.PaymentMethod;
                invoice.PaymentReference = paymentInfo.PaymentReference;

                showPaymentModal = false;
                successMessage = "Payment recorded successfully.";

                // Clear the message after 3 seconds
                _ = Task.Delay(3000).ContinueWith(_ =>
                {
                    successMessage = string.Empty;
                    InvokeAsync(StateHasChanged);
                });
            }
            else
            {
                errorMessage = "Failed to record payment.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error recording payment: {ex.Message}";
        }
        finally
        {
            isProcessing = false;
            StateHasChanged();
        }
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
        try
        {
            await JSRuntime.InvokeVoidAsync("window.print");
        }
        catch (Exception ex)
        {
            errorMessage = $"Error printing invoice: {ex.Message}";
        }
    }

    private async Task DownloadPdf()
    {
        try
        {
            // This would typically call an API endpoint to generate and download a PDF
            // For now, we'll show a placeholder message
            successMessage = "PDF generation functionality will be implemented with the API.";

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
    }

    private void ResetPaymentInfo()
    {
        paymentInfo = new PaymentInfo
        {
            PaymentDate = DateTime.Today
        };
    }

    protected async Task HandleLogout()
    {
        // This should integrate with your authentication system when implemented
        // For now, redirect to login or home page
        Navigation.NavigateTo("/");
    }

    protected override void OnParametersSet()
    {
        if (showPaymentModal && invoice != null)
        {
            ResetPaymentInfo();
        }
    }

    public class PaymentInfo
    {
        [Required(ErrorMessage = "Payment date is required")]
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Payment method is required")]
        public string PaymentMethod { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Payment reference cannot exceed 200 characters")]
        public string? PaymentReference { get; set; }

        public string? Notes { get; set; }
    }
}