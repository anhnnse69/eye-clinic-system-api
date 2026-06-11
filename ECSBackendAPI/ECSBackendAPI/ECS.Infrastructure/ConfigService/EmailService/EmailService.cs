using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;

namespace ECS.Infrastructure.ConfigService.EmailService
{
    /// <summary>
    /// Email service implementation using SMTP.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("Smtp");
                var emailConfig = smtpSettings.Get<SmtpConfiguration>();

                if (emailConfig == null || string.IsNullOrEmpty(emailConfig.Host))
                {
                    _logger.LogError("SMTP configuration is missing or invalid");
                    return false;
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(
                    emailConfig.SenderName ?? "ECS Medical",
                    emailConfig.SenderEmail));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;

                var builder = new BodyBuilder
                {
                    HtmlBody = body,
                    TextBody = "This email requires an HTML-compatible email client."
                };

                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();

                await client.ConnectAsync(
                    emailConfig.Host,
                    emailConfig.Port,
                    emailConfig.UseSsl ? MailKit.Security.SecureSocketOptions.StartTls : MailKit.Security.SecureSocketOptions.None);

                if (!string.IsNullOrEmpty(emailConfig.Username))
                {
                    await client.AuthenticateAsync(emailConfig.Username, emailConfig.Password);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {Recipient}", to);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient}", to);
                return false;
            }
        }

        /// <summary>
        /// Builds HTML email body for password reset OTP.
        /// </summary>
        /// <param name="otp">The OTP code to include in the email.</param>
        /// <returns>HTML string for the email body.</returns>
        public string BuildPasswordResetEmailBody(string otp)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; }}
        .header h1 {{ color: #ffffff; margin: 0; font-size: 24px; }}
        .content {{ padding: 40px 30px; text-align: center; }}
        .otp-code {{ font-size: 36px; font-weight: bold; color: #667eea; letter-spacing: 10px; margin: 30px 0; }}
        .message {{ color: #666666; font-size: 14px; line-height: 1.6; }}
        .warning {{ color: #ff6b6b; font-size: 12px; margin-top: 20px; }}
        .footer {{ background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #999999; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>ECS Medical</h1>
        </div>
        <div class='content'>
            <h2 style='color: #333; margin-bottom: 20px;'>Password Reset Request</h2>
            <p class='message'>You have requested to reset your password. Please use the following OTP code:</p>
            <div class='otp-code'>{otp}</div>
            <p class='message'>This OTP will expire in <strong>5 minutes</strong>.</p>
            <p class='warning'>If you did not request this, please ignore this email.</p>
        </div>
        <div class='footer'>
            <p>ECS Medical Clinic Management System</p>
        </div>
    </div>
</body>
</html>";
        }
    }

    public class SmtpConfiguration
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool UseSsl { get; set; } = true;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = "ECS Medical";
    }
}
