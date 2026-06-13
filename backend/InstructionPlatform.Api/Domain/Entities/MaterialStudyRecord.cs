using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InstructionPlatform.Api.Domain.Entities;

[Table("material_study_records")]
[Index(nameof(EmployeeId), nameof(TrainingMaterialId), IsUnique = true)]
public class MaterialStudyRecord
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public int TrainingMaterialId { get; set; }
    public TrainingMaterial? TrainingMaterial { get; set; }

    public DateTime FirstViewedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastViewedAt { get; set; } = DateTime.UtcNow;
    public int ViewCount { get; set; } = 1;
    public DateTime? AcknowledgedAt { get; set; }
}
