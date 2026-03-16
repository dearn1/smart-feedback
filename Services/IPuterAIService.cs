using smart_feedback.Models.ViewModels;

namespace smart_feedback.Services
{
    public interface IPuterAIService
    {
        // Only keep overall feedback method
        Task<string> GenerateOverallConstructiveFeedbackAsync(double percentage, List<StudentCriteriaResult> criteriaResults);
        
        // NEW METHOD: Get existing feedback or generate and save new one
        Task<string> GetOrGenerateOverallFeedbackAsync(int assessmentId, int studentId, double percentage, List<StudentCriteriaResult> criteriaResults);
    }
}