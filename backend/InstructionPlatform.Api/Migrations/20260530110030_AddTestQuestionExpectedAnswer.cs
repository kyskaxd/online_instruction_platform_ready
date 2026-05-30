using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstructionPlatform.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTestQuestionExpectedAnswer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExpectedAnswer",
                table: "test_questions",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TestAnswerOptionId",
                table: "test_attempt_answers",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "AnswerText",
                table: "test_attempt_answers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedAnswer",
                table: "test_questions");

            migrationBuilder.DropColumn(
                name: "AnswerText",
                table: "test_attempt_answers");

            migrationBuilder.AlterColumn<int>(
                name: "TestAnswerOptionId",
                table: "test_attempt_answers",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
