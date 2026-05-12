using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InstructionPlatform.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InstructionPlatform.Api.Domain.Entities;

[Table("employees")]
[Index(nameof(Email), IsUnique = true)]
public class Employee
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? MiddleName { get; set; }

    [Required]
    [MaxLength(150)]
    public string Department { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Position { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Employee;
    public bool IsActive { get; set; } = true;
    public DateTime? HireDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TrainingMaterial> UploadedTrainingMaterials { get; set; } = new List<TrainingMaterial>();
    public ICollection<Test> CreatedTests { get; set; } = new List<Test>();
    public ICollection<TestAssignment> AssignedTestAssignments { get; set; } = new List<TestAssignment>();
    public ICollection<TestAssignment> TestAssignments { get; set; } = new List<TestAssignment>();
    public ICollection<TestAttempt> TestAttempts { get; set; } = new List<TestAttempt>();
}
