using System.Security.Claims;
using ECS.Application.Common.Helpers;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.PatientAppointmentManagementServices.CreateAppointmentServices
{
    /// <summary>
    /// Implements core domain orchestration workflows executing authorized patient appointment creation logic bounded within transactional scopes.
    /// </summary>
    public class CreateAppointmentService : ICreateAppointmentService
    {
        private readonly IRepositoryBaseAsync<Appointment, Guid, AppDbContext> _appointmentRepository;
        private readonly IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext> _slotRepository;
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> _doctorRepository;
        private readonly IRepositoryQueryBase<Service, Guid, AppDbContext> _serviceRepository;
        private readonly IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> _patientProfileRepository;
        private readonly IValidator<CreateAppointmentRequest> _validator;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new operational instance of the <see cref="CreateAppointmentService"/> class with required domain boundary infrastructure dependencies.
        /// </summary>
        /// <param name="appointmentRepository">Repository boundary instance for tracking persistent appointment write changes.</param>
        /// <param name="slotRepository">Repository boundary instance for tracking persistent slot state adjustments.</param>
        /// <param name="doctorRepository">Repository boundary instance for querying physical doctor profile records.</param>
        /// <param name="serviceRepository">Repository boundary instance for querying physical healthcare service records.</param>
        /// <param name="patientProfileRepository">Repository boundary instance for querying physical patient relationship profile records.</param>
        /// <param name="validator">The declarative structural request boundary validation contract engine.</param>
        /// <param name="context">The underlying infrastructure entity framework core database contextual session unit.</param>
        /// <param name="httpContextAccessor">The infrastructure environment component capturing localized incoming transport context state streams.</param>
        public CreateAppointmentService(
            IRepositoryBaseAsync<Appointment, Guid, AppDbContext> appointmentRepository,
            IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext> slotRepository,
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepository,
            IRepositoryQueryBase<Service, Guid, AppDbContext> serviceRepository,
            IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> patientProfileRepository,
            IValidator<CreateAppointmentRequest> validator,
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _appointmentRepository = appointmentRepository;
            _slotRepository = slotRepository;
            _doctorRepository = doctorRepository;
            _serviceRepository = serviceRepository;
            _patientProfileRepository = patientProfileRepository;
            _validator = validator;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes the internal transactional business logic data pipeline to validate parameters, check restrictions, and commit a new appointment record.
        /// </summary>
        /// <param name="request">The data container tracking transaction parameters and structural entity keys requested by the presentation layer.</param>
        /// <returns>An <see cref="ApiResponse{CreateAppointmentResponse}"/> enclosing descriptive state transaction payloads alongside system outcomes.</returns>
        public async Task<ApiResponse<CreateAppointmentResponse>> Process(CreateAppointmentRequest request)
        {
            var state = new ExecutionState();

            // Step 1: Validate incoming request data
            ValidateRequest(request, state);

            // Step 2: Extract authenticated user ID from JWT token
            RetrieveAuthenticatedUserId(state);

            // Step 3: Validate and retrieve active doctor profile
            await RetrieveActiveDoctor(request.DoctorId, state);

            // Step 4: Validate and retrieve active service (optional)
            await RetrieveActiveService(request.ServiceId, state);

            // Step 5: Ensure patient profile is accessible by current user
            await EnsurePatientProfileAccess(request.PatientId, state);

            // Step 6: Validate and retrieve bookable time slot
            await RetrieveBookableSlot(request.SlotId, request.DoctorId, state);

            // Step 7: Check for duplicate appointments
            await CheckDuplicateAppointment(request.PatientId, request.SlotId, state);

            // Step 8: Construct appointment entity
            ConstructAppointmentEntity(request, state);

            // Step 9: Persist appointment and update slot capacity
            await PersistAppointmentGraph(state);

            // Step 10: Build and return the response
            return CreateResponse(state);
        }

        /// <summary>
        /// ExecutionState holds all mutable state for the process flow.
        /// All properties are initialized to safe defaults to avoid null checks.
        /// </summary>
        private class ExecutionState
        {
            public bool IsValidationPassed { get; set; } = true;
            public bool IsUserValid { get; set; } = true;
            public bool IsDoctorValid { get; set; } = true;
            public bool IsServiceValid { get; set; } = true;
            public bool IsPatientAccessible { get; set; } = true;
            public bool IsSlotValid { get; set; } = true;
            public bool IsSlotInPast { get; set; } = false;
            public bool IsDuplicateValid { get; set; } = true;
            public bool IsExecutionSuccess { get; set; } = true;
            public bool HasError { get; set; } = false;

            public Guid ActiveUserId { get; set; }
            public DoctorProfile? DoctorProfile { get; set; }
            public Service? Service { get; set; }
            public TimeSlot? TimeSlot { get; set; }
            public Appointment? Appointment { get; set; }

            public string? ErrorCode { get; set; }
        }

        /// <summary>
        /// Evaluates structural input configurations against predefined declarative constraints using the core validation provider engine.
        /// </summary>
        /// <param name="request">The data container tracking transaction parameters and structural entity keys from the presentation boundary.</param>
        /// <param name="state">The mutable pipeline execution context matrix monitoring workflow parameters state mutations.</param>
        private void ValidateRequest(CreateAppointmentRequest request, ExecutionState state)
        {
            var result = _validator.Validate(request);
            state.IsValidationPassed = result.IsValid;
            state.HasError = !result.IsValid;
            state.ErrorCode = result.IsValid ? null : GeneralCode.APP_MESSAGE_4003.ToString();
        }

        /// <summary>
        /// Dispatches claims resolution algorithms to safely decode and map incoming authenticated transport identity token contexts.
        /// </summary>
        /// <param name="state">The mutable pipeline execution context matrix monitoring workflow parameters state mutations.</param>
        private void RetrieveAuthenticatedUserId(ExecutionState state)
        {
            var principalIdValue = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var parseResult = Guid.TryParse(principalIdValue, out var parsedUserId);

            state.IsUserValid = parseResult;
            state.ActiveUserId = parseResult ? parsedUserId : Guid.Empty;
            state.HasError = state.HasError || !parseResult;
            state.ErrorCode = parseResult ? state.ErrorCode : GeneralCode.APP_MESSAGE_4033.ToString();
        }

        /// <summary>
        /// Validates doctor record parameters and related clinic operational status against the central data layer.
        /// </summary>
        /// <param name="doctorId">The unique primary reference identity coordinate token locating the target doctor.</param>
        /// <param name="state">The mutable pipeline execution context matrix monitoring workflow parameters state mutations.</param>
        private async Task RetrieveActiveDoctor(Guid doctorId, ExecutionState state)
        {
            if (state.HasError) return;

            var doctor = await _doctorRepository
                .FindByCondition(d => d.Id == doctorId && d.IsActive)
                .Include(d => d.User)
                .Include(d => d.Clinic)
                .FirstOrDefaultAsync();

            state.DoctorProfile = doctor;
            state.IsDoctorValid = doctor != null && doctor.Clinic.IsActive;
            state.HasError = state.HasError || !state.IsDoctorValid;
            state.ErrorCode = state.IsDoctorValid ? state.ErrorCode : GeneralCode.APP_MESSAGE_4011.ToString();
        }

        /// <summary>
        /// Resolves healthcare service metadata records verifying matching functional clinic infrastructure assignment matrices.
        /// </summary>
        /// <param name="serviceId">The optional system tracking token identifier mapping care options.</param>
        /// <param name="state">The mutable pipeline execution context matrix monitoring workflow parameters state mutations.</param>
        private async Task RetrieveActiveService(Guid? serviceId, ExecutionState state)
        {
            if (state.HasError) return;
            if (!serviceId.HasValue) return;

            var clinicId = state.DoctorProfile?.ClinicId;
            if (!clinicId.HasValue) return;

            var service = await _serviceRepository
                .FindByCondition(s => s.Id == serviceId.Value && s.ClinicId == clinicId.Value && s.IsActive)
                .FirstOrDefaultAsync();

            state.Service = service;
            state.IsServiceValid = service != null;
            state.HasError = state.HasError || !state.IsServiceValid;
            state.ErrorCode = state.IsServiceValid ? state.ErrorCode : GeneralCode.APP_MESSAGE_4044.ToString();
        }

        /// <summary>
        /// Asserts context security policies ensuring relationship paths authorize data sharing interactions between user tokens and patient profiles.
        /// </summary>
        /// <param name="patientId">The physical tracking target identifier mapping active profile structures.</param>
        /// <param name="state">The mutable pipeline execution context matrix monitoring workflow parameters state mutations.</param>
        private async Task EnsurePatientProfileAccess(Guid patientId, ExecutionState state)
        {
            if (state.HasError) return;

            var isAccessible = await PatientProfileAccessHelper.IsProfileAccessibleAsync(
                state.ActiveUserId,
                patientId,
                _patientProfileRepository,
                _context);

            state.IsPatientAccessible = isAccessible;
            state.HasError = state.HasError || !state.IsPatientAccessible;
            state.ErrorCode = state.IsPatientAccessible ? state.ErrorCode : GeneralCode.APP_MESSAGE_4014.ToString();
        }

        /// <summary>
        /// Assesses the requested scheduler availability segment mapping capacity parameters and timeline constraints.
        /// </summary>
        /// <param name="slotId">The timeline grid cell checkpoint tracking primary identifier value token.</param>
        /// <param name="doctorId">The assigned doctor profile key used to safeguard operational calendar boundaries.</param>
        /// <param name="state">The mutable pipeline execution context matrix monitoring workflow parameters state mutations.</param>
        private async Task RetrieveBookableSlot(Guid slotId, Guid doctorId, ExecutionState state)
        {
            if (state.HasError) return;

            var slot = await _slotRepository
                .FindByCondition(s => s.Id == slotId, trackChanges: true)
                .Include(s => s.Schedule)
                .FirstOrDefaultAsync();

            if (slot is null || slot.Schedule.DoctorId != doctorId)
            {
                state.IsSlotValid = false;
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4006.ToString();
                return;
            }

            if (slot.StartTime <= DateTime.Now)
            {
                state.IsSlotValid = false;
                state.IsSlotInPast = true;
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4005.ToString();
                return;
            }

            if (slot.Status != SlotStatus.AVAILABLE || slot.CurrentPatients >= slot.MaxPatients)
            {
                state.IsSlotValid = false;
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4007.ToString();
                return;
            }

            state.TimeSlot = slot;
            state.IsSlotValid = true;
        }

        /// <summary>
        /// Executes analytical concurrency verification querying storage indices for active overlapping duplicate patient appointments.
        /// </summary>
        /// <param name="patientId">The specific patient tracking structural token identifier context.</param>
        /// <param name="slotId">The targeted scheduler time matrix primary reference block key identifier.</param>
        /// <param name="state">The mutable pipeline execution context matrix monitoring workflow parameters state mutations.</param>
        private async Task CheckDuplicateAppointment(Guid patientId, Guid slotId, ExecutionState state)
        {
            if (state.HasError) return;

            var duplicateExists = await _appointmentRepository
                .FindByCondition(a => a.PatientId == patientId
                    && a.SlotId == slotId
                    && a.Status != AppointmentStatus.CANCELLED)
                .AnyAsync();

            state.IsDuplicateValid = !duplicateExists;
            state.HasError = state.HasError || !state.IsDuplicateValid;
            state.ErrorCode = state.IsDuplicateValid ? state.ErrorCode : GeneralCode.APP_MESSAGE_4015.ToString();
        }

        /// <summary>
        /// Constructs a transient concrete instance of the appointment domain core entity using successfully validated configuration frameworks.
        /// </summary>
        /// <param name="request">The parameters containing initial presentation layer values.</param>
        /// <param name="state">The mutable pipeline execution context matrix monitoring workflow parameters state mutations.</param>
        private void ConstructAppointmentEntity(CreateAppointmentRequest request, ExecutionState state)
        {
            if (state.HasError || state.TimeSlot == null) return;

            state.Appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = request.PatientId,
                DoctorId = request.DoctorId,
                SlotId = request.SlotId,
                ServiceId = request.ServiceId,
                AppointmentDate = state.TimeSlot.StartTime,
                Symptoms = request.Symptoms,
                Status = AppointmentStatus.PENDING,
                BookingSource = "ONLINE",
                CreatedById = state.ActiveUserId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        /// <summary>
        /// Commits the structured entity record graph atomically to the persistent database store while incrementing timeline vacancy metrics.
        /// </summary>
        /// <param name="state">The mutable pipeline execution context matrix monitoring workflow parameters state mutations.</param>
        private async Task PersistAppointmentGraph(ExecutionState state)
        {
            if (state.HasError || state.Appointment == null || state.TimeSlot == null)
            {
                state.IsExecutionSuccess = false;
                return;
            }

            using var transactionalScope = await _appointmentRepository.BeginTransactionAsync();

            try
            {
                await _appointmentRepository.CreateAsync(state.Appointment);

                state.TimeSlot.CurrentPatients += 1;
                if (state.TimeSlot.CurrentPatients >= state.TimeSlot.MaxPatients)
                {
                    state.TimeSlot.Status = SlotStatus.BOOKED;
                }

                await _appointmentRepository.SaveChangesAsync();
                await transactionalScope.CommitAsync();

                state.IsExecutionSuccess = true;
            }
            catch (Exception)
            {
                await transactionalScope.RollbackAsync();
                state.IsExecutionSuccess = false;
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_5001.ToString();
            }
        }

        /// <summary>
        /// Evaluates transaction processing checkpoints to compile an structured network-safe outcome application response serialization payload.
        /// </summary>
        /// <param name="state">The mutable pipeline execution context matrix monitoring workflow parameters state mutations.</param>
        /// <returns>An endpoint transport safe formatted serialization envelope containing specific process diagnostic state indicators.</returns>
        private ApiResponse<CreateAppointmentResponse> CreateResponse(ExecutionState state)
        {
            if (state.HasError)
            {
                var errorCode = state.ErrorCode ?? GeneralCode.APP_MESSAGE_4001.ToString();
                return ApiResponse<CreateAppointmentResponse>.Fail(errorCode);
            }

            var doctor = state.DoctorProfile!;
            var slot = state.TimeSlot!;
            var appointment = state.Appointment!;
            var service = state.Service;

            var response = new CreateAppointmentResponse
            {
                Id_appointment = appointment.Id,
                DoctorName = doctor.User.FullName,
                ClinicName = doctor.Clinic.Name,
                ServiceName = service?.ServiceName ?? "Khám tổng quát",
                AppointmentDate = slot.StartTime.ToString("dd/MM/yyyy"),
                TimeSlot = $"{slot.StartTime.ToString("HH:mm")} - {slot.EndTime.ToString("HH:mm")}",
                Status = appointment.Status.ToString(),
                DepositAmount = appointment.DepositAmount,
                DepositPaid = appointment.DepositPaid,
                BookingSource = appointment.BookingSource
            };

            return ApiResponse<CreateAppointmentResponse>.Success(
                GeneralCode.APP_MESSAGE_2001.ToString(),
                response);
        }
    }
}