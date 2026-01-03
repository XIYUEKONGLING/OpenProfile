using OpenProfileServer.Models.DTOs.Auth;
using OpenProfileServer.Models.DTOs.Common;

namespace OpenProfileServer.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<TokenResponseDto>> RegisterAsync(RegisterRequestDto dto);
    Task<ApiResponse<TokenResponseDto>> LoginAsync(LoginRequestDto dto, string? deviceInfo);
    Task<ApiResponse<TokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto);
    Task<ApiResponse<MessageResponse>> LogoutAsync(string refreshToken);
    Task<ApiResponse<MessageResponse>> LogoutAllDevicesAsync(Guid accountId);
}