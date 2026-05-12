using InstructionPlatform.Api.Data;
using InstructionPlatform.Api.Domain.Entities;
using InstructionPlatform.Api.Domain.Enums;
using InstructionPlatform.Api.Dtos;
using InstructionPlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstructionPlatform.Api.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeesController(AppDbContext db, PasswordHashService passwordHashService) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<List<EmployeeDto>>> GetAll()
    {
        var employees = await db.Employees
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Department)
            .ThenBy(x => x.Position)
            .ThenBy(x => x.LastName)
            .Select(x => ToDto(x))
            .ToListAsync();

        return Ok(employees);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> GetById(int id)
    {
        var employee = await db.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        return employee is null ? NotFound() : Ok(ToDto(employee));
    }

    [HttpGet("me")]
    public async Task<ActionResult<EmployeeDto>> GetMe()
    {
        var employeeId = User.GetEmployeeId();
        if (employeeId is null)
        {
            return BadRequest("У пользователя не найден профиль сотрудника.");
        }

        var employee = await db.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == employeeId.Value && x.IsActive);
        return employee is null ? NotFound() : Ok(ToDto(employee));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<EmployeeDto>> Create(CreateEmployeeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LastName) || string.IsNullOrWhiteSpace(request.FirstName))
        {
            return BadRequest("Фамилия и имя обязательны.");
        }

        if (string.IsNullOrWhiteSpace(request.Department) || string.IsNullOrWhiteSpace(request.Position))
        {
            return BadRequest("Отдел и должность обязательны.");
        }

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Email и пароль обязательны.");
        }

        if (!User.IsInRole("Admin") && request.Role != UserRole.Employee)
        {
            return Forbid();
        }

        var emailExists = await db.Employees.AnyAsync(x => x.Email.ToLower() == request.Email.ToLower());
        if (emailExists)
        {
            return Conflict("Сотрудник с таким email уже существует.");
        }

        var employee = new Employee
        {
            LastName = request.LastName.Trim(),
            FirstName = request.FirstName.Trim(),
            MiddleName = request.MiddleName?.Trim(),
            Department = request.Department.Trim(),
            Position = request.Position.Trim(),
            Email = request.Email.Trim(),
            PasswordHash = passwordHashService.Hash(request.Password),
            Role = request.Role == 0 ? UserRole.Employee : request.Role,
            IsActive = true,
            HireDate = ToUtc(request.HireDate)
        };

        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, ToDto(employee));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = User.GetUserId();
        if (id == currentUserId)
        {
            return BadRequest("Нельзя удалить текущего пользователя.");
        }

        var employee = await db.Employees.FirstOrDefaultAsync(x => x.Id == id);
        if (employee is null)
        {
            return NotFound();
        }

        await using var transaction = await db.Database.BeginTransactionAsync();

        var attemptIds = await db.TestAttempts
            .Where(x => x.EmployeeId == id)
            .Select(x => x.Id)
            .ToListAsync();

        var attemptAnswers = await db.TestAttemptAnswers
            .Where(x => attemptIds.Contains(x.TestAttemptId))
            .ToListAsync();
        db.TestAttemptAnswers.RemoveRange(attemptAnswers);

        var attempts = await db.TestAttempts
            .Where(x => x.EmployeeId == id)
            .ToListAsync();
        db.TestAttempts.RemoveRange(attempts);

        var assignments = await db.TestAssignments
            .Where(x => x.EmployeeId == id)
            .ToListAsync();
        db.TestAssignments.RemoveRange(assignments);

        var assignedByEmployee = await db.TestAssignments
            .Where(x => x.AssignedByUserId == id)
            .ToListAsync();
        foreach (var assignment in assignedByEmployee)
        {
            assignment.AssignedByUserId = currentUserId;
        }

        var createdTests = await db.Tests
            .Where(x => x.CreatedByUserId == id)
            .ToListAsync();
        foreach (var test in createdTests)
        {
            test.CreatedByUserId = currentUserId;
        }

        var uploadedMaterials = await db.TrainingMaterials
            .Where(x => x.UploadedByUserId == id)
            .ToListAsync();
        foreach (var material in uploadedMaterials)
        {
            material.UploadedByUserId = currentUserId;
        }

        db.Employees.Remove(employee);

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return NoContent();
    }

    private static DateTime? ToUtc(DateTime? value)
    {
        if (value is null)
        {
            return null;
        }

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };
    }

    private static EmployeeDto ToDto(Employee x) => new(
        x.Id,
        x.LastName,
        x.FirstName,
        x.MiddleName,
        x.Department,
        x.Position,
        x.Email,
        x.HireDate,
        x.Role,
        x.IsActive);
}
