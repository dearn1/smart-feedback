using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NPOI.XWPF.UserModel;
using smart_feedback.Data;
using smart_feedback.Models;
using Microsoft.VisualStudio.Web.CodeGeneration.Design;
using smart_feedback.Data.Migrations;
using MathNet.Numerics.RootFinding;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using System.Text.RegularExpressions;

namespace smart_feedback.Controllers
{
    public class RubricsController : Controller
    {        
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<HomeController> _logger;

        public RubricsController(ApplicationDbContext context, IWebHostEnvironment hostingEnvironment, UserManager<ApplicationUser> userManager, ILogger<HomeController> logger)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
            _userManager = userManager;
            _logger = logger;
        }
        // GET: Rubrics
        [Authorize]
        public async Task<IActionResult> Index(string courseId = null, string role = null)
        {
            // Get current user
            var currentUser = await _userManager.GetUserAsync(User);
            var userId = currentUser?.UserName; // or currentUser?.Id if you want to use ID

            var course = await _context.CourseRoles.FindAsync(int.Parse(courseId));

            // Start with all rubrics
            var rubricsQuery = _context.Rubrics.AsQueryable();

            // Apply filters if parameters are provided
            if (!string.IsNullOrEmpty(course.CourseCode))
            {
                rubricsQuery = rubricsQuery.Where(r => r.CourseCode == course.CourseCode);
            }

            if (!string.IsNullOrEmpty(course.TermName))
            {
                rubricsQuery = rubricsQuery.Where(r => r.TermName == course.TermName);
            }

            // Apply user authorization check
            if (!string.IsNullOrEmpty(role))
            {
                // Verify user has the specified role for the course
                var hasAccess = await _context.CourseRoles
                    .AnyAsync(cr => cr.CourseCode == course.CourseCode &&
                                   cr.TermName == course.TermName &&
                                   ((role == "Lecturer" && cr.RoleLecturer == userId) ||
                                    (role == "Moderator" && cr.RoleModerator == userId) ||
                                    (role == "Admin")));

                if (!hasAccess)
                {
                    TempData["ErrorMessage"] = "You don't have permission to access rubrics for this course.";
                    return RedirectToAction("Index", "Home");
                }
            }

            var rubrics = await rubricsQuery.ToListAsync();

            // Set ViewBag data for the view to display filtering context
            ViewBag.FilteredCourseCode = course.CourseCode;
            ViewBag.FilteredCourseName = course.CourseName;
            ViewBag.FilteredCourseTerm = course.TermName;
            ViewBag.CourseId = course.CourseRolesId.ToString();
            ViewBag.CurrentUserRole = role;

            return View(rubrics);
        }

        // GET: Rubrics/Details/5
        public async Task<IActionResult> Details(int? id, string? courseid, string? role)
        {
            if (id == null)
            {
                return NotFound();
            }

            ViewBag.CourseId = courseid;
            ViewBag.CurrentUserRole = role;

            var rubrics = await _context.Rubrics
                .FirstOrDefaultAsync(m => m.RubricsId == id);
            if (rubrics == null)
            {
                return NotFound();
            }

            // Get the related RubricTasks
            var rubricTasks = await _context.RubricTask
                .Where(rt => rt.RubricsId == id)
                .ToListAsync();

            // Get the related RubricCriteria
            List<RubricCriteria> rubricCriteria = new List<RubricCriteria>();
            foreach (RubricTask rt in rubricTasks)
            {
                var rubricCriteriaTemp = await _context.RubricCriteria
                .Where(rct => rct.RubricTaskId == rt.RubricTaskId)
                .ToListAsync();
                rubricCriteria.AddRange(rubricCriteriaTemp);
            }

            //Get the related RubricCriteriaScore
            List<RubricCriteriaScore> rubricCriteriaScores = new List<RubricCriteriaScore>();
            foreach (RubricCriteria rc in rubricCriteria)
            {
                var rubricCriteriaScoreTemp = await _context.RubricCriteriaScore
                    .Where(rcst => rcst.RubricCriteriaId == rc.RubricCriteriaId)
                    .ToListAsync();
                rubricCriteriaScores.AddRange(rubricCriteriaScoreTemp);
            }

            // Create the ViewModel
            var viewModel = new RubricDetailsViewModel
            {
                Rubric = rubrics,
                RubricTasks = rubricTasks,
                RubricCriterias = rubricCriteria,
                RubricCriteriaScores = rubricCriteriaScores
            };

            return View(viewModel);
        }

        // GET: Rubrics/Create
        public async Task<IActionResult> Create(string courseid = null, string role = null)
        {
            var course = await _context.CourseRoles.FindAsync(int.Parse(courseid));

            ViewBag.CourseCode = course.CourseCode;
            ViewBag.CourseName = course.CourseName;
            ViewBag.CourseTerm = course.TermName;
            ViewBag.Programme = course.Programme;
            ViewBag.Institution = course.Institution;
            ViewBag.CourseId = courseid;
            ViewBag.CurrentUserRole = role;


            return View();
        }

