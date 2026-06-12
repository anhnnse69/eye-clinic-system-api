using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicAppointmentServices
{
    /// <summary>
    /// Handles the business logic for retrieving, filtering, and paging appointments inside a specific clinic.
    /// </summary>
    public class GetClinicAppointmentsService : IGetClinicAppointmentsService
    {
        private readonly IRepositoryQueryBase<Appointment, Guid, AppDbContext> _appointmentRepository;
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetClinicAppointmentsService"/> class with required infrastructure dependencies.
        /// </summary>
        /// <param name="appointmentRepository">Repository boundary instance for querying underlying domain appointment records.</param>
        /// <param name="staffClinicRepository">Repository boundary instance managing system staff-to-clinic contextual relation rows.</param>
        /// <param name="httpContextAccessor">Accessor to safely retrieve authentication claims identities out of current HTTP request pipelines.</param>
        public GetClinicAppointmentsService(
            IRepositoryQueryBase<Appointment, Guid, AppDbContext> appointmentRepository,
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _appointmentRepository = appointmentRepository;
            _staffClinicRepository = staffClinicRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes the internal data pipeline workflow to parse filters, evaluate operational bounds, and return paginated data collections.
        /// </summary>
        /// <param name="request">The parameters containing data filters, keywords, and explicit pagination criteria details.</param>
        /// <returns>An <see cref="ApiResponse{List{GetClinicAppointmentResponse}}"/> enclosing descriptive state payloads alongside metadata boundaries.</returns>
        public async Task<ApiResponse<List<GetClinicAppointmentResponse>>> Process(
            GetClinicAppointmentsRequest request)
        {
            // Step 1: Initialize sequential control status validation variables
            bool isUserValid = true;

            // Step 2: Extract identity information parameter metrics from the active security claim session context
            var userId = RetrieveUserId(out isUserValid);

            // Step 3: Search operational relational databases to identify the clinic bound tightly to the active account
            var clinicId = await RetrieveClinicId(userId, isUserValid);

            // Step 4: Synthesize a flexible criteria lambda matrix expression using dynamic criteria logic mapping
            var filterExpression = BuildFilterExpression(clinicId ?? Guid.Empty, request);

            // Step 5: Query underlying target physical data storage blocks executing paged evaluation algorithms
            var (appointments, totalRecords) = await ExecutePagedQuery(filterExpression, request);

            // Step 6: Map internal domain state model attributes onto decoupled serialized response data schemas
            var result = MapToResponseDto(appointments);

            // Step 7: Package contextual index layout tracker parameters to formulate pagination tracking wrappers
            var meta = BuildPaginationMeta(request, totalRecords);

            // Step 8: Evaluate processing parameters and package state structures dynamically to handle execution outcomes
            return CreateResponse(result, meta, isUserValid, clinicId.HasValue);
        }

        /// <summary>
        /// Resolves the logged-in user signature coordinates using synchronous token parsing streams.
        /// </summary>
        /// <param name="isUserValid">Output evaluation status updating to false if identification variables fail parsing matches.</param>
        /// <returns>The decoded user identity descriptor parameter identifier value.</returns>
        private Guid RetrieveUserId(out bool isUserValid)
        {
            isUserValid = true;

            var userIdClaim = _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                isUserValid = false;
                return Guid.Empty;
            }

            return userId;
        }

        /// <summary>
        /// Probes system allocation tables to isolate unique context configurations related directly onto the logged-in account.
        /// </summary>
        /// <param name="userId">The system identity identifier tracking target metrics variables.</param>
        /// <param name="isUserValid">Validation controller metric parameter guarding access execution states.</param>
        /// <returns>A tracking key token matching system database configurations if matched; otherwise null.</returns>
        private async Task<Guid?> RetrieveClinicId(
            Guid userId,
            bool isUserValid)
        {
            if (!isUserValid)
            {
                return null;
            }

            var staffClinic = await _staffClinicRepository
                .FindByCondition(
                    x => x.UserId == userId &&
                         x.IsActive)
                .FirstOrDefaultAsync();

            return staffClinic?.ClinicId;
        }

        /// <summary>
        /// Assembles dynamic lambda filter structures mapping request parameters against entity state conditions.
        /// </summary>
        /// <param name="clinicId">The contextual location domain entity reference filter identity value.</param>
        /// <param name="request">The structural entity containing filtering keys and status search parameters.</param>
        /// <returns>A composite queryable logic filter expression block.</returns>
        private Expression<Func<Appointment, bool>> BuildFilterExpression(
            Guid clinicId,
            GetClinicAppointmentsRequest request)
        {
            // Normalize inputs to bypass tracking lookup string format casing collisions
            var searchTerm = request.SearchTerm?
                .Trim()
                .ToLower();

            return x =>
                // Filter targeted rows mapping directly against verified environment context
                x.Doctor.ClinicId == clinicId

                && (!request.Status.HasValue
                    || x.Status == request.Status.Value)

                && (!request.AppointmentDate.HasValue
                    || x.AppointmentDate.Date == request.AppointmentDate.Value.Date)

                && (string.IsNullOrEmpty(searchTerm)
                    || x.Patient.FullName.ToLower().Contains(searchTerm)
                    || x.Doctor.User.FullName.ToLower().Contains(searchTerm));
        }

        /// <summary>
        /// Executes eager loading database evaluations using explicit transactional tracking skips and page index limit controls.
        /// </summary>
        /// <param name="filterExpression">The structured operational logic filter condition matrix maps.</param>
        /// <param name="request">The data container tracking layout configuration bounds.</param>
        /// <returns>A tuple pairing structural result detail item list rows with overall record aggregate bounds integers.</returns>
        private async Task<(List<Appointment> Appointments, int TotalRecords)> ExecutePagedQuery(
            Expression<Func<Appointment, bool>> filterExpression,
            GetClinicAppointmentsRequest request)
        {
            // Bind structural relational tables explicitly via internal eager include graph navigations
            var query = _appointmentRepository
                .FindByCondition(
                    filterExpression,
                    trackChanges: false)
                .Include(x => x.Patient)
                .Include(x => x.Doctor)
                    .ThenInclude(x => x.User)
                .Include(x => x.Slot)
                .Include(x => x.Service);

            // Fetch absolute total database matches hitting the specific constraint filters
            var totalRecords = await query.CountAsync();

            // Segment target storage collections leveraging sequential date and time indices order lines
            var items = await query
                .OrderByDescending(x => x.AppointmentDate)
                .ThenBy(x => x.Slot.StartTime)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return (items, totalRecords);
        }

        /// <summary>
        /// Transforms internal persistent context database model list directly onto business serialization object schemas.
        /// </summary>
        /// <param name="data">The physical domain core model list array returned directly out of backend operational layers.</param>
        /// <returns>A structured target presentation data payload collection instance context.</returns>
        private List<GetClinicAppointmentResponse> MapToResponseDto(
            List<Appointment> data)
        {
            return data.Select(app => new GetClinicAppointmentResponse
            {
                // Mapping physical core system database record keys onto string data representations
                Id_appointment = app.Id.ToString(),
                PatientName = app.Patient.FullName,

                // Enforce safety fallbacks to block structural layout disruption from unexpected white space rows
                PatientPhone = string.IsNullOrWhiteSpace(app.Patient.PhoneNumber)
                    ? "N/A"
                    : app.Patient.PhoneNumber,
                DoctorName = app.Doctor.User.FullName,
                ServiceName = app.Service?.ServiceName ?? "N/A",

                // Formatting timestamp intervals into localized date presentation layout strings
                AppointmentDate = app.AppointmentDate.ToString("dd/MM/yyyy"),

                // String parsing timeline bounds intervals to build legible customer schedule strings
                TimeSlot = $"{app.Slot.StartTime:HH\\:mm} - {app.Slot.EndTime:HH\\:mm}",
                Status = app.Status.ToString(),
                DepositAmount = app.DepositAmount,
                DepositPaid = app.DepositPaid,
                BookingSource = app.BookingSource,

                Symptoms = string.IsNullOrWhiteSpace(app.Symptoms)
                    ? "N/A"
                    : app.Symptoms,

                CreatedAt = app.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            }).ToList();
        }

        /// <summary>
        /// Assembles pagination envelope metrics parameters using custom constructor signatures.
        /// </summary>
        /// <param name="request">The tracking item block mapping target layout page configurations.</param>
        /// <param name="totalRecords">The quantified data baseline row count metrics context.</param>
        /// <returns>A populated metadata envelope serialization module component instance.</returns>
        private MetaResponse BuildPaginationMeta(
            GetClinicAppointmentsRequest request,
            int totalRecords)
        {
            return new MetaResponse(
                request.PageNumber,
                request.PageSize,
                totalRecords);
        }

        /// <summary>
        /// Analyzes state logic monitoring variables to determine outcome layout packaging choices.
        /// </summary>
        /// <param name="result">The structured data response payload list projected from database layers.</param>
        /// <param name="meta">The pagination metadata structure layout configuration parameters.</param>
        /// <param name="isUserValid">Indicates whether user token extraction verification matched expectations successfully.</param>
        /// <param name="isClinicExist">Indicates data layer tracking context presence attributes.</param>
        /// <returns>A standardized application payload container detailed for transport serialization layers.</returns>
        private ApiResponse<List<GetClinicAppointmentResponse>> CreateResponse(
            List<GetClinicAppointmentResponse> result,
            MetaResponse meta,
            bool isUserValid,
            bool isClinicExist)
        {
            var errorResponse = CreateErrorResponse(isUserValid, isClinicExist);
            if (errorResponse != null)
            {
                return errorResponse;
            }

            return ApiResponse<List<GetClinicAppointmentResponse>>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                result,
                meta);
        }

        /// <summary>
        /// Evaluates structural tracking conditions matrix variables to issue systemized application error entries.
        /// </summary>
        /// <param name="isUserValid">The structural state value verifying the validation of incoming authentication parameters.</param>
        /// <param name="isClinicExist">The state checking indicator capturing contextual environment existence benchmarks.</param>
        /// <returns>A failed API standard metadata package capsule if an error rule trips; otherwise null properties.</returns>
        private ApiResponse<List<GetClinicAppointmentResponse>>? CreateErrorResponse(
            bool isUserValid,
            bool isClinicExist)
        {
            // Return 4001 if authentication tokens fail validation operations
            if (!isUserValid)
            {
                return ApiResponse<List<GetClinicAppointmentResponse>>.Fail(
                    GeneralCode.APP_MESSAGE_4001.ToString());
            }

            // Return 4020 if the linked operations clinic setup drops out of active entity streams
            if (!isClinicExist)
            {
                return ApiResponse<List<GetClinicAppointmentResponse>>.Fail(
                    GeneralCode.APP_MESSAGE_4020.ToString());
            }

            return null;
        }
    }
}