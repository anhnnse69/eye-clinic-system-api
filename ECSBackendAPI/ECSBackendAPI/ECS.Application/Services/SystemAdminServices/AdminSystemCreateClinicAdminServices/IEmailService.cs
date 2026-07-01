using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.SystemAdminServices.AdminSystemCreateClinicAdminServices
{
    /// <summary>
    /// Defines communication contracts for infrastructure messaging and notification dispatches.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Asynchronously dispatches an email notification message to a designated target address.
        /// </summary>
        /// <param name="toEmail">The primary recipient communication endpoint address.</param>
        /// <param name="subject">The descriptive title or intent classification tracker header.</param>
        /// <param name="body">The structural core payload or message layout block definition.</param>
        /// <returns>A tracking state parameter detailing execution operational outcomes.</returns>
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
