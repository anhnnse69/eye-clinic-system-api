using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Application.Services.PatientAppointmentManagementServices.CreateAppointmentServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.PatientAppointmentManagementServices.CreateAppointmentByClinicServices
{
    /// <summary>
    /// Handles the business logic for creating patient appointments based on clinic working hours without specific doctor selection.
    /// </summary>
    public class CreateAppointmentByClinicService : ICreateAppointmentByClinicService
    {
        private readonly ICreateAppointmentService _createAppointmentService;
        private readonly IRepositoryQueryBase<Clinic, Guid, AppDbContext> _clinicRepository;
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> _doctorRepository;
        private readonly IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext> _slotRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateAppointmentByClinicService"/> class with required infrastructure boundaries.
        /// </summary>
        /// <param name="createAppointmentService">The underlying appointment creation service used for final persistence.</param>
        /// <param name="clinicRepository">Repository boundary instance for querying physical clinic records.</param>
        /// <param name="doctorRepository">Repository boundary instance for querying physical doctor profile records.</param>
        /// <param name="slotRepository">Repository boundary instance for querying physical time slot records.</param>
        /// <param name="httpContextAccessor">HTTP context accessor for extracting authenticated user claims.</param>
        public CreateAppointmentByClinicService(
            ICreateAppointmentService createAppointmentService,
            IRepositoryQueryBase<Clinic, Guid, AppDbContext> clinicRepository,
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepository,
            IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext> slotRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _createAppointmentService = createAppointmentService;
            _clinicRepository = clinicRepository;
            _doctorRepository = doctorRepository;
            _slotRepository = slotRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes the internal transactional business logic data pipeline to validate parameters, find available doctor, and commit a new appointment record.
        /// </summary>
        /// <param name="request">The appointment creation request containing clinic ID, patient ID, slot ID, and optional service/symptoms.</param>
        public async Task<ApiResponse<CreateAppointmentByClinicResponse>> Process(CreateAppointmentByClinicRequest request)
        {
            // Initialize status tracking flags
            bool isClinicValid = true;
            bool isSlotValid = true;
            bool isDoctorAvailable = true;

            // Step 1: Extract identity information metrics from active token pipelines
            var userId = RetrieveUserId();

            // Step 2: Validate clinic existence and active status
            var (clinic, updatedClinicValid) = await ValidateClinicProfile(request.ClinicId, isClinicValid);
            isClinicValid = updatedClinicValid;

            // Step 3: Parse and validate composite slot identifier
            var (slotDateTime, updatedSlotValid) = ParseSlotIdentifier(request.SlotId, request.ClinicId, isClinicValid);
            isSlotValid = updatedSlotValid;

            // Step 4: Validate that the requested time falls within clinic working hours
            isSlotValid = ValidateWorkingHours(slotDateTime, clinic, isSlotValid);

            // Step 5: Find available doctor for the requested time slot
            var (availableDoctor, updatedDoctorAvailableFromFind) = await FindAvailableDoctor(request.ClinicId, slotDateTime, isSlotValid);
            isDoctorAvailable = updatedDoctorAvailableFromFind;

            // Step 6: Retrieve available time slot for the selected doctor
            var (timeSlot, updatedDoctorAvailableFromSlot) = await RetrieveAvailableTimeSlot(availableDoctor, slotDateTime, isDoctorAvailable);
            isDoctorAvailable = updatedDoctorAvailableFromSlot;

            // Step 7: Delegate to existing appointment creation service
            var result = await DelegateAppointmentCreation(request, availableDoctor, timeSlot, isDoctorAvailable);

            // Step 8: Package contextual payloads dynamically to manage outcome states
            return CreateResponse(result, isClinicValid, isSlotValid, isDoctorAvailable);
        }

        /// <summary>
        /// Resolves the logged-in user credentials via claims identity mapping streams.
        /// </summary>
        /// <returns>The authenticated user's unique identifier, or Guid.Empty if not found.</returns>
        private Guid RetrieveUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Guid.Empty;
            }

            return userId;
        }

        /// <summary>
        /// Validates whether the specified clinic identifier maps onto an existing, active core business entity.
        /// </summary>
        /// <param name="clinicId">The unique enterprise primary reference coordinates identifier token.</param>
        /// <param name="currentClinicState">The current validation state flag from the pipeline.</param>
        /// <returns>A tuple containing the clinic entity and a state indicator returning true if verification matches baseline benchmarks.</returns>
        private async Task<(Clinic? ClinicNode, bool IsClinicValid)> ValidateClinicProfile(Guid clinicId, bool currentClinicState)
        {
            if (!currentClinicState)
            {
                return (null, false);
            }

            var clinic = await _clinicRepository
                .FindByCondition(c => c.Id == clinicId && c.IsActive)
                .FirstOrDefaultAsync();

            return (clinic, clinic != null);
        }

        /// <summary>
        /// Parses composite slot identifier into date and time components.
        /// </summary>
        /// <param name="slotId">The composite slot identifier in format "clinic_{clinicId}_{date}_{startTime}".</param>
        /// <param name="clinicId">The expected clinic ID for validation.</param>
        /// <param name="currentClinicState">The current validation state flag from the pipeline.</param>
        private (DateTime SlotDateTime, bool IsSlotValid) ParseSlotIdentifier(string slotId, Guid clinicId, bool currentClinicState)
        {
            if (!currentClinicState || string.IsNullOrWhiteSpace(slotId))
            {
                return (DateTime.MinValue, false);
            }

            var parts = slotId.Split('_');
            if (parts.Length != 4 || parts[0] != "clinic")
            {
                return (DateTime.MinValue, false);
            }

            if (!Guid.TryParse(parts[1], out var parsedClinicId) || parsedClinicId != clinicId)
            {
                return (DateTime.MinValue, false);
            }

            if (!DateTime.TryParse(parts[2], out var slotDate) || !TimeSpan.TryParse(parts[3], out var slotTime))
            {
                return (DateTime.MinValue, false);
            }

            return (slotDate.Date.Add(slotTime), true);
        }

        /// <summary>
        /// Validates that the requested time falls within clinic working hours.
        /// </summary>
        /// <param name="slotDateTime">The date and time of the requested slot.</param>
        /// <param name="clinic">The clinic entity containing working hours configuration.</param>
        /// <param name="currentSlotState">The current validation state flag from the pipeline.</param>
        private bool ValidateWorkingHours(DateTime slotDateTime, Clinic? clinic, bool currentSlotState)
        {
            if (!currentSlotState || clinic == null)
            {
                return false;
            }

            var slotTime = slotDateTime.TimeOfDay;
            var openTime = clinic.OpenTime.ToTimeSpan();
            var closeTime = clinic.CloseTime.ToTimeSpan();

            return slotTime >= openTime && slotTime < closeTime;
        }

        /// <summary>
        /// Finds an available doctor for the requested time slot within the clinic.
        /// </summary>
        /// <param name="clinicId">The clinic identifier.</param>
        /// <param name="startTime">The requested start time of the slot.</param>
        /// <param name="currentSlotState">The current validation state flag from the pipeline.</param>
        private async Task<(DoctorProfile? DoctorNode, bool IsDoctorAvailable)> FindAvailableDoctor(Guid clinicId, DateTime startTime, bool currentSlotState)
        {
            if (!currentSlotState)
            {
                return (null, false);
            }

            var doctors = await _doctorRepository
                .FindByCondition(d => d.ClinicId == clinicId && d.IsActive)
                .ToListAsync();

            foreach (var doctor in doctors)
            {
                var hasAvailableSlot = await _slotRepository
                    .FindByCondition(s => s.Schedule.DoctorId == doctor.Id
                        && s.StartTime == startTime
                        && s.Status == SlotStatus.AVAILABLE)
                    .AnyAsync();

                if (hasAvailableSlot)
                {
                    return (doctor, true);
                }
            }

            return (null, false);
        }

        /// <summary>
        /// Retrieves available time slot for the specified doctor and time.
        /// </summary>
        /// <param name="availableDoctor">The available doctor profile.</param>
        /// <param name="startTime">The requested start time of the slot.</param>
        /// <param name="currentDoctorState">The current doctor availability state flag from the pipeline.</param>
        private async Task<(TimeSlot? TimeSlotNode, bool IsDoctorAvailable)> RetrieveAvailableTimeSlot(DoctorProfile? availableDoctor, DateTime startTime, bool currentDoctorState)
        {
            if (!currentDoctorState || availableDoctor == null)
            {
                return (null, false);
            }

            var timeSlot = await _slotRepository
                .FindByCondition(s => s.Schedule.DoctorId == availableDoctor.Id
                    && s.StartTime == startTime
                    && s.Status == SlotStatus.AVAILABLE)
                .Include(s => s.Schedule)
                .FirstOrDefaultAsync();

            return (timeSlot, timeSlot != null);
        }

        /// <summary>
        /// Safely handles the delegation to the underlying internal creation service using standard orchestration guards.
        /// </summary>
        /// <param name="request">The original appointment creation request.</param>
        /// <param name="availableDoctor">The available doctor profile to assign.</param>
        /// <param name="timeSlot">The time slot record to book.</param>
        /// <param name="currentDoctorState">The current doctor availability state flag from the pipeline.</param>
        private async Task<ApiResponse<CreateAppointmentResponse>?> DelegateAppointmentCreation(
            CreateAppointmentByClinicRequest request,
            DoctorProfile? availableDoctor,
            TimeSlot? timeSlot,
            bool currentDoctorState)
        {
            if (!currentDoctorState || availableDoctor == null || timeSlot == null)
            {
                return null;
            }

            var createRequest = new CreateAppointmentRequest
            {
                PatientId = request.PatientId,
                DoctorId = availableDoctor.Id,
                SlotId = timeSlot.Id,
                ServiceId = request.ServiceId,
                Symptoms = request.Symptoms
            };

            return await _createAppointmentService.Process(createRequest);
        }

        /// <summary>
        /// Resolves transaction outcome wrappers packing serialization nodes safely.
        /// </summary>
        /// <param name="result">The result from the underlying appointment creation service.</param>
        /// <param name="isClinicValid">Indicates whether clinic validation passed.</param>
        /// <param name="isSlotValid">Indicates whether slot validation passed.</param>
        /// <param name="isDoctorAvailable">Indicates whether a doctor is available.</param>
        private ApiResponse<CreateAppointmentByClinicResponse> CreateResponse(
            ApiResponse<CreateAppointmentResponse>? result,
            bool isClinicValid,
            bool isSlotValid,
            bool isDoctorAvailable)
        {
            var errorResponse = FilterSystemicValidationFailures(result, isClinicValid, isSlotValid, isDoctorAvailable);
            if (errorResponse != null)
            {
                return errorResponse;
            }

            var response = new CreateAppointmentByClinicResponse
            {
                Id_appointment = result!.Data!.Id_appointment,
                DoctorName = result.Data.DoctorName,
                ClinicName = result.Data.ClinicName,
                ServiceName = result.Data.ServiceName,
                AppointmentDate = result.Data.AppointmentDate,
                TimeSlot = result.Data.TimeSlot,
                Status = result.Data.Status,
                DepositAmount = result.Data.DepositAmount,
                DepositPaid = result.Data.DepositPaid,
                BookingSource = result.Data.BookingSource
            };

            return ApiResponse<CreateAppointmentByClinicResponse>.Success(
                GeneralCode.APP_MESSAGE_2001.ToString(),
                response);
        }

        /// <summary>
        /// Analyzes runtime flags status metrics to convert issues directly into clear system response error structures.
        /// </summary>
        /// <param name="result">The result from the underlying appointment creation service.</param>
        /// <param name="isClinicValid">Indicates whether clinic validation passed.</param>
        /// <param name="isSlotValid">Indicates whether slot validation passed.</param>
        /// <param name="isDoctorAvailable">Indicates whether a doctor is available.</param>
        private ApiResponse<CreateAppointmentByClinicResponse>? FilterSystemicValidationFailures(
            ApiResponse<CreateAppointmentResponse>? result,
            bool isClinicValid,
            bool isSlotValid,
            bool isDoctorAvailable)
        {
            if (!isClinicValid)
            {
                return ApiResponse<CreateAppointmentByClinicResponse>.Fail(GeneralCode.APP_MESSAGE_4045.ToString());
            }
            if (!isSlotValid)
            {
                return ApiResponse<CreateAppointmentByClinicResponse>.Fail(GeneralCode.APP_MESSAGE_4052.ToString());
            }
            if (!isDoctorAvailable)
            {
                return ApiResponse<CreateAppointmentByClinicResponse>.Fail(GeneralCode.APP_MESSAGE_4011.ToString());
            }
            if (result == null || result.Data == null)
            {
                return ApiResponse<CreateAppointmentByClinicResponse>.Fail(GeneralCode.APP_MESSAGE_5000.ToString());
            }
            return null;
        }
    }
}