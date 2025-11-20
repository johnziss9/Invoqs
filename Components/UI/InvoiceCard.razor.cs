using Microsoft.AspNetCore.Components;
using Invoqs.Models;

namespace Invoqs.Components.UI
{
    public class MarkAsPaidEventArgs
    {
        public InvoiceModel Invoice { get; set; } = new();
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? PaymentReference { get; set; }
    }

    public class CancelInvoiceEventArgs
    {
        public InvoiceModel Invoice { get; set; } = new();
        public string? CancellationReason { get; set; }
        public string? CancellationNotes { get; set; }
    }

    public partial class InvoiceCard : ComponentBase
    {
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        [Parameter] public InvoiceModel Invoice { get; set; } = new();
        [Parameter] public EventCallback<InvoiceModel> OnView { get; set; }
        [Parameter] public EventCallback<InvoiceModel> OnEdit { get; set; }
        [Parameter] public EventCallback<InvoiceModel> OnMarkAsSent { get; set; }
        [Parameter] public EventCallback<InvoiceModel> OnMarkAsDelivered { get; set; }
        [Parameter] public EventCallback<MarkAsPaidEventArgs> OnMarkAsPaid { get; set; }
        [Parameter] public EventCallback<CancelInvoiceEventArgs> OnCancel { get; set; }
        [Parameter] public EventCallback<InvoiceModel> OnDelete { get; set; }

        private bool showSendConfirmation = false;
        private bool isSending = false;
        private bool showDeliveredConfirmation = false;
        private bool isMarkingDelivered = false;
        private bool showDeleteConfirmation = false;
        private bool isDeleting = false;
        private string? errorMessage;
        private bool showCancelConfirmation = false;
        private bool isCancelling = false;
        private string cancellationReason = "";
        private string customCancellationReason = "";
        private bool showMarkAsPaidConfirmation = false;
        private bool isMarkingPaid = false;
        private DateTime paymentDate = DateTime.Today;
        private string paymentMethod = "Bank Transfer";
        private string paymentReference = "";

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
            // Check if customer has email before attempting to send
            if (string.IsNullOrWhiteSpace(Invoice.CustomerEmail))
            {
                errorMessage = "Cannot send invoice: Customer has no email address.";
                showSendConfirmation = false;
                StateHasChanged();

                // Clear error after 5 seconds
                _ = Task.Delay(5000).ContinueWith(_ =>
                {
                    errorMessage = null;
                    InvokeAsync(StateHasChanged);
                });
                return;
            }

            isSending = true;
            StateHasChanged();

            try
            {
                await OnMarkAsSent.InvokeAsync(Invoice);
                showSendConfirmation = false;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error sending invoice: {ex.Message}";
                showSendConfirmation = false;

                // Clear error after 5 seconds
                _ = Task.Delay(5000).ContinueWith(_ =>
                {
                    errorMessage = null;
                    InvokeAsync(StateHasChanged);
                });
            }
            finally
            {
                isSending = false;
                StateHasChanged();
            }
        }

        private void ShowDeleteConfirmation()
        {
            if (Invoice.Status != InvoiceStatus.Draft)
            {
                errorMessage = GetDeleteDisabledReason();
                StateHasChanged();
                return;
            }

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
            isDeleting = true;
            StateHasChanged();

            try
            {
                await OnDelete.InvokeAsync(Invoice);
                showDeleteConfirmation = false;
            }
            finally
            {
                isDeleting = false;
                StateHasChanged();
            }
        }

        private void ShowCancelConfirmation()
        {
            showCancelConfirmation = true;
            cancellationReason = "";
            customCancellationReason = "";
            StateHasChanged();
        }

        private void HideCancelConfirmation()
        {
            showCancelConfirmation = false;
            StateHasChanged();
        }

        private async Task ConfirmCancel()
        {
            isCancelling = true;
            StateHasChanged();

            try
            {
                var finalNotes = cancellationReason == "Other" ? customCancellationReason : null;
                
                var args = new CancelInvoiceEventArgs
                {
                    Invoice = Invoice,
                    CancellationReason = cancellationReason,
                    CancellationNotes = finalNotes
                };
                
                await OnCancel.InvokeAsync(args);
                showCancelConfirmation = false;
            }
            finally
            {
                isCancelling = false;
                StateHasChanged();
            }
        }

        private void ShowMarkAsPaidConfirmation()
        {
            showMarkAsPaidConfirmation = true;
            paymentDate = DateTime.Today;
            paymentMethod = "Bank Transfer";
            paymentReference = "";
            StateHasChanged();
        }

        private void HideMarkAsPaidConfirmation()
        {
            showMarkAsPaidConfirmation = false;
            StateHasChanged();
        }

        private async Task ConfirmMarkAsPaid()
        {
            isMarkingPaid = true;
            StateHasChanged();

            try
            {
                var utcPaymentDate = DateTime.SpecifyKind(paymentDate.Date, DateTimeKind.Utc);

                var args = new MarkAsPaidEventArgs
                {
                    Invoice = Invoice,
                    PaymentDate = utcPaymentDate,
                    PaymentMethod = paymentMethod,
                    PaymentReference = paymentReference
                };

                await OnMarkAsPaid.InvokeAsync(args);
                showMarkAsPaidConfirmation = false;
            }
            finally
            {
                isMarkingPaid = false;
                StateHasChanged();
            }
        }

        protected string GetCustomerInitials()
        {
            // Use flat CustomerName property instead of nested Customer object
            var name = Invoice.CustomerName;

            if (string.IsNullOrWhiteSpace(name))
                return "?";

            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                return "?";

            if (parts.Length == 1)
                return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();

            return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
        }

        private void ShowDeliveredConfirmation()
        {
            showDeliveredConfirmation = true;
            StateHasChanged();
        }

        private void HideDeliveredConfirmation()
        {
            showDeliveredConfirmation = false;
            StateHasChanged();
        }

        private async Task ConfirmDeliver()
        {
            isMarkingDelivered = true;
            StateHasChanged();

            try
            {
                await OnMarkAsDelivered.InvokeAsync(Invoice);
                showDeliveredConfirmation = false;
            }
            finally
            {
                isMarkingDelivered = false;
                StateHasChanged();
            }
        }

        private string GetDeleteButtonText()
        {
            if (Invoice.Status != InvoiceStatus.Draft)
                return "Cannot Delete";
            
            return "Delete Invoice";
        }

        private string GetDeleteDisabledReason()
        {
            return Invoice.Status switch
            {
                InvoiceStatus.Sent => "Cannot delete sent invoices. Cancel the invoice first if needed.",
                InvoiceStatus.Delivered => "Cannot delete delivered invoices. Cancel the invoice first if needed.",
                InvoiceStatus.Paid => "Cannot delete paid invoices. Paid invoices must be preserved for financial records.",
                InvoiceStatus.Overdue => "Cannot delete overdue invoices. Cancel the invoice first if needed.",
                InvoiceStatus.Cancelled => "Cannot delete cancelled invoices. Cancelled invoices must be preserved for audit trail.",
                _ => ""
            };
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