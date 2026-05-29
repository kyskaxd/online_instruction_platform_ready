using InstructionPlatform.Api.Domain.Enums;

namespace InstructionPlatform.Api.Dtos;

public record LoginRequest(string Email, string Password);

public record RegisterUserRequest(
    string LastName,
    string FirstName,
    string? MiddleName,
    int DepartmentId,
    string Position,
    string Email,
    string Password,
    UserRole Role,
    DateTime? HireDate);

public record AuthResponse(
    int UserId,
    string Email,
    UserRole Role,
    int? EmployeeId);

public record CurrentUserResponse(
    int UserId,
    string Email,
    UserRole Role,
    int? EmployeeId);
