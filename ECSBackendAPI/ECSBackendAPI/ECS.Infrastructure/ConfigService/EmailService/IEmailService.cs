namespace ECS.Infrastructure.ConfigService.EmailService
{
    /// <summary>
    /// Interface for email sending operations.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email with the specified parameters.
        /// </summary>
        /// <param name="to">Recipient email address.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="body">Email body content.</param>
        /// <returns>True if email was sent successfully; otherwise false.</returns>
        Task<bool> SendEmailAsync(string to, string subject, string body);

        /// <summary>
        /// Builds HTML email body for password reset OTP.
        /// </summary>
        /// <param name="otp">The OTP code to include in the email.</param>
        /// <returns>HTML string for the email body.</returns>
        string BuildPasswordResetEmailBody(string otp);
    }
}
