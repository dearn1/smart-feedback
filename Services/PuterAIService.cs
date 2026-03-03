using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using smart_feedback.Data;
using smart_feedback.Models;
using smart_feedback.Models.ViewModels;
using System.Text;

namespace smart_feedback.Services
{
    public class PuterAIService : IPuterAIService
    {
        private readonly ILogger<PuterAIService> _logger;
        private readonly ApplicationDbContext _context;

        public PuterAIService(ILogger<PuterAIService> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Only overall feedback - serves as fallback when JavaScript fails
        public async Task<string> GenerateOverallConstructiveFeedbackAsync(double percentage, List<Models.ViewModels.StudentCriteriaResult> criteriaResults)
        {
            var strengths = criteriaResults.Where(cr => cr.Score >= (cr.MaxScore * 0.75)).Select(cr => cr.CriteriaTitle).ToList();
            var improvements = criteriaResults.Where(cr => cr.Score < (cr.MaxScore * 0.5)).Select(cr => cr.CriteriaTitle).ToList();
            return await Task.FromResult(GenerateFallbackOverallFeedback(percentage, strengths, improvements));
        }

        // NEW METHOD: Get existing feedback or generate and save new one
        public async Task<string> GetOrGenerateOverallFeedbackAsync(int assessmentId, int studentId, double percentage, List<StudentCriteriaResult> criteriaResults)
        {
            try
            {
                // Check if feedback already exists in database
                var existingFeedback = await _context.StudentOverallFeedback
                    .FirstOrDefaultAsync(f => f.AssessmentId == assessmentId && f.StudentId == studentId);

                if (existingFeedback != null)
                {
                    _logger.LogInformation("Retrieved existing overall feedback for Student {StudentId}, Assessment {AssessmentId}", 
                        studentId, assessmentId);
                    return existingFeedback.OverallFeedback;
                }

                // Generate new feedback
                _logger.LogInformation("Generating new overall feedback for Student {StudentId}, Assessment {AssessmentId}", 
                    studentId, assessmentId);
                
                var feedback = await GenerateOverallConstructiveFeedbackAsync(percentage, criteriaResults);

                // Save to database
                var newFeedback = new StudentOverallFeedback
                {
                    AssessmentId = assessmentId,
                    StudentId = studentId,
                    OverallFeedback = feedback,
                    GeneratedDate = DateTime.Now
                };

                _context.StudentOverallFeedback.Add(newFeedback);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Saved new overall feedback for Student {StudentId}, Assessment {AssessmentId}", 
                    studentId, assessmentId);

                return feedback;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or generating overall feedback for Student {StudentId}, Assessment {AssessmentId}", 
                    studentId, assessmentId);
                
                // Fallback to generating feedback without saving
                return await GenerateOverallConstructiveFeedbackAsync(percentage, criteriaResults);
            }
        }

        private string GenerateFallbackOverallFeedback(double percentage, List<string> strengths, List<string> improvements)
        {
            var feedback = new StringBuilder();

            // Strengths section
            feedback.AppendLine("**Strengths:**");
            if (percentage >= 85)
            {
                feedback.AppendLine($"Outstanding work! You have achieved an excellent overall score of {percentage:F1}%. Your performance demonstrates strong mastery across multiple criteria.");
                if (strengths.Any())
                {
                    feedback.AppendLine($"You particularly excelled in: {string.Join(", ", strengths)}. This level of achievement shows dedication, understanding, and the ability to apply concepts effectively.");
                }
            }
            else if (percentage >= 70)
            {
                feedback.AppendLine($"Good work! Your overall score of {percentage:F1}% reflects solid understanding and competent performance across the assessment.");
                if (strengths.Any())
                {
                    feedback.AppendLine($"You showed particular strength in: {string.Join(", ", strengths)}. These areas demonstrate your capability and provide a foundation for further improvement.");
                }
            }
            else if (percentage >= 50)
            {
                feedback.AppendLine($"You have achieved a satisfactory score of {percentage:F1}%, meeting the basic requirements of the assessment. This shows that you have grasped the fundamental concepts.");
                if (strengths.Any())
                {
                    feedback.AppendLine($"Areas where you performed well include: {string.Join(", ", strengths)}. Build on these strengths as you work to improve other areas.");
                }
            }
            else
            {
                feedback.AppendLine($"Your current score of {percentage:F1}% indicates that this assessment has been challenging. However, every learning experience is valuable, and this feedback will help guide your improvement.");
            }

            feedback.AppendLine();

            // Areas for Improvement section
            feedback.AppendLine("**Areas for Improvement:**");
            if (improvements.Any())
            {
                feedback.AppendLine($"To enhance your performance, focus your attention on: {string.Join(", ", improvements)}. These areas would benefit from additional study, practice, and review of course materials.");
                feedback.AppendLine();
                feedback.AppendLine("Consider the following strategies:");
                feedback.AppendLine("• Review the relevant course materials and examples for the identified areas");
                feedback.AppendLine("• Practice with additional exercises to reinforce your understanding");
                feedback.AppendLine("• Attend office hours or tutoring sessions for personalized guidance");
                feedback.AppendLine("• Form study groups with classmates to discuss challenging concepts");
                feedback.AppendLine("• Break down complex topics into smaller, manageable components");
            }
            else if (percentage >= 85)
            {
                feedback.AppendLine("You are performing at an excellent level across all criteria. To continue growing, consider taking on more challenging projects, exploring advanced topics, or mentoring fellow students. Reflect on your successful strategies and document them for future reference.");
            }
            else
            {
                feedback.AppendLine("While you've met the requirements, there's always room for growth. Review the individual criterion feedback for specific guidance on how to enhance your skills. Focus on consistent practice and seeking clarification when concepts are unclear.");
            }

            return feedback.ToString();
        }
    }
}