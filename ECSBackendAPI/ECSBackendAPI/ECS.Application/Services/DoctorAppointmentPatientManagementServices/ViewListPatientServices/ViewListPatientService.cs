using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewListPatientServices
{
    /// <summary>
    /// Handles retrieving the paginated patient list
    /// for a given doctor.
    /// </summary>
    public class ViewPatientListService
        : IViewListPatientService
    {
        private readonly IRepositoryQueryBase<
            DoctorProfile,
            Guid,
            AppDbContext> _doctorRepository;

        private readonly IRepositoryQueryBase<
            Appointment,
            Guid,
            AppDbContext> _appointmentRepository;

        /// <summary>
        /// Initializes a new instance of
        /// <see cref="ViewPatientListService"/>.
        /// </summary>
        public ViewPatientListService(
            IRepositoryQueryBase<
                DoctorProfile,
                Guid,
                AppDbContext> doctorRepository,
            IRepositoryQueryBase<
                Appointment,
                Guid,
                AppDbContext> appointmentRepository)
        {
            _doctorRepository = doctorRepository;
            _appointmentRepository = appointmentRepository;
        }

        /// <summary>
        /// Retrieves paginated patients who booked
        /// appointments with the specified doctor,
        /// identified by the related user account id.
        /// </summary>
        /// <param name="userId">
        /// Identifier of the user account linked to the doctor profile.
        /// </param>
        /// <param name="request">
        /// Pagination and filter parameters.
        /// </param>
        /// <returns>
        /// A successful response containing the paginated
        /// patient list.
        /// </returns>
        public async Task<ApiResponse<ViewListPatientResponse>> Process(
            Guid userId,
            ViewListPatientRequest request)
        {
            var doctorProfile = await ResolveActiveDoctorProfileAsync(userId);

            var (pageNumber, pageSize) = NormalizePaging(request);

            var totalRecords = await CountPatientsAsync(
                doctorProfile.Id,
                request.Status);

            var totalPages = CalculateTotalPages(
                totalRecords,
                pageSize);

            var patients = await FetchPatientsAsync(
                doctorProfile.Id,
                request.Status,
                pageNumber,
                pageSize);

            var response = BuildResponse(
                patients,
                pageNumber,
                pageSize,
                totalPages,
                totalRecords);

            return CreateSuccessResponse(response);
        }

        /// <summary>
        /// Resolves the active doctor profile for the specified user.
        /// Throws when not found.
        /// </summary>
        /// <param name="userId">
        /// Identifier of the user account.
        /// </param>
        /// <returns>
        /// The resolved <see cref="DoctorProfile"/>.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the doctor cannot be found.
        /// </exception>
        private async Task<DoctorProfile> ResolveActiveDoctorProfileAsync(
            Guid userId)
        {
            var doctorProfile = await _doctorRepository
                .FindByCondition(d =>
                    d.UserId == userId &&
                    d.IsActive)
                .FirstOrDefaultAsync();

            if (doctorProfile is null)
                throw new KeyNotFoundException(
                    GeneralCode.APP_MESSAGE_4011.ToString());

            return doctorProfile;
        }

        /// <summary>
        /// Normalizes pagination parameters.
        /// </summary>
        private static (int PageNumber, int PageSize) NormalizePaging(
            ViewListPatientRequest request)
        {
            var pageNumber = request.PageNumber < 1
                ? 1
                : request.PageNumber;
            var pageSize = request.PageSize < 1
                ? 10
                : request.PageSize;
            return (pageNumber, pageSize);
        }

        /// <summary>
        /// Counts total unique patients for the doctor,
        /// optionally filtered by status.
        /// </summary>
        private async Task<int> CountPatientsAsync(
            Guid doctorId,
            AppointmentStatus? status)
        {
            return await _appointmentRepository
                .FindByCondition(a =>
                    a.DoctorId == doctorId &&
                    (status == null || a.Status == status))
                .Select(a => a.PatientId)
                .Distinct()
                .CountAsync();
        }

        /// <summary>
        /// Calculates the total number of pages.
        /// </summary>
        private static int CalculateTotalPages(
            int totalRecords,
            int pageSize)
        {
            return pageSize == 0
                ? 0
                : (int)Math.Ceiling(
                    (double)totalRecords / pageSize);
        }

        /// <summary>
        /// Retrieves a page of distinct patients for the doctor,
        /// showing each patient once with their latest appointment.
        /// </summary>
        private async Task<List<PatientAppointmentItem>>
            FetchPatientsAsync(
                Guid doctorId,
                AppointmentStatus? status,
                int pageNumber,
                int pageSize)
        {
            var baseQuery = _appointmentRepository
                .FindByCondition(a =>
                    a.DoctorId == doctorId &&
                    (status == null || a.Status == status));

            var pagedPatientIds = await baseQuery
                .GroupBy(a => a.PatientId)
                .Select(g => new
                {
                    PatientId = g.Key,
                    LatestDate = g.Max(a => a.AppointmentDate)
                })
                .OrderByDescending(x => x.LatestDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.PatientId)
                .ToListAsync();

            var result = new List<PatientAppointmentItem>();

            foreach (var pId in pagedPatientIds)
            {
                var latestApp = await _appointmentRepository
                    .FindByCondition(a =>
                        a.DoctorId == doctorId &&
                        a.PatientId == pId &&
                        (status == null || a.Status == status))
                    .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                    .OrderByDescending(a => a.AppointmentDate)
                    .FirstOrDefaultAsync();

                if (latestApp != null)
                {
                    result.Add(new PatientAppointmentItem
                    {
                        AppointmentId = latestApp.Id,
                        PatientId = latestApp.Patient.Id,
                        PatientName = latestApp.Patient.FullName,
                        PatientAvatarUrl = latestApp.Patient.User != null
                            ? latestApp.Patient.User.AvatarUrl
                            : null,
                        PatientPhone = latestApp.Patient.PhoneNumber,
                        AppointmentDate = latestApp.AppointmentDate,
                        Status = latestApp.Status.ToString(),
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Builds the patient list response.
        /// </summary>
        private static ViewListPatientResponse BuildResponse(
            List<PatientAppointmentItem> patients,
            int pageNumber,
            int pageSize,
            int totalPages,
            int totalRecords)
        {
            return new ViewListPatientResponse
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                Patients = patients
            };
        }

        /// <summary>
        /// Creates a successful API response.
        /// </summary>
        private static ApiResponse<ViewListPatientResponse>
            CreateSuccessResponse(
                ViewListPatientResponse response)
        {
            return ApiResponse<ViewListPatientResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                response);
        }
    }
}
