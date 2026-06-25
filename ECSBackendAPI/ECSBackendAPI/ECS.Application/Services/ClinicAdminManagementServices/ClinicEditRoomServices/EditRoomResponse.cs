using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicEditRoomServices
{
    /// <summary>
    /// Response schema containing updated domain state properties representing a physical room context.
    /// </summary>
    public class EditRoomResponse
    {
        /// <summary>
        /// Gets or sets the unique transaction identifier value.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the referenced location clinic owner node framework identifier.
        /// </summary>
        public Guid ClinicId { get; set; }

        /// <summary>
        /// Gets or sets the system display title mapped directly against database records.
        /// </summary>
        public string RoomName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the specific department classification context details.
        /// </summary>
        public string? RoomType { get; set; }

        /// <summary>
        /// Gets or sets a status validation metric indicator flag.
        /// </summary>
        public bool IsActive { get; set; }
    }
}
