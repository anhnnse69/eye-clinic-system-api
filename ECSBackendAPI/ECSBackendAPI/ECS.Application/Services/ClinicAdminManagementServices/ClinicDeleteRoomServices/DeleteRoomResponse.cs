using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicDeleteRoomServices
{
    /// <summary>
    /// Response payload mirroring the changed state attributes of the modified room context.
    /// </summary>
    public class DeleteRoomResponse
    {
        /// <summary>
        /// Gets or sets the room unique database tracking signature index key.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the parent clinic environment identification reference link.
        /// </summary>
        public Guid ClinicId { get; set; }

        /// <summary>
        /// Gets or sets the validated literal designation text containing the room name.
        /// </summary>
        public string RoomName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the functional specification attribute configuration mapping the room node.
        /// </summary>
        public string? RoomType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the facility room node remains active inside active entity streams.
        /// </summary>
        public bool IsActive { get; set; }
    }
}
