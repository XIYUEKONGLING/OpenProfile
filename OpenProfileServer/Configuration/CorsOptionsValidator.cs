using Microsoft.Extensions.Options;

namespace OpenProfileServer.Configuration;

public class CorsOptionsValidator : IValidateOptions<CorsOptions>
{
    public ValidateOptionsResult Validate(string? name, CorsOptions options)
    {
        if (options.AllowedOrigins == null)
        {
            return ValidateOptionsResult.Fail("AllowedOrigins cannot be null.");
        }

        return ValidateOptionsResult.Success;
    }
}