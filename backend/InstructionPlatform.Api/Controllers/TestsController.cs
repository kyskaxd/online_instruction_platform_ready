using System.Text.Json;
using System.Text.Json.Serialization;
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
[Route("api/tests")]
[Authorize]
public class TestsController(AppDbContext db) : ControllerBase
{
    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<ActionResult<List<TestListDto>>> GetAll()
    {
        var tests = await db.Tests
            .AsNoTracking()
            .Include(x => x.Questions)
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new TestListDto(
                x.Id,
                x.Title,
                x.Description,
                x.PassingScorePercent,
                x.Questions.Count,
                x.CreatedAt,
                x.TrainingMaterialId))
            .ToListAsync();

        return Ok(tests);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost("import-json")]
    public async Task<ActionResult<TestListDto>> ImportJson([FromBody] TestImportRequest request)
    {
        var validationError = await ValidateImportRequest(request);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var test = MapImportRequestToTest(request, User.GetUserId());
        db.Tests.Add(test);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = test.Id }, new TestListDto(
            test.Id,
            test.Title,
            test.Description,
            test.PassingScorePercent,
            test.Questions.Count,
            test.CreatedAt,
            test.TrainingMaterialId));
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost("import-file")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<TestListDto>> ImportFile(IFormFile file)
    {
        if (file.Length == 0 || Path.GetExtension(file.FileName).ToLowerInvariant() != ".json")
        {
            return BadRequest("Загрузите JSON-файл с тестом.");
        }

        await using var stream = file.OpenReadStream();
        var request = await JsonSerializer.DeserializeAsync<TestImportRequest>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        });

        if (request is null)
        {
            return BadRequest("Не удалось прочитать JSON.");
        }

        return await ImportJson(request);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost("{testId:int}/assign")]
    public async Task<IActionResult> Assign(int testId, AssignTestRequest request)
    {
        var testExists = await db.Tests.AnyAsync(x => x.Id == testId && x.IsActive);
        if (!testExists)
        {
            return NotFound("Тест не найден.");
        }

        if (request.EmployeeIds.Count == 0)
        {
            return BadRequest("Выберите хотя бы одного сотрудника.");
        }

        var employeeIds = request.EmployeeIds.Distinct().ToList();
        var existingEmployeeIds = await db.Employees
            .Where(x => employeeIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync();

        var notFoundIds = employeeIds.Except(existingEmployeeIds).ToList();
        if (notFoundIds.Count > 0)
        {
            return BadRequest($"Сотрудники не найдены: {string.Join(", ", notFoundIds)}");
        }

        var existingAssignments = await db.TestAssignments
            .Where(x => x.TestId == testId && employeeIds.Contains(x.EmployeeId))
            .ToListAsync();

        var existingIds = existingAssignments.Select(x => x.EmployeeId).ToHashSet();
        foreach (var assignment in existingAssignments)
        {
            assignment.Deadline = ToUtc(request.Deadline);
        }

        var userId = User.GetUserId();
        var newAssignments = employeeIds
            .Where(employeeId => !existingIds.Contains(employeeId))
            .Select(employeeId => new TestAssignment
            {
                TestId = testId,
                EmployeeId = employeeId,
                AssignedByUserId = userId,
                AssignedAt = DateTime.UtcNow,
                Deadline = ToUtc(request.Deadline),
                Status = TestAssignmentStatus.Assigned
            });

        db.TestAssignments.AddRange(newAssignments);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Employee")]
    [HttpGet("my")]
    public async Task<ActionResult<List<MyTestAssignmentDto>>> GetMyTests()
    {
        var employeeId = User.GetEmployeeId();
        if (employeeId is null)
        {
            return BadRequest("Профиль сотрудника не привязан к пользователю.");
        }

        var assignments = await db.TestAssignments
            .AsNoTracking()
            .Include(x => x.Test)
            .Where(x => x.EmployeeId == employeeId.Value)
            .OrderByDescending(x => x.AssignedAt)
            .Select(x => new MyTestAssignmentDto(
                x.Id,
                x.TestId,
                x.Test!.Title,
                x.Test.Description,
                x.Status.ToString(),
                x.LastScorePercent,
                x.AssignedAt,
                x.Deadline,
                x.CompletedAt,
                x.Test.TrainingMaterialId))
            .ToListAsync();

        return Ok(assignments);
    }

    [Authorize(Roles = "Employee")]
    [HttpGet("{testId:int}/take")]
    public async Task<ActionResult<TakeTestDto>> Take(int testId)
    {
        var employeeId = User.GetEmployeeId();
        if (employeeId is null)
        {
            return BadRequest("Профиль сотрудника не привязан к пользователю.");
        }

        var assignment = await db.TestAssignments
            .Include(x => x.Test)!
            .ThenInclude(x => x.Questions)
            .ThenInclude(x => x.Options)
            .FirstOrDefaultAsync(x => x.TestId == testId && x.EmployeeId == employeeId.Value);

        if (assignment?.Test is null)
        {
            return NotFound("Тест не назначен этому сотруднику.");
        }

        if (assignment.Status == TestAssignmentStatus.Assigned)
        {
            assignment.Status = TestAssignmentStatus.InProgress;
            await db.SaveChangesAsync();
        }

        var test = assignment.Test;
        var dto = new TakeTestDto(
            test.Id,
            assignment.Id,
            test.Title,
            test.Description,
            test.PassingScorePercent,
            test.Questions
                .OrderBy(q => q.SortOrder)
                .Select(q => new TakeQuestionDto(
                    q.Id,
                    q.Text,
                    q.Type,
                    q.Options
                        .OrderBy(o => o.SortOrder)
                        .Select(o => new TakeAnswerOptionDto(o.Id, o.Text))
                        .ToList()))
                .ToList());

        return Ok(dto);
    }

    [Authorize(Roles = "Employee")]
    [HttpPost("{testId:int}/submit")]
    public async Task<ActionResult<SubmitTestResponse>> Submit(int testId, SubmitTestRequest request)
    {
        var employeeId = User.GetEmployeeId();
        if (employeeId is null)
        {
            return BadRequest("Профиль сотрудника не привязан к пользователю.");
        }

        var assignment = await db.TestAssignments
            .Include(x => x.Test)!
            .ThenInclude(x => x.Questions)
            .ThenInclude(x => x.Options)
            .FirstOrDefaultAsync(x => x.TestId == testId && x.EmployeeId == employeeId.Value);

        if (assignment?.Test is null)
        {
            return NotFound("Тест не назначен этому сотруднику.");
        }

        var test = assignment.Test;
        var questions = test.Questions.OrderBy(q => q.SortOrder).ToList();
        if (questions.Count == 0)
        {
            return BadRequest("В тесте нет вопросов.");
        }

        var submittedByQuestion = request.Answers.ToDictionary(x => x.QuestionId, x => x.OptionIds.Distinct().OrderBy(id => id).ToList());
        var correctCount = 0;

        foreach (var question in questions)
        {
            var correctOptionIds = question.Options
                .Where(o => o.IsCorrect)
                .Select(o => o.Id)
                .OrderBy(id => id)
                .ToList();

            submittedByQuestion.TryGetValue(question.Id, out var submittedOptionIds);
            submittedOptionIds ??= [];

            if (correctOptionIds.SequenceEqual(submittedOptionIds))
            {
                correctCount++;
            }
        }

        var score = (int)Math.Round(correctCount * 100.0 / questions.Count, MidpointRounding.AwayFromZero);
        var isPassed = score >= test.PassingScorePercent;

        var attempt = new TestAttempt
        {
            TestId = test.Id,
            EmployeeId = employeeId.Value,
            TestAssignmentId = assignment.Id,
            StartedAt = DateTime.UtcNow,
            FinishedAt = DateTime.UtcNow,
            ScorePercent = score,
            IsPassed = isPassed
        };

        foreach (var answer in request.Answers)
        {
            var validOptionIds = questions
                .Where(q => q.Id == answer.QuestionId)
                .SelectMany(q => q.Options.Select(o => o.Id))
                .ToHashSet();

            foreach (var optionId in answer.OptionIds.Distinct().Where(validOptionIds.Contains))
            {
                attempt.Answers.Add(new TestAttemptAnswer
                {
                    TestQuestionId = answer.QuestionId,
                    TestAnswerOptionId = optionId
                });
            }
        }

        db.TestAttempts.Add(attempt);

        assignment.LastScorePercent = score;
        assignment.CompletedAt = DateTime.UtcNow;
        assignment.Status = isPassed ? TestAssignmentStatus.Passed : TestAssignmentStatus.Failed;

        await db.SaveChangesAsync();

        return Ok(new SubmitTestResponse(attempt.Id, score, isPassed, correctCount, questions.Count));
    }

    private async Task<string?> ValidateImportRequest(TestImportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return "Название теста обязательно.";
        }

        if (request.PassingScorePercent is < 0 or > 100)
        {
            return "Проходной балл должен быть от 0 до 100.";
        }

        if (request.TrainingMaterialId.HasValue && !await db.TrainingMaterials.AnyAsync(x => x.Id == request.TrainingMaterialId.Value && x.IsActive))
        {
            return "Обучающий материал не найден.";
        }

        if (request.Questions.Count == 0)
        {
            return "Добавьте хотя бы один вопрос.";
        }

        for (var i = 0; i < request.Questions.Count; i++)
        {
            var question = request.Questions[i];
            if (string.IsNullOrWhiteSpace(question.Text))
            {
                return $"Вопрос #{i + 1}: текст обязателен.";
            }

            if (question.Options.Count < 2)
            {
                return $"Вопрос #{i + 1}: нужно минимум 2 варианта ответа.";
            }

            var correctCount = question.Options.Count(x => x.IsCorrect);
            if (correctCount == 0)
            {
                return $"Вопрос #{i + 1}: укажите правильный ответ.";
            }

            if (question.Type == QuestionType.SingleChoice && correctCount != 1)
            {
                return $"Вопрос #{i + 1}: для SingleChoice должен быть ровно один правильный ответ.";
            }
        }

        return null;
    }

    private static Test MapImportRequestToTest(TestImportRequest request, int userId)
    {
        var test = new Test
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            TrainingMaterialId = request.TrainingMaterialId,
            PassingScorePercent = request.PassingScorePercent,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        for (var questionIndex = 0; questionIndex < request.Questions.Count; questionIndex++)
        {
            var questionRequest = request.Questions[questionIndex];
            var question = new TestQuestion
            {
                Text = questionRequest.Text.Trim(),
                Type = questionRequest.Type,
                SortOrder = questionIndex + 1
            };

            for (var optionIndex = 0; optionIndex < questionRequest.Options.Count; optionIndex++)
            {
                var optionRequest = questionRequest.Options[optionIndex];
                question.Options.Add(new TestAnswerOption
                {
                    Text = optionRequest.Text.Trim(),
                    IsCorrect = optionRequest.IsCorrect,
                    SortOrder = optionIndex + 1
                });
            }

            test.Questions.Add(question);
        }

        return test;
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
}
