using InstructionPlatform.Api.Domain.Enums;

namespace InstructionPlatform.Api.Dtos;

public record EmployeeDto(
    int Id,
    string LastName,
    string FirstName,
    string? MiddleName,
    string Department,
    string Position,
    string Email,
    DateTime? HireDate,
    UserRole Role,
    bool IsActive);

public record CreateEmployeeRequest(
    string LastName,
    string FirstName,
    string? MiddleName,
    string Department,
    string Position,
    string Email,
    DateTime? HireDate,
    string? Password,
    UserRole Role);
