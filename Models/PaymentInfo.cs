using System.ComponentModel.DataAnnotations;

namespace Invoqs.Models
{
    public class PaymentInfo
    {
        [Required(ErrorMessage = "Payment date is required")]
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Payment method is required")]
        public string PaymentMethod { get; set; } = string.Empty;

        [Required(ErrorMessage = "Amount paid is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
        public decimal AmountPaid { get; set; }

        public string? Notes { get; set; }
    }
}