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
        ForkLiftService = 2
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

        // NEW: Invoice integration properties
        public bool IsInvoiced { get; set; } = false;
        public int? InvoiceId { get; set; }
        public DateTime? InvoicedDate { get; set; }

        // ===== JOB TYPE SPECIFIC FIELDS =====
        
        // Skip Rental specific fields
        public string? SkipType { get; set; }
        public string? SkipNumber { get; set; }
        
        // Sand Delivery specific fields
        public string? SandMaterialType { get; set; }
        public string? SandDeliveryMethod { get; set; }
        
        // Forklift Service specific fields
        public string? ForkliftSize { get; set; }

        // Navigation properties
        public CustomerModel? Customer { get; set; }
        public InvoiceModel? Invoice { get; set; }

        // Existing computed properties
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
            JobType.SkipRental => "/images/icons/skip.png",
            JobType.SandDelivery => "/images/icons/sand.png", 
            JobType.ForkLiftService => "/images/icons/forklift.png",
            _ => "bi-building"
        };

        public string TypeDisplayName => Type switch
        {
            JobType.SkipRental => "Skip Rental",
            JobType.SandDelivery => "Sand Delivery",
            JobType.ForkLiftService => "Fork Lift Service",
            _ => "Unknown"
        };

        // ===== JOB TYPE SPECIFIC DISPLAY HELPERS =====
        
        public string SkipTypeDisplay => SkipType switch
        {
            "SmallSkip" => "Small Skip",
            "LargeSkip" => "Large Skip",
            "Hook" => "Hook",
            _ => ""
        };
        
        public string SandMaterialTypeDisplay => SandMaterialType switch
        {
            "Sand" => "Sand",
            "CrushedStone" => "Crushed Stone",
            "SandMixedWithCrushedStone" => "Sand Mixed with Crushed Stone",
            "Soil" => "Soil",
            _ => ""
        };
        
        public string SandDeliveryMethodDisplay => SandDeliveryMethod switch
        {
            "InBags" => "In Bags",
            "ByTruck" => "By Truck",
            _ => ""
        };
        
        public string ForkliftSizeDisplay => ForkliftSize switch
        {
            "17m" => "17m",
            "25m" => "25m",
            _ => ""
        };

        public string ShortAddress => Address.Length > 50 ? Address.Substring(0, 47) + "..." : Address;

        // NEW: Invoice integration helper properties and methods
        public bool CanBeInvoiced => Status == JobStatus.Completed && !IsInvoiced;

        public decimal GetVatRate() => Type switch
        {
            JobType.SkipRental => 5m,
            JobType.SandDelivery => 19m,
            JobType.ForkLiftService => 19m,
            _ => 5m // Default to lower rate
        };

        public string FormattedPrice => $"£{Price:N2}";

        public void MarkAsInvoiced(int invoiceId)
        {
            IsInvoiced = true;
            InvoiceId = invoiceId;
            InvoicedDate = DateTime.Now;
            UpdatedDate = DateTime.Now;
        }

        public void RemoveFromInvoice()
        {
            IsInvoiced = false;
            InvoiceId = null;
            InvoicedDate = null;
            UpdatedDate = DateTime.Now;
        }

        public string GetInvoiceDescription()
        {
            var dateRange = EndDate.HasValue && EndDate != StartDate
                ? $"{StartDate:dd/MM/yyyy} - {EndDate:dd/MM/yyyy}"
                : StartDate.ToString("dd/MM/yyyy");

            var description = $"{TypeDisplayName} - {Address} ({dateRange})";
            
            // Add job title if it's different from type
            if (!string.IsNullOrEmpty(Title) && Title != TypeDisplayName)
            {
                description = $"{Title} - {TypeDisplayName} - {Address} ({dateRange})";
            }

            return description;
        }
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

        // NEW: Invoice-related properties for address groups
        public int CompletedUninvoicedJobs => Jobs.Count(j => j.CanBeInvoiced);
        public decimal UninvoicedRevenue => Jobs.Where(j => j.CanBeInvoiced).Sum(j => j.Price);
        public bool HasJobsToInvoice => CompletedUninvoicedJobs > 0;
    }
}