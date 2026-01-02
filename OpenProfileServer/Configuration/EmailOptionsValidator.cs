using Microsoft.Extensions.Options;

namespace OpenProfileServer.Configuration;

public class EmailOptionsValidator : IValidateOptions<EmailOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        if (!options.IsEnabled) return ValidateOptionsResult.Success;

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Host))
            errors.Add("Email Host is required when Email is enabled.");

        if (options.Port <= 0)
            errors.Add("Email Port must be greater than 0.");

        if (string.IsNullOrWhiteSpace(options.FromAddress))
            errors.Add("FromAddress is required.");

        return errors.Count > 0 ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
    }
}