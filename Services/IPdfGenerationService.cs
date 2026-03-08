namespace smart_feedback.Services
{
    public interface IPdfGenerationService
    {
        byte[] GenerateStudentFeedbackPdf(Models.ViewModels.StudentFeedbackViewModel feedback);
        Task<List<(string FileName, byte[] PdfData)>> GenerateBatchPdfsAsync(List<Models.ViewModels.StudentFeedbackViewModel> feedbacks);
        Task<List<(string fileName, byte[] pdfData)>> GenerateFinalReportPdfsAsync(List<Models.ViewModels.FinalReportViewModel> reports);
    }
}