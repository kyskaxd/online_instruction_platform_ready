using InstructionPlatform.Api.Domain.Enums;

namespace InstructionPlatform.Api.Dtos;

public class TestImportRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? TrainingMaterialId { get; set; }
    public int PassingScorePercent { get; set; } = 80;
    public List<TestQuestionImportDto> Questions { get; set; } = [];
}

public class TestQuestionImportDto
{
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; } = QuestionType.SingleChoice;
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
    int PassingScorePercent,
    int QuestionsCount,
    DateTime CreatedAt,
    int? TrainingMaterialId);

public record AssignTestRequest(List<int> EmployeeIds, DateTime? Deadline);

public record MyTestAssignmentDto(
    int AssignmentId,
    int TestId,
    string TestTitle,
    string? Description,
    string Status,
    int? LastScorePercent,
    DateTime AssignedAt,
    DateTime? Deadline,
    DateTime? CompletedAt,
    int? TrainingMaterialId);

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
}

public record SubmitTestResponse(
    int AttemptId,
    int ScorePercent,
    bool IsPassed,
    int CorrectAnswers,
    int TotalQuestions);
