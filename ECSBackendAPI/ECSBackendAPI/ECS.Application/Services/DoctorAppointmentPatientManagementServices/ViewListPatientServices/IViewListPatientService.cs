using ECS.Application.Common.Response;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewListPatientServices
{
    /// <summary>
    /// Defines the contract for viewing a doctor's patient list.
    /// </summary>
    public interface IViewListPatientService
    {
        /// <summary>
        /// Returns the paginated list of patients
        /// who booked an appointment with the doctor.
        /// </summary>
        Task<ApiResponse<ViewListPatientResponse>> Process(
            Guid doctorId,
            ViewListPatientRequest request);
    }
}