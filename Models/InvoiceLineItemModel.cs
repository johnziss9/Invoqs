using System.ComponentModel.DataAnnotations;

namespace Invoqs.Models
{
    public class InvoiceLineItemModel
    {
        public int Id { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        public InvoiceModel? Invoice { get; set; }

        [Required]
        public int JobId { get; set; }

        public JobModel? Job { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = "";

        [Required]
        [Range(1, 1, ErrorMessage = "Quantity must be 1 (each job is individual)")]
        public int Quantity { get; set; } = 1;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
        public decimal UnitPrice { get; set; }

        public decimal LineTotal => Quantity * UnitPrice;

        public string? JobTitle { get; set; }
        public JobType? JobType { get; set; }
        public string? JobAddress { get; set; }

        // Computed properties for UI display
        public string FormattedUnitPrice => $"€{UnitPrice:N2}";
        public string FormattedLineTotal => $"€{LineTotal:N2}";

        // Helper method to create line item from job
        public static InvoiceLineItemModel FromJob(JobModel job)
        {
            return new InvoiceLineItemModel
            {
                JobId = job.Id,
                Job = job,
                Description = GenerateDescription(job),
                UnitPrice = job.Price,
                Quantity = 1
            };
        }

        private static string GenerateDescription(JobModel job)
        {
            var typeDescription = job.TypeDisplayName;
            
            var dateRange = job.EndDate.HasValue && job.EndDate != job.StartDate
                ? $"{job.StartDate:dd/MM/yyyy} - {job.EndDate:dd/MM/yyyy}"
                : job.StartDate.ToString("dd/MM/yyyy");

            var description = $"{typeDescription} - {job.Address} ({dateRange})";
            
            // Add job title if it's different from type
            if (!string.IsNullOrEmpty(job.Title) && job.Title != job.TypeDisplayName)
            {
                description = $"{job.Title} - {typeDescription} - {job.Address} ({dateRange})";
            }

            return description;
        }

        // Helper method to validate job can be invoiced
        public static bool CanJobBeInvoiced(JobModel job)
        {
            return job.Status == JobStatus.Completed && !job.IsInvoiced;
        }
    }
}