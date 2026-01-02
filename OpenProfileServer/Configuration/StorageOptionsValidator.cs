using Microsoft.Extensions.Options;

namespace OpenProfileServer.Configuration;

public class StorageOptionsValidator : IValidateOptions<StorageOptions>
{
    public ValidateOptionsResult Validate(string? name, StorageOptions options)
    {
        if (options.MaxUploadSizeBytes <= 0)
            return ValidateOptionsResult.Fail("MaxUploadSizeBytes must be greater than 0.");

        if (options.AllowedMimeTypes == null || options.AllowedMimeTypes.Length == 0)
            return ValidateOptionsResult.Fail("AllowedMimeTypes cannot be empty.");

        return ValidateOptionsResult.Success;
    }
}