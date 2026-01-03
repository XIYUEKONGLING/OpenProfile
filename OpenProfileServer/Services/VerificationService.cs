using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using OpenProfileServer.Data;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.Entities.Auth;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Services;

public class VerificationService : IVerificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    
    // 9 characters, Uppercase + Digits
    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int CodeLength = 9;
    private const int ExpirationMinutes = 15;

    public VerificationService(ApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<bool> GenerateAndSendCodeAsync(string identifier, VerificationType type, string? username = null)
    {
        // 1. Generate Code
        var code = GenerateCode();

        // 2. Store in DB
        // Remove existing codes for this identifier/type to prevent spam/clutter
        var existing = await _context.VerificationCodes
            .Where(v => v.Identifier == identifier && v.Type == type)
            .ToListAsync();
        
        if (existing.Any())
        {
            _context.VerificationCodes.RemoveRange(existing);
        }

        var entity = new VerificationCode
        {
            Identifier = identifier,
            Code = code,
            Type = type,
            ExpiresAt = DateTime.UtcNow.AddMinutes(ExpirationMinutes),
            CreatedAt = DateTime.UtcNow
        };

        _context.VerificationCodes.Add(entity);
        await _context.SaveChangesAsync();

        // 3. Send Email (if enabled)
        if (_emailService.IsEnabled)
        {
            // Note: In a real scenario, we might want to customize the template based on VerificationType.
            // For now, we use the generic verification template or assume the EmailService handles it.
            return await _emailService.SendVerificationEmailAsync(identifier, username ?? "User", code);
        }

        // If email is disabled, we still return true as the code was generated successfully.
        // In dev mode, one might check the DB.
        return true;
    }

    public async Task<bool> ValidateCodeAsync(string identifier, VerificationType type, string code)
    {
        var entity = await _context.VerificationCodes
            .FirstOrDefaultAsync(v => v.Identifier == identifier && v.Type == type && v.Code == code);

        if (entity == null) return false;

        if (DateTime.UtcNow > entity.ExpiresAt)
        {
            _context.VerificationCodes.Remove(entity);
            await _context.SaveChangesAsync();
            return false;
        }

        // Valid code - consume it
        _context.VerificationCodes.Remove(entity);
        await _context.SaveChangesAsync();

        return true;
    }

    private static string GenerateCode()
    {
        return string.Create(CodeLength, Chars, (span, chars) =>
        {
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
            }
        });
    }
}
