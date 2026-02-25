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
        private readonly ILogger<RubricsController> _logger;

        public RubricsController(ApplicationDbContext context, IWebHostEnvironment hostingEnvironment, UserManager<ApplicationUser> userManager, ILogger<RubricsController> logger)
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
            _logger.LogInformation("Index action called with courseId: {CourseId}, role: {Role}", courseId, role);

            try
            {
                // Get current user
                var currentUser = await _userManager.GetUserAsync(User);
                var userId = currentUser?.UserName;

                _logger.LogInformation("Current user: {UserId}", userId ?? "Anonymous");

                if (string.IsNullOrEmpty(courseId))
                {
                    _logger.LogWarning("CourseId is null or empty");
                    return BadRequest("Course ID is required");
                }

                if (!int.TryParse(courseId, out int parsedCourseId))
                {
                    _logger.LogWarning("Invalid courseId format: {CourseId}", courseId);
                    return BadRequest("Invalid course ID format");
                }

                var course = await _context.CourseRoles.FindAsync(parsedCourseId);
                if (course == null)
                {
                    _logger.LogWarning("Course not found for ID: {CourseId}", courseId);
                    return NotFound("Course not found");
                }

                _logger.LogDebug("Found course: {CourseCode} - {CourseName}", course.CourseCode, course.CourseName);

                // Start with all rubrics
                var rubricsQuery = _context.Rubrics.AsQueryable();

                // Apply filters if parameters are provided
                if (!string.IsNullOrEmpty(course.CourseCode))
                {
                    rubricsQuery = rubricsQuery.Where(r => r.CourseCode == course.CourseCode);
                    _logger.LogDebug("Filtered by course code: {CourseCode}", course.CourseCode);
                }

                if (course.Year > 0)
                {
                    rubricsQuery = rubricsQuery.Where(r => r.Year == course.Year);
                    _logger.LogDebug("Filtered by year: {Year}", course.Year);
                }

                if (course.Trimester > 0)
                {
                    rubricsQuery = rubricsQuery.Where(r => r.Trimester == course.Trimester);
                    _logger.LogDebug("Filtered by trimester: {Trimester}", course.Trimester);
                }

                // Apply user authorization check
                if (!string.IsNullOrEmpty(role))
                {
                    _logger.LogDebug("Checking access for role: {Role}, user: {UserId}", role, userId);

                    // Verify user has the specified role for the course
                    var hasAccess = await _context.CourseRoles
                        .AnyAsync(cr => cr.CourseCode == course.CourseCode &&
                                       cr.Year == course.Year &&
                                       cr.Trimester == course.Trimester &&
                                       ((role == "Lecturer" && cr.RoleLecturer == userId) ||
                                        (role == "Moderator" && cr.RoleModerator == userId) ||
                                        (role == "Admin")));

                    if (!hasAccess)
                    {
                        _logger.LogWarning("Access denied for user {UserId} with role {Role} for course {CourseCode}", userId, role, course.CourseCode);
                        TempData["ErrorMessage"] = "You don't have permission to access rubrics for this course.";
                        return RedirectToAction("Index", "Home");
                    }

                    _logger.LogInformation("Access granted for user {UserId} with role {Role}", userId, role);
                }

                var rubrics = await rubricsQuery.ToListAsync();
                _logger.LogInformation("Retrieved {RubricCount} rubrics for course {CourseCode}", rubrics.Count, course.CourseCode);

                // Get full names for lecturer and moderator
                string lecturerFullName = null;
                string moderatorFullName = null;

                if (!string.IsNullOrEmpty(course.RoleLecturer))
                {
                    var lecturer = await _userManager.FindByNameAsync(course.RoleLecturer);
                    lecturerFullName = lecturer?.FullName ?? course.RoleLecturer;
                    _logger.LogDebug("Retrieved lecturer: {LecturerName} for username {Username}", lecturerFullName, course.RoleLecturer);
                }

                if (!string.IsNullOrEmpty(course.RoleModerator))
                {
                    var moderator = await _userManager.FindByNameAsync(course.RoleModerator);
                    moderatorFullName = moderator?.FullName ?? course.RoleModerator;
                    _logger.LogDebug("Retrieved moderator: {ModeratorName} for username {Username}", moderatorFullName, course.RoleModerator);
                }

                // Set ViewBag data for the view to display filtering context
                ViewBag.FilteredCourseCode = course.CourseCode;
                ViewBag.FilteredCourseName = course.CourseName;
                ViewBag.FilteredCourseYear = course.Year;
                ViewBag.FilteredCourseTrimester = course.Trimester;
                ViewBag.CourseId = course.CourseRolesId.ToString();
                ViewBag.CurrentUserRole = role;
                ViewBag.LecturerName = lecturerFullName;
                ViewBag.ModeratorName = moderatorFullName;

                return View(rubrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Index action for courseId: {CourseId}, role: {Role}", courseId, role);
                throw;
            }
        }

        // GET: Rubrics/Details/5
        public async Task<IActionResult> Details(int? id, string? courseid, string? role)
        {
            _logger.LogInformation("Details action called with id: {RubricId}, courseId: {CourseId}, role: {Role}", id, courseid, role);

            if (id == null)
            {
                _logger.LogWarning("Rubric ID is null in Details action");
                return NotFound();
            }

            try
            {
                ViewBag.CourseId = courseid;
                ViewBag.CurrentUserRole = role;

                var rubrics = await _context.Rubrics
                    .FirstOrDefaultAsync(m => m.RubricsId == id);
                if (rubrics == null)
                {
                    _logger.LogWarning("Rubric not found for ID: {RubricId}", id);
                    return NotFound();
                }

                _logger.LogDebug("Found rubric: {RubricName} (ID: {RubricId})", rubrics.RubricName, id);

                // Get the related RubricTasks
                var rubricTasks = await _context.RubricTask
                    .Where(rt => rt.RubricsId == id)
                    .ToListAsync();

                _logger.LogDebug("Found {TaskCount} tasks for rubric {RubricId}", rubricTasks.Count, id);

                // Get the related RubricCriteria
                List<RubricCriteria> rubricCriteria = new List<RubricCriteria>();
                foreach (RubricTask rt in rubricTasks)
                {
                    var rubricCriteriaTemp = await _context.RubricCriteria
                    .Where(rct => rct.RubricTaskId == rt.RubricTaskId)
                    .ToListAsync();
                    rubricCriteria.AddRange(rubricCriteriaTemp);
                }

                _logger.LogDebug("Found {CriteriaCount} criteria for rubric {RubricId}", rubricCriteria.Count, id);

                //Get the related RubricCriteriaScore
                List<RubricCriteriaScore> rubricCriteriaScores = new List<RubricCriteriaScore>();
                foreach (RubricCriteria rc in rubricCriteria)
                {
                    var rubricCriteriaScoreTemp = await _context.RubricCriteriaScore
                        .Where(rcst => rcst.RubricCriteriaId == rc.RubricCriteriaId)
                        .ToListAsync();
                    rubricCriteriaScores.AddRange(rubricCriteriaScoreTemp);
                }

                _logger.LogDebug("Found {ScoreCount} scores for rubric {RubricId}", rubricCriteriaScores.Count, id);

                // Create the ViewModel
                var viewModel = new RubricDetailsViewModel
                {
                    Rubric = rubrics,
                    RubricTasks = rubricTasks,
                    RubricCriterias = rubricCriteria,
                    RubricCriteriaScores = rubricCriteriaScores
                };

                _logger.LogInformation("Successfully loaded details for rubric {RubricId} with {TaskCount} tasks, {CriteriaCount} criteria, {ScoreCount} scores",
                    id, rubricTasks.Count, rubricCriteria.Count, rubricCriteriaScores.Count);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Details action for rubric ID: {RubricId}", id);
                throw;
            }
        }

        // GET: Rubrics/Create
        public async Task<IActionResult> Create(string courseid = null, string role = null)
        {
            _logger.LogInformation("Create GET action called with courseId: {CourseId}, role: {Role}", courseid, role);

            try
            {
                if (!string.IsNullOrEmpty(courseid) && int.TryParse(courseid, out int parsedCourseId))
                {
                    var course = await _context.CourseRoles.FindAsync(parsedCourseId);
                    if (course != null)
                    {
                        _logger.LogDebug("Loading create form for course: {CourseCode} - {CourseName}", course.CourseCode, course.CourseName);

                        ViewBag.CourseCode = course.CourseCode;
                        ViewBag.CourseName = course.CourseName;
                        ViewBag.CourseYear = course.Year;
                        ViewBag.CourseTrimester = course.Trimester;
                        ViewBag.Programme = course.Programme;
                        ViewBag.Institution = course.Institution;
                        ViewBag.CourseId = courseid;
                        ViewBag.CurrentUserRole = role;
                    }
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create GET action for courseId: {CourseId}", courseid);
                throw;
            }
        }

        // POST: Rubrics/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string courseId, string role, [Bind("RubricsId,RubricName,Institution,Programme,CourseCode,CourseName, TermName,TotalMarks,SourceFile")] Rubrics rubrics)
        {
            _logger.LogInformation("Create POST action called for rubric: {RubricName}, courseId: {CourseId}", rubrics?.RubricName, courseId);

            try
            {
                // Check if a rubric with the same name already exists for this course and year/trimester
                var existingRubric = await _context.Rubrics
                    .FirstOrDefaultAsync(r => r.RubricName == rubrics.RubricName &&
                                             r.CourseCode == rubrics.CourseCode &&
                                             r.Year == rubrics.Year &&
                                             r.Trimester == rubrics.Trimester);

                if (existingRubric != null)
                {
                    _logger.LogWarning("Attempted to create duplicate rubric: {RubricName} for course {CourseCode} in year {Year}, trimester {Trimester}",
                        rubrics.RubricName, rubrics.CourseCode, rubrics.Year, rubrics.Trimester);

                    ModelState.AddModelError("RubricName", "A rubric with this name already exists for this course and term.");

                    // Re-populate ViewBag data for the view
                    if (!string.IsNullOrEmpty(courseId))
                    {
                        var course = await _context.CourseRoles.FindAsync(int.Parse(courseId));
                        if (course != null)
                        {
                            ViewBag.CourseCode = course.CourseCode;
                            ViewBag.CourseName = course.CourseName;
                            ViewBag.CourseYear = course.Year;
                            ViewBag.CourseTrimester = course.Trimester;
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

                _logger.LogInformation("Successfully created rubric: {RubricName} (ID: {RubricId}) for course {CourseCode}",
                    rubrics.RubricName, rubrics.RubricsId, rubrics.CourseCode);

                return RedirectToAction("Index", "Rubrics", new { courseId, role });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating rubric: {RubricName} for courseId: {CourseId}", rubrics?.RubricName, courseId);
                throw;
            }
        }

        // GET: Rubrics/Edit/5
        public async Task<IActionResult> Edit(int? id, string? courseid = null, string? role = null)
        {
            _logger.LogInformation("Edit GET action called with id: {RubricId}, courseId: {CourseId}, role: {Role}", id, courseid, role);

            if (id == null)
            {
                _logger.LogWarning("Rubric ID is null in Edit action");
                return NotFound();
            }

            try
            {
                ViewBag.CourseId = courseid;
                ViewBag.CurrentUserRole = role;

                var rubrics = await _context.Rubrics.FindAsync(id);
                if (rubrics == null)
                {
                    _logger.LogWarning("Rubric not found for edit: ID {RubricId}", id);
                    return NotFound();
                }

                _logger.LogDebug("Loading edit form for rubric: {RubricName} (ID: {RubricId})", rubrics.RubricName, id);
                return View(rubrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Edit GET action for rubric ID: {RubricId}", id);
                throw;
            }
        }

        // POST: Rubrics/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string courseid, string role, [Bind("RubricsId,RubricName,Institution,Programme,CourseCode,CourseName,TermName,TotalMarks,SourceFile")] Rubrics rubrics)
        {
            _logger.LogInformation("Edit POST action called for rubric ID: {RubricId}", id);

            if (id != rubrics.RubricsId)
            {
                _logger.LogWarning("ID mismatch in Edit POST: URL ID {UrlId} vs Model ID {ModelId}", id, rubrics.RubricsId);
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(rubrics);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Successfully updated rubric: {RubricName} (ID: {RubricId})", rubrics.RubricName, id);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogWarning(ex, "Concurrency conflict while updating rubric ID: {RubricId}", id);

                    if (!RubricsExists(rubrics.RubricsId))
                    {
                        _logger.LogWarning("Rubric {RubricId} no longer exists during update", rubrics.RubricsId);
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating rubric ID: {RubricId}", id);
                    throw;
                }
                return RedirectToAction("Index", "Rubrics", new { courseid, role });
            }

            _logger.LogWarning("Model validation failed for rubric edit ID: {RubricId}. Errors: {ValidationErrors}",
                id, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

            return RedirectToAction("Index", "Rubrics", new { courseid, role });
        }

        // GET: Rubrics/EditTask/5
        public async Task<IActionResult> EditTask(int? id, int? rubricId, string? courseid = null, string? role = null)
        {
            _logger.LogInformation("EditTask GET action called with taskId: {TaskId}, rubricId: {RubricId}", id, rubricId);

            if (id == null)
            {
                _logger.LogWarning("Task ID is null in EditTask action");
                return NotFound();
            }

            try
            {
                var rubricTask = await _context.RubricTask.FindAsync(id);
                if (rubricTask == null)
                {
                    _logger.LogWarning("Task not found for edit: ID {TaskId}", id);
                    return NotFound();
                }

                _logger.LogDebug("Loading edit form for task: {TaskTitle} (ID: {TaskId})", rubricTask.TaskTitle, id);

                ViewBag.RubricId = rubricId;
                ViewBag.CourseId = courseid;
                ViewBag.CurrentUserRole = role;

                return View(rubricTask);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EditTask GET action for task ID: {TaskId}", id);
                throw;
            }
        }

        // POST: Rubrics/EditTask/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]


        public async Task<IActionResult> EditTask(int id, int rubricId, string courseid, string role, [Bind("RubricTaskId,RubricsId,TaskTitle,TaskDescription,MaxMarks")] RubricTask rubricTask)
        {
            _logger.LogInformation("EditTask POST action called for task ID: {TaskId}", id);

            if (id != rubricTask.RubricTaskId)
            {
                _logger.LogWarning("ID mismatch in EditTask POST: URL ID {UrlId} vs Model ID {ModelId}", id, rubricTask.RubricTaskId);
                return NotFound();
            }

            try
            {
                _context.Update(rubricTask);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated task: {TaskTitle} (ID: {TaskId})", rubricTask.TaskTitle, id);

                return RedirectToAction("Details", "Rubrics", new { id = rubricId, courseid, role });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while updating task ID: {TaskId}", id);

                if (!RubricTaskExists(rubricTask.RubricTaskId))
                {
                    _logger.LogWarning("Task {TaskId} no longer exists during update", rubricTask.RubricTaskId);
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task ID: {TaskId}", id);
                throw;
            }
        }

        // GET: Rubrics/EditCriteria/5
        public async Task<IActionResult> EditCriteria(int? criteriaId, int? rubricId, string? courseid = null, string? role = null)
        {
            _logger.LogInformation("EditCriteria GET action called with criteriaId: {CriteriaId}, rubricId: {RubricId}", criteriaId, rubricId);

            if (criteriaId == null)
            {
                _logger.LogWarning("Criteria ID is null in EditCriteria action");
                return NotFound();
            }

            try
            {
                var rubricCriteria = await _context.RubricCriteria.FindAsync(criteriaId);
                if (rubricCriteria == null)
                {
                    _logger.LogWarning("Criteria not found for edit: ID {CriteriaId}", criteriaId);
                    return NotFound();
                }

                _logger.LogDebug("Loading edit form for criteria: {CriterionTitle} (ID: {CriteriaId})", rubricCriteria.CriterionTitle, criteriaId);

                // Get existing scores for this criteria
                var existingScores = await _context.RubricCriteriaScore
                    .Where(rcs => rcs.RubricCriteriaId == criteriaId.Value)
                    .OrderByDescending(rcs => rcs.CriterionScore)
                    .ToListAsync();

                _logger.LogDebug("Found {ScoreCount} existing scores for criteria {CriteriaId}", existingScores.Count, criteriaId);

                ViewBag.RubricId = rubricId;
                ViewBag.CourseId = courseid;
                ViewBag.CurrentUserRole = role;
                ViewBag.ExistingScores = existingScores;

                return View(rubricCriteria);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EditCriteria GET action for criteria ID: {CriteriaId}", criteriaId);
                throw;
            }
        }

        // POST: Rubrics/EditCriteria/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCriteria(int criteriaId, int rubricId, string courseid, string role, [Bind("RubricCriteriaId,RubricTaskId,CriterionTitle,Weight,MaxScore")] RubricCriteria rubricCriteria)
        {
            _logger.LogInformation("EditCriteria POST action called for criteria ID: {CriteriaId}", criteriaId);

            if (criteriaId != rubricCriteria.RubricCriteriaId)
            {
                _logger.LogWarning("ID mismatch in EditCriteria POST: URL ID {UrlId} vs Model ID {ModelId}", criteriaId, rubricCriteria.RubricCriteriaId);
                return NotFound();
            }

            try
            {
                // VALIDATION: Check if total weight exceeds 100% (excluding the current criteria)
                var existingCriterias = await _context.RubricCriteria
                    .Where(rc => rc.RubricTaskId == rubricCriteria.RubricTaskId && rc.RubricCriteriaId != criteriaId)
                    .ToListAsync();

                var currentTotalWeight = existingCriterias.Sum(rc => rc.Weight);
                var newTotalWeight = currentTotalWeight + rubricCriteria.Weight;

                _logger.LogDebug("Weight validation for criteria {CriteriaId}: current total {CurrentWeight}%, new total would be {NewWeight}%",
                    criteriaId, currentTotalWeight, newTotalWeight);

                if (newTotalWeight > 100)
                {
                    _logger.LogWarning("Weight validation failed for criteria {CriteriaId}: new total {NewWeight}% exceeds 100%",
                        criteriaId, newTotalWeight);

                    ModelState.AddModelError("Weight", $"Updating this weight ({rubricCriteria.Weight}%) would exceed 100%. Current total (excluding this): {currentTotalWeight}%. Maximum allowed: {100 - currentTotalWeight}%");

                    // Re-populate ViewBag data for the view
                    ViewBag.RubricId = rubricId;
                    ViewBag.CourseId = courseid;
                    ViewBag.CurrentUserRole = role;

                    // Get existing scores for this criteria
                    var existingScoresView = await _context.RubricCriteriaScore
                        .Where(rcs => rcs.RubricCriteriaId == criteriaId)
                        .OrderByDescending(rcs => rcs.CriterionScore)
                        .ToListAsync();

                    ViewBag.ExistingScores = existingScoresView;

                    return View(rubricCriteria);
                }

                // Update the criteria
                _context.Update(rubricCriteria);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated criteria: {CriterionTitle} (ID: {CriteriaId})", rubricCriteria.CriterionTitle, criteriaId);

                // Update the scores
                var existingScores = await _context.RubricCriteriaScore
                    .Where(rcs => rcs.RubricCriteriaId == criteriaId)
                    .ToListAsync();

                _logger.LogDebug("Updating {ScoreCount} scores for criteria {CriteriaId}", existingScores.Count, criteriaId);

                foreach (var existingScore in existingScores)
                {
                    var scoreTitle = Request.Form["ScoreTitle_" + existingScore.CriterionScore];
                    var scoreDescription = Request.Form["ScoreDescription_" + existingScore.CriterionScore];

                    existingScore.ScoreTitle = scoreTitle;
                    existingScore.ScoreDescription = scoreDescription;
                    _context.Update(existingScore);
                }

                await _context.SaveChangesAsync();
                _logger.LogDebug("Successfully updated scores for criteria {CriteriaId}", criteriaId);

                return RedirectToAction("Details", "Rubrics", new { id = rubricId, courseid, role });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while updating criteria ID: {CriteriaId}", criteriaId);

                if (!RubricCriteriaExists(rubricCriteria.RubricCriteriaId))
                {
                    _logger.LogWarning("Criteria {CriteriaId} no longer exists during update", rubricCriteria.RubricCriteriaId);
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating criteria ID: {CriteriaId}", criteriaId);
                throw;
            }
        }

        // GET: Rubrics/Delete/5
        public async Task<IActionResult> Delete(int? id, string courseId, string role)
        {
            _logger.LogInformation("Delete GET action called with id: {RubricId}, courseId: {CourseId}, role: {Role}", id, courseId, role);

            if (id == null)
            {
                _logger.LogWarning("Rubric ID is null in Delete action");
                return NotFound();
            }

            try
            {
                ViewBag.CourseId = courseId;
                ViewBag.CurrentUserRole = role;

                var rubrics = await _context.Rubrics
                    .FirstOrDefaultAsync(m => m.RubricsId == id);
                if (rubrics == null)
                {
                    _logger.LogWarning("Rubric not found for delete: ID {RubricId}", id);
                    return NotFound();
                }

                _logger.LogDebug("Loading delete confirmation for rubric: {RubricName} (ID: {RubricId})", rubrics.RubricName, id);
                return View(rubrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Delete GET action for rubric ID: {RubricId}", id);
                throw;
            }
        }

        // POST: Rubrics/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, string courseId, string role)
        {
            _logger.LogInformation("Delete POST action called for rubric ID: {RubricId}", id);

            try
            {
                var rubrics = await _context.Rubrics.FindAsync(id);
                if (rubrics != null)
                {
                    _logger.LogDebug("Found rubric to delete: {RubricName} (ID: {RubricId})", rubrics.RubricName, id);

                    // Check if rubric is being used in any assessments
                    var assessmentsUsingRubric = await _context.Assessments
                        .Where(a => a.RubricsId == id)
                        .ToListAsync();

                    if (assessmentsUsingRubric.Any())
                    {
                        _logger.LogWarning("Cannot delete rubric {RubricId} - in use by {AssessmentCount} assessments",
                            id, assessmentsUsingRubric.Count);

                        TempData["ErrorMessage"] = $"Cannot delete rubric '{rubrics.RubricName}' because it is being used in {assessmentsUsingRubric.Count} assessment(s). Please delete or reassign the assessments first.";
                        return RedirectToAction("Index", "Rubrics", new { courseId, role });
                    }

                    // Get all tasks for this rubric
                    var rubricTasks = await _context.RubricTask
                        .Where(rt => rt.RubricsId == id)
                        .ToListAsync();

                    _logger.LogDebug("Found {TaskCount} tasks to delete for rubric {RubricId}", rubricTasks.Count, id);

                    // Get all criteria for these tasks
                    var taskIds = rubricTasks.Select(rt => rt.RubricTaskId).ToList();
                    var rubricCriterias = await _context.RubricCriteria
                        .Where(rc => taskIds.Contains(rc.RubricTaskId))
                        .ToListAsync();

                    _logger.LogDebug("Found {CriteriaCount} criteria to delete for rubric {RubricId}", rubricCriterias.Count, id);

                    // Get all scores for these criteria
                    var criteriaIds = rubricCriterias.Select(rc => rc.RubricCriteriaId).ToList();
                    var rubricCriteriaScores = await _context.RubricCriteriaScore
                        .Where(rcs => criteriaIds.Contains(rcs.RubricCriteriaId))
                        .ToListAsync();

                    _logger.LogDebug("Found {ScoreCount} scores to delete for rubric {RubricId}", rubricCriteriaScores.Count, id);

                    // Check if any criteria are being used in student assessments
                    var studentScoresUsingCriteria = await _context.StudentAssessmentScores
                        .Where(sas => criteriaIds.Contains(sas.RubricCriteriaId))
                        .ToListAsync();

                    if (studentScoresUsingCriteria.Any())
                    {
                        _logger.LogWarning("Cannot delete rubric {RubricId} - criteria in use by {StudentScoreCount} student assessments",
                            id, studentScoresUsingCriteria.Count);

                        TempData["ErrorMessage"] = $"Cannot delete rubric '{rubrics.RubricName}' because it contains criteria that are being used in student assessments. Please delete the related assessments first.";
                        return RedirectToAction("Index", "Rubrics", new { courseId, role });
                    }

                    // Delete in the correct order to maintain referential integrity
                    _logger.LogInformation("Starting cascade delete for rubric {RubricId}: {ScoreCount} scores, {CriteriaCount} criteria, {TaskCount} tasks",
                        id, rubricCriteriaScores.Count, rubricCriterias.Count, rubricTasks.Count);

                    // 1. Delete rubric criteria scores first
                    if (rubricCriteriaScores.Any())
                    {
                        _context.RubricCriteriaScore.RemoveRange(rubricCriteriaScores);
                        _logger.LogDebug("Marked {ScoreCount} scores for deletion", rubricCriteriaScores.Count);
                    }

                    // 2. Delete rubric criteria
                    if (rubricCriterias.Any())
                    {
                        _context.RubricCriteria.RemoveRange(rubricCriterias);
                        _logger.LogDebug("Marked {CriteriaCount} criteria for deletion", rubricCriterias.Count);
                    }

                    // 3. Delete rubric tasks
                    if (rubricTasks.Any())
                    {
                        _context.RubricTask.RemoveRange(rubricTasks);
                        _logger.LogDebug("Marked {TaskCount} tasks for deletion", rubricTasks.Count);
                    }

                    // 4. Finally delete the rubric itself
                    _context.Rubrics.Remove(rubrics);
                    _logger.LogDebug("Marked rubric {RubricId} for deletion", id);

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Successfully deleted rubric {RubricName} (ID: {RubricId}) and all related data",
                        rubrics.RubricName, id);

                    TempData["SuccessMessage"] = $"Rubric '{rubrics.RubricName}' and all its related data have been successfully deleted.";
                }
                else
                {
                    _logger.LogWarning("Attempted to delete non-existent rubric ID: {RubricId}", id);
                    TempData["ErrorMessage"] = "Rubric not found.";
                }

                return RedirectToAction("Index", "Rubrics", new { courseId, role });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting rubric ID: {RubricId}", id);
                throw;
            }
        }

        // GET: Rubrics/DeleteTask/5
        public async Task<IActionResult> DeleteTask(int? id, int? rubricId, string? courseid, string? role)
        {
            _logger.LogInformation("DeleteTask GET action called with taskId: {TaskId}, rubricId: {RubricId}", id, rubricId);

            if (id == null)
            {
                _logger.LogWarning("Task ID is null in DeleteTask action");
                return NotFound();
            }

            try
            {
                var rubricTask = await _context.RubricTask
                    .FirstOrDefaultAsync(m => m.RubricTaskId == id);
                if (rubricTask == null)
                {
                    _logger.LogWarning("Task not found for delete: ID {TaskId}", id);
                    return NotFound();
                }

                _logger.LogDebug("Loading delete confirmation for task: {TaskTitle} (ID: {TaskId})", rubricTask.TaskTitle, id);

                // Pass the rubricId to the view
                ViewBag.RubricId = rubricId;
                ViewBag.CourseId = courseid;
                ViewBag.CurrentUserRole = role;

                return View(rubricTask);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteTask GET action for task ID: {TaskId}", id);
                throw;
            }
        }

        // POST: Rubrics/DeleteTask/5
        [HttpPost, ActionName("DeleteTask")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTaskConfirmed(int id, int rubricId, string courseid, string role)
        {
            _logger.LogInformation("DeleteTask POST action called for task ID: {TaskId}", id);

            try
            {
                var rubricTask = await _context.RubricTask.FindAsync(id);
                if (rubricTask != null)
                {
                    _logger.LogDebug("Found task to delete: {TaskTitle} (ID: {TaskId})", rubricTask.TaskTitle, id);

                    // Get all criteria for this task
                    var rubricCriterias = await _context.RubricCriteria
                        .Where(rc => rc.RubricTaskId == id)
                        .ToListAsync();

                    _logger.LogDebug("Found {CriteriaCount} criteria to delete for task {TaskId}", rubricCriterias.Count, id);

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
                            _logger.LogWarning("Cannot delete task {TaskId} - criteria in use by {StudentScoreCount} student assessments",
                                id, studentScoresUsingCriteria.Count);

                            TempData["ErrorMessage"] = $"Cannot delete task '{rubricTask.TaskTitle}' because it contains criteria that are being used in student assessments. Please delete the related assessments first.";
                            return RedirectToAction("Details", "Rubrics", new { id = rubricId, courseid, role });
                        }

                        // Get all scores for these criteria
                        var rubricCriteriaScores = await _context.RubricCriteriaScore
                            .Where(rcs => criteriaIds.Contains(rcs.RubricCriteriaId))
                            .ToListAsync();

                        _logger.LogDebug("Found {ScoreCount} scores to delete for task {TaskId}", rubricCriteriaScores.Count, id);

                        // Delete in the correct order to maintain referential integrity
                        _logger.LogInformation("Starting cascade delete for task {TaskId}: {ScoreCount} scores, {CriteriaCount} criteria",
                            id, rubricCriteriaScores.Count, rubricCriterias.Count);

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

                    _logger.LogInformation("Successfully deleted task {TaskTitle} (ID: {TaskId}) and all related data",
                        rubricTask.TaskTitle, id);

                    TempData["SuccessMessage"] = $"Task '{rubricTask.TaskTitle}' and all its related criteria and scores have been successfully deleted.";
                }
                else
                {
                    _logger.LogWarning("Attempted to delete non-existent task ID: {TaskId}", id);
                    TempData["ErrorMessage"] = "Task not found.";
                }

                return RedirectToAction("Details", "Rubrics", new { id = rubricId, courseid, role });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task ID: {TaskId}", id);
                throw;
            }
        }

        // GET: Rubrics/DeleteCriteria/5
        public async Task<IActionResult> DeleteCriteria(int? criteriaId, int? rubricId, string? courseid = null, string? role = null)
        {
            _logger.LogInformation("DeleteCriteria GET action called with criteriaId: {CriteriaId}, rubricId: {RubricId}", criteriaId, rubricId);

            if (criteriaId == null)
            {
                _logger.LogWarning("Criteria ID is null in DeleteCriteria action");
                return NotFound();
            }

            try
            {
                var rubrics = await _context.RubricCriteria
                    .FirstOrDefaultAsync(m => m.RubricCriteriaId == criteriaId);
                if (rubrics == null)
                {
                    _logger.LogWarning("Criteria not found for delete: ID {CriteriaId}", criteriaId);
                    return NotFound();
                }

                _logger.LogDebug("Loading delete confirmation for criteria: {CriterionTitle} (ID: {CriteriaId})", rubrics.CriterionTitle, criteriaId);

                // Pass the rubricId to the view
                ViewBag.RubricId = rubricId;
                ViewBag.CourseId = courseid;
                ViewBag.CurrentUserRole = role;

                return View(rubrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteCriteria GET action for criteria ID: {CriteriaId}", criteriaId);
                throw;
            }
        }

        // POST: Rubrics/DeleteCriteria/5
        [HttpPost, ActionName("DeleteCriteria")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCriteriaConfirmed(int criteriaId, int rubricId, string courseid, string role)
        {
            _logger.LogInformation("DeleteCriteria POST action called for criteria ID: {CriteriaId}", criteriaId);

            try
            {
                var rubricCriteria = await _context.RubricCriteria.FindAsync(criteriaId);
                if (rubricCriteria != null)
                {
                    _logger.LogDebug("Found criteria to delete: {CriterionTitle} (ID: {CriteriaId})", rubricCriteria.CriterionTitle, criteriaId);

                    // Check if this criteria is being used in student assessments
                    var studentScoresUsingCriteria = await _context.StudentAssessmentScores
                        .Where(sas => sas.RubricCriteriaId == criteriaId)
                        .ToListAsync();

                    if (studentScoresUsingCriteria.Any())
                    {
                        _logger.LogWarning("Cannot delete criteria {CriteriaId} - in use by {StudentScoreCount} student assessments",
                            criteriaId, studentScoresUsingCriteria.Count);

                        TempData["ErrorMessage"] = $"Cannot delete criteria '{rubricCriteria.CriterionTitle}' because it is being used in student assessments. Please delete the related assessments first.";
                        return RedirectToAction("Details", "Rubrics", new { id = rubricId, courseid, role });
                    }

                    // Get all scores for this criteria
                    var rubricCriteriaScores = await _context.RubricCriteriaScore
                        .Where(rcs => rcs.RubricCriteriaId == criteriaId)
                        .ToListAsync();

                    _logger.LogDebug("Found {ScoreCount} scores to delete for criteria {CriteriaId}", rubricCriteriaScores.Count, criteriaId);

                    // Delete in the correct order to maintain referential integrity
                    _logger.LogInformation("Starting cascade delete for criteria {CriteriaId}: {ScoreCount} scores",
                        criteriaId, rubricCriteriaScores.Count);

                    // 1. Delete rubric criteria scores first
                    if (rubricCriteriaScores.Any())
                    {
                        _context.RubricCriteriaScore.RemoveRange(rubricCriteriaScores);
                        _logger.LogDebug("Marked {ScoreCount} scores for deletion", rubricCriteriaScores.Count);
                    }

                    // 2. Finally delete the criteria itself
                    _context.RubricCriteria.Remove(rubricCriteria);
                    _logger.LogDebug("Marked criteria {CriteriaId} for deletion", criteriaId);

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Successfully deleted criteria {CriterionTitle} (ID: {CriteriaId}) and all related scores",
                        rubricCriteria.CriterionTitle, criteriaId);

                    TempData["SuccessMessage"] = $"Criteria '{rubricCriteria.CriterionTitle}' and all its related scores have been successfully deleted.";
                }
                else
                {
                    _logger.LogWarning("Attempted to delete non-existent criteria ID: {CriteriaId}", criteriaId);
                    TempData["ErrorMessage"] = "Criteria not found.";
                }

                return RedirectToAction("Details", "Rubrics", new { id = rubricId, courseid, role });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting criteria ID: {CriteriaId}", criteriaId);
                throw;
            }
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
            _logger.LogInformation("CreateTask GET action called with rubricId: {RubricId}, courseId: {CourseId}, role: {Role}", id, courseid, role);

            try
            {
                if (id == null)
                {
                    _logger.LogWarning("Rubric ID is null in CreateTask action");
                    return BadRequest("Rubric ID is required");
                }

                _logger.LogDebug("Loading create task form for rubric {RubricId}", id);

                ViewBag.RubricId = id;
                ViewBag.CourseId = courseid;
                ViewBag.CurrentUserRole = role;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateTask GET action for rubric ID: {RubricId}", id);
                throw;
            }
        }

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> CreateTask(int id, string courseid, string role, [Bind("RubricTaskId,RubricsId,TaskTitle,TaskDescription,MaxMarks")] RubricTask rubricTask)
        {
            _logger.LogInformation("CreateTask POST action called for rubric ID: {RubricId}, task: {TaskTitle}", id, rubricTask?.TaskTitle);

            try
            {
                rubricTask.RubricsId = id;
                _context.Add(rubricTask);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully created task: {TaskTitle} (ID: {TaskId}) for rubric {RubricId}",
                    rubricTask.TaskTitle, rubricTask.RubricTaskId, id);

                return RedirectToAction("Details", "Rubrics", new { id, courseid, role });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task: {TaskTitle} for rubric ID: {RubricId}", rubricTask?.TaskTitle, id);
                throw;
            }
        }

        // GET: Rubrics/CreateTaskCriteria
        public async Task<IActionResult> CreateCriteria(int? id, int? rubricsId, string? courseid = null, string? role = null)
        {
            _logger.LogInformation("CreateCriteria GET action called with taskId: {TaskId}, rubricId: {RubricId}", id, rubricsId);

            try
            {
                if (id == null)
                {
                    _logger.LogWarning("Task ID is null in CreateCriteria action");
                    return BadRequest("Task ID is required");
                }

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

                    _logger.LogDebug("Task {TaskId} weight analysis: current total {CurrentWeight}%, remaining {RemainingWeight}%",
                        id, currentTotalWeight, remainingWeight);

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

                        _logger.LogDebug("Found existing criteria template: {CriterionTitle} with {ScoreCount} scores",
                            firstExistingCriteria.CriterionTitle, existingScores.Count);

                        ViewBag.FirstExistingCriteria = firstExistingCriteria;
                        ViewBag.ExistingScores = existingScores;
                        ViewBag.HasExistingCriteria = true;
                    }
                    else
                    {
                        _logger.LogDebug("No existing criteria found for task {TaskId}", id);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateCriteria GET action for task ID: {TaskId}", id);
                throw;
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCriteria(int id, int rubricsId, string courseid, string role, [Bind("RubricCriteriaId,RubricTaskId,CriterionTitle,Weight,MaxScore")] RubricCriteria rubricCriteria)
        {
            _logger.LogInformation("CreateCriteria POST action called for task ID: {TaskId}, criteria: {CriterionTitle}", id, rubricCriteria?.CriterionTitle);

            try
            {
                // VALIDATION: Check if total weight exceeds 100%
                var existingCriterias = await _context.RubricCriteria
                    .Where(rc => rc.RubricTaskId == id)
                    .ToListAsync();

                var currentTotalWeight = existingCriterias.Sum(rc => rc.Weight);
                var newTotalWeight = currentTotalWeight + rubricCriteria.Weight;

                _logger.LogDebug("Weight validation for new criteria: current total {CurrentWeight}%, proposed weight {ProposedWeight}%, new total would be {NewWeight}%",
                    currentTotalWeight, rubricCriteria.Weight, newTotalWeight);

                if (newTotalWeight > 100)
                {
                    _logger.LogWarning("Weight validation failed: new total {NewWeight}% would exceed 100%", newTotalWeight);

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

                _logger.LogInformation("Successfully created criteria: {CriterionTitle} (ID: {CriteriaId}) for task {TaskId}",
                    rubricCriteria.CriterionTitle, rubricCriteria.RubricCriteriaId, id);

                // Check if there's a first existing criteria in this rubric task
                var firstExistingCriteriaForScores = await _context.RubricCriteria
                    .Where(rc => rc.RubricTaskId == id && rc.RubricCriteriaId != rubricCriteria.RubricCriteriaId)
                    .OrderBy(rc => rc.RubricCriteriaId)
                    .FirstOrDefaultAsync();

                if (firstExistingCriteriaForScores != null)
                {
                    _logger.LogDebug("Using existing criteria template for scores: {CriterionTitle}", firstExistingCriteriaForScores.CriterionTitle);

                    // Get the scores from the first existing criteria
                    var existingScores = await _context.RubricCriteriaScore
                        .Where(rcs => rcs.RubricCriteriaId == firstExistingCriteriaForScores.RubricCriteriaId)
                        .OrderByDescending(rcs => rcs.CriterionScore)
                        .ToListAsync();

                    _logger.LogDebug("Found {ExistingScoreCount} existing scores to copy", existingScores.Count);

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
                        _logger.LogDebug("Creating additional scores from {StartScore} to {EndScore}", maxExistingScore + 1, rubricCriteria.MaxScore);

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
                    _logger.LogDebug("Successfully saved scores for new criteria {CriteriaId}", rubricCriteria.RubricCriteriaId);
                }
                else
                {
                    _logger.LogDebug("No existing criteria found, creating scores from form data for max score {MaxScore}", rubricCriteria.MaxScore);

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
                    _logger.LogDebug("Successfully created {ScoreCount} new scores for criteria {CriteriaId}",
                        rubricCriteria.MaxScore + 1, rubricCriteria.RubricCriteriaId);
                }

                _logger.LogInformation("Completed criteria creation process for {CriterionTitle} (ID: {CriteriaId})",
                    rubricCriteria.CriterionTitle, rubricCriteria.RubricCriteriaId);

                return RedirectToAction("Details", "Rubrics", new { id = rubricsId, courseid, role });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating criteria: {CriterionTitle} for task ID: {TaskId}", rubricCriteria?.CriterionTitle, id);
                throw;
            }
        }

        // GET: UploadRubrics
        public async Task<IActionResult> Upload(string courseid = null, string role = null)
        {
            _logger.LogInformation("Upload GET action called with courseId: {CourseId}, role: {Role}", courseid, role);

            try
            {
                if (!string.IsNullOrEmpty(courseid) && int.TryParse(courseid, out int parsedCourseId))
                {
                    var course = await _context.CourseRoles.FindAsync(parsedCourseId);
                    if (course != null)
                    {
                        _logger.LogDebug("Loading upload form for course: {CourseCode} - {CourseName}", course.CourseCode, course.CourseName);

                        ViewBag.CourseId = courseid;
                        ViewBag.CurrentUserRole = role;
                    }
                }
                else
                {
                    ViewBag.CourseId = courseid;
                    ViewBag.CurrentUserRole = role;
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Upload GET action for courseId: {CourseId}", courseid);
                throw;
            }
        }

        // POST: UploadRubrics
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(string courseId, string role, IFormFile rubricsFile)
        {
            _logger.LogInformation("Upload POST action called for courseId: {CourseId}, file: {FileName}, size: {FileSize} bytes",
                courseId, rubricsFile?.FileName, rubricsFile?.Length);

            try
            {
                if (rubricsFile == null || rubricsFile.Length == 0)
                {
                    _logger.LogWarning("Upload attempted with null or empty file");
                    ModelState.AddModelError("rubricsFile", "Please upload your Rubrics File.");
                    return RedirectToAction("Upload", "Rubrics");
                }

                // Check file extension
                var extension = Path.GetExtension(rubricsFile.FileName).ToLower();
                if (extension != ".doc" && extension != ".docx")
                {
                    _logger.LogWarning("Upload attempted with invalid file extension: {Extension}", extension);
                    ModelState.AddModelError("rubricsFile", "Only Word documents are allowed.");
                    return RedirectToAction("Upload", "Rubrics");
                }

                // Check file size (limit to 10MB)
                if (rubricsFile.Length > 10 * 1024 * 1024)
                {
                    _logger.LogWarning("Upload attempted with oversized file: {FileSize} bytes", rubricsFile.Length);
                    ModelState.AddModelError("rubricsFile", "File size must be less than 10MB.");
                    return RedirectToAction("Upload", "Rubrics");
                }

                _logger.LogDebug("File validation passed, processing document: {FileName}", rubricsFile.FileName);

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
                        _logger.LogDebug("Successfully opened DOCX document with {ParagraphCount} paragraphs and {TableCount} tables",
                            docx.Paragraphs.Count, docx.Tables.Count);

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

                        _logger.LogDebug("Extracted {ParagraphCount} paragraphs from document", rubricsParagraphs.Count);

                        // Extract rubric criteria from tables
                        int tableIndex = 0;
                        int taskIndex = 0;
                        bool isTableRead = false;

                        foreach (var table in docx.Tables)
                        {
                            isTableRead = false;
                            _logger.LogDebug("Processing table {TableIndex} with {RowCount} rows", tableIndex, table.Rows.Count);

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
                                                _logger.LogDebug("Added task: {TaskTitle} with {MaxMarks} marks", task.TaskTitle, task.MaxMarks);
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

                                        _logger.LogDebug("Extracted {ScoreHeaderCount} score headers for table {TableIndex}", maxScore + 1, tableIndex);

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

                                            _logger.LogDebug("Added criteria: {CriterionTitle} with weight {Weight}% and max score {MaxScore}",
                                                rubricCriteria.CriterionTitle, rubricCriteria.Weight, rubricCriteria.MaxScore);

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
                                if (course == null)
                                {
                                    _logger.LogWarning("Course not found for ID: {CourseId} during upload", courseId);
                                    ModelState.AddModelError("", "Course not found");
                                    ViewBag.CourseId = courseId;
                                    ViewBag.CurrentUserRole = role;
                                    return View();
                                }

                                rubric.Institution = course.Institution;
                                rubric.Programme = rubricsParagraphs[0];
                                rubric.CourseCode = firstSpaceIndex > 0 ? fullText.Substring(0, firstSpaceIndex) : fullText;
                                rubric.CourseName = firstSpaceIndex > 0 ? fullText.Substring(firstSpaceIndex + 1).Trim() : "";
                                rubric.RubricName = rubricsParagraphs.Count > 2 ? rubricsParagraphs[2] : "";
                                rubric.Year = course.Year;
                                rubric.Trimester = course.Trimester;
                                rubric.TotalMarks = rubricTasks.Sum(t => t.MaxMarks);
                                rubric.SourceFile = $"{rubricsFile.FileName} (Size: {rubricsFile.Length} bytes, Uploaded: {DateTime.Now:yyyy-MM-dd HH:mm:ss})";

                                _logger.LogDebug("Parsed rubric data - Name: {RubricName}, Course: {CourseCode}, Year: {Year}, Trimester: {Trimester}, Total Marks: {TotalMarks}",
                                    rubric.RubricName, rubric.CourseCode, rubric.Year, rubric.Trimester, rubric.TotalMarks);

                                // VALIDATION 1: Check if course information matches current course context
                                if (!string.IsNullOrEmpty(courseId))
                                {
                                    if (course != null)
                                    {
                                        bool hasValidationError = false;

                                        if (!string.Equals(rubric.Programme, course.Programme, StringComparison.OrdinalIgnoreCase))
                                        {
                                            _logger.LogWarning("Programme mismatch: Document has '{DocumentProgramme}' but expected '{CourseProgramme}'",
                                                rubric.Programme, course.Programme);
                                            ModelState.AddModelError("", $"Programme mismatch: Document has '{rubric.Programme}' but expected '{course.Programme}'");
                                            hasValidationError = true;
                                        }

                                        if (!string.Equals(rubric.CourseCode, course.CourseCode, StringComparison.OrdinalIgnoreCase))
                                        {
                                            _logger.LogWarning("Course Code mismatch: Document has '{DocumentCourseCode}' but expected '{CourseCourseCode}'",
                                                rubric.CourseCode, course.CourseCode);
                                            ModelState.AddModelError("", $"Course Code mismatch: Document has '{rubric.CourseCode}' but expected '{course.CourseCode}'");
                                            hasValidationError = true;
                                        }

                                        if (!string.Equals(rubric.CourseName, course.CourseName, StringComparison.OrdinalIgnoreCase))
                                        {
                                            _logger.LogWarning("Course Name mismatch: Document has '{DocumentCourseName}' but expected '{CourseCourseName}'",
                                                rubric.CourseName, course.CourseName);
                                            ModelState.AddModelError("", $"Course Name mismatch: Document has '{rubric.CourseName}' but expected '{course.CourseName}'");
                                            hasValidationError = true;
                                        }

                                        if (rubric.Year != course.Year)
                                        {
                                            _logger.LogWarning("Year mismatch: Document has '{DocumentYear}' but expected '{CourseYear}'",
                                                rubric.Year, course.Year);
                                            ModelState.AddModelError("", $"Year mismatch: Document has '{rubric.Year}' but expected '{course.Year}'");
                                            hasValidationError = true;
                                        }

                                        if (rubric.Trimester != course.Trimester)
                                        {
                                            _logger.LogWarning("Trimester mismatch: Document has '{DocumentTrimester}' but expected '{CourseTrimester}'",
                                                rubric.Trimester, course.Trimester);
                                            ModelState.AddModelError("", $"Trimester mismatch: Document has '{rubric.Trimester}' but expected '{course.Trimester}'");
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
                                                             r.Year == rubric.Year &&
                                                             r.Trimester == rubric.Trimester &&
                                                             r.RubricName == rubric.RubricName);

                                if (existingRubric != null)
                                {
                                    _logger.LogWarning("Duplicate rubric upload attempted: {RubricName} for course {CourseCode} in year {Year}, trimester {Trimester}",
                                        rubric.RubricName, rubric.CourseCode, rubric.Year, rubric.Trimester);

                                    ModelState.AddModelError("", $"A rubric with the name '{rubric.RubricName}' already exists for course '{rubric.CourseCode}' in year '{rubric.Year}', trimester '{rubric.Trimester}'");

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

                        _logger.LogInformation("Document processing completed. Extracted: {TaskCount} tasks, {CriteriaCount} criteria, {ScoreCount} scores",
                            rubricTasks.Count, rubricCriterias.Count, rubricCriteriaScores.Count);

                        // Save rubric first to get the RubricsId
                        _context.Add(rubric);
                        await _context.SaveChangesAsync();
                        _logger.LogDebug("Saved rubric with ID: {RubricId}", rubric.RubricsId);

                        // Save rubric tasks with the rubric ID
                        foreach (var task in rubricTasks)
                        {
                            task.RubricsId = rubric.RubricsId;
                            _context.RubricTask.Add(task);
                        }
                        await _context.SaveChangesAsync();
                        _logger.LogDebug("Saved {TaskCount} tasks", rubricTasks.Count);

                        // Save rubric criterias with the correct RubricTaskId
                        foreach (var criteria in rubricCriterias)
                        {
                            var correspondingTask = rubricTasks[criteria.RubricTaskId];
                            criteria.RubricTaskId = correspondingTask.RubricTaskId;
                            _context.RubricCriteria.Add(criteria);
                        }
                        await _context.SaveChangesAsync();
                        _logger.LogDebug("Saved {CriteriaCount} criteria", rubricCriterias.Count);

                        // Save rubric criteria scores with the correct RubricCriteriaId
                        foreach (var score in rubricCriteriaScores)
                        {
                            var correspondingCriteria = rubricCriterias[score.RubricCriteriaId];
                            score.RubricCriteriaId = correspondingCriteria.RubricCriteriaId;
                            _context.RubricCriteriaScore.Add(score);
                        }
                        await _context.SaveChangesAsync();
                        _logger.LogDebug("Saved {ScoreCount} scores", rubricCriteriaScores.Count);

                        _logger.LogInformation("Successfully uploaded and processed rubric: {RubricName} (ID: {RubricId}) with {TaskCount} tasks",
                            rubric.RubricName, rubric.RubricsId, rubricTasks.Count);

                        TempData["SuccessMessage"] = $"Your rubrics has been submitted successfully! {rubricTasks.Count} tasks extracted.";
                    }
                }
                return RedirectToAction("Details", "Rubrics", new { id = rubric.RubricsId, courseId, role });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during rubric upload for courseId: {CourseId}, filename: {FileName}",
                    courseId, rubricsFile?.FileName);

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

        // GET: Rubrics/Management
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Management(string sortOrder, int? year, int? trimester, string searchTerm)
        {
            _logger.LogInformation("Management action called with filters - Year: {Year}, Trimester: {Trimester}, SearchTerm: {SearchTerm}, SortOrder: {SortOrder}",
                year, trimester, searchTerm, sortOrder);

            try
            {
                // Set up ViewData for sorting links
                ViewData["CurrentSort"] = sortOrder;
                ViewData["RubricNameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "rubricName_desc" : "";
                ViewData["CourseCodeSortParm"] = sortOrder == "courseCode" ? "courseCode_desc" : "courseCode";
                ViewData["CourseNameSortParm"] = sortOrder == "courseName" ? "courseName_desc" : "courseName";
                ViewData["YearSortParm"] = sortOrder == "year" ? "year_desc" : "year";
                ViewData["TrimesterSortParm"] = sortOrder == "trimester" ? "trimester_desc" : "trimester";
                ViewData["ProgrammeSortParm"] = sortOrder == "programme" ? "programme_desc" : "programme";
                ViewData["TotalMarksSortParm"] = sortOrder == "totalMarks" ? "totalMarks_desc" : "totalMarks";

                // Set up ViewData for current filter values
                ViewData["CurrentYearFilter"] = year;
                ViewData["CurrentTrimesterFilter"] = trimester;
                ViewData["CurrentSearchTerm"] = searchTerm;

                // Get distinct years and trimesters for dropdowns
                ViewBag.Years = new SelectList(await _context.Rubrics
                    .Select(r => r.Year)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .ToListAsync());

                ViewBag.Trimesters = new SelectList(await _context.Rubrics
                    .Select(r => r.Trimester)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToListAsync());

                // Preserve current filter values for dropdowns
                ViewBag.CurrentYear = year;
                ViewBag.CurrentTrimester = trimester;

                // Start with all rubrics (NO course code filtering)
                var rubricsQuery = _context.Rubrics.AsQueryable();

                // Apply filters
                if (year.HasValue)
                {
                    rubricsQuery = rubricsQuery.Where(r => r.Year == year.Value);
                    _logger.LogDebug("Applied year filter: {Year}", year);
                }

                if (trimester.HasValue)
                {
                    rubricsQuery = rubricsQuery.Where(r => r.Trimester == trimester.Value);
                    _logger.LogDebug("Applied trimester filter: {Trimester}", trimester);
                }

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    rubricsQuery = rubricsQuery.Where(r => 
                        r.RubricName.Contains(searchTerm) || 
                        r.CourseCode.Contains(searchTerm) || 
                        r.CourseName.Contains(searchTerm) ||
                        r.Programme.Contains(searchTerm));
                    _logger.LogDebug("Applied search filter: {SearchTerm}", searchTerm);
                }

                // Apply sorting
                switch (sortOrder)
                {
                    case "rubricName_desc":
                        rubricsQuery = rubricsQuery.OrderByDescending(r => r.RubricName);
                        break;
                    case "courseCode":
                        rubricsQuery = rubricsQuery.OrderBy(r => r.CourseCode);
                        break;
                    case "courseCode_desc":
                        rubricsQuery = rubricsQuery.OrderByDescending(r => r.CourseCode);
                        break;
                    case "courseName":
                        rubricsQuery = rubricsQuery.OrderBy(r => r.CourseName);
                        break;
                    case "courseName_desc":
                        rubricsQuery = rubricsQuery.OrderByDescending(r => r.CourseName);
                        break;
                    case "year":
                        rubricsQuery = rubricsQuery.OrderBy(r => r.Year);
                        break;
                    case "year_desc":
                        rubricsQuery = rubricsQuery.OrderByDescending(r => r.Year);
                        break;
                    case "trimester":
                        rubricsQuery = rubricsQuery.OrderBy(r => r.Trimester);
                        break;
                    case "trimester_desc":
                        rubricsQuery = rubricsQuery.OrderByDescending(r => r.Trimester);
                        break;
                    case "programme":
                        rubricsQuery = rubricsQuery.OrderBy(r => r.Programme);
                        break;
                    case "programme_desc":
                        rubricsQuery = rubricsQuery.OrderByDescending(r => r.Programme);
                        break;
                    case "totalMarks":
                        rubricsQuery = rubricsQuery.OrderBy(r => r.TotalMarks);
                        break;
                    case "totalMarks_desc":
                        rubricsQuery = rubricsQuery.OrderByDescending(r => r.TotalMarks);
                        break;
                    default:
                        rubricsQuery = rubricsQuery.OrderBy(r => r.RubricName);
                        break;
                }

                var rubrics = await rubricsQuery.ToListAsync();

                _logger.LogInformation("Successfully retrieved {RubricCount} rubrics for management (filtered: Year={HasYearFilter}, Trimester={HasTrimesterFilter}, Search={HasSearchFilter})",
                    rubrics.Count, year.HasValue, trimester.HasValue, !string.IsNullOrEmpty(searchTerm));

                return View(rubrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Management action with filters - Year: {Year}, Trimester: {Trimester}, SearchTerm: {SearchTerm}",
                    year, trimester, searchTerm);
                throw;
            }
        }

        // GET: Rubrics/AssessmentManagement
        [Authorize]
        public async Task<IActionResult> AssessmentManagement(int? courseId, string role = null)
        {
            _logger.LogInformation("AssessmentManagement GET action called with courseId: {CourseId}, role: {Role}", courseId, role);

            try
            {
                if (!courseId.HasValue)
                {
                    _logger.LogWarning("CourseId is null in AssessmentManagement");
                    return BadRequest("Course ID is required");
                }

                var course = await _context.CourseRoles.FindAsync(courseId.Value);
                if (course == null)
                {
                    _logger.LogWarning("Course not found for ID: {CourseId}", courseId);
                    return NotFound("Course not found");
                }

                // Get current user
                var currentUser = await _userManager.GetUserAsync(User);
                var userId = currentUser?.UserName;

                // Verify access
                if (!string.IsNullOrEmpty(role))
                {
                    var hasAccess = await _context.CourseRoles
                        .AnyAsync(cr => cr.CourseRolesId == courseId.Value &&
                                       ((role == "Lecturer" && cr.RoleLecturer == userId) ||
                                        (role == "Moderator" && cr.RoleModerator == userId) ||
                                        (role == "Admin")));

                    if (!hasAccess && role != "Admin")
                    {
                        _logger.LogWarning("Access denied for user {UserId} with role {Role} for course {CourseCode}", userId, role, course.CourseCode);
                        TempData["ErrorMessage"] = "You don't have permission to manage assessments for this course.";
                        return RedirectToAction("Index", "Home");
                    }
                }

                var viewModel = new smart_feedback.Models.ViewModels.AssessmentRubricManagementViewModel
                {
                    CourseRolesId = course.CourseRolesId,
                    CourseCode = course.CourseCode,
                    CourseName = course.CourseName,
                    Year = course.Year,
                    Trimester = course.Trimester,
                    Programme = course.Programme,
                    TotalAssessment = course.TotalAssessment,
                    UserRole = role
                };

                // Get existing assessments for this course
                var existingAssessments = await _context.Assessments
                    .Where(a => a.CourseCode == course.CourseCode && 
                               a.Year == course.Year && 
                               a.Trimester == course.Trimester)
                    .ToListAsync();

                _logger.LogDebug("Found {AssessmentCount} existing assessments for course {CourseCode}", existingAssessments.Count, course.CourseCode);

                // Create assessment rows based on TotalAssessment
                for (int i = 0; i < course.TotalAssessment; i++)
                {
                    var existingAssessment = existingAssessments.ElementAtOrDefault(i);
                    viewModel.AssessmentRows.Add(new smart_feedback.Models.ViewModels.AssessmentRubricRow
                    {
                        Index = i + 1,
                        AssessmentName = existingAssessment?.AssessmentName ?? $"Assessment {i + 1}",
                        RubricId = existingAssessment?.RubricsId,
                        ProportionalMarks = 0 // Will need to add this field to Assessment model
                    });
                }

                // Get available rubrics filtered by Programme and Course Code
                viewModel.AvailableRubrics = await _context.Rubrics
                    .Where(r => r.Programme == course.Programme && 
                               r.CourseCode == course.CourseCode &&
                               r.Year == course.Year &&
                               r.Trimester == course.Trimester)
                    .OrderBy(r => r.RubricName)
                    .ToListAsync();

                // Get all rubrics for optional selection
                viewModel.AllRubrics = await _context.Rubrics
                    .Where(r => r.Year == course.Year && r.Trimester == course.Trimester)
                    .OrderBy(r => r.Programme)
                    .ThenBy(r => r.CourseCode)
                    .ThenBy(r => r.RubricName)
                    .ToListAsync();

                _logger.LogInformation("Successfully loaded assessment management for course {CourseCode} with {AssessmentCount} assessments",
                    course.CourseCode, course.TotalAssessment);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AssessmentManagement GET action for courseId: {CourseId}", courseId);
                throw;
            }
        }

        // POST: Rubrics/AssessmentManagement
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AssessmentManagement(smart_feedback.Models.ViewModels.AssessmentRubricManagementViewModel model)
        {
            _logger.LogInformation("AssessmentManagement POST action called for course ID: {CourseId}", model.CourseRolesId);

            try
            {
                // Validate total proportional marks = 100
                if (model.TotalProportionalMarks != 100)
                {
                    _logger.LogWarning("Total proportional marks validation failed: {Total}% (expected 100%)", model.TotalProportionalMarks);
                    ModelState.AddModelError("", $"Total proportional marks must equal 100%. Current total: {model.TotalProportionalMarks}%");
                    
                    // Reload data for view
                    var courseData = await _context.CourseRoles.FindAsync(model.CourseRolesId);
                    model.AvailableRubrics = await _context.Rubrics
                        .Where(r => r.Programme == courseData.Programme && 
                                   r.CourseCode == courseData.CourseCode &&
                                   r.Year == courseData.Year &&
                                   r.Trimester == courseData.Trimester)
                        .OrderBy(r => r.RubricName)
                        .ToListAsync();

                    model.AllRubrics = await _context.Rubrics
                        .Where(r => r.Year == courseData.Year && r.Trimester == courseData.Trimester)
                        .OrderBy(r => r.Programme)
                        .ThenBy(r => r.CourseCode)
                        .ThenBy(r => r.RubricName)
                        .ToListAsync();
                    
                    return View(model);
                }

                var course = await _context.CourseRoles.FindAsync(model.CourseRolesId);
                if (course == null)
                {
                    return NotFound();
                }

                // Get current user
                var currentUser = await _userManager.GetUserAsync(User);

                // Get existing assessments
                var existingAssessments = await _context.Assessments
                    .Where(a => a.CourseCode == course.CourseCode && 
                               a.Year == course.Year && 
                               a.Trimester == course.Trimester)
                    .ToListAsync();

                // Update or create assessments
                for (int i = 0; i < model.AssessmentRows.Count; i++)
                {
                    var row = model.AssessmentRows[i];
                    var existingAssessment = existingAssessments.ElementAtOrDefault(i);

                    if (existingAssessment != null)
                    {
                        // Update existing assessment
                        existingAssessment.AssessmentName = row.AssessmentName;
                        existingAssessment.RubricsId = row.RubricId ?? 0;
                        // TODO: Update ProportionalMarks when field is added to Assessment model
                        _context.Update(existingAssessment);
                        _logger.LogDebug("Updated assessment: {AssessmentName}", row.AssessmentName);
                    }
                    else if (row.RubricId.HasValue)
                    {
                        // Create new assessment
                        var newAssessment = new Assessment
                        {
                            AssessmentName = row.AssessmentName,
                            CourseCode = course.CourseCode,
                            Year = course.Year,
                            Trimester = course.Trimester,
                            RubricsId = row.RubricId.Value,
                            CreatedDate = DateTime.Now,
                            CreatedBy = currentUser?.UserName ?? "System",
                            Status = "Marking"
                            // TODO: Add ProportionalMarks when field is added to Assessment model
                        };
                        _context.Add(newAssessment);
                        _logger.LogDebug("Created new assessment: {AssessmentName}", row.AssessmentName);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully saved assessment rubric assignments for course {CourseCode}", course.CourseCode);
                TempData["SuccessMessage"] = "Assessment rubric assignments saved successfully!";

                return RedirectToAction("AssessmentManagement", new { courseId = model.CourseRolesId, role = model.UserRole });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AssessmentManagement POST action for course ID: {CourseId}", model.CourseRolesId);
                ModelState.AddModelError("", "An error occurred while saving the assessment assignments.");
                return View(model);
            }
        }
    }
}
