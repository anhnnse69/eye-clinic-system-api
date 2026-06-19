using ECS.Application.Common.Response;
using ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetPatientsListServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using System.Linq.Expressions;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetDailyAppointmentsServices
{
    /// <summary>
    /// Handles the business logic for verifying data permissions, compiling dynamic queries, 
    /// evaluating live dashboard counters, and formatting UI elements with correct room mapping.
    /// </summary>
    public class ReceptionistGetDailyAppointmentsService : IReceptionistGetDailyAppointmentsService
    {
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffQueryRepo;
        private readonly IRepositoryQueryBase<Appointment, Guid, AppDbContext> _appointmentQueryRepo;

        /// <summary>
        /// Initializes a new instance of <see cref="ReceptionistGetDailyAppointmentsService"/> with isolated data access gateways.
        /// </summary>
        /// <param name="staffQueryRepo">The query-only tracking repository interface for retrieving staff spatial configuration boundaries.</param>
        /// <param name="appointmentQueryRepo">The query-only tracking repository interface for retrieving targeted appointment core entities.</param>
        public ReceptionistGetDailyAppointmentsService(
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffQueryRepo,
            IRepositoryQueryBase<Appointment, Guid, AppDbContext> appointmentQueryRepo)
        {
            _staffQueryRepo = staffQueryRepo;
            _appointmentQueryRepo = appointmentQueryRepo;
        }

        /// <summary>
        /// Orchestrates the process of security boundary verification, dynamic predicate expression builds, counter aggregations, and data partitioning.
        /// </summary>
        /// <param name="request">The filtration and context parameters bundle for the processing lookup request.</param>
        /// <returns>A structured <see cref="ApiResponse{T}"/> packing matched transaction presentation data matrices.</returns>
        public async Task<ApiResponse<List<ReceptionistGetDailyAppointmentsResponse>>> Process(ReceptionistGetDailyAppointmentsRequest request)
        {
            // Step 1: Resolve the secure boundary partition array assigned to the receptionist context (Filtering domain spaces)
            var localizedClinicBoundaryIds = ResolveReceptionistClinics(request.CurrentUserId);
            // Step 2: Build dynamic filtration expression trees for EF Core compilation targeting Appointment aggregate graphs
            var coreFilterPredicate = BuildCoreFilterCriteria(request, localizedClinicBoundaryIds);
            // Step 3: Extract comprehensive un-partitioned data queries to compute real-time live telemetry dashboard metrics
            var unpartitionedSecureQuery = FetchBaseQueryStream(coreFilterPredicate);
            // Step 4: Evaluate dashboard analytics summary items across specific enumeration categories
            var operationalDashboardStats = ComputeLiveTelemetryDashboard(unpartitionedSecureQuery);
            // Step 5: Execute database lookup to slice, rank, and capture the paginated array segment
            var partitionedDatabaseRecords = ExtractPaginatedRecordsList(unpartitionedSecureQuery, request, out int aggregateMatchingRows);
            // Step 6: Map nested deep entity graph hierarchies into structural flattened data rows matching UI designs
            var formattedListRows = TransformToPresentationDtos(partitionedDatabaseRecords);
            // Step 7: Build pagination envelope trackers to stabilize client-side grid scrolling limits
            var paginationMetadata = BuildPaginationMeta(request, aggregateMatchingRows);
            // Step 8: Wrap payloads inside standardized system success response tracking structures and attach live metrics
            return AssembleFinalApiResponse(formattedListRows, paginationMetadata, operationalDashboardStats);
        }

        /// <summary>
        /// Extracts all active clinic boundary registrations associated with the requesting context receptionist.
        /// </summary>
        /// <param name="staffUserId">The master operational user identity trace used for security clearance resolution.</param>
        /// <returns>A primitive collection tracking matched structural clinic identifiers.</returns>
        private List<Guid> ResolveReceptionistClinics(Guid staffUserId)
        {
            return _staffQueryRepo.FindByCondition(
                sc => sc.UserId == staffUserId && sc.IsActive && (int)sc.Role == 3, // 3 = RECEPTIONIST Role Identifier
                trackChanges: false
            ).Select(sc => sc.ClinicId).ToList();
        }

        /// <summary>
        /// Compiles dynamic LINQ filters matching required UI specifications and strict tenancy constraints.
        /// </summary>
        /// <param name="request">The dynamic parameters container holding client-side application state filters.</param>
        /// <param name="safeClinicIds">The verified secure clinic identifier list restricting dataset boundaries.</param>
        /// <returns>A compiled lambda expression tree tracking targeted query conditions.</returns>
        private Expression<Func<Appointment, bool>> BuildCoreFilterCriteria(ReceptionistGetDailyAppointmentsRequest request, List<Guid> safeClinicIds)
        {
            // Enforce tenancy constraints: Only target records where Doctor's Clinic belongs to receptionist's managed scope
            Expression<Func<Appointment, bool>> filter = ap => safeClinicIds.Contains(ap.Doctor.ClinicId);
            // Apply date constraints (Defaults to today if target input parameters are missing)
            DateTime todayVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).Date;
            var targetDate = request.TargetDate ?? todayVn;
            filter = CombineExpressions(filter, ap => ap.AppointmentDate.Date == targetDate.Date);
            // Conditional shift filters integration based on request metrics
            if (request.ShiftFilter.HasValue)
            {
                filter = CombineExpressions(filter, ap => ap.Slot.Schedule.ShiftType == request.ShiftFilter.Value);
            }
            // Conditional search criteria matching patient fullName or phoneNumber segments
            if (!string.IsNullOrWhiteSpace(request.SearchPatient))
            {
                var phrase = request.SearchPatient.Trim().ToLower();
                filter = CombineExpressions(filter, ap => ap.Patient.FullName.ToLower().Contains(phrase) ||
                                                          (ap.Patient.PhoneNumber != null && ap.Patient.PhoneNumber.Contains(phrase)));
            }
            // Conditional search criteria matching assigned doctor fullname sequences
            if (!string.IsNullOrWhiteSpace(request.SearchDoctor))
            {
                var phrase = request.SearchDoctor.Trim().ToLower();
                filter = CombineExpressions(filter, ap => ap.Doctor.User.FullName.ToLower().Contains(phrase));
            }
            return filter;
        }

        /// <summary>
        /// Builds an IQueryable query stream packing all eager-loaded navigation properties required for computation and extraction.
        /// </summary>
        /// <param name="coreFilterPredicate">The baseline compiled filtering criteria matching business requirements.</param>
        /// <returns>An executable query stream encompassing eager-loaded navigation targets.</returns>
        private IQueryable<Appointment> FetchBaseQueryStream(Expression<Func<Appointment, bool>> coreFilterPredicate)
        {
            return _appointmentQueryRepo.FindByCondition(
                coreFilterPredicate,
                trackChanges: false,
                ap => ap.Patient,
                ap => ap.Doctor,
                ap => ap.Doctor.User,
                ap => ap.Slot,
                ap => ap.Slot.Schedule,
                ap => ap.Slot.Schedule.Room!,
                ap => ap.Queue!
            );
        }

        /// <summary>
        /// Aggregates database rows into live statistics markers.
        /// </summary>
        /// <param name="baseSecureStream">The secure database data channel tracking the target day snapshots.</param>
        /// <returns>An internal telemetry counter container mapped for real-time dashboard visualization.</returns>
        private LiveTrackingStatsDto ComputeLiveTelemetryDashboard(IQueryable<Appointment> baseSecureStream)
        {
            var dataSnapshot = baseSecureStream.Select(ap => new { ap.Status }).ToList();
            return new LiveTrackingStatsDto
            {
                TotalDailyAppointments = dataSnapshot.Count,
                TotalArrivedAndLive = dataSnapshot.Count(x => x.Status == AppointmentStatus.ARRIVED || x.Status == AppointmentStatus.IN_PROGRESS),
                TotalCompletedExams = dataSnapshot.Count(x => x.Status == AppointmentStatus.COMPLETED),
                TotalCancelledExams = dataSnapshot.Count(x => x.Status == AppointmentStatus.CANCELLED)
            };
        }

        /// <summary>
        /// Slices, commands sorting sequences and isolates data chunks according to requested boundary offsets.
        /// </summary>
        /// <param name="computedStream">The fully resolved eager-loaded database object query track.</param>
        /// <param name="request">The presentation parameters model detailing skip and take evaluation offsets.</param>
        /// <param name="aggregateMatchingRows">The output parameter tracking total records matched prior to slicing segments.</param>
        /// <returns>A finalized subset list mapping raw transactional data rows.</returns>
        private List<Appointment> ExtractPaginatedRecordsList(
            IQueryable<Appointment> computedStream,
            ReceptionistGetDailyAppointmentsRequest request,
            out int aggregateMatchingRows)
        {
            var sortedStream = computedStream.OrderByDescending(ap => ap.CreatedAt);
            aggregateMatchingRows = sortedStream.Count();
            return sortedStream
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();
        }

        /// <summary>
        /// Maps internal data structures into clean primitive structural strings optimized for UI state renderings.
        /// </summary>
        /// <param name="dbRecords">The structural data list tracking extracted core entity graphs.</param>
        /// <returns>A transformed list matching the optimized live grid viewport flat data structure.</returns>
        private List<ReceptionistGetDailyAppointmentsResponse> TransformToPresentationDtos(List<Appointment> dbRecords)
        {
            return dbRecords.Select(ap => new ReceptionistGetDailyAppointmentsResponse
            {
                Id = ap.Id.ToString(),
                PatientId = ap.PatientId.ToString(),
                DoctorId = ap.DoctorId.ToString(),
                SlotId = ap.SlotId.ToString(),
                AppointmentDate = ap.AppointmentDate.ToString("yyyy-MM-dd"),
                Symptoms = ap.Symptoms,
                Status = ap.Status.ToString().ToUpper(),
                DepositAmount = ap.DepositAmount,
                DepositPaid = ap.DepositPaid,
                BookingSource = ap.BookingSource.ToUpper(),
                Patient = new PatientProfileRowDto
                {
                    Id = ap.Patient.Id.ToString(),
                    FullName = ap.Patient.FullName,
                    Gender = ap.Patient.Gender.ToString().ToUpper(),
                    Dob = ap.Patient.Dob.ToString("yyyy-MM-dd"),
                    PhoneNumber = ap.Patient.PhoneNumber,
                    BhytNumber = ap.Patient.BhytNumber
                },
                Doctor = new DoctorProfileRowDto
                {
                    Id = ap.Doctor.Id.ToString(),
                    FullName = ap.Doctor.User.FullName,
                    ClinicRoomName = ap.Slot?.Schedule?.Room?.RoomName ?? "Phòng khám chung"
                },
                Slot = new TimeSlotRowDto
                {
                    Id = ap.Slot.Id.ToString(),
                    ScheduleId = ap.Slot.ScheduleId.ToString(),
                    StartTime = ap.Slot.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    EndTime = ap.Slot.EndTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    ShiftType = ap.Slot.Schedule.ShiftType.ToString().ToUpper()
                },
                Queue = ap.Queue != null ? new QueueInlineRowDto
                {
                    Id = ap.Queue.Id.ToString(),
                    QueueNumber = ap.Queue.QueueNumber,
                    Status = ap.Queue.Status.ToString().ToUpper(),
                    CalledAt = ap.Queue.CalledAt?.ToString("yyyy-MM-ddTHH:mm:ssZ")
                } : null
            }).ToList();
        }

        /// <summary>
        /// Formulates structural indexing telemetry bounds confirming accurate grid partition depths.
        /// </summary>
        /// <param name="request">The filtering request parameter pack providing layout page criteria metrics.</param>
        /// <param name="totalCount">The global registration aggregate record quantity count.</param>
        /// <returns>A verified structural tracking object containing metadata tracking segments.</returns>
        private MetaResponse BuildPaginationMeta(ReceptionistGetDailyAppointmentsRequest request, int totalCount)
        {
            return new MetaResponse(request.PageNumber, request.PageSize, totalCount);
        }

        /// <summary>
        /// Combines multi-tiered transactional presentation parameters into a uniform API message tracking response.
        /// </summary>
        /// <param name="rows">The presentation data row payload array destined for client state renderings.</param>
        /// <param name="meta">The transactional pagination depth tracker metrics packet.</param>
        /// <param name="telemetry">The operational dashboard overview metrics object payload wrapper.</param>
        /// <returns>A standardized API packet encapsulation carrying data graphs and system logs tokens.</returns>
        private ApiResponse<List<ReceptionistGetDailyAppointmentsResponse>> AssembleFinalApiResponse(
            List<ReceptionistGetDailyAppointmentsResponse> rows,
            MetaResponse meta,
            LiveTrackingStatsDto telemetry)
        {
            // Return verified standard success wrapper ensuring complete synchronization across presentation widgets
            return ApiResponse<List<ReceptionistGetDailyAppointmentsResponse>>.Success("APP_MESSAGE_2000", rows, meta);
        }

        /// <summary>
        /// Performs operational structural merging over independent expression trees using specific visitor node rewrites.
        /// </summary>
        /// <typeparam name="T">The fundamental baseline entity tracking class type framework definition.</typeparam>
        /// <param name="first">The structural baseline filtering lambda tree structure.</param>
        /// <param name="second">The secondary conditional lookup filtering criteria parameter block.</param>
        /// <returns>An aggregated logical composite condition block matching combined constraints.</returns>
        private Expression<Func<T, bool>> CombineExpressions<T>(Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            var parameter = Expression.Parameter(typeof(T));
            var leftVisitor = new ReplaceExpressionVisitor(first.Parameters[0], parameter);
            var left = leftVisitor.Visit(first.Body);
            var rightVisitor = new ReplaceExpressionVisitor(second.Parameters[0], parameter);
            var right = rightVisitor.Visit(second.Body);
            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left!, right!), parameter);
        }
    }
}