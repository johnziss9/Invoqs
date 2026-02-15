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
        public bool CustomerIsDeleted { get; set; }
        public string? CustomerEmail { get; set; }
        public List<string> CustomerEmails { get; set; } = new();
        public string? CustomerPhone { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Discount must be non-negative")]
        public decimal DiscountAmount { get; set; } = 0m;

        public decimal AmountToPay => TotalAmount - DiscountAmount;

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
        public DateTime? PaymentDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentReference { get; set; }
    }

    public class CreateReceiptModel
    {
        [Required]
        public int CustomerId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one invoice must be selected")]
        public List<int> InvoiceIds { get; set; } = new();

        [Range(0, double.MaxValue, ErrorMessage = "Discount must be non-negative")]
        public decimal? DiscountAmount { get; set; }
    }
}