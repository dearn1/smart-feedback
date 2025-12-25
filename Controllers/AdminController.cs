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


        private string GenerateTemporaryPassword()
        {
            try
            {
                _logger.LogDebug("Generating temporary password");

                const string chars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!@#$%^&*()-=_+[]{}|;':,./<>?";
                var random = new Random();
                var password = new StringBuilder();

                // Ensure at least one uppercase, one lowercase, one digit, and one special character
                password.Append(chars[random.Next(0, 25)]); // Uppercase
                password.Append(chars[random.Next(26, 50)]); // Lowercase  
                password.Append(chars[random.Next(51, 60)]); // Digit
                password.Append(chars[random.Next(61, 88)]); // Special character

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
