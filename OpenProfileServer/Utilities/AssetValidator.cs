using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.Enums;
using System.Text;

namespace OpenProfileServer.Utilities;

public static class AssetValidator
{
    /// <summary>
    /// Validates an asset DTO against size limits and type restrictions.
    /// </summary>
    public static (bool Valid, string? Error) Validate(AssetDto? asset, int maxSize, string fieldName = "Asset")
    {
        if (asset == null || string.IsNullOrEmpty(asset.Value))
            return (true, null);

        // 1. Prohibit the use of 'Identifier' from frontend (Reserved for system/backend)
        if (asset.Type == AssetType.Identifier)
        {
            return (false, $"{fieldName}: Type 'Identifier' is reserved for system use.");
        }

        // 2. Size Check
        long sizeInBytes;

        if (asset.Type == AssetType.Image)
        {
            // Base64 size calculation: (n * 3) / 4 - padding
            var base64 = asset.Value;
            int padding = base64.EndsWith("==") ? 2 : (base64.EndsWith("=") ? 1 : 0);
            sizeInBytes = (long)base64.Length * 3 / 4 - padding;
        }
        else
        {
            sizeInBytes = Encoding.UTF8.GetByteCount(asset.Value);
        }

        if (sizeInBytes > maxSize)
        {
            double mb = Math.Round((double)maxSize / 1024 / 1024, 2);
            return (false, $"{fieldName} payload too large. Maximum allowed is {mb}MB.");
        }

        return (true, null);
    }
}