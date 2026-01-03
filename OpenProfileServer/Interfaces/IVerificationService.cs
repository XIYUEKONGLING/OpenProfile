using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Interfaces;

public interface IVerificationService
{
    /// <summary>
    /// Generates a 9-char uppercase alphanumeric code, stores it, and sends via Email.
    /// Returns false if Email Service is disabled or sending failed.
    /// </summary>
    Task<bool> SendCodeAsync(string email, VerificationType type, string? username = null);

    /// <summary>
    /// Checks if the code is valid. If valid, deletes it (consume).
    /// </summary>
    Task<bool> ValidateCodeAsync(string email, VerificationType type, string code);
}