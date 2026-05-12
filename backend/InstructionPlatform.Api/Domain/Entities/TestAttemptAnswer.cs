using System.ComponentModel.DataAnnotations.Schema;

namespace InstructionPlatform.Api.Domain.Entities;

[Table("test_attempt_answers")]
public class TestAttemptAnswer
{
    public int Id { get; set; }

    public int TestAttemptId { get; set; }
    public TestAttempt? TestAttempt { get; set; }

    public int TestQuestionId { get; set; }
    public TestQuestion? Question { get; set; }

    public int TestAnswerOptionId { get; set; }
    public TestAnswerOption? AnswerOption { get; set; }
}
