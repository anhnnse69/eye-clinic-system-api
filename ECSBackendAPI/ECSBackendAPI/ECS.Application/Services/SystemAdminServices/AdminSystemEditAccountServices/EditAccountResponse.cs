using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.SystemAdminServices.AdminSystemEditAccountServices
{
    /// <summary>
    /// Response payload envelope returning updated account details post persistence operations.
    /// </summary>
    public class EditAccountResponse
    {
        public Guid Id { get; set; }
        public string Phone { get; set; } = null!;
        public string? Email { get; set; }
        public string FullName { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string UpdatedAt { get; set; } = null!;
    }
}
