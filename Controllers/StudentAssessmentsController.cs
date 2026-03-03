using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using smart_feedback.Data;
using smart_feedback.Models;
using smart_feedback.Models.ViewModels;
using smart_feedback.Services;
using System.Text;

namespace smart_feedback.Controllers
{
    [Authorize]
    public class StudentAssessmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFeedbackGenerationService _feedbackService;
        private readonly IPuterAIService _puterAIService; // ADD THIS
        private readonly ILogger<StudentAssessmentsController> _logger; 
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _environment;

        public StudentAssessmentsController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager, 
            IFeedbackGenerationService feedbackService, 
            IPuterAIService puterAIService, // ADD THIS
            ILogger<StudentAssessmentsController> logger, 
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _feedbackService = feedbackService;
            _puterAIService = puterAIService; // ADD THIS
            _logger = logger;
            _environment = environment;
        }

        // GET: StudentAssessments
        public async Task<IActionResult> Index(string courseId, string role)
        {
            if (string.IsNullOrEmpty(courseId) || string.IsNullOrEmpty(role))
            {
                TempData["ErrorMessage"] = "Invalid course or role specified.";
                return RedirectToAction("Index", "Home");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var userId = currentUser?.UserName;

            var course = await _context.CourseRoles.FindAsync(int.Parse(courseId));
            if (course == null)
            {
                TempData["ErrorMessage"] = "Course not found.";
                return RedirectToAction("Index", "Home");
            }

            // Verify user has access
            if (!User.IsInRole("Admin"))
            {
                var hasAccess = await _context.CourseRoles
                    .AnyAsync(cr => cr.CourseRolesId == int.Parse(courseId) &&
                                   ((role == "Lecturer" && cr.RoleLecturer == userId) ||
                                    (role == "Moderator" && cr.RoleModerator == userId)));

                if (!hasAccess)
                {
                    TempData["ErrorMessage"] = "You don't have permission to access assessments for this course.";
                    return RedirectToAction("Index", "Home");
                }
            }

            var assessments = await _context.Assessments
                .Include(a => a.Rubric)
                .Where(a => a.CourseCode == course.CourseCode && a.Year == course.Year && a.Trimester == course.Trimester)
                .ToListAsync();

            // *** CHANGED: Fetch students from CourseStudent table for this specific course ***
            var students = await _context.CourseStudent
                .Where(cs => cs.CourseRolesId == int.Parse(courseId))
                .Include(cs => cs.Student)
                .Select(cs => cs.Student)
                .OrderBy(s => s.StudentId)
                .ToListAsync();

            var availableRubrics = await _context.Rubrics
                .Where(r => r.CourseCode == course.CourseCode && r.Year == course.Year && r.Trimester == course.Trimester)
                .ToListAsync();

            // Get marking status for each assessment
            var assessmentMarkedStudents = new Dictionary<int, List<int>>();
            var assessmentMarkingProgress = new Dictionary<int, int>();

            foreach (var assessment in assessments)
            {
                // Get list of students who have been marked for this assessment
                var markedStudentIds = await _context.StudentAssessmentScores
                    .Where(sas => sas.AssessmentId == assessment.AssessmentId)
                    .Select(sas => sas.StudentId)
                    .Distinct()
                    .ToListAsync();

                assessmentMarkedStudents[assessment.AssessmentId] = markedStudentIds;

                // Calculate progress percentage
                var progressPercentage = students.Count > 0
                    ? (int)((double)markedStudentIds.Count / students.Count * 100)
                    : 0;
                assessmentMarkingProgress[assessment.AssessmentId] = progressPercentage;
            }

            // Get FullName for Lecturer and Moderator
            var lecturerFullName = !string.IsNullOrEmpty(course.RoleLecturer)
                ? (await _userManager.FindByNameAsync(course.RoleLecturer))?.FullName
                : null;

            var moderatorFullName = !string.IsNullOrEmpty(course.RoleModerator)
                ? (await _userManager.FindByNameAsync(course.RoleModerator))?.FullName
                : null;

            // Add to ViewBag
            ViewBag.LecturerFullName = lecturerFullName ?? "Not Assigned";
            ViewBag.ModeratorFullName = moderatorFullName ?? "Not Assigned";

            var viewModel = new StudentAssessmentViewModel
            {
                CourseRolesId = course.CourseRolesId,
                CourseCode = course.CourseCode,
                CourseName = course.CourseName,
                Year = course.Year,
                Trimester = course.Trimester,   
                Role = role,
                Assessments = assessments,
                Students = students,
                AvailableRubrics = availableRubrics,
                AssessmentMarkedStudents = assessmentMarkedStudents,
                AssessmentMarkingProgress = assessmentMarkingProgress
            };

            return View(viewModel);
        }

        // GET: StudentAssessments/Create
        public async Task<IActionResult> Create(string courseId, string role)
        {
            var course = await _context.CourseRoles.FindAsync(int.Parse(courseId));
            if (course == null)
            {
                TempData["ErrorMessage"] = "Course not found.";
                return RedirectToAction("Index", new { courseId, role });
            }

            // Get all rubrics for this course
            var allRubrics = await _context.Rubrics
                .Where(r => r.CourseCode == course.CourseCode && r.Year == course.Year && r.Trimester == course.Trimester)
                .ToListAsync();

            // Get rubrics that already have assessments
            var rubricsWithAssessments = await _context.Assessments
                .Where(a => a.CourseCode == course.CourseCode && a.Year == course.Year && a.Trimester == course.Trimester)
                .Select(a => a.RubricsId)
                .ToListAsync();

            // Filter to only rubrics without assessments
            var availableRubrics = allRubrics
                .Where(r => !rubricsWithAssessments.Contains(r.RubricsId))
                .ToList();

            ViewBag.AvailableRubrics = availableRubrics;
            ViewBag.CourseId = courseId;
            ViewBag.Role = role;
            ViewBag.CourseCode = course.CourseCode;
            ViewBag.Year = course.Year;
            ViewBag.Trimester = course.Trimester;

            return View();
        }

        // POST: StudentAssessments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Assessment assessment, string courseId, string role)
        {
            var course = await _context.CourseRoles.FindAsync(int.Parse(courseId));

            // Validate that the rubric doesn't already have an assessment
            var existingAssessment = await _context.Assessments
                .FirstOrDefaultAsync(a => a.RubricsId == assessment.RubricsId && 
                                         a.CourseCode == assessment.CourseCode && 
                                         a.Year == assessment.Year && 
                                         a.Trimester == assessment.Trimester);
            var availableRubrics = new List<Rubrics>();

            if (existingAssessment != null)
            {
                ModelState.AddModelError("RubricsId", "An assessment already exists for this rubric.");
                
                // Reload available rubrics (only those without assessments)
                var allRubrics = await _context.Rubrics
                    .Where(r => r.CourseCode == course.CourseCode && r.Year == course.Year && r.Trimester == course.Trimester)
                    .ToListAsync();

                var rubricsWithAssessments = await _context.Assessments
                    .Where(a => a.CourseCode == course.CourseCode && a.Year == course.Year && a.Trimester == course.Trimester)
                    .Select(a => a.RubricsId)
                    .ToListAsync();

                availableRubrics = allRubrics
                    .Where(r => !rubricsWithAssessments.Contains(r.RubricsId))
                    .ToList();

                ViewBag.AvailableRubrics = availableRubrics;
                ViewBag.CourseId = courseId;
                ViewBag.Role = role;
                ViewBag.CourseCode = course?.CourseCode;
                ViewBag.Year = course?.Year;
                ViewBag.Trimester = course?.Trimester;  

                return View(assessment);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            assessment.CreatedBy = currentUser?.UserName;
            assessment.CreatedDate = DateTime.Now;
            assessment.StatusChangedBy = currentUser?.UserName;
            assessment.StatusChangedDate = DateTime.Now;
            assessment.Rubric = null; // To avoid EF Core tracking issues

            _context.Add(assessment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Assessment created successfully.";
            return RedirectToAction("Index", new { courseId, role });

            ViewBag.AvailableRubrics = availableRubrics;
            ViewBag.CourseId = courseId;
            ViewBag.Role = role;
            ViewBag.CourseCode = course?.CourseCode;
            ViewBag.Year = course?.Year;
            ViewBag.Trimester = course?.Trimester;

            return View(assessment);
        }

        // GET: StudentAssessments/Delete/5
        public async Task<IActionResult> Delete(int? id, string courseId, string role)
        {
            _logger.LogInformation("Delete GET action called for assessment ID: {AssessmentId}", id);

            if (id == null)
            {
                _logger.LogWarning("Assessment ID is null in Delete action");
                return NotFound();
            }

            try
            {
                var assessment = await _context.Assessments
                    .Include(a => a.Rubric)
                    .FirstOrDefaultAsync(a => a.AssessmentId == id);

                if (assessment == null)
                {
                    _logger.LogWarning("Assessment not found for ID: {AssessmentId}", id);
                    return NotFound();
                }

                // Check if assessment has student scores
                var hasScores = await _context.StudentAssessmentScores
                    .AnyAsync(sas => sas.AssessmentId == id);

                ViewBag.HasScores = hasScores;
                ViewBag.CourseId = courseId;
                ViewBag.Role = role;

                _logger.LogDebug("Loading delete confirmation for assessment: {AssessmentName} (ID: {AssessmentId})",
                    assessment.AssessmentName, id);

                return View(assessment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Delete GET action for assessment ID: {AssessmentId}", id);
                throw;
            }
        }

        // POST: StudentAssessments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, string courseId, string role)
        {
            _logger.LogInformation("Delete POST action called for assessment ID: {AssessmentId}", id);

            try
            {
                var assessment = await _context.Assessments.FindAsync(id);
                if (assessment != null)
                {
                    _logger.LogDebug("Found assessment to delete: {AssessmentName} (ID: {AssessmentId})",
                        assessment.AssessmentName, id);

                    // Get all student scores for this assessment
                    var studentScores = await _context.StudentAssessmentScores
                        .Where(sas => sas.AssessmentId == id)
                        .ToListAsync();

                    _logger.LogDebug("Found {ScoreCount} student scores to delete for assessment {AssessmentId}",
                        studentScores.Count, id);

                    if (studentScores.Any())
                    {
                        // Delete all student scores first
                        _context.StudentAssessmentScores.RemoveRange(studentScores);
                        _logger.LogDebug("Marked {ScoreCount} student scores for deletion", studentScores.Count);
                    }

                    // Delete the assessment
                    _context.Assessments.Remove(assessment);
                    _logger.LogDebug("Marked assessment {AssessmentId} for deletion", id);

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Successfully deleted assessment {AssessmentName} (ID: {AssessmentId}) and all related data",
                        assessment.AssessmentName, id);

                    TempData["SuccessMessage"] = $"Assessment '{assessment.AssessmentName}' and all its student scores have been successfully deleted.";
                }
                else
                {
                    _logger.LogWarning("Attempted to delete non-existent assessment ID: {AssessmentId}", id);
                    TempData["ErrorMessage"] = "Assessment not found.";
                }

                return RedirectToAction("Index", new { courseId, role });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting assessment ID: {AssessmentId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the assessment. Please try again.";
                return RedirectToAction("Index", new { courseId, role });
            }
        }

        // GET: StudentAssessments/Mark/5
        public async Task<IActionResult> Mark(int id, string courseId, string role, int studentIndex = 0)
        {
            var assessment = await _context.Assessments
                .Include(a => a.Rubric)
                .FirstOrDefaultAsync(a => a.AssessmentId == id);

            if (assessment == null)
            {
                TempData["ErrorMessage"] = "Assessment not found.";
                return RedirectToAction("Index", new { courseId, role });
            }

            // *** CHANGED: Fetch students from CourseStudent table for this specific course ***
            var allStudents = await _context.CourseStudent
                .Where(cs => cs.CourseRolesId == int.Parse(courseId))
                .Include(cs => cs.Student)
                .Select(cs => cs.Student)
                .OrderBy(s => s.StudentId)
                .ToListAsync();

            // Handle pagination
            if (studentIndex < 0 || studentIndex >= allStudents.Count)
                studentIndex = 0;

            var currentStudent = allStudents.Skip(studentIndex).FirstOrDefault();
            if (currentStudent == null)
            {
                TempData["ErrorMessage"] = "No students enrolled in this course.";
                return RedirectToAction("Index", new { courseId, role });
            }

            var rubricTasks = await _context.RubricTask
                .Where(rt => rt.RubricsId == assessment.RubricsId)
                .ToListAsync();

            var rubricCriterias = await _context.RubricCriteria
                .Where(rc => rubricTasks.Select(rt => rt.RubricTaskId).Contains(rc.RubricTaskId))
                .ToListAsync();

            var criteriaScores = await _context.RubricCriteriaScore
                .Where(rcs => rubricCriterias.Select(rc => rc.RubricCriteriaId).Contains(rcs.RubricCriteriaId))
                .ToListAsync();

            var existingScores = await _context.StudentAssessmentScores
                .Where(sas => sas.AssessmentId == id && sas.StudentId == currentStudent.Id)
                .ToListAsync();

            // Organize existing scores for current student only
            var studentScores = new Dictionary<int, Dictionary<int, StudentAssessmentScore>>
            {
                [currentStudent.Id] = new Dictionary<int, StudentAssessmentScore>()
            };

            foreach (var criteria in rubricCriterias)
            {
                // *** FIXED: Use FirstOrDefault instead of FirstOrDefaultAsync on in-memory collection ***
                var existingScore = existingScores.FirstOrDefault(es =>
                    es.StudentId == currentStudent.Id && es.RubricCriteriaId == criteria.RubricCriteriaId);

                if (existingScore != null)
                {
                    studentScores[currentStudent.Id][criteria.RubricCriteriaId] = existingScore;
                }
            }

            var course = await _context.CourseRoles.FindAsync(int.Parse(courseId));

            var viewModel = new AssessmentMarkingViewModel
            {
                Assessment = assessment,
                Students = new List<Student> { currentStudent }, // Only current student
                RubricTasks = rubricTasks,
                RubricCriterias = rubricCriterias,
                CriteriaScores = criteriaScores,
                StudentScores = studentScores,
                CourseCode = course?.CourseCode,
                Year = course.Year,
                Trimester = course.Trimester,
                CourseRolesId = int.Parse(courseId),
                Role = role,
                // Add pagination info
                CurrentStudentIndex = studentIndex,
                TotalStudents = allStudents.Count,
                AllStudents = allStudents // For navigation
            };

            return View(viewModel);
        }


        // POST: StudentAssessments/SaveScores
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveScores(int assessmentId, string courseId, string role,
    Dictionary<string, string> scores, Dictionary<string, string> comments)
        {
            try
            {
                _logger.LogInformation("SaveScores called for assessmentId: {AssessmentId}, scores count: {ScoreCount}, comments count: {CommentCount}",
                    assessmentId, scores?.Count ?? 0, comments?.Count ?? 0);

                var assessment = await _context.Assessments.FindAsync(assessmentId);
                if (assessment == null)
                {
                    _logger.LogWarning("Assessment not found with ID: {AssessmentId}", assessmentId);
                    return Json(new { success = false, message = "Assessment not found." });
                }

                // Get all rubric criteria for this assessment
                var rubricTasks = await _context.RubricTask
                    .Where(rt => rt.RubricsId == assessment.RubricsId)
                    .ToListAsync();

                var rubricCriterias = await _context.RubricCriteria
                    .Where(rc => rubricTasks.Select(rt => rt.RubricTaskId).Contains(rc.RubricTaskId))
                    .ToListAsync();

                var totalCriteria = rubricCriterias.Count;

                _logger.LogInformation("Assessment has {TotalCriteria} criteria that need to be scored", totalCriteria);

                // Validate that all criteria are marked for the current student
                // Extract unique student IDs from the submitted scores
                var studentIds = scores.Keys
                    .Select(key => key.Replace("score_", "").Split('_')[0])
                    .Where(id => int.TryParse(id, out _))
                    .Select(id => int.Parse(id))
                    .Distinct()
                    .ToList();

                if (!studentIds.Any())
                {
                    _logger.LogWarning("No student scores found in submission");
                    return Json(new { success = false, message = "No scores found to save." });
                }

                // Check each student has all criteria marked
                var unmarkedCriteria = new List<string>();
                foreach (var studentId in studentIds)
                {
                    var student = await _context.Student.FindAsync(studentId);
                    var studentName = student?.Name ?? $"Student {studentId}";

                    var studentScoreCount = scores.Keys
                        .Count(key => key.StartsWith($"score_{studentId}_"));

                    _logger.LogDebug("Student {StudentId} has {ScoreCount} scores out of {TotalCriteria} criteria",
                        studentId, studentScoreCount, totalCriteria);

                    if (studentScoreCount < totalCriteria)
                    {
                        // Find which criteria are missing
                        var markedCriteriaIds = scores.Keys
                            .Where(key => key.StartsWith($"score_{studentId}_"))
                            .Select(key => key.Replace($"score_{studentId}_", ""))
                            .Where(id => int.TryParse(id, out _))
                            .Select(id => int.Parse(id))
                            .ToHashSet();

                        var missingCriteria = rubricCriterias
                            .Where(rc => !markedCriteriaIds.Contains(rc.RubricCriteriaId))
                            .Select(rc => rc.CriterionTitle)
                            .ToList();

                        unmarkedCriteria.Add($"{studentName}: {string.Join(", ", missingCriteria)}");
                        
                        _logger.LogWarning("Student {StudentId} is missing scores for {MissingCount} criteria: {MissingCriteria}",
                            studentId, missingCriteria.Count, string.Join(", ", missingCriteria));
                    }
                }

                // If any criteria are unmarked, return validation error
                if (unmarkedCriteria.Any())
                {
                    var errorMessage = $"Please mark all criteria before saving. Missing scores for:\n{string.Join("\n", unmarkedCriteria)}";
                    _logger.LogWarning("Validation failed: Not all criteria marked for all students");
                    return Json(new { 
                        success = false, 
                        message = errorMessage,
                        unmarkedCriteria = unmarkedCriteria
                    });
                }

                var existingScores = await _context.StudentAssessmentScores
                    .Where(sas => sas.AssessmentId == assessmentId)
                    .ToListAsync();

                _logger.LogInformation("Found {ExistingScoreCount} existing scores for assessment {AssessmentId}",
                    existingScores.Count, assessmentId);

                int savedCount = 0;
                int updatedCount = 0;

                foreach (var scoreEntry in scores)
                {
                    _logger.LogDebug("Processing score entry: {Key} = {Value}", scoreEntry.Key, scoreEntry.Value);

                    // Parse the key format: "score_studentId_criteriaId"
                    var keyParts = scoreEntry.Key.Replace("score_", "").Split('_');
                    if (keyParts.Length != 2)
                    {
                        _logger.LogWarning("Invalid score key format: {Key}", scoreEntry.Key);
                        continue;
                    }

                    if (!int.TryParse(keyParts[0], out int studentId) ||
                        !int.TryParse(keyParts[1], out int criteriaId) ||
                        !int.TryParse(scoreEntry.Value, out int score))
                    {
                        _logger.LogWarning("Failed to parse score data: key={Key}, value={Value}", scoreEntry.Key, scoreEntry.Value);
                        continue;
                    }

                    // Get corresponding comment
                    var commentKey = $"comment_{studentId}_{criteriaId}";
                    var customComment = comments.ContainsKey(commentKey) ? comments[commentKey] : "";

                    _logger.LogDebug("Parsed data - StudentId: {StudentId}, CriteriaId: {CriteriaId}, Score: {Score}, Comment: {Comment}",
                        studentId, criteriaId, score, customComment?.Length ?? 0);

                    // Find existing score or create new one
                    var existingScore = existingScores.FirstOrDefault(es =>
                        es.StudentId == studentId && es.RubricCriteriaId == criteriaId);

                    if (existingScore != null)
                    {
                        // Update existing score
                        existingScore.Score = score;
                        existingScore.CustomComment = customComment;
                        existingScore.LastModified = DateTime.Now;
                        _context.Update(existingScore);
                        updatedCount++;

                        _logger.LogDebug("Updated existing score for Student {StudentId}, Criteria {CriteriaId}",
                            studentId, criteriaId);
                    }
                    else
                    {
                        // Create new score
                        var newScore = new StudentAssessmentScore
                        {
                            AssessmentId = assessmentId,
                            StudentId = studentId,
                            RubricCriteriaId = criteriaId,
                            Score = score,
                            CustomComment = customComment == null ? "" : customComment,
                            ModeratorComments = "",
                            LastModified = DateTime.Now
                        };
                        _context.Add(newScore);
                        savedCount++;

                        _logger.LogDebug("Created new score for Student {StudentId}, Criteria {CriteriaId}",
                            studentId, criteriaId);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully saved scores: {SavedCount} new, {UpdatedCount} updated for assessment {AssessmentId}",
                    savedCount, updatedCount, assessmentId);

                return Json(new
                {
                    success = true,
                    message = $"Scores saved successfully!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving scores for assessment {AssessmentId}", assessmentId);
                return Json(new
                {
                    success = false,
                    message = "An error occurred while saving scores. Please try again.",
                    error = ex.Message
                });
            }
        }


        // GET: StudentAssessments/GenerateFeedback/5?studentId=1 OR GenerateFeedback/5?studentIndex=0 (for batch)
        public async Task<IActionResult> GenerateFeedback(int id, int? studentId, string courseId, string role, int? studentIndex)
        {
            var assessment = await _context.Assessments
                .Include(a => a.Rubric)
                .FirstOrDefaultAsync(a => a.AssessmentId == id);

            if (assessment == null)
            {
                TempData["ErrorMessage"] = "Assessment not found.";
                return RedirectToAction("Index", new { courseId, role });
            }

            // Get all students enrolled in this course
            var students = await _context.CourseStudent
                .Where(cs => cs.CourseRolesId == int.Parse(courseId))
                .Include(cs => cs.Student)
                .Select(cs => cs.Student)
                .OrderBy(s => s.StudentId)
                .ToListAsync();

            if (!students.Any())
            {
                TempData["ErrorMessage"] = "No students enrolled in this course.";
                return RedirectToAction("Index", new { courseId, role });
            }

            ViewBag.CourseId = courseId;
            ViewBag.Role = role;
            ViewBag.AssessmentId = id;

            // Determine mode: Batch mode if no studentId is provided
            var isBatchMode = !studentId.HasValue;

            if (isBatchMode)
            {
                // BATCH MODE with pagination
                var currentIndex = studentIndex ?? 0;
                
                // Ensure index is valid
                if (currentIndex < 0 || currentIndex >= students.Count)
                    currentIndex = 0;

                var currentStudent = students[currentIndex];
                
                _logger.LogInformation("Generating batch feedback - Student {CurrentIndex} of {TotalStudents} in assessment {AssessmentId}", 
                    currentIndex + 1, students.Count, id);

                // Generate feedback for current student
                var currentFeedback = await GenerateStudentFeedbackAsync(assessment, currentStudent);
                
                // Generate all feedbacks for print preview (done once, cached)
                var allFeedbacks = new List<StudentFeedbackViewModel>();
                foreach (var student in students)
                {
                    var feedback = await GenerateStudentFeedbackAsync(assessment, student);
                    allFeedbacks.Add(feedback);
                }

                // Set batch mode properties
                currentFeedback.IsBatchMode = true;
                currentFeedback.CurrentStudentIndex = currentIndex;
                currentFeedback.TotalStudents = students.Count;
                currentFeedback.AllStudents = students;
                currentFeedback.AllStudentFeedbacks = allFeedbacks;
                currentFeedback.CourseRolesId = int.Parse(courseId);
                currentFeedback.Role = role;
                currentFeedback.CourseCode = assessment.CourseCode;
                currentFeedback.CourseName = assessment.Rubric?.CourseName;
                currentFeedback.Year = assessment.Year;
                currentFeedback.Trimester = assessment.Trimester;

                return View(currentFeedback);
            }
            else
            {
                // SINGLE STUDENT MODE (existing behavior)
                var student = await _context.Student.FindAsync(studentId.Value);

                if (student == null)
                {
                    TempData["ErrorMessage"] = "Student not found.";
                    return RedirectToAction("Index", new { courseId, role });
                }

                var feedback = await GenerateStudentFeedbackAsync(assessment, student);
                feedback.IsBatchMode = false;
                feedback.CourseRolesId = int.Parse(courseId);
                feedback.Role = role;
                
                return View(feedback);
            }
        }
    

        private async Task<StudentFeedbackViewModel> GenerateStudentFeedbackAsync(Assessment assessment, Student student)
        {
            var rubricTasks = await _context.RubricTask
                .Where(rt => rt.RubricsId == assessment.RubricsId)
                .ToListAsync();

            var rubricCriterias = await _context.RubricCriteria
                .Where(rc => rubricTasks.Select(rt => rt.RubricTaskId).Contains(rc.RubricTaskId))
                .ToListAsync();

            var criteriaScores = await _context.RubricCriteriaScore
                .Where(rcs => rubricCriterias.Select(rc => rc.RubricCriteriaId).Contains(rcs.RubricCriteriaId))
                .ToListAsync();

            var studentScores = await _context.StudentAssessmentScores
                .Where(sas => sas.AssessmentId == assessment.AssessmentId && sas.StudentId == student.Id)
                .ToListAsync();

            var criteriaResults = new List<StudentCriteriaResult>();
            var taskSummaries = new Dictionary<string, TaskScoreSummary>();
            
            double totalActualMarks = 0;
            double totalMaxMarks = 0;
                
            foreach (var task in rubricTasks)
            {
                var taskCriterias = rubricCriterias.Where(rc => rc.RubricTaskId == task.RubricTaskId).ToList();
                double taskWeightedScore = 0;
                double taskMaxWeightedScore = 0;

                foreach (var criteria in taskCriterias)
                {
                    var studentScore = studentScores.FirstOrDefault(ss => ss.RubricCriteriaId == criteria.RubricCriteriaId);
                    var score = studentScore?.Score ?? 0;
                    var maxScore = criteriaScores.Where(cs => cs.RubricCriteriaId == criteria.RubricCriteriaId)
                        .Max(cs => cs.CriterionScore);

                    double weightedScore = (score * criteria.Weight) / 100.0;
                    double maxWeightedScore = (maxScore * criteria.Weight) / 100.0;

                    taskWeightedScore += weightedScore;
                    taskMaxWeightedScore += maxWeightedScore;

                    var scoreDescription = criteriaScores.FirstOrDefault(cs =>
                        cs.RubricCriteriaId == criteria.RubricCriteriaId && cs.CriterionScore == score)?.ScoreDescription ?? "";

                    // CHANGED: Use rule-based feedback for criteria (not AI)
                    var generatedFeedback = GenerateFeedbackComment(criteria, score, scoreDescription);

                    criteriaResults.Add(new StudentCriteriaResult
                    {
                        TaskTitle = task.TaskTitle,
                        CriteriaTitle = criteria.CriterionTitle,
                        Score = score,
                        MaxScore = maxScore,
                        Weight = criteria.Weight,
                        WeightedScore = weightedScore,
                        MaxWeightedScore = maxWeightedScore,
                        ScoreDescription = scoreDescription,
                        GeneratedFeedback = generatedFeedback,
                        CustomComment = studentScore?.CustomComment
                    });
                }

                double taskPercentage = taskMaxWeightedScore > 0 ? (taskWeightedScore / taskMaxWeightedScore * 100) : 0;
                double actualMarks = (taskPercentage / 100.0) * task.MaxMarks;
                
                totalActualMarks += actualMarks;
                totalMaxMarks += task.MaxMarks;

                taskSummaries[task.TaskTitle] = new TaskScoreSummary
                {
                    TaskTitle = task.TaskTitle,
                    TaskDescription = task.TaskDescription,
                    TotalWeightedScore = taskWeightedScore,
                    MaxWeightedScore = taskMaxWeightedScore,
                    Percentage = taskPercentage,
                    MaxMarks = task.MaxMarks,
                    ActualMarks = actualMarks
                };
            }

            var percentage = totalMaxMarks > 0 ? (totalActualMarks / totalMaxMarks * 100) : 0;
            
            // CHANGED: Use new method that saves/retrieves from database
            var overallFeedback = await _puterAIService.GetOrGenerateOverallFeedbackAsync(
                assessment.AssessmentId, 
                student.Id, 
                percentage, 
                criteriaResults);

            return new StudentFeedbackViewModel
            {
                Student = student,
                Assessment = assessment,
                CriteriaResults = criteriaResults,
                TaskSummaries = taskSummaries,
                TotalScore = (int)Math.Round(totalActualMarks),
                MaxPossibleScore = (int)Math.Round(totalMaxMarks),
                Percentage = percentage,
                OverallFeedback = overallFeedback
            };
        }

        private string GenerateFeedbackComment(RubricCriteria criteria, int score, string scoreDescription)
        {
            var feedback = new StringBuilder();

            feedback.AppendLine($"For {criteria.CriterionTitle}:");

            if (score == 4)
            {
                feedback.AppendLine("Excellent work! " + scoreDescription);
                feedback.AppendLine("Continue maintaining this high standard of performance.");
            }
            else if (score == 3)
            {
                feedback.AppendLine("Good work! " + scoreDescription);
                feedback.AppendLine("With minor improvements, you can achieve excellent results.");
            }
            else if (score == 2)
            {
                feedback.AppendLine("Satisfactory performance. " + scoreDescription);
                feedback.AppendLine("Focus on strengthening this area to improve your overall performance.");
            }
            else if (score == 1)
            {
                feedback.AppendLine("Needs improvement. " + scoreDescription);
                feedback.AppendLine("Please review the requirements and seek additional support if needed.");
            }
            else
            {
                feedback.AppendLine("Not demonstrated. " + scoreDescription);
                feedback.AppendLine("This area requires significant attention and improvement.");
            }

            return feedback.ToString();
        }

        private string GenerateOverallFeedback(double percentage, List<StudentCriteriaResult> criteriaResults)
        {
            var feedback = new StringBuilder();

            if (percentage >= 85)
            {
                feedback.AppendLine("Outstanding performance! You have demonstrated excellent understanding and skill across all criteria.");
            }
            else if (percentage >= 70)
            {
                feedback.AppendLine("Good overall performance with solid understanding demonstrated in most areas.");
            }
            else if (percentage >= 50)
            {
                feedback.AppendLine("Satisfactory performance. You have met the basic requirements but there is room for improvement.");
            }
            else
            {
                feedback.AppendLine("Your performance indicates significant areas requiring improvement. Please seek additional support.");
            }

            // Identify strengths and weaknesses
            var strengths = criteriaResults.Where(cr => cr.Score >= 3).ToList();
            var weaknesses = criteriaResults.Where(cr => cr.Score <= 1).ToList();

            if (strengths.Any())
            {
                feedback.AppendLine($"\nStrengths: {string.Join(", ", strengths.Select(s => s.CriteriaTitle))}");
            }

            if (weaknesses.Any())
            {
                feedback.AppendLine($"\nAreas for improvement: {string.Join(", ", weaknesses.Select(w => w.CriteriaTitle))}");
            }

            return feedback.ToString();
        }

        private async Task<StudentFeedbackViewModel> GenerateStudentFeedbackAsync_ML(Assessment assessment, Student student)
        {
            var rubricTasks = await _context.RubricTask
                .Where(rt => rt.RubricsId == assessment.RubricsId)
                .ToListAsync();

            var rubricCriterias = await _context.RubricCriteria
                .Where(rc => rubricTasks.Select(rt => rt.RubricTaskId).Contains(rc.RubricTaskId))
                .ToListAsync();

            var criteriaScores = await _context.RubricCriteriaScore
                .Where(rcs => rubricCriterias.Select(rc => rc.RubricCriteriaId).Contains(rcs.RubricCriteriaId))
                .ToListAsync();

            var studentScores = await _context.StudentAssessmentScores
                .Where(sas => sas.AssessmentId == assessment.AssessmentId && sas.StudentId == student.Id)
                .ToListAsync();

            var criteriaResults = new List<StudentCriteriaResult>();
            var taskSummaries = new Dictionary<string, TaskScoreSummary>();
            int totalScore = 0;
            int maxPossibleScore = 0;

            foreach (var task in rubricTasks)
            {
                var taskCriterias = rubricCriterias.Where(rc => rc.RubricTaskId == task.RubricTaskId).ToList();
                double taskWeightedScore = 0;
                double taskMaxWeightedScore = 0;

                foreach (var criteria in taskCriterias)
                {
                    var studentScore = studentScores.FirstOrDefault(ss => ss.RubricCriteriaId == criteria.RubricCriteriaId);
                    var score = studentScore?.Score ?? 0;
                    var maxScore = criteriaScores.Where(cs => cs.RubricCriteriaId == criteria.RubricCriteriaId)
                        .Max(cs => cs.CriterionScore);

                    // Calculate weighted scores
                    double weightedScore = (score * criteria.Weight) / 100.0;
                    double maxWeightedScore = (maxScore * criteria.Weight) / 100.0;

                    taskWeightedScore += weightedScore;
                    taskMaxWeightedScore += maxWeightedScore;

                    var scoreDescription = criteriaScores.FirstOrDefault(cs =>
                        cs.RubricCriteriaId == criteria.RubricCriteriaId && cs.CriterionScore == score)?.ScoreDescription ?? "";

                    var scoreTitle = criteriaScores.FirstOrDefault(cs =>
                        cs.RubricCriteriaId == criteria.RubricCriteriaId && cs.CriterionScore == score)?.ScoreTitle ?? "";

                    // Use ML.NET-powered feedback generation
                    var generatedFeedback = await _feedbackService.GenerateFeedbackAsync(
                        criteria, score, scoreDescription, scoreTitle, task.TaskTitle, studentScore?.CustomComment);

                    criteriaResults.Add(new StudentCriteriaResult
                    {
                        TaskTitle = task.TaskTitle,
                        CriteriaTitle = criteria.CriterionTitle,
                        Score = score,
                        MaxScore = maxScore,
                        Weight = criteria.Weight,
                        WeightedScore = weightedScore,
                        MaxWeightedScore = maxWeightedScore,
                        ScoreDescription = scoreDescription,
                        GeneratedFeedback = generatedFeedback,
                        CustomComment = studentScore?.CustomComment
                    });

                    totalScore += score;
                    maxPossibleScore += maxScore;
                }

                // Calculate actual marks for this task based on weighted scores and task max marks
                double taskPercentage = taskMaxWeightedScore > 0 ? (taskWeightedScore / taskMaxWeightedScore * 100) : 0;
                // Fix: Calculate actual marks as a percentage of task.MaxMarks
                double actualMarks = (taskPercentage / 100.0) * task.MaxMarks;

                // Add task summary
                taskSummaries[task.TaskTitle] = new TaskScoreSummary
                {
                    TaskTitle = task.TaskTitle,
                    TotalWeightedScore = taskWeightedScore,
                    MaxWeightedScore = taskMaxWeightedScore,
                    Percentage = taskPercentage,
                    MaxMarks = task.MaxMarks,
                    ActualMarks = actualMarks
                };
            }

            var percentage = maxPossibleScore > 0 ? (double)totalScore / maxPossibleScore * 100 : 0;

            // Generate overall feedback using ML service
            var overallFeedback = await _feedbackService.GenerateOverallFeedbackAsync(percentage, criteriaResults);

            return new StudentFeedbackViewModel
            {
                Student = student,
                Assessment = assessment,
                CriteriaResults = criteriaResults,
                TaskSummaries = taskSummaries,
                TotalScore = totalScore,
                MaxPossibleScore = maxPossibleScore,
                Percentage = percentage,
                OverallFeedback = overallFeedback
            };
        }

        // POST: StudentAssessments/UpdateStatusAssessment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatusAssessment(int assessmentId, string courseId, string role, string nextStatus)
        {
            try
            {
                var assessment = await _context.Assessments.FindAsync(assessmentId);
                if (assessment == null)
                {
                    return Json(new { success = false, message = "Assessment not found." });
                }

                // Update assessment status
                var currentUser = await _userManager.GetUserAsync(User);
                assessment.Status = nextStatus;
                assessment.StatusChangedBy = currentUser?.UserName ?? "Unknown";
                assessment.StatusChangedDate = DateTime.Now;

                _context.Update(assessment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Assessment {AssessmentId} sent to {NextStatus} by {User}", 
                    assessmentId, nextStatus, currentUser?.UserName);

                return Json(new { 
                    success = true, 
                    message = $"Assessment has been successfully sent to {nextStatus}." 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending assessment {AssessmentId} to {NextStatus}", assessmentId, nextStatus);
                return Json(new { 
                    success = false, 
                    message = $"An error occurred while sending to {nextStatus}. Please try again." 
                });
            }
        }

        // POST: StudentAssessments/SaveModeratorComments
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveModeratorComments(int assessmentId, string courseId, string role,
    Dictionary<string, string> moderatorComments)
        {
            try
            {
                _logger.LogInformation("SaveModeratorComments called for assessmentId: {AssessmentId}, comments count: {CommentCount}",
                    assessmentId, moderatorComments?.Count ?? 0);

                var assessment = await _context.Assessments.FindAsync(assessmentId);
                if (assessment == null)
                {
                    _logger.LogWarning("Assessment not found with ID: {AssessmentId}", assessmentId);
                    return Json(new { success = false, message = "Assessment not found." });
                }

                // Verify assessment is in Moderation status
                if (assessment.Status != "Moderation")
                {
                    _logger.LogWarning("Attempted to save moderator comments for assessment {AssessmentId} not in Moderation status. Current status: {Status}",
                        assessmentId, assessment.Status);
                    return Json(new { success = false, message = "Moderator comments can only be saved during Moderation status." });
                }

                var existingScores = await _context.StudentAssessmentScores
                    .Where(sas => sas.AssessmentId == assessmentId)
                    .ToListAsync();

                _logger.LogInformation("Found {ExistingScoreCount} existing student scores for assessment {AssessmentId}",
                    existingScores.Count, assessmentId);

                int updatedCount = 0;
                int notFoundCount = 0;

                foreach (var commentEntry in moderatorComments)
                {
                    _logger.LogDebug("Processing moderator comment entry: {Key}", commentEntry.Key);

                    // Parse the key format: "moderator_comment_studentId_criteriaId"
                    var keyParts = commentEntry.Key.Replace("moderator_comment_", "").Split('_');
                    if (keyParts.Length != 2)
                    {
                        _logger.LogWarning("Invalid moderator comment key format: {Key}", commentEntry.Key);
                        continue;
                    }

                    if (!int.TryParse(keyParts[0], out int studentId) ||
                        !int.TryParse(keyParts[1], out int criteriaId))
                    {
                        _logger.LogWarning("Failed to parse moderator comment data: key={Key}", commentEntry.Key);
                        continue;
                    }

                    var moderatorComment = commentEntry.Value ?? "";

                    _logger.LogDebug("Parsed data - StudentId: {StudentId}, CriteriaId: {CriteriaId}, Comment length: {CommentLength}",
                        studentId, criteriaId, moderatorComment.Length);

                    // Find existing score record
                    var existingScore = existingScores.FirstOrDefault(es =>
                        es.StudentId == studentId && es.RubricCriteriaId == criteriaId);

                    if (existingScore != null)
                    {
                        // Update moderator comment
                        existingScore.ModeratorComments = moderatorComment;
                        existingScore.LastModified = DateTime.Now;
                        _context.Update(existingScore);
                        updatedCount++;

                        _logger.LogDebug("Updated moderator comment for Student {StudentId}, Criteria {CriteriaId}",
                            studentId, criteriaId);
                    }
                    else
                    {
                        // Log if score record doesn't exist (this shouldn't happen in normal workflow)
                        _logger.LogWarning("No existing score record found for Student {StudentId}, Criteria {CriteriaId}. Moderator comment cannot be saved without a score.",
                            studentId, criteriaId);
                        notFoundCount++;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully saved moderator comments: {UpdatedCount} updated for assessment {AssessmentId}",
                    updatedCount, assessmentId);

                var message = $"Moderator comments saved successfully! {updatedCount} comments updated.";
                if (notFoundCount > 0)
                {
                    message += $" ({notFoundCount} records not found - scores may need to be entered first)";
                }

                return Json(new
                {
                    success = true,
                    message = message,
                    updatedCount = updatedCount,
                    notFoundCount = notFoundCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving moderator comments for assessment {AssessmentId}", assessmentId);
                return Json(new
                {
                    success = false,
                    message = "An error occurred while saving moderator comments. Please try again.",
                    error = ex.Message
                });
            }
        }

        // Add this method to your existing StudentAssessmentsController

[HttpPost]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> TrainMLModels()
{
    try
    {
        var trainer = new MLModelTrainer(_context, _environment);
        
        // Prepare training data from database
        await trainer.PrepareTrainingDataFromDatabase();
        
        // Train both models
        var feedbackResult = await trainer.TrainFeedbackModelAsync();
        var sentimentResult = await trainer.TrainSentimentModelAsync();
        
        if (feedbackResult && sentimentResult)
        {
            return Json(new { 
                success = true, 
                message = "ML models trained successfully! The feedback service will reload the models automatically." 
            });
        }
        else
        {
            return Json(new { 
                success = false, 
                message = "One or more models failed to train. Check server logs for details." 
            });
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error training ML models");
        return Json(new { 
            success = false, 
            message = $"Error: {ex.Message}" 
        });
    }
}

// Add this new method to save AI-generated feedback from the browser
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SaveAIGeneratedFeedback(int assessmentId, int studentId, string feedback)
{
    try
    {
        if (string.IsNullOrWhiteSpace(feedback))
        {
            return Json(new { success = false, message = "Feedback cannot be empty." });
        }

        // Check if feedback already exists
        var existingFeedback = await _context.StudentOverallFeedback
            .FirstOrDefaultAsync(f => f.AssessmentId == assessmentId && f.StudentId == studentId);

        if (existingFeedback != null)
        {
            // Update existing feedback
            existingFeedback.OverallFeedback = feedback;
            existingFeedback.LastModified = DateTime.Now;
            _context.Update(existingFeedback);
            
            _logger.LogInformation("Updated AI-generated feedback for Student {StudentId}, Assessment {AssessmentId}", 
                studentId, assessmentId);
        }
        else
        {
            // Create new feedback
            var newFeedback = new StudentOverallFeedback
            {
                AssessmentId = assessmentId,
                StudentId = studentId,
                OverallFeedback = feedback,
                GeneratedDate = DateTime.Now
            };
            
            _context.StudentOverallFeedback.Add(newFeedback);
            
            _logger.LogInformation("Saved AI-generated feedback for Student {StudentId}, Assessment {AssessmentId}", 
                studentId, assessmentId);
        }

        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Feedback saved successfully." });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error saving AI-generated feedback for Student {StudentId}, Assessment {AssessmentId}", 
            studentId, assessmentId);
        
        return Json(new { 
            success = false, 
            message = "An error occurred while saving feedback." 
        });
    }
}

// Add this new GET action to check if student has scores saved in database
[HttpGet]
public async Task<IActionResult> CheckStudentScoreExists(int assessmentId, int studentId)
{
    try
    {
        var hasScores = await _context.StudentAssessmentScores
            .AnyAsync(sas => sas.AssessmentId == assessmentId && sas.StudentId == studentId);

        return Json(new { exists = hasScores });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error checking if student {StudentId} has scores for assessment {AssessmentId}", 
            studentId, assessmentId);
        return Json(new { exists = false });
    }
}
    }
}
