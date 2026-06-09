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
        var employeeId = User.GetEmployeeId();
        var studyRecords = employeeId.HasValue
            ? await db.MaterialStudyRecords
                .AsNoTracking()
                .Where(x => x.EmployeeId == employeeId.Value)
                .ToDictionaryAsync(x => x.TrainingMaterialId)
            : new Dictionary<int, MaterialStudyRecord>();

        var materials = await db.TrainingMaterials
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.UploadedAt)
            .Select(x => new TrainingMaterialDto(
                x.Id,
                x.Title,
                x.Description,
                x.Category,
                x.InstructionType,
                x.RetrainingIntervalMonths,
                x.OriginalFileName,
                x.FileSize,
                x.UploadedAt,
                x.UploadedByUserId,
                null))
            .ToListAsync();

        if (employeeId.HasValue)
        {
            materials = materials.Select(m =>
            {
                if (!studyRecords.TryGetValue(m.Id, out var record))
                {
                    return m with
                    {
                        StudyStatus = new MaterialStudyStatusDto(m.Id, false, false, null, null)
                    };
                }

                return m with
                {
                    StudyStatus = new MaterialStudyStatusDto(
                        m.Id,
                        true,
                        record.AcknowledgedAt.HasValue,
                        record.FirstViewedAt,
                        record.AcknowledgedAt)
                };
            }).ToList();
        }

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

        if (request.RetrainingIntervalMonths is < 1 or > 60)
        {
            return BadRequest("Срок повторного инструктажа должен быть от 1 до 60 месяцев.");
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
            Category = request.Category,
            InstructionType = request.InstructionType,
            RetrainingIntervalMonths = request.RetrainingIntervalMonths,
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
            material.Category,
            material.InstructionType,
            material.RetrainingIntervalMonths,
            material.OriginalFileName,
            material.FileSize,
            material.UploadedAt,
            material.UploadedByUserId,
            null));
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

    [Authorize(Roles = "Employee")]
    [HttpPost("{id:int}/view")]
    public async Task<ActionResult<MaterialStudyStatusDto>> RecordView(int id)
    {
        var employeeId = User.GetEmployeeId();
        if (employeeId is null)
        {
            return BadRequest("Профиль сотрудника не привязан к пользователю.");
        }

        var materialExists = await db.TrainingMaterials.AnyAsync(x => x.Id == id && x.IsActive);
        if (!materialExists)
        {
            return NotFound("Материал не найден.");
        }

        var record = await db.MaterialStudyRecords
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId.Value && x.TrainingMaterialId == id);

        var now = DateTime.UtcNow;
        if (record is null)
        {
            record = new MaterialStudyRecord
            {
                EmployeeId = employeeId.Value,
                TrainingMaterialId = id,
                FirstViewedAt = now,
                LastViewedAt = now,
                ViewCount = 1
            };
            db.MaterialStudyRecords.Add(record);
        }
        else
        {
            record.LastViewedAt = now;
            record.ViewCount++;
        }

        await db.SaveChangesAsync();

        return Ok(new MaterialStudyStatusDto(
            id,
            true,
            record.AcknowledgedAt.HasValue,
            record.FirstViewedAt,
            record.AcknowledgedAt));
    }

    [Authorize(Roles = "Employee")]
    [HttpPost("{id:int}/acknowledge")]
    public async Task<ActionResult<MaterialStudyStatusDto>> Acknowledge(int id)
    {
        var employeeId = User.GetEmployeeId();
        if (employeeId is null)
        {
            return BadRequest("Профиль сотрудника не привязан к пользователю.");
        }

        var materialExists = await db.TrainingMaterials.AnyAsync(x => x.Id == id && x.IsActive);
        if (!materialExists)
        {
            return NotFound("Материал не найден.");
        }

        var record = await db.MaterialStudyRecords
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId.Value && x.TrainingMaterialId == id);

        var now = DateTime.UtcNow;
        if (record is null)
        {
            record = new MaterialStudyRecord
            {
                EmployeeId = employeeId.Value,
                TrainingMaterialId = id,
                FirstViewedAt = now,
                LastViewedAt = now,
                ViewCount = 1,
                AcknowledgedAt = now
            };
            db.MaterialStudyRecords.Add(record);
        }
        else
        {
            record.AcknowledgedAt = now;
            record.LastViewedAt = now;
        }

        await db.SaveChangesAsync();

        return Ok(new MaterialStudyStatusDto(
            id,
            true,
            true,
            record.FirstViewedAt,
            record.AcknowledgedAt));
    }

    [HttpGet("{id:int}/study-status")]
    public async Task<ActionResult<MaterialStudyStatusDto>> GetStudyStatus(int id)
    {
        var employeeId = User.GetEmployeeId();
        if (employeeId is null)
        {
            return BadRequest("Профиль сотрудника не привязан к пользователю.");
        }

        var record = await db.MaterialStudyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId.Value && x.TrainingMaterialId == id);

        if (record is null)
        {
            return Ok(new MaterialStudyStatusDto(id, false, false, null, null));
        }

        return Ok(new MaterialStudyStatusDto(
            id,
            true,
            record.AcknowledgedAt.HasValue,
            record.FirstViewedAt,
            record.AcknowledgedAt));
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
