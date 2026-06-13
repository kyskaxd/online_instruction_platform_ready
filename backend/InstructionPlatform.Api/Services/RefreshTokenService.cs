using System.Security.Cryptography;
using System.Text;
using InstructionPlatform.Api.Data;
using InstructionPlatform.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InstructionPlatform.Api.Services;

public class RefreshTokenService(AppDbContext db, IConfiguration configuration)
{
    public async Task<(string RawToken, RefreshToken Entity)> CreateAsync(int employeeId)
    {
        var rawToken = GenerateRawToken();
        var entity = new RefreshToken
        {
            EmployeeId = employeeId,
            TokenHash = Hash(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(configuration.GetValue("Jwt:RefreshTokenExpiresDays", 30))
        };

        db.RefreshTokens.Add(entity);
        await db.SaveChangesAsync();

        return (rawToken, entity);
    }

    public async Task<RefreshToken?> ValidateAsync(string rawToken)
    {
        var hash = Hash(rawToken);
        var token = await db.RefreshTokens
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.TokenHash == hash);

        if (token is null || token.RevokedAt.HasValue || token.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        if (token.Employee is null || !token.Employee.IsActive)
        {
            return null;
        }

        return token;
    }

    public async Task RevokeAsync(RefreshToken token, string? replacedByHash = null)
    {
        token.RevokedAt = DateTime.UtcNow;
        token.ReplacedByTokenHash = replacedByHash;
        await db.SaveChangesAsync();
    }

    public async Task RevokeAllForEmployeeAsync(int employeeId)
    {
        var tokens = await db.RefreshTokens
            .Where(x => x.EmployeeId == employeeId && x.RevokedAt == null)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var token in tokens)
        {
            token.RevokedAt = now;
        }

        await db.SaveChangesAsync();
    }

    private static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public static string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
