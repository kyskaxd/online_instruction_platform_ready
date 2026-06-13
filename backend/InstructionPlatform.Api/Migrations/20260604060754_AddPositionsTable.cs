using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InstructionPlatform.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "positions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DepartmentId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_positions_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<int>(
                name: "PositionId",
                table: "employees",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                INSERT INTO positions ("Name", "DepartmentId", "CreatedAt")
                SELECT DISTINCT
                    COALESCE(NULLIF(e."Position", ''), 'Unknown') AS "Name",
                    e."DepartmentId",
                    NOW()
                FROM employees e
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM positions p
                    WHERE p."Name" = COALESCE(NULLIF(e."Position", ''), 'Unknown')
                      AND p."DepartmentId" = e."DepartmentId"
                );

                UPDATE employees e
                SET "PositionId" = p."Id"
                FROM positions p
                WHERE p."Name" = COALESCE(NULLIF(e."Position", ''), 'Unknown')
                  AND p."DepartmentId" = e."DepartmentId";
                """);

            migrationBuilder.AlterColumn<int>(
                name: "PositionId",
                table: "employees",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_PositionId",
                table: "employees",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_positions_DepartmentId",
                table: "positions",
                column: "DepartmentId");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "employees");

            migrationBuilder.AddForeignKey(
                name: "FK_employees_positions_PositionId",
                table: "employees",
                column: "PositionId",
                principalTable: "positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_employees_positions_PositionId",
                table: "employees");

            migrationBuilder.AddColumn<string>(
                name: "Position",
                table: "employees",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE employees e
                SET "Position" = p."Name"
                FROM positions p
                WHERE p."Id" = e."PositionId";
                """);

            migrationBuilder.DropIndex(
                name: "IX_employees_PositionId",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "PositionId",
                table: "employees");

            migrationBuilder.DropTable(
                name: "positions");
        }
    }
}
