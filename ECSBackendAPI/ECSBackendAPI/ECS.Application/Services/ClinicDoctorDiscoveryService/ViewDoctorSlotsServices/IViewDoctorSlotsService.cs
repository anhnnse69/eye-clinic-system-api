using ECS.Application.Common.Response;

namespace ECS.Application.Services.ClinicDoctorDiscoveryService.ViewDoctorSlotsServices
{
    /// <summary>
    /// Defines the contract for viewing doctor available slots.
    /// </summary>
    public interface IViewDoctorSlotsService
    {
        /// <summary>
        /// Returns doctor info and available slots
        /// from today onward for the next 30 days.
        /// </summary>
        Task<ApiResponse<ViewDoctorSlotsResponse>> Process(Guid doctorId);
    }
}
