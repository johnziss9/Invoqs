namespace Invoqs.Models
{
    public class CustomerModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int ActiveJobs { get; set; }
        public int CompletedJobs { get; set; }
        public decimal TotalRevenue { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}