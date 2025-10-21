using System.ComponentModel.DataAnnotations;

namespace Invoqs.Models
{
    public class ReceiptModel
    {
        public int Id { get; set; }

        [Required]
        public string ReceiptNumber { get; set; } = string.Empty;

        [Required]
        public int CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        [Required]
        [StringLength(100)]
        public string PaymentMethod { get; set; } = "Bank Transfer";

        [StringLength(200)]
        public string? PaymentReference { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        public DateTime CreatedDate { get; set; }

        public bool IsSent { get; set; } = false;
        public DateTime? SentDate { get; set; }

        public List<ReceiptInvoiceModel> Invoices { get; set; } = new();
    }

    public class ReceiptInvoiceModel
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public decimal AllocatedAmount { get; set; }
    }

    public class CreateReceiptModel
    {
        [Required]
        public int CustomerId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one invoice must be selected")]
        public List<int> InvoiceIds { get; set; } = new();

        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        [Required]
        public string PaymentMethod { get; set; } = "Bank Transfer";

        public string? PaymentReference { get; set; }
    }
}