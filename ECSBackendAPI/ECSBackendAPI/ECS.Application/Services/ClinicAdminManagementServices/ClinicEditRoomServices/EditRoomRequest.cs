using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicEditRoomServices
{
    /// <summary>
    /// Request object containing parameters required to update an existing facility room configuration.
    /// </summary>
    public class EditRoomRequest
    {
        /// <summary>
        /// Gets or sets the unique identity tracking key of the targeted room entity.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// Gets or sets the updated descriptive name representing the facility room.
        /// </summary>
        public string RoomName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the functional operational type classifier metadata string.
        /// </summary>
        public string? RoomType { get; set; }
    }
}
