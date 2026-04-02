using System.Net;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace smart_feedback.Services
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderPassword { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string DefaultTestEmail { get; set; } = string.Empty;
        public bool UseTestEmail { get; set; }
    }

    public class UserEmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<UserEmailService> _logger;
        private readonly IWebHostEnvironment _environment;

        public UserEmailService(IOptions<EmailSettings> emailSettings, ILogger<UserEmailService> logger, IWebHostEnvironment environment)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _environment = environment;

            // Validate configuration
            ValidateEmailSettings();
        }

        private void ValidateEmailSettings()
        {
            if (string.IsNullOrEmpty(_emailSettings.SmtpServer))
                throw new InvalidOperationException("SMTP Server is not configured");

            if (_emailSettings.SmtpPort <= 0)
                throw new InvalidOperationException("SMTP Port is not configured properly");

            if (string.IsNullOrEmpty(_emailSettings.SenderEmail))
                throw new InvalidOperationException("Sender Email is not configured");

            if (string.IsNullOrEmpty(_emailSettings.SenderPassword))
                throw new InvalidOperationException("Sender Password is not configured");

            _logger.LogInformation("Email settings validated successfully. SMTP: {Server}:{Port}, Sender: {Email}",
                _emailSettings.SmtpServer, _emailSettings.SmtpPort, _emailSettings.SenderEmail);
        }

        public async Task SendPasswordEmailAsync(string toEmail, string fullName, string tempPassword)
        {
            try
            {
                var subject = "Welcome to Smart Feedback System - Your Account Information";
                var body = GenerateWelcomeEmailBody(fullName, toEmail, tempPassword);

                // Use test email in development if configured
                var finalToEmail = _environment.IsDevelopment() && _emailSettings.UseTestEmail && !string.IsNullOrEmpty(_emailSettings.DefaultTestEmail)
                    ? _emailSettings.DefaultTestEmail
                    : toEmail;

                _logger.LogInformation("Sending welcome email to {Email} (Original: {OriginalEmail}) for user {FullName}",
                    finalToEmail, toEmail, fullName);

                await SendEmailAsync(finalToEmail, subject, body);

                _logger.LogInformation("Welcome email sent successfully to {Email} for user {FullName}", finalToEmail, fullName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to {Email} for user {FullName}", toEmail, fullName);
                throw;
            }
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string fullName, string tempPassword)
        {
            try
            {
                var subject = "Smart Feedback System - Password Reset";
                var body = GeneratePasswordResetEmailBody(fullName, tempPassword);

                // Use test email in development if configured
                var finalToEmail = _emailSettings.UseTestEmail && !string.IsNullOrEmpty(_emailSettings.DefaultTestEmail)
                    ? _emailSettings.DefaultTestEmail
                    : toEmail;

                _logger.LogInformation("Sending password reset email to {Email} (Original: {OriginalEmail}) for user {FullName}",
                    finalToEmail, toEmail, fullName);

                await SendEmailAsync(finalToEmail, subject, body);

                _logger.LogInformation("Password reset email sent successfully to {Email} for user {FullName}", finalToEmail, fullName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email} for user {FullName}", toEmail, fullName);
                throw;
            }
        }

        public async Task SendPasswordChangeConfirmationEmailAsync(string toEmail, string fullName)
        {
            try
            {
                var subject = "Smart Feedback System - Password Changed Successfully";
                var body = GeneratePasswordChangeConfirmationEmailBody(fullName);

                // Use test email in development if configured
                var finalToEmail = _environment.IsDevelopment() && _emailSettings.UseTestEmail && !string.IsNullOrEmpty(_emailSettings.DefaultTestEmail)
                    ? _emailSettings.DefaultTestEmail
                    : toEmail;

                _logger.LogInformation("Sending password change confirmation email to {Email} (Original: {OriginalEmail}) for user {FullName}",
                    finalToEmail, toEmail, fullName);

                await SendEmailAsync(finalToEmail, subject, body);

                _logger.LogInformation("Password change confirmation email sent successfully to {Email} for user {FullName}", finalToEmail, fullName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password change confirmation email to {Email} for user {FullName}", toEmail, fullName);
                throw;
            }
        }

        public async Task SendAssessmentStatusChangeEmailAsync(string toEmail, string fullName, string assessmentName, string courseCode, string courseName, string oldStatus, string newStatus)
        {
            try
            {
                var subject = $"Assessment Status Update - {assessmentName}";
                var body = GenerateAssessmentStatusChangeEmailBody(fullName, assessmentName, courseCode, courseName, oldStatus, newStatus);

                // Use test email in development if configured
                var finalToEmail = _environment.IsDevelopment() && _emailSettings.UseTestEmail && !string.IsNullOrEmpty(_emailSettings.DefaultTestEmail)
                    ? _emailSettings.DefaultTestEmail
                    : toEmail;

                _logger.LogInformation("Sending assessment status change email to {Email} (Original: {OriginalEmail}) for {AssessmentName}: {OldStatus} -> {NewStatus}",
                    finalToEmail, toEmail, assessmentName, oldStatus, newStatus);

                await SendEmailAsync(finalToEmail, subject, body);

                _logger.LogInformation("Assessment status change email sent successfully to {Email} for {AssessmentName}", finalToEmail, assessmentName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send assessment status change email to {Email} for {AssessmentName}", toEmail, assessmentName);
                throw;
            }
        }

        // In UserEmailService.cs, add more detailed error logging in SendEmailAsync method
        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = body };
                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    // Add timeout and logging
                    client.Timeout = 30000; // 30 seconds timeout

                    _logger.LogInformation("Connecting to SMTP server {Server}:{Port}",
                        _emailSettings.SmtpServer, _emailSettings.SmtpPort);

                    await client.ConnectAsync(
                        _emailSettings.SmtpServer,
                        _emailSettings.SmtpPort,
                        SecureSocketOptions.StartTls
                    );

                    _logger.LogInformation("Authenticating with SMTP server using {Email}",
                        _emailSettings.SenderEmail);

                    await client.AuthenticateAsync(
                        _emailSettings.SenderEmail,
                        _emailSettings.SenderPassword
                    );

                    _logger.LogInformation("Sending email to {Recipient}", toEmail);
                    await client.SendAsync(message);

                    await client.DisconnectAsync(true);

                    _logger.LogInformation("Email sent successfully to {Recipient}", toEmail);
                }
            }
            catch (AuthenticationException authEx)
            {
                _logger.LogError(authEx, "SMTP Authentication failed for {Email}", _emailSettings.SenderEmail);
                throw new InvalidOperationException($"Email authentication failed: {authEx.Message}", authEx);
            }
            catch (SmtpCommandException smtpEx)
            {
                _logger.LogError(smtpEx, "SMTP command error. Status: {StatusCode}", smtpEx.StatusCode);
                throw new InvalidOperationException($"SMTP error: {smtpEx.Message}", smtpEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General email sending error");
                throw new InvalidOperationException($"Error sending email: {ex.Message}", ex);
            }
        }
        

        private string GenerateWelcomeEmailBody(string fullName, string email, string tempPassword)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f8f9fa; padding: 30px; border-radius: 0 0 5px 5px; }}
        .credentials {{ background-color: #e9ecef; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .warning {{ color: #dc3545; font-weight: bold; }}
        .footer {{ margin-top: 30px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to Smart Feedback System</h1>
        </div>
        <div class='content'>
            <h2>Hello {fullName},</h2>
            
            <p>Your account has been successfully created in the Smart Feedback System. Below are your login credentials:</p>
            
            <div class='credentials'>
                <strong>Email:</strong> {email}<br>
                <strong>Temporary Password:</strong> {tempPassword}
            </div>
            
            <p class='warning'>⚠️ Important Security Notice:</p>
            <ul>
                <li>You will be required to change your password on first login</li>
                <li>Please keep your credentials secure and do not share them</li>
                <li>If you did not request this account, please contact the administrator</li>
            </ul>
            
            <p>You can now log in to the system and start using the feedback platform.</p>
            
            <p>Best regards,<br>Smart Feedback System Team</p>
        </div>
        <div class='footer'>
            This is an automated message. Please do not reply to this email.
        </div>
    </div>
</body>
</html>";
        }

        private string GeneratePasswordResetEmailBody(string fullName, string tempPassword)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f8f9fa; padding: 30px; border-radius: 0 0 5px 5px; }}
        .credentials {{ background-color: #e9ecef; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .warning {{ color: #dc3545; font-weight: bold; }}
        .footer {{ margin-top: 30px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Password Reset - Smart Feedback System</h1>
        </div>
        <div class='content'>
            <h2>Hello {fullName},</h2>
            
            <p>Your password has been reset by an administrator. Below is your new temporary password:</p>
            
            <div class='credentials'>
                <strong>New Temporary Password:</strong> {tempPassword}
            </div>
            
            <p class='warning'>⚠️ Important Security Notice:</p>
            <ul>
                <li>You will be required to change this password on next login</li>
                <li>Please keep your credentials secure and do not share them</li>
                <li>If you did not request this password reset, please contact the administrator immediately</li>
            </ul>
            
            <p>Please log in with this temporary password and change it immediately.</p>
            
            <p>Best regards,<br>Smart Feedback System Team</p>
        </div>
        <div class='footer'>
            This is an automated message. Please do not reply to this email.
        </div>
    </div>
</body>
</html>";
        }

        private string GeneratePasswordChangeConfirmationEmailBody(string fullName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f8f9fa; padding: 30px; border-radius: 0 0 5px 5px; }}
        .info-box {{ background-color: #d4edda; border-left: 4px solid #28a745; padding: 15px; margin: 20px 0; }}
        .warning {{ color: #dc3545; font-weight: bold; }}
        .footer {{ margin-top: 30px; font-size: 12px; color: #666; text-align: center; }}
        .timestamp {{ background-color: #e9ecef; padding: 10px; border-radius: 5px; margin: 20px 0; font-size: 14px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✓ Password Changed Successfully</h1>
        </div>
        <div class='content'>
            <h2>Hello {fullName},</h2>
            
            <div class='info-box'>
                <p><strong>Your password has been changed successfully.</strong></p>
            </div>
            
            <div class='timestamp'>
                <strong>Change Date & Time:</strong> {DateTime.Now:MMMM dd, yyyy 'at' HH:mm}
            </div>
            
            <p>This email confirms that your Smart Feedback System account password was recently changed.</p>
            
            <p class='warning'>⚠️ Security Alert:</p>
            <ul>
                <li>If you made this change, no further action is required</li>
                <li>If you did NOT change your password, please contact the administrator immediately</li>
                <li>Your account may have been compromised - take action to secure it</li>
            </ul>
            
            <p><strong>What to do if you didn't make this change:</strong></p>
            <ol>
                <li>Contact your system administrator immediately</li>
                <li>Request a password reset from the login page</li>
                <li>Review your account activity for any suspicious actions</li>
            </ol>
            
            <p>Best regards,<br>Smart Feedback System Team</p>
        </div>
        <div class='footer'>
            This is an automated security notification. Please do not reply to this email.<br>
            If you have concerns about your account security, contact your administrator.
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateAssessmentStatusChangeEmailBody(string fullName, string assessmentName, string courseCode, string courseName, string oldStatus, string newStatus)
        {
            var actionRequired = newStatus == "Moderation" ? "Please review and moderate the assessment." : "Please review the moderated assessment.";
            var statusColor = newStatus == "Moderation" ? "#ffc107" : "#28a745";
            
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: {statusColor}; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f8f9fa; padding: 30px; border-radius: 0 0 5px 5px; }}
        .info-box {{ background-color: #e9ecef; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .info-box strong {{ color: #495057; }}
        .status-change {{ background-color: #fff3cd; border-left: 4px solid {statusColor}; padding: 15px; margin: 20px 0; }}
        .action-required {{ color: #dc3545; font-weight: bold; font-size: 16px; }}
        .footer {{ margin-top: 30px; font-size: 12px; color: #666; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📋 Assessment Status Update</h1>
        </div>
        <div class='content'>
            <h2>Hello {fullName},</h2>
            
            <p>An assessment has been updated and requires your attention.</p>
            
            <div class='info-box'>
                <strong>Assessment:</strong> {assessmentName}<br>
                <strong>Course:</strong> {courseCode} - {courseName}<br>
                <strong>Previous Status:</strong> {oldStatus}<br>
                <strong>New Status:</strong> <span style='color: {statusColor}; font-weight: bold;'>{newStatus}</span>
            </div>
            
            <div class='status-change'>
                <p class='action-required'>⚠️ Action Required</p>
                <p>{actionRequired}</p>
            </div>
            
            <p>Please log in to the Smart Feedback System to review this assessment:</p>
            <ul>
                <li>Navigate to your dashboard</li>
                <li>Find the course: {courseCode}</li>
                <li>Access the assessment: {assessmentName}</li>
            </ul>
            
            <p>If you have any questions, please contact the course administrator.</p>
            
            <p>Best regards,<br>Smart Feedback System Team</p>
        </div>
        <div class='footer'>
            This is an automated message. Please do not reply to this email.<br>
            If you believe you received this email in error, please contact support.
        </div>
    </div>
</body>
</html>";
        }
    }
}
