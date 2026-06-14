using InstructionPlatform.Api.Domain.Enums;

namespace InstructionPlatform.Api.Dtos;

public record TestResultReportDto(
    int AssignmentId,
    int EmployeeId,
    string EmployeeFullName,
    string EmployeeEmail,
    int InstructorUserId,
    string InstructorFullName,
    string InstructorEmail,
    string Department,
    string Position,
    int TestId,
    string TestTitle,
    InstructionCategory Category,
    InstructionType InstructionType,
    string Status,
    int? ScorePercent,
    bool? IsPassed,
    DateTime AssignedAt,
    DateTime? Deadline,
    DateTime? CompletedAt,
    DateTime? NextRetrainingDueAt);
