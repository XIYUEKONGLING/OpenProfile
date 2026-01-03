using OpenProfileServer.Models.DTOs.Account;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Profile;
using OpenProfileServer.Models.DTOs.Settings;
using OpenProfileServer.Models.DTOs.Social;

namespace OpenProfileServer.Interfaces;

public interface IAccountService
{
    Task<ApiResponse<AccountDto>> GetMyAccountAsync(Guid accountId);
    Task<ApiResponse<AccountPermissionsDto>> GetMyPermissionsAsync(Guid accountId);
    
    Task<ApiResponse<PersonalSettingsDto>> GetMySettingsAsync(Guid accountId);
    Task<ApiResponse<MessageResponse>> UpdateMySettingsAsync(Guid accountId, UpdatePersonalSettingsRequestDto dto);
    Task<ApiResponse<MessageResponse>> PatchMySettingsAsync(Guid accountId, UpdatePersonalSettingsRequestDto dto);

    Task<ApiResponse<ProfileDto>> GetMyProfileAsync(Guid accountId);
    Task<ApiResponse<MessageResponse>> UpdateMyProfileAsync(Guid accountId, UpdateProfileRequestDto dto);
    Task<ApiResponse<MessageResponse>> PatchMyProfileAsync(Guid accountId, UpdateProfileRequestDto dto);
    
    Task<ApiResponse<MessageResponse>> ChangePasswordAsync(Guid accountId, ChangePasswordRequestDto dto);
    Task<ApiResponse<MessageResponse>> RequestDeletionAsync(Guid accountId);
    Task<ApiResponse<MessageResponse>> RestoreAccountAsync(Guid accountId);

    // === Email Management ===
    Task<ApiResponse<IEnumerable<AccountEmailDto>>> GetEmailsAsync(Guid accountId);
    Task<ApiResponse<MessageResponse>> AddEmailAsync(Guid accountId, AddEmailRequestDto dto);
    Task<ApiResponse<MessageResponse>> SetPrimaryEmailAsync(Guid accountId, string email);
    Task<ApiResponse<MessageResponse>> DeleteEmailAsync(Guid accountId, string email);
    Task<ApiResponse<MessageResponse>> VerifyEmailAsync(Guid accountId, string email, VerifyEmailRequestDto dto);

    // === Block Management ===
    Task<ApiResponse<IEnumerable<BlockDto>>> GetMyBlockedUsersAsync(Guid accountId);
}
