using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewDoctorAppointmentsServices
{
    /// <summary>
    /// Handles retrieving paginated doctor appointments with full details 
    /// including patient information, service, slot, and medical record data.
    /// Supports filtering by status, date, and search keyword.
    /// </summary>
    public class ViewDoctorAppointmentsService : IViewDoctorAppointmentsService
    {
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> _doctorRepository;
        private readonly IRepositoryQueryBase<Appointment, Guid, AppDbContext> _appointmentRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewDoctorAppointmentsService"/>.
        /// </summary>
        public ViewDoctorAppointmentsService(
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepository,
            IRepositoryQueryBase<Appointment, Guid, AppDbContext> appointmentRepository)
        {
            _doctorRepository = doctorRepository;
            _appointmentRepository = appointmentRepository;
        }

        /// <summary>
        /// Retrieves paginated appointments for a doctor with support for filtering and searching.
        /// </summary>
        /// <param name="userId">Identifier of the user account linked to the doctor profile.</param>
        /// <param name="request">The request containing pagination and filter parameters.</param>
        /// <returns>A successful response containing the paginated list of appointments.</returns>
        public async Task<ApiResponse<ViewDoctorAppointmentsResponse>> Process(
            Guid userId,
            ViewDoctorAppointmentsRequest request)
        {
            var doctorProfile = await ResolveActiveDoctorProfileAsync(userId);
            var (pageNumber, pageSize) = NormalizePaging(request);
            var totalRecords = await CountAppointmentsAsync(doctorProfile.Id, request);
            var totalPages = CalculateTotalPages(totalRecords, pageSize);
            var appointments = await FetchAppointmentsAsync(
                doctorProfile.Id, request, pageNumber, pageSize);
            var response = BuildResponse(
                appointments, pageNumber, pageSize, totalPages, totalRecords);
            return CreateSuccessResponse(response);
        }

        /// <summary>
        /// Resolves the active doctor profile for the specified user.
        /// </summary>
        /// <param name="userId">The user ID linked to the doctor profile.</param>
        /// <returns>The active <see cref="DoctorProfile"/>.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when no active doctor profile is found.</exception>
        private async Task<DoctorProfile> ResolveActiveDoctorProfileAsync(Guid userId)
        {
            var doctorProfile = await _doctorRepository
                .FindByCondition(d => d.UserId == userId && d.IsActive)
                .FirstOrDefaultAsync();
            if (doctorProfile is null)
                throw new KeyNotFoundException(GeneralCode.APP_MESSAGE_4008.ToString());
            return doctorProfile;
        }

        /// <summary>
        /// Normalizes pagination parameters, ensuring valid page number and page size.
        /// </summary>
        /// <param name="request">The incoming request.</param>
        /// <returns>Normalized page number and page size.</returns>
        private static (int PageNumber, int PageSize) NormalizePaging(ViewDoctorAppointmentsRequest request)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
            return (pageNumber, pageSize);
        }

        /// <summary>
        /// Counts the total number of appointments matching the filter criteria.
        /// </summary>
        private async Task<int> CountAppointmentsAsync(
            Guid doctorId,
            ViewDoctorAppointmentsRequest request)
        {
            var query = BuildBaseQuery(doctorId, request);
            return await query.CountAsync();
        }

        /// <summary>
        /// Retrieves a paginated list of appointments with all related data.
        /// </summary>
        private async Task<List<AppointmentItem>> FetchAppointmentsAsync(
            Guid doctorId,
            ViewDoctorAppointmentsRequest request,
            int pageNumber,
            int pageSize)
        {
            var query = BuildBaseQuery(doctorId, request);

            return await query
                .OrderByDescending(a => a.AppointmentDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AppointmentItem
                {
                    AppointmentId = a.Id,
                    AppointmentDate = a.AppointmentDate,
                    Status = a.Status.ToString(),
                    Symptoms = a.Symptoms,
                    BookingSource = a.BookingSource,
                    DepositAmount = a.DepositAmount,
                    DepositPaid = a.DepositPaid,
                    PatientId = a.Patient.Id,
                    PatientName = a.Patient.FullName,
                    PatientPhone = a.Patient.PhoneNumber,
                    PatientAvatarUrl = a.Patient.User != null ? a.Patient.User.AvatarUrl : null,
                    PatientGender = a.Patient.Gender,
                    PatientDob = a.Patient.Dob,
                    ServiceId = a.ServiceId,
                    ServiceName = a.Service != null ? a.Service.ServiceName : null,
                    ServicePrice = a.Service != null ? a.Service.Price : null,
                    SlotStartTime = a.Slot.StartTime,
                    SlotEndTime = a.Slot.EndTime,
                    HasMedicalRecord = a.MedicalRecord != null,
                    MedicalRecordId = a.MedicalRecord != null ? a.MedicalRecord.Id : null,
                    CreatedAt = a.CreatedAt,
                })
                .ToListAsync();
        }

        /// <summary>
        /// Builds the base query with necessary includes and applies all filters.
        /// </summary>
        private IQueryable<Appointment> BuildBaseQuery(
            Guid doctorId,
            ViewDoctorAppointmentsRequest request)
        {
            var query = _appointmentRepository
                .FindByCondition(a => a.DoctorId == doctorId)
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Service)
                .Include(a => a.Slot)
                .Include(a => a.MedicalRecord)
                .AsQueryable();

            // Apply filters
            if (request.Status.HasValue)
            {
                query = query.Where(a => a.Status == request.Status.Value);
            }
            if (request.Date.HasValue)
            {
                var date = request.Date.Value.ToDateTime(TimeOnly.MinValue);
                query = query.Where(a => a.AppointmentDate.Date == date.Date);
            }
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var keyword = request.Search.Trim().ToLower();
                query = query.Where(a =>
                    (a.Patient.FullName != null && a.Patient.FullName.ToLower().Contains(keyword)) ||
                    (a.Patient.PhoneNumber != null && a.Patient.PhoneNumber.Contains(keyword)));
            }
            return query;
        }

        /// <summary>
        /// Calculates the total number of pages.
        /// </summary>
        private static int CalculateTotalPages(int totalRecords, int pageSize)
        {
            return pageSize == 0
                ? 0
                : (int)Math.Ceiling((double)totalRecords / pageSize);
        }

        /// <summary>
        /// Builds the response object containing pagination metadata and appointment list.
        /// </summary>
        private static ViewDoctorAppointmentsResponse BuildResponse(
            List<AppointmentItem> appointments,
            int pageNumber,
            int pageSize,
            int totalPages,
            int totalRecords)
        {
            return new ViewDoctorAppointmentsResponse
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                Appointments = appointments
            };
        }

        /// <summary>
        /// Creates a standardized successful API response.
        /// </summary>
        private static ApiResponse<ViewDoctorAppointmentsResponse> CreateSuccessResponse(
            ViewDoctorAppointmentsResponse response)
        {
            return ApiResponse<ViewDoctorAppointmentsResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                response);
        }
    }
}