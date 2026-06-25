using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicDeleteRoomServices
{
    /// <summary>
    /// Request object containing parameters needed to toggle the operational or soft-deleted status of a clinic room.
    /// </summary>
    public class DeleteRoomRequest
    {
        /// <summary>
        /// Gets or sets the unique tracking identifier of the targeted facility room.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the targeted active configuration status state.
        /// Set to <c>false</c> for soft-deleting/blocking, and <c>true</c> for reversing/unblocking.
        /// </summary>
        public bool IsActive { get; set; }
    }
}
