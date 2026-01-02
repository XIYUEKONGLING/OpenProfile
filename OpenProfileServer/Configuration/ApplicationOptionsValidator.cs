using Microsoft.Extensions.Options;

namespace OpenProfileServer.Configuration;

public class ApplicationOptionsValidator : IValidateOptions<ApplicationOptions>
{
    public ValidateOptionsResult Validate(string? name, ApplicationOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Version))
        {
            return ValidateOptionsResult.Fail("Application Version is required in configuration.");
        }

        return ValidateOptionsResult.Success;
    }
}