using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.PatientAppointmentManagementServices.ClinicSlotServices
{
    /// <summary>
    /// Handles the business logic for retrieving available time slots within a specific clinic, formatted for booking selections.
    /// </summary>
    public class ClinicSlotService : IClinicSlotService
    {
        private readonly IRepositoryQueryBase<Clinic, Guid, AppDbContext> _clinicRepository;
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> _doctorRepository;
        private readonly IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext> _slotRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClinicSlotService"/> class with required infrastructure boundaries.
        /// </summary>
        /// <param name="clinicRepository">Repository boundary instance for querying physical clinic records.</param>
        /// <param name="doctorRepository">Repository boundary instance for querying physical doctor profile records.</param>
        /// <param name="slotRepository">Repository boundary instance for querying physical time slot records.</param>
        public ClinicSlotService(
            IRepositoryQueryBase<Clinic, Guid, AppDbContext> clinicRepository,
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepository,
            IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext> slotRepository)
        {
            _clinicRepository = clinicRepository;
            _doctorRepository = doctorRepository;
            _slotRepository = slotRepository;
        }

        /// <summary>
        /// Processes the internal data pipeline workflow to validate clinic status, parse date, and return available time slots.
        /// </summary>
        /// <param name="clinicId">The unique identifier mapping the targeted underlying clinic entity.</param>
        /// <param name="date">The date to check availability for (format: yyyy-MM-dd).</param>
        /// <param name="serviceId">Optional service identifier to filter slots.</param>
        /// <returns>An <see cref="ApiResponse{List{ClinicSlotResponse}}"/> enclosing descriptive state payloads alongside configuration boundaries.</returns>
        public async Task<ApiResponse<List<ClinicSlotResponse>>> Process(Guid clinicId, string date, Guid? serviceId = null)
        {
            // Step 1: Search operational relational databases to verify target clinic profile existence and status
            var (clinic, isClinicValid) = await ValidateClinicProfile(clinicId);

            // Step 2: Parse the date string and validate format
            bool isDateValid = TryParseDate(date, out var selectedDate);

            // Step 3: Query underlying target physical data storage blocks executing evaluation and ordering algorithms
            var doctors = await GetDoctors(clinicId, isClinicValid);

            // Step 4: Generate time slots from clinic working hours with doctor availability checking
            var slots = GenerateSlotsFromWorkingHours(clinic, selectedDate, doctors, isClinicValid, isDateValid);

            // Step 5: Evaluate processing parameters and package state structures dynamically to handle execution outcomes
            return CreateResponse(slots, isClinicValid, isDateValid);
        }

        /// <summary>
        /// Verifies whether the specified clinic identifier maps onto an existing, active core business entity.
        /// </summary>
        /// <param name="clinicId">The unique enterprise primary reference coordinates identifier token.</param>
        /// <returns>A tuple containing the clinic entity and a state indicator returning true if verification matches baseline benchmarks.</returns>
        private async Task<(Clinic? Clinic, bool IsClinicValid)> ValidateClinicProfile(Guid clinicId)
        {
            var clinic = await _clinicRepository
                .FindByCondition(c => c.Id == clinicId && c.IsActive)
                .FirstOrDefaultAsync();

            return (clinic, clinic != null);
        }

        /// <summary>
        /// Attempts to parse the date string into a DateTime object.
        /// </summary>
        /// <param name="date">The date string to parse.</param>
        /// <param name="selectedDate">The parsed DateTime object if successful.</param>
        /// <returns>True if parsing was successful, false otherwise.</returns>
        private bool TryParseDate(string date, out DateTime selectedDate)
        {
            return DateTime.TryParse(date, out selectedDate);
        }

        /// <summary>
        /// Retrieves all active doctor profiles associated with the specified clinic.
        /// </summary>
        /// <param name="clinicId">The clinic identifier used to filter doctor records.</param>
        /// <param name="isClinicValid">Guard condition preventing database load pipelines on validation failure states.</param>
        /// <returns>A structured list of physical domain doctor profile entity models.</returns>
        private async Task<List<DoctorProfile>> GetDoctors(Guid clinicId, bool isClinicValid)
        {
            if (!isClinicValid)
            {
                return new List<DoctorProfile>();
            }

            return await _doctorRepository
                .FindByCondition(d => d.ClinicId == clinicId && d.IsActive)
                .ToListAsync();
        }

        /// <summary>
        /// Generates time slots from clinic working hours and checks doctor availability for each slot.
        /// </summary>
        /// <param name="clinic">The clinic entity containing working hours configuration.</param>
        /// <param name="selectedDate">The selected date to generate slots for.</param>
        /// <param name="doctors">List of active doctors in the clinic.</param>
        /// <param name="isClinicValid">Indicates whether the clinic is valid.</param>
        /// <param name="isDateValid">Indicates whether the date is valid.</param>
        /// <param name="slotDurationMinutes">Duration of each slot in minutes. Default is 30 minutes.</param>
        /// <returns>A list of clinic slot responses with availability status.</returns>
        private List<ClinicSlotResponse> GenerateSlotsFromWorkingHours(
            Clinic? clinic,
            DateTime selectedDate,
            List<DoctorProfile> doctors,
            bool isClinicValid,
            bool isDateValid,
            int slotDurationMinutes = 30)
        {
            // Early return if validation fails
            if (!isClinicValid || !isDateValid || clinic == null)
            {
                return new List<ClinicSlotResponse>();
            }

            var slots = new List<ClinicSlotResponse>();
            var openTime = clinic.OpenTime.ToTimeSpan();
            var closeTime = clinic.CloseTime.ToTimeSpan();

            var current = selectedDate.Date.Add(openTime);
            var end = selectedDate.Date.Add(closeTime);

            // Iterate through each time slot within working hours
            while (current < end)
            {
                var slotEnd = current.AddMinutes(slotDurationMinutes);

                if (slotEnd <= end)
                {
                    // Check if any doctor is available for this slot
                    var (hasAvailableDoctor, availableDoctorId) = CheckDoctorAvailability(doctors, current);

                    // Create slot response with format: clinic_{clinicId}_{date}_{startTime}
                    var slot = new ClinicSlotResponse
                    {
                        Id = $"clinic_{clinic.Id}_{selectedDate:yyyy-MM-dd}_{current:HH:mm}",
                        StartTime = current.ToString("HH:mm"),
                        EndTime = slotEnd.ToString("HH:mm"),
                        IsAvailable = hasAvailableDoctor,
                        ClinicId = clinic.Id,
                        Date = selectedDate.ToString("yyyy-MM-dd"),
                        DoctorId = availableDoctorId,
                        HasAvailableDoctor = hasAvailableDoctor
                    };

                    slots.Add(slot);
                }

                current = slotEnd;
            }

            return slots;
        }

        /// <summary>
        /// Checks if any doctor is available for a specific time slot.
        /// Uses _slotRepository to query directly from database for accurate time comparison.
        /// </summary>
        /// <param name="doctors">List of doctors to check availability for.</param>
        /// <param name="startTime">The start time of the slot to check.</param>
        /// <returns>A tuple containing availability status and the available doctor ID if found.</returns>
        private (bool HasAvailableDoctor, Guid? AvailableDoctorId) CheckDoctorAvailability(
            List<DoctorProfile> doctors,
            DateTime startTime)
        {
            // Early return if no doctors available
            if (!doctors.Any())
            {
                return (false, null);
            }

            // Extract list of doctor IDs for the query
            var doctorIds = doctors.Select(d => d.Id).ToList();

            // Query database once to find all available slots matching the start time
            // This approach is more efficient than loading all data into memory
            var availableSlots = _slotRepository
                .FindByCondition(s => doctorIds.Contains(s.Schedule.DoctorId)
                    && s.StartTime == startTime
                    && s.Status == SlotStatus.AVAILABLE
                    && (s.MaxPatients - s.CurrentPatients) > 0)
                .Select(s => s.Schedule.DoctorId)
                .ToList();

            // Return the first available doctor if any
            if (availableSlots.Any())
            {
                return (true, availableSlots.First());
            }

            return (false, null);
        }

        /// <summary>
        /// Analyzes state logic monitoring variables to determine outcome layout packaging choices.
        /// </summary>
        /// <param name="slots">The structured data response payload list projected from database layers.</param>
        /// <param name="isClinicValid">Indicates whether clinic token extraction verification matched expectations successfully.</param>
        /// <param name="isDateValid">Indicates whether the date parsing was successful.</param>
        /// <returns>A standardized application payload container detailed for transport serialization layers.</returns>
        private ApiResponse<List<ClinicSlotResponse>> CreateResponse(
            List<ClinicSlotResponse> slots,
            bool isClinicValid,
            bool isDateValid)
        {
            if (!isClinicValid)
            {
                return ApiResponse<List<ClinicSlotResponse>>.Fail(
                    GeneralCode.APP_MESSAGE_4045.ToString()); // Clinic Not Found or Inactive
            }

            if (!isDateValid)
            {
                return ApiResponse<List<ClinicSlotResponse>>.Fail(
                    GeneralCode.APP_MESSAGE_4052.ToString()); // Invalid Date Format
            }

            return ApiResponse<List<ClinicSlotResponse>>.Success(
                GeneralCode.APP_MESSAGE_2001.ToString(),
                slots);
        }
    }
}