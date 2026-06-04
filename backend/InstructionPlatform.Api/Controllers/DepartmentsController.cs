using InstructionPlatform.Api.Data;
using InstructionPlatform.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstructionPlatform.Api.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<DepartmentDto>>> GetAll()
    {
        var departments = await db.Departments
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new DepartmentDto(x.Id, x.Name))
            .ToListAsync();

        return Ok(departments);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost]
    public async Task<ActionResult<DepartmentDto>> Create([FromBody] CreateDepartmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Название отдела не может быть пустым.");
        }

        var departmentExists = await db.Departments
            .AnyAsync(x => x.Name == request.Name);

        if (departmentExists)
        {
            return BadRequest("Отдел с таким названием уже существует.");
        }

        var department = new Domain.Entities.Department
        {
            Name = request.Name,
            CreatedAt = DateTime.UtcNow
        };

        db.Departments.Add(department);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new DepartmentDto(department.Id, department.Name));
    }
}
