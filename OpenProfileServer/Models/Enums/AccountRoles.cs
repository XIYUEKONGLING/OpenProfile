namespace OpenProfileServer.Models.Enums;

public static class AccountRoles
{
    public const string Root = nameof(AccountRole.Root);
    public const string Admin = nameof(AccountRole.Admin);
    public const string User = nameof(AccountRole.User);
    
    public const string AdminOrHigher = $"{Root},{Admin}";
    public const string All = $"{Root},{Admin},{User}";
    
    public static string FromRoles(params AccountRole[] roles)
    {
        return string.Join(",", roles.Select(r => r.ToString()));
    }
}