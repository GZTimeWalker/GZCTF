using System.ComponentModel.DataAnnotations;

namespace GZCTF.Utils;

/// <summary>
/// Validator for Transfer data models
/// </summary>
public static class TransferValidator
{
    /// <summary>
    /// Validate an object with DataAnnotations attributes
    /// </summary>
    /// <param name="obj">Object to validate</param>
    /// <param name="objectName">Name of the object for error messages</param>
    /// <exception cref="InvalidOperationException">Thrown when validation fails</exception>
    public static void Validate(object obj, string objectName = "Object")
    {
        if (!TryValidate(obj, out var results, validateRecursive: false))
        {
            var errors = results.Select(r => r.ErrorMessage).ToList();
            throw new InvalidOperationException(
                $"{objectName} validation failed:\n- {string.Join("\n- ", errors)}");
        }
    }

    /// <summary>
    /// Validate an object recursively
    /// </summary>
    /// <param name="obj">Object to validate</param>
    /// <param name="objectName">Name of the object for error messages</param>
    /// <exception cref="InvalidOperationException">Thrown when validation fails</exception>
    public static void ValidateRecursive(object obj, string objectName = "Object")
    {
        if (!TryValidate(obj, out var results, validateRecursive: true))
        {
            var errors = results.Select(r => r.ErrorMessage).ToList();
            throw new InvalidOperationException(
                $"{objectName} validation failed:\n- {string.Join("\n- ", errors)}");
        }
    }

    /// <summary>
    /// Validate an object and collect all validation errors
    /// </summary>
    /// <param name="obj">Object to validate</param>
    /// <param name="results">List to collect validation results</param>
    /// <param name="validateRecursive">Whether to validate nested objects</param>
    /// <returns>True if validation passed, false otherwise</returns>
    public static bool TryValidate(object obj, out List<ValidationResult> results, bool validateRecursive = true)
    {
        results = new List<ValidationResult>();
        var context = new ValidationContext(obj);

        var isValid = Validator.TryValidateObject(obj, context, results, validateAllProperties: true);

        if (validateRecursive && isValid)
        {
            // Recursively validate collection properties
            var properties = obj.GetType().GetProperties()
                .Where(p => typeof(IEnumerable<object>).IsAssignableFrom(p.PropertyType) &&
                            p.PropertyType != typeof(string));

            foreach (var property in properties)
            {
                var value = property.GetValue(obj);
                if (value is IEnumerable<object> collection)
                {
                    var index = 0;
                    foreach (var item in collection)
                    {
                        if (!TryValidate(item, out var itemResults, validateRecursive: true))
                        {
                            results.AddRange(itemResults.Select(r =>
                                new ValidationResult(
                                    $"{property.Name}[{index}]: {r.ErrorMessage}",
                                    r.MemberNames.Select(m => $"{property.Name}[{index}].{m}"))));
                            isValid = false;
                        }
                        index++;
                    }
                }
            }

            // Recursively validate object properties
            var objectProperties = obj.GetType().GetProperties()
                .Where(p => p.PropertyType.IsClass &&
                            p.PropertyType != typeof(string) &&
                            !typeof(IEnumerable<object>).IsAssignableFrom(p.PropertyType));

            foreach (var property in objectProperties)
            {
                var value = property.GetValue(obj);
                if (value is not null && !TryValidate(value, out var propResults, validateRecursive: true))
                {
                    results.AddRange(propResults.Select(r =>
                        new ValidationResult(
                            $"{property.Name}.{r.ErrorMessage}",
                            r.MemberNames.Select(m => $"{property.Name}.{m}"))));
                    isValid = false;
                }
            }
        }

        return isValid;
    }
}