        // POST: Rubrics/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string courseId, string role, [Bind("RubricsId,RubricName,Institution,Programme,CourseCode,CourseName, TermName,TotalMarks,SourceFile")] Rubrics rubrics)
        {
            // Check if a rubric with the same name already exists for this course and term
            var existingRubric = await _context.Rubrics
                .FirstOrDefaultAsync(r => r.RubricName == rubrics.RubricName &&
                                         r.CourseCode == rubrics.CourseCode &&
                                         r.TermName == rubrics.TermName);

            if (existingRubric != null)
            {
                ModelState.AddModelError("RubricName", "A rubric with this name already exists for this course and term.");

                // Re-populate ViewBag data for the view
                if (!string.IsNullOrEmpty(courseId))
                {
                    var course = await _context.CourseRoles.FindAsync(int.Parse(courseId));
                    if (course != null)
                    {
                        ViewBag.CourseCode = course.CourseCode;
                        ViewBag.CourseName = course.CourseName;
                        ViewBag.CourseTerm = course.TermName;
                        ViewBag.Programme = course.Programme;
                        ViewBag.Institution = course.Institution;
                        ViewBag.CourseId = courseId;
                        ViewBag.CurrentUserRole = role;
                    }
                }

                return View(rubrics);
            }

            rubrics.TotalMarks = 0; // Set default value for TotalMarks
            rubrics.SourceFile = ""; // Set default value for SourceFile

            _context.Add(rubrics);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Rubrics", new { courseId, role });
        }

        // GET: Rubrics/Edit/5
        public async Task<IActionResult> Edit(int? id, string? courseid = null, string? role = null)
        {
            if (id == null)
            {
                return NotFound();
            }

            ViewBag.CourseId = courseid;
            ViewBag.CurrentUserRole = role;

            var rubrics = await _context.Rubrics.FindAsync(id);
            if (rubrics == null)
            {
                return NotFound();
            }
            return View(rubrics);
        }

        // POST: Rubrics/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string courseid, string role, [Bind("RubricsId,RubricName,Institution,Programme,CourseCode,CourseName,TermName,TotalMarks,SourceFile")] Rubrics rubrics)
        {
            if (id != rubrics.RubricsId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(rubrics);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RubricsExists(rubrics.RubricsId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index", "Rubrics", new { courseid, role });
            }
            return RedirectToAction("Index", "Rubrics", new { courseid, role });
        }

        // GET: Rubrics/EditTask/5
        public async Task<IActionResult> EditTask(int? id, int? rubricId, string? courseid = null, string? role = null)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rubricTask = await _context.RubricTask.FindAsync(id);
            if (rubricTask == null)
            {
                return NotFound();
            }

            ViewBag.RubricId = rubricId;
            ViewBag.CourseId = courseid;
            ViewBag.CurrentUserRole = role;

            return View(rubricTask);
        }

        // POST: Rubrics/EditTask/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]


