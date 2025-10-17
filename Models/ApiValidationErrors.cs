namespace Invoqs.Models
{
    public class ApiValidationError
    {
        public List<ValidationError> Errors { get; set; } = new();
        public string? Error { get; set; } // For single error messages
        
        public List<string> GetFieldErrors(string fieldName)
        {
            return Errors
                .Where(e => e.Field?.Equals(fieldName, StringComparison.OrdinalIgnoreCase) == true)
                .Select(e => e.Message)
                .ToList();
        }
        
        public List<string> GetAllErrors()
        {
            var allErrors = Errors.Select(e => e.Message).ToList();
            if (!string.IsNullOrEmpty(Error))
            {
                allErrors.Add(Error);
            }
            return allErrors;
        }
    }
    
    public class ValidationError
    {
        public string Field { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}