using ECS.Application.Common.Helpers;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorScheduleManagementServices.ClinicShiftServices
{
    /// <summary>
    /// Reads the clinic tied to a receptionist and computes its shift
    /// time ranges using the same logic as schedule creation, so the
    /// ranges shown to the receptionist always match the ranges actually
    /// used to generate time slots.
    /// </summary>
    public class ClinicShiftService : IClinicShiftService
    {
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepo;
        private readonly IRepositoryQueryBase<Clinic, Guid, AppDbContext> _clinicRepo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClinicShiftService"/> class.
        /// Injecting required read-only repositories via Dependency Injection (DI).
        /// </summary>
        /// <param name="staffClinicRepo">Repository to query and validate receptionist's clinic assignment.</param>
        /// <param name="clinicRepo">Repository to query and validate clinic information.</param>
        public ClinicShiftService(
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepo,
            IRepositoryQueryBase<Clinic, Guid, AppDbContext> clinicRepo)
        {
            _staffClinicRepo = staffClinicRepo;
            _clinicRepo = clinicRepo;
        }

        /// <summary>
        /// Orchestrates the business workflow to retrieve shift ranges for a receptionist.
        /// </summary>
        /// <param name="receptionistUserId">The ID of the receptionist user.</param>
        /// <returns>A list of shift range items with start and end times.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when receptionist or clinic is not found.</exception>
        public async Task<ApiResponse<List<ShiftRangeItem>>> Process(Guid receptionistUserId)
        {
            // 1. Resolve clinic for the receptionist
            var clinic = await ResolveReceptionistClinicAsync(receptionistUserId);
            // 2. Build shift ranges based on clinic operating hours
            var ranges = BuildShiftRanges(clinic);
            // 3. Convert to response items
            var response = MapToShiftRangeItems(ranges);
            // 4. Return success response
            return CreateSuccessResponse(response);
        }

        /// <summary>
        /// Resolves the active clinic associated with the receptionist.
        /// </summary>
        /// <param name="receptionistUserId">The ID of the receptionist user.</param>
        /// <returns>The active clinic entity.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when receptionist has no active clinic assignment.</exception>
        private async Task<Clinic> ResolveReceptionistClinicAsync(Guid receptionistUserId)
        {
            var staffClinic = await _staffClinicRepo
                .FindByCondition(sc => sc.UserId == receptionistUserId && sc.IsActive)
                .FirstOrDefaultAsync();
            if (staffClinic is null)
                throw new KeyNotFoundException(GeneralCode.APP_MESSAGE_4008.ToString());
            var clinic = await _clinicRepo
                .FindByCondition(c => c.Id == staffClinic.ClinicId && c.IsActive)
                .FirstOrDefaultAsync();
            if (clinic is null)
                throw new KeyNotFoundException(GeneralCode.APP_MESSAGE_4008.ToString());
            return clinic;
        }

        /// <summary>
        /// Builds shift ranges based on the clinic's operating hours.
        /// </summary>
        /// <param name="clinic">The clinic entity with operating hours.</param>
        /// <returns>A dictionary mapping shift types to their start and end times.</returns>
        private static Dictionary<ShiftType, (TimeOnly Start, TimeOnly End)> BuildShiftRanges(Clinic clinic)
        {
            return ScheduleHelper.BuildShiftRanges(clinic.OpenTime, clinic.CloseTime);
        }

        /// <summary>
        /// Maps shift ranges dictionary to a list of ShiftRangeItem DTOs.
        /// </summary>
        /// <param name="ranges">Dictionary of shift ranges.</param>
        /// <returns>List of ShiftRangeItem ordered by start time.</returns>
        private static List<ShiftRangeItem> MapToShiftRangeItems(
            Dictionary<ShiftType, (TimeOnly Start, TimeOnly End)> ranges)
        {
            return ranges
                .Select(kv => new ShiftRangeItem
                {
                    ShiftType = kv.Key,
                    StartTime = kv.Value.Start,
                    EndTime = kv.Value.End,
                })
                .OrderBy(r => r.StartTime)
                .ToList();
        }

        /// <summary>
        /// Creates a successful API response with the shift range items.
        /// </summary>
        /// <param name="items">List of shift range items.</param>
        /// <returns>API response containing the shift ranges.</returns>
        private static ApiResponse<List<ShiftRangeItem>> CreateSuccessResponse(List<ShiftRangeItem> items)
        {
            return ApiResponse<List<ShiftRangeItem>>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                items);
        }
    }
}