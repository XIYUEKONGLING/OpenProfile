using Microsoft.Extensions.Options;

namespace OpenProfileServer.Configuration;

public class RateLimitOptionsValidator : IValidateOptions<RateLimitOptions>
{
    public ValidateOptionsResult Validate(string? name, RateLimitOptions options)
    {
        if (options.Policies.GroupBy(p => p.Name).Any(g => g.Count() > 1))
            return ValidateOptionsResult.Fail("Rate limit policy names must be unique.");

        if (options.Policies.Any(p => p.PermitLimit <= 0 || p.PeriodSeconds <= 0))
            return ValidateOptionsResult.Fail("Rate limit policies must have positive limits and periods.");

        return ValidateOptionsResult.Success;
    }
}