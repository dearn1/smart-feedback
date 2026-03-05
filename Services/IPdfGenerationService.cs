namespace smart_feedback.Services
{
    public interface IPdfGenerationService
    {
        byte[] GenerateStudentFeedbackPdf(Models.ViewModels.StudentFeedbackViewModel feedback);
        Task<List<(string FileName, byte[] PdfData)>> GenerateBatchPdfsAsync(List<Models.ViewModels.StudentFeedbackViewModel> feedbacks);
    }
}