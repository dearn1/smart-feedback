using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using smart_feedback.Data;
using smart_feedback.Models;
using smart_feedback.Models.ViewModels;
using smart_feedback.Services;
using System.Security.Cryptography;
using System.Text;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.SS.Util;
using System.IO;

namespace smart_feedback.Controllers
{
    [Authorize(Roles = ApplicationRoles.Admin)]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminController> _logger;
        private readonly IEmailService _emailService;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<AdminController> logger, IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task<IActionResult> UserManagement()
        {
            try
            {
                _logger.LogInformation("Admin {AdminUser} accessed user management at {Timestamp}",
                    User.Identity.Name, DateTime.UtcNow);

                var users = await _userManager.Users.ToListAsync();
                var userViewModels = new List<UserManagementViewModel>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userViewModels.Add(new UserManagementViewModel
                    {
                        Id = user.Id,
                        Email = user.Email,
                        UserName = user.Email,
                        FullName = user.FullName,
                        Department = user.Department ?? string.Empty,
                        JobTitle = user.JobTitle ?? string.Empty,
                        EmailConfirmed = user.EmailConfirmed,
                        Roles = roles.ToList(),
                        LockoutEnd = user.LockoutEnd,
                        AccessFailedCount = user.AccessFailedCount
                    });
                }

                _logger.LogInformation("Successfully loaded {UserCount} users for user management", users.Count);
                return View(userViewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading user management for admin {AdminUser}", User.Identity.Name);
                TempData["ErrorMessage"] = "An error occurred while loading user data.";
                return View(new List<UserManagementViewModel>());
            }
        }

        [HttpGet]
        public IActionResult MLManagement()
        {
            _logger.LogInformation("Admin {AdminUser} accessed ML Management at {Timestamp}",
                User.Identity.Name, DateTime.UtcNow);

            return View();
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            _logger.LogInformation("Admin {AdminUser} accessed create user page at {Timestamp}",
                User.Identity.Name, DateTime.UtcNow);

            var model = new CreateUserViewModel();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            _logger.LogInformation("Admin {AdminUser} attempting to create user with email {Email} at {Timestamp}",
                User.Identity.Name, model?.Email, DateTime.UtcNow);

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if user already exists
                    var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Admin {AdminUser} attempted to create user {Email} but user already exists",
                        User.Identity.Name, model.Email);
                    ModelState.AddModelError(string.Empty, "A user with this email already exists.");
                    return View(model);
                }

                // Generate a random password
                string tempPassword = GenerateTemporaryPassword();

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    EmailConfirmed = true
                };

                _logger.LogInformation("Creating new user {Email} with temporary password", model.Email);
                var result = await _userManager.CreateAsync(user, tempPassword);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Successfully created user {Email} with ID {UserId}",
                            model.Email, user.Id);

                    // Assign the lecturer role
                    await _userManager.AddToRoleAsync(user, ApplicationRoles.Lecturer);

                    // Force password change on first login
                    await _userManager.SetAuthenticationTokenAsync(user, "Default", "RequirePasswordChange", "true");

                        // Send email with credentials
                        try
                        {
                            await _emailService.SendPasswordEmailAsync(model.Email, model.FullName, tempPassword);
                            _logger.LogInformation("Email sent successfully to {Email} for new user account", model.Email);

                            TempData["SuccessMessage"] = $"User created successfully! An email with login credentials has been sent to {model.Email}.";
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, "Failed to send email to {Email} for new user account. Error details: {ErrorType} - {ErrorMessage}",
                                model.Email, emailEx.GetType().Name, emailEx.Message);

                            // Check for specific error types
                            if (emailEx.InnerException != null)
                            {
                                _logger.LogError("Inner exception: {InnerExceptionType} - {InnerExceptionMessage}",
                                    emailEx.InnerException.GetType().Name, emailEx.InnerException.Message);
                            }

                            TempData["SuccessMessage"] = $"User created successfully! Email: {model.Email}, Temporary Password: {tempPassword} (Email sending failed - please provide credentials manually)";
                            TempData["TempPassword"] = tempPassword;
                        }

                        _logger.LogInformation("User creation completed successfully for {Email} by admin {AdminUser}",
                            model.Email, User.Identity.Name);
                    return RedirectToAction(nameof(UserManagement));
                }

                _logger.LogError("Failed to create user {Email}. Errors: {Errors}",
                        model.Email, string.Join(", ", result.Errors.Select(e => e.Description)));

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception occurred while creating user {Email} by admin {AdminUser}",
                        model.Email, User.Identity.Name);
                    ModelState.AddModelError(string.Empty, "An error occurred while creating the user.");
                }
            }
            else
            {
                _logger.LogWarning("Create user form validation failed for admin {AdminUser}. Model errors: {ModelErrors}",
                    User.Identity.Name, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            _logger.LogInformation("Admin {AdminUser} attempting to delete user {UserId} at {Timestamp}",
                User.Identity.Name, userId, DateTime.UtcNow);

            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    _logger.LogInformation("Found user {Email} (ID: {UserId}) for deletion", user.Email, userId);
                    var result = await _userManager.DeleteAsync(user);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Successfully deleted user {Email} (ID: {UserId}) by admin {AdminUser}",
                                user.Email, userId, User.Identity.Name);
                        TempData["SuccessMessage"] = "User deleted successfully.";
                    }
                    else
                    {
                        _logger.LogError("Failed to delete user {Email} (ID: {UserId}). Errors: {Errors}",
                                user.Email, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                        TempData["ErrorMessage"] = "Failed to delete user.";
                    }
                }
                else
                {
                    _logger.LogWarning("Admin {AdminUser} attempted to delete non-existent user {UserId}",
                        User.Identity.Name, userId);
                    TempData["ErrorMessage"] = "User not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while deleting user {UserId} by admin {AdminUser}",
                    userId, User.Identity.Name);
                TempData["ErrorMessage"] = "An error occurred while deleting the user.";
            }

            return RedirectToAction(nameof(UserManagement));
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string userId)
        {
            _logger.LogInformation("Admin {AdminUser} attempting to reset password for user {UserId} at {Timestamp}",
                User.Identity.Name, userId, DateTime.UtcNow);
            
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    _logger.LogInformation("Found user {Email} (ID: {UserId}) for password reset", user.Email, userId);

                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    string newPassword = GenerateTemporaryPassword();

                    var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Successfully reset password for user {Email} (ID: {UserId}) by admin {AdminUser}",
                            user.Email, userId, User.Identity.Name);

                        // Force password change on next login
                        await _userManager.SetAuthenticationTokenAsync(user, "Default", "RequirePasswordChange", "true");
                        _logger.LogInformation("Set password change requirement for user {Email}", user.Email);

                        // Send email with new password
                        try
                        {
                            await _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName ?? user.Email, newPassword);
                            _logger.LogInformation("Password reset email sent successfully to {Email}", user.Email);

                            TempData["SuccessMessage"] = $"Password reset successfully. An email with the new password has been sent to {user.Email}.";
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, "Failed to send email to {Email} for new user account. Error details: {ErrorType} - {ErrorMessage}",
                                user.Email, emailEx.GetType().Name, emailEx.Message);

                            // Check for specific error types
                            if (emailEx.InnerException != null)
                            {
                                _logger.LogError("Inner exception: {InnerExceptionType} - {InnerExceptionMessage}",
                                    emailEx.InnerException.GetType().Name, emailEx.InnerException.Message);
                            }

                            TempData["SuccessMessage"] = $"User created successfully! Email: {user.Email}, Temporary Password: {newPassword} (Email sending failed - please provide credentials manually)";
                            TempData["TempPassword"] = newPassword;
                        }
                    }
                    else
                    {
                        _logger.LogError("Failed to reset password for user {Email} (ID: {UserId}). Errors: {Errors}",
                            user.Email, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                        TempData["ErrorMessage"] = "Failed to reset password.";
                    }
                }
                else
                {
                    _logger.LogWarning("Admin {AdminUser} attempted to reset password for non-existent user {UserId}",
                        User.Identity.Name, userId);
                    TempData["ErrorMessage"] = "User not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while resetting password for user {UserId} by admin {AdminUser}",
                    userId, User.Identity.Name);
                TempData["ErrorMessage"] = "An error occurred while resetting the password.";
            }

            return RedirectToAction(nameof(UserManagement));
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            _logger.LogInformation("Admin {AdminUser} accessing edit user page for user {UserId} at {Timestamp}",
                User.Identity.Name, id, DateTime.UtcNow);

            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Edit user attempt with null or empty userId by admin {AdminUser}", User.Identity.Name);
                return NotFound();
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("Admin {AdminUser} attempted to edit non-existent user {UserId}",
                        User.Identity.Name, id);
                    return NotFound();
                }
                
                _logger.LogInformation("Loading edit form for user {Email} (ID: {UserId})", user.Email, id);

                var model = new EditUserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Department = user.Department,
                    JobTitle = user.JobTitle
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while loading edit form for user {UserId} by admin {AdminUser}",
                    id, User.Identity.Name);
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            _logger.LogInformation("Admin {AdminUser} attempting to update user {UserId} ({Email}) at {Timestamp}",
                User.Identity.Name, model?.Id, model?.Email, DateTime.UtcNow);


            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.FindByIdAsync(model.Id);
                    if (user == null)
                    {
                        _logger.LogWarning("Admin {AdminUser} attempted to update non-existent user {UserId}",
                            User.Identity.Name, model.Id);
                        return NotFound();
                    }

                    _logger.LogInformation("Updating user {Email} (ID: {UserId}). Changes: Email: {OldEmail} -> {NewEmail}, FullName: {OldFullName} -> {NewFullName}, Department: {OldDepartment} -> {NewDepartment}, JobTitle: {OldJobTitle} -> {NewJobTitle}",
                        user.Email, model.Id, user.Email, model.Email, user.FullName, model.FullName, user.Department, model.Department, user.JobTitle, model.JobTitle);

                    // Update user properties
                    user.Email = model.Email;
                    user.UserName = model.Email; // Keep UserName in sync with Email
                    user.FullName = model.FullName;
                    user.Department = model.Department ?? string.Empty;
                    user.JobTitle = model.JobTitle ?? string.Empty;

                    var result = await _userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Successfully updated user {Email} (ID: {UserId}) by admin {AdminUser}",
                            model.Email, model.Id, User.Identity.Name);
                        TempData["SuccessMessage"] = "User information updated successfully.";
                        return RedirectToAction(nameof(UserManagement));
                    }

                    _logger.LogError("Failed to update user {Email} (ID: {UserId}). Errors: {Errors}",
                        model.Email, model.Id, string.Join(", ", result.Errors.Select(e => e.Description)));

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception occurred while updating user {UserId} by admin {AdminUser}",
                        model.Id, User.Identity.Name);
                    ModelState.AddModelError(string.Empty, "An error occurred while updating the user.");
                }
            }
            else
            {
                _logger.LogWarning("Edit user form validation failed for user {UserId} by admin {AdminUser}. Model errors: {ModelErrors}",
                    model?.Id, User.Identity.Name, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
            }

            return View(model);
        }


        // GET: Admin/UploadUsers
        [HttpGet]
        public IActionResult UploadUsers()
        {
            _logger.LogInformation("Admin {AdminUser} accessed upload users page at {Timestamp}",
                User.Identity.Name, DateTime.UtcNow);

            return View();
        }

        // GET: Admin/DownloadUserTemplate
        [HttpGet]
        public IActionResult DownloadUserTemplate()
        {
            try
            {
                _logger.LogInformation("Admin {AdminUser} downloading user upload template at {Timestamp}",
                    User.Identity.Name, DateTime.UtcNow);

                IWorkbook workbook = new XSSFWorkbook();
                ISheet worksheet = workbook.CreateSheet("Users");

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
                var headers = new[] { "Email", "Full Name", "Department", "Job Title" };
                for (int i = 0; i < headers.Length; i++)
                {
                    ICell cell = headerRow.CreateCell(i);
                    cell.SetCellValue(headers[i]);
                    cell.CellStyle = headerStyle;
                }

                // Add sample data rows
                IRow row1 = worksheet.CreateRow(1);
                ICell cell1_0 = row1.CreateCell(0);
                cell1_0.SetCellValue("john.doe@university.edu");
                cell1_0.CellStyle = yellowStyle;

                ICell cell1_1 = row1.CreateCell(1);
                cell1_1.SetCellValue("John Doe");
                cell1_1.CellStyle = yellowStyle;

                ICell cell1_2 = row1.CreateCell(2);
                cell1_2.SetCellValue("Computer Science");
                cell1_2.CellStyle = yellowStyle;

                ICell cell1_3 = row1.CreateCell(3);
                cell1_3.SetCellValue("Senior Lecturer");
                cell1_3.CellStyle = yellowStyle;

                IRow row2 = worksheet.CreateRow(2);
                ICell cell2_0 = row2.CreateCell(0);
                cell2_0.SetCellValue("jane.smith@university.edu");
                cell2_0.CellStyle = yellowStyle;

                ICell cell2_1 = row2.CreateCell(1);
                cell2_1.SetCellValue("Jane Smith");
                cell2_1.CellStyle = yellowStyle;

                ICell cell2_2 = row2.CreateCell(2);
                cell2_2.SetCellValue("Information Technology");
                cell2_2.CellStyle = yellowStyle;

                ICell cell2_3 = row2.CreateCell(3);
                cell2_3.SetCellValue("Associate Professor");
                cell2_3.CellStyle = yellowStyle;

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
                    "1. Column A (Email) is REQUIRED and must be a valid email format",
                    "2. Column B (Full Name) is REQUIRED",
                    "3. Column C (Department) is OPTIONAL",
                    "4. Column D (Job Title) is OPTIONAL",
                    "5. Delete the sample rows (2 and 3) and add your actual user data",
                    "6. Users with duplicate emails will be skipped",
                    "7. Rows with missing required fields will be rejected",
                    "8. All users will be created with temporary passwords",
                    "9. Users will be assigned the 'Lecturer' role by default",
                    "10. Email notifications will be sent with login credentials",
                    "11. Users will be required to change password on first login"
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
                    var fileName = $"UserUploadTemplate_{DateTime.Now:yyyyMMdd}.xlsx";
                    
                    _logger.LogInformation("User upload template generated successfully: {FileName}", fileName);
                    
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating user upload template");
                TempData["ErrorMessage"] = "Error generating template file.";
                return RedirectToAction(nameof(UserManagement));
            }
        }

        // POST: Admin/UploadUsers
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadUsers(IFormFile excelFile)
        {
            try
            {
                _logger.LogInformation("Admin {AdminUser} uploading users from Excel at {Timestamp}, file: {FileName}, size: {FileSize} bytes",
                    User.Identity.Name, DateTime.UtcNow, excelFile?.FileName, excelFile?.Length);

                if (excelFile == null || excelFile.Length == 0)
                {
                    _logger.LogWarning("Excel upload attempted with null or empty file");
                    TempData["ErrorMessage"] = "Please select a valid Excel file.";
                    return RedirectToAction(nameof(UploadUsers));
                }

                // Check file extension
                var extension = Path.GetExtension(excelFile.FileName).ToLower();
                if (extension != ".xlsx" && extension != ".xls")
                {
                    _logger.LogWarning("Excel upload attempted with invalid file extension: {Extension}", extension);
                    TempData["ErrorMessage"] = "Only Excel files (.xlsx, .xls) are allowed.";
                    return RedirectToAction(nameof(UploadUsers));
                }

                // Check file size (limit to 5MB)
                if (excelFile.Length > 5 * 1024 * 1024)
                {
                    _logger.LogWarning("Excel upload attempted with oversized file: {FileSize} bytes", excelFile.Length);
                    TempData["ErrorMessage"] = "File size must be less than 5MB.";
                    return RedirectToAction(nameof(UploadUsers));
                }

                var usersToCreate = new List<(string Email, string FullName, string Department, string JobTitle, string TempPassword)>();
                var rowErrors = new List<string>();
                var duplicateEmails = new List<string>();
                var successfulCreations = new List<string>();
                var failedCreations = new List<string>();
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
                        if (firstCell != null && (firstCell.Contains("email") || firstCell.Contains("mail")))
                        {
                            startRow = 1;
                            _logger.LogDebug("Header row detected, starting from row 1");
                        }
                    }

                    // Get existing emails for duplicate check
                    var existingEmails = await _userManager.Users
                        .Select(u => u.Email.ToLower())
                        .ToListAsync();

                    for (int row = startRow; row <= rowCount; row++)
                    {
                        rowNumber = row + 1; // +1 for display (Excel rows start at 1)

                        IRow currentRow = worksheet.GetRow(row);
                        if (currentRow == null) continue;

                        var emailCell = GetCellValue(currentRow.GetCell(0))?.Trim();
                        var fullNameCell = GetCellValue(currentRow.GetCell(1))?.Trim();
                        var departmentCell = GetCellValue(currentRow.GetCell(2))?.Trim();
                        var jobTitleCell = GetCellValue(currentRow.GetCell(3))?.Trim();

                        // Skip empty rows
                        if (string.IsNullOrWhiteSpace(emailCell) &&
                            string.IsNullOrWhiteSpace(fullNameCell))
                        {
                            _logger.LogDebug("Skipping empty row {Row}", rowNumber);
                            continue;
                        }

                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(emailCell))
                        {
                            rowErrors.Add($"Row {rowNumber}: Missing Email");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(fullNameCell))
                        {
                            rowErrors.Add($"Row {rowNumber}: Missing Full Name");
                            continue;
                        }

                        // Validate email format
                        if (!IsValidEmail(emailCell))
                        {
                            rowErrors.Add($"Row {rowNumber}: Invalid Email format '{emailCell}'");
                            continue;
                        }

                        // Check for duplicate email in database
                        if (existingEmails.Contains(emailCell.ToLower()))
                        {
                            duplicateEmails.Add(emailCell);
                            _logger.LogDebug("Row {Row}: Duplicate Email '{Email}'", rowNumber, emailCell);
                            continue;
                        }

                        // Check for duplicates within the uploaded file
                        if (usersToCreate.Any(u => u.Email.Equals(emailCell, StringComparison.OrdinalIgnoreCase)))
                        {
                            rowErrors.Add($"Row {rowNumber}: Duplicate Email '{emailCell}' within the file");
                            continue;
                        }

                        // Generate temporary password
                        string tempPassword = GenerateTemporaryPassword();

                        usersToCreate.Add((emailCell, fullNameCell, departmentCell ?? "", jobTitleCell ?? "", tempPassword));
                    }
                }

                _logger.LogInformation("Extracted {Count} valid users from Excel file, {ErrorCount} rows with errors, {DuplicateCount} duplicates",
                    usersToCreate.Count, rowErrors.Count, duplicateEmails.Count);

                // Create users
                if (usersToCreate.Any())
                {
                    foreach (var (email, fullName, department, jobTitle, tempPassword) in usersToCreate)
                    {
                        try
                        {
                            var user = new ApplicationUser
                            {
                                UserName = email,
                                Email = email,
                                FullName = fullName,
                                Department = department,
                                JobTitle = jobTitle,
                                EmailConfirmed = true
                            };

                            _logger.LogInformation("Creating user {Email} from Excel upload", email);
                            var result = await _userManager.CreateAsync(user, tempPassword);

                            if (result.Succeeded)
                            {
                                _logger.LogInformation("Successfully created user {Email} with ID {UserId}",
                                    email, user.Id);

                                // Assign the lecturer role
                                await _userManager.AddToRoleAsync(user, ApplicationRoles.Lecturer);

                                // Force password change on first login
                                await _userManager.SetAuthenticationTokenAsync(user, "Default", "RequirePasswordChange", "true");

                                // Send email with credentials
                                try
                                {
                                    await _emailService.SendPasswordEmailAsync(email, fullName, tempPassword);
                                    _logger.LogInformation("Email sent successfully to {Email} for new user account", email);

                                    successfulCreations.Add($"{fullName} ({email})");
                                }
                                catch (Exception emailEx)
                                {
                                    _logger.LogError(emailEx, "Failed to send email to {Email}. Error: {ErrorMessage}",
                                        email, emailEx.Message);

                                    successfulCreations.Add($"{fullName} ({email}) - Email failed, password: {tempPassword}");
                                }
                            }
                            else
                            {
                                _logger.LogError("Failed to create user {Email}. Errors: {Errors}",
                                    email, string.Join(", ", result.Errors.Select(e => e.Description)));
                                failedCreations.Add($"{fullName} ({email}): {string.Join(", ", result.Errors.Select(e => e.Description))}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Exception occurred while creating user {Email}", email);
                            failedCreations.Add($"{fullName} ({email}): {ex.Message}");
                        }
                    }
                }

                _logger.LogInformation("User creation completed: {SuccessCount} successful, {FailedCount} failed",
                    successfulCreations.Count, failedCreations.Count);

                // Build result message
                var messages = new List<string>();

                if (successfulCreations.Any())
                {
                    var successList = string.Join("<br/>", successfulCreations.Take(10));
                    if (successfulCreations.Count > 10)
                    {
                        successList += $"<br/>...and {successfulCreations.Count - 10} more";
                    }
                    messages.Add($"✅ Successfully created {successfulCreations.Count} user(s):<br/>{successList}");
                }

                if (duplicateEmails.Any())
                {
                    var duplicateList = string.Join(", ", duplicateEmails.Take(5));
                    if (duplicateEmails.Count > 5)
                    {
                        duplicateList += $" and {duplicateEmails.Count - 5} more";
                    }
                    messages.Add($"ℹ️ {duplicateEmails.Count} user(s) skipped (already exist): {duplicateList}");
                }

                if (failedCreations.Any())
                {
                    var failedList = string.Join("<br/>", failedCreations.Take(5));
                    if (failedCreations.Count > 5)
                    {
                        failedList += $"<br/>...and {failedCreations.Count - 5} more";
                    }
                    messages.Add($"❌ {failedCreations.Count} user(s) failed to create:<br/>{failedList}");
                }

                if (rowErrors.Any())
                {
                    var errorList = string.Join("<br/>", rowErrors.Take(10));
                    if (rowErrors.Count > 10)
                    {
                        errorList += $"<br/>...and {rowErrors.Count - 10} more error(s)";
                    }
                    messages.Add($"⚠️ {rowErrors.Count} row(s) had validation errors:<br/>{errorList}");
                }

                if (!successfulCreations.Any() && !duplicateEmails.Any())
                {
                    TempData["ErrorMessage"] = "No valid user records found in the Excel file. Please check the format and validation requirements.";
                }
                else if (messages.Any())
                {
                    if (successfulCreations.Any())
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
                _logger.LogError(ex, "Error processing Excel file for user upload");
                TempData["ErrorMessage"] = $"An error occurred while processing the Excel file: {ex.Message}";
            }

            return RedirectToAction(nameof(UserManagement));
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

        private string GenerateTemporaryPassword()
        {
            try
            {
                _logger.LogDebug("Generating temporary password");

                const string chars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!#$%*()-=_+;:,./?";
                var random = new Random();
                var password = new StringBuilder();

                // Ensure at least one uppercase, one lowercase, one digit, and one special character
                password.Append(chars[random.Next(0, 25)]); // Uppercase
                password.Append(chars[random.Next(26, 50)]); // Lowercase  
                password.Append(chars[random.Next(51, 60)]); // Digit
                password.Append(chars[random.Next(61, 77)]); // Special character

                // Fill the rest randomly
                for (int i = 4; i < 8; i++)
                {
                    password.Append(chars[random.Next(chars.Length-1)]);
                }

                // Shuffle the password
                var generatedPassword = new string(password.ToString().ToCharArray().OrderBy(x => random.Next()).ToArray());

                _logger.LogDebug("Temporary password generated successfully");
                return generatedPassword;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating temporary password");
                throw;
            }
        }
    }
}
