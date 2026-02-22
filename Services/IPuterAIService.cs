namespace smart_feedback.Services
{
    public interface IPuterAIService
    {
        // Only keep overall feedback method
        Task<string> GenerateOverallConstructiveFeedbackAsync(double percentage, List<Models.ViewModels.StudentCriteriaResult> criteriaResults);
    }
}