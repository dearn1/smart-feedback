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
        private readonly ILogger<StudentAssessmentsController> _logger; 

        public StudentAssessmentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IFeedbackGenerationService feedbackService, ILogger<StudentAssessmentsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _feedbackService = feedbackService;
            _logger = logger;
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
                .Where(a => a.CourseCode == course.CourseCode && a.TermName == course.TermName)
                .ToListAsync();

            var students = await _context.Student.ToListAsync();

            var availableRubrics = await _context.Rubrics
                .Where(r => r.CourseCode == course.CourseCode && r.TermName == course.TermName)
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

            var viewModel = new StudentAssessmentViewModel
            {
                CourseRolesId = course.CourseRolesId,
                CourseCode = course.CourseCode,
                CourseName = course.CourseName,
                TermName = course.TermName,
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

            var availableRubrics = await _context.Rubrics
                .Where(r => r.CourseCode == course.CourseCode && r.TermName == course.TermName)
                .ToListAsync();

            ViewBag.AvailableRubrics = availableRubrics;
            ViewBag.CourseId = courseId;
            ViewBag.Role = role;
            ViewBag.CourseCode = course.CourseCode;
            ViewBag.TermName = course.TermName;

            return View();
        }

        // POST: StudentAssessments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Assessment assessment, string courseId, string role)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            assessment.CreatedBy = currentUser?.UserName;
            assessment.CreatedDate = DateTime.Now;
            assessment.Rubric = null; // To avoid EF Core tracking issues

            //if (ModelState.IsValid)
            //{
                
                

                _context.Add(assessment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Assessment created successfully.";
                return RedirectToAction("Index", new { courseId, role });
            //}

            var course = await _context.CourseRoles.FindAsync(int.Parse(courseId));
            var availableRubrics = await _context.Rubrics
                .Where(r => r.CourseCode == course.CourseCode && r.TermName == course.TermName)
                .ToListAsync();

            ViewBag.AvailableRubrics = availableRubrics;
            ViewBag.CourseId = courseId;
            ViewBag.Role = role;
            ViewBag.CourseCode = course?.CourseCode;
            ViewBag.TermName = course?.TermName;

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

            var allStudents = await _context.Student.ToListAsync();

            // Handle pagination
            if (studentIndex < 0 || studentIndex >= allStudents.Count)
                studentIndex = 0;

            var currentStudent = allStudents.Skip(studentIndex).FirstOrDefault();
            if (currentStudent == null)
            {
                TempData["ErrorMessage"] = "No students found.";
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
                TermName = course?.TermName,
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
                            CustomComment = customComment,
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
                    message = $"Scores saved successfully! {savedCount} new scores created, {updatedCount} scores updated.",
                    savedCount = savedCount,
                    updatedCount = updatedCount
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


        // GET: StudentAssessments/GenerateFeedback/5?studentId=1
        public async Task<IActionResult> GenerateFeedback(int id, int studentId, string courseId, string role)
        {
            var assessment = await _context.Assessments
                .Include(a => a.Rubric)
                .FirstOrDefaultAsync(a => a.AssessmentId == id);

            var student = await _context.Student.FindAsync(studentId);

            if (assessment == null || student == null)
            {
                TempData["ErrorMessage"] = "Assessment or student not found.";
                return RedirectToAction("Mark", new { id, courseId, role });
            }

            var feedback = await GenerateStudentFeedbackAsync(assessment, student);

            ViewBag.CourseId = courseId;
            ViewBag.Role = role;
            ViewBag.AssessmentId = id;

            return View(feedback);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TrainMLModel()
        {
            try
            {
                await _feedbackService.TrainModelAsync();
                TempData["SuccessMessage"] = "ML model training initiated successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to train ML model: " + ex.Message;
            }

            return RedirectToAction("Index");
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
            int totalScore = 0;
            int maxPossibleScore = 0;

            foreach (var task in rubricTasks)
            {
                var taskCriterias = rubricCriterias.Where(rc => rc.RubricTaskId == task.RubricTaskId);

                foreach (var criteria in taskCriterias)
                {
                    var studentScore = studentScores.FirstOrDefault(ss => ss.RubricCriteriaId == criteria.RubricCriteriaId);
                    var score = studentScore?.Score ?? 0;
                    var maxScore = criteriaScores.Where(cs => cs.RubricCriteriaId == criteria.RubricCriteriaId)
                        .Max(cs => cs.CriterionScore);

                    var scoreDescription = criteriaScores.FirstOrDefault(cs =>
                        cs.RubricCriteriaId == criteria.RubricCriteriaId && cs.CriterionScore == score)?.ScoreDescription ?? "";

                    var generatedFeedback = GenerateFeedbackComment(criteria, score, scoreDescription);

                    criteriaResults.Add(new StudentCriteriaResult
                    {
                        TaskTitle = task.TaskTitle,
                        CriteriaTitle = criteria.CriterionTitle,
                        Score = score,
                        MaxScore = maxScore,
                        ScoreDescription = scoreDescription,
                        GeneratedFeedback = generatedFeedback,
                        CustomComment = studentScore?.CustomComment
                    });

                    totalScore += score;
                    maxPossibleScore += maxScore;
                }
            }

            var percentage = maxPossibleScore > 0 ? (double)totalScore / maxPossibleScore * 100 : 0;
            var overallFeedback = GenerateOverallFeedback(percentage, criteriaResults);

            return new StudentFeedbackViewModel
            {
                Student = student,
                Assessment = assessment,
                CriteriaResults = criteriaResults,
                TotalScore = totalScore,
                MaxPossibleScore = maxPossibleScore,
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
            int totalScore = 0;
            int maxPossibleScore = 0;

            foreach (var task in rubricTasks)
            {
                var taskCriterias = rubricCriterias.Where(rc => rc.RubricTaskId == task.RubricTaskId);

                foreach (var criteria in taskCriterias)
                {
                    var studentScore = studentScores.FirstOrDefault(ss => ss.RubricCriteriaId == criteria.RubricCriteriaId);
                    var score = studentScore?.Score ?? 0;
                    var maxScore = criteriaScores.Where(cs => cs.RubricCriteriaId == criteria.RubricCriteriaId)
                        .Max(cs => cs.CriterionScore);

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
                        ScoreDescription = scoreDescription,
                        GeneratedFeedback = generatedFeedback,
                        CustomComment = studentScore?.CustomComment
                    });

                    totalScore += score;
                    maxPossibleScore += maxScore;
                }
            }

            var percentage = maxPossibleScore > 0 ? (double)totalScore / maxPossibleScore * 100 : 0;

            // Use ML.NET-powered overall feedback generation
            var overallFeedback = await _feedbackService.GenerateOverallFeedbackAsync(percentage, criteriaResults);

            return new StudentFeedbackViewModel
            {
                Student = student,
                Assessment = assessment,
                CriteriaResults = criteriaResults,
                TotalScore = totalScore,
                MaxPossibleScore = maxPossibleScore,
                Percentage = percentage,
                OverallFeedback = overallFeedback
            };
        }
    }
}
