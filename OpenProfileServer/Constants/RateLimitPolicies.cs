namespace OpenProfileServer.Constants;

public static class RateLimitPolicies
{
    /// <summary>
    /// Standard limit for general API usage.
    /// </summary>
    public const string General = "general";

    /// <summary>
    /// Strict limit for login attempts to prevent brute force.
    /// </summary>
    public const string Login = "login";

    /// <summary>
    /// Limit for account registration to prevent mass account creation.
    /// </summary>
    public const string Register = "register";

    /// <summary>
    /// Limit for email-related actions (verification, password reset) to prevent spam.
    /// </summary>
    public const string Email = "email";
}