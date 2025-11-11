using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using smart_feedback.Models;
using smart_feedback.Data;
using Microsoft.AspNetCore.Identity;

namespace smart_feedback.Controllers
{
    
public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userId = currentUser?.UserName;

            _logger.LogInformation("Dashboard accessed by user: {UserId}", userId);

            var lecturerCourses = await _context.CourseRoles
                .Where(cr => cr.RoleLecturer == userId)
                .ToListAsync();
            _logger.LogInformation("Lecturer courses fetched for user: {UserId}, count: {Count}", userId, lecturerCourses.Count);

            var moderatorCourses = await _context.CourseRoles
                .Where(cr => cr.RoleModerator == userId)
                .ToListAsync();
            _logger.LogInformation("Moderator courses fetched for user: {UserId}, count: {Count}", userId, moderatorCourses.Count);

            ViewBag.LecturerCourses = lecturerCourses;
            ViewBag.ModeratorCourses = moderatorCourses;

            return View();
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
