using OpenProfileServer.Models.DTOs.Assets;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Interfaces;

public interface IAssetService
{
    // ==========================================
    // Public Asset Library (No Auth)
    // ==========================================

    /// <summary>
    /// Get public asset by UUID (for static generator and public access).
    /// Returns 404 if asset is private, deleted, or belongs to suspended/banned account.
    /// </summary>
    Task<ApiResponse<AccountAssetDto>> GetPublicAssetAsync(Guid uuid);

    // ==========================================
    // Personal Asset Management
    // ==========================================

    /// <summary>
    /// List my assets (paginated).
    /// </summary>
    Task<ApiResponse<PagedResponse<AccountAssetDto>>> GetMyAssetsAsync(
        Guid accountId,
        int page,
        int pageSize,
        string? category,
        Visibility? visibility,
        string? search);

    /// <summary>
    /// Get specific asset (my own).
    /// </summary>
    Task<ApiResponse<AccountAssetDto>> GetMyAssetAsync(Guid accountId, Guid uuid);

    /// <summary>
    /// Create new asset.
    /// </summary>
    Task<ApiResponse<AccountAssetDto>> CreateMyAssetAsync(Guid accountId, CreateAccountAssetRequestDto dto);

    /// <summary>
    /// Full update asset.
    /// </summary>
    Task<ApiResponse<AccountAssetDto>> UpdateMyAssetAsync(Guid accountId, Guid uuid, CreateAccountAssetRequestDto dto);

    /// <summary>
    /// Partial update asset.
    /// </summary>
    Task<ApiResponse<AccountAssetDto>> PatchMyAssetAsync(Guid accountId, Guid uuid, UpdateAccountAssetRequestDto dto);

    /// <summary>
    /// Delete asset.
    /// </summary>
    Task<ApiResponse<MessageResponse>> DeleteMyAssetAsync(Guid accountId, Guid uuid);

    /// <summary>
    /// Batch update visibility.
    /// </summary>
    Task<ApiResponse<MessageResponse>> BatchUpdateMyAssetVisibilityAsync(Guid accountId, BatchUpdateVisibilityRequestDto dto);

    /// <summary>
    /// Batch delete assets.
    /// </summary>
    Task<ApiResponse<MessageResponse>> BatchDeleteMyAssetsAsync(Guid accountId, BatchDeleteRequestDto dto);

    /// <summary>
    /// List all my categories (distinct).
    /// </summary>
    Task<ApiResponse<IEnumerable<string>>> GetMyAssetCategoriesAsync(Guid accountId);

    // ==========================================
    // System Asset Management (Admin Only)
    // ==========================================

    /// <summary>
    /// List all system assets (paginated).
    /// </summary>
    Task<ApiResponse<PagedResponse<SystemAssetDto>>> GetSystemAssetsAsync(
        int page,
        int pageSize,
        string? category,
        Visibility? visibility);

    /// <summary>
    /// Get specific system asset.
    /// </summary>
    Task<ApiResponse<SystemAssetDto>> GetSystemAssetAsync(Guid uuid);

    /// <summary>
    /// Create system asset.
    /// </summary>
    Task<ApiResponse<SystemAssetDto>> CreateSystemAssetAsync(CreateSystemAssetRequestDto dto);

    /// <summary>
    /// Full update system asset.
    /// </summary>
    Task<ApiResponse<SystemAssetDto>> UpdateSystemAssetAsync(Guid uuid, CreateSystemAssetRequestDto dto);

    /// <summary>
    /// Partial update system asset.
    /// </summary>
    Task<ApiResponse<SystemAssetDto>> PatchSystemAssetAsync(Guid uuid, UpdateSystemAssetRequestDto dto);

    /// <summary>
    /// Delete system asset.
    /// </summary>
    Task<ApiResponse<MessageResponse>> DeleteSystemAssetAsync(Guid uuid);
}
