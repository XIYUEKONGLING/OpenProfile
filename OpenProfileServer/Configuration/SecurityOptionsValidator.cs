using Microsoft.Extensions.Options;

namespace OpenProfileServer.Configuration;

public class SecurityOptionsValidator : IValidateOptions<SecurityOptions>
{
    public ValidateOptionsResult Validate(string? name, SecurityOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.RootUser))
            errors.Add("RootUser is required.");
        
        if (string.IsNullOrWhiteSpace(options.RootPassword) || options.RootPassword.Length < 12)
            errors.Add("RootPassword must be at least 12 characters long.");

        // JWT HMAC-SHA256 requires at least 256 bits (32 bytes)
        if (string.IsNullOrWhiteSpace(options.ApplicationSecret))
        {
            errors.Add("ApplicationSecret is required.");
        }
        else if (options.ApplicationSecret.Length < 32)
        {
            errors.Add("ApplicationSecret must be at least 32 characters long to satisfy HMAC-SHA256 requirements.");
        }

        return errors.Count > 0 
            ? ValidateOptionsResult.Fail(errors) 
            : ValidateOptionsResult.Success;
    }
}