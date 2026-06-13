using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InstructionPlatform.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInstructionJournal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "instruction_journal_entries");

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_EmployeeId",
                table: "refresh_tokens",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TokenHash",
                table: "refresh_tokens",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.CreateTable(
                name: "instruction_journal_entries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    InstructedByUserId = table.Column<int>(type: "integer", nullable: false),
                    TestAssignmentId = table.Column<int>(type: "integer", nullable: false),
                    TestAttemptId = table.Column<int>(type: "integer", nullable: false),
                    TestId = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Department = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EmployeeFullName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    InstructionType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsPassed = table.Column<bool>(type: "boolean", nullable: false),
                    MaterialStudiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextRetrainingDueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Position = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ProtocolNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ScorePercent = table.Column<int>(type: "integer", nullable: false),
                    TestTitle = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false)
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
        }
    }
}
