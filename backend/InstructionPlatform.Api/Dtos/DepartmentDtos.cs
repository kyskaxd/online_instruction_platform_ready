namespace InstructionPlatform.Api.Dtos;

public record DepartmentDto(
    int Id,
    string Name);

public record CreateDepartmentRequest(
    string Name);
