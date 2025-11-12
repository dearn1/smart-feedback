using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using smart_feedback.Data;
using smart_feedback.Models.ViewModels;
using System.Security.Cryptography;
using System.Text;

namespace smart_feedback.Controllers
{
    [Authorize(Roles = ApplicationRoles.Admin)]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
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

                var user = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email,
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

        private string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!@#$%";
            var random = new Random();
            var password = new StringBuilder();

            // Ensure at least one uppercase, one lowercase, one digit, and one special character
            password.Append(chars[random.Next(0, 26)]); // Uppercase
            password.Append(chars[random.Next(26, 52)]); // Lowercase  
            password.Append(chars[random.Next(52, 62)]); // Digit
            password.Append(chars[random.Next(62, 67)]); // Special character

            // Fill the rest randomly
            for (int i = 4; i < 8; i++)
            {
                password.Append(chars[random.Next(chars.Length)]);
            }

            // Shuffle the password
            return new string(password.ToString().ToCharArray().OrderBy(x => random.Next()).ToArray());
        }
    }
}
