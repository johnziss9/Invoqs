using System.ComponentModel.DataAnnotations;

namespace Invoqs.Models
{
    public class CustomerStatementModel
    {
        public int Id { get; set; }
        public string StatementNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public List<string> CustomerEmails { get; set; } = new();
        public string? CustomerVatNumber { get; set; }
        public string? CustomerCompanyRegistrationNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalInvoiced { get; set; }
        public decimal TotalVatAmount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalPartiallyPaid { get; set; }
        public decimal TotalCancelled { get; set; }
        public decimal OutstandingBalance { get; set; }
        public int InvoiceCount { get; set; }
        public int CancelledInvoiceCount { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsSent { get; set; }
        public DateTime? SentDate { get; set; }
        public bool IsDelivered { get; set; }
        public DateTime? DeliveredDate { get; set; }

        public List<CustomerStatementInvoiceModel> Invoices { get; set; } = new();
        public List<CustomerStatementInvoiceModel> CancelledInvoices { get; set; } = new();
    }

    public class CustomerStatementInvoiceModel
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public decimal Subtotal { get; set; }
        public decimal VatAmount { get; set; }
        public decimal Total { get; set; }
        public InvoiceStatus Status { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentReference { get; set; }
        public string? JobAddress { get; set; }
    }

    public class CreateCustomerStatementModel
    {
        [Required(ErrorMessage = "Customer is required")]
        public int? CustomerId { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        public DateTime? StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        public DateTime? EndDate { get; set; }
    }
}
