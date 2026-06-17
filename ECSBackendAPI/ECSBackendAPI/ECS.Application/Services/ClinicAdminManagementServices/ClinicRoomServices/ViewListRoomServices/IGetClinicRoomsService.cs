using ECS.Application.Common.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicRoomServices.ViewListRoomServices
{
    /// <summary>
    /// Service contract defining retrieval and search operations for clinic room definitions.
    /// </summary>
    public interface IGetClinicRoomsService
    {
        /// <summary>
        /// Processes the internal application pipelines to resolve administrator contexts and page targeted datasets.
        /// </summary>
        /// <param name="request">The parameters containing search keywords, filters, and custom paging indices.</param>
        /// <returns>A standard encapsulated response layout wrapping the mapped matching results collection.</returns>
        Task<ApiResponse<List<GetClinicRoomResponse>>> Process(GetClinicRoomsRequest request);
    }
}
