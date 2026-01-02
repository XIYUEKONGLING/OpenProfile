using Microsoft.Extensions.Options;

namespace OpenProfileServer.Configuration;

public class SecurityOptionsValidator : IValidateOptions<SecurityOptions>
{
    public ValidateOptionsResult Validate(string? name, SecurityOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.RootUser))
            return ValidateOptionsResult.Fail("RootUser is required.");
        
        // Enforce strong root password
        if (string.IsNullOrWhiteSpace(options.RootPassword) || options.RootPassword.Length < 12)
            return ValidateOptionsResult.Fail("RootPassword must be at least 12 characters long.");

        return ValidateOptionsResult.Success;
    }
}