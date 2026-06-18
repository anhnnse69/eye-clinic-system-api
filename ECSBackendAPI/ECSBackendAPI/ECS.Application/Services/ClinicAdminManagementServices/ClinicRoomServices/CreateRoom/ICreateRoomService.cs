using ECS.Application.Common.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicRoomServices.CreateRoom
{
    /// <summary>
    /// Service contract defining core operational orchestration workflows for creating clinic facility rooms.
    /// </summary>
    public interface ICreateRoomService
    {
        /// <summary>
        /// Orchestrates runtime execution pipelines to insert a validated facility room into data layers.
        /// </summary>
        /// <param name="request">The specialized request message details package.</param>
        /// <returns>A structured envelope context tracking execution outputs status blocks.</returns>
        Task<ApiResponse<CreateRoomResponse>> Process(CreateRoomRequest request);
    }
}
