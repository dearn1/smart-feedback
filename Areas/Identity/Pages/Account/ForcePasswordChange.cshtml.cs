using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using smart_feedback.Models;
using smart_feedback.Services;
using System.ComponentModel.DataAnnotations;

namespace smart_feedback.Areas.Identity.Pages.Account
{
    [Authorize]
    public class ForcePasswordChangeModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<ForcePasswordChangeModel> _logger;

        public ForcePasswordChangeModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService,
            ILogger<ForcePasswordChangeModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Current password")]
            public string CurrentPassword { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var requiresPasswordChange = await _userManager.GetAuthenticationTokenAsync(user, "Default", "RequirePasswordChange");

            if (requiresPasswordChange != "true")
            {
                return RedirectToPage("/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            
            _logger.LogInformation("User {Email} (ID: {UserId}) is changing password (forced)", 
                user.Email, user.Id);

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);

            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            _logger.LogInformation("User {Email} (ID: {UserId}) successfully changed password (forced)",
                user.Email, user.Id);

            // Remove the password change requirement
            await _userManager.RemoveAuthenticationTokenAsync(user, "Default", "RequirePasswordChange");

            await _signInManager.RefreshSignInAsync(user);

            // Send email notification
            try
            {
                await _emailService.SendPasswordChangeConfirmationEmailAsync(user.Email, user.FullName ?? user.Email);
                _logger.LogInformation("Password change confirmation email sent to {Email}", user.Email);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Failed to send password change confirmation email to {Email}", user.Email);
                // Don't fail the password change if email fails
            }

            TempData["StatusMessage"] = "Your password has been changed successfully.";
            return RedirectToPage("/Index");
        }
    }
}
