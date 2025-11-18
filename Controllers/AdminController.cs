using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using smart_feedback.Data;
using smart_feedback.Models;
using smart_feedback.Models.ViewModels;
using System.Security.Cryptography;
using System.Text;

namespace smart_feedback.Controllers
{
    [Authorize(Roles = ApplicationRoles.Admin)]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> UserManagement()
        {
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

            return View(userViewModels);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            var model = new CreateUserViewModel();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Generate a random password
                string tempPassword = GenerateTemporaryPassword();

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, tempPassword);

                if (result.Succeeded)
                {
                    // Assign the lecturer role
                    await _userManager.AddToRoleAsync(user, ApplicationRoles.Lecturer);

                    // Force password change on first login
                    await _userManager.SetAuthenticationTokenAsync(user, "Default", "RequirePasswordChange", "true");

                    TempData["SuccessMessage"] = $"User created successfully! Email: {model.Email}, Temporary Password: {tempPassword}";
                    TempData["TempPassword"] = tempPassword;
                    return RedirectToAction(nameof(UserManagement));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "User deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete user.";
                }
            }

            return RedirectToAction(nameof(UserManagement));
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                string newPassword = GenerateTemporaryPassword();

                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
                if (result.Succeeded)
                {
                    // Force password change on next login
                    await _userManager.SetAuthenticationTokenAsync(user, "Default", "RequirePasswordChange", "true");

                    TempData["SuccessMessage"] = $"Password reset successfully. New password: {newPassword}";
                    TempData["TempPassword"] = newPassword;
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to reset password.";
                }
            }

            return RedirectToAction(nameof(UserManagement));
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    return NotFound();
                }

                // Update user properties
                user.Email = model.Email;
                user.UserName = model.Email; // Keep UserName in sync with Email
                user.FullName = model.FullName;
                user.Department = model.Department ?? string.Empty;
                user.JobTitle = model.JobTitle ?? string.Empty;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "User information updated successfully.";
                    return RedirectToAction(nameof(UserManagement));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }


        private string GenerateTemporaryPassword()
        {
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
            return new string(password.ToString().ToCharArray().OrderBy(x => random.Next()).ToArray());
        }
    }
}
