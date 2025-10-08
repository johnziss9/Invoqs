namespace Invoqs.Models
{
    public class ApiValidationError
    {
        public Dictionary<string, List<string>> Errors { get; set; } = new();
        public string Title { get; set; } = string.Empty;
        public int Status { get; set; }
        
        public List<string> GetFieldErrors(string fieldName)
        {
            return Errors.ContainsKey(fieldName) ? Errors[fieldName] : new List<string>();
        }
    }
}