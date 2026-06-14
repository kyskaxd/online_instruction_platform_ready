using System.IO.Compression;
using InstructionPlatform.Api.Data;
using InstructionPlatform.Api.Dtos;
using InstructionPlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstructionPlatform.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin,Manager,HR")]
public class ReportsController(AppDbContext db) : ControllerBase
{
    [HttpGet("test-results")]
    public async Task<ActionResult<List<TestResultReportDto>>> GetTestResults(
        [FromQuery] int? testId,
        [FromQuery] int? employeeId,
        [FromQuery] string? department,
        [FromQuery] Domain.Enums.InstructionCategory? category)
    {
        var query = BuildReportQuery(testId, employeeId, department, category);
        return Ok(await query.ToListAsync());
    }

    [HttpGet("test-results.xlsx")]
    public async Task<IActionResult> ExportTestResultsExcel(
        [FromQuery] int? testId,
        [FromQuery] int? employeeId,
        [FromQuery] string? department,
        [FromQuery] Domain.Enums.InstructionCategory? category)
    {
        if (category is null)
        {
            return BadRequest("Выберите одно направление для выгрузки Excel.");
        }

        var rows = await BuildReportQuery(testId, employeeId, department, category).ToListAsync();

        return File(
            BuildExcelFile(rows, category.Value),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "test-results.xlsx");
    }

    private IQueryable<TestResultReportDto> BuildReportQuery(
        int? testId,
        int? employeeId,
        string? department,
        Domain.Enums.InstructionCategory? category)
    {
        var assignments = db.TestAssignments
            .AsNoTracking()
            .Include(x => x.Employee)
            .ThenInclude(x => x!.PositionRef)
            .Include(x => x.Test)
            .ThenInclude(x => x!.TrainingMaterial)
            .ThenInclude(x => x!.UploadedByUser)
            .Include(x => x.Test)
            .ThenInclude(x => x!.CreatedByUser)
            .Where(x => x.Employee != null && x.Test != null && x.Employee.PositionRef != null)
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
            assignments = assignments.Where(x => x.Employee != null && x.Employee.Department.ToLower().Contains(department.ToLower()));
        }

        if (category.HasValue)
        {
            assignments = assignments.Where(x => x.Test != null && x.Test.Category == category.Value);
        }

