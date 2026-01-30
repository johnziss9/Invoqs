namespace Invoqs.Models
{
    public class DuplicateCheckRequest
    {
        public List<string> Emails { get; set; } = new();
        public int? ExcludeCustomerId { get; set; }
    }

    public class DuplicateCheckResponse
    {
        public bool HasDuplicates { get; set; }
        public List<DuplicateCustomerModel> DuplicateCustomers { get; set; } = new();
    }

    public class DuplicateCustomerModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int TotalJobs { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<string> MatchingEmails { get; set; } = new();
    }
}