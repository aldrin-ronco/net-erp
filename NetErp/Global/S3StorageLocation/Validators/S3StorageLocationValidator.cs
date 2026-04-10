using System.Collections.Generic;

namespace NetErp.Global.S3StorageLocation.Validators
{
    public class S3StorageLocationValidator
    {
        public IReadOnlyList<string> Validate(string propertyName, string? value)
        {
            if (string.IsNullOrEmpty(value)) value = string.Empty;

            return propertyName switch
            {
                "Description" when string.IsNullOrEmpty(value) => ["La descripción no puede estar vacía"],
                "Key" when string.IsNullOrEmpty(value) => ["La clave no puede estar vacía"],
                "Bucket" when string.IsNullOrEmpty(value) => ["El bucket no puede estar vacío"],
                "Directory" when string.IsNullOrEmpty(value) => ["El directorio no puede estar vacío"],
                _ => []
            };
        }

        public Dictionary<string, IReadOnlyList<string>> ValidateAll(string? description, string? key, string? bucket, string? directory)
        {
            var result = new Dictionary<string, IReadOnlyList<string>>();

            var descErrors = Validate("Description", description);
            if (descErrors.Count > 0) result["Description"] = descErrors;

            var keyErrors = Validate("Key", key);
            if (keyErrors.Count > 0) result["Key"] = keyErrors;

            var bucketErrors = Validate("Bucket", bucket);
            if (bucketErrors.Count > 0) result["Bucket"] = bucketErrors;

            var dirErrors = Validate("Directory", directory);
            if (dirErrors.Count > 0) result["Directory"] = dirErrors;

            return result;
        }

        public bool CanSave(bool hasChanges, bool hasErrors)
        {
            return !hasErrors && hasChanges;
        }
    }
}
