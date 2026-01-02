using System.IO.Compression;
using Microsoft.Extensions.Options;

namespace OpenProfileServer.Configuration;

public class CompressionOptionsValidator : IValidateOptions<CompressionOptions>
{
    public ValidateOptionsResult Validate(string? name, CompressionOptions options)
    {
        if (!options.Enabled) return ValidateOptionsResult.Success;

        var validLevels = new[] { "Fastest", "Optimal", "SmallestSize", "NoCompression" };
        
        if (string.IsNullOrWhiteSpace(options.Level) || 
            !validLevels.Contains(options.Level, StringComparer.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail($"Compression Level must be one of: {string.Join(", ", validLevels)}");
        }

        return ValidateOptionsResult.Success;
    }
}