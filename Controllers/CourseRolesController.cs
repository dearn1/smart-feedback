using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using smart_feedback.Data;
using smart_feedback.Models;
using smart_feedback.Models.Configuration;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
    
namespace smart_feedback.Controllers
{
    public class CourseRolesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CourseRolesController> _logger;
        private readonly ApplicationSettings _appSettings;

        public CourseRolesController(ApplicationDbContext context, ILogger<CourseRolesController> logger, IOptions<ApplicationSettings> appSettings)
        {
            _context = context;
            _logger = logger;
            _appSettings = appSettings.Value;
        }

        // GET: CourseRoles
        public async Task<IActionResult> Index(string sortOrder, int? year, int? trimester, string programme)
        {
            try
            {
                _logger.LogInformation("CourseRoles Index called with filters - Year: {Year}, Trimester: {Trimester}, Programme: {Programme}, SortOrder: {SortOrder}",
                    year, trimester, programme, sortOrder);

                // Set up ViewData for sorting links
                ViewData["CurrentSort"] = sortOrder;
                ViewData["CourseCodeSortParm"] = string.IsNullOrEmpty(sortOrder) ? "courseCode_desc" : "";
                ViewData["CourseNameSortParm"] = sortOrder == "courseName" ? "courseName_desc" : "courseName";
                ViewData["YearSortParm"] = sortOrder == "year" ? "year_desc" : "year";
                ViewData["TrimesterSortParm"] = sortOrder == "trimester" ? "trimester_desc" : "trimester";
                ViewData["ProgrammeSortParm"] = sortOrder == "programme" ? "programme_desc" : "programme";
                ViewData["TotalAssessmentSortParm"] = sortOrder == "totalAssessment" ? "totalAssessment_desc" : "totalAssessment";
                ViewData["StatusSortParm"] = sortOrder == "status" ? "status_desc" : "status";

                // Set up ViewData for current filter values
                ViewData["CurrentYearFilter"] = year;
                ViewData["CurrentTrimesterFilter"] = trimester;
                ViewData["CurrentProgrammeFilter"] = programme;

                // Prepare programme dropdown options
                ViewBag.ProgrammeOptions = _appSettings.GetProgrammeSelectList(programme);

                // Get distinct years and trimesters for dropdowns
                ViewBag.Years = new SelectList(await _context.CourseRoles
                    .Select(cr => cr.Year)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .ToListAsync());

                ViewBag.Trimesters = new SelectList(await _context.CourseRoles
                    .Select(cr => cr.Trimester)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToListAsync());

                // Preserve current filter values for dropdowns
                ViewBag.CurrentYear = year;
                ViewBag.CurrentTrimester = trimester;

                // Start with all course roles
                var courseRolesQuery = _context.CourseRoles.AsQueryable();

                // Apply filters
                if (year.HasValue)
                {
                    courseRolesQuery = courseRolesQuery.Where(cr => cr.Year == year.Value);
                    _logger.LogDebug("Applied year filter: {Year}", year);
                }

                if (trimester.HasValue)
                {
                    courseRolesQuery = courseRolesQuery.Where(cr => cr.Trimester == trimester.Value);
                    _logger.LogDebug("Applied trimester filter: {Trimester}", trimester);
                }

                if (!string.IsNullOrEmpty(programme))
                {
                    courseRolesQuery = courseRolesQuery.Where(cr => cr.Programme.Equals(programme));
                    _logger.LogDebug("Applied programme filter: {Programme}", programme);
                }

                // Apply sorting
                switch (sortOrder)
                {
                    case "courseCode_desc":
                        courseRolesQuery = courseRolesQuery.OrderByDescending(cr => cr.CourseCode);
                        break;
                    case "courseName":
                        courseRolesQuery = courseRolesQuery.OrderBy(cr => cr.CourseName);
                        break;
                    case "courseName_desc":
                        courseRolesQuery = courseRolesQuery.OrderByDescending(cr => cr.CourseName);
                        break;
                    case "year":
                        courseRolesQuery = courseRolesQuery.OrderBy(cr => cr.Year);
                        break;
                    case "year_desc":
                        courseRolesQuery = courseRolesQuery.OrderByDescending(cr => cr.Year);
                        break;
                    case "trimester":
                        courseRolesQuery = courseRolesQuery.OrderBy(cr => cr.Trimester);
                        break;
                    case "trimester_desc":
                        courseRolesQuery = courseRolesQuery.OrderByDescending(cr => cr.Trimester);
                        break;
                    case "programme":
                        courseRolesQuery = courseRolesQuery.OrderBy(cr => cr.Programme);
                        break;
                    case "programme_desc":
                        courseRolesQuery = courseRolesQuery.OrderByDescending(cr => cr.Programme);
                        break;
                    case "totalAssessment":
                        courseRolesQuery = courseRolesQuery.OrderBy(cr => cr.TotalAssessment);
                        break;
                    case "totalAssessment_desc":
                        courseRolesQuery = courseRolesQuery.OrderByDescending(cr => cr.TotalAssessment);
                        break;
                    case "status":
                        courseRolesQuery = courseRolesQuery.OrderBy(cr => cr.Status);
                        break;
                    case "status_desc":
                        courseRolesQuery = courseRolesQuery.OrderByDescending(cr => cr.Status);
                        break;
                    default:
                        courseRolesQuery = courseRolesQuery.OrderBy(cr => cr.CourseCode);
                        break;
                }

                var courseRoles = await courseRolesQuery.ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} course roles (filtered: Year={HasYearFilter}, Trimester={HasTrimesterFilter}, Programme={HasProgrammeFilter})",
                    courseRoles.Count, year.HasValue, trimester.HasValue, !string.IsNullOrEmpty(programme));