        public async Task<IActionResult> EditTask(int id, int rubricId, string courseid, string role, [Bind("RubricTaskId,RubricsId,TaskTitle,TaskDescription,MaxMarks")] RubricTask rubricTask)
        {
            if (id != rubricTask.RubricTaskId)
            {
                return NotFound();
            }

            //if (ModelState.IsValid)
            //{
            try
            {
                _context.Update(rubricTask);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RubricTaskExists(rubricTask.RubricTaskId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction("Details", "Rubrics", new { id = rubricId, courseid, role });
            //}
        }

        // GET: Rubrics/EditCriteria/5
        public async Task<IActionResult> EditCriteria(int? criteriaId, int? rubricId, string? courseid = null, string? role = null)
        {
            if (criteriaId == null)
            {
                return NotFound();
            }

            var rubricCriteria = await _context.RubricCriteria.FindAsync(criteriaId);
            if (rubricCriteria == null)
            {
                return NotFound();
            }

            // Get existing scores for this criteria
            var existingScores = await _context.RubricCriteriaScore
                .Where(rcs => rcs.RubricCriteriaId == criteriaId.Value)
                .OrderByDescending(rcs => rcs.CriterionScore)
                .ToListAsync();

            ViewBag.RubricId = rubricId;
            ViewBag.CourseId = courseid;
            ViewBag.CurrentUserRole = role;
            ViewBag.ExistingScores = existingScores;

            return View(rubricCriteria);
        }

        // POST: Rubrics/EditCriteria/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCriteria(int criteriaId, int rubricId, string courseid, string role, [Bind("RubricCriteriaId,RubricTaskId,CriterionTitle,Weight,MaxScore")] RubricCriteria rubricCriteria)
        {
            if (criteriaId != rubricCriteria.RubricCriteriaId)
            {
                return NotFound();
            }

            // VALIDATION: Check if total weight exceeds 100% (excluding the current criteria)
            var existingCriterias = await _context.RubricCriteria
                .Where(rc => rc.RubricTaskId == rubricCriteria.RubricTaskId && rc.RubricCriteriaId != criteriaId)
                .ToListAsync();

            var currentTotalWeight = existingCriterias.Sum(rc => rc.Weight);
            var newTotalWeight = currentTotalWeight + rubricCriteria.Weight;

            if (newTotalWeight > 100)
            {
                ModelState.AddModelError("Weight", $"Updating this weight ({rubricCriteria.Weight}%) would exceed 100%. Current total (excluding this): {currentTotalWeight}%. Maximum allowed: {100 - currentTotalWeight}%");

                // Re-populate ViewBag data for the view
                ViewBag.RubricId = rubricId;
                ViewBag.CourseId = courseid;
                ViewBag.CurrentUserRole = role;

                // Get existing scores for this criteria
                var existingScores = await _context.RubricCriteriaScore
                    .Where(rcs => rcs.RubricCriteriaId == criteriaId)
                    .OrderByDescending(rcs => rcs.CriterionScore)
                    .ToListAsync();

                ViewBag.ExistingScores = existingScores;

                return View(rubricCriteria);
            }

            try
            {
                // Update the criteria
                _context.Update(rubricCriteria);
                await _context.SaveChangesAsync();

                // Update the scores
                var existingScores = await _context.RubricCriteriaScore
                    .Where(rcs => rcs.RubricCriteriaId == criteriaId)
                    .ToListAsync();

                foreach (var existingScore in existingScores)
                {
                    var scoreTitle = Request.Form["ScoreTitle_" + existingScore.CriterionScore];
                    var scoreDescription = Request.Form["ScoreDescription_" + existingScore.CriterionScore];

                    existingScore.ScoreTitle = scoreTitle;
                    existingScore.ScoreDescription = scoreDescription;
                    _context.Update(existingScore);
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RubricCriteriaExists(rubricCriteria.RubricCriteriaId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction("Details", "Rubrics", new { id = rubricId, courseid, role });
        }

        // GET: Rubrics/Delete/5
        public async Task<IActionResult> Delete(int? id, string courseId, string role)
        {
            if (id == null)
            {
                return NotFound();
            }

            ViewBag.CourseId = courseId;
            ViewBag.CurrentUserRole = role;

            var rubrics = await _context.Rubrics
                .FirstOrDefaultAsync(m => m.RubricsId == id);
            if (rubrics == null)
            {
                return NotFound();
            }

            return View(rubrics);
        }

        // POST: Rubrics/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, string courseId, string role)
        {
            var rubrics = await _context.Rubrics.FindAsync(id);
            if (rubrics != null)
            {
                // Check if rubric is being used in any assessments
                var assessmentsUsingRubric = await _context.Assessments
                    .Where(a => a.RubricsId == id)
                    .ToListAsync();

                if (assessmentsUsingRubric.Any())
                {
                    TempData["ErrorMessage"] = $"Cannot delete rubric '{rubrics.RubricName}' because it is being used in {assessmentsUsingRubric.Count} assessment(s). Please delete or reassign the assessments first.";
                    return RedirectToAction("Index", "Rubrics", new { courseId, role });
                }

                // Get all tasks for this rubric
                var rubricTasks = await _context.RubricTask
                    .Where(rt => rt.RubricsId == id)
                    .ToListAsync();

                // Get all criteria for these tasks
                var taskIds = rubricTasks.Select(rt => rt.RubricTaskId).ToList();
                var rubricCriterias = await _context.RubricCriteria
                    .Where(rc => taskIds.Contains(rc.RubricTaskId))
                    .ToListAsync();

                // Get all scores for these criteria
                var criteriaIds = rubricCriterias.Select(rc => rc.RubricCriteriaId).ToList();
                var rubricCriteriaScores = await _context.RubricCriteriaScore
                    .Where(rcs => criteriaIds.Contains(rcs.RubricCriteriaId))
                    .ToListAsync();

                // Check if any criteria are being used in student assessments
                var studentScoresUsingCriteria = await _context.StudentAssessmentScores
                    .Where(sas => criteriaIds.Contains(sas.RubricCriteriaId))
                    .ToListAsync();

                if (studentScoresUsingCriteria.Any())
                {
                    TempData["ErrorMessage"] = $"Cannot delete rubric '{rubrics.RubricName}' because it contains criteria that are being used in student assessments. Please delete the related assessments first.";
                    return RedirectToAction("Index", "Rubrics", new { courseId, role });
                }

                // Delete in the correct order to maintain referential integrity

                // 1. Delete rubric criteria scores first
                if (rubricCriteriaScores.Any())
                {
                    _context.RubricCriteriaScore.RemoveRange(rubricCriteriaScores);
                }

                // 2. Delete rubric criteria
                if (rubricCriterias.Any())
                {
                    _context.RubricCriteria.RemoveRange(rubricCriterias);
                }

                // 3. Delete rubric tasks
                if (rubricTasks.Any())
                {
                    _context.RubricTask.RemoveRange(rubricTasks);
                }

                // 4. Finally delete the rubric itself
                _context.Rubrics.Remove(rubrics);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Rubric '{rubrics.RubricName}' and all its related data have been successfully deleted.";
            }
            else
            {
                TempData["ErrorMessage"] = "Rubric not found.";
            }

            return RedirectToAction("Index", "Rubrics", new { courseId, role });
        }

        // GET: Rubrics/DeleteTask/5
        public async Task<IActionResult> DeleteTask(int? id, int? rubricId, string? courseid, string? role)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rubricTask = await _context.RubricTask
                .FirstOrDefaultAsync(m => m.RubricTaskId == id);
            if (rubricTask == null)
            {
                return NotFound();
            }

            // Pass the rubricId to the view
            ViewBag.RubricId = rubricId;
            ViewBag.CourseId = courseid;
            ViewBag.CurrentUserRole = role;

            return View(rubricTask);
        }

        // POST: Rubrics/DeleteTask/5
        [HttpPost, ActionName("DeleteTask")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTaskConfirmed(int id, int rubricId, string courseid, string role)
        {
            var rubricTask = await _context.RubricTask.FindAsync(id);
            if (rubricTask != null)
            {
                // Get all criteria for this task
                var rubricCriterias = await _context.RubricCriteria
                    .Where(rc => rc.RubricTaskId == id)
                    .ToListAsync();

                if (rubricCriterias.Any())
                {
                    // Get all criteria IDs for this task
                    var criteriaIds = rubricCriterias.Select(rc => rc.RubricCriteriaId).ToList();

                    // Check if any criteria are being used in student assessments
                    var studentScoresUsingCriteria = await _context.StudentAssessmentScores
                        .Where(sas => criteriaIds.Contains(sas.RubricCriteriaId))
                        .ToListAsync();

                    if (studentScoresUsingCriteria.Any())
                    {
                        TempData["ErrorMessage"] = $"Cannot delete task '{rubricTask.TaskTitle}' because it contains criteria that are being used in student assessments. Please delete the related assessments first.";
                        return RedirectToAction("Details", "Rubrics", new { id = rubricId, courseid, role });
                    }

                    // Get all scores for these criteria
                    var rubricCriteriaScores = await _context.RubricCriteriaScore
                        .Where(rcs => criteriaIds.Contains(rcs.RubricCriteriaId))
                        .ToListAsync();

                    // Delete in the correct order to maintain referential integrity

                    // 1. Delete rubric criteria scores first
                    if (rubricCriteriaScores.Any())
                    {
                        _context.RubricCriteriaScore.RemoveRange(rubricCriteriaScores);
                    }

                    // 2. Delete rubric criteria
                    _context.RubricCriteria.RemoveRange(rubricCriterias);
                }

                // 3. Finally delete the task itself
                _context.RubricTask.Remove(rubricTask);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Task '{rubricTask.TaskTitle}' and all its related criteria and scores have been successfully deleted.";
            }
            else
            {
                TempData["ErrorMessage"] = "Task not found.";
            }

            return RedirectToAction("Details", "Rubrics", new { id = rubricId, courseid, role });
        }

        // GET: Rubrics/DeleteCriteria/5
        public async Task<IActionResult> DeleteCriteria(int? criteriaId, int? rubricId, string? courseid = null, string? role = null)
        {
            if (criteriaId == null)
            {
                return NotFound();
            }

            var rubrics = await _context.RubricCriteria
                .FirstOrDefaultAsync(m => m.RubricCriteriaId == criteriaId);
            if (rubrics == null)
            {
                return NotFound();
            }

            // Pass the rubricId to the view
            ViewBag.RubricId = rubricId;
            ViewBag.CourseId = courseid;
            ViewBag.CurrentUserRole = role;

            return View(rubrics);
        }

        // POST: Rubrics/DeleteCriteria/5
        [HttpPost, ActionName("DeleteCriteria")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCriteriaConfirmed(int criteriaId, int rubricId, string courseid, string role)
        {
            var rubricCriteria = await _context.RubricCriteria.FindAsync(criteriaId);
            if (rubricCriteria != null)
            {
                // Check if this criteria is being used in student assessments
                var studentScoresUsingCriteria = await _context.StudentAssessmentScores
                    .Where(sas => sas.RubricCriteriaId == criteriaId)
                    .ToListAsync();

                if (studentScoresUsingCriteria.Any())
                {
                    TempData["ErrorMessage"] = $"Cannot delete criteria '{rubricCriteria.CriterionTitle}' because it is being used in student assessments. Please delete the related assessments first.";
                    return RedirectToAction("Details", "Rubrics", new { id = rubricId, courseid, role });
                }

                // Get all scores for this criteria
                var rubricCriteriaScores = await _context.RubricCriteriaScore
                    .Where(rcs => rcs.RubricCriteriaId == criteriaId)
                    .ToListAsync();

                // Delete in the correct order to maintain referential integrity

                // 1. Delete rubric criteria scores first
                if (rubricCriteriaScores.Any())
                {
                    _context.RubricCriteriaScore.RemoveRange(rubricCriteriaScores);
                }

                // 2. Finally delete the criteria itself
                _context.RubricCriteria.Remove(rubricCriteria);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Criteria '{rubricCriteria.CriterionTitle}' and all its related scores have been successfully deleted.";
            }
            else
            {
                TempData["ErrorMessage"] = "Criteria not found.";
            }

            return RedirectToAction("Details", "Rubrics", new { id = rubricId, courseid, role });
        }

        private bool RubricsExists(int id)
        {
            return _context.Rubrics.Any(e => e.RubricsId == id);
        }

        private bool RubricTaskExists(int id)
        {
            return _context.RubricTask.Any(e => e.RubricTaskId == id);
        }

        private bool RubricCriteriaExists(int id)
        {
            return _context.RubricCriteria.Any(e => e.RubricCriteriaId == id);
        }

        // GET: Rubrics/Task/CreateTask
        public async Task<IActionResult> CreateTask(int? id, string? courseid = null, string? role = null)
        {
            ViewBag.RubricId = id;
            ViewBag.CourseId = courseid;
            ViewBag.CurrentUserRole = role;
            return View();
        }

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> CreateTask(int id, string courseid, string role, [Bind("RubricTaskId,RubricsId,TaskTitle,TaskDescription,MaxMarks")] RubricTask rubricTask)
        {
            rubricTask.RubricsId = id;
            _context.Add(rubricTask);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Rubrics", new { id, courseid, role });
        }

        // GET: Rubrics/CreateTaskCriteria
        // GET: Rubrics/CreateTaskCriteria
        public async Task<IActionResult> CreateCriteria(int? id, int? rubricsId, string? courseid = null, string? role = null)
        {
            ViewBag.RubricTaskId = id;
            ViewBag.RubricId = rubricsId;
            ViewBag.CourseId = courseid;
            ViewBag.CurrentUserRole = role;

            // Get existing criteria and calculate current total weight
            if (id.HasValue)
            {
                var existingCriterias = await _context.RubricCriteria
                    .Where(rc => rc.RubricTaskId == id.Value)
                    .ToListAsync();

                var currentTotalWeight = existingCriterias.Sum(rc => rc.Weight);
                var remainingWeight = 100 - currentTotalWeight;

                ViewBag.CurrentTotalWeight = currentTotalWeight;
                ViewBag.RemainingWeight = remainingWeight;

                var firstExistingCriteria = existingCriterias.OrderBy(rc => rc.RubricCriteriaId).FirstOrDefault();

                if (firstExistingCriteria != null)
                {
                    // Get the scores for the first existing criteria
                    var existingScores = await _context.RubricCriteriaScore
                        .Where(rcs => rcs.RubricCriteriaId == firstExistingCriteria.RubricCriteriaId)
                        .OrderByDescending(rcs => rcs.CriterionScore)
                        .ToListAsync();

                    ViewBag.FirstExistingCriteria = firstExistingCriteria;
                    ViewBag.ExistingScores = existingScores;
                    ViewBag.HasExistingCriteria = true;
                }
                else
                {
                    ViewBag.FirstExistingCriteria = null;
                    ViewBag.ExistingScores = null;
                    ViewBag.HasExistingCriteria = false;
                }
            }
            else
            {
                ViewBag.CurrentTotalWeight = 0;
                ViewBag.RemainingWeight = 100;
                ViewBag.FirstExistingCriteria = null;
                ViewBag.ExistingScores = null;
                ViewBag.HasExistingCriteria = false;
            }

            return View(new RubricCriteria());
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCriteria(int id, int rubricsId, string courseid, string role, [Bind("RubricCriteriaId,RubricTaskId,CriterionTitle,Weight,MaxScore")] RubricCriteria rubricCriteria)
        {
            // VALIDATION: Check if total weight exceeds 100%
            var existingCriterias = await _context.RubricCriteria
                .Where(rc => rc.RubricTaskId == id)
                .ToListAsync();

            var currentTotalWeight = existingCriterias.Sum(rc => rc.Weight);
            var newTotalWeight = currentTotalWeight + rubricCriteria.Weight;

            if (newTotalWeight > 100)
            {
                ModelState.AddModelError("Weight", $"Adding this weight ({rubricCriteria.Weight}%) would exceed 100%. Current total: {currentTotalWeight}%. Maximum allowed: {100 - currentTotalWeight}%");

                // Re-populate ViewBag data for the view
                ViewBag.RubricTaskId = id;
                ViewBag.RubricId = rubricsId;
                ViewBag.CourseId = courseid;
                ViewBag.CurrentUserRole = role;

                // Get existing criteria data again for display
                var firstExistingCriteria = existingCriterias.OrderBy(rc => rc.RubricCriteriaId).FirstOrDefault();
                if (firstExistingCriteria != null)
                {
                    var existingScores = await _context.RubricCriteriaScore
                        .Where(rcs => rcs.RubricCriteriaId == firstExistingCriteria.RubricCriteriaId)
                        .OrderByDescending(rcs => rcs.CriterionScore)
                        .ToListAsync();

                    ViewBag.FirstExistingCriteria = firstExistingCriteria;
                    ViewBag.ExistingScores = existingScores;
                    ViewBag.HasExistingCriteria = true;
                }
                else
                {
                    ViewBag.FirstExistingCriteria = null;
                    ViewBag.ExistingScores = null;
                    ViewBag.HasExistingCriteria = false;
                }

                return View(rubricCriteria);
            }

            rubricCriteria.RubricTaskId = id;
            _context.Add(rubricCriteria);
            await _context.SaveChangesAsync();

            // Check if there's a first existing criteria in this rubric task
            var firstExistingCriteriaForScores = await _context.RubricCriteria
                .Where(rc => rc.RubricTaskId == id && rc.RubricCriteriaId != rubricCriteria.RubricCriteriaId)
                .OrderBy(rc => rc.RubricCriteriaId)
                .FirstOrDefaultAsync();

            if (firstExistingCriteriaForScores != null)
            {
                // Get the scores from the first existing criteria
                var existingScores = await _context.RubricCriteriaScore
                    .Where(rcs => rcs.RubricCriteriaId == firstExistingCriteriaForScores.RubricCriteriaId)
                    .OrderByDescending(rcs => rcs.CriterionScore)
                    .ToListAsync();

                // Assign scores from the first criteria to the new criteria
                foreach (var existingScore in existingScores)
                {
                    // Only create scores up to the new criteria's MaxScore
                    if (existingScore.CriterionScore <= rubricCriteria.MaxScore)
                    {
                        var newScore = new RubricCriteriaScore
                        {
                            RubricCriteriaId = rubricCriteria.RubricCriteriaId,
                            CriterionScore = existingScore.CriterionScore,
                            ScoreTitle = existingScore.ScoreTitle,
                            ScoreDescription = existingScore.ScoreDescription
                        };
                        _context.RubricCriteriaScore.Add(newScore);
                    }
                }

                // If the new criteria has a higher MaxScore, fill in the remaining scores from form data
                var maxExistingScore = existingScores.Max(es => es.CriterionScore);
                if (rubricCriteria.MaxScore > maxExistingScore)
                {
                    for (int score = maxExistingScore + 1; score <= rubricCriteria.MaxScore; score++)
                    {
                        string scoreTitle = Request.Form["ScoreTitle_" + score];
                        string scoreDescription = Request.Form["ScoreDescription_" + score];
                        var rubricCriteriaScore = new RubricCriteriaScore
                        {
                            RubricCriteriaId = rubricCriteria.RubricCriteriaId,
                            CriterionScore = score,
                            ScoreTitle = scoreTitle,
                            ScoreDescription = scoreDescription
                        };
                        _context.RubricCriteriaScore.Add(rubricCriteriaScore);
                    }
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                // No existing criteria, use the form data as before
                for (int score = rubricCriteria.MaxScore; score >= 0; score--)
                {
                    string scoreTitle = Request.Form["ScoreTitle_" + score];
                    string scoreDescription = Request.Form["ScoreDescription_" + score];
                    var rubricCriteriaScore = new RubricCriteriaScore
                    {
                        RubricCriteriaId = rubricCriteria.RubricCriteriaId,
                        CriterionScore = score,
                        ScoreTitle = scoreTitle,
                        ScoreDescription = scoreDescription
                    };
                    _context.RubricCriteriaScore.Add(rubricCriteriaScore);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction("Details", "Rubrics", new { id = rubricsId, courseid, role });
        }

        // GET: UploadRubrics
        public async Task<IActionResult> Upload(string courseid = null, string role = null)
        {
            var course = await _context.CourseRoles.FindAsync(int.Parse(courseid));

            ViewBag.CourseId = courseid;
            ViewBag.CurrentUserRole = role;

            return View();
        }

        // POST: UploadRubrics
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(string courseId, string role, IFormFile rubricsFile)
        {
            if (rubricsFile == null || rubricsFile.Length == 0)
            {
                ModelState.AddModelError("rubricsFile", "Please upload your Rubrics File.");
                return RedirectToAction("Upload", "Rubrics");
            }

            // Check file extension
            var extension = Path.GetExtension(rubricsFile.FileName).ToLower();
            if (extension != ".doc" && extension != ".docx")
            {
                ModelState.AddModelError("rubricsFile", "Only Word documents are allowed.");
                return RedirectToAction("Upload", "Rubrics");
            }

            // Check file size (limit to 10MB)
            if (rubricsFile.Length > 10 * 1024 * 1024)
            {
                ModelState.AddModelError("rubricsFile", "File size must be less than 10MB.");
                return RedirectToAction("Upload", "Rubrics");
            }

            try
            {                
                var rubric = new Rubrics();
                List<string> rubricsParagraphs = new List<string>();
                List<RubricTask> rubricTasks = new List<RubricTask>();
                List<RubricCriteria> rubricCriterias = new List<RubricCriteria>();
                List<RubricCriteriaScore> rubricCriteriaScores = new List<RubricCriteriaScore>();
                if (extension == ".docx")
                {
                    using (var stream = rubricsFile.OpenReadStream())
                    {
                        XWPFDocument docx = new XWPFDocument(stream);

                        // Extract rubrics paragraphs
                        foreach (var para in docx.Paragraphs)
                        {
                            if (!string.IsNullOrWhiteSpace(para.ParagraphText))
                            {
                                rubricsParagraphs.Add(para.ParagraphText.Trim());
                                if (rubricsParagraphs.Count >= 4)
                                    break;
                            }
                        }

                        // Extract rubric criteria from tables
                        int tableIndex = 0;
                        int taskIndex = 0;
                        bool isTableRead = false;
                        foreach (var table in docx.Tables)
                        {
                            isTableRead = false;
                            // Extract rubric criteria from ONLY the first table
                            if (docx.Tables.Count > 0)
                            {
                                if (tableIndex == 0)
                                {
                                    //Fetch rubrics tasks from the first table
                                    var rows = table.Rows;

                                    // Skip header row (index 0) and process data rows
                                    for (int i = 1; i < rows.Count; i++)
                                    {
                                        var row = rows[i];
                                        var cells = row.GetTableCells();

                                        if (cells.Count >= 3) // Ensure we have at least 3 columns
                                        {
                                            var task = new RubricTask
                                            {
                                                TaskTitle = GetCellText(cells[0]),
                                                TaskDescription = GetCellText(cells[1]),
                                                MaxMarks = ParseMaxMarks(GetCellText(cells[2]))
                                            };

                                            // Only add if we have meaningful data
                                            if (!string.IsNullOrWhiteSpace(task.TaskTitle))
                                            {
                                                rubricTasks.Add(task);
                                            }

                                            isTableRead = true;
                                        }
                                    }
                                }
                                else
                                {
                                    //Fetch rubrics tasks criterias from the remaining table
                                    var rows = table.Rows;

                                    // Extract score headers from the first row (header row)
                                    var headerRow = rows[0];                                    
                                    var headerCells = headerRow.GetTableCells();

                                    if (headerCells.Count >= 4) // Ensure we have at least 4 columns
                                    {
                                        var checkRow = rows[1];
                                        var checkCells = checkRow.GetTableCells();
                                        string[] scoreHeaders = new string[headerCells.Count];
                                        int maxScore = -1;

                                    
                                        for (int col = 2; col < headerCells.Count; col++)
                                        {
                                            if (GetCellText(checkCells[col]).Trim() == "")
                                            {
                                                break; // Stop if we encounter an empty cell
                                            }

                                            string scoreHeader = GetCellText(headerCells[col]);
                                            // Clean up the score header by removing numbering or dashes if present
                                            scoreHeader = System.Text.RegularExpressions.Regex.Replace(scoreHeader, @"^[\d\-\–\.\s]+", "").Trim();
                                            scoreHeaders[col - 2] = scoreHeader;
                                            maxScore++;
                                        }
                                    

                                        // Skip header row (index 0) and process data rows
                                        for (int i = 1; i < rows.Count; i++)
                                        {
                                            var row = rows[i];
                                            var cells = row.GetTableCells();

                                            var rubricCriteria = new RubricCriteria
                                            {
                                                RubricTaskId = tableIndex - 1,      // Link to the correct RubricTask (temp value=0,1,2,3,...)
                                                CriterionTitle = GetCellText(cells[0]),
                                                Weight = double.TryParse(GetCellText(cells[1]).TrimEnd('%'), out double weight) ? weight : 0,
                                                MaxScore = maxScore
                                            };
                                            rubricCriterias.Add(rubricCriteria);

                                            // Extract scores for this criterion
                                            for (int j = 0; j <= maxScore; j++)
                                            {
                                                var rubricCriteriaScore = new RubricCriteriaScore
                                                {
                                                    RubricCriteriaId = taskIndex,       // Link to the correct RubricCriteria (temp value=0,1,2,3,...)
                                                    CriterionScore = maxScore - j,
                                                    ScoreTitle = j < scoreHeaders.Length ? scoreHeaders[j] : "", // Use header from first row
                                                    ScoreDescription = GetCellText(cells[j + 2])
                                                };
                                                rubricCriteriaScores.Add(rubricCriteriaScore);
                                            }
                                            taskIndex++;
                                            isTableRead = true;
                                        }
                                    }
                                }
                                string fullText = rubricsParagraphs.Count > 1 ? rubricsParagraphs[1] : "";
                                int firstSpaceIndex = fullText.IndexOf(' ');

                                var course = await _context.CourseRoles.FindAsync(int.Parse(courseId));

                                rubric.Institution = course.Institution;
                                rubric.Programme = rubricsParagraphs[0];
                                rubric.CourseCode = firstSpaceIndex > 0 ? fullText.Substring(0, firstSpaceIndex) : fullText;
                                rubric.CourseName = firstSpaceIndex > 0 ? fullText.Substring(firstSpaceIndex + 1).Trim() : "";
                                rubric.RubricName = rubricsParagraphs.Count > 2 ? rubricsParagraphs[2] : "";
                                rubric.TermName = rubricsParagraphs[3].Replace(" ","");
                                rubric.TotalMarks = rubricTasks.Sum(t => t.MaxMarks);
                                rubric.SourceFile = $"{rubricsFile.FileName} (Size: {rubricsFile.Length} bytes, Uploaded: {DateTime.Now:yyyy-MM-dd HH:mm:ss})";

                                // VALIDATION 1: Check if course information matches current course context
                                if (!string.IsNullOrEmpty(courseId))
                                {
                                    
                                    if (course != null)
                                    {
                                        bool hasValidationError = false;

                                        if (!string.Equals(rubric.Programme, course.Programme, StringComparison.OrdinalIgnoreCase))
                                        {
                                            ModelState.AddModelError("", $"Programme mismatch: Document has '{rubric.Programme}' but expected '{course.Programme}'");
                                            hasValidationError = true;
                                        }

                                        if (!string.Equals(rubric.CourseCode, course.CourseCode, StringComparison.OrdinalIgnoreCase))
                                        {
                                            ModelState.AddModelError("", $"Course Code mismatch: Document has '{rubric.CourseCode}' but expected '{course.CourseCode}'");
                                            hasValidationError = true;
                                        }

                                        if (!string.Equals(rubric.CourseName, course.CourseName, StringComparison.OrdinalIgnoreCase))
                                        {
                                            ModelState.AddModelError("", $"Course Name mismatch: Document has '{rubric.CourseName}' but expected '{course.CourseName}'");
                                            hasValidationError = true;
                                        }

                                        if (!string.Equals(rubric.TermName, course.TermName, StringComparison.OrdinalIgnoreCase))
                                        {
                                            ModelState.AddModelError("", $"Term Name mismatch: Document has '{rubric.TermName}' but expected '{course.TermName}'");
                                            hasValidationError = true;
                                        }

                                        if (hasValidationError)
                                        {
                                            ViewBag.CourseId = courseId;
                                            ViewBag.CurrentUserRole = role;
                                            return View();
                                        }
                                    }
                                }

                                // VALIDATION 2: Check if rubric with same course code, term name, and rubric name already exists
                                var existingRubric = await _context.Rubrics
                                    .FirstOrDefaultAsync(r => r.CourseCode == rubric.CourseCode &&
                                                             r.TermName == rubric.TermName &&
                                                             r.RubricName == rubric.RubricName);

                                if (existingRubric != null)
                                {
                                    ModelState.AddModelError("", $"A rubric with the name '{rubric.RubricName}' already exists for course '{rubric.CourseCode}' in term '{rubric.TermName}'");

                                    ViewBag.CourseId = courseId;
                                    ViewBag.CurrentUserRole = role;
                                    return View();
                                }

                                if (isTableRead)
                                {
                                    tableIndex++;
                                }
                            }

                        }

                        // Save rubric first to get the RubricsId
                        _context.Add(rubric);
                        await _context.SaveChangesAsync();

                        // Save rubric tasks with the rubric ID
                        foreach (var task in rubricTasks)
                        {
                            task.RubricsId = rubric.RubricsId;
                            _context.RubricTask.Add(task);
                        }
                        await _context.SaveChangesAsync();

                        // Save rubric criterias with the correct RubricTaskId
                        foreach (var criteria in rubricCriterias)
                        {
                            var correspondingTask = rubricTasks[criteria.RubricTaskId];
                            criteria.RubricTaskId = correspondingTask.RubricTaskId;
                            _context.RubricCriteria.Add(criteria);
                        }
                        await _context.SaveChangesAsync();

                        // Save rubric criteria scores with the correct RubricCriteriaId
                        foreach (var score in rubricCriteriaScores)
                        {
                            var correspondingCriteria = rubricCriterias[score.RubricCriteriaId];
                            score.RubricCriteriaId = correspondingCriteria.RubricCriteriaId;
                            _context.RubricCriteriaScore.Add(score);
                        }
                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = $"Your rubrics has been submitted successfully! {rubricTasks.Count} tasks extracted.";
                    }
                }
                return RedirectToAction("Details", "Rubrics", new {id = rubric.RubricsId, courseId, role });
            }
            catch (Exception ex)

            {
                ModelState.AddModelError("", $"An error occurred while uploading your Rubrics: {ex.Message}");
                ViewBag.CourseId = courseId;
                ViewBag.CurrentUserRole = role;
                return View();
            }
        }

        // Helper method to extract text from table cell
        private string GetCellText(XWPFTableCell cell)
        {
            if (cell == null) return "";

            var text = cell.GetText().Trim();
            // Remove extra whitespace and line breaks
            return System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        }

        // Helper method to parse marks from text
        private int ParseMaxMarks(string marksText)
        {
            if (string.IsNullOrWhiteSpace(marksText)) return 0;

            // Try to extract numbers from the text
            var numbers = System.Text.RegularExpressions.Regex.Matches(marksText, @"\d+");
            if (numbers.Count > 0)
            {
                if (int.TryParse(numbers[0].Value, out int marks))
                {
                    return marks;
                }
            }

            return 0;
        }
    }
}
