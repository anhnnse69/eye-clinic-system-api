using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicRoomServices.CreateRoom
{
    /// <summary>
    /// Request data contract for creating a new facility room.
    /// </summary>
    public class CreateRoomRequest
    {
        /// <summary>
        /// Gets or sets the unique display name of the room.
        /// </summary>
        public string RoomName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the functional classification or type of the room.
        /// </summary>
        public string? RoomType { get; set; }
    }
}
