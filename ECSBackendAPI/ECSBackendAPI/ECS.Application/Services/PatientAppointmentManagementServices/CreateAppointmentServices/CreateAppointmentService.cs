using System.Security.Claims;
using ECS.Application.Common.Helpers;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.PatientAppointmentManagementServices.CreateAppointmentServices
{
    /// <summary>
    /// Implements domain process pipelines to create appointments with validation and transaction management.
    /// </summary>
    public class CreateAppointmentService : ICreateAppointmentService
    {
        private readonly IRepositoryBaseAsync<Appointment, Guid, AppDbContext> _appointmentRepository;
        private readonly IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext> _slotRepository;
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> _doctorRepository;
        private readonly IRepositoryQueryBase<Service, Guid, AppDbContext> _serviceRepository;
        private readonly IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> _patientProfileRepository;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new operational instance with required repository dependencies.
        /// </summary>
        /// <param name="appointmentRepository">Repository boundary instance for tracking persistent appointment write changes.</param>
        /// <param name="slotRepository">Repository boundary instance for tracking persistent slot state adjustments.</param>
        /// <param name="doctorRepository">Repository boundary instance for querying physical doctor profile records.</param>
        /// <param name="serviceRepository">Repository boundary instance for querying physical healthcare service records.</param>
        /// <param name="patientProfileRepository">Repository boundary instance for querying physical patient relationship profile records.</param>
        /// <param name="context">The underlying infrastructure entity framework core database contextual session unit.</param>
        /// <param name="httpContextAccessor">The infrastructure environment component capturing localized incoming transport context state streams.</param>
        public CreateAppointmentService(
            IRepositoryBaseAsync<Appointment, Guid, AppDbContext> appointmentRepository,
            IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext> slotRepository,
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepository,
            IRepositoryQueryBase<Service, Guid, AppDbContext> serviceRepository,
            IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> patientProfileRepository,
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _appointmentRepository = appointmentRepository;
            _slotRepository = slotRepository;
            _doctorRepository = doctorRepository;
            _serviceRepository = serviceRepository;
            _patientProfileRepository = patientProfileRepository;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Core orchestration handling transactional logic to create appointments with validation.
        /// </summary>
        /// <param name="request">The parameters containing appointment data.</param>
        /// <returns>An <see cref="ApiResponse{CreateAppointmentResponse}"/> enclosing state payloads.</returns>
        public async Task<ApiResponse<CreateAppointmentResponse>> Process(CreateAppointmentRequest request)
        {
            // Initialize status tracking flags tracking pipeline mutations safely without memory ref constraints
            var validationResult = new ValidationResult
            {
                IsUserValid = true,
                IsDoctorValid = true,
                IsServiceValid = true,
                IsPatientAccessible = true,
                IsSlotValid = true,
                IsDuplicateValid = true,
                IsTransactionSuccess = true
            };

            // Step 1: Extract Authenticated User ID from contextual claims identity tokens
            var activeUserId = RetrieveAuthenticatedUserId(validationResult);

            // Step 2: Validate and retrieve doctor profile metadata from database stores
            var doctorProfile = await RetrieveActiveDoctor(request.DoctorId, validationResult);

            // Step 3: Validate and retrieve service information within matching clinic boundaries
            var serviceEntity = await RetrieveActiveService(
                request.ServiceId,
                doctorProfile?.ClinicId,
                validationResult);

            // Step 4: Ensure target patient profile is fully accessible by current user context boundaries
            await EnsurePatientProfileAccess(request.PatientId, activeUserId, validationResult);

            // Step 5: Validate and retrieve bookable timeline chronological availability slot structures
            var timeSlot = await RetrieveBookableSlot(request.SlotId, request.DoctorId, validationResult);

            // Step 6: Check for parallel active duplicates across identical patient timelines
            await CheckDuplicateAppointment(request.PatientId, request.SlotId, validationResult);

            // Step 7: Build persistent appointment domain core entity records from parameters
            var appointmentEntity = ConstructAppointmentEntity(
                request, activeUserId, timeSlot, validationResult);

            // Step 8: Persist transactional graphics records and update capacity values atomically
            var (committedAppointment, executionSuccess) = await PersistAppointmentGraph(
                appointmentEntity, timeSlot, validationResult);

            // Step 9: Compile data objects onto decoupled final payload container structures
            return CreateResponse(
                committedAppointment, doctorProfile, serviceEntity, timeSlot,
                validationResult, executionSuccess);
        }

        /// <summary>
        /// Resolves claims structures to retrieve authenticated user identifier.
        /// </summary>
        /// <param name="validationResult">Validation result tracking instance used to mark token decoding state failures.</param>
        /// <returns>The decoded user identity identifier value token.</returns>
        private Guid RetrieveAuthenticatedUserId(ValidationResult validationResult)
        {
            var principalIdValue = _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(principalIdValue, out var parsedUserId))
            {
                validationResult.IsUserValid = false;
                return Guid.Empty;
            }

            return parsedUserId;
        }

        /// <summary>
        /// Retrieves active doctor profile with related data.
        /// </summary>
        /// <param name="doctorId">The doctor identifier to validate.</param>
        /// <param name="validationResult">Validation result tracking instance used to flag database row lookup failure states.</param>
        /// <returns>The active doctor profile or null if invalid.</returns>
        private async Task<DoctorProfile?> RetrieveActiveDoctor(Guid doctorId, ValidationResult validationResult)
        {
            if (!validationResult.IsUserValid) return null;

            var doctor = await _doctorRepository
                .FindByCondition(d => d.Id == doctorId && d.IsActive)
                .Include(d => d.User)
                .Include(d => d.Clinic)
                .FirstOrDefaultAsync();

            if (doctor is null || !doctor.Clinic.IsActive)
            {
                validationResult.IsDoctorValid = false;
                return null;
            }

            return doctor;
        }

        /// <summary>
        /// Retrieves active service within the specified clinic.
        /// </summary>
        /// <param name="serviceId">The service identifier (nullable).</param>
        /// <param name="clinicId">The clinic identifier to validate service belongs to.</param>
        /// <param name="validationResult">Validation result tracking instance monitoring specific service parameters mismatch states.</param>
        /// <returns>The active service or null if not provided or invalid.</returns>
        private async Task<Service?> RetrieveActiveService(Guid? serviceId, Guid? clinicId, ValidationResult validationResult)
        {
            if (!validationResult.IsUserValid || !validationResult.IsDoctorValid) return null;
            if (!serviceId.HasValue || !clinicId.HasValue) return null;

            var service = await _serviceRepository
                .FindByCondition(s => s.Id == serviceId.Value && s.ClinicId == clinicId.Value && s.IsActive)
                .FirstOrDefaultAsync();

            if (service is null)
            {
                validationResult.IsServiceValid = false;
                return null;
            }

            return service;
        }

        /// <summary>
        /// Ensures the patient profile is accessible by the current user.
        /// </summary>
        /// <param name="patientId">The physical tracking target identifier mapping active profile structures.</param>
        /// <param name="userId">The current executing operator identification verification context coordinates.</param>
        /// <param name="validationResult">Validation result tracking instance updating relationship context access boundaries failure flags.</param>
        private async Task EnsurePatientProfileAccess(Guid patientId, Guid userId, ValidationResult validationResult)
        {
            if (!validationResult.IsUserValid) return;

            var isAccessible = await PatientProfileAccessHelper.IsProfileAccessibleAsync(
                userId, patientId, _patientProfileRepository, _context);

            if (!isAccessible)
            {
                validationResult.IsPatientAccessible = false;
            }
        }

        /// <summary>
        /// Retrieves and validates a bookable time slot.
        /// </summary>
        /// <param name="slotId">The slot identifier.</param>
        /// <param name="doctorId">The doctor identifier for validation.</param>
        /// <param name="validationResult">Validation result tracking instance monitoring timeline expiration states.</param>
        /// <returns>The validated time slot or null if invalid.</returns>
        private async Task<TimeSlot?> RetrieveBookableSlot(Guid slotId, Guid doctorId, ValidationResult validationResult)
        {
            if (!validationResult.IsUserValid || !validationResult.IsDoctorValid) return null;

            var slot = await _slotRepository
                .FindByCondition(s => s.Id == slotId, trackChanges: true)
                .Include(s => s.Schedule)
                .FirstOrDefaultAsync();

            if (slot is null || slot.Schedule.DoctorId != doctorId)
            {
                validationResult.IsSlotValid = false;
                return null;
            }

            if (slot.StartTime <= DateTime.Now)
            {
                validationResult.IsSlotValid = false;
                validationResult.IsSlotInPast = true;
                return null;
            }

            if (slot.Status != SlotStatus.AVAILABLE || slot.CurrentPatients >= slot.MaxPatients)
            {
                validationResult.IsSlotValid = false;
                return null;
            }

            return slot;
        }

        /// <summary>
        /// Checks for duplicate appointments for the same patient and slot.
        /// </summary>
        /// <param name="patientId">The targeted patient system tracker identity vector.</param>
        /// <param name="slotId">The timeline checkpoint slot mapping location parameters.</param>
        /// <param name="validationResult">Validation result tracking instance setting concurrency conflict flag maps.</param>
        private async Task CheckDuplicateAppointment(Guid patientId, Guid slotId, ValidationResult validationResult)
        {
            if (!validationResult.IsUserValid || !validationResult.IsPatientAccessible ||
                !validationResult.IsSlotValid) return;

            var duplicateExists = await _appointmentRepository
                .FindByCondition(a => a.PatientId == patientId
                    && a.SlotId == slotId
                    && a.Status != AppointmentStatus.CANCELLED)
                .AnyAsync();

            if (duplicateExists)
            {
                validationResult.IsDuplicateValid = false;
            }
        }

        /// <summary>
        /// Constructs the appointment entity from validated data.
        /// </summary>
        /// <param name="request">The input payload model detailing request context maps.</param>
        /// <param name="userId">The tracking authenticated principal creator token key identifier.</param>
        /// <param name="slot">The target chronological timeline sector configuration model details.</param>
        /// <param name="validationResult">Validation result state evaluation metadata block matrix.</param>
        /// <returns>A concrete initialized domain core entity state ready for persistence tracks.</returns>
        private Appointment? ConstructAppointmentEntity(
            CreateAppointmentRequest request,
            Guid userId,
            TimeSlot? slot,
            ValidationResult validationResult)
        {
            if (!validationResult.IsUserValid || !validationResult.IsDoctorValid ||
                !validationResult.IsPatientAccessible || !validationResult.IsSlotValid ||
                !validationResult.IsDuplicateValid || slot is null)
            {
                return null;
            }

            return new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = request.PatientId,
                DoctorId = request.DoctorId,
                SlotId = request.SlotId,
                ServiceId = request.ServiceId,
                AppointmentDate = slot.StartTime,
                Symptoms = request.Symptoms,
                Status = AppointmentStatus.PENDING,
                BookingSource = "ONLINE",
                CreatedById = userId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        /// <summary>
        /// Persists the appointment and updates slot capacity in a transaction.
        /// </summary>
        /// <param name="appointment">The constructed transient appointment model entity.</param>
        /// <param name="slot">The physical entity track allocation database tracking row to lock.</param>
        /// <param name="validationResult">Validation result context updating structural storage transaction checkpoint logs.</param>
        /// <returns>A compound result pairing the updated core entity model safely alongside storage success state values.</returns>
        private async Task<(Appointment? Appointment, bool IsSuccess)> PersistAppointmentGraph(
            Appointment? appointment,
            TimeSlot? slot,
            ValidationResult validationResult)
        {
            if (appointment is null || slot is null ||
                !validationResult.IsUserValid || !validationResult.IsDoctorValid ||
                !validationResult.IsPatientAccessible || !validationResult.IsSlotValid ||
                !validationResult.IsDuplicateValid)
            {
                return (null, false);
            }

            using var transactionalScope = await _appointmentRepository.BeginTransactionAsync();

            try
            {
                await _appointmentRepository.CreateAsync(appointment);

                slot.CurrentPatients += 1;
                if (slot.CurrentPatients >= slot.MaxPatients)
                {
                    slot.Status = SlotStatus.BOOKED;
                }

                await _appointmentRepository.SaveChangesAsync();
                await transactionalScope.CommitAsync();

                validationResult.IsTransactionSuccess = true;
                return (appointment, true);
            }
            catch (Exception)
            {
                await transactionalScope.RollbackAsync();
                validationResult.IsTransactionSuccess = false;
                return (null, false);
            }
        }

        /// <summary>
        /// Transforms processing contexts into appropriate application response payloads.
        /// </summary>
        /// <param name="appointment">The successfully committed backend model core representation row layout.</param>
        /// <param name="doctor">The associated structured profile configuration dataset metrics context.</param>
        /// <param name="service">The selected transactional operational care procedure description text metadata profile.</param>
        /// <param name="slot">The timeline interval coordinate parameters.</param>
        /// <param name="validationResult">The final tracking diagnostic matrix structure assessing data flow safety loops.</param>
        /// <param name="successState">Guard tracking flag verifying internal transactional engine operations outcome state logs.</param>
        /// <returns>An endpoint transport safe formatted serialization container.</returns>
        private ApiResponse<CreateAppointmentResponse> CreateResponse(
            Appointment? appointment,
            DoctorProfile? doctor,
            Service? service,
            TimeSlot? slot,
            ValidationResult validationResult,
            bool successState)
        {
            if (!validationResult.IsUserValid)
            {
                return ApiResponse<CreateAppointmentResponse>.Fail(GeneralCode.APP_MESSAGE_4033.ToString());
            }

            if (!validationResult.IsDoctorValid)
            {
                return ApiResponse<CreateAppointmentResponse>.Fail(GeneralCode.APP_MESSAGE_4011.ToString());
            }

            if (!validationResult.IsPatientAccessible)
            {
                return ApiResponse<CreateAppointmentResponse>.Fail(GeneralCode.APP_MESSAGE_4014.ToString());
            }

            if (!validationResult.IsSlotValid)
            {
                return ApiResponse<CreateAppointmentResponse>.Fail(GeneralCode.APP_MESSAGE_4006.ToString());
            }

            if (validationResult.IsSlotInPast)
            {
                return ApiResponse<CreateAppointmentResponse>.Fail(GeneralCode.APP_MESSAGE_4005.ToString());
            }

            if (!validationResult.IsDuplicateValid)
            {
                return ApiResponse<CreateAppointmentResponse>.Fail(GeneralCode.APP_MESSAGE_4015.ToString());
            }

            if (!validationResult.IsServiceValid)
            {
                return ApiResponse<CreateAppointmentResponse>.Fail(GeneralCode.APP_MESSAGE_4044.ToString());
            }

            if (!successState || appointment is null || doctor is null || slot is null)
            {
                return ApiResponse<CreateAppointmentResponse>.Fail(GeneralCode.APP_MESSAGE_5001.ToString());
            }

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
                GeneralCode.APP_MESSAGE_2001.ToString(), response);
        }

        /// <summary>
        /// Validation result container class to avoid ref parameters in async methods.
        /// </summary>
        private class ValidationResult
        {
            public bool IsUserValid { get; set; }
            public bool IsDoctorValid { get; set; }
            public bool IsServiceValid { get; set; }
            public bool IsPatientAccessible { get; set; }
            public bool IsSlotValid { get; set; }
            public bool IsDuplicateValid { get; set; }
            public bool IsTransactionSuccess { get; set; }
            public bool IsSlotInPast { get; set; }
        }
    }
}