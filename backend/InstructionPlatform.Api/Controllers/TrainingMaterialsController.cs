using InstructionPlatform.Api.Data;
using InstructionPlatform.Api.Domain.Entities;
using InstructionPlatform.Api.Dtos;
using InstructionPlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstructionPlatform.Api.Controllers;

[ApiController]
[Route("api/training-materials")]
[Authorize]
public class TrainingMaterialsController(AppDbContext db, IConfiguration configuration, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TrainingMaterialDto>>> GetAll()
    {
        var materials = await db.TrainingMaterials
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.UploadedAt)
            .Select(x => new TrainingMaterialDto(
                x.Id,
                x.Title,
                x.Description,
                x.OriginalFileName,
                x.FileSize,
                x.UploadedAt,
                x.UploadedByUserId))
            .ToListAsync();

        return Ok(materials);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<TrainingMaterialDto>> Upload([FromForm] UploadTrainingMaterialRequest request)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest("Выберите PDF-файл.");
        }

        var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
        if (extension != ".pdf")
        {
            return BadRequest("Можно загружать только PDF-файлы.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest("Название материала обязательно.");
        }

        var uploadsFolder = GetUploadsFolder();
        Directory.CreateDirectory(uploadsFolder);

        var storedFileName = $"{Guid.NewGuid():N}.pdf";
        var filePath = Path.Combine(uploadsFolder, storedFileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await request.File.CopyToAsync(stream);
        }

        var material = new TrainingMaterial
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            OriginalFileName = request.File.FileName,
            StoredFileName = storedFileName,
            ContentType = "application/pdf",
            FileSize = request.File.Length,
            UploadedByUserId = User.GetUserId(),
            UploadedAt = DateTime.UtcNow,
            IsActive = true
        };

        db.TrainingMaterials.Add(material);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetFile), new { id = material.Id }, new TrainingMaterialDto(
            material.Id,
            material.Title,
            material.Description,
            material.OriginalFileName,
            material.FileSize,
            material.UploadedAt,
            material.UploadedByUserId));
    }

    [HttpGet("{id:int}/file")]
    public async Task<IActionResult> GetFile(int id)
    {
        var material = await db.TrainingMaterials.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (material is null)
        {
            return NotFound();
        }

        var path = Path.Combine(GetUploadsFolder(), material.StoredFileName);
        if (!System.IO.File.Exists(path))
        {
            return NotFound("Файл не найден на сервере.");
        }

        var bytes = await System.IO.File.ReadAllBytesAsync(path);
        return File(bytes, "application/pdf", material.OriginalFileName);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var material = await db.TrainingMaterials.FirstOrDefaultAsync(x => x.Id == id);
        if (material is null)
        {
            return NotFound();
        }

        material.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private string GetUploadsFolder()
    {
        var relativePath = configuration["FileStorage:UploadsPath"] ?? "uploads";
        return Path.Combine(environment.ContentRootPath, relativePath);
    }
}
