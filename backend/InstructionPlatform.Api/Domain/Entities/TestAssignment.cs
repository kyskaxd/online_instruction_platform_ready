using System.ComponentModel.DataAnnotations.Schema;
using InstructionPlatform.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InstructionPlatform.Api.Domain.Entities;

[Table("test_assignments")]
[Index(nameof(TestId), nameof(EmployeeId), IsUnique = true)]
public class TestAssignment
{
    public int Id { get; set; }

    public int TestId { get; set; }
    public Test? Test { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public int AssignedByUserId { get; set; }
    public Employee? AssignedByUser { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? Deadline { get; set; }
    public TestAssignmentStatus Status { get; set; } = TestAssignmentStatus.Assigned;
    public int? LastScorePercent { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ICollection<TestAttempt> Attempts { get; set; } = new List<TestAttempt>();
}
