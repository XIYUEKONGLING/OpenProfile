using Microsoft.Extensions.Options;

namespace OpenProfileServer.Configuration;

public class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
            errors.Add("Jwt:Issuer is required.");

        if (string.IsNullOrWhiteSpace(options.Audience))
            errors.Add("Jwt:Audience is required.");

        if (options.AccessTokenExpirationMinutes <= 0)
            errors.Add("Jwt:AccessTokenExpirationMinutes must be greater than zero.");

        if (options.RefreshTokenExpirationDays <= 0)
            errors.Add("Jwt:RefreshTokenExpirationDays must be greater than zero.");

        return errors.Count > 0 ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
    }
}