using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InstructionPlatform.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInstructionComplianceFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "training_materials",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InstructionType",
                table: "training_materials",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RetrainingIntervalMonths",
                table: "training_materials",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "tests",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InstructionType",
                table: "tests",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RetrainingIntervalMonths",
                table: "tests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "InstructionType",
                table: "test_assignments",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetrainingDueAt",
                table: "test_assignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "instruction_journal_entries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProtocolNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    TestId = table.Column<int>(type: "integer", nullable: false),
                    TestAssignmentId = table.Column<int>(type: "integer", nullable: false),
                    TestAttemptId = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    InstructionType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EmployeeFullName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Department = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Position = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TestTitle = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ScorePercent = table.Column<int>(type: "integer", nullable: false),
                    IsPassed = table.Column<bool>(type: "boolean", nullable: false),
                    MaterialStudiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextRetrainingDueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InstructedByUserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_instruction_journal_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_instruction_journal_entries_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_instruction_journal_entries_employees_InstructedByUserId",
                        column: x => x.InstructedByUserId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_instruction_journal_entries_test_assignments_TestAssignment~",
                        column: x => x.TestAssignmentId,
                        principalTable: "test_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_instruction_journal_entries_test_attempts_TestAttemptId",
                        column: x => x.TestAttemptId,
                        principalTable: "test_attempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_instruction_journal_entries_tests_TestId",
                        column: x => x.TestId,
                        principalTable: "tests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "material_study_records",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    TrainingMaterialId = table.Column<int>(type: "integer", nullable: false),
                    FirstViewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastViewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_study_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_material_study_records_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_material_study_records_training_materials_TrainingMaterialId",
                        column: x => x.TrainingMaterialId,
                        principalTable: "training_materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_instruction_journal_entries_EmployeeId",
                table: "instruction_journal_entries",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_instruction_journal_entries_InstructedByUserId",
                table: "instruction_journal_entries",
                column: "InstructedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_instruction_journal_entries_ProtocolNumber",
                table: "instruction_journal_entries",
                column: "ProtocolNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_instruction_journal_entries_TestAssignmentId",
                table: "instruction_journal_entries",
                column: "TestAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_instruction_journal_entries_TestAttemptId",
                table: "instruction_journal_entries",
                column: "TestAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_instruction_journal_entries_TestId",
                table: "instruction_journal_entries",
                column: "TestId");

            migrationBuilder.CreateIndex(
                name: "IX_material_study_records_EmployeeId_TrainingMaterialId",
                table: "material_study_records",
                columns: new[] { "EmployeeId", "TrainingMaterialId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_material_study_records_TrainingMaterialId",
                table: "material_study_records",
                column: "TrainingMaterialId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "instruction_journal_entries");

            migrationBuilder.DropTable(
                name: "material_study_records");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "training_materials");

            migrationBuilder.DropColumn(
                name: "InstructionType",
                table: "training_materials");

            migrationBuilder.DropColumn(
                name: "RetrainingIntervalMonths",
                table: "training_materials");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "tests");

            migrationBuilder.DropColumn(
                name: "InstructionType",
                table: "tests");

            migrationBuilder.DropColumn(
                name: "RetrainingIntervalMonths",
                table: "tests");

            migrationBuilder.DropColumn(
                name: "InstructionType",
                table: "test_assignments");

            migrationBuilder.DropColumn(
                name: "NextRetrainingDueAt",
                table: "test_assignments");
        }
    }
}
