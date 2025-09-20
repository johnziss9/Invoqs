using System.ComponentModel.DataAnnotations;

namespace Invoqs.Models
{
    public class CustomerModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Customer name is required")]
        [StringLength(100, ErrorMessage = "Customer name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Company registration number cannot exceed 20 characters")]
        public string? CompanyRegistrationNumber { get; set; }

        [StringLength(20, ErrorMessage = "VAT number cannot exceed 20 characters")]
        public string? VatNumber { get; set; }

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string? Notes { get; set; }

        public int ActiveJobs { get; set; }
        public int CompletedJobs { get; set; }
        public decimal TotalRevenue { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Computed properties
        public int TotalJobs => ActiveJobs + CompletedJobs;
        public string Status => ActiveJobs > 0 ? "Active" : "Inactive";
    }
}