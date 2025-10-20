using System.ComponentModel.DataAnnotations;

namespace Invoqs.Models
{
    public class InvoiceModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Invoice number is required")]
        [StringLength(50, ErrorMessage = "Invoice number cannot exceed 50 characters")]
        public string InvoiceNumber { get; set; } = "";

        [Required(ErrorMessage = "Customer is required")]
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }

        public CustomerModel? Customer => string.IsNullOrEmpty(CustomerName) ? null : new CustomerModel
        {
            Id = CustomerId,
            Name = CustomerName,
            Email = CustomerEmail ?? "",
            Phone = CustomerPhone ?? ""
        };

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Required]
        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(30);

        public DateTime? PaidDate { get; set; }

        [Required]
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

        [Range(0, double.MaxValue, ErrorMessage = "Subtotal must be positive")]
        public decimal Subtotal { get; set; }

        [Range(0, 100, ErrorMessage = "VAT rate must be between 0 and 100")]
        public decimal VatRate { get; set; } = 0;

        public decimal VatAmount => Math.Round(Subtotal * (VatRate / 100), 2);

        public decimal Total => Subtotal + VatAmount;

        [Range(0, int.MaxValue, ErrorMessage = "Payment terms must be positive")]
        public int PaymentTermsDays { get; set; } = 30;

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string? Notes { get; set; }

        [StringLength(100, ErrorMessage = "Payment method cannot exceed 100 characters")]
        public string? PaymentMethod { get; set; }

        [StringLength(200, ErrorMessage = "Payment reference cannot exceed 200 characters")]
        public string? PaymentReference { get; set; }

        public DateTime? UpdatedDate { get; set; }

        // Navigation property for line items
        public List<InvoiceLineItemModel> LineItems { get; set; } = new();

        // Computed properties for UI
        public string StatusBadgeClass => Status switch
        {
            InvoiceStatus.Draft => "bg-secondary",
            InvoiceStatus.Sent => "bg-primary",
            InvoiceStatus.Paid => "bg-success",
            InvoiceStatus.Overdue => "bg-danger",
            InvoiceStatus.Cancelled => "bg-dark",
            _ => "bg-secondary"
        };

        public string StatusIcon => Status switch
        {
            InvoiceStatus.Draft => "bi-file-earmark",
            InvoiceStatus.Sent => "bi-send",
            InvoiceStatus.Paid => "bi-check-circle",
            InvoiceStatus.Overdue => "bi-exclamation-triangle",
            InvoiceStatus.Cancelled => "bi-x-circle",
            _ => "bi-file-earmark"
        };

        public bool IsOverdue => Status != InvoiceStatus.Paid 
            && Status != InvoiceStatus.Cancelled 
            && DueDate < DateTime.Now;

        public int DaysUntilDue => (int)(DueDate - DateTime.Now).TotalDays;

        public string DueDateDisplay
        {
            get
            {
                if (Status == InvoiceStatus.Paid && PaidDate.HasValue)
                    return $"Paid {PaidDate.Value:dd/MM/yyyy}";

                if (IsOverdue)
                    return $"Overdue by {Math.Abs(DaysUntilDue)} days";

                return DaysUntilDue switch
                {
                    0 => "Due today",
                    1 => "Due tomorrow",
                    _ when DaysUntilDue > 0 => $"Due in {DaysUntilDue} days",
                    _ => $"Overdue by {Math.Abs(DaysUntilDue)} days"
                };
            }
        }

        // Helper method for invoice numbering
        public static string GenerateInvoiceNumber(int invoiceId)
        {
            return $"INV-{DateTime.Now.Year}-{invoiceId:D4}";
        }
    }

    public enum InvoiceStatus
    {
        Draft,
        Sent,
        Paid,
        Overdue,
        Cancelled
    }
}