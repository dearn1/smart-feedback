using System.Threading.Tasks;

namespace smart_feedback.Services
{
    public interface IEmailService
    {
        Task SendPasswordEmailAsync(string toEmail, string fullName, string tempPassword);
        Task SendPasswordResetEmailAsync(string toEmail, string fullName, string tempPassword);
        Task SendPasswordChangeConfirmationEmailAsync(string toEmail, string fullName);
        Task SendAssessmentStatusChangeEmailAsync(string toEmail, string fullName, string assessmentName, string courseCode, string courseName, string oldStatus, string newStatus);
    }
}
