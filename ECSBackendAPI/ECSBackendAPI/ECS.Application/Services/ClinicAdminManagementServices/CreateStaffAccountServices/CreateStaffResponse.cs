using ECS.Domain.Enums;

namespace ECS.Application.Services.ClinicAdminManagementServices.CreateStaffAccountServices
{
    /// <summary>
    /// Response data entity conveying confirmation details of the provisioned staff member.
    /// </summary>
    public class CreateStaffResponse
    {
        /// <summary>
        /// Gets or sets the globally unique system user identifier token.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the absolute unique staff-to-clinic assignment tracking identifier.
        /// </summary>
        public Guid StaffClinicId { get; set; }

        /// <summary>
        /// Gets or sets the verified phone registration indicator.
        /// </summary>
        public string Phone { get; set; } = null!;

        /// <summary>
        /// Gets or sets the assigned functional profile designation.
        /// </summary>
        public string AssignedRole { get; set; } = null!;
    }
}
