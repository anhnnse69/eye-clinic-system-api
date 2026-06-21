using ECS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.SystemAdminServices.AccountServices.Create
{
    /// <summary>
    /// Request object containing data parameters required to create a new user account.
    /// </summary>
    public class CreateAccountRequest
    {
        public string Phone { get; set; } = null!;
        public string? Email { get; set; }
        public string Password { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public UserRole Role { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
