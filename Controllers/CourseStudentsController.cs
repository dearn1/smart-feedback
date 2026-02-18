using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using smart_feedback.Data;
using smart_feedback.Models;
using smart_feedback.Models.ViewModels;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace smart_feedback.Controllers
{
    [Authorize]
    public class CourseStudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CourseStudentsController> _logger;

        public CourseStudentsController(ApplicationDbContext context, ILogger<CourseStudentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: CourseStudents/Manage
        public async Task<IActionResult> Manage(int courseId, string role)
        {
            _logger.LogInformation("Manage students called for courseId: {CourseId}, role: {Role}", courseId, role);

            var course = await _context.CourseRoles.FindAsync(courseId);
            if (course == null)
            {
                _logger.LogWarning("Course not found with ID: {CourseId}", courseId);
                TempData["ErrorMessage"] = "Course not found.";
                return RedirectToAction("Index", "Home");
            }

            // Get all students in the system
            var allStudents = await _context.Student
                .OrderBy(s => s.StudentId)
                .ToListAsync();

            // Get students enrolled in this course
            var enrolledStudents = await _context.CourseStudent
                .Where(cs => cs.CourseRolesId == courseId)
                .Include(cs => cs.Student)
                .OrderBy(cs => cs.Student.StudentId)
                .Select(cs => new StudentEnrollmentInfo
                {
                    CourseStudentId = cs.CourseStudentId,
                    StudentId = cs.StudentId,
                    StudentIdNumber = cs.Student.StudentId,
                    Name = cs.Student.Name,
                    Email = cs.Student.Email,
                    EnrolledDate = cs.EnrolledDate
                })
                .ToListAsync();

            var enrolledStudentIds = enrolledStudents.Select(es => es.StudentId).ToHashSet();

            // Get available students (not enrolled yet)
            var availableStudents = allStudents
                .Where(s => !enrolledStudentIds.Contains(s.Id))
                .ToList();

            var viewModel = new CourseStudentManagementViewModel
            {
                CourseRolesId = courseId,
                CourseCode = course.CourseCode,
                CourseName = course.CourseName,
                Year = course.Year,
                Trimester = course.Trimester,   
                Programme = course.Programme,
                Role = role,
                AllStudents = allStudents,
                EnrolledStudents = enrolledStudents,
                AvailableStudents = availableStudents
            };

            _logger.LogInformation("Found {TotalStudents} total students, {EnrolledCount} enrolled, {AvailableCount} available",
                allStudents.Count, enrolledStudents.Count, availableStudents.Count);

            return View(viewModel);
        }

        // POST: CourseStudents/AddStudent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudent(int courseId, int studentId, string role)
        {
            try
            {
                _logger.LogInformation("Adding student {StudentId} to course {CourseId}", studentId, courseId);

                // Check if student is already enrolled
                var existingEnrollment = await _context.CourseStudent
                    .FirstOrDefaultAsync(cs => cs.CourseRolesId == courseId && cs.StudentId == studentId);

                if (existingEnrollment != null)
                {
                    _logger.LogWarning("Student {StudentId} already enrolled in course {CourseId}", studentId, courseId);
                    TempData["ErrorMessage"] = "Student is already enrolled in this course.";
                    return RedirectToAction("Manage", new { courseId, role });
                }

                var courseStudent = new CourseStudent
                {
                    CourseRolesId = courseId,
                    StudentId = studentId,
                    EnrolledDate = DateTime.Now
                };

                _context.CourseStudent.Add(courseStudent);
                await _context.SaveChangesAsync();

                var student = await _context.Student.FindAsync(studentId);
                _logger.LogInformation("Successfully enrolled student {StudentName} (ID: {StudentId}) in course {CourseId}",
                    student?.Name, studentId, courseId);

                TempData["SuccessMessage"] = $"Student {student?.Name} successfully enrolled.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling student {StudentId} in course {CourseId}", studentId, courseId);
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Manage", new { courseId, role });
        }

        // POST: CourseStudents/AddMultipleStudents
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMultipleStudents(int courseId, string role, string studentIds)
        {
            try
            {
                _logger.LogInformation("Adding students to course {CourseId}, studentIds raw: {StudentIds}", courseId, studentIds);

                if (string.IsNullOrEmpty(studentIds))
                {
                    TempData["ErrorMessage"] = "No students selected.";
                    return RedirectToAction("Manage", new { courseId, role });
                }

                // Parse the comma-separated string
                var studentIdList = studentIds.Split(',')
                    .Where(id => int.TryParse(id.Trim(), out _))
                    .Select(id => int.Parse(id.Trim()))
                    .ToList();

                if (!studentIdList.Any())
                {
                    TempData["ErrorMessage"] = "Invalid student IDs provided.";
                    return RedirectToAction("Manage", new { courseId, role });
                }

                _logger.LogInformation("Parsed {Count} student IDs", studentIdList.Count);

                // Get existing enrollments
                var existingEnrollments = await _context.CourseStudent
                    .Where(cs => cs.CourseRolesId == courseId && studentIdList.Contains(cs.StudentId))
                    .Select(cs => cs.StudentId)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} existing enrollments", existingEnrollments.Count);

                // Filter out already enrolled students
                var newStudentIds = studentIdList.Except(existingEnrollments).ToList();

                if (!newStudentIds.Any())
                {
                    _logger.LogWarning("All selected students already enrolled in course {CourseId}", courseId);
                    TempData["ErrorMessage"] = "All selected students are already enrolled.";
                    return RedirectToAction("Manage", new { courseId, role });
                }

                var newEnrollments = newStudentIds.Select(studentId => new CourseStudent
                {
                    CourseRolesId = courseId,
                    StudentId = studentId,
                    EnrolledDate = DateTime.Now
                }).ToList();

                _context.CourseStudent.AddRange(newEnrollments);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully enrolled {Count} students in course {CourseId}", newEnrollments.Count, courseId);

                var message = $"Successfully enrolled {newEnrollments.Count} student(s).";
                if (existingEnrollments.Any())
                {
                    message += $" {existingEnrollments.Count} student(s) were already enrolled.";
                }

                TempData["SuccessMessage"] = message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding multiple students to course {CourseId}", courseId);
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Manage", new { courseId, role });
        }

        // POST: CourseStudents/RemoveStudent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveStudent(int courseStudentId, int courseId, string role)
        {
            try
            {
                _logger.LogInformation("Removing enrollment ID: {CourseStudentId}", courseStudentId);

                var courseStudent = await _context.CourseStudent
                    .Include(cs => cs.Student)
                    .FirstOrDefaultAsync(cs => cs.CourseStudentId == courseStudentId);

                if (courseStudent == null)
                {
                    _logger.LogWarning("Enrollment not found with ID: {CourseStudentId}", courseStudentId);
                    TempData["ErrorMessage"] = "Enrollment not found.";
                    return RedirectToAction("Manage", new { courseId, role });
                }

                var studentName = courseStudent.Student?.Name;

                _context.CourseStudent.Remove(courseStudent);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully removed student {StudentName} from course {CourseId}",
                    studentName, courseId);

                TempData["SuccessMessage"] = $"Student {studentName} successfully removed from course.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing enrollment ID: {CourseStudentId}", courseStudentId);
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Manage", new { courseId, role });
        }

        // POST: CourseStudents/UploadExcel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadExcel(int courseId, string role, IFormFile excelFile)
        {
            try
            {
                _logger.LogInformation("Excel upload initiated for course {CourseId}, file: {FileName}, size: {FileSize} bytes",
                    courseId, excelFile?.FileName, excelFile?.Length);

                if (excelFile == null || excelFile.Length == 0)
                {
                    _logger.LogWarning("Excel upload attempted with null or empty file");
                    TempData["ErrorMessage"] = "Please select a valid Excel file.";
                    return RedirectToAction("Manage", new { courseId, role });
                }

                // Check file extension
                var extension = Path.GetExtension(excelFile.FileName).ToLower();
                if (extension != ".xlsx" && extension != ".xls")
                {
                    _logger.LogWarning("Excel upload attempted with invalid file extension: {Extension}", extension);
                    TempData["ErrorMessage"] = "Only Excel files (.xlsx, .xls) are allowed.";
                    return RedirectToAction("Manage", new { courseId, role });
                }

                // Check file size (limit to 5MB)
                if (excelFile.Length > 5 * 1024 * 1024)
                {
                    _logger.LogWarning("Excel upload attempted with oversized file: {FileSize} bytes", excelFile.Length);
                    TempData["ErrorMessage"] = "File size must be less than 5MB.";
                    return RedirectToAction("Manage", new { courseId, role });
                }

                // Get course information for validation
                var course = await _context.CourseRoles.FindAsync(courseId);
                if (course == null)
                {
                    _logger.LogWarning("Course not found with ID: {CourseId}", courseId);
                    TempData["ErrorMessage"] = "Course not found.";
                    return RedirectToAction("Manage", new { courseId, role });
                }

                var studentIdsToEnroll = new List<string>();
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

                    // Expected format: 
                    // Column A = Course Code
                    // Column B = Term Year
                    // Column C = Trimester
                    // Column D = Student ID
                    // Column E = Name (optional)
                    // Column F = Email (optional)

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
                        var termNameCell = GetCellValue(currentRow.GetCell(1))?.Trim();
                        var trimesterCell = GetCellValue(currentRow.GetCell(2))?.Trim();
                        var studentIdCell = GetCellValue(currentRow.GetCell(3))?.Trim();

                        // Skip empty rows
                        if (string.IsNullOrWhiteSpace(courseCodeCell) && 
                            string.IsNullOrWhiteSpace(termNameCell) && 
                            string.IsNullOrWhiteSpace(trimesterCell) &&     
                            string.IsNullOrWhiteSpace(studentIdCell))
                        {
                            _logger.LogDebug("Skipping empty row {Row}", rowNumber);
                            continue;
                        }

                        // Validate Course Code
                        if (string.IsNullOrWhiteSpace(courseCodeCell))
                        {
                            rowErrors.Add($"Row {rowNumber}: Missing Course Code");
                            _logger.LogWarning("Row {Row}: Missing Course Code", rowNumber);
                            continue;
                        }

                        if (!courseCodeCell.Equals(course.CourseCode, StringComparison.OrdinalIgnoreCase))
                        {
                            rowErrors.Add($"Row {rowNumber}: Invalid Course Code '{courseCodeCell}' (Expected: '{course.CourseCode}')");
                            _logger.LogWarning("Row {Row}: Course Code mismatch - got '{CourseCodeCell}', expected '{ExpectedCode}'", 
                                rowNumber, courseCodeCell, course.CourseCode);
                            continue;
                        }

                        // Validate Year
                        if (string.IsNullOrWhiteSpace(termNameCell))
                        {
                            rowErrors.Add($"Row {rowNumber}: Missing Year");
                            _logger.LogWarning("Row {Row}: Missing Year", rowNumber);
                            continue;
                        }

                        if (!termNameCell.Equals(course.Year.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            rowErrors.Add($"Row {rowNumber}: Invalid Year '{termNameCell}' (Expected: '{course.Year}')");
                            _logger.LogWarning("Row {Row}: Year mismatch - got '{YearCell}', expected '{ExpectedYear}'", 
                                rowNumber, termNameCell, course.Year);
                            continue;
                        }

                        // Validate Trimester
                        if (string.IsNullOrWhiteSpace(trimesterCell))
                        {
                            rowErrors.Add($"Row {rowNumber}: Missing Trimester");
                            _logger.LogWarning("Row {Row}: Missing Trimester", rowNumber);
                            continue;
                        }

                        if (!trimesterCell.Equals(course.Trimester.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            rowErrors.Add($"Row {rowNumber}: Invalid Trimester '{trimesterCell}' (Expected: '{course.Trimester}')");
                            _logger.LogWarning("Row {Row}: Trimester mismatch - got '{TrimesterCell}', expected '{ExpectedTrimester}'", 
                                rowNumber, trimesterCell, course.Trimester);
                            continue;
                        }

                        // Validate Student ID
                        if (string.IsNullOrWhiteSpace(studentIdCell))
                        {
                            rowErrors.Add($"Row {rowNumber}: Missing Student ID");
                            _logger.LogWarning("Row {Row}: Missing Student ID", rowNumber);
                            continue;
                        }

                        studentIdsToEnroll.Add(studentIdCell);
                    }
                }

                _logger.LogInformation("Extracted {Count} valid student IDs from Excel file, {ErrorCount} rows with errors",
                    studentIdsToEnroll.Count, rowErrors.Count);

                // If there are validation errors, show them to the user
                if (rowErrors.Any())
                {
                    var errorMessage = $"Found {rowErrors.Count} validation error(s):<br/>" +
                        string.Join("<br/>", rowErrors.Take(10));
                    
                    if (rowErrors.Count > 10)
                    {
                        errorMessage += $"<br/>...and {rowErrors.Count - 10} more error(s)";
                    }

                    if (!studentIdsToEnroll.Any())
                    {
                        _logger.LogWarning("No valid rows found in Excel file due to validation errors");
                        TempData["ErrorMessage"] = errorMessage;
                        return RedirectToAction("Manage", new { courseId, role });
                    }
                    else
                    {
                        TempData["WarningMessage"] = errorMessage + "<br/><br/>Valid rows will still be processed.";
                    }
                }

                if (!studentIdsToEnroll.Any())
                {
                    _logger.LogWarning("No valid student IDs found in Excel file");
                    TempData["ErrorMessage"] = "No valid student records found in the Excel file. Please check the format and validation requirements.";
                    return RedirectToAction("Manage", new { courseId, role });
                }

                // Match student IDs with database
                var matchedStudents = await _context.Student
                    .Where(s => studentIdsToEnroll.Contains(s.StudentId))
                    .ToListAsync();

                var matchedStudentIds = matchedStudents.Select(s => s.StudentId).ToHashSet();
                var notFoundIds = studentIdsToEnroll.Where(id => !matchedStudentIds.Contains(id)).ToList();

                _logger.LogInformation("Matched {MatchedCount} students from Excel, {NotFoundCount} not found",
                    matchedStudents.Count, notFoundIds.Count);

                if (!matchedStudents.Any())
                {
                    _logger.LogWarning("None of the student IDs from Excel file exist in database");
                    TempData["ErrorMessage"] = "None of the student IDs in the Excel file were found in the database.";
                    return RedirectToAction("Manage", new { courseId, role });
                }

                // Check for already enrolled students
                var dbStudentIds = matchedStudents.Select(s => s.Id).ToList();
                var existingEnrollments = await _context.CourseStudent
                    .Where(cs => cs.CourseRolesId == courseId && dbStudentIds.Contains(cs.StudentId))
                    .Select(cs => cs.StudentId)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} students already enrolled", existingEnrollments.Count);

                // Filter out already enrolled students
                var studentsToEnroll = matchedStudents
                    .Where(s => !existingEnrollments.Contains(s.Id))
                    .ToList();

                if (!studentsToEnroll.Any())
                {
                    _logger.LogWarning("All students from Excel file already enrolled in course {CourseId}", courseId);
                    
                    var warningMsg = "All students from the Excel file are already enrolled in this course.";
                    if (rowErrors.Any())
                    {
                        warningMsg += $" Additionally, {rowErrors.Count} row(s) had validation errors.";
                    }
                    
                    TempData["WarningMessage"] = warningMsg;
                    return RedirectToAction("Manage", new { courseId, role });
                }

                // Create enrollments
                var newEnrollments = studentsToEnroll.Select(student => new CourseStudent
                {
                    CourseRolesId = courseId,
                    StudentId = student.Id,
                    EnrolledDate = DateTime.Now
                }).ToList();

                _context.CourseStudent.AddRange(newEnrollments);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully enrolled {Count} students from Excel file for course {CourseId}",
                    newEnrollments.Count, courseId);

                // Build success message with details
                var successMessage = $"✅ Successfully enrolled {newEnrollments.Count} student(s) from Excel file.";
                
                if (existingEnrollments.Any())
                {
                    successMessage += $"<br/>ℹ️ {existingEnrollments.Count} student(s) were already enrolled (skipped).";
                }

                if (notFoundIds.Any())
                {
                    successMessage += $"<br/>⚠️ {notFoundIds.Count} student ID(s) not found in database: {string.Join(", ", notFoundIds.Take(5))}" +
                        (notFoundIds.Count > 5 ? "..." : "");
                }

                if (rowErrors.Any())
                {
                    successMessage += $"<br/>❌ {rowErrors.Count} row(s) had validation errors and were skipped.";
                }

                TempData["SuccessMessage"] = successMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Excel file for course {CourseId}", courseId);
                TempData["ErrorMessage"] = $"An error occurred while processing the Excel file: {ex.Message}";
            }

            return RedirectToAction("Manage", new { courseId, role });
        }

        // GET: CourseStudents/DownloadTemplate
        public async Task<IActionResult> DownloadTemplate(int courseId)
        {
            try
            {
                _logger.LogInformation("Downloading Excel template for student enrollment, courseId: {CourseId}", courseId);

                // Get course information to pre-fill in template
                var course = await _context.CourseRoles.FindAsync(courseId);
                if (course == null)
                {
                    _logger.LogWarning("Course not found with ID: {CourseId}", courseId);
                    TempData["ErrorMessage"] = "Course not found.";
                    return RedirectToAction("Manage", new { courseId });
                }

                IWorkbook workbook = new XSSFWorkbook();
                ISheet worksheet = workbook.CreateSheet("Student Enrollment");

                // Create header style
                ICellStyle headerStyle = workbook.CreateCellStyle();
                headerStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Blue.Index;
                headerStyle.FillPattern = FillPattern.SolidForeground;
                IFont headerFont = workbook.CreateFont();
                headerFont.Color = NPOI.HSSF.Util.HSSFColor.White.Index;
                headerFont.IsBold = true;
                headerStyle.SetFont(headerFont);

                // Create yellow highlight style for pre-filled data
                ICellStyle yellowStyle = workbook.CreateCellStyle();
                yellowStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Yellow.Index;
                yellowStyle.FillPattern = FillPattern.SolidForeground;

                // Create header row
                IRow headerRow = worksheet.CreateRow(0);
                var headers = new[] { "Course Code", "Term Name", "Student ID", "Name (Optional)", "Email (Optional)" };
                for (int i = 0; i < headers.Length; i++)
                {
                    ICell cell = headerRow.CreateCell(i);
                    cell.SetCellValue(headers[i]);
                    cell.CellStyle = headerStyle;
                }

                // Add sample data with pre-filled course info
                IRow row1 = worksheet.CreateRow(1);
                ICell cell1_0 = row1.CreateCell(0);
                cell1_0.SetCellValue(course.CourseCode);
                cell1_0.CellStyle = yellowStyle;
                
                ICell cell1_1 = row1.CreateCell(1);
                cell1_1.SetCellValue(course.Year);
                cell1_1.CellStyle = yellowStyle;

                ICell cell1_2 = row1.CreateCell(2);
                cell1_2.SetCellValue(course.Trimester);
                cell1_2.CellStyle = yellowStyle;   

                ICell cell1_3 = row1.CreateCell(3);
                cell1_3.SetCellValue("20250001");
                cell1_3.CellStyle = yellowStyle;

                ICell cell1_4 = row1.CreateCell(4);
                cell1_4.SetCellValue("John Doe");
                cell1_4.CellStyle = yellowStyle;

                ICell cell1_5 = row1.CreateCell(5);
                cell1_5.SetCellValue("john.doe@example.com");
                cell1_5.CellStyle = yellowStyle;

                IRow row2 = worksheet.CreateRow(2);
                ICell cell2_0 = row2.CreateCell(0);
                cell2_0.CellStyle = yellowStyle;
                cell2_0.SetCellValue(course.CourseCode);
                
                
                ICell cell2_1 = row2.CreateCell(1);
                cell2_1.SetCellValue(course.Year);
                cell2_1.CellStyle = yellowStyle;

                ICell cell2_2 = row2.CreateCell(2);
                cell2_2.SetCellValue(course.Trimester);
                cell2_2.CellStyle = yellowStyle;
                
                ICell cell2_3 = row2.CreateCell(3);
                cell2_3.SetCellValue("20250002");
                cell2_3.CellStyle = yellowStyle;
                
                ICell cell2_4 = row2.CreateCell(4);
                cell2_4.SetCellValue("Jane Smith");
                cell2_4.CellStyle = yellowStyle;
                
                ICell cell2_5 = row2.CreateCell(5);
                cell2_5.SetCellValue("jane.smith@example.com");
                cell2_5.CellStyle = yellowStyle;

                // Auto-size columns
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.AutoSizeColumn(i);
                }

                // Add instructions
                ISheet worksheet2 = workbook.CreateSheet("Instructions");

                IRow instructionHeaderRow = worksheet2.CreateRow(0);
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
                    "2. Column B (Term Name) is REQUIRED",
                    "3. Column C (Trimester) is REQUIRED",
                    "4. Column D (Student ID) is REQUIRED and must match existing student records",
                    "5. Columns E and F (Name and Email) are optional and for reference only",
                    "6. Delete the sample rows (2 and 3) and add your actual student data",
                    "7. Each row will be validated - rows with incorrect Course Code or Term Name will be rejected",
                    "8. Students already enrolled in the course will be automatically skipped"
                };

                for (int i = 0; i < instructions.Length; i++)
                {
                    IRow instructionRow = worksheet2.CreateRow(1 + i);
                    ICell instructionCellTemp = instructionRow.CreateCell(0);
                    instructionCellTemp.SetCellValue(instructions[i]);
                    instructionCellTemp.CellStyle = boldStyle;
                }

                
                // Write to memory stream
                using (var stream = new MemoryStream())
                {
                    workbook.Write(stream);
                    var fileName = $"StudentEnrollmentTemplate.xlsx";
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Excel template for course {CourseId}", courseId);
                TempData["ErrorMessage"] = "Error generating template file.";
                return RedirectToAction("Manage", new { courseId });
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
                    return cell.NumericCellValue.ToString();
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