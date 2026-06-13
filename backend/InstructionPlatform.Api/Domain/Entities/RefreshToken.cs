using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InstructionPlatform.Api.Domain.Entities;

[Table("refresh_tokens")]
[Index(nameof(TokenHash), IsUnique = true)]
public class RefreshToken
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    [Required]
    [MaxLength(128)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }

    [MaxLength(128)]
    public string? ReplacedByTokenHash { get; set; }
}
