using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace BuddyScript.Backend.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class StrongPasswordAttribute : ValidationAttribute
{
    private const int MinLength = 8;
    private const int MaxLength = 128;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var password = value as string;

        if (string.IsNullOrEmpty(password))
            return new ValidationResult("Password is required.");

        var missing = new List<string>();

        if (password.Length < MinLength)
            missing.Add($"at least {MinLength} characters");

        if (password.Length > MaxLength)
            missing.Add($"no more than {MaxLength} characters");

        if (!Regex.IsMatch(password, "[A-Z]"))
            missing.Add("one uppercase letter (A–Z)");

        if (!Regex.IsMatch(password, "[a-z]"))
            missing.Add("one lowercase letter (a–z)");

        if (!Regex.IsMatch(password, "[0-9]"))
            missing.Add("one digit (0–9)");

        if (!Regex.IsMatch(password, @"[^A-Za-z0-9]"))
            missing.Add("one special character (!@#$%^&* …)");

        if (missing.Count == 0)
            return ValidationResult.Success;

        return new ValidationResult(
            $"Password must contain: {string.Join(", ", missing)}.");
    }
}
