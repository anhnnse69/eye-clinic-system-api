using System;

namespace ECS.Application.Services.ClinicAdminManagementServices.EditStaffAccountServices
{
    /// <summary>
    /// Response data entity conveying confirmation details of the successfully modified staff member.
    /// </summary>
    public class EditStaffResponse
    {
        /// <summary>
        /// Gets or sets the modified user identification token primary index key.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the updated phone network communication pathway metadata.
        /// </summary>
        public string Phone { get; set; } = null!;

        /// <summary>
        /// Gets or sets the validated identity electronic mail transmission pathway address.
        /// </summary>
        public string Email { get; set; } = null!;

        /// <summary>
        /// Gets or sets the modified official full name representation identity metadata.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the updated active lifecycle tracking state flag.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the updated operational role text context descriptor inside the specific clinic domain.
        /// </summary>
        public string UpdatedRole { get; set; } = null!;
    }
}