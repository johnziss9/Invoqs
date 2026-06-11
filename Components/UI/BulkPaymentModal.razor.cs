using Microsoft.AspNetCore.Components;
using Invoqs.Models;

namespace Invoqs.Components.UI
{
    public partial class BulkPaymentModal : ComponentBase
    {
        [Parameter] public List<InvoiceModel> OutstandingInvoices { get; set; } = new();
        [Parameter] public EventCallback<BulkPaymentConfirmArgs> OnConfirm { get; set; }
        [Parameter] public EventCallback OnCancel { get; set; }

        private decimal totalPaymentAmount = 0m;
        private DateTime paymentDate = DateTime.Today;
        private string paymentMethod = "Bank Transfer";
        private string? paymentReference;
        private Dictionary<int, decimal> allocations = new();
        private string errorMessage = "";
        private bool isSubmitting = false;

        private decimal TotalAllocated => allocations.Values.Sum();
        private decimal Unallocated => totalPaymentAmount - TotalAllocated;
        private bool CanConfirm =>
            totalPaymentAmount > 0 &&
            TotalAllocated > 0 &&
            TotalAllocated <= totalPaymentAmount + 0.01m &&
            allocations.All(a => a.Value <= (OutstandingInvoices.FirstOrDefault(i => i.Id == a.Key)?.RemainingAmount ?? 0) + 0.01m) &&
            !isSubmitting;

        protected override void OnInitialized()
        {
            foreach (var inv in OutstandingInvoices)
                allocations[inv.Id] = 0m;
        }

        private void OnTotalAmountChanged(ChangeEventArgs e)
        {
            if (decimal.TryParse(e.Value?.ToString(), out var val))
                totalPaymentAmount = val;
            else
                totalPaymentAmount = 0m;

            errorMessage = "";
        }

        private void OnAllocationChanged(int invoiceId, decimal maxAmount, ChangeEventArgs e)
        {
            if (decimal.TryParse(e.Value?.ToString(), out var val))
                allocations[invoiceId] = Math.Min(Math.Max(val, 0m), maxAmount);
            else
                allocations[invoiceId] = 0m;

            errorMessage = "";
        }

        private void AutoAllocate()
        {
            var remaining = totalPaymentAmount;
            foreach (var invoice in OutstandingInvoices.OrderBy(i => i.CreatedDate))
            {
                var canAllocate = Math.Min(remaining, invoice.RemainingAmount);
                allocations[invoice.Id] = Math.Round(canAllocate, 2);
                remaining -= canAllocate;
                if (remaining <= 0.01m) break;
            }
            // zero out invoices not reached
            foreach (var invoice in OutstandingInvoices)
            {
                if (!allocations.ContainsKey(invoice.Id))
                    allocations[invoice.Id] = 0m;
            }
            errorMessage = "";
        }

        private async Task Confirm()
        {
            errorMessage = "";

            if (TotalAllocated <= 0)
            {
                errorMessage = "Παρακαλώ κατανείμετε τουλάχιστον ένα ποσό σε ένα τιμολόγιο.";
                return;
            }

            if (TotalAllocated > totalPaymentAmount + 0.01m)
            {
                errorMessage = "Το κατανεμημένο ποσό υπερβαίνει το συνολικό ποσό πληρωμής.";
                return;
            }

            isSubmitting = true;

            var activeAllocations = allocations
                .Where(a => a.Value > 0)
                .Select(a => (InvoiceId: a.Key, Amount: a.Value))
                .ToList();

            await OnConfirm.InvokeAsync(new BulkPaymentConfirmArgs
            {
                Allocations = activeAllocations,
                PaymentDate = paymentDate,
                PaymentMethod = paymentMethod,
                PaymentReference = paymentReference
            });

            isSubmitting = false;
        }
    }

    public class BulkPaymentConfirmArgs
    {
        public List<(int InvoiceId, decimal Amount)> Allocations { get; set; } = new();
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = "Bank Transfer";
        public string? PaymentReference { get; set; }
    }
}
