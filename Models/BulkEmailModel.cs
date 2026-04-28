namespace Invoqs.Models
{
    public class BulkEmailRequest
    {
        public List<int> CustomerIds { get; set; } = new();
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Language { get; set; } = "el";
    }

    public class BulkEmailResult
    {
        public int SentCount { get; set; }
        public int FailedCount { get; set; }
        public List<BulkEmailFailure> Failures { get; set; } = new();
    }

    public class BulkEmailFailure
    {
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}
