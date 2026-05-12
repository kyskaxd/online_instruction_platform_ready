namespace InstructionPlatform.Api.Dtos;

public record TestResultReportDto(
    int AssignmentId,
    int EmployeeId,
    string EmployeeFullName,
    string Department,
    string Position,
    int TestId,
    string TestTitle,
    string Status,
    int? ScorePercent,
    bool? IsPassed,
    DateTime AssignedAt,
    DateTime? Deadline,
    DateTime? CompletedAt);
