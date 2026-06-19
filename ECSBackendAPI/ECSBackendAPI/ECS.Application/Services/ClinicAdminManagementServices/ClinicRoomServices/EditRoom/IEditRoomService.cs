using ECS.Application.Common.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicRoomServices.EditRoom
{
    /// <summary>
    /// Service contract defining updates and configuration operations for clinic facility rooms.
    /// </summary>
    public interface IEditRoomService
    {
        /// <summary>
        /// Executes the modification engine lifecycle to rewrite core attributes of a target facility room.
        /// </summary>
        /// <param name="request">The structural model criteria details wrapping the requested changes.</param>
        /// <returns>A unified standard envelope tracking result response context block.</returns>
        Task<ApiResponse<EditRoomResponse>> Process(EditRoomRequest request);
    }
}
