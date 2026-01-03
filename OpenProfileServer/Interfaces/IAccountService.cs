using OpenProfileServer.Models.DTOs.Account;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Profile;
using OpenProfileServer.Models.DTOs.Settings;

namespace OpenProfileServer.Interfaces;

public interface IAccountService
{
    Task<ApiResponse<AccountDto>> GetMyAccountAsync(Guid accountId);
    Task<ApiResponse<AccountPermissionsDto>> GetMyPermissionsAsync(Guid accountId);
    
    Task<ApiResponse<PersonalSettingsDto>> GetMySettingsAsync(Guid accountId);
    Task<ApiResponse<MessageResponse>> UpdateMySettingsAsync(Guid accountId, UpdatePersonalSettingsRequestDto dto);

    Task<ApiResponse<ProfileDto>> GetMyProfileAsync(Guid accountId);
    Task<ApiResponse<MessageResponse>> UpdateMyProfileAsync(Guid accountId, UpdateProfileRequestDto dto);
    
    Task<ApiResponse<MessageResponse>> ChangePasswordAsync(Guid accountId, ChangePasswordRequestDto dto);
    Task<ApiResponse<MessageResponse>> RequestDeletionAsync(Guid accountId);
    Task<ApiResponse<MessageResponse>> RestoreAccountAsync(Guid accountId);
}