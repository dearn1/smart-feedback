using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using smart_feedback.Data;
using smart_feedback.Models;

namespace smart_feedback.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProgrammesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProgrammesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Programmes
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["Title"] = "Programme List Management";
            ViewData["CurrentFilter"] = searchString;

            var programmes = from p in _context.Programmes
                            select p;

            if (!string.IsNullOrEmpty(searchString))
            {
                programmes = programmes.Where(p => p.ProgrammeName.Contains(searchString));
            }

            return View(await programmes.OrderBy(p => p.ProgrammeName).ToListAsync());
        }

        // GET: Programmes/Create
        public IActionResult Create()
        {
            ViewData["Title"] = "Create Programme";
            return View();
        }

        // POST: Programmes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProgrammeName")] Programme programme)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate programme name
                var existingProgramme = await _context.Programmes
                    .FirstOrDefaultAsync(p => p.ProgrammeName == programme.ProgrammeName);

                if (existingProgramme != null)
                {
                    ModelState.AddModelError("ProgrammeName", "A programme with this name already exists.");
                    ViewData["Title"] = "Create Programme";
                    return View(programme);
                }

                _context.Add(programme);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Programme created successfully.";
                return RedirectToAction(nameof(Index));
            }
            ViewData["Title"] = "Create Programme";
            return View(programme);
        }

        // GET: Programmes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var programme = await _context.Programmes.FindAsync(id);
            if (programme == null)
            {
                return NotFound();
            }
            ViewData["Title"] = "Edit Programme";
            return View(programme);
        }

        // POST: Programmes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ProgrammeName")] Programme programme)
        {
            if (id != programme.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate programme name (excluding current programme)
                    var existingProgramme = await _context.Programmes
                        .FirstOrDefaultAsync(p => p.ProgrammeName == programme.ProgrammeName && p.Id != id);

                    if (existingProgramme != null)
                    {
                        ModelState.AddModelError("ProgrammeName", "A programme with this name already exists.");
                        ViewData["Title"] = "Edit Programme";
                        return View(programme);
                    }

                    _context.Update(programme);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Programme updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProgrammeExists(programme.Id))
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
            ViewData["Title"] = "Edit Programme";
            return View(programme);
        }

        // GET: Programmes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var programme = await _context.Programmes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (programme == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Delete Programme";
            return View(programme);
        }

        // POST: Programmes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var programme = await _context.Programmes.FindAsync(id);
            if (programme != null)
            {
                // Check if programme is being used in CourseRoles
                var usedInCourseRoles = await _context.CourseRoles
                    .AnyAsync(cr => cr.Programme == programme.ProgrammeName);

                // Check if programme is being used in Students
                var usedInStudents = await _context.Student
                    .AnyAsync(s => s.Programme == programme.ProgrammeName);

                // Check if programme is being used in Courses
                var usedInCourses = await _context.Courses
                    .AnyAsync(c => c.Programme == programme.ProgrammeName);

                if (usedInCourseRoles || usedInStudents || usedInCourses)
                {
                    TempData["Error"] = "Cannot delete this programme because it is currently being used by course roles, students, or courses.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Programmes.Remove(programme);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Programme deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Programmes/Upload
        public IActionResult Upload()
        {
            return View();
        }

        // GET: Programmes/DownloadTemplate
        public IActionResult DownloadTemplate()
        {
            try
            {
                IWorkbook workbook = new XSSFWorkbook();
                ISheet worksheet = workbook.CreateSheet("Programmes");

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
                ICell headerCell = headerRow.CreateCell(0);
                headerCell.SetCellValue("Programme Name");
                headerCell.CellStyle = headerStyle;

                // Add sample data rows
                IRow row1 = worksheet.CreateRow(1);
                ICell cell1 = row1.CreateCell(0);
                cell1.SetCellValue("Bachelor of Computer Science");
                cell1.CellStyle = yellowStyle;

                IRow row2 = worksheet.CreateRow(2);
                ICell cell2 = row2.CreateCell(0);
                cell2.SetCellValue("Bachelor of Business Administration");
                cell2.CellStyle = yellowStyle;

                IRow row3 = worksheet.CreateRow(3);
                ICell cell3 = row3.CreateCell(0);
                cell3.SetCellValue("Bachelor of Information Technology");
                cell3.CellStyle = yellowStyle;

                // Auto-size column
                worksheet.AutoSizeColumn(0);
                worksheet.SetColumnWidth(0, 15000); // Set minimum width

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
                    "1. Column A (Programme Name) is REQUIRED and must not be empty",
                    "2. Programme names must be unique - duplicates will be skipped",
                    "3. Maximum length for Programme Name is 200 characters",
                    "4. Delete the sample rows (2, 3, and 4) and add your actual programme data",
                    "5. Rows with missing Programme Name will be rejected",
                    "6. The upload will skip any programme names that already exist in the database"
                };

                for (int i = 0; i < instructions.Length; i++)
                {
                    IRow instructionRow = instructionsSheet.CreateRow(1 + i);
                    instructionRow.CreateCell(0).SetCellValue(instructions[i]);
                }

                instructionsSheet.SetColumnWidth(0, 20000);

                // Write to memory stream
                using (var stream = new MemoryStream())
                {
                    workbook.Write(stream);
                    var fileName = $"ProgrammesUploadTemplate_{DateTime.Now:yyyyMMdd}.xlsx";
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error generating template file.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Programmes/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile excelFile)
        {
            try
            {
                if (excelFile == null || excelFile.Length <= 0)
                {
                    TempData["Error"] = "Please select a valid Excel file.";
                    return RedirectToAction(nameof(Index));
                }

                // Check file extension
                var extension = Path.GetExtension(excelFile.FileName).ToLower();
                if (extension != ".xlsx" && extension != ".xls")
                {
                    TempData["Error"] = "Only Excel files (.xlsx, .xls) are allowed.";
                    return RedirectToAction(nameof(Index));
                }

                // Check file size (limit to 5MB)
                if (excelFile.Length > 5 * 1024 * 1024)
                {
                    TempData["Error"] = "File size must be less than 5MB.";
                    return RedirectToAction(nameof(Index));
                }

                var programmesToAdd = new List<Programme>();
                var rowErrors = new List<string>();
                var skippedDuplicates = new List<string>();
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

                    // Determine start row (skip header if present)
                    int startRow = 0;
                    IRow firstRow = worksheet.GetRow(0);
                    if (firstRow != null)
                    {
                        var firstCell = GetCellValue(firstRow.GetCell(0))?.ToLower();
                        if (firstCell != null && firstCell.Contains("programme"))
                        {
                            startRow = 1;
                        }
                    }

                    // Get existing programme names for duplicate checking
                    var existingProgrammeNames = await _context.Programmes
                        .Select(p => p.ProgrammeName.ToLower())
                        .ToListAsync();

                    for (int row = startRow; row <= rowCount; row++)
                    {
                        rowNumber = row + 1; // +1 for display (Excel rows start at 1)

                        IRow currentRow = worksheet.GetRow(row);
                        if (currentRow == null) continue;

                        var programmeNameCell = GetCellValue(currentRow.GetCell(0))?.Trim();

                        // Skip empty rows
                        if (string.IsNullOrWhiteSpace(programmeNameCell))
                        {
                            continue;
                        }

                        // Validate programme name
                        if (programmeNameCell.Length > 200)
                        {
                            rowErrors.Add($"Row {rowNumber}: Programme name exceeds 200 characters");
                            continue;
                        }

                        // Check for duplicates in database
                        if (existingProgrammeNames.Contains(programmeNameCell.ToLower()))
                        {
                            skippedDuplicates.Add($"Row {rowNumber}: '{programmeNameCell}'");
                            continue;
                        }

                        // Check for duplicates in current batch
                        if (programmesToAdd.Any(p => p.ProgrammeName.Equals(programmeNameCell, StringComparison.OrdinalIgnoreCase)))
                        {
                            skippedDuplicates.Add($"Row {rowNumber}: '{programmeNameCell}' (duplicate in file)");
                            continue;
                        }

                        // Create programme object
                        var programme = new Programme
                        {
                            ProgrammeName = programmeNameCell
                        };

                        programmesToAdd.Add(programme);
                    }
                }

                // Build result message
                var messages = new List<string>();

                if (programmesToAdd.Any())
                {
                    _context.Programmes.AddRange(programmesToAdd);
                    await _context.SaveChangesAsync();
                    messages.Add($"✅ Successfully added {programmesToAdd.Count} programme(s) from Excel file.");
                }

                if (skippedDuplicates.Any())
                {
                    var duplicateList = string.Join("<br/>", skippedDuplicates.Take(10));
                    if (skippedDuplicates.Count > 10)
                    {
                        duplicateList += $"<br/>...and {skippedDuplicates.Count - 10} more duplicate(s)";
                    }
                    messages.Add($"⚠️ Skipped {skippedDuplicates.Count} duplicate programme(s):<br/>{duplicateList}");
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

                if (!programmesToAdd.Any() && !messages.Any())
                {
                    TempData["Error"] = "No valid programme records found in the Excel file. Please check the format and validation requirements.";
                }
                else if (messages.Any())
                {
                    if (programmesToAdd.Any())
                    {
                        TempData["Success"] = string.Join("<br/><br/>", messages);
                    }
                    else
                    {
                        TempData["Warning"] = string.Join("<br/><br/>", messages);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while processing the Excel file: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProgrammeExists(int id)
        {
            return _context.Programmes.Any(e => e.Id == id);
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
    }
}