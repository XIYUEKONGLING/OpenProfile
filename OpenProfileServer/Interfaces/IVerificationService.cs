using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Interfaces;

public interface IVerificationService
{
    /// <summary>
    /// Generates a 9-character alphanumeric code, stores it, and attempts to send it via email.
    /// </summary>
    Task<bool> GenerateAndSendCodeAsync(string identifier, VerificationType type, string? username = null);

    /// <summary>
    /// Validates a code. If valid, deletes the code to prevent reuse.
    /// </summary>
    Task<bool> ValidateCodeAsync(string identifier, VerificationType type, string code);
}