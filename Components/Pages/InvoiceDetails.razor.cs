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
    private bool showCancelModal = false;
    private bool showSendConfirmation = false;
    private bool showMarkAsPaidConfirmation = false;
    private bool isMarkingPaid = false;
    private DateTime paymentDate = DateTime.Today;
    private string paymentMethod = "Bank Transfer";
    private string paymentReference = string.Empty;
    private bool showDeleteModal = false;
    private bool isDeleting = false;

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

    private async Task ConfirmMarkAsPaid()
    {
        if (invoice == null) return;

        try
        {
            isMarkingPaid = true;

            var success = await InvoiceService.MarkInvoiceAsPaidAsync(
                invoice.Id,
                paymentDate,
                paymentMethod,
                paymentReference);

            if (success)
            {
                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidDate = paymentDate;
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
        try
        {
            await JSRuntime.InvokeVoidAsync("window.print");
        }
        catch (Exception ex)
        {
            errorMessage = $"Error printing invoice: {ex.Message}";
        }
    }

    private void DownloadPdf()
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

    private async Task ConfirmSend()
    {
        showSendConfirmation = false;
        await MarkAsSent();
    }

    protected void HandleLogout()
    {
        // This should integrate with your authentication system when implemented
        // For now, redirect to login or home page
        Navigation.NavigateTo("/");
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
}