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

    public async Task<bool> SendCodeAsync(string email, VerificationType type, string? username = null)
    {
        // 0. If Email Service is down/disabled, we physically cannot verify ownership.
        // The controller should handle whether to allow bypass or fail based on config.
        if (!_emailService.IsEnabled) return false;

        // 1. Generate Code
        var code = GenerateCode();

        // 2. Store in DB (Cleanup old codes for same email/type)
        var existing = await _context.VerificationCodes
            .Where(v => v.Identifier == email && v.Type == type)
            .ToListAsync();
        
        if (existing.Any())
        {
            _context.VerificationCodes.RemoveRange(existing);
        }

        var entity = new VerificationCode
        {
            Identifier = email,
            Code = code,
            Type = type,
            ExpiresAt = DateTime.UtcNow.AddMinutes(ExpirationMinutes),
            CreatedAt = DateTime.UtcNow
        };

        _context.VerificationCodes.Add(entity);
        await _context.SaveChangesAsync();

        // 3. Send Email
        return await _emailService.SendVerificationEmailAsync(email, username ?? "User", code);
    }

    public async Task<bool> ValidateCodeAsync(string email, VerificationType type, string code)
    {
        // Normalize input
        var normalizedCode = code.Trim().ToUpperInvariant();
        
        var entity = await _context.VerificationCodes
            .FirstOrDefaultAsync(v => v.Identifier == email && v.Type == type && v.Code == normalizedCode);

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
