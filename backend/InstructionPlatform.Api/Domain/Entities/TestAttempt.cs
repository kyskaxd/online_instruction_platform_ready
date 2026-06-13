using System.ComponentModel.DataAnnotations.Schema;

namespace InstructionPlatform.Api.Domain.Entities;

[Table("test_attempts")]
public class TestAttempt
{
    public int Id { get; set; }

    public int TestId { get; set; }
    public Test? Test { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public int TestAssignmentId { get; set; }
    public TestAssignment? TestAssignment { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }
    public int ScorePercent { get; set; }
    public bool IsPassed { get; set; }

    public ICollection<TestAttemptAnswer> Answers { get; set; } = new List<TestAttemptAnswer>();
}
