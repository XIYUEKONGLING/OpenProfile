using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Utilities;

public static class AssetValidator
{
    /// <summary>
    /// Validates an asset DTO against size limits and type restrictions.
    /// </summary>
    /// <param name="asset">The asset to validate.</param>
    /// <param name="maxSize">The maximum allowed size in bytes.</param>
    /// <returns>A tuple indicating success and an optional error message.</returns>
    public static (bool Valid, string? Error) Validate(AssetDto? asset, int maxSize)
    {
        if (asset == null || string.IsNullOrEmpty(asset.Value))
            return (true, null);

        // 1. Prohibit the use of 'Identifier' from frontend
        if (asset.Type == AssetType.Identifier)
        {
            return (false, "Asset type 'Identifier' is reserved for system use.");
        }

        // 2. Size Check
        long sizeInBytes = 0;

        if (asset.Type == AssetType.Image)
        {
            // Base64 size calculation: (n * 3) / 4 - padding
            var base64 = asset.Value;
            int padding = base64.EndsWith("==") ? 2 : (base64.EndsWith("=") ? 1 : 0);
            sizeInBytes = (long)base64.Length * 3 / 4 - padding;
        }
        else
        {
            // For Text, Remote, Style, we just check the string length
            sizeInBytes = System.Text.Encoding.UTF8.GetByteCount(asset.Value);
        }

        if (sizeInBytes > maxSize)
        {
            double mb = Math.Round((double)maxSize / 1024 / 1024, 2);
            return (false, $"Asset payload too large. Maximum allowed is {mb}MB.");
        }

        return (true, null);
    }
}