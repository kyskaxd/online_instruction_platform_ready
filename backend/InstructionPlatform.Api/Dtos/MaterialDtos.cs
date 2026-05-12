namespace InstructionPlatform.Api.Dtos;

public record TrainingMaterialDto(
    int Id,
    string Title,
    string? Description,
    string OriginalFileName,
    long FileSize,
    DateTime UploadedAt,
    int UploadedByUserId);

public class UploadTrainingMaterialRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IFormFile File { get; set; } = default!;
}
