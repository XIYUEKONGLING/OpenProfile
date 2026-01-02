using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenProfileServer.Configuration;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.Entities;

namespace OpenProfileServer.Services;

public class TokenService : ITokenService
{
    private readonly SecurityOptions _securityOptions;
    private readonly JwtOptions _jwtOptions;

    public TokenService(IOptions<SecurityOptions> securityOptions, IOptions<JwtOptions> jwtOptions)
    {
        _securityOptions = securityOptions.Value;
        _jwtOptions = jwtOptions.Value;
    }

    public string GenerateAccessToken(Account account)
    {
        var key = Encoding.ASCII.GetBytes(_securityOptions.ApplicationSecret);
        var tokenHandler = new JwtSecurityTokenHandler();
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Name, account.AccountName),
            new(ClaimTypes.Role, account.Role.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = GetAccessTokenExpiration(),
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        // Simple secure random string, or JWT if claims are needed in refresh token
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + "_" + Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    }

    public DateTime GetAccessTokenExpiration()
    {
        return DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes);
    }
}
