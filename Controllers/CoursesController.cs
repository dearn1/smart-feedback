using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using smart_feedback.Data;
using smart_feedback.Models;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using System.IO;

namespace smart_feedback.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CoursesController> _logger;

        public CoursesController(ApplicationDbContext context, ILogger<CoursesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Courses
        public async Task<IActionResult> Index(string searchString, string programme)
        {
            ViewData["Title"] = "Course List Management";
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentProgramme"] = programme;

            var courses = from c in _context.Courses
                         select c;

            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                courses = courses.Where(c => c.CourseCode.Contains(searchString) 
                                          || c.CourseName.Contains(searchString) 
                                          || c.Programme.Contains(searchString));
            }

            // Apply programme filter
            if (!string.IsNullOrEmpty(programme))
            {
                courses = courses.Where(c => c.Programme == programme);
            }

            // Get distinct programmes for dropdown
            ViewBag.Programmes = new SelectList(await _context.Courses
                .Select(c => c.Programme)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync());

            return View(await courses.OrderBy(c => c.CourseCode).ToListAsync());
        }

        // GET: Courses/Create
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Create Course";
            
            // Get programmes from database table
            var programmes = await _context.Programmes
                .OrderBy(p => p.ProgrammeName)
                .ToListAsync();
            
            ViewBag.Programmes = new SelectList(programmes, "ProgrammeName", "ProgrammeName");
            
            return View();
        }

        // POST: Courses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CourseCode,CourseName,Programme")] Course course)
        {
            if (ModelState.IsValid)
            {
                _context.Add(course);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Course created successfully.";
                return RedirectToAction(nameof(Index));
            }
            
            ViewData["Title"] = "Create Course";
            
            // Reload programmes dropdown on validation error
            var programmes = await _context.Programmes
                .OrderBy(p => p.ProgrammeName)
                .ToListAsync();
            
            ViewBag.Programmes = new SelectList(programmes, "ProgrammeName", "ProgrammeName", course.Programme);
            
            return View(course);
        }

        // GET: Courses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }
            
            ViewData["Title"] = "Edit Course";
            
            // Get programmes from database table
            var programmes = await _context.Programmes
                .OrderBy(p => p.ProgrammeName)
                .ToListAsync();
            
            ViewBag.Programmes = new SelectList(programmes, "ProgrammeName", "ProgrammeName", course.Programme);
            
            return View(course);
        }

        // POST: Courses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CourseCode,CourseName,Programme")] Course course)
        {
            if (id != course.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(course);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Course updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.Id))
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
            
            ViewData["Title"] = "Edit Course";
            
            // Reload programmes dropdown on validation error
            var programmes = await _context.Programmes
                .OrderBy(p => p.ProgrammeName)
                .ToListAsync();
            
            ViewBag.Programmes = new SelectList(programmes, "ProgrammeName", "ProgrammeName", course.Programme);
            
            return View(course);
        }

        // GET: Courses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(m => m.Id == id);
            if (course == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Delete Course";
            return View(course);
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Course deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Courses/DownloadTemplate
        public IActionResult DownloadTemplate()
        {
            try
            {
                _logger.LogInformation("Downloading Excel template for courses upload");

                IWorkbook workbook = new XSSFWorkbook();
                ISheet worksheet = workbook.CreateSheet("Courses");

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
                var headers = new[] { "Course Code", "Course Name", "Programme" };
                for (int i = 0; i < headers.Length; i++)
                {
                    ICell cell = headerRow.CreateCell(i);
                    cell.SetCellValue(headers[i]);
                    cell.CellStyle = headerStyle;
                }

                // Add sample data rows
                IRow row1 = worksheet.CreateRow(1);
                ICell cell1_0 = row1.CreateCell(0);
                cell1_0.SetCellValue("CS101");
                cell1_0.CellStyle = yellowStyle;

                ICell cell1_1 = row1.CreateCell(1);
                cell1_1.SetCellValue("Introduction to Programming");
                cell1_1.CellStyle = yellowStyle;

                ICell cell1_2 = row1.CreateCell(2);
                cell1_2.SetCellValue("Bachelor of Computer Science");
                cell1_2.CellStyle = yellowStyle;

                IRow row2 = worksheet.CreateRow(2);
                ICell cell2_0 = row2.CreateCell(0);
                cell2_0.SetCellValue("CS102");
                cell2_0.CellStyle = yellowStyle;

                ICell cell2_1 = row2.CreateCell(1);
                cell2_1.SetCellValue("Data Structures and Algorithms");
                cell2_1.CellStyle = yellowStyle;

                ICell cell2_2 = row2.CreateCell(2);
                cell2_2.SetCellValue("Bachelor of Computer Science");
                cell2_2.CellStyle = yellowStyle;

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
                    "1. Column A (Course Code) is REQUIRED and must be unique (max 20 characters)",
                    "2. Column B (Course Name) is REQUIRED",
                    "3. Column C (Programme) is REQUIRED (max 100 characters)",
                    "4. Delete the sample rows (2 and 3) and add your actual course data",
                    "5. Courses with duplicate Course Codes will be skipped",
                    "6. Rows with missing required fields will be rejected"
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
                    var fileName = $"CourseUploadTemplate_{DateTime.Now:yyyyMMdd}.xlsx";
                    
                    _logger.LogInformation("Course upload template generated successfully: {FileName}", fileName);
                    
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating course upload template");
                TempData["ErrorMessage"] = "Error generating template file.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Courses/UploadExcel
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

                var coursesToAdd = new List<Course>();
                var rowErrors = new List<string>();
                var duplicateCourses = new List<string>();
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
                        if (firstCell != null && (firstCell.Contains("course") || firstCell.Contains("code")))
                        {
                            startRow = 1;
                            _logger.LogDebug("Header row detected, starting from row 1");
                        }
                    }

                    // Get existing course codes for duplicate check
                    var existingCourseCodes = await _context.Courses
                        .Select(c => c.CourseCode.ToLower())
                        .ToListAsync();

                    for (int row = startRow; row <= rowCount; row++)
                    {
                        rowNumber = row + 1; // +1 for display (Excel rows start at 1)

                        IRow currentRow = worksheet.GetRow(row);
                        if (currentRow == null) continue;

                        var courseCodeCell = GetCellValue(currentRow.GetCell(0))?.Trim();
                        var courseNameCell = GetCellValue(currentRow.GetCell(1))?.Trim();
                        var programmeCell = GetCellValue(currentRow.GetCell(2))?.Trim();

                        // Skip empty rows
                        if (string.IsNullOrWhiteSpace(courseCodeCell) &&
                            string.IsNullOrWhiteSpace(courseNameCell) &&
                            string.IsNullOrWhiteSpace(programmeCell))
                        {
                            _logger.LogDebug("Skipping empty row {Row}", rowNumber);
                            continue;
                        }

                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(courseCodeCell))
                        {
                            rowErrors.Add($"Row {rowNumber}: Missing Course Code");
                            continue;
                        }

                        if (courseCodeCell.Length > 20)
                        {
                            rowErrors.Add($"Row {rowNumber}: Course Code '{courseCodeCell}' exceeds 20 characters");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(courseNameCell))
                        {
                            rowErrors.Add($"Row {rowNumber}: Missing Course Name");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(programmeCell))
                        {
                            rowErrors.Add($"Row {rowNumber}: Missing Programme");
                            continue;
                        }

                        if (programmeCell.Length > 100)
                        {
                            rowErrors.Add($"Row {rowNumber}: Programme '{programmeCell}' exceeds 100 characters");
                            continue;
                        }

                        // Check for duplicate course code in database
                        if (existingCourseCodes.Contains(courseCodeCell.ToLower()))
                        {
                            duplicateCourses.Add(courseCodeCell);
                            _logger.LogDebug("Row {Row}: Duplicate Course Code '{CourseCode}'", rowNumber, courseCodeCell);
                            continue;
                        }

                        // Check for duplicates within the uploaded file
                        if (coursesToAdd.Any(c => c.CourseCode.Equals(courseCodeCell, StringComparison.OrdinalIgnoreCase)))
                        {
                            rowErrors.Add($"Row {rowNumber}: Duplicate Course Code '{courseCodeCell}' within the file");
                            continue;
                        }

                        // Create course object
                        var course = new Course
                        {
                            CourseCode = courseCodeCell,
                            CourseName = courseNameCell,
                            Programme = programmeCell
                        };

                        coursesToAdd.Add(course);
                    }
                }

                _logger.LogInformation("Extracted {Count} valid courses from Excel file, {ErrorCount} rows with errors, {DuplicateCount} duplicates",
                    coursesToAdd.Count, rowErrors.Count, duplicateCourses.Count);

                // Build result message
                var messages = new List<string>();

                if (coursesToAdd.Any())
                {
                    _context.Courses.AddRange(coursesToAdd);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Successfully added {Count} courses from Excel file", coursesToAdd.Count);
                    messages.Add($"✅ Successfully added {coursesToAdd.Count} course(s) from Excel file.");
                }

                if (duplicateCourses.Any())
                {
                    var duplicateList = string.Join(", ", duplicateCourses.Take(5));
                    if (duplicateCourses.Count > 5)
                    {
                        duplicateList += $" and {duplicateCourses.Count - 5} more";
                    }
                    messages.Add($"ℹ️ {duplicateCourses.Count} course(s) skipped (already exist): {duplicateList}");
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

                if (!coursesToAdd.Any() && !duplicateCourses.Any())
                {
                    TempData["ErrorMessage"] = "No valid course records found in the Excel file. Please check the format and validation requirements.";
                }
                else if (messages.Any())
                {
                    if (coursesToAdd.Any())
                    {
                        TempData["Success"] = string.Join("<br/><br/>", messages);
                    }
                    else
                    {
                        TempData["WarningMessage"] = string.Join("<br/><br/>", messages);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Excel file for courses upload");
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

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }
    }
}