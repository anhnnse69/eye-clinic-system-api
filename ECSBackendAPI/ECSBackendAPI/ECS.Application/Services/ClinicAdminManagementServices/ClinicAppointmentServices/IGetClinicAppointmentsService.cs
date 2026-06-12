using System;
using System.Collections.Generic;
using System.Text;
using ECS.Application.Common.Response;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicAppointmentServices
{
    /// <summary>
    /// Service interface for retrieving appointments dataset for Clinic Admin.
    /// </summary>
    public interface IGetClinicAppointmentsService
    {
        /// <summary>
        /// Processes the appointment retrieval request.
        /// </summary>
        /// <param name="request">The pagination and filtering request criteria.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the list of formatted appointment details and metadata.</returns>
        Task<ApiResponse<List<GetClinicAppointmentResponse>>> Process(GetClinicAppointmentsRequest request);
    }
}