        return assignments
            .OrderBy(x => x.Employee != null ? x.Employee.Department : string.Empty)
            .ThenBy(x => x.Employee != null ? x.Employee.LastName : string.Empty)
            .ThenBy(x => x.Test != null ? x.Test.Title : string.Empty)
            .Select(x => new TestResultReportDto(
                x.Id,
                x.EmployeeId,
                x.Employee != null ? x.Employee.LastName + " " + x.Employee.FirstName + " " + (x.Employee.MiddleName ?? string.Empty) : string.Empty,
                x.Employee != null ? x.Employee.Email : string.Empty,
                x.Test != null && x.Test.TrainingMaterial != null ? x.Test.TrainingMaterial.UploadedByUserId : x.Test != null ? x.Test.CreatedByUserId : 0,
                x.Test != null && x.Test.TrainingMaterial != null && x.Test.TrainingMaterial.UploadedByUser != null
                    ? x.Test.TrainingMaterial.UploadedByUser.LastName + " " + x.Test.TrainingMaterial.UploadedByUser.FirstName + " " + (x.Test.TrainingMaterial.UploadedByUser.MiddleName ?? string.Empty)
                    : x.Test != null && x.Test.CreatedByUser != null
                        ? x.Test.CreatedByUser.LastName + " " + x.Test.CreatedByUser.FirstName + " " + (x.Test.CreatedByUser.MiddleName ?? string.Empty)
                        : string.Empty,
                x.Test != null && x.Test.TrainingMaterial != null && x.Test.TrainingMaterial.UploadedByUser != null
                    ? x.Test.TrainingMaterial.UploadedByUser.Email
                    : x.Test != null && x.Test.CreatedByUser != null
                        ? x.Test.CreatedByUser.Email
                        : string.Empty,
                x.Employee != null ? x.Employee.Department : string.Empty,
                x.Employee != null && x.Employee.PositionRef != null ? x.Employee.PositionRef.Name : string.Empty,
                x.TestId,
                x.Test != null ? x.Test.Title : string.Empty,
                x.Test != null ? x.Test.Category : Domain.Enums.InstructionCategory.OccupationalSafety,
                x.InstructionType,
                x.Status.ToString(),
                x.LastScorePercent,
                x.LastScorePercent.HasValue && x.Test != null ? x.LastScorePercent.Value >= x.Test.PassingScorePercent : null,
                x.AssignedAt,
                x.Deadline,
                x.CompletedAt,
                x.NextRetrainingDueAt));
    }

    private static byte[] BuildExcelFile(
        List<TestResultReportDto> rows,
        Domain.Enums.InstructionCategory category)
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
                <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:x14ac="http://schemas.microsoft.com/office/spreadsheetml/2009/9/ac">
                  <fonts count="3" x14ac:knownFonts="1">
                    <font><sz val="11"/><name val="Calibri"/></font>
                    <font><sz val="11"/><name val="Calibri"/><family val="2"/><charset val="204"/></font>
                    <font><sz val="12"/><color rgb="FF000000"/><name val="Times New Roman"/><family val="1"/><charset val="204"/></font>
                  </fonts>
                  <fills count="2">
                    <fill><patternFill patternType="none"/></fill>
                    <fill><patternFill patternType="gray125"/></fill>
                  </fills>
                  <borders count="13">
                    <border><left/><right/><top/><bottom/><diagonal/></border>
                    <border><left style="medium"><color rgb="FF000000"/></left><right style="medium"><color rgb="FF000000"/></right><top style="medium"><color rgb="FF000000"/></top><bottom/><diagonal/></border>
                    <border><left style="medium"><color rgb="FF000000"/></left><right style="medium"><color rgb="FF000000"/></right><top/><bottom/><diagonal/></border>
                    <border><left/><right style="medium"><color rgb="FF000000"/></right><top style="medium"><color rgb="FF000000"/></top><bottom/><diagonal/></border>
                    <border><left/><right style="medium"><color rgb="FF000000"/></right><top/><bottom/><diagonal/></border>
                    <border><left/><right style="medium"><color rgb="FF000000"/></right><top/><bottom style="medium"><color rgb="FF000000"/></bottom><diagonal/></border>
                    <border><left/><right/><top style="medium"><color rgb="FF000000"/></top><bottom/><diagonal/></border>
                    <border><left/><right/><top/><bottom style="medium"><color rgb="FF000000"/></bottom><diagonal/></border>
                    <border><left style="medium"><color rgb="FF000000"/></left><right style="medium"><color rgb="FF000000"/></right><top/><bottom style="medium"><color rgb="FF000000"/></bottom><diagonal/></border>
                    <border><left style="medium"><color rgb="FF000000"/></left><right/><top style="medium"><color rgb="FF000000"/></top><bottom/><diagonal/></border>
                    <border><left style="medium"><color rgb="FF000000"/></left><right/><top/><bottom/><diagonal/></border>
                    <border><left style="medium"><color rgb="FF000000"/></left><right/><top/><bottom style="medium"><color rgb="FF000000"/></bottom><diagonal/></border>
                    <border><left style="medium"><color rgb="FF000000"/></left><right style="medium"><color rgb="FF000000"/></right><top style="medium"><color rgb="FF000000"/></top><bottom style="medium"><color rgb="FF000000"/></bottom><diagonal/></border>
                  </borders>
                  <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
                  <cellXfs count="21">
                    <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
                    <xf numFmtId="0" fontId="2" fillId="0" borderId="1" xfId="0" applyFont="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="0" fillId="0" borderId="5" xfId="0" applyBorder="1" applyAlignment="1"><alignment vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="2" fillId="0" borderId="5" xfId="0" applyFont="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="2" fillId="0" borderId="8" xfId="0" applyFont="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="14" fontId="2" fillId="0" borderId="8" xfId="0" applyNumberFormat="1" applyFont="1" applyBorder="1" applyAlignment="1"><alignment vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="2" fillId="0" borderId="5" xfId="0" applyFont="1" applyBorder="1" applyAlignment="1"><alignment vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="2" fillId="0" borderId="8" xfId="0" applyFont="1" applyBorder="1" applyAlignment="1"><alignment vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="0" fillId="0" borderId="12" xfId="0" applyBorder="1"/>
                    <xf numFmtId="0" fontId="2" fillId="0" borderId="1" xfId="0" applyFont="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="2" fillId="0" borderId="2" xfId="0" applyFont="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="1" fillId="0" borderId="12" xfId="0" applyFont="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="2" fillId="0" borderId="9" xfId="0" applyFont="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="2" fillId="0" borderId="3" xfId="0" applyFont="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="2" fillId="0" borderId="10" xfId="0" applyFont="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="2" fillId="0" borderId="4" xfId="0" applyFont="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="2" fillId="0" borderId="11" xfId="0" applyFont="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="2" fillId="0" borderId="5" xfId="0" applyFont="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="2" fillId="0" borderId="6" xfId="0" applyFont="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="2" fillId="0" borderId="0" xfId="0" applyFont="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
                    <xf numFmtId="0" fontId="2" fillId="0" borderId="7" xfId="0" applyFont="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
                  </cellXfs>
                  <cellStyles count="1"><cellStyle name="Обычный" xfId="0" builtinId="0"/></cellStyles>
                  <dxfs count="0"/>
                  <tableStyles count="0" defaultTableStyle="TableStyleMedium2" defaultPivotStyle="PivotStyleLight16"/>
                </styleSheet>
                """);

            AddEntry(archive, "xl/worksheets/sheet1.xml", BuildWorksheetXml(rows, category));
        }

        return stream.ToArray();
    }

    private static string BuildWorksheetXml(
        List<TestResultReportDto> rows,
        Domain.Enums.InstructionCategory category)
    {
        return category switch
        {
            Domain.Enums.InstructionCategory.FireSafety
                or Domain.Enums.InstructionCategory.OccupationalSafety => BuildSafetyWorksheetXml(rows),
            Domain.Enums.InstructionCategory.ElectricalSafety => BuildElectricalSafetyWorksheetXml(rows),
            _ => BuildDefaultWorksheetXml(rows)
        };
    }

    private static string BuildDefaultWorksheetXml(List<TestResultReportDto> rows)
    {
        var headers = new[]
        {
            "Сотрудник",
            "Отдел",
            "Должность",
            "Направление",
            "Вид инструктажа",
            "Тест",
            "Статус",
            "Балл",
            "Пройден",
            "Назначен",
            "Дата прохождения",
            "Следующий инструктаж"
        };

        var sheet = new System.Text.StringBuilder();
        sheet.Append("""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <sheetViews><sheetView workbookViewId="0"/></sheetViews>
              <sheetFormatPr defaultRowHeight="15"/>
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
                InstructionLabels.Category(row.Category),
                InstructionLabels.Type(row.InstructionType),
                row.TestTitle,
                StatusLabel(row.Status),
                row.ScorePercent?.ToString() ?? string.Empty,
                row.IsPassed switch { true => "Да", false => "Нет", _ => string.Empty },
                FormatDate(row.AssignedAt),
                row.CompletedAt.HasValue ? FormatDate(row.CompletedAt.Value) : string.Empty,
                row.NextRetrainingDueAt.HasValue ? FormatDate(row.NextRetrainingDueAt.Value) : string.Empty
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

    private static string BuildSafetyWorksheetXml(List<TestResultReportDto> rows)
    {
        var sheet = new System.Text.StringBuilder();
        sheet.Append("""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <cols>
                <col min="1" max="1" width="11.29" customWidth="1"/>
                <col min="2" max="2" width="14.43" customWidth="1"/>
                <col min="3" max="3" width="11.29" customWidth="1"/>
                <col min="4" max="4" width="12.29" customWidth="1"/>
                <col min="5" max="5" width="24.71" customWidth="1"/>
                <col min="6" max="6" width="19.14" customWidth="1"/>
                <col min="7" max="7" width="21.43" customWidth="1"/>
                <col min="8" max="8" width="25.29" customWidth="1"/>
                <col min="9" max="9" width="31.57" customWidth="1"/>
                <col min="10" max="10" width="18.71" customWidth="1"/>
                <col min="11" max="11" width="19.71" customWidth="1"/>
                <col min="12" max="12" width="18.5" customWidth="1"/>
              </cols>
              <sheetData>
            """);

        AddSafetyHeader(sheet);

        for (var i = 0; i < rows.Count; i++)
        {
            var rowNumber = i + 7;
            var row = rows[i];
            var values = new[]
            {
                FormatDate(row.AssignedAt),
                InstructionLabels.Type(row.InstructionType),
                row.EmployeeFullName,
                row.Position,
                row.InstructorFullName,
                row.EmployeeEmail,
                row.InstructorEmail,
                row.CompletedAt.HasValue ? FormatDate(row.CompletedAt.Value) : string.Empty,
                row.InstructorFullName,
                row.EmployeeEmail,
                row.InstructorEmail,
                row.NextRetrainingDueAt.HasValue ? FormatDate(row.NextRetrainingDueAt.Value) : string.Empty
            };

            sheet.Append($"<row r=\"{rowNumber}\" spans=\"1:12\" ht=\"48\">");
            for (var column = 0; column < values.Length; column++)
            {
                sheet.Append(Cell(rowNumber, column + 1, values[column], SafetyDataStyle(column + 1)));
            }
            sheet.Append("</row>");
        }

        sheet.Append("""
              </sheetData>
              <mergeCells count="13">
                <mergeCell ref="I4:I5"/>
                <mergeCell ref="L1:L6"/>
                <mergeCell ref="H1:H5"/>
                <mergeCell ref="A1:A5"/>
                <mergeCell ref="B1:B5"/>
                <mergeCell ref="C4:C5"/>
                <mergeCell ref="D4:D5"/>
                <mergeCell ref="E4:E5"/>
                <mergeCell ref="C1:D3"/>
                <mergeCell ref="E1:G3"/>
                <mergeCell ref="I1:K3"/>
                <mergeCell ref="F4:G4"/>
                <mergeCell ref="J4:K4"/>
              </mergeCells>
              <pageMargins left="0.7" right="0.7" top="0.75" bottom="0.75" header="0.3" footer="0.3"/>
              <pageSetup paperSize="9" orientation="portrait"/>
            </worksheet>
            """);

        return sheet.ToString();
    }

    private static string BuildElectricalSafetyWorksheetXml(List<TestResultReportDto> rows)
    {
        var sheet = new System.Text.StringBuilder();
        sheet.Append("""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <sheetViews><sheetView workbookViewId="0"/></sheetViews>
              <sheetFormatPr defaultRowHeight="15"/>
              <cols>
                <col min="1" max="1" width="8.5" customWidth="1"/>
                <col min="2" max="2" width="16.29" customWidth="1"/>
                <col min="3" max="3" width="19.86" customWidth="1"/>
                <col min="4" max="4" width="24.43" customWidth="1"/>
                <col min="5" max="5" width="35.29" customWidth="1"/>
                <col min="6" max="6" width="22" customWidth="1"/>
                <col min="7" max="7" width="19.29" customWidth="1"/>
              </cols>
              <sheetData>
            """);

        AddElectricalSafetyHeader(sheet);

        for (var i = 0; i < rows.Count; i++)
        {
            var rowNumber = i + 3;
            var row = rows[i];
            var values = new[]
            {
                (i + 1).ToString(),
                $"{row.EmployeeFullName}\n{row.Position}",
                string.Empty,
                $"{FormatDate(row.AssignedAt)}\n{InstructionLabels.Type(row.InstructionType)}",
                $"{StatusLabel(row.Status)}; {ResultLabel(row)}",
                row.EmployeeEmail,
                row.NextRetrainingDueAt.HasValue ? FormatDate(row.NextRetrainingDueAt.Value) : string.Empty
            };

            sheet.Append($"<row r=\"{rowNumber}\" spans=\"1:7\" ht=\"48\" customHeight=\"1\">");
            for (var column = 0; column < values.Length; column++)
            {
                sheet.Append(Cell(rowNumber, column + 1, values[column], 3));
            }
            sheet.Append("</row>");
        }

        sheet.Append("""
              </sheetData>
              <pageMargins left="0.7" right="0.7" top="0.75" bottom="0.75" header="0.3" footer="0.3"/>
              <pageSetup paperSize="9" orientation="portrait"/>
            </worksheet>
            """);

        return sheet.ToString();
    }

    private static void AddElectricalSafetyHeader(System.Text.StringBuilder sheet)
    {
        var headers = new[]
        {
            "№ п/п",
            "ФИО, должность",
            "Дата предыдущей проверки(при наличии)",
            "дата и причина проверки",
            "Оценка знаний, группа по электробезопасности и заключение по проверке знаний",
            "подпись",
            "дата след проверки"
        };

        sheet.Append("<row r=\"1\" spans=\"1:7\" ht=\"45\" customHeight=\"1\">");
        for (var column = 1; column <= headers.Length; column++)
        {
            sheet.Append(Cell(1, column, headers[column - 1], column is 3 or 5 ? 2 : 1));
        }
        sheet.Append("</row>");

        sheet.Append("<row r=\"2\" spans=\"1:7\">");
        for (var column = 1; column <= headers.Length; column++)
        {
            sheet.Append(Cell(2, column, column.ToString(), 3));
        }
        sheet.Append("</row>");
    }

    private static void AddSafetyHeader(System.Text.StringBuilder sheet)
    {
        AddSafetyHeaderRow(sheet, 1, "15.75", new Dictionary<int, (string Value, int Style)>
        {
            [1] = ("Дата", 9),
            [2] = ("Вид проводимого инструктажа", 9),
            [3] = ("Инструктируемый", 12),
            [4] = (string.Empty, 13),
            [5] = ("Теоретическая часть", 12),
            [6] = (string.Empty, 18),
            [7] = (string.Empty, 13),
            [8] = ("Дата", 9),
            [9] = ("Практическая часть", 12),
            [10] = (string.Empty, 18),
            [11] = (string.Empty, 13),
            [12] = ("Дата следующей проверки", 11)
        });

        AddSafetyHeaderRow(sheet, 2, "15.75", new Dictionary<int, (string Value, int Style)>
        {
            [1] = (string.Empty, 10),
            [2] = (string.Empty, 10),
            [3] = (string.Empty, 14),
            [4] = (string.Empty, 15),
            [5] = (string.Empty, 14),
            [6] = (string.Empty, 19),
            [7] = (string.Empty, 15),
            [8] = (string.Empty, 10),
            [9] = (string.Empty, 14),
            [10] = (string.Empty, 19),
            [11] = (string.Empty, 15),
            [12] = (string.Empty, 11)
        });

        AddSafetyHeaderRow(sheet, 3, "15.75", new Dictionary<int, (string Value, int Style)>
        {
            [1] = (string.Empty, 10),
            [2] = (string.Empty, 10),
            [3] = (string.Empty, 16),
            [4] = (string.Empty, 17),
            [5] = (string.Empty, 16),
            [6] = (string.Empty, 20),
            [7] = (string.Empty, 17),
            [8] = (string.Empty, 10),
            [9] = (string.Empty, 16),
            [10] = (string.Empty, 20),
            [11] = (string.Empty, 17),
            [12] = (string.Empty, 11)
        });

        AddSafetyHeaderRow(sheet, 4, "16.5", new Dictionary<int, (string Value, int Style)>
        {
            [1] = (string.Empty, 10),
            [2] = (string.Empty, 10),
            [3] = ("Фамилия, имя, отчество (при наличии)", 9),
            [4] = ("Профессия, должность", 9),
            [5] = ("Фамилия, имя, отчество (при наличии) инструктирующего, номер документа об образовании и (или) квалификации или документа об обучении инструктирующего", 9),
            [6] = ("Подпись", 12),
            [7] = (string.Empty, 13),
            [8] = (string.Empty, 10),
            [9] = ("Фамилия, имя, отчество (при наличии) инструктирующего, номер документа об образовании и (или) квалификации или документа об обучении инструктирующего", 9),
            [10] = ("Подпись", 12),
            [11] = (string.Empty, 13),
            [12] = (string.Empty, 11)
        });

        AddSafetyHeaderRow(sheet, 5, "180", new Dictionary<int, (string Value, int Style)>
        {
            [1] = (string.Empty, 10),
            [2] = (string.Empty, 10),
            [3] = (string.Empty, 10),
            [4] = (string.Empty, 10),
            [5] = (string.Empty, 10),
            [6] = ("инструктируемого\n(подписано с помощью ПЭП (логин: …))", 1),
            [7] = ("инструктирующего\n(подписано с помощью ПЭП (логин: …))", 1),
            [8] = (string.Empty, 10),
            [9] = (string.Empty, 10),
            [10] = ("инструктируемого\n(подписано с помощью ПЭП (логин: …))", 1),
            [11] = ("инструктирующего\n(подписано с помощью ПЭП (логин: …))", 1),
            [12] = (string.Empty, 11)
        });

        sheet.Append("<row r=\"6\" spans=\"1:12\" ht=\"31.5\" customHeight=\"1\">");
        for (var column = 1; column <= 11; column++)
        {
            sheet.Append(Cell(6, column, column.ToString(), column == 1 ? 4 : 3));
        }
        sheet.Append(Cell(6, 12, string.Empty, 11));
        sheet.Append("</row>");

    }

    private static void AddSafetyHeaderRow(
        System.Text.StringBuilder sheet,
        int rowNumber,
        string height,
        Dictionary<int, (string Value, int Style)> cells)
    {
        sheet.Append($"<row r=\"{rowNumber}\" spans=\"1:12\" ht=\"{height}\" customHeight=\"1\">");
        for (var column = 1; column <= 12; column++)
        {
            var cell = cells[column];
            sheet.Append(Cell(rowNumber, column, cell.Value, cell.Style));
        }
        sheet.Append("</row>");
    }

    private static int SafetyDataStyle(int column) => column switch
    {
        1 or 8 => 7,
        6 or 7 or 10 or 11 => 3,
        12 => 8,
        _ => 2
    };

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

    private static string ResultLabel(TestResultReportDto row)
    {
        var score = row.ScorePercent.HasValue ? $"Балл: {row.ScorePercent.Value}" : "Балл: -";
        var passed = row.IsPassed switch
        {
            true => "пройден",
            false => "не пройден",
            _ => "без результата"
        };

        return $"{score}; {passed}";
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
