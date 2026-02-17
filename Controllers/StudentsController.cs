using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using smart_feedback.Data;
using smart_feedback.Models;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;

namespace smart_feedback.Controllers
{
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(ApplicationDbContext context, ILogger<StudentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Students
        public async Task<IActionResult> Index(string programme, int? yearEnrolled, int? trimesterEnrolled)
        {
            var students = from s in _context.Student
                           select s;

            // Apply filters
            if (!string.IsNullOrEmpty(programme))
            {
                students = students.Where(s => s.Programme == programme);
            }

            if (yearEnrolled.HasValue)
            {
                students = students.Where(s => s.YearEnrolled == yearEnrolled.Value);
            }

            if (trimesterEnrolled.HasValue)
            {
                students = students.Where(s => s.TrimesterEnrolled == trimesterEnrolled.Value);
            }

            // Get distinct values for dropdowns
            ViewBag.Programmes = new SelectList(await _context.Student
                .Select(s => s.Programme)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync());

            ViewBag.Years = new SelectList(await _context.Student
                .Select(s => s.YearEnrolled)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync());

            ViewBag.Trimesters = new SelectList(await _context.Student
                .Select(s => s.TrimesterEnrolled)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync());

            // Preserve current filter values
            ViewBag.CurrentProgramme = programme;
            ViewBag.CurrentYear = yearEnrolled;
            ViewBag.CurrentTrimester = trimesterEnrolled;

            return View(await students.ToListAsync());
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Student
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // GET: Students/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Students/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,StudentId,Name,Email,Programme,YearEnrolled,TrimesterEnrolled")] Student student)
        {
            if (ModelState.IsValid)
            {
                _context.Add(student);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(student);
        }

        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Student.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        // POST: Students/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StudentId,Name,Email,Programme,YearEnrolled,TrimesterEnrolled")] Student student)
        {
            if (id != student.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(student);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(student);
        }

        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Student
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Student.FindAsync(id);
            if (student != null)
            {
                _context.Student.Remove(student);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Students/DownloadTemplate
        public IActionResult DownloadTemplate()
        {
            try
            {
                _logger.LogInformation("Downloading Excel template for student upload");

                IWorkbook workbook = new XSSFWorkbook();
                ISheet worksheet = workbook.CreateSheet("Students");

                // Create header style
                ICellStyle headerStyle = workbook.CreateCellStyle();
                headerStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Blue.Index;
                headerStyle.FillPattern = FillPattern.SolidForeground;
                IFont headerFont = workbook.CreateFont();
                headerFont.Color = NPOI.HSSF.Util.HSSFColor.White.Index;
                headerFont.IsBold = true;
                headerStyle.SetFont(headerFont);

                // Create yellow highlight style for sample data
                ICellStyle yellowStyle = workbook.CreateCellStyle();
                yellowStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Yellow.Index;
                yellowStyle.FillPattern = FillPattern.SolidForeground;

                // Create header row
                IRow headerRow = worksheet.CreateRow(0);
                var headers = new[] { "Student ID", "Name", "Email", "Programme", "Year Enrolled", "Trimester Enrolled" };
                for (int i = 0; i < headers.Length; i++)
                {
                    ICell cell = headerRow.CreateCell(i);
                    cell.SetCellValue(headers[i]);
                    cell.CellStyle = headerStyle;
                }

                // Add sample data rows
                IRow row1 = worksheet.CreateRow(1);
                ICell cell1_0 = row1.CreateCell(0);
                cell1_0.SetCellValue("20250001");
                cell1_0.CellStyle = yellowStyle;

                ICell cell1_1 = row1.CreateCell(1);
                cell1_1.SetCellValue("John Doe");
                cell1_1.CellStyle = yellowStyle;

                ICell cell1_2 = row1.CreateCell(2);
                cell1_2.SetCellValue("john.doe@example.com");
                cell1_2.CellStyle = yellowStyle;

                ICell cell1_3 = row1.CreateCell(3);
                cell1_3.SetCellValue("Bachelor of Computer Science");
                cell1_3.CellStyle = yellowStyle;

                ICell cell1_4 = row1.CreateCell(4);
                cell1_4.SetCellValue(2025);
                cell1_4.CellStyle = yellowStyle;

                ICell cell1_5 = row1.CreateCell(5);
                cell1_5.SetCellValue(1);
                cell1_5.CellStyle = yellowStyle;

                IRow row2 = worksheet.CreateRow(2);
                ICell cell2_0 = row2.CreateCell(0);
                cell2_0.SetCellValue("20250002");
                cell2_0.CellStyle = yellowStyle;

                ICell cell2_1 = row2.CreateCell(1);
                cell2_1.SetCellValue("Jane Smith");
                cell2_1.CellStyle = yellowStyle;

                ICell cell2_2 = row2.CreateCell(2);
                cell2_2.SetCellValue("jane.smith@example.com");
                cell2_2.CellStyle = yellowStyle;

                ICell cell2_3 = row2.CreateCell(3);
                cell2_3.SetCellValue("Bachelor of Information Technology");
                cell2_3.CellStyle = yellowStyle;

                ICell cell2_4 = row2.CreateCell(4);
                cell2_4.SetCellValue(2025);
                cell2_4.CellStyle = yellowStyle;

                ICell cell2_5 = row2.CreateCell(5);
                cell2_5.SetCellValue(2);
                cell2_5.CellStyle = yellowStyle;

                // Auto-size columns
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.AutoSizeColumn(i);
                }

                // Add instructions sheet
                ISheet instructionsSheet = workbook.CreateSheet("Instructions");

                IRow instructionHeaderRow = instructionsSheet.CreateRow(0);
                ICell instructionCell = instructionHeaderRow.CreateCell(0);
                instructionCell.SetCellValue("IMPORTANT INSTRUCTIONS:");
                ICellStyle boldStyle = workbook.CreateCellStyle();
                IFont boldFont = workbook.CreateFont();
                boldFont.IsBold = true;
                boldFont.FontHeightInPoints = 12;
                boldStyle.SetFont(boldFont);
                instructionCell.CellStyle = boldStyle;

                var instructions = new[]
                {
                    "1. Column A (Student ID) is REQUIRED and must be unique",
                    "2. Column B (Name) is REQUIRED",
                    "3. Column C (Email) is REQUIRED and must be a valid email format",
                    "4. Column D (Programme) is REQUIRED",
                    "5. Column E (Year Enrolled) is REQUIRED and must be a 4-digit year (e.g., 2025)",
                    "6. Column F (Trimester Enrolled) is REQUIRED and must be 1, 2, or 3",
                    "7. Delete the sample rows (2 and 3) and add your actual student data",
                    "8. Students with duplicate Student IDs will be skipped",
                    "9. Rows with missing required fields will be rejected"
                };

                for (int i = 0; i < instructions.Length; i++)
                {
                    IRow instructionRow = instructionsSheet.CreateRow(1 + i);
                    instructionRow.CreateCell(0).SetCellValue(instructions[i]);
                }

                instructionsSheet.AutoSizeColumn(0);

                // Write to memory stream
                using (var stream = new MemoryStream())
                {
                    workbook.Write(stream);
                    var fileName = $"StudentUploadTemplate_{DateTime.Now:yyyyMMdd}.xlsx";
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Excel template");
                TempData["ErrorMessage"] = "Error generating template file.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Students/UploadExcel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadExcel(IFormFile excelFile)
        {
            try
            {
                _logger.LogInformation("Excel upload initiated, file: {FileName}, size: {FileSize} bytes",
                    excelFile?.FileName, excelFile?.Length);

                if (excelFile == null || excelFile.Length == 0)
                {
                    _logger.LogWarning("Excel upload attempted with null or empty file");
                    TempData["ErrorMessage"] = "Please select a valid Excel file.";
                    return RedirectToAction(nameof(Index));
                }

                // Check file extension
                var extension = Path.GetExtension(excelFile.FileName).ToLower();
                if (extension != ".xlsx" && extension != ".xls")
                {
                    _logger.LogWarning("Excel upload attempted with invalid file extension: {Extension}", extension);
                    TempData["ErrorMessage"] = "Only Excel files (.xlsx, .xls) are allowed.";
                    return RedirectToAction(nameof(Index));
                }

                // Check file size (limit to 5MB)
                if (excelFile.Length > 5 * 1024 * 1024)
                {
                    _logger.LogWarning("Excel upload attempted with oversized file: {FileSize} bytes", excelFile.Length);
                    TempData["ErrorMessage"] = "File size must be less than 5MB.";
                    return RedirectToAction(nameof(Index));
                }

                var studentsToAdd = new List<Student>();
                var rowErrors = new List<string>();
                var duplicateStudentIds = new List<string>();
                int rowNumber = 1;

                using (var stream = excelFile.OpenReadStream())
                {
                    IWorkbook workbook;

                    // Create appropriate workbook based on file extension
                    if (extension == ".xlsx")
                    {
                        workbook = new XSSFWorkbook(stream);
                    }
                    else
                    {
                        workbook = new HSSFWorkbook(stream);
                    }

                    ISheet worksheet = workbook.GetSheetAt(0);
                    int rowCount = worksheet.LastRowNum;

                    _logger.LogInformation("Processing Excel file with {RowCount} rows", rowCount);

                    // Determine start row (skip header if present)
                    int startRow = 0;
                    IRow firstRow = worksheet.GetRow(0);
                    if (firstRow != null)
                    {
                        var firstCell = GetCellValue(firstRow.GetCell(0))?.ToLower();
                        if (firstCell != null && (firstCell.Contains("student") || firstCell.Contains("id")))
                        {
                            startRow = 1;
                            _logger.LogDebug("Header row detected, starting from row 1");
                        }
                    }

                    // Get existing student IDs for duplicate check
                    var existingStudentIds = await _context.Student
                        .Select(s => s.StudentId.ToLower())
                        .ToListAsync();

                    for (int row = startRow; row <= rowCount; row++)
                    {
                        rowNumber = row + 1; // +1 for display (Excel rows start at 1)

                        IRow currentRow = worksheet.GetRow(row);
                        if (currentRow == null) continue;

                        var studentIdCell = GetCellValue(currentRow.GetCell(0))?.Trim();
                        var nameCell = GetCellValue(currentRow.GetCell(1))?.Trim();
                        var emailCell = GetCellValue(currentRow.GetCell(2))?.Trim();
                        var programmeCell = GetCellValue(currentRow.GetCell(3))?.Trim();
                        var yearEnrolledCell = GetCellValue(currentRow.GetCell(4))?.Trim();
                        var trimesterEnrolledCell = GetCellValue(currentRow.GetCell(5))?.Trim();

                        // Skip empty rows
                        if (string.IsNullOrWhiteSpace(studentIdCell) &&
                            string.IsNullOrWhiteSpace(nameCell) &&
                            string.IsNullOrWhiteSpace(emailCell))
                        {
                            _logger.LogDebug("Skipping empty row {Row}", rowNumber);
                            continue;
                        }

                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(studentIdCell))
                        {
                            rowErrors.Add($"Row {rowNumber}: Missing Student ID");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(nameCell))
                        {
                            rowErrors.Add($"Row {rowNumber}: Missing Name");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(emailCell))
                        {
                            rowErrors.Add($"Row {rowNumber}: Missing Email");
                            continue;
                        }

                        // Validate email format
                        if (!IsValidEmail(emailCell))
                        {
                            rowErrors.Add($"Row {rowNumber}: Invalid Email format '{emailCell}'");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(programmeCell))
                        {
                            rowErrors.Add($"Row {rowNumber}: Missing Programme");
                            continue;
                        }

                        // Validate Year Enrolled
                        if (string.IsNullOrWhiteSpace(yearEnrolledCell) || !int.TryParse(yearEnrolledCell, out int yearEnrolled))
                        {
                            rowErrors.Add($"Row {rowNumber}: Invalid Year Enrolled '{yearEnrolledCell}' (must be a number)");
                            continue;
                        }

                        if (yearEnrolled < 1900 || yearEnrolled > 2100)
                        {
                            rowErrors.Add($"Row {rowNumber}: Year Enrolled '{yearEnrolled}' is out of valid range (1900-2100)");
                            continue;
                        }

                        // Validate Trimester Enrolled
                        if (string.IsNullOrWhiteSpace(trimesterEnrolledCell) || !int.TryParse(trimesterEnrolledCell, out int trimesterEnrolled))
                        {
                            rowErrors.Add($"Row {rowNumber}: Invalid Trimester Enrolled '{trimesterEnrolledCell}' (must be a number)");
                            continue;
                        }

                        if (trimesterEnrolled < 1 || trimesterEnrolled > 3)
                        {
                            rowErrors.Add($"Row {rowNumber}: Trimester Enrolled must be 1, 2, or 3 (got '{trimesterEnrolled}')");
                            continue;
                        }

                        // Check for duplicate Student ID
                        if (existingStudentIds.Contains(studentIdCell.ToLower()))
                        {
                            duplicateStudentIds.Add(studentIdCell);
                            _logger.LogDebug("Row {Row}: Duplicate Student ID '{StudentId}'", rowNumber, studentIdCell);
                            continue;
                        }

                        // Check for duplicates within the uploaded file
                        if (studentsToAdd.Any(s => s.StudentId.Equals(studentIdCell, StringComparison.OrdinalIgnoreCase)))
                        {
                            rowErrors.Add($"Row {rowNumber}: Duplicate Student ID '{studentIdCell}' within the file");
                            continue;
                        }

                        // Create student object
                        var student = new Student
                        {
                            StudentId = studentIdCell,
                            Name = nameCell,
                            Email = emailCell,
                            Programme = programmeCell,
                            YearEnrolled = yearEnrolled,
                            TrimesterEnrolled = trimesterEnrolled
                        };

                        studentsToAdd.Add(student);
                    }
                }

                _logger.LogInformation("Extracted {Count} valid students from Excel file, {ErrorCount} rows with errors, {DuplicateCount} duplicates",
                    studentsToAdd.Count, rowErrors.Count, duplicateStudentIds.Count);

                // Build result message
                var messages = new List<string>();

                if (studentsToAdd.Any())
                {
                    _context.Student.AddRange(studentsToAdd);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Successfully added {Count} students from Excel file", studentsToAdd.Count);
                    messages.Add($"✅ Successfully added {studentsToAdd.Count} student(s) from Excel file.");
                }

                if (duplicateStudentIds.Any())
                {
                    var duplicateList = string.Join(", ", duplicateStudentIds.Take(5));
                    if (duplicateStudentIds.Count > 5)
                    {
                        duplicateList += $" and {duplicateStudentIds.Count - 5} more";
                    }
                    messages.Add($"ℹ️ {duplicateStudentIds.Count} student(s) skipped (already exist): {duplicateList}");
                }

                if (rowErrors.Any())
                {
                    var errorList = string.Join("<br/>", rowErrors.Take(10));
                    if (rowErrors.Count > 10)
                    {
                        errorList += $"<br/>...and {rowErrors.Count - 10} more error(s)";
                    }
                    messages.Add($"❌ {rowErrors.Count} row(s) had validation errors:<br/>{errorList}");
                }

                if (!studentsToAdd.Any() && !duplicateStudentIds.Any())
                {
                    TempData["ErrorMessage"] = "No valid student records found in the Excel file. Please check the format and validation requirements.";
                }
                else if (messages.Any())
                {
                    if (studentsToAdd.Any())
                    {
                        TempData["SuccessMessage"] = string.Join("<br/><br/>", messages);
                    }
                    else
                    {
                        TempData["WarningMessage"] = string.Join("<br/><br/>", messages);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Excel file");
                TempData["ErrorMessage"] = $"An error occurred while processing the Excel file: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // Helper method to get cell value as string
        private string GetCellValue(ICell cell)
        {
            if (cell == null) return null;

            switch (cell.CellType)
            {
                case CellType.String:
                    return cell.StringCellValue;
                case CellType.Numeric:
                    if (DateUtil.IsCellDateFormatted(cell))
                    {
                        return cell.DateCellValue.ToString();
                    }
                    return cell.NumericCellValue.ToString("0");
                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();
                case CellType.Formula:
                    return cell.StringCellValue;
                default:
                    return null;
            }
        }

        // Helper method to validate email format
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool StudentExists(int id)
        {
            return _context.Student.Any(e => e.Id == id);
        }
    }
}
