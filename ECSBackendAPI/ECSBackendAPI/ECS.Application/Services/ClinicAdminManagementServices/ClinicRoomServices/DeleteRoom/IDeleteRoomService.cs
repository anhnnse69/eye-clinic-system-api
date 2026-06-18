using ECS.Application.Common.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicRoomServices.DeleteRoom
{
    /// <summary>
    /// Service contract defining soft deletion and block toggling workflows for clinic facility rooms.
    /// </summary>
    public interface IDeleteRoomService
    {
        /// <summary>
        /// Executes the application workflow pipeline to safely mutate or soft-delete room persistence indicators.
        /// </summary>
        /// <param name="request">The structural criteria data packet carrying modification parameter indexes.</param>
        /// <returns>A unified standard envelope payload indicating transaction outcome metrics results.</returns>
        Task<ApiResponse<DeleteRoomResponse>> Process(DeleteRoomRequest request);
    }
}
