using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicRoomServices.ViewListRoomServices
{
    /// <summary>
    /// Request criteria model representing filtration, searching, and pagination constraints for clinic rooms.
    /// </summary>
    public class GetClinicRoomsRequest
    {
        /// <summary>
        /// Optional query string to search across room names.
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Optional classification string to filter rooms by physical/operational type.
        /// </summary>
        public string? RoomType { get; set; }

        /// <summary>
        /// Optional state filter to isolate active or inactive facility assets.
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Page sequence positional index indicator.
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Total capacity ceiling bounds segment allocation per page.
        /// </summary>
        public int PageSize { get; set; } = 10;
    }
}
