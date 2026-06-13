using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InstructionPlatform.Api.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace InstructionPlatform.Api.Services;

public class JwtTokenService(IConfiguration configuration)
{
    public string CreateToken(Employee user)
    {
        var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing.");
        var expiresHours = configuration.GetValue("Jwt:ExpiresHours", 12);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("employeeId", user.Id.ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiresHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
