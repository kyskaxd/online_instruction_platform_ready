using InstructionPlatform.Api.Data;
using InstructionPlatform.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InstructionPlatform.Api.Services;

/// <summary>
/// Сервис для валидации идентичности категории и вида инструктажа между тестами и материалами
/// </summary>
public class InstructionValidationService(AppDbContext db)
{
    /// <summary>
    /// Проверяет, что категория и вид инструктажа теста совпадают с материалом
    /// </summary>
    /// <param name="trainingMaterialId">ID материала (может быть null)</param>
    /// <param name="testCategory">Категория теста</param>
    /// <param name="testInstructionType">Вид инструктажа теста</param>
    /// <returns>Сообщение об ошибке или null если валидация прошла</returns>
    public async Task<string?> ValidateTestInstructionMatch(
        int? trainingMaterialId,
        InstructionCategory testCategory,
        InstructionType testInstructionType)
    {
        if (!trainingMaterialId.HasValue)
        {
            return null; // Если материал не привязан, валидация не требуется
        }

        var material = await db.TrainingMaterials
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == trainingMaterialId.Value && x.IsActive);

        if (material is null)
        {
            return "Обучающий материал не найден.";
        }

        var errors = new List<string>();

        if (material.Category != testCategory)
        {
            errors.Add($"Категория теста ({testCategory}) должна совпадать с категорией материала ({material.Category}).");
        }

        if (material.InstructionType != testInstructionType)
        {
            errors.Add($"Вид инструктажа теста ({testInstructionType}) должен совпадать с видом инструктажа материала ({material.InstructionType}).");
        }

        return errors.Count > 0 ? string.Join(" ", errors) : null;
    }

    /// <summary>
    /// Проверяет, что категория и вид инструктажа материала согласованы с привязанными тестами
    /// </summary>
    /// <param name="trainingMaterialId">ID материала</param>
    /// <param name="materialCategory">Категория материала</param>
    /// <param name="materialInstructionType">Вид инструктажа материала</param>
    /// <returns>Сообщение об ошибке или null если валидация прошла</returns>
    public async Task<string?> ValidateMaterialInstructionMatch(
        int trainingMaterialId,
        InstructionCategory materialCategory,
        InstructionType materialInstructionType)
    {
        var conflictingTests = await db.Tests
            .AsNoTracking()
            .Where(x => x.TrainingMaterialId == trainingMaterialId 
                     && x.IsActive
                     && (x.Category != materialCategory || x.InstructionType != materialInstructionType))
            .Select(x => new { x.Id, x.Category, x.InstructionType })
            .ToListAsync();

        if (conflictingTests.Count == 0)
        {
            return null; // Конфликтов нет
        }

        var conflictDetails = string.Join(", ", conflictingTests.Select(t =>
            $"Тест #{t.Id} ({t.Category}/{t.InstructionType})"));

        return $"Материал невозможно изменить. Привязанные тесты имеют конфликтующие категории/виды: {conflictDetails}";
    }
}
