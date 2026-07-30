using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewClinicAppointmentsServices
{
    /// <summary>
    /// Handles retrieving paginated appointments for every doctor within
    /// a receptionist's clinic, including doctor, patient, service, slot,
    /// and medical record data. Supports filtering by doctor, status,
    /// date, and search keyword. The receptionist's clinic is resolved
    /// via their active <see cref="StaffClinic"/> assignment.
    /// </summary>
    public class ViewClinicAppointmentsService : IViewClinicAppointmentsService
    {
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepo;
        private readonly IRepositoryQueryBase<Appointment, Guid, AppDbContext> _appointmentRepo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewClinicAppointmentsService"/>.
        /// </summary>
        public ViewClinicAppointmentsService(
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepo,
            IRepositoryQueryBase<Appointment, Guid, AppDbContext> appointmentRepo)
        {
            _staffClinicRepo = staffClinicRepo;
            _appointmentRepo = appointmentRepo;
        }

        /// <summary>
        /// Retrieves paginated appointments for the receptionist's clinic,
        /// across all doctors, with support for filtering and searching.
        /// </summary>
        /// <param name="receptionistUserId">Identifier of the user account of the logged-in receptionist.</param>
        /// <param name="request">The request containing pagination and filter parameters.</param>
        /// <returns>A successful response containing the paginated list of appointments.</returns>
        public async Task<ApiResponse<ViewClinicAppointmentsResponse>> Process(
            Guid receptionistUserId,
            ViewClinicAppointmentsRequest request)
        {
            var clinicId = await ResolveReceptionistClinicIdAsync(receptionistUserId);
            var (pageNumber, pageSize) = NormalizePaging(request);
            var totalRecords = await CountAppointmentsAsync(clinicId, request);
            var totalPages = CalculateTotalPages(totalRecords, pageSize);
            var appointments = await FetchAppointmentsAsync(clinicId, request, pageNumber, pageSize);
            var response = BuildResponse(appointments, pageNumber, pageSize, totalPages, totalRecords);
            return CreateSuccessResponse(response);
        }

        /// <summary>
        /// Resolves the clinic that the receptionist (current user) belongs to,
        /// via their active <see cref="StaffClinic"/> assignment.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when the receptionist has no active clinic assignment.</exception>
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
        /// Normalizes pagination parameters, ensuring valid page number and page size.
        /// </summary>
        private static (int PageNumber, int PageSize) NormalizePaging(ViewClinicAppointmentsRequest request)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
            return (pageNumber, pageSize);
        }

        /// <summary>
        /// Counts the total number of appointments matching the filter criteria.
        /// </summary>
        private async Task<int> CountAppointmentsAsync(
            Guid clinicId,
            ViewClinicAppointmentsRequest request)
        {
            var query = BuildBaseQuery(clinicId, request);
            return await query.CountAsync();
        }

        /// <summary>
        /// Retrieves a paginated list of appointments with all related data.
        /// </summary>
        private async Task<List<ClinicAppointmentItem>> FetchAppointmentsAsync(
            Guid clinicId,
            ViewClinicAppointmentsRequest request,
            int pageNumber,
            int pageSize)
        {
            var query = BuildBaseQuery(clinicId, request);

            return await query
                .OrderByDescending(a => a.AppointmentDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new ClinicAppointmentItem
                {
                    AppointmentId = a.Id,
                    AppointmentDate = a.AppointmentDate,
                    Status = a.Status.ToString(),
                    Symptoms = a.Symptoms,
                    BookingSource = a.BookingSource,
                    DepositAmount = a.DepositAmount,
                    DepositPaid = a.DepositPaid,

                    DoctorId = a.DoctorId,
                    DoctorName = a.Doctor.User != null ? a.Doctor.User.FullName : null,
                    DoctorTitle = a.Doctor.Title,
                    DoctorAvatarUrl = a.Doctor.User != null ? a.Doctor.User.AvatarUrl : null,

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
        /// Builds the base query scoped to every doctor belonging to the
        /// receptionist's clinic, with necessary includes and all filters applied.
        /// </summary>
        private IQueryable<Appointment> BuildBaseQuery(
            Guid clinicId,
            ViewClinicAppointmentsRequest request)
        {
            var query = _appointmentRepo
                .FindByCondition(a => a.Doctor.ClinicId == clinicId)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.User)
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Service)
                .Include(a => a.Slot)
                .Include(a => a.MedicalRecord)
                .AsQueryable();

            if (request.DoctorId.HasValue)
            {
                query = query.Where(a => a.DoctorId == request.DoctorId.Value);
            }
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
                    (a.Patient.PhoneNumber != null && a.Patient.PhoneNumber.Contains(keyword)) ||
                    (a.Doctor.User != null && a.Doctor.User.FullName != null &&
                        a.Doctor.User.FullName.ToLower().Contains(keyword)));
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
        private static ViewClinicAppointmentsResponse BuildResponse(
            List<ClinicAppointmentItem> appointments,
            int pageNumber,
            int pageSize,
            int totalPages,
            int totalRecords)
        {
            return new ViewClinicAppointmentsResponse
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
        private static ApiResponse<ViewClinicAppointmentsResponse> CreateSuccessResponse(
            ViewClinicAppointmentsResponse response)
        {
            return ApiResponse<ViewClinicAppointmentsResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                response);
        }
    }
}