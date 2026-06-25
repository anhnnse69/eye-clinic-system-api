using ECS.Domain.Enums;

namespace ECS.Application.Services.ClinicAdminManagementServices.ViewListStaffAccountsServices
{
    /// <summary>
    /// Response object containing brief profile info for a staff member.
    /// </summary>
    public class StaffAccountResponse
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}