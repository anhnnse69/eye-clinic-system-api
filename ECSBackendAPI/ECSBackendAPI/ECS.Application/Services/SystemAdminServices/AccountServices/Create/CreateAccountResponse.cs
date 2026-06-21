using ECS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.SystemAdminServices.AccountServices.Create
{
    /// <summary>
    /// Response object enclosing the newly created user account attributes.
    /// </summary>
    public class CreateAccountResponse
    {
        public Guid Id { get; set; }
        public string Phone { get; set; } = null!;
        public string? Email { get; set; }
        public string FullName { get; set; } = null!;
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
        public string? AvatarUrl { get; set; }
        public string CreatedAt { get; set; } = null!;
    }
}
