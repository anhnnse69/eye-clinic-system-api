using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.ClinicDoctorDiscoveryService.ViewDoctorSlotsServices
{
    /// <summary>
    /// Handles retrieving a doctor's profile and available
    /// time slots for the next 30 days from today.
    /// </summary>
    public class ViewDoctorSlotsService : IViewDoctorSlotsService
    {
        private readonly IRepositoryQueryBase<
            DoctorProfile, Guid, AppDbContext> _doctorRepository;

        private readonly IRepositoryQueryBase<
            DoctorSchedule, Guid, AppDbContext> _scheduleRepository;

        public ViewDoctorSlotsService(
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>
                doctorRepository,
            IRepositoryQueryBase<DoctorSchedule, Guid, AppDbContext>
                scheduleRepository)
        {
            _doctorRepository = doctorRepository;
            _scheduleRepository = scheduleRepository;
        }

        /// <summary>
        /// Main method to process the request: retrieves doctor profile and available slots for the next 30 days.
        /// </summary>
        /// <param name="doctorId">The unique identifier of the doctor.</param>
        /// <returns>An API response containing the doctor's information and available schedule days.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the doctor is not found or is inactive.</exception>
        public async Task<ApiResponse<ViewDoctorSlotsResponse>> Process(
        Guid doctorId)
        {
            var doctor = await GetDoctorOrThrowAsync(doctorId);
            var scheduleDays = await FetchScheduleDaysAsync(doctorId);
            var response = BuildResponse(doctor, scheduleDays);
            return CreateSuccessResponse(response);
        }

        /// <summary>
        /// Retrieves the doctor profile or throws an exception if not found or inactive.
        /// </summary>
        /// <param name="doctorId">The ID of the doctor to fetch.</param>
        /// <returns>The active doctor profile.</returns>
        private async Task<DoctorProfile> GetDoctorOrThrowAsync(Guid doctorId)
        {
            var doctorResult = await FetchDoctorAsync(doctorId);
            return doctorResult.Doctor
                ?? throw new KeyNotFoundException(
                    GeneralCode.APP_MESSAGE_4011.ToString());
        }

        /// <summary>
        /// Creates a successful API response wrapper.
        /// </summary>
        /// <param name="response">The view model containing doctor and schedule data.</param>
        /// <returns>Success API response.</returns>
        private ApiResponse<ViewDoctorSlotsResponse> CreateSuccessResponse(ViewDoctorSlotsResponse response)
        {
            return ApiResponse<ViewDoctorSlotsResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                response);
        }

        /// <summary>
        /// Fetches doctor profile with related entities (User, Specialty, Clinic).
        /// </summary>
        /// <param name="doctorId">The ID of the doctor.</param>
        /// <returns>A result object indicating whether the doctor was found and the profile data.</returns>
        private async Task<DoctorFetchResult> FetchDoctorAsync(Guid doctorId)
        {
            var doctor = await _doctorRepository
                .FindByCondition(d => d.Id == doctorId && d.IsActive)
                .Include(d => d.User)
                .Include(d => d.Specialty)
                .Include(d => d.Clinic)
                .FirstOrDefaultAsync();
            return doctor is null
                ? new DoctorFetchResult(false, null)
                : new DoctorFetchResult(true, doctor);
        }

        /// <summary>
        /// Fetches all schedule days from today up to +30 days that contain at least one available time slot.
        /// </summary>
        /// <param name="doctorId">The ID of the doctor.</param>
        /// <returns>List of schedule days with available slots.</returns>
        private async Task<List<DoctorScheduleDay>> FetchScheduleDaysAsync(Guid doctorId)
        {
            var todayUtc = DateTime.UtcNow.Date;
            var maxDateUtc = todayUtc.AddDays(30);
            var nowUtc = DateTime.UtcNow;
            var schedules = await _scheduleRepository
                .FindByCondition(s =>
                    s.DoctorId == doctorId &&
                    s.WorkDate >= todayUtc &&
                    s.WorkDate <= maxDateUtc)
                .Include(s => s.TimeSlots)
                .OrderBy(s => s.WorkDate)
                .ToListAsync();
            // Map and filter only days that have available slots
            return schedules
                .Select(s => MapScheduleDay(s, nowUtc))
                .Where(d => d.Slots.Count > 0)
                .ToList();
        }

        /// <summary>
        /// Maps a DoctorSchedule entity to DoctorScheduleDay response model,
        /// filtering only available time slots that haven't started yet.
        /// </summary>
        /// <param name="schedule">The schedule entity from database.</param>
        /// <param name="nowUtc">Current UTC time used to filter past slots.</param>
        /// <returns>Mapped schedule day with available slots.</returns>
        private static DoctorScheduleDay MapScheduleDay(DoctorSchedule schedule, DateTime nowUtc)
        {
            var availableSlots = schedule.TimeSlots
                .Where(t =>
                    t.Status == SlotStatus.AVAILABLE &&
                    t.CurrentPatients < t.MaxPatients &&
                    t.StartTime > nowUtc)
                .OrderBy(t => t.StartTime)
                .Select(t => new DoctorTimeSlot
                {
                    SlotId = t.Id,
                    StartTime = t.StartTime,
                    EndTime = t.EndTime,
                    MaxPatients = t.MaxPatients,
                    CurrentPatients = t.CurrentPatients,
                    Remaining = t.MaxPatients - t.CurrentPatients,
                    Status = t.Status.ToString(),
                })
                .ToList();
            return new DoctorScheduleDay
            {
                ScheduleId = schedule.Id,
                WorkDate = DateOnly.FromDateTime(schedule.WorkDate),
                ShiftType = schedule.ShiftType.ToString(),
                Slots = availableSlots,
            };
        }

        /// <summary>
        /// Builds the final response object combining doctor profile and schedule data.
        /// </summary>
        /// <param name="doctor">The doctor profile entity.</param>
        /// <param name="scheduleDays">List of days with available slots.</param>
        /// <returns>Complete response model for the client.</returns>
        private static ViewDoctorSlotsResponse BuildResponse(
            DoctorProfile doctor,
            List<DoctorScheduleDay> scheduleDays) =>
            new()
            {
                DoctorId = doctor.Id,
                FullName = doctor.User.FullName,
                AvatarUrl = doctor.User.AvatarUrl,
                Title = doctor.Title,
                Specialty = doctor.Specialty?.Name,
                ClinicName = doctor.Clinic.Name,
                ClinicAddress = doctor.Clinic.Address,
                ExperienceYears = doctor.ExperienceYears,
                Bio = doctor.Bio,
                RatingAvg = doctor.RatingAvg,
                ReviewCount = doctor.ReviewCount,
                ScheduleDays = scheduleDays,
            };

        /// <summary>
        /// Internal record to wrap fetch result and avoid null reference issues.
        /// </summary>
        private record DoctorFetchResult(bool IsFound, DoctorProfile? Doctor);
    }
}