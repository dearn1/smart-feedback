using smart_feedback.Models;
using smart_feedback.Models.ViewModels;


namespace smart_feedback.Services
{
    public interface IFeedbackGenerationService
    {
        Task<string> GenerateFeedbackAsync(RubricCriteria criteria, int score, string scoreDescription, string scoreTitle, string taskTitle, string customComment = null);
        Task<string> GenerateOverallFeedbackAsync(double percentage, List<StudentCriteriaResult> criteriaResults);
        Task<string> ImproveFeedbackAsync(string originalFeedback, string context);
        Task<bool> AnalyzeFeedbackSentimentAsync(string feedback);
        Task TrainModelAsync();
    }
}
