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
        public async Task<IActionResult> Index(int? year, int? trimester)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var userId = currentUser?.UserName;

                // Set default year and trimester based on current date if not provided
                if (!year.HasValue)
                {
                    year = DateTime.Now.Year;
                }

                if (!trimester.HasValue)
                {
                    int currentMonth = DateTime.Now.Month;
                    // Trimester 1: Jan-Apr (months 1-4)
                    // Trimester 2: May-Aug (months 5-8)
                    // Trimester 3: Sep-Dec (months 9-12)
                    if (currentMonth <= 4)
                    {
                        trimester = 1;
                    }
                    else if (currentMonth <= 8)
                    {
                        trimester = 2;
                    }
                    else
                    {
                        trimester = 3;
                    }
                }

                _logger.LogInformation("Dashboard accessed by user: {UserId}, Year: {Year}, Trimester: {Trimester}", userId, year, trimester);

                // Check roles using Identity
                bool isLecturer = await _userManager.IsInRoleAsync(currentUser, "Lecturer");
                bool isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

                var lecturerCourses = new List<CourseRolesViewModel>();
                var moderatorCourses = new List<CourseRolesViewModel>();

                if (isAdmin)
                {                    
                    var coursesQuery = _context.CourseRoles.AsQueryable();
                    
                    // Apply filters
                    if (year.HasValue)
                    {
                        coursesQuery = coursesQuery.Where(cr => cr.Year == year.Value);
                        _logger.LogInformation("Applied year filter: {Year}", year.Value);
                    }
                    if (trimester.HasValue)
                    {
                        coursesQuery = coursesQuery.Where(cr => cr.Trimester == trimester.Value);
                        _logger.LogInformation("Applied trimester filter: {Trimester}", trimester.Value);
                    }
                    
                    var courses = await coursesQuery.ToListAsync();
                    lecturerCourses = await MapCoursesToViewModels(courses);

                    _logger.LogInformation("Admin courses fetched for user: {UserId}, count: {Count}", userId, lecturerCourses.Count);
                }
                else if (isLecturer)
                {
                    var lecturerCoursesQuery = _context.CourseRoles
                        .Where(cr => cr.RoleLecturer == userId);
                    
                    // Apply filters
                    if (year.HasValue)
                    {
                        lecturerCoursesQuery = lecturerCoursesQuery.Where(cr => cr.Year == year.Value);
                    }
                    if (trimester.HasValue)
                    {
                        lecturerCoursesQuery = lecturerCoursesQuery.Where(cr => cr.Trimester == trimester.Value);
                    }
                    
                    var lecturerCoursesData = await lecturerCoursesQuery.ToListAsync();
                    lecturerCourses = await MapCoursesToViewModels(lecturerCoursesData);
                    _logger.LogInformation("Lecturer courses fetched for user: {UserId}, count: {Count}", userId, lecturerCourses.Count);

                    var moderatorCoursesQuery = _context.CourseRoles
                        .Where(cr => cr.RoleModerator == userId);
                    
                    // Apply filters
                    if (year.HasValue)
                    {
                        moderatorCoursesQuery = moderatorCoursesQuery.Where(cr => cr.Year == year.Value);
                    }
                    if (trimester.HasValue)
                    {
                        moderatorCoursesQuery = moderatorCoursesQuery.Where(cr => cr.Trimester == trimester.Value);
                    }
                    
                    var moderatorCoursesData = await moderatorCoursesQuery.ToListAsync();
                    moderatorCourses = await MapCoursesToViewModels(moderatorCoursesData);
                    _logger.LogInformation("Moderator courses fetched for user: {UserId}, count: {Count}", userId, moderatorCourses.Count);
                }

                ViewBag.LecturerCourses = lecturerCourses;
                ViewBag.ModeratorCourses = moderatorCourses;
                
                // Set filter values for the view
                ViewBag.SelectedYear = year;
                ViewBag.SelectedTrimester = trimester;
                
                // Get distinct years and trimesters for filter dropdowns
                ViewBag.AvailableYears = await _context.CourseRoles
                    .Select(cr => cr.Year)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .ToListAsync();
                    
                ViewBag.AvailableTrimesters = await _context.CourseRoles
                    .Select(cr => cr.Trimester)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToListAsync();

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

                // Get assessment status counts for this course
                var assessmentStatusCounts = await _context.Assessments
                    .Where(a => a.CourseCode == course.CourseCode && 
                               a.Year == course.Year && 
                               a.Trimester == course.Trimester)
                    .GroupBy(a => a.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                viewModel.FinalReviewCount = assessmentStatusCounts
                    .FirstOrDefault(s => s.Status == "FinalReview")?.Count ?? 0;
                
                viewModel.ModerationCount = assessmentStatusCounts
                    .FirstOrDefault(s => s.Status == "Moderation")?.Count ?? 0;

                // NEW: Check if course has rubrics (at least one assessment with a rubric)
                viewModel.HasRubrics = await _context.Assessments
                    .AnyAsync(a => a.CourseCode == course.CourseCode && 
                                  a.Year == course.Year && 
                                  a.Trimester == course.Trimester &&
                                  a.RubricsId > 0);

                // NEW: Get student count for this course
                viewModel.StudentCount = await _context.CourseStudent
                    .Where(cs => cs.CourseRolesId == course.CourseRolesId)
                    .CountAsync();

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
