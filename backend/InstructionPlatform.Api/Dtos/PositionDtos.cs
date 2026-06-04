namespace InstructionPlatform.Api.Dtos;

public record PositionDto(
    int Id,
    string Name,
    int DepartmentId);

public record CreatePositionRequest(
    string Name,
    int DepartmentId);
