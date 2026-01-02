namespace OpenProfileServer.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "OpenProfileServer";
    public string Audience { get; set; } = "OpenProfileClient";
    
    public int AccessTokenExpirationMinutes { get; set; } = 10;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}