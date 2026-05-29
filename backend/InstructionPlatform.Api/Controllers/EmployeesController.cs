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
    [Authorize(Roles = "Admin,HR")]
    [HttpGet]
    public async Task<ActionResult<List<EmployeeDto>>> GetAll(int? departmentId = null)
    {
        var employeesQuery = db.Employees.AsNoTracking()
            .Where(x => x.Role != UserRole.Admin);

        if (departmentId.HasValue)
        {
            employeesQuery = employeesQuery.Where(x => x.DepartmentId == departmentId.Value);
        }

        var employees = await employeesQuery
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Department)
            .ThenBy(x => x.Position)
            .ThenBy(x => x.LastName)
            .Select(x => ToDto(x))
            .ToListAsync();

        return Ok(employees);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet("lookup")]
    public async Task<ActionResult<List<EmployeeLookupDto>>> GetLookup()
    {
        var employees = await db.Employees
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Department)
            .ThenBy(x => x.LastName)
            .Select(x => new EmployeeLookupDto(
                x.Id,
                x.LastName,
                x.FirstName,
                x.MiddleName,
                x.Department,
                x.Position))
            .ToListAsync();

        return Ok(employees);
    }

    [Authorize(Roles = "Admin,HR")]
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

    [Authorize(Roles = "Admin,HR")]
    [HttpPost]
    public async Task<ActionResult<EmployeeDto>> Create(CreateEmployeeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LastName) || string.IsNullOrWhiteSpace(request.FirstName))
        {
            return BadRequest("Фамилия и имя обязательны.");
        }

        if (request.DepartmentId <= 0 || string.IsNullOrWhiteSpace(request.Position))
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

        if (request.Role == UserRole.Admin)
        {
            return BadRequest("Создание второго администратора запрещено.");
        }

        var department = await db.Departments.FindAsync(request.DepartmentId);
        if (department is null)
        {
            return BadRequest("Выбранный отдел не существует.");
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
            Department = department.Name,
            DepartmentId = department.Id,
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

    [Authorize(Roles = "Admin,HR")]
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

        if (employee.Role == UserRole.Admin)
        {
            return BadRequest("Нельзя удалить администратора.");
        }

        if (!employee.IsActive)
        {
            return BadRequest("Сотрудник уже неактивен.");
        }

        employee.IsActive = false;
        await db.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(x => x.Id == id);
        if (employee is null)
        {
            return NotFound();
        }

        if (employee.Role == UserRole.Admin)
        {
            return BadRequest("Нельзя изменять статус администратора.");
        }

        if (employee.IsActive)
        {
            return BadRequest("Сотрудник уже активен.");
        }

        employee.IsActive = true;
        await db.SaveChangesAsync();

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
