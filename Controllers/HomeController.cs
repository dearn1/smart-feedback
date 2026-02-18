using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using smart_feedback.Models;
using smart_feedback.Data;
using Microsoft.AspNetCore.Identity;
using smart_feedback.Models.ViewModels;

namespace smart_feedback.Controllers
{
    
public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var userId = currentUser?.UserName;

                _logger.LogInformation("Dashboard accessed by user: {UserId}", userId);

                // Check roles using Identity
                bool isLecturer = await _userManager.IsInRoleAsync(currentUser, "Lecturer");
                bool isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

                var lecturerCourses = new List<CourseRolesViewModel>();
                var moderatorCourses = new List<CourseRolesViewModel>();

                if (isAdmin)
                {                    
                    var courses = await _context.CourseRoles.ToListAsync();
                    lecturerCourses = await MapCoursesToViewModels(courses);

                    _logger.LogInformation("Admin courses fetched for user: {UserId}, count: {Count}", userId, lecturerCourses.Count);
                }
                else if (isLecturer)
                {
                    var lecturerCoursesData = await _context.CourseRoles
                        .Where(cr => cr.RoleLecturer == userId)
                        .ToListAsync();
                    lecturerCourses = await MapCoursesToViewModels(lecturerCoursesData);
                    _logger.LogInformation("Lecturer courses fetched for user: {UserId}, count: {Count}", userId, lecturerCourses.Count);

                    var moderatorCoursesData = await _context.CourseRoles
                        .Where(cr => cr.RoleModerator == userId)
                        .ToListAsync();
                    moderatorCourses = await MapCoursesToViewModels(moderatorCoursesData);
                    _logger.LogInformation("Moderator courses fetched for user: {UserId}, count: {Count}", userId, moderatorCourses.Count);
                }

                ViewBag.LecturerCourses = lecturerCourses;
                ViewBag.ModeratorCourses = moderatorCourses;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading dashboard for user {UserId}. Error: {ErrorMessage}",
                    User.Identity?.Name, ex.Message);

                TempData["ErrorMessage"] = "An error occurred while loading your dashboard. Please try again.";
                return View();
            }
        }

        private async Task<List<CourseRolesViewModel>> MapCoursesToViewModels(List<CourseRoles> courses)
        {
            var viewModels = new List<CourseRolesViewModel>();

            foreach (var course in courses)
            {
                var viewModel = new CourseRolesViewModel
                {
                    CourseRolesId = course.CourseRolesId,
                    CourseCode = course.CourseCode,
                    CourseName = course.CourseName,
                    Year = course.Year,
                    Trimester = course.Trimester,
                    Programme = course.Programme,
                    Institution = course.Institution,
                    RoleLecturer = course.RoleLecturer,
                    RoleModerator = course.RoleModerator
                };

                // Get lecturer full name
                if (!string.IsNullOrEmpty(course.RoleLecturer))
                {
                    var lecturer = await _userManager.FindByNameAsync(course.RoleLecturer);
                    viewModel.LecturerFullName = lecturer?.FullName ?? course.RoleLecturer;
                }

                // Get moderator full name
                if (!string.IsNullOrEmpty(course.RoleModerator))
                {
                    var moderator = await _userManager.FindByNameAsync(course.RoleModerator);
                    viewModel.ModeratorFullName = moderator?.FullName ?? course.RoleModerator;
                }

                viewModels.Add(viewModel);
            }

            return viewModels;
        }

        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
