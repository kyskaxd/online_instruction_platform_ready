using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InstructionPlatform.Api.Domain.Entities;

[Table("tests")]
public class Test
{
    public int Id { get; set; }

    [Required]
    [MaxLength(250)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }
    public int PassingScorePercent { get; set; } = 80;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? TrainingMaterialId { get; set; }
    public TrainingMaterial? TrainingMaterial { get; set; }

    public int CreatedByUserId { get; set; }
    public Employee? CreatedByUser { get; set; }

    public ICollection<TestQuestion> Questions { get; set; } = new List<TestQuestion>();
    public ICollection<TestAssignment> Assignments { get; set; } = new List<TestAssignment>();
}
