using System.Threading.Tasks;

namespace smart_feedback.Services
{
    public interface IEmailService
    {
        Task SendPasswordEmailAsync(string toEmail, string fullName, string tempPassword);
        Task SendPasswordResetEmailAsync(string toEmail, string fullName, string tempPassword);
    }
}
