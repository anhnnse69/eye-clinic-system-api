using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetAvailableSlotsServices
{
    /// <summary>
    /// Handles the business logic for resolving, filtering, and mapping doctor schedules for a specific clinic.
    /// </summary>
    public class GetAvailableSlotsService : IGetAvailableSlotsService
    {
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffQueryRepo;
        private readonly IRepositoryQueryBase<DoctorSchedule, Guid, AppDbContext> _scheduleQueryRepo;

        /// <summary>
        /// Initializes a new instance of <see cref="GetAvailableSlotsService"/> with query repositories.
        /// </summary>
        /// <param name="staffQueryRepo">The query repository for staff clinic assignments.</param>
        /// <param name="scheduleQueryRepo">The query repository for doctor schedules.</param>
        public GetAvailableSlotsService(
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffQueryRepo,
            IRepositoryQueryBase<DoctorSchedule, Guid, AppDbContext> scheduleQueryRepo)
        {
            _staffQueryRepo = staffQueryRepo;
            _scheduleQueryRepo = scheduleQueryRepo;
        }

        /// <summary>
        /// Orchestrates the process of building criteria and extracting doctor schedule matrices.
        /// </summary>
        /// <param name="request">The filtration and context parameters for the schedule query.</param>
        /// <returns>A structured <see cref="ApiResponse{List{GetAvailableSlotsResponse}}"/> wrapping the final data.</returns>
        public async Task<ApiResponse<List<GetAvailableSlotsResponse>>> Process(GetAvailableSlotsRequest request)
        {
            // Step 1: Resolve the unique ClinicId of the current receptionist
            Guid clinicId = await ResolveReceptionistClinicId(request.CurrentUserId);
            // Step 2: Construct the dynamic filtration expression for EF Core
            var filterExpression = BuildCriteriaExpression(clinicId, request);
            // Step 3: Execute query with necessary eager loaded navigation properties
            var rawSchedules = await ExecuteScheduleQuery(filterExpression);
            // Step 4: Map entity tracking graph data structures into flattened DTO arrays
            var formattedResult = MapToDoctorShiftMatrix(rawSchedules);
            // Step 5: Wrap payload inside standard response envelope and finalize
            return CreateApiResponse(formattedResult);
        }

        /// <summary>
        /// Resolves the specific clinic identifier assigned to the active receptionist.
        /// </summary>
        /// <param name="userId">The unique identifier of the receptionist user.</param>
        /// <returns>The assigned clinic Guid.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when the account is not linked to any active clinic configuration.</exception>
        private async Task<Guid> ResolveReceptionistClinicId(Guid userId)
        {
            var staffAssignment = await _staffQueryRepo
                .FindByCondition(sc => sc.UserId == userId && sc.IsActive, trackChanges: false)
                .AsQueryable()
                .FirstOrDefaultAsync();
            if (staffAssignment == null)
            {
                throw new UnauthorizedAccessException("Tài khoản lễ tân chưa được gán hoặc liên kết vào bất kỳ cơ sở phòng khám nào.");
            }
            return staffAssignment.ClinicId;
        }

        /// <summary>
        /// Builds a dynamic expressions tree targeting doctor schedule entities based on criteria arguments.
        /// </summary>
        /// <param name="clinicId">The primary clinic scope boundary filter.</param>
        /// <param name="request">The filters package from consumer query strings.</param>
        /// <returns>A reusable LINQ system predicate expression lambda.</returns>
        private Expression<Func<DoctorSchedule, bool>> BuildCriteriaExpression(Guid clinicId, GetAvailableSlotsRequest request)
        {
            // Initialize parameter binding structure representing "s =>" expression root
            var parameter = Expression.Parameter(typeof(DoctorSchedule), "s");
            // Base criteria: s.Doctor.ClinicId == clinicId AND s.WorkDate.Date == request.WorkDate.Date
            var doctorProp = Expression.Property(parameter, "Doctor");
            var clinicIdProp = Expression.Property(doctorProp, "ClinicId");
            var clinicIdLeft = Expression.Equal(clinicIdProp, Expression.Constant(clinicId));
            var workDateProp = Expression.Property(parameter, "WorkDate");
            var dateProp = Expression.Property(workDateProp, "Date");
            var workDateLeft = Expression.Equal(dateProp, Expression.Constant(request.WorkDate.Date));
            Expression combinedBody = Expression.AndAlso(clinicIdLeft, workDateLeft);
            // 1. Filter by Doctor FullName (Case-Insensitive substring matching)
            if (!string.IsNullOrWhiteSpace(request.SearchDoctor))
            {
                var search = request.SearchDoctor.ToLower();
                var userProp = Expression.Property(doctorProp, "User");
                var fullNameProp = Expression.Property(userProp, "FullName");
                var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;
                var fullNameToLower = Expression.Call(fullNameProp, toLowerMethod);
                var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
                var containsExpression = Expression.Call(fullNameToLower, containsMethod, Expression.Constant(search));
                combinedBody = Expression.AndAlso(combinedBody, containsExpression);
            }

            // 2. Filter by Shift Type Enum (Safely parsed from configuration strings)
            if (!string.IsNullOrWhiteSpace(request.ShiftType) && Enum.TryParse<ShiftType>(request.ShiftType, true, out var shiftEnum))
            {
                var shiftTypeProp = Expression.Property(parameter, "ShiftType");
                var shiftExpression = Expression.Equal(shiftTypeProp, Expression.Constant(shiftEnum));
                combinedBody = Expression.AndAlso(combinedBody, shiftExpression);
            }

            // 3. Filter by Specialty Identifier (Safely casted to support Nullable comparison maps)
            if (!string.IsNullOrWhiteSpace(request.SpecialtyId) && request.SpecialtyId != "All" && Guid.TryParse(request.SpecialtyId, out var specialtyGuid))
            {
                var specialtyIdProp = Expression.Property(doctorProp, "SpecialtyId");
                var constantExpression = Expression.Constant(specialtyGuid, typeof(Guid?));
                var specialtyExpression = Expression.Equal(specialtyIdProp, constantExpression);
                combinedBody = Expression.AndAlso(combinedBody, specialtyExpression);
            }
            return Expression.Lambda<Func<DoctorSchedule, bool>>(combinedBody, parameter);
        }

        /// <summary>
        /// Executes the optimized schedule database query including relational entity graphs.
        /// </summary>
        /// <param name="filterExpression">The compiled lambda filters expression.</param>
        /// <returns>A list of resolved <see cref="DoctorSchedule"/> graph tracking segments.</returns>
        private async Task<List<DoctorSchedule>> ExecuteScheduleQuery(Expression<Func<DoctorSchedule, bool>> filterExpression)
        {
            return await _scheduleQueryRepo.FindByCondition(filterExpression, trackChanges: false,
                    s => s.Doctor,
                    s => s.Doctor.User,
                    s => s.Doctor.Specialty!,
                    s => s.Room!,
                    s => s.TimeSlots!)
                .AsQueryable()
                .OrderBy(s => s.ShiftType)
                .ThenBy(s => s.Doctor.User.FullName)
                .ToListAsync();
        }

        /// <summary>
        /// Maps raw schedule tracking entities into structured flat API response matrix arrays.
        /// </summary>
        /// <param name="schedules">The raw source list tracking database values.</param>
        /// <returns>The collection containing formatted <see cref="GetAvailableSlotsResponse"/> items.</returns>
        private List<GetAvailableSlotsResponse> MapToDoctorShiftMatrix(List<DoctorSchedule> schedules)
        {
            var nowLocal = DateTime.UtcNow;
            return schedules.Select(s => new GetAvailableSlotsResponse
            {
                Id = s.Id.ToString(),
                ShiftType = s.ShiftType.ToString().ToUpper(),
                DoctorName = s.Doctor.User.FullName,
                Title = s.Doctor.Title,
                SpecialtyName = s.Doctor.Specialty != null ? s.Doctor.Specialty.Name : "Chưa phân khoa",
                RoomName = s.Room != null ? s.Room.RoomName : "Chưa gán phòng trực",
                Slots = (s.TimeSlots ?? new List<TimeSlot>())
                    .OrderBy(ts => ts.StartTime)
                    .Select(ts =>
        {
            var currentStatus = ts.Status.ToString().ToUpper();
            if (currentStatus == "AVAILABLE")
            {
                if (nowLocal >= ts.StartTime.AddMinutes(30))
                {
                    currentStatus = "BLOCKED";
                }
            }
            return new TimeSlotResponse
            {
                Id = ts.Id.ToString(),
                StartTime = ts.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                EndTime = ts.EndTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                MaxPatients = ts.MaxPatients,
                CurrentPatients = ts.CurrentPatients,
                Status = currentStatus
            };
                }).ToList()
                }).ToList();
        }

        /// <summary>
        /// Wraps the generated matrix result payload inside a standard success framework wrapper.
        /// </summary>
        /// <param name="result">The formatted response items array.</param>
        /// <returns>A structured successful operational <see cref="ApiResponse{T}"/> context.</returns>
        private ApiResponse<List<GetAvailableSlotsResponse>> CreateApiResponse(List<GetAvailableSlotsResponse> result)
        {
            return ApiResponse<List<GetAvailableSlotsResponse>>.Success("APP_MESSAGE_2000", result);
        }
    }
}