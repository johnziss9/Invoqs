using System.ComponentModel.DataAnnotations;

namespace Invoqs.Models
{
    public class StatementModel
    {
        public int Id { get; set; }

        [Required]
        public string StatementNumber { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal TotalVatAmount { get; set; }
        public decimal CancelledAmount { get; set; }
        public decimal CancelledVatAmount { get; set; }

        public int InvoiceCount { get; set; }
        public int CancelledInvoiceCount { get; set; }

        public DateTime CreatedDate { get; set; }

        public bool IsSent { get; set; } = false;
        public DateTime? SentDate { get; set; }

        public bool IsDelivered { get; set; } = false;
        public DateTime? DeliveredDate { get; set; }

        public List<StatementInvoiceModel> Invoices { get; set; } = new();
        public List<StatementInvoiceModel> CancelledInvoices { get; set; } = new();
    }

    public class StatementInvoiceModel
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public decimal VatAmount { get; set; }
        public InvoiceStatus Status { get; set; }
    }

    public class CreateStatementModel
    {
        [Required(ErrorMessage = "Start date is required")]
        public DateTime? StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        public DateTime? EndDate { get; set; }
    }
}
