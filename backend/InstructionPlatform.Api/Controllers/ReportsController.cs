using System.IO.Compression;
using InstructionPlatform.Api.Data;
using InstructionPlatform.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstructionPlatform.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin,Manager")]
public class ReportsController(AppDbContext db) : ControllerBase
{
    [HttpGet("test-results")]
    public async Task<ActionResult<List<TestResultReportDto>>> GetTestResults(
        [FromQuery] int? testId,
        [FromQuery] int? employeeId,
        [FromQuery] string? department)
    {
        var query = BuildReportQuery(testId, employeeId, department);
        return Ok(await query.ToListAsync());
    }

    [HttpGet("test-results.xlsx")]
    public async Task<IActionResult> ExportTestResultsExcel(
        [FromQuery] int? testId,
        [FromQuery] int? employeeId,
        [FromQuery] string? department)
    {
        var rows = await BuildReportQuery(testId, employeeId, department).ToListAsync();

        return File(
            BuildExcelFile(rows),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "test-results.xlsx");
    }

    private IQueryable<TestResultReportDto> BuildReportQuery(int? testId, int? employeeId, string? department)
    {
        var assignments = db.TestAssignments
            .AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.Test)
            .AsQueryable();

        if (testId.HasValue)
        {
            assignments = assignments.Where(x => x.TestId == testId.Value);
        }

        if (employeeId.HasValue)
        {
            assignments = assignments.Where(x => x.EmployeeId == employeeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            assignments = assignments.Where(x => x.Employee!.Department.ToLower().Contains(department.ToLower()));
        }

        return assignments
            .OrderBy(x => x.Employee!.Department)
            .ThenBy(x => x.Employee!.LastName)
            .ThenBy(x => x.Test!.Title)
            .Select(x => new TestResultReportDto(
                x.Id,
                x.EmployeeId,
                x.Employee!.LastName + " " + x.Employee.FirstName + " " + (x.Employee.MiddleName ?? string.Empty),
                x.Employee.Department,
                x.Employee.Position,
                x.TestId,
                x.Test!.Title,
                x.Status.ToString(),
                x.LastScorePercent,
                x.LastScorePercent.HasValue ? x.LastScorePercent.Value >= x.Test.PassingScorePercent : null,
                x.AssignedAt,
                x.Deadline,
                x.CompletedAt));
    }

    private static byte[] BuildExcelFile(List<TestResultReportDto> rows)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
                </Types>
                """);

            AddEntry(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """);

            AddEntry(archive, "xl/workbook.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets>
                    <sheet name="Test results" sheetId="1" r:id="rId1"/>
                  </sheets>
                </workbook>
                """);

            AddEntry(archive, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
                </Relationships>
                """);

            AddEntry(archive, "xl/styles.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <fonts count="2"><font/><font><b/></font></fonts>
                  <fills count="1"><fill><patternFill patternType="none"/></fill></fills>
                  <borders count="1"><border/></borders>
                  <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
                  <cellXfs count="2">
                    <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
                    <xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/>
                  </cellXfs>
                </styleSheet>
                """);

            AddEntry(archive, "xl/worksheets/sheet1.xml", BuildWorksheetXml(rows));
        }

        return stream.ToArray();
    }

    private static string BuildWorksheetXml(List<TestResultReportDto> rows)
    {
        var headers = new[]
        {
            "Сотрудник",
            "Отдел",
            "Должность",
            "Тест",
            "Статус",
            "Балл",
            "Пройден",
            "Назначен",
            "Дата прохождения"
        };

        var sheet = new System.Text.StringBuilder();
        sheet.Append("""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <cols>
                <col min="1" max="1" width="28" customWidth="1"/>
                <col min="2" max="4" width="22" customWidth="1"/>
                <col min="5" max="7" width="14" customWidth="1"/>
                <col min="8" max="10" width="19" customWidth="1"/>
              </cols>
              <sheetData>
            """);

        sheet.Append("<row r=\"1\">");
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Append(Cell(1, i + 1, headers[i], style: 1));
        }
        sheet.Append("</row>");

        for (var i = 0; i < rows.Count; i++)
        {
            var rowNumber = i + 2;
            var row = rows[i];
            var values = new[]
            {
                row.EmployeeFullName,
                row.Department,
                row.Position,
                row.TestTitle,
                StatusLabel(row.Status),
                row.ScorePercent?.ToString() ?? string.Empty,
                row.IsPassed switch { true => "Да", false => "Нет", _ => string.Empty },
                FormatDate(row.AssignedAt),
                row.CompletedAt.HasValue ? FormatDate(row.CompletedAt.Value) : string.Empty
            };

            sheet.Append($"<row r=\"{rowNumber}\">");
            for (var column = 0; column < values.Length; column++)
            {
                sheet.Append(Cell(rowNumber, column + 1, values[column]));
            }
            sheet.Append("</row>");
        }

        sheet.Append("""
              </sheetData>
            </worksheet>
            """);

        return sheet.ToString();
    }

    private static string Cell(int row, int column, string value, int style = 0)
    {
        var styleAttribute = style > 0 ? $" s=\"{style}\"" : string.Empty;
        return $"<c r=\"{ColumnName(column)}{row}\" t=\"inlineStr\"{styleAttribute}><is><t>{EscapeXml(value)}</t></is></c>";
    }

    private static string ColumnName(int column)
    {
        var name = string.Empty;
        while (column > 0)
        {
            column--;
            name = (char)('A' + column % 26) + name;
            column /= 26;
        }

        return name;
    }

    private static string FormatDate(DateTime value) => value.ToString("yyyy-MM-dd HH:mm");

    private static string StatusLabel(string status)
    {
        return status switch
        {
            "Assigned" => "Назначен",
            "InProgress" => "В процессе",
            "Passed" => "Пройден",
            "Failed" => "Не пройден",
            _ => status
        };
    }

    private static string EscapeXml(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    private static void AddEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), new System.Text.UTF8Encoding(false));
        writer.Write(content);
    }
}
