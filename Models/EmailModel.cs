namespace Invoqs.Models
{
    public class EmailModel
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}