using System.Text.RegularExpressions;

namespace OpenProfileServer.Utilities;

/// <summary>
/// Provides validation for account names.
/// Account names must be 3-64 characters, containing only letters, numbers, underscores, or hyphens.
/// </summary>
public static class AccountNameValidator
{
    // Pattern: 3-64 characters, only alphanumeric, underscore, or hyphen
    private static readonly Regex AccountNameRegex = new("^[a-zA-Z0-9_-]{3,64}$", RegexOptions.Compiled);

    /// <summary>
    /// The minimum allowed length for an account name.
    /// </summary>
    public const int MinLength = 3;

    /// <summary>
    /// The maximum allowed length for an account name.
    /// </summary>
    public const int MaxLength = 64;

    /// <summary>
    /// The regex pattern used for validation.
    /// </summary>
    public const string Pattern = @"^[a-zA-Z0-9_-]{3,64}$";

    /// <summary>
    /// Validates whether the given account name meets the format requirements.
    /// </summary>
    /// <param name="accountName">The account name to validate.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    public static bool IsValid(string accountName)
    {
        return !string.IsNullOrWhiteSpace(accountName) && AccountNameRegex.IsMatch(accountName);
    }

    /// <summary>
    /// Validates the account name and returns a detailed result.
    /// </summary>
    /// <param name="accountName">The account name to validate.</param>
    /// <returns>A validation result indicating success or the specific error.</returns>
    public static AccountNameValidationResult ValidateWithResult(string accountName)
    {
        if (string.IsNullOrWhiteSpace(accountName))
            return AccountNameValidationResult.Fail("Account name is required.");

        if (accountName.Length < MinLength)
            return AccountNameValidationResult.Fail($"Account name must be at least {MinLength} characters.");

        if (accountName.Length > MaxLength)
            return AccountNameValidationResult.Fail($"Account name must not exceed {MaxLength} characters.");

        if (!AccountNameRegex.IsMatch(accountName))
            return AccountNameValidationResult.Fail("Account name can only contain letters, numbers, underscores, and hyphens.");

        return AccountNameValidationResult.Success();
    }
}

/// <summary>
/// Represents the result of an account name validation.
/// </summary>
public sealed record AccountNameValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }

    public static AccountNameValidationResult Success() => new() { IsValid = true };
    public static AccountNameValidationResult Fail(string errorMessage) => new() { IsValid = false, ErrorMessage = errorMessage };
}
