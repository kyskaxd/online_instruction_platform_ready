using InstructionPlatform.Api.Data;
using InstructionPlatform.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstructionPlatform.Api.Controllers;

[ApiController]
[Route("api/positions")]
[Authorize]
public class PositionsController(AppDbContext db) : ControllerBase
{
    [HttpGet("by-department/{departmentId:int}")]
    public async Task<ActionResult<List<PositionDto>>> GetByDepartment(int departmentId)
    {
        var positions = await db.Positions
            .AsNoTracking()
            .Where(x => x.DepartmentId == departmentId)
            .OrderBy(x => x.Name)
            .Select(x => new PositionDto(x.Id, x.Name, x.DepartmentId))
            .ToListAsync();

        return Ok(positions);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<PositionDto>> Create([FromBody] CreatePositionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Название должности не может быть пустым.");
        }

        var departmentExists = await db.Departments
            .AnyAsync(x => x.Id == request.DepartmentId);

        if (!departmentExists)
        {
            return BadRequest("Отдел не найден.");
        }

        var positionExists = await db.Positions
            .AnyAsync(x => x.Name == request.Name && x.DepartmentId == request.DepartmentId);

        if (positionExists)
        {
            return BadRequest("Должность с таким названием уже существует в этом отделе.");
        }

        var position = new Domain.Entities.Position
        {
            Name = request.Name,
            DepartmentId = request.DepartmentId,
            CreatedAt = DateTime.UtcNow
        };

        db.Positions.Add(position);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByDepartment), new { departmentId = position.DepartmentId }, 
            new PositionDto(position.Id, position.Name, position.DepartmentId));
    }
}
