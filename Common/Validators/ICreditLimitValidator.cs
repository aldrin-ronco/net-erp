namespace Common.Validators
{
    public interface ICreditLimitValidator
    {
        ValidationResult ValidateLimit(decimal newLimit, decimal currentUsed, decimal originalLimit);
    }
}