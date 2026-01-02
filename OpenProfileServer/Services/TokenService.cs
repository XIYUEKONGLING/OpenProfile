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

        // Include SecurityStamp in claims to allow immediate invalidation
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Name, account.AccountName),
            new(ClaimTypes.Role, account.Role.ToString()),
            new("SecurityStamp", account.Credential?.SecurityStamp ?? string.Empty) 
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = GetAccessTokenExpiration(),
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken() => 
        Guid.NewGuid().ToString("N") + Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(128));

    public DateTime GetAccessTokenExpiration() => 
        DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes);
}
