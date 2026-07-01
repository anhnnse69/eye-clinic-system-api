using ECS.Application.Services.SystemAdminServices.AdminSystemCreateClinicAdminServices;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace ECS.Application.Services.SystemAdminServices.AdminSystemCreateClinicAdminServices
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public SmtpEmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var host = _configuration["Smtp:Host"];
            var port = int.Parse(_configuration["Smtp:Port"] ?? "587");
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];
            var senderEmail = _configuration["Smtp:SenderEmail"];
            var senderName = _configuration["Smtp:SenderName"];
            var useSsl = bool.Parse(_configuration["Smtp:UseSsl"] ?? "true");

            using (var client = new SmtpClient(host, port))
            {
                client.Credentials = new NetworkCredential(username, password);
                client.EnableSsl = useSsl;

                string fromAddress = !string.IsNullOrEmpty(senderEmail) ? senderEmail : username!;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromAddress, senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);
                await client.SendMailAsync(mailMessage);
            }
        }
    }
}
