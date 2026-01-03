namespace OpenProfileServer.Models.Enums;

public enum VerificationType
{
    /// <summary>
    /// Used when a new user is signing up.
    /// </summary>
    Registration = 0,

    /// <summary>
    /// Used for Two-Factor Authentication during login.
    /// </summary>
    Login = 1,

    /// <summary>
    /// Used when a user forgets their password.
    /// </summary>
    ResetPassword = 2,

    /// <summary>
    /// Used to verify a new email address added to an account.
    /// </summary>
    VerifyEmail = 3,
    
    /// <summary>
    /// Used for sensitive actions like deleting an account.
    /// </summary>
    SudoMode = 4,
}