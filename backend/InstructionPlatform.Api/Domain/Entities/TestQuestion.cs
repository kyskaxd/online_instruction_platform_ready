using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InstructionPlatform.Api.Domain.Enums;

namespace InstructionPlatform.Api.Domain.Entities;

[Table("test_questions")]
public class TestQuestion
{
    public int Id { get; set; }
    public int TestId { get; set; }
    public Test? Test { get; set; }

    [Required]
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; } = QuestionType.SingleChoice;
    public string? ExpectedAnswer { get; set; }
    public int SortOrder { get; set; }

    public ICollection<TestAnswerOption> Options { get; set; } = new List<TestAnswerOption>();
}