                return View(courseRoles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving course roles with filters - Year: {Year}, Trimester: {Trimester}, Programme: {Programme}",
                    year, trimester, programme);
                throw;
            }
        }

        // GET: CourseRoles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Course role details requested with null ID");
                return NotFound();
            }

            try
            {
                var courseRoles = await _context.CourseRoles
                    .FirstOrDefaultAsync(m => m.CourseRolesId == id);
                if (courseRoles == null)
                {
                    _logger.LogWarning("Course role with ID {Id} not found", id);
                    return NotFound();
                }

                _logger.LogInformation("Successfully retrieved course role details for ID: {Id}, Course: {CourseCode}",
                    id, courseRoles.CourseCode);
                return View(courseRoles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving course role details for ID: {Id}", id);
                throw;
            }
        }

        // GET: CourseRoles/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CourseRoles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CourseRolesId,CourseCode,CourseName,Year,Trimester,Programme,Institution,RoleLecturer,RoleModerator,TotalAssessment,Status")] CourseRoles courseRoles)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Set default status if not provided
                    if (string.IsNullOrEmpty(courseRoles.Status))
                    {
                        courseRoles.Status = "Active";
                    }

                    _context.Add(courseRoles);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully created course role with ID: {Id}, Course: {CourseCode}, Year: {Year}, Trimester: {Trimester}, Status: {Status}",
                        courseRoles.CourseRolesId, courseRoles.CourseCode, courseRoles.Year, courseRoles.Trimester, courseRoles.Status);

                    TempData["SuccessMessage"] = $"Course role '{courseRoles.CourseCode}' has been successfully created.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while creating course role for course: {CourseCode}", courseRoles?.CourseCode);
                    throw;
                }
            }
            
            _logger.LogWarning("Course role creation failed - ModelState is invalid. Course: {CourseCode}, Errors: {Errors}",
                courseRoles?.CourseCode,
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                        
            return View(courseRoles);
        }

        // GET: CourseRoles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Course role edit requested with null ID");
                return NotFound();
            }

            try
            {
                var courseRoles = await _context.CourseRoles.FindAsync(id);
                if (courseRoles == null)
                {
                    _logger.LogWarning("Course role with ID {Id} not found for editing", id);
                    return NotFound();
                }

                _logger.LogInformation("Successfully retrieved course role for editing - ID: {Id}, Course: {CourseCode}",
                    id, courseRoles.CourseCode);
                return View(courseRoles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving course role for editing - ID: {Id}", id);
                throw;
            }
        }

        // POST: CourseRoles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CourseRolesId,CourseCode,CourseName,Year,Trimester,Programme,Institution,RoleLecturer,RoleModerator,TotalAssessment,Status")] CourseRoles courseRoles)
        {
            if (id != courseRoles.CourseRolesId)
            {
                _logger.LogWarning("Course role edit failed - ID mismatch. URL ID: {UrlId}, Model ID: {ModelId}",
                    id, courseRoles.CourseRolesId);
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(courseRoles);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Successfully updated course role - ID: {Id}, Course: {CourseCode}, Year: {Year}, Trimester: {Trimester}, Status: {Status}",
                        id, courseRoles.CourseCode, courseRoles.Year, courseRoles.Trimester, courseRoles.Status);

                    TempData["SuccessMessage"] = $"Course role '{courseRoles.CourseCode}' has been successfully updated.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!CourseRolesExists(courseRoles.CourseRolesId))
                    {
                        _logger.LogWarning("Course role with ID {Id} no longer exists during update", courseRoles.CourseRolesId);
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError(ex, "Concurrency error occurred while updating course role - ID: {Id}", id);
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while updating course role - ID: {Id}", id);
                    throw;
                }
            }

            _logger.LogWarning("Course role update failed - ModelState is invalid. ID: {Id}, Course: {CourseCode}, Errors: {Errors}",
                id, courseRoles?.CourseCode,
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

            return View(courseRoles);
        }

        // GET: CourseRoles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Course role delete requested with null ID");
                return NotFound();
            }

            try
            {
                var courseRoles = await _context.CourseRoles
                    .FirstOrDefaultAsync(m => m.CourseRolesId == id);
                if (courseRoles == null)
                {
                    _logger.LogWarning("Course role with ID {Id} not found for deletion", id);
                    return NotFound();
                }

                _logger.LogInformation("Successfully retrieved course role for deletion confirmation - ID: {Id}, Course: {CourseCode}",
                        id, courseRoles.CourseCode);
                return View(courseRoles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving course role for deletion - ID: {Id}", id);
                throw;
            }
        }

        // POST: CourseRoles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var courseRoles = await _context.CourseRoles.FindAsync(id);
                if (courseRoles != null)
                {
                    _context.CourseRoles.Remove(courseRoles);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Successfully deleted course role - ID: {Id}, Course: {CourseCode}",
                        id, courseRoles.CourseCode);
                    
                    TempData["SuccessMessage"] = $"Course role '{courseRoles.CourseCode}' has been successfully deleted.";
                }
                else
                {
                    _logger.LogWarning("Course role with ID {Id} not found for deletion", id);
                    TempData["ErrorMessage"] = "Course role not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting course role with ID: {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the course role. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: CourseRoles/Archive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id)
        {
            try
            {
                var courseRoles = await _context.CourseRoles.FindAsync(id);
                if (courseRoles == null)
                {
                    _logger.LogWarning("Course role with ID {Id} not found for archiving", id);
                    TempData["ErrorMessage"] = "Course role not found.";
                    return RedirectToAction(nameof(Index));
                }

                courseRoles.Status = "Archived";
                _context.Update(courseRoles);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully archived course role - ID: {Id}, Course: {CourseCode}",
                    id, courseRoles.CourseCode);

                TempData["SuccessMessage"] = $"Course role '{courseRoles.CourseCode}' has been successfully archived.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while archiving course role with ID: {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while archiving the course role. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: CourseRoles/Unarchive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unarchive(int id)
        {
            try
            {
                var courseRoles = await _context.CourseRoles.FindAsync(id);
                if (courseRoles == null)
                {
                    _logger.LogWarning("Course role with ID {Id} not found for unarchiving", id);
                    TempData["ErrorMessage"] = "Course role not found.";
                    return RedirectToAction(nameof(Index));
                }

                courseRoles.Status = "Active";
                _context.Update(courseRoles);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully unarchived course role - ID: {Id}, Course: {CourseCode}",
                    id, courseRoles.CourseCode);

                TempData["SuccessMessage"] = $"Course role '{courseRoles.CourseCode}' has been successfully activated.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while unarchiving course role with ID: {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while unarchiving the course role. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: CourseRoles/BulkArchive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkArchive(List<int> selectedIds)
        {
            try
            {
                _logger.LogInformation("BulkArchive called with {Count} IDs: {Ids}", 
                    selectedIds?.Count ?? 0, 
                    selectedIds != null ? string.Join(", ", selectedIds) : "null");

                if (selectedIds == null || !selectedIds.Any())
                {
                    _logger.LogWarning("Bulk archive attempted with no selections");
                    TempData["ErrorMessage"] = "Please select at least one course role to archive.";
                    return RedirectToAction(nameof(Index));
                }

                var courseRolesToArchive = await _context.CourseRoles
                    .Where(cr => selectedIds.Contains(cr.CourseRolesId) && cr.Status == "Active")
                    .ToListAsync();

                if (!courseRolesToArchive.Any())
                {
                    _logger.LogWarning("No active course roles found for bulk archive with IDs: {Ids}", string.Join(", ", selectedIds));
                    TempData["ErrorMessage"] = "No active course roles found to archive.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var courseRole in courseRolesToArchive)
                {
                    courseRole.Status = "Archived";
                }

                _context.UpdateRange(courseRolesToArchive);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully archived {Count} course roles. IDs: {Ids}",
                    courseRolesToArchive.Count, string.Join(", ", courseRolesToArchive.Select(cr => cr.CourseRolesId)));

                TempData["SuccessMessage"] = $"Successfully archived {courseRolesToArchive.Count} course role(s).";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during bulk archive operation");
                TempData["ErrorMessage"] = "An error occurred while archiving the course roles. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: CourseRoles/BulkUnarchive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUnarchive(List<int> selectedIds)
        {
            try
            {
                if (selectedIds == null || !selectedIds.Any())
                {
                    _logger.LogWarning("Bulk unarchive attempted with no selections");
                    TempData["ErrorMessage"] = "Please select at least one course role to unarchive.";
                    return RedirectToAction(nameof(Index));
                }

                var courseRolesToUnarchive = await _context.CourseRoles
                    .Where(cr => selectedIds.Contains(cr.CourseRolesId) && cr.Status == "Archived")
                    .ToListAsync();

                if (!courseRolesToUnarchive.Any())
                {
                    _logger.LogWarning("No archived course roles found for bulk unarchive with IDs: {Ids}", string.Join(", ", selectedIds));
                    TempData["ErrorMessage"] = "No archived course roles found to unarchive.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var courseRole in courseRolesToUnarchive)
                {
                    courseRole.Status = "Active";
                }

                _context.UpdateRange(courseRolesToUnarchive);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully unarchived {Count} course roles. IDs: {Ids}",
                    courseRolesToUnarchive.Count, string.Join(", ", courseRolesToUnarchive.Select(cr => cr.CourseRolesId)));

                TempData["SuccessMessage"] = $"Successfully unarchived {courseRolesToUnarchive.Count} course role(s).";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during bulk unarchive operation");
                TempData["ErrorMessage"] = "An error occurred while unarchiving the course roles. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: CourseRoles/Upload
        public IActionResult Upload()
        {
            return View();
        }

        // GET: CourseRoles/DownloadTemplate
        public IActionResult DownloadTemplate()
        {
            try
            {
                _logger.LogInformation("Downloading Excel template for course roles upload");

                IWorkbook workbook = new XSSFWorkbook();
                ISheet worksheet = workbook.CreateSheet("CourseRoles");

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
                var headers = new[] { "Course Code", "Course Name", "Year", "Trimester", "Programme", "Institution", "Role Lecturer", "Role Moderator", "Total Assessment", "Status" };
                for (int i = 0; i < headers.Length; i++)
                {
                    ICell cell = headerRow.CreateCell(i);
                    cell.SetCellValue(headers[i]);
                    cell.CellStyle = headerStyle;
                }

                // Add sample data row 1
                IRow row1 = worksheet.CreateRow(1);
                var sampleData1 = new object[] { "CS101", "Introduction to Programming", 2025, 1, "Bachelor of Computer Science", "XYZ University", "lecturer1@university.edu", "moderator1@university.edu", 5, "Active" };
                for (int i = 0; i < sampleData1.Length; i++)
                {
                    ICell cell = row1.CreateCell(i);
                    if (sampleData1[i] is int intValue)
                        cell.SetCellValue(intValue);
                    else
                        cell.SetCellValue(sampleData1[i].ToString());
                    cell.CellStyle = yellowStyle;
                }

                // Add sample data row 2
                IRow row2 = worksheet.CreateRow(2);
                var sampleData2 = new object[] { "CS102", "Data Structures", 2025, 2, "Bachelor of Computer Science", "XYZ University", "lecturer2@university.edu", "moderator2@university.edu", 3, "Active" };
                for (int i = 0; i < sampleData2.Length; i++)
                {
                    ICell cell = row2.CreateCell(i);
                    if (sampleData2[i] is int intValue)
                        cell.SetCellValue(intValue);
                    else
                        cell.SetCellValue(sampleData2[i].ToString());
                    cell.CellStyle = yellowStyle;
                }

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
                    "1. Column A (Course Code) is REQUIRED",
                    "2. Column B (Course Name) is REQUIRED",
                    "3. Column C (Year) is REQUIRED and must be a 4-digit year (e.g., 2025)",
                    "4. Column D (Trimester) is REQUIRED and must be 1, 2, or 3",
                    "5. Column E (Programme) is REQUIRED",
                    "6. Column F (Institution) is REQUIRED",
                    "7. Column G (Role Lecturer) is REQUIRED (email format recommended)",
                    "8. Column H (Role Moderator) is REQUIRED (email format recommended)",
                    "9. Column I (Total Assessment) is OPTIONAL (default: 0, must be a number)",
                    "10. Column J (Status) is OPTIONAL (default: Active, allowed values: Active or Archived)",
                    "11. Delete the sample rows (2 and 3) and add your actual course role data",
                    "12. Rows with missing required fields will be rejected"
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
                    var fileName = $"CourseRolesUploadTemplate_{DateTime.Now:yyyyMMdd}.xlsx";
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

        // POST: CourseRoles/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile excelFile)
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

                var courseRolesToAdd = new List<CourseRoles>();
                var rowErrors = new List<string>();
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

                    for (int row = startRow; row <= rowCount; row++)
                    {
                        rowNumber = row + 1; // +1 for display (Excel rows start at 1)

                        IRow currentRow = worksheet.GetRow(row);
                        if (currentRow == null) continue;

                        var courseCodeCell = GetCellValue(currentRow.GetCell(0))?.Trim();
                        var courseNameCell = GetCellValue(currentRow.GetCell(1))?.Trim();
                        var yearCell = GetCellValue(currentRow.GetCell(2))?.Trim();
                        var trimesterCell = GetCellValue(currentRow.GetCell(3))?.Trim();
                        var programmeCell = GetCellValue(currentRow.GetCell(4))?.Trim();
                        var institutionCell = GetCellValue(currentRow.GetCell(5))?.Trim();
                        var roleLecturerCell = GetCellValue(currentRow.GetCell(6))?.Trim();
                        var roleModeratorCell = GetCellValue(currentRow.GetCell(7))?.Trim();
                        var totalAssessmentCell = GetCellValue(currentRow.GetCell(8))?.Trim();
                        var statusCell = GetCellValue(currentRow.GetCell(9))?.Trim();

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

                        if (string.IsNullOrWhiteSpace(institutionCell))
                        {
                            rowErrors.Add($"Row {rowNumber}: Missing Institution");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(roleLecturerCell))
                        {
                            rowErrors.Add($"Row {rowNumber}: Missing Role Lecturer");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(roleModeratorCell))
                        {
                            rowErrors.Add($"Row {rowNumber}: Missing Role Moderator");
                            continue;
                        }

                        // Validate Year
                        if (string.IsNullOrWhiteSpace(yearCell) || !int.TryParse(yearCell, out int year))
                        {
                            rowErrors.Add($"Row {rowNumber}: Invalid Year '{yearCell}' (must be a number)");
                            continue;
                        }

                        if (year < 1900 || year > 2100)
                        {
                            rowErrors.Add($"Row {rowNumber}: Year '{year}' is out of valid range (1900-2100)");
                            continue;
                        }

                        // Validate Trimester
                        if (string.IsNullOrWhiteSpace(trimesterCell) || !int.TryParse(trimesterCell, out int trimester))
                        {
                            rowErrors.Add($"Row {rowNumber}: Invalid Trimester '{trimesterCell}' (must be a number)");
                            continue;
                        }

                        if (trimester < 1 || trimester > 3)
                        {
                            rowErrors.Add($"Row {rowNumber}: Trimester must be 1, 2, or 3 (got '{trimester}')");
                            continue;
                        }

                        // Validate Total Assessment (optional, default to 0)
                        int totalAssessment = 0;
                        if (!string.IsNullOrWhiteSpace(totalAssessmentCell))
                        {
                            if (!int.TryParse(totalAssessmentCell, out totalAssessment))
                            {
                                rowErrors.Add($"Row {rowNumber}: Invalid Total Assessment '{totalAssessmentCell}' (must be a number)");
                                continue;
                            }

                            if (totalAssessment < 0)
                            {
                                rowErrors.Add($"Row {rowNumber}: Total Assessment cannot be negative");
                                continue;
                            }
                        }

                        // Validate Status (optional, default to "Active")
                        string status = "Active";
                        if (!string.IsNullOrWhiteSpace(statusCell))
                        {
                            if (statusCell.Equals("Active", StringComparison.OrdinalIgnoreCase))
                            {
                                status = "Active";
                            }
                            else if (statusCell.Equals("Archived", StringComparison.OrdinalIgnoreCase))
                            {
                                status = "Archived";
                            }
                            else
                            {
                                rowErrors.Add($"Row {rowNumber}: Invalid Status '{statusCell}' (must be 'Active' or 'Archived')");
                                continue;
                            }
                        }

                        // Create course role object
                        var courseRole = new CourseRoles
                        {
                            CourseCode = courseCodeCell,
                            CourseName = courseNameCell,
                            Year = year,
                            Trimester = trimester,
                            Programme = programmeCell,
                            Institution = institutionCell,
                            RoleLecturer = roleLecturerCell,
                            RoleModerator = roleModeratorCell,
                            TotalAssessment = totalAssessment,
                            Status = status
                        };

                        courseRolesToAdd.Add(courseRole);
                    }
                }

                _logger.LogInformation("Extracted {Count} valid course roles from Excel file, {ErrorCount} rows with errors",
                    courseRolesToAdd.Count, rowErrors.Count);

                // Build result message
                var messages = new List<string>();

                if (courseRolesToAdd.Any())
                {
                    _context.CourseRoles.AddRange(courseRolesToAdd);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Successfully added {Count} course roles from Excel file", courseRolesToAdd.Count);
                    messages.Add($"✅ Successfully added {courseRolesToAdd.Count} course role(s) from Excel file.");
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

                if (!courseRolesToAdd.Any() && !messages.Any())
                {
                    TempData["ErrorMessage"] = "No valid course role records found in the Excel file. Please check the format and validation requirements.";
                }
                else if (messages.Any())
                {
                    if (courseRolesToAdd.Any())
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

        // GET: CourseRoles/DownloadData
        public async Task<IActionResult> DownloadData(string sortOrder, int? year, int? trimester, string programme)
        {
            try
            {
                _logger.LogInformation("Downloading Excel data for course roles with filters - Year: {Year}, Trimester: {Trimester}, Programme: {Programme}, SortOrder: {SortOrder}",
                    year, trimester, programme, sortOrder);

                // Start with all course roles
                var courseRolesQuery = _context.CourseRoles.AsQueryable();

                // Apply filters
                if (year.HasValue)
                {
                    courseRolesQuery = courseRolesQuery.Where(cr => cr.Year == year.Value);
                }

                if (trimester.HasValue)
                {
                    courseRolesQuery = courseRolesQuery.Where(cr => cr.Trimester == trimester.Value);
                }

                if (!string.IsNullOrEmpty(programme))
                {
                    courseRolesQuery = courseRolesQuery.Where(cr => cr.Programme.Equals(programme));
                }

                // Apply sorting
                switch (sortOrder)
                {
                    case "courseCode_desc":
                        courseRolesQuery = courseRolesQuery.OrderByDescending(cr => cr.CourseCode);
                        break;
                    case "courseName":
                        courseRolesQuery = courseRolesQuery.OrderBy(cr => cr.CourseName);
                        break;
                    case "courseName_desc":
                        courseRolesQuery = courseRolesQuery.OrderByDescending(cr => cr.CourseName);
                        break;
                    case "year":
                        courseRolesQuery = courseRolesQuery.OrderBy(cr => cr.Year);
                        break;
                    case "year_desc":
                        courseRolesQuery = courseRolesQuery.OrderByDescending(cr => cr.Year);
                        break;
                    case "trimester":
                        courseRolesQuery = courseRolesQuery.OrderBy(cr => cr.Trimester);
                        break;
                    case "trimester_desc":
                        courseRolesQuery = courseRolesQuery.OrderByDescending(cr => cr.Trimester);
                        break;
                    case "programme":
                        courseRolesQuery = courseRolesQuery.OrderBy(cr => cr.Programme);
                        break;
                    case "programme_desc":
                        courseRolesQuery = courseRolesQuery.OrderByDescending(cr => cr.Programme);
                        break;
                    case "totalAssessment":
                        courseRolesQuery = courseRolesQuery.OrderBy(cr => cr.TotalAssessment);
                        break;
                    case "totalAssessment_desc":
                        courseRolesQuery = courseRolesQuery.OrderByDescending(cr => cr.TotalAssessment);
                        break;
                    case "status":
                        courseRolesQuery = courseRolesQuery.OrderBy(cr => cr.Status);
                        break;
                    case "status_desc":
                        courseRolesQuery = courseRolesQuery.OrderByDescending(cr => cr.Status);
                        break;
                    default:
                        courseRolesQuery = courseRolesQuery.OrderBy(cr => cr.CourseCode);
                        break;
                }

                var courseRoles = await courseRolesQuery.ToListAsync();

                _logger.LogInformation("Retrieved {Count} course roles for Excel export", courseRoles.Count);

                IWorkbook workbook = new XSSFWorkbook();
                ISheet worksheet = workbook.CreateSheet("CourseRoles");

                // Create header style
                ICellStyle headerStyle = workbook.CreateCellStyle();
                headerStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Blue.Index;
                headerStyle.FillPattern = FillPattern.SolidForeground;
                IFont headerFont = workbook.CreateFont();
                headerFont.Color = NPOI.HSSF.Util.HSSFColor.White.Index;
                headerFont.IsBold = true;
                headerStyle.SetFont(headerFont);

                // Create data cell style
                ICellStyle dataCellStyle = workbook.CreateCellStyle();

                // Create header row
                IRow headerRow = worksheet.CreateRow(0);
                var headers = new[] { "Course Code", "Course Name", "Year", "Trimester", "Programme", "Institution", "Role Lecturer", "Role Moderator", "Total Assessment", "Status" };
                for (int i = 0; i < headers.Length; i++)
                {
                    ICell cell = headerRow.CreateCell(i);
                    cell.SetCellValue(headers[i]);
                    cell.CellStyle = headerStyle;
                }

                // Add data rows
                int rowIndex = 1;
                foreach (var courseRole in courseRoles)
                {
                    IRow dataRow = worksheet.CreateRow(rowIndex);
                    
                    dataRow.CreateCell(0).SetCellValue(courseRole.CourseCode ?? "");
                    dataRow.CreateCell(1).SetCellValue(courseRole.CourseName ?? "");
                    dataRow.CreateCell(2).SetCellValue(courseRole.Year);
                    dataRow.CreateCell(3).SetCellValue(courseRole.Trimester);
                    dataRow.CreateCell(4).SetCellValue(courseRole.Programme ?? "");
                    dataRow.CreateCell(5).SetCellValue(courseRole.Institution ?? "");
                    dataRow.CreateCell(6).SetCellValue(courseRole.RoleLecturer ?? "");
                    dataRow.CreateCell(7).SetCellValue(courseRole.RoleModerator ?? "");
                    dataRow.CreateCell(8).SetCellValue(courseRole.TotalAssessment);
                    dataRow.CreateCell(9).SetCellValue(courseRole.Status ?? "Active");

                    rowIndex++;
                }

                // Auto-size columns
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.AutoSizeColumn(i);
                }

                // Write to memory stream
                using (var stream = new MemoryStream())
                {
                    workbook.Write(stream);
                    
                    // Build filename with filter information
                    var filterInfo = new List<string>();
                    if (year.HasValue) filterInfo.Add($"Year{year}");
                    if (trimester.HasValue) filterInfo.Add($"T{trimester}");
                    if (!string.IsNullOrEmpty(programme)) filterInfo.Add(programme.Replace(" ", ""));
                    
                    var filterSuffix = filterInfo.Any() ? "_" + string.Join("_", filterInfo) : "";
                    var fileName = $"CourseRoles{filterSuffix}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    
                    _logger.LogInformation("Successfully generated Excel file: {FileName} with {Count} records", fileName, courseRoles.Count);
                    
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Excel data file");
                TempData["ErrorMessage"] = "Error generating data file.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Helper method to check if course role exists
        private bool CourseRolesExists(int id)
        {
            try
            {
                var exists = _context.CourseRoles.Any(e => e.CourseRolesId == id);
                _logger.LogDebug("Course role exists check for ID {Id}: {Exists}", id, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if course role exists for ID: {Id}", id);
                return false;
            }
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
