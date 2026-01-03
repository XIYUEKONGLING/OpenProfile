namespace OpenProfileServer.Constants;

public static class SystemSettingKeys
{
    // Policies (Runtime Logic)
    public const string MaintenanceMode = "MaintenanceMode";
    public const string AllowRegistration = "AllowRegistration";
    public const string RegistrationRequiresEmail = "RegistrationRequiresEmail"; 
    
    public const string AllowSearchEngineIndexing = "AllowSearchEngineIndexing";
    public const string RequireEmailVerification = "RequireEmailVerification";
    
    // Limits
    public const string DefaultStorageLimit = "DefaultStorageLimit";
    
    // Email Templates
    public const string EmailVerificationSubject = "Email:Template:Verification:Subject";
    public const string EmailVerificationBody = "Email:Template:Verification:Body";
    public const string EmailPasswordResetSubject = "Email:Template:PasswordReset:Subject";
    public const string EmailPasswordResetBody = "Email:Template:PasswordReset:Body";
}