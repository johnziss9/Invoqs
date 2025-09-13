using Microsoft.AspNetCore.Components;
using Invoqs.Models;

namespace Invoqs.Components.UI
{
    public partial class InvoiceCard : ComponentBase
    {
        [Parameter] public InvoiceModel Invoice { get; set; } = new();
        [Parameter] public EventCallback<InvoiceModel> OnView { get; set; }
        [Parameter] public EventCallback<InvoiceModel> OnEdit { get; set; }
        [Parameter] public EventCallback<InvoiceModel> OnMarkAsSent { get; set; }
        [Parameter] public EventCallback<InvoiceModel> OnMarkAsPaid { get; set; }
        [Parameter] public EventCallback<InvoiceModel> OnCancel { get; set; }

        protected string GetCustomerInitials()
        {
            if (Invoice.Customer == null || string.IsNullOrWhiteSpace(Invoice.Customer.Name))
                return "?";

            var parts = Invoice.Customer.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();

            return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
        }
    }
}