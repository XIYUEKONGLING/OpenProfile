using OpenProfileServer.Models.Entities;

namespace OpenProfileServer.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(Account account);
    string GenerateRefreshToken();
    DateTime GetAccessTokenExpiration();
}