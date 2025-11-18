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

        public StudentAssessmentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IFeedbackGenerationService feedbackService)
        {
            _context = context;
            _userManager = userManager;
            _feedbackService = feedbackService;
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

            var viewModel = new StudentAssessmentViewModel
            {
                CourseRolesId = course.CourseRolesId,
                CourseCode = course.CourseCode,
                CourseName = course.CourseName,
                TermName = course.TermName,
                Role = role,
                Assessments = assessments,
                Students = students,
                AvailableRubrics = availableRubrics
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
                var assessment = await _context.Assessments.FindAsync(assessmentId);
                if (assessment == null)
                {
                    return Json(new { success = false, message = "Assessment not found." });
                }

                var existingScores = await _context.StudentAssessmentScores
                    .Where(sas => sas.AssessmentId == assessmentId)
                    .ToListAsync();

                foreach (var scoreEntry in scores)
                {
                    var keyParts = scoreEntry.Key.Split('_'); // Format: studentId_criteriaId
                    if (keyParts.Length != 2) continue;

                    if (!int.TryParse(keyParts[0], out int studentId) ||
                        !int.TryParse(keyParts[1], out int criteriaId) ||
                        !int.TryParse(scoreEntry.Value, out int score))
                        continue;

                    var commentKey = $"comment_{studentId}_{criteriaId}";
                    var customComment = comments.ContainsKey(commentKey) ? comments[commentKey] : null;

                    var existingScore = existingScores.FirstOrDefault(es =>
                        es.StudentId == studentId && es.RubricCriteriaId == criteriaId);

                    if (existingScore != null)
                    {
                        existingScore.Score = score;
                        existingScore.CustomComment = customComment;
                        existingScore.LastModified = DateTime.Now;
                        _context.Update(existingScore);
                    }
                    else
                    {
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
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Scores saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while saving scores." });
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
