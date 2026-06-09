using InstructionPlatform.Api.Domain.Enums;

namespace InstructionPlatform.Api.Dtos;

public record MaterialStudyStatusDto(
    int TrainingMaterialId,
    bool HasViewed,
    bool IsAcknowledged,
    DateTime? FirstViewedAt,
    DateTime? AcknowledgedAt);

public record TrainingMaterialDto(
    int Id,
    string Title,
    string? Description,
    InstructionCategory Category,
    InstructionType InstructionType,
    int RetrainingIntervalMonths,
    string OriginalFileName,
    long FileSize,
    DateTime UploadedAt,
    int UploadedByUserId,
    MaterialStudyStatusDto? StudyStatus);

public class UploadTrainingMaterialRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public InstructionCategory Category { get; set; } = InstructionCategory.OccupationalSafety;
    public InstructionType InstructionType { get; set; } = InstructionType.Repeated;
    public int RetrainingIntervalMonths { get; set; } = 12;
    public IFormFile File { get; set; } = default!;
}
