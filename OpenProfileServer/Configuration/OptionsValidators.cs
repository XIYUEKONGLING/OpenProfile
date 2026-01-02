using Microsoft.Extensions.Options;

namespace OpenProfileServer.Configuration;

public class CacheOptionsValidator : IValidateOptions<CacheOptions>
{
    public ValidateOptionsResult Validate(string? name, CacheOptions options)
    {
        if (!options.IsEnabled) return ValidateOptionsResult.Success;

        var errors = new List<string>();

        if (options.UseRedis && string.IsNullOrWhiteSpace(options.RedisConnection))
            errors.Add("Redis is enabled but connection string is missing.");

        if (options.DefaultExpirationMinutes <= 0)
            errors.Add("DefaultExpirationMinutes must be greater than zero.");

        if (options.FactorySoftTimeoutMilliseconds <= 0)
            errors.Add("FactorySoftTimeoutMilliseconds must be greater than zero.");

        return errors.Count > 0 ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
    }
}