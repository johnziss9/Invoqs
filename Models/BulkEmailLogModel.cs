namespace Invoqs.Models
{
    public class BulkEmailLogModel
    {
        public int Id { get; set; }
        public DateTime SentDate { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Language { get; set; } = "el";
        public string SentByUserName { get; set; } = string.Empty;
        public int TotalRecipients { get; set; }
        public int SentCount { get; set; }
        public int FailedCount { get; set; }
        public List<BulkEmailRecipientModel> Recipients { get; set; } = new();
    }

    public class BulkEmailRecipientModel
    {
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
}
