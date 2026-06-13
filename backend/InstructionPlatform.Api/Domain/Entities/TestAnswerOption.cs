using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InstructionPlatform.Api.Domain.Entities;

[Table("test_answer_options")]
public class TestAnswerOption
{
    public int Id { get; set; }
    public int TestQuestionId { get; set; }
    public TestQuestion? Question { get; set; }

    [Required]
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int SortOrder { get; set; }
}
