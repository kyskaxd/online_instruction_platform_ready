using InstructionPlatform.Api.Domain.Enums;

namespace InstructionPlatform.Api.Dtos;

public class TestImportRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? TrainingMaterialId { get; set; }
    public InstructionCategory Category { get; set; } = InstructionCategory.OccupationalSafety;
    public InstructionType InstructionType { get; set; } = InstructionType.Repeated;
    public int RetrainingIntervalMonths { get; set; } = 12;
    public int PassingScorePercent { get; set; } = 80;
    public List<TestQuestionImportDto> Questions { get; set; } = [];
}

public class TestQuestionImportDto
{
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; } = QuestionType.SingleChoice;
    public string? ExpectedAnswer { get; set; }
    public List<TestAnswerOptionImportDto> Options { get; set; } = [];
}

public class TestAnswerOptionImportDto
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public record TestListDto(
    int Id,
    string Title,
    string? Description,
    InstructionCategory Category,
    InstructionType InstructionType,
    int RetrainingIntervalMonths,
    int PassingScorePercent,
    int QuestionsCount,
    DateTime CreatedAt,
    int? TrainingMaterialId);

public record AssignTestRequest(
    List<int>? EmployeeIds,
    List<int>? DepartmentIds,
    DateTime? Deadline,
    InstructionType? InstructionType);

public record MyTestAssignmentDto(
    int AssignmentId,
    int TestId,
    string TestTitle,
    string? Description,
    InstructionCategory Category,
    InstructionType InstructionType,
    string Status,
    int? LastScorePercent,
    int? BestScorePercent,
    int AttemptCount,
    int MaxAttempts,
    bool CanRetake,
    DateTime AssignedAt,
    DateTime? Deadline,
    DateTime? CompletedAt,
    DateTime? NextRetrainingDueAt,
    int? TrainingMaterialId,
    bool MaterialStudyRequired,
    bool MaterialStudyCompleted);

public record TestAttemptSummaryDto(
    int AttemptId,
    int AttemptNumber,
    int ScorePercent,
    bool IsPassed,
    DateTime FinishedAt);

public record TestAssignmentResultDto(
    int AssignmentId,
    int TestId,
    string TestTitle,
    string? Description,
    InstructionCategory Category,
    InstructionType InstructionType,
    string Status,
    int PassingScorePercent,
    int? BestScorePercent,
    int AttemptCount,
    int MaxAttempts,
    bool CanRetake,
    DateTime? NextRetrainingDueAt,
    List<TestAttemptSummaryDto> Attempts);

public record TakeTestDto(
    int TestId,
    int AssignmentId,
    string Title,
    string? Description,
    int PassingScorePercent,
    List<TakeQuestionDto> Questions);

public record TakeQuestionDto(
    int Id,
    string Text,
    QuestionType Type,
    List<TakeAnswerOptionDto> Options);

public record TakeAnswerOptionDto(int Id, string Text);

public class SubmitTestRequest
{
    public List<SubmittedQuestionAnswerDto> Answers { get; set; } = [];
}

public class SubmittedQuestionAnswerDto
{
    public int QuestionId { get; set; }
    public List<int> OptionIds { get; set; } = [];
    public string? AnswerText { get; set; }
}

public record SubmitTestResponse(
    int AttemptId,
    int ScorePercent,
    bool IsPassed,
    int CorrectAnswers,
    int TotalQuestions);

public class TestDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public InstructionCategory Category { get; set; }
    public InstructionType InstructionType { get; set; }
    public int RetrainingIntervalMonths { get; set; }
    public int? TrainingMaterialId { get; set; }
    public int PassingScorePercent { get; set; }
    public List<TestDetailQuestionDto> Questions { get; set; } = [];
}

public class TestDetailQuestionDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public string? ExpectedAnswer { get; set; }
    public List<TestDetailOptionDto> Options { get; set; } = [];
}

public class TestDetailOptionDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
