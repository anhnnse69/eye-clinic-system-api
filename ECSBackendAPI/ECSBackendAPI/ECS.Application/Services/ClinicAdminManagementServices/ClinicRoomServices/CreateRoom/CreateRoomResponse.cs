using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicRoomServices.CreateRoom
{
    /// <summary>
    /// Response data contract containing structural details of the newly created room.
    /// </summary>
    public class CreateRoomResponse
    {
        /// <summary>
        /// Gets or sets the primary identity token identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the unique corporate clinic environment reference link key.
        /// </summary>
        public Guid ClinicId { get; set; }

        /// <summary>
        /// Gets or sets the registered nomenclature designation name.
        /// </summary>
        public string RoomName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the contextual specialized utility definition type.
        /// </summary>
        public string? RoomType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the facility unit is operational.
        /// </summary>
        public bool IsActive { get; set; }
    }
}
