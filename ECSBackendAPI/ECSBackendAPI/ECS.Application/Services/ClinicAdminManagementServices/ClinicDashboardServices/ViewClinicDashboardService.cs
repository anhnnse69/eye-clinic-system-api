using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicDashboardServices
{
    /// <summary>
    /// Handles clinic dashboard metric calculation and statistical aggregation by verifying administrator context.
    /// </summary>
    public class ViewClinicDashboardService : IViewClinicDashboardService
    {
        private readonly IRepositoryQueryBase<Appointment, Guid, AppDbContext> _appointmentRepository;
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepository;
        private readonly IRepositoryQueryBase<Service, Guid, AppDbContext> _serviceRepository;
        private readonly IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext> _roomRepository;
        private readonly IRepositoryQueryBase<MedicineCatalog, Guid, AppDbContext> _medicineRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of <see cref="ViewClinicDashboardService"/> with required repositories and context dependencies.
        /// </summary>
        /// <param name="appointmentRepository">Repository for querying clinic appointment schedules.</param>
        /// <param name="staffClinicRepository">Repository for querying staff-to-clinic relationships.</param>
        /// <param name="serviceRepository">Repository for querying clinic medical service catalog items.</param>
        /// <param name="roomRepository">Repository for querying facility rooms and operational spaces.</param>
        /// <param name="medicineRepository">Repository for querying clinic medicine catalog inventories.</param>
        /// <param name="httpContextAccessor">Accessor to retrieve authentication context from HTTP request.</param>
        public ViewClinicDashboardService(
            IRepositoryQueryBase<Appointment, Guid, AppDbContext> appointmentRepository,
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepository,
            IRepositoryQueryBase<Service, Guid, AppDbContext> serviceRepository,
            IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext> roomRepository,
            IRepositoryQueryBase<MedicineCatalog, Guid, AppDbContext> medicineRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _appointmentRepository = appointmentRepository;
            _staffClinicRepository = staffClinicRepository;
            _serviceRepository = serviceRepository;
            _roomRepository = roomRepository;
            _medicineRepository = medicineRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes clinic dashboard request by validating the authenticated user, aggregating active domain metrics, and generating statistical timelines.
        /// </summary>
        /// <param name="request">The view clinic dashboard request criteria.</param>
        /// <returns>An <see cref="ApiResponse{ViewClinicDashboardResponse}"/> containing aggregated metrics on success, or an error code.</returns>
        public async Task<ApiResponse<ViewClinicDashboardResponse>> Process()
        {
            // Initialize status tracking flags
            bool isUserValid = true;
            bool isClinicExist = true;

            // Extract User ID from current token context
            var userId = RetrieveUserId(ref isUserValid);

            // Fetch linked Clinic ID based on user relationship mapping
            var clinicId = await RetrieveClinicId(
                userId,
                isUserValid);

            // Aggregate core statistics, counts, and historical transaction datasets
            var dashboardData = await RetrieveDashboardData(
                clinicId);

            // Validate that the targeted clinic context exists and has valid metrics computed
            ValidateRetrievedData(
                dashboardData,
                ref isClinicExist);

            // Assemble API payload or generate contextual workflow error response
            return CreateResponse(
                dashboardData,
                isUserValid,
                isClinicExist);
        }

        /// <summary>
        /// Retrieves the current user's unique identifier from HTTP context JWT identity claims.
        /// </summary>
        /// <param name="isUserValid">Flag updated to <c>false</c> if the identity claim is missing or malformed.</param>
        /// <returns>The extracted <see cref="Guid"/> on success; otherwise <see cref="Guid.Empty"/>.</returns>
        private Guid RetrieveUserId(ref bool isUserValid)
        {
            var userIdClaim = _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                isUserValid = false;
                return Guid.Empty;
            }

            return userId;
        }

        /// <summary>
        /// Resolves the associated Clinic ID for the specified staff/user account.
        /// </summary>
        /// <param name="userId">The verified user identifier.</param>
        /// <param name="isUserValid">Pre-condition check status indicating if user resolution is skipped.</param>
        /// <returns>The mapped <see cref="Guid"/> of the target clinic, or <c>null</c> if skipped/not found.</returns>
        private async Task<Guid?> RetrieveClinicId(
            Guid userId,
            bool isUserValid)
        {
            if (!isUserValid)
            {
                return null;
            }

            var staffClinic = await _staffClinicRepository
                .FindByCondition(x =>
                    x.UserId == userId &&
                    x.IsActive)
                .FirstOrDefaultAsync();

            return staffClinic?.ClinicId;
        }

        /// <summary>
        /// Queries and aggregates data stores to compile dynamic metrics, active resource counts, and revenue indicators for the specified clinic.
        /// </summary>
        /// <param name="clinicId">The targeted clinic identifier token.</param>
        /// <returns>The fully compiled <see cref="ViewClinicDashboardResponse"/> if clinic exists; otherwise <c>null</c>.</returns>
        private async Task<ViewClinicDashboardResponse?>
            RetrieveDashboardData(Guid? clinicId)
        {
            if (!clinicId.HasValue)
            {
                return null;
            }

            var today = DateTime.UtcNow.Date;

            // Retrieve entire clinic history datasets to process in-memory aggregations safely
            var appointments = await _appointmentRepository
                .FindByCondition(x =>
                    x.Doctor.ClinicId == clinicId.Value)
                .Include(x => x.Doctor)
                .ToListAsync();

            // Isolate appointment datasets scheduled for the current operational date
            var todayAppointments = appointments
                .Where(x => x.AppointmentDate.Date == today)
                .ToList();

            var dashboard = new ViewClinicDashboardResponse
            {
                TotalAppointments = todayAppointments.Count,

                CompletedAppointments = todayAppointments.Count(
                    x => x.Status == AppointmentStatus.COMPLETED),

                CancelledAppointments = todayAppointments.Count(
                    x => x.Status == AppointmentStatus.CANCELLED),

                TotalRevenue = todayAppointments
                    .Where(x => x.DepositPaid)
                    .Sum(x => x.DepositAmount),

                TotalStaffs = await _staffClinicRepository
                    .FindByCondition(x =>
                        x.ClinicId == clinicId.Value &&
                        x.IsActive)
                    .CountAsync(),

                TotalServices = await _serviceRepository
                    .FindByCondition(x =>
                        x.ClinicId == clinicId.Value &&
                        x.IsActive)
                    .CountAsync(),

                TotalRooms = await _roomRepository
                    .FindByCondition(x =>
                        x.ClinicId == clinicId.Value &&
                        x.IsActive)
                    .CountAsync(),

                TotalMedicines = await _medicineRepository
                    .FindByCondition(x =>
                        x.ClinicId == clinicId.Value &&
                        x.IsActive)
                    .CountAsync()
            };

            // Inject sequential temporal rolling weekly statistics into the summary payload
            dashboard.WeeklyStatistics =
                BuildWeeklyStatistics(appointments);

            return dashboard;
        }

        /// <summary>
        /// Evaluates the generated dashboard structure presence and flags missing data discrepancies.
        /// </summary>
        /// <param name="dashboardData">The resolved statistical data object, or <c>null</c> if the lookup yielded no parent clinic record.</param>
        /// <param name="isClinicExist">Flag updated to <c>false</c> if data validation checks fail.</param>
        private void ValidateRetrievedData(
            ViewClinicDashboardResponse? dashboardData,
            ref bool isClinicExist)
        {
            if (dashboardData == null)
            {
                isClinicExist = false;
            }
        }

        /// <summary>
        /// Generates an encapsulation framework structure response payload containing dashboard metrics or validation failures.
        /// </summary>
        /// <param name="dashboardData">The queried and compiled clinic dashboard metrics.</param>
        /// <param name="isUserValid">Flag state parameter mapping token context validity.</param>
        /// <param name="isClinicExist">Flag state parameter mapping entity persistence verification.</param>
        /// <returns>A configured <see cref="ApiResponse{ViewClinicDashboardResponse}"/>.</returns>
        private ApiResponse<ViewClinicDashboardResponse>
            CreateResponse(
                ViewClinicDashboardResponse? dashboardData,
                bool isUserValid,
                bool isClinicExist)
        {
            var errorResponse =
                CreateErrorResponse(
                    isUserValid,
                    isClinicExist);

            if (errorResponse != null)
            {
                return errorResponse;
            }

            return ApiResponse<ViewClinicDashboardResponse>
                .Success(
                    GeneralCode.APP_MESSAGE_2000.ToString(),
                    MapToResponse(dashboardData!));
        }

        /// <summary>
        /// Transforms internal persistent context database model data directly onto business serialization object schemas.
        /// </summary>
        /// <param name="dashboard">The internal compiled dashboard data representation source object.</param>
        /// <returns>A structural domain projection instance representation object.</returns>
        private ViewClinicDashboardResponse MapToResponse(
            ViewClinicDashboardResponse dashboard)
        {
            return new ViewClinicDashboardResponse
            {
                TotalAppointments = dashboard.TotalAppointments,
                CompletedAppointments = dashboard.CompletedAppointments,
                CancelledAppointments = dashboard.CancelledAppointments,
                TotalRevenue = dashboard.TotalRevenue,
                TotalStaffs = dashboard.TotalStaffs,
                TotalServices = dashboard.TotalServices,
                TotalRooms = dashboard.TotalRooms,
                TotalMedicines = dashboard.TotalMedicines,
                WeeklyStatistics = dashboard.WeeklyStatistics
            };
        }

        /// <summary>
        /// Extrapolates historical appointment arrays into a rolling seven-day chronological timeline of analytical records.
        /// </summary>
        /// <param name="appointments">The cumulative raw collection of historic and active application appointments.</param>
        /// <returns>A structured list containing daily discrete sequential chart parameters.</returns>
        private List<DashboardChartItem>
            BuildWeeklyStatistics(
                List<Appointment> appointments)
        {
            var result = new List<DashboardChartItem>();

            var startDate =
                DateTime.UtcNow.Date.AddDays(-6);

            // Sequentially enumerate across each discrete calendar step to reconstruct financial milestones
            for (int i = 0; i < 7; i++)
            {
                var date = startDate.AddDays(i);

                var dailyAppointments = appointments
                    .Where(x =>
                        x.AppointmentDate.Date == date)
                    .ToList();

                result.Add(new DashboardChartItem
                {
                    Date = date.ToString("yyyy-MM-dd"),

                    Revenue = dailyAppointments
                        .Where(x => x.DepositPaid)
                        .Sum(x => x.DepositAmount),

                    Appointments = dailyAppointments.Count
                });
            }

            return result;
        }

        /// <summary>
        /// Screens runtime process execution flag indicators to return standardized systemic application error schemas.
        /// </summary>
        /// <param name="isUserValid">Indicates whether user resolution parameter parsing was successful.</param>
        /// <param name="isClinicExist">Indicates whether a clinic instance entity was successfully retrieved.</param>
        /// <returns>A failed <see cref="ApiResponse{ViewClinicDashboardResponse}"/> variant if errors are found; otherwise <c>null</c>.</returns>
        private ApiResponse<ViewClinicDashboardResponse>?
            CreateErrorResponse(
                bool isUserValid,
                bool isClinicExist)
        {
            // Return 4001 if user session context is corrupted
            if (!isUserValid)
            {
                return ApiResponse<ViewClinicDashboardResponse>
                    .Fail(
                        GeneralCode.APP_MESSAGE_4001.ToString());
            }

            // Return 4020 if the clinic data instance does not exist
            if (!isClinicExist)
            {
                return ApiResponse<ViewClinicDashboardResponse>
                    .Fail(
                        GeneralCode.APP_MESSAGE_4020.ToString());
            }

            return null;
        }
    }
}