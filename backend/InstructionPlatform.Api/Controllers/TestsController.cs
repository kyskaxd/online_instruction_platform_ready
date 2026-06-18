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
public class TestsController(AppDbContext db, InstructionValidationService validationService) : ControllerBase
{
    private const int MaxAttempts = 2;

    [Authorize(Roles = "Admin,HR")]
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
                x.Category,
                x.InstructionType,
                x.RetrainingIntervalMonths,
                x.PassingScorePercent,
                x.Questions.Count,
                x.CreatedAt,
                x.TrainingMaterialId,
                x.CreatedByUserId))
            .ToListAsync();

        return Ok(tests);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpGet("{testId:int}")]
    public async Task<ActionResult<TestDetailDto>> GetById(int testId)
    {
        var test = await db.Tests
            .AsNoTracking()
            .Include(x => x.Questions)
            .ThenInclude(x => x.Options)
            .FirstOrDefaultAsync(x => x.Id == testId && x.IsActive);

        if (test is null)
        {
            return NotFound("Тест не найден.");
        }

        var dto = new TestDetailDto
        {
            Id = test.Id,
            Title = test.Title,
            Description = test.Description,
            Category = test.Category,
            InstructionType = test.InstructionType,
            RetrainingIntervalMonths = test.RetrainingIntervalMonths,
            TrainingMaterialId = test.TrainingMaterialId,
            PassingScorePercent = test.PassingScorePercent,
            CreatedByUserId = test.CreatedByUserId,
            Questions = test.Questions.Select(q => new TestDetailQuestionDto
            {
                Id = q.Id,
                Text = q.Text,
                Type = q.Type,
                ExpectedAnswer = q.ExpectedAnswer,
                Options = q.Options.Select(o => new TestDetailOptionDto
                {
                    Id = o.Id,
                    Text = o.Text,
                    IsCorrect = o.IsCorrect
                }).ToList()
            }).ToList()
        };

        return Ok(dto);
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPut("{testId:int}")]
    public async Task<IActionResult> Update(int testId, [FromBody] TestImportRequest request)
    {
        var test = await db.Tests
            .Include(x => x.Questions)
            .ThenInclude(x => x.Options)
            .FirstOrDefaultAsync(x => x.Id == testId && x.IsActive);

        if (test is null)
        {
            return NotFound("Тест не найден.");
        }

        var validationError = await ValidateImportRequest(request);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        // Проверяем идентичность категории и вида инструктажа с материалом
        var instructionError = await validationService.ValidateTestInstructionMatch(
            request.TrainingMaterialId,
            request.Category,
            request.InstructionType);
        if (instructionError is not null)
        {
            return BadRequest(instructionError);
        }

        test.Title = request.Title;
        test.Description = request.Description;
        test.Category = request.Category;
        test.InstructionType = request.InstructionType;
        test.RetrainingIntervalMonths = request.RetrainingIntervalMonths;
        test.TrainingMaterialId = request.TrainingMaterialId;
        test.PassingScorePercent = request.PassingScorePercent;

        // Удалить старые вопросы
        db.TestQuestions.RemoveRange(test.Questions);
        test.Questions.Clear();

        // Добавить новые вопросы
        foreach (var questionRequest in request.Questions)
        {
            var question = new TestQuestion
            {
                TestId = testId,
                Text = questionRequest.Text,
                Type = questionRequest.Type,
                ExpectedAnswer = questionRequest.ExpectedAnswer,
                Options = questionRequest.Options.Select(o => new TestAnswerOption
                {
                    Text = o.Text,
                    IsCorrect = o.IsCorrect
                }).ToList()
            };
            test.Questions.Add(question);
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost("import-json")]
    public async Task<ActionResult<TestListDto>> ImportJson([FromBody] TestImportRequest request)
    {
        var validationError = await ValidateImportRequest(request);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        // Проверяем идентичность категории и вида инструктажа с материалом
        var instructionError = await validationService.ValidateTestInstructionMatch(
            request.TrainingMaterialId,
            request.Category,
            request.InstructionType);
        if (instructionError is not null)
        {
            return BadRequest(instructionError);
        }

        var test = MapImportRequestToTest(request, User.GetUserId());
        db.Tests.Add(test);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = test.Id }, new TestListDto(
            test.Id,
            test.Title,
            test.Description,
            test.Category,
            test.InstructionType,
            test.RetrainingIntervalMonths,
            test.PassingScorePercent,
            test.Questions.Count,
            test.CreatedAt,
            test.TrainingMaterialId,
            test.CreatedByUserId));
    }

    [Authorize(Roles = "Admin,HR")]
    [HttpPost("{testId:int}/assign")]
    public async Task<IActionResult> Assign(int testId, AssignTestRequest request)
    {
        var testExists = await db.Tests.AnyAsync(x => x.Id == testId && x.IsActive);
        if (!testExists)
        {
            return NotFound("Тест не найден.");
        }

        var departmentIds = request.DepartmentIds ?? new List<int>();
        var employeeIds = request.EmployeeIds?.Distinct().ToList() ?? new List<int>();

        if (departmentIds.Count == 0 && employeeIds.Count == 0)
        {
            return BadRequest("Выберите отделы для назначения.");
        }

        if (departmentIds.Count > 0)
        {
            var validDepartmentIds = await db.Departments
                .Where(x => departmentIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();

            var notFoundDepartmentIds = departmentIds.Except(validDepartmentIds).ToList();
            if (notFoundDepartmentIds.Count > 0)
            {
                return BadRequest($"Отделы не найдены: {string.Join(", ", notFoundDepartmentIds)}");
            }

            var departmentEmployeeIds = await db.Employees
                .Where(x => validDepartmentIds.Contains(x.DepartmentId) && x.IsActive)
                .Select(x => x.Id)
                .ToListAsync();

            employeeIds.AddRange(departmentEmployeeIds);
        }

        employeeIds = employeeIds.Distinct().ToList();
        if (employeeIds.Count == 0)
        {
            return BadRequest("Сотрудники для назначения не найдены.");
        }

        var existingEmployeeIds = await db.Employees
            .Where(x => employeeIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync();

        var notFoundIds = employeeIds.Except(existingEmployeeIds).ToList();
        if (notFoundIds.Count > 0)
        {
            return BadRequest($"Сотрудники не найдены: {string.Join(", ", notFoundIds)}");
        }

        var test = await db.Tests.AsNoTracking().FirstAsync(x => x.Id == testId);
        var instructionType = request.InstructionType ?? test.InstructionType;

        var existingAssignments = await db.TestAssignments
            .Where(x => x.TestId == testId && employeeIds.Contains(x.EmployeeId))
            .ToListAsync();

        var existingIds = existingAssignments.Select(x => x.EmployeeId).ToHashSet();
        foreach (var assignment in existingAssignments)
        {
            assignment.Deadline = ToUtc(request.Deadline);
            assignment.InstructionType = instructionType;
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
                InstructionType = instructionType,
                Status = TestAssignmentStatus.Assigned
            });

        db.TestAssignments.AddRange(newAssignments);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{testId:int}/delete")]
    public async Task<IActionResult> Delete(int testId)
    {
        var test = await db.Tests.FirstOrDefaultAsync(x => x.Id == testId);
        if (test is null)
        {
            return NotFound("Тест не найден.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync();

        var attemptIds = await db.TestAttempts
            .Where(x => x.TestId == testId)
            .Select(x => x.Id)
            .ToListAsync();

        var attemptAnswers = await db.TestAttemptAnswers
            .Where(x => attemptIds.Contains(x.TestAttemptId))
            .ToListAsync();
        db.TestAttemptAnswers.RemoveRange(attemptAnswers);

        var attempts = await db.TestAttempts
            .Where(x => x.TestId == testId)
            .ToListAsync();
        db.TestAttempts.RemoveRange(attempts);

        var assignments = await db.TestAssignments
            .Where(x => x.TestId == testId)
            .ToListAsync();
        db.TestAssignments.RemoveRange(assignments);

        var questionIds = await db.TestQuestions
            .Where(x => x.TestId == testId)
            .Select(x => x.Id)
            .ToListAsync();

        var answerOptions = await db.TestAnswerOptions
            .Where(x => questionIds.Contains(x.TestQuestionId))
            .ToListAsync();
        db.TestAnswerOptions.RemoveRange(answerOptions);

        var questions = await db.TestQuestions
            .Where(x => x.TestId == testId)
            .ToListAsync();
        db.TestQuestions.RemoveRange(questions);

        db.Tests.Remove(test);

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

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
            .Include(x => x.Attempts)
            .Where(x => x.EmployeeId == employeeId.Value)
            .OrderByDescending(x => x.AssignedAt)
            .ToListAsync();

        var materialIds = assignments
            .Where(x => x.Test?.TrainingMaterialId.HasValue == true)
            .Select(x => x.Test!.TrainingMaterialId!.Value)
            .Distinct()
            .ToList();

        var acknowledgedMaterialIds = await db.MaterialStudyRecords
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId.Value
                        && materialIds.Contains(x.TrainingMaterialId)
                        && x.AcknowledgedAt != null)
            .Select(x => x.TrainingMaterialId)
            .ToListAsync();

        var acknowledgedSet = acknowledgedMaterialIds.ToHashSet();

        var result = assignments.Select(x =>
        {
            var materialId = x.Test?.TrainingMaterialId;
            var materialRequired = materialId.HasValue;
            var materialCompleted = materialId.HasValue && acknowledgedSet.Contains(materialId.Value);

            var attemptCount = x.Attempts.Count;
            var bestScore = x.Attempts.Count > 0 ? x.Attempts.Max(a => a.ScorePercent) : x.LastScorePercent;
            var canRetake = CanRetakeAssignment(x);

            return new MyTestAssignmentDto(
                x.Id,
                x.TestId,
                x.Test!.Title,
                x.Test.Description,
                x.Test.Category,
                x.InstructionType,
                ResolveDisplayStatus(x),
                x.LastScorePercent,
                bestScore,
                attemptCount,
                MaxAttempts,
                canRetake,
                x.AssignedAt,
                x.Deadline,
                x.CompletedAt,
                x.NextRetrainingDueAt,
                materialId,
                materialRequired,
                materialCompleted);
        }).ToList();

        return Ok(result);
    }

    [Authorize(Roles = "Employee")]
    [HttpGet("{testId:int}/result")]
    public async Task<ActionResult<TestAssignmentResultDto>> GetResult(int testId)
    {
        var employeeId = User.GetEmployeeId();
        if (employeeId is null)
        {
            return BadRequest("Профиль сотрудника не привязан к пользователю.");
        }

        var assignment = await db.TestAssignments
            .AsNoTracking()
            .Include(x => x.Test)
            .Include(x => x.Attempts)
            .FirstOrDefaultAsync(x => x.TestId == testId && x.EmployeeId == employeeId.Value);

        if (assignment?.Test is null)
        {
            return NotFound("Тест не назначен этому сотруднику.");
        }

        return Ok(MapAssignmentResult(assignment));
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
            .Include(x => x.Attempts)
            .Include(x => x.Test!)
            .ThenInclude(x => x.Questions)
            .ThenInclude(x => x.Options)
            .FirstOrDefaultAsync(x => x.TestId == testId && x.EmployeeId == employeeId.Value);

        if (assignment?.Test is null)
        {
            return NotFound("Тест не назначен этому сотруднику.");
        }

        if (!CanRetakeAssignment(assignment))
        {
            return BadRequest("Тест уже сдан или попытки исчерпаны. Откройте результат.");
        }

        var test = assignment.Test;
        if (test.TrainingMaterialId.HasValue)
        {
            var materialStudied = await db.MaterialStudyRecords
                .AnyAsync(x => x.EmployeeId == employeeId.Value
                               && x.TrainingMaterialId == test.TrainingMaterialId.Value
                               && x.AcknowledgedAt != null);

            if (!materialStudied)
            {
                return BadRequest("Перед прохождением теста необходимо изучить и подтвердить ознакомление с обучающим материалом.");
            }
        }

        if (assignment.Status == TestAssignmentStatus.Assigned)
        {
            assignment.Status = TestAssignmentStatus.InProgress;
            await db.SaveChangesAsync();
        }
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
            .Include(x => x.Attempts)
            .Include(x => x.Test!)
            .ThenInclude(x => x.Questions)
            .ThenInclude(x => x.Options)
            .FirstOrDefaultAsync(x => x.TestId == testId && x.EmployeeId == employeeId.Value);

        if (assignment?.Test is null)
        {
            return NotFound("Тест не назначен этому сотруднику.");
        }

        if (!CanRetakeAssignment(assignment))
        {
            return BadRequest("Тест уже сдан или попытки исчерпаны.");
        }

        var test = assignment.Test;
        if (test.TrainingMaterialId.HasValue)
        {
            var materialStudied = await db.MaterialStudyRecords
                .AnyAsync(x => x.EmployeeId == employeeId.Value
                               && x.TrainingMaterialId == test.TrainingMaterialId.Value
                               && x.AcknowledgedAt != null);

            if (!materialStudied)
            {
                return BadRequest("Перед прохождением теста необходимо изучить и подтвердить ознакомление с обучающим материалом.");
            }
        }

        var questions = test.Questions.OrderBy(q => q.SortOrder).ToList();
        if (questions.Count == 0)
        {
            return BadRequest("В тесте нет вопросов.");
        }

        var submittedByQuestion = request.Answers?.ToDictionary(x => x.QuestionId) ?? new Dictionary<int, SubmittedQuestionAnswerDto>();
        var correctCount = 0;

        foreach (var question in questions)
        {
            if (question.Type == QuestionType.Text)
            {
                submittedByQuestion.TryGetValue(question.Id, out var submittedTextAnswer);
                var submittedText = submittedTextAnswer?.AnswerText?.Trim();
                var expectedText = question.ExpectedAnswer?.Trim();

                if (!string.IsNullOrWhiteSpace(expectedText)
                    && !string.IsNullOrWhiteSpace(submittedText)
                    && string.Equals(expectedText, submittedText, StringComparison.OrdinalIgnoreCase))
                {
                    correctCount++;
                }

                continue;
            }

            var correctOptionIds = question.Options
                .Where(o => o.IsCorrect)
                .Select(o => o.Id)
                .OrderBy(id => id)
                .ToList();

            submittedByQuestion.TryGetValue(question.Id, out var submittedOptionAnswer);
            var submittedOptionIds = submittedOptionAnswer?.OptionIds?.Distinct().OrderBy(id => id).ToList() ?? [];

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

        foreach (var answer in request.Answers ?? [])
        {
            var question = questions.FirstOrDefault(q => q.Id == answer.QuestionId);
            if (question is null)
            {
                continue;
            }

            if (question.Type == QuestionType.Text)
            {
                var answerText = answer.AnswerText?.Trim();
                if (!string.IsNullOrWhiteSpace(answerText))
                {
                    attempt.Answers.Add(new TestAttemptAnswer
                    {
                        TestQuestionId = answer.QuestionId,
                        AnswerText = answerText
                    });
                }

                continue;
            }

            var validOptionIds = question.Options
                .Select(o => o.Id)
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

        var completedAt = DateTime.UtcNow;
        var attemptsAfterSubmit = await db.TestAttempts.CountAsync(x => x.TestAssignmentId == assignment.Id) + 1;

        assignment.LastScorePercent = score;
        assignment.CompletedAt = completedAt;

        if (isPassed || assignment.Status == TestAssignmentStatus.Passed)
        {
            assignment.Status = TestAssignmentStatus.Passed;
            assignment.NextRetrainingDueAt = completedAt.AddMonths(test.RetrainingIntervalMonths);
        }
        else if (attemptsAfterSubmit >= MaxAttempts)
        {
            assignment.Status = TestAssignmentStatus.Failed;
        }
        else
        {
            assignment.Status = TestAssignmentStatus.InProgress;
        }

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

        if (request.RetrainingIntervalMonths is < 1 or > 60)
        {
            return "Срок повторного инструктажа должен быть от 1 до 60 месяцев.";
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

            if (question.Type == QuestionType.Text)
            {
                if (string.IsNullOrWhiteSpace(question.ExpectedAnswer))
                {
                    return $"Вопрос #{i + 1}: для текстового вопроса укажите правильный ответ.";
                }

                if (question.Options.Count > 0)
                {
                    return $"Вопрос #{i + 1}: текстовый вопрос не должен содержать вариантов ответа.";
                }

                continue;
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
            Category = request.Category,
            InstructionType = request.InstructionType,
            RetrainingIntervalMonths = request.RetrainingIntervalMonths,
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
                ExpectedAnswer = questionRequest.Type == QuestionType.Text
                    ? questionRequest.ExpectedAnswer?.Trim()
                    : null,
                SortOrder = questionIndex + 1
            };

            if (questionRequest.Type != QuestionType.Text)
            {
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
            }

            test.Questions.Add(question);
        }

        return test;
    }
    private static string ResolveDisplayStatus(TestAssignment assignment)
    {
        if (assignment.Status == TestAssignmentStatus.Passed || assignment.Attempts.Any(x => x.IsPassed))
        {
            return TestAssignmentStatus.Passed.ToString();
        }

        return assignment.Status.ToString();
    }

    private static bool CanRetakeAssignment(TestAssignment assignment)
    {
        if (assignment.Status == TestAssignmentStatus.Passed)
        {
            return false;
        }

        return assignment.Attempts.Count < MaxAttempts;
    }

    private static TestAssignmentResultDto MapAssignmentResult(TestAssignment assignment)
    {
        var test = assignment.Test!;
        var attempts = assignment.Attempts
            .OrderBy(x => x.FinishedAt)
            .Select((attempt, index) => new TestAttemptSummaryDto(
                attempt.Id,
                index + 1,
                attempt.ScorePercent,
                attempt.IsPassed,
                attempt.FinishedAt ?? attempt.StartedAt))
            .ToList();

        var bestScore = attempts.Count > 0
            ? attempts.Max(x => x.ScorePercent)
            : assignment.LastScorePercent;

        return new TestAssignmentResultDto(
            assignment.Id,
            assignment.TestId,
            test.Title,
            test.Description,
            test.Category,
            assignment.InstructionType,
            ResolveDisplayStatus(assignment),
            test.PassingScorePercent,
            bestScore,
            attempts.Count,
            MaxAttempts,
            CanRetakeAssignment(assignment),
            assignment.NextRetrainingDueAt,
            attempts);
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
