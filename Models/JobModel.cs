using System.ComponentModel.DataAnnotations;

namespace Invoqs.Models
{
    public enum JobStatus
    {
        New = 0,
        Active = 1,
        Completed = 2,
        Cancelled = 3
    }

    public enum JobType
    {
        SkipRental = 0,
        SandDelivery = 1,
        FortCliffService = 2
    }

    public class JobModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Customer ID is required")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Job title is required")]
        [StringLength(200, ErrorMessage = "Job title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Job type is required")]
        public JobType Type { get; set; }

        [Required(ErrorMessage = "Job status is required")]
        public JobStatus Status { get; set; } = JobStatus.New;

        [Required(ErrorMessage = "Service address is required")]
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        // Navigation property
        public CustomerModel? Customer { get; set; }

        // Computed properties
        public int DurationDays => EndDate.HasValue
            ? (int)(EndDate.Value - StartDate).TotalDays + 1
            : (int)(DateTime.Now - StartDate).TotalDays + 1;

        public string StatusColor => Status switch
        {
            JobStatus.New => "secondary",
            JobStatus.Active => "primary",
            JobStatus.Completed => "success",
            JobStatus.Cancelled => "danger",
            _ => "secondary"
        };

        public string StatusIcon => Status switch
        {
            JobStatus.New => "bi-clock",
            JobStatus.Active => "bi-play-circle",
            JobStatus.Completed => "bi-check-circle",
            JobStatus.Cancelled => "bi-x-circle",
            _ => "bi-clock"
        };

        public string TypeIcon => Type switch
        {
            JobType.SkipRental => "icon-skip", // Custom skip icon
            JobType.SandDelivery => "icon-delivery-truck", // Custom delivery truck icon
            JobType.FortCliffService => "icon-cliff", // Custom cliff icon
            _ => "bi-briefcase"
        };

        public string TypeDisplayName => Type switch
        {
            JobType.SkipRental => "Skip Rental",
            JobType.SandDelivery => "Sand Delivery",
            JobType.FortCliffService => "Fort Cliff Service",
            _ => "Unknown"
        };

        public string ShortAddress => Address.Length > 50 ? Address.Substring(0, 47) + "..." : Address;
    }

    public class AddressJobGroup
    {
        public string Address { get; set; } = string.Empty;
        public List<JobModel> Jobs { get; set; } = new();
        public int TotalJobs => Jobs.Count;
        public int ActiveJobs => Jobs.Count(j => j.Status == JobStatus.Active);
        public int CompletedJobs => Jobs.Count(j => j.Status == JobStatus.Completed);
        public int NewJobs => Jobs.Count(j => j.Status == JobStatus.New);
        public int CancelledJobs => Jobs.Count(j => j.Status == JobStatus.Cancelled);
        public decimal TotalRevenue => Jobs.Where(j => j.Status == JobStatus.Completed).Sum(j => j.Price);
        public DateTime? EarliestJobDate => Jobs.Min(j => j.StartDate);
        public DateTime? LatestJobDate => Jobs.Max(j => j.StartDate);
    }
}