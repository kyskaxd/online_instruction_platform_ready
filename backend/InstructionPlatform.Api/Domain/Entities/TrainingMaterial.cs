using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InstructionPlatform.Api.Domain.Enums;

namespace InstructionPlatform.Api.Domain.Entities;

[Table("training_materials")]
public class TrainingMaterial
{
    public int Id { get; set; }

    [Required]
    [MaxLength(250)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public InstructionCategory Category { get; set; } = InstructionCategory.OccupationalSafety;
    public InstructionType InstructionType { get; set; } = InstructionType.Repeated;
    public int RetrainingIntervalMonths { get; set; } = 12;

    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string StoredFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = "application/pdf";
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public int UploadedByUserId { get; set; }
    public Employee? UploadedByUser { get; set; }

    public ICollection<Test> Tests { get; set; } = new List<Test>();
    public ICollection<MaterialStudyRecord> StudyRecords { get; set; } = new List<MaterialStudyRecord>();
}
