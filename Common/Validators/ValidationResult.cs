namespace Common.Validators
{
    public enum ValidationSeverity
    {
        Error,
        Warning,
        Information
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public ValidationSeverity Severity { get; set; }
        
        public static ValidationResult Success() => new() { IsValid = true };
        
        public static ValidationResult Error(string message) => new() 
        { 
            IsValid = false, 
            ErrorMessage = message, 
            Severity = ValidationSeverity.Error 
        };
        
        public static ValidationResult Warning(string message) => new() 
        { 
            IsValid = true, 
            ErrorMessage = message, 
            Severity = ValidationSeverity.Warning 
        };
        
        public static ValidationResult Info(string message) => new() 
        { 
            IsValid = true, 
            ErrorMessage = message, 
            Severity = ValidationSeverity.Information 
        };
    }
}