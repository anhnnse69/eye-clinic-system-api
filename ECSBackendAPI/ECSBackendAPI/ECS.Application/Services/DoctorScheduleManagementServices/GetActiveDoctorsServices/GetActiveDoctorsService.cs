using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorScheduleManagementServices.GetActiveDoctorsServices
{
    /// <summary>
    /// Service responsible for retrieving a list of active doctors filtered strictly by the performing receptionist's clinic boundary.
    /// Primarily utilized to populate doctor selection pickers or dropdown components during schedule creation.
    /// </summary>
    public class GetActiveDoctorsService : IGetActiveDoctorsService
    {
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepo;
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> _doctorRepo;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetActiveDoctorsService"/> class.
        /// </summary>
        /// <param name="staffClinicRepo">Repository interface to verify receptionist identities and clinic mapping boundaries.</param>
        /// <param name="doctorRepo">Repository interface to query and filter active doctor profiles.</param>
        public GetActiveDoctorsService(
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepo,
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepo)
        {
            _staffClinicRepo = staffClinicRepo;
            _doctorRepo = doctorRepo;
        }

        /// <summary>
        /// Processes the request to look up all active doctors belonging to the receptionist's assigned clinic context.
        /// </summary>
        /// <param name="receptionistUserId">The unique identifier of the performing receptionist user.</param>
        /// <returns>An API standard template response wrapping the list of structured doctor option payloads.</returns>
        public async Task<ApiResponse<List<DoctorOptionResponse>>> Process(Guid receptionistUserId)
        {
            var clinicId = await ResolveReceptionistClinicIdAsync(receptionistUserId);
            var doctors = await QueryActiveDoctorsByClinicAsync(clinicId);

            return CreateSuccessResponse(doctors);
        }

        /// <summary>
        /// Resolves the clinic identifier associated with the active receptionist user.
        /// </summary>
        /// <param name="receptionistUserId">The unique identifier of the receptionist user.</param>
        /// <returns>The clinic identifier mapped to the specified receptionist.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the receptionist is not found or is inactive.</exception>
        private async Task<Guid> ResolveReceptionistClinicIdAsync(Guid receptionistUserId)
        {
            var staffClinic = await _staffClinicRepo
                .FindByCondition(sc => sc.UserId == receptionistUserId && sc.IsActive)
                .FirstOrDefaultAsync();

            if (staffClinic is null)
                throw new KeyNotFoundException(GeneralCode.APP_MESSAGE_4008.ToString());

            return staffClinic.ClinicId;
        }

        /// <summary>
        /// Queries and maps active doctor profiles within a specific clinic, sorted alphabetically by full name.
        /// </summary>
        /// <param name="clinicId">The unique identifier of the clinic target context.</param>
        /// <returns>A read-only list of mapped <see cref="DoctorOptionResponse"/> objects.</returns>
        private async Task<List<DoctorOptionResponse>> QueryActiveDoctorsByClinicAsync(Guid clinicId)
        {
            return await _doctorRepo
                .FindByCondition(d => d.ClinicId == clinicId && d.IsActive)
                .Select(d => new DoctorOptionResponse
                {
                    DoctorId = d.Id,
                    FullName = d.User.FullName,
                    Specialty = d.Specialty != null ? d.Specialty.Name : null,
                    AvatarUrl = d.User.AvatarUrl,
                    IsActive = d.IsActive
                })
                .OrderBy(d => d.FullName)
                .ToListAsync();
        }

        /// <summary>
        /// Wraps the retrieved doctor list into a standardized API success template response wrapper.
        /// </summary>
        /// <param name="doctors">The list of mapped doctor option responses payload.</param>
        /// <returns>The API standard outcome object encapsulating the collection response context.</returns>
        private static ApiResponse<List<DoctorOptionResponse>> CreateSuccessResponse(List<DoctorOptionResponse> doctors)
        {
            return ApiResponse<List<DoctorOptionResponse>>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                doctors);
        }
    }
}