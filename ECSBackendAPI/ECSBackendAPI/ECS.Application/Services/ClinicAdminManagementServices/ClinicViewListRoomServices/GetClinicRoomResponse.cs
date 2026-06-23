using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicViewListRoomServices
{
    /// <summary>
    /// Data transport structure exposing specific attributes of a facility room asset.
    /// </summary>
    public class GetClinicRoomResponse
    {
        /// <summary>
        /// Serialized database identifier token coordinates.
        /// </summary>
        public string Id_room { get; set; } = null!;

        /// <summary>
        /// Human-readable label designated to the physical room.
        /// </summary>
        public string RoomName { get; set; } = null!;

        /// <summary>
        /// Structural functional description taxonomy of the venue space.
        /// </summary>
        public string RoomType { get; set; } = null!;

        /// <summary>
        /// Operational presence visibility indicator tracking active readiness.
        /// </summary>
        public bool IsActive { get; set; }
    }
}
