using InstructionPlatform.Api.Domain.Entities;
using InstructionPlatform.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InstructionPlatform.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<TrainingMaterial> TrainingMaterials => Set<TrainingMaterial>();
    public DbSet<Test> Tests => Set<Test>();
    public DbSet<TestQuestion> TestQuestions => Set<TestQuestion>();
    public DbSet<TestAnswerOption> TestAnswerOptions => Set<TestAnswerOption>();
    public DbSet<TestAssignment> TestAssignments => Set<TestAssignment>();
    public DbSet<TestAttempt> TestAttempts => Set<TestAttempt>();
    public DbSet<TestAttemptAnswer> TestAttemptAnswers => Set<TestAttemptAnswer>();
    public DbSet<Department> Departments => Set<Department>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(x => x.DepartmentRef)
                .WithMany(x => x.Employees)
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("departments");
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
        });

        modelBuilder.Entity<TrainingMaterial>(entity =>
        {
            entity.HasOne(x => x.UploadedByUser)
                .WithMany(x => x.UploadedTrainingMaterials)
                .HasForeignKey(x => x.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Test>(entity =>
        {
            entity.HasOne(x => x.TrainingMaterial)
                .WithMany(x => x.Tests)
                .HasForeignKey(x => x.TrainingMaterialId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.CreatedByUser)
                .WithMany(x => x.CreatedTests)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TestQuestion>(entity =>
        {
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(x => x.Test)
                .WithMany(x => x.Questions)
                .HasForeignKey(x => x.TestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TestAnswerOption>(entity =>
        {
            entity.HasOne(x => x.Question)
                .WithMany(x => x.Options)
                .HasForeignKey(x => x.TestQuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TestAssignment>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(x => x.Test)
                .WithMany(x => x.Assignments)
                .HasForeignKey(x => x.TestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Employee)
                .WithMany(x => x.TestAssignments)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.AssignedByUser)
                .WithMany(x => x.AssignedTestAssignments)
                .HasForeignKey(x => x.AssignedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TestAttempt>(entity =>
        {
            entity.HasOne(x => x.Test)
                .WithMany()
                .HasForeignKey(x => x.TestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Employee)
                .WithMany(x => x.TestAttempts)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.TestAssignment)
                .WithMany(x => x.Attempts)
                .HasForeignKey(x => x.TestAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TestAttemptAnswer>(entity =>
        {
            entity.HasOne(x => x.TestAttempt)
                .WithMany(x => x.Answers)
                .HasForeignKey(x => x.TestAttemptId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Question)
                .WithMany()
                .HasForeignKey(x => x.TestQuestionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AnswerOption)
                .WithMany()
                .HasForeignKey(x => x.TestAnswerOptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
