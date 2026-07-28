using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECS.Application.Services.MedicalRecordsServices.PreliminaryDiagnosisServices
{
    /// <summary>
    /// Service implementation for preliminary diagnosis / Triage processing.
    /// This is a SEPARATE workflow from MedicalRecord with completely different fields.
    /// Used for quick initial assessment and symptom screening.
    /// </summary>
    public class PreliminaryDiagnosisService : IPreliminaryDiagnosisService
    {
        private readonly IRepositoryQueryBase<Appointment, Guid, AppDbContext> _appointmentRepository;
        private readonly IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> _patientRepository;
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> _doctorRepository;
        private readonly IRepositoryQueryBase<PreliminaryDiagnosis, Guid, AppDbContext> _preliminaryDiagnosisRepository;
        private readonly IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> _medicalRecordRepository;
        private readonly IValidator<PreliminaryDiagnosisRequest> _validator;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<PreliminaryDiagnosisService> _logger;

        public PreliminaryDiagnosisService(
            IRepositoryQueryBase<Appointment, Guid, AppDbContext> appointmentRepository,
            IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> patientRepository,
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepository,
            IRepositoryQueryBase<PreliminaryDiagnosis, Guid, AppDbContext> preliminaryDiagnosisRepository,
            IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> medicalRecordRepository,
            IValidator<PreliminaryDiagnosisRequest> validator,
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<PreliminaryDiagnosisService> logger)
        {
            _appointmentRepository = appointmentRepository;
            _patientRepository = patientRepository;
            _doctorRepository = doctorRepository;
            _preliminaryDiagnosisRepository = preliminaryDiagnosisRepository;
            _medicalRecordRepository = medicalRecordRepository;
            _validator = validator;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Main orchestration method for preliminary diagnosis / triage processing.
        /// </summary>
        public async Task<ApiResponse<PreliminaryDiagnosisResponse>> Process(PreliminaryDiagnosisRequest request)
        {
            var state = new ExecutionState();
            // Step 1: Validate incoming request data
            ValidateRequest(request, state);
            // Step 2: Extract authenticated user ID from JWT token
            RetrieveAuthenticatedUserId(state);
            // Step 3: Parse appointment ID
            ParseAppointmentId(request.AppointmentId, state);
            // Step 4: Verify appointment exists and is valid
            await GetAppointmentAsync(state);
            // Step 5: Verify doctor exists and is active
            await GetDoctorProfileAsync(state);
            // Step 6: Verify patient exists
            await GetPatientProfileAsync(state);
            // Step 7: Check if preliminary diagnosis already exists for this appointment
            CheckPreliminaryDiagnosisExists(state);
            // Step 8: Create preliminary diagnosis record (separate from MedicalRecord)
            await CreatePreliminaryDiagnosisAsync(request, state);
            // Step 9: Update appointment status based on triage result
            await UpdateAppointmentStatusAsync(state, request);
            // Step 10: Build and return the response
            return CreateResponse(state);
        }

        #region Execution State

        /// <summary>
        /// ExecutionState holds all mutable state for the process flow.
        /// </summary>
        private class ExecutionState
        {
            public bool IsValidationPassed { get; set; } = true;
            public bool IsUserValid { get; set; } = true;
            public bool IsAppointmentValid { get; set; } = true;
            public bool IsDoctorExists { get; set; } = true;
            public bool IsPatientExists { get; set; } = true;
            public bool IsPreliminaryDiagnosisExists { get; set; } = false;
            public bool IsExecutionSuccess { get; set; } = true;
            public bool HasError { get; set; } = false;
            public Guid ActiveUserId { get; set; }
            public Guid AppointmentId { get; set; }
            public Appointment? Appointment { get; set; }
            public DoctorProfile? DoctorProfile { get; set; }
            public PatientProfile? PatientProfile { get; set; }
            public PreliminaryDiagnosis? CreatedPreliminaryDiagnosis { get; set; }
            public string? ErrorCode { get; set; }
            public string? PatientName { get; set; }
            public string? DoctorName { get; set; }
        }

        #endregion

        #region Validation Steps

        /// <summary>
        /// Validates the incoming request using FluentValidation rules.
        /// </summary>
        private void ValidateRequest(PreliminaryDiagnosisRequest request, ExecutionState state)
        {
            var result = _validator.Validate(request);
            state.IsValidationPassed = result.IsValid;
            state.HasError = !result.IsValid;
            state.ErrorCode = result.IsValid ? null : GeneralCode.APP_MESSAGE_4019.ToString();

            if (!result.IsValid)
            {
                _logger.LogWarning("[VALIDATION FAILED] Errors: {Errors}", 
                    string.Join("; ", result.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
            }
        }

        /// <summary>
        /// Extracts the authenticated user ID from JWT token in HTTP context.
        /// </summary>
        private void RetrieveAuthenticatedUserId(ExecutionState state)
        {
            var principalIdValue = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var parseResult = Guid.TryParse(principalIdValue, out var parsedUserId);
            state.IsUserValid = parseResult;
            state.ActiveUserId = parseResult ? parsedUserId : Guid.Empty;
            state.HasError = !parseResult;
            state.ErrorCode = parseResult ? null : GeneralCode.APP_MESSAGE_4033.ToString();
        }

        /// <summary>
        /// Parses the appointment ID from string to Guid.
        /// </summary>
        private void ParseAppointmentId(string appointmentId, ExecutionState state)
        {
            var parseResult = Guid.TryParse(appointmentId, out var parsedId);
            state.AppointmentId = parseResult ? parsedId : Guid.Empty;
            state.HasError = state.HasError || !parseResult;
            state.ErrorCode = parseResult ? state.ErrorCode : GeneralCode.APP_MESSAGE_4019.ToString();
        }

        /// <summary>
        /// Retrieves and validates the appointment from database.
        /// </summary>
        private async Task GetAppointmentAsync(ExecutionState state)
        {
            if (state.HasError) return;
            var appointment = await _appointmentRepository
                .FindByCondition(a => a.Id == state.AppointmentId, trackChanges: false)
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .ThenInclude(d => d.User)
                .FirstOrDefaultAsync();
            state.Appointment = appointment;
            state.IsAppointmentValid = appointment != null;
            state.HasError = !state.IsAppointmentValid;
            state.ErrorCode = state.IsAppointmentValid ? state.ErrorCode : GeneralCode.APP_MESSAGE_4012.ToString();
        }

        /// <summary>
        /// Retrieves the doctor profile for the authenticated user.
        /// </summary>
        private async Task GetDoctorProfileAsync(ExecutionState state)
        {
            if (state.HasError) return;
            var doctorProfile = await _doctorRepository
                .FindByCondition(d => d.UserId == state.ActiveUserId && d.IsActive, trackChanges: false)
                .Include(d => d.User)
                .FirstOrDefaultAsync();
            state.DoctorProfile = doctorProfile;
            state.IsDoctorExists = doctorProfile != null;
            state.DoctorName = doctorProfile?.User?.FullName;
            state.HasError = !state.IsDoctorExists;
            state.ErrorCode = state.IsDoctorExists ? state.ErrorCode : GeneralCode.APP_MESSAGE_4011.ToString();
        }

        /// <summary>
        /// Retrieves the patient profile associated with the appointment.
        /// </summary>
        private async Task GetPatientProfileAsync(ExecutionState state)
        {
            if (state.HasError || state.Appointment == null) return;
            var patientProfile = await _patientRepository
                .FindByCondition(p => p.Id == state.Appointment.PatientId, trackChanges: false)
                .FirstOrDefaultAsync();
            state.PatientProfile = patientProfile;
            state.IsPatientExists = patientProfile != null;
            state.PatientName = patientProfile?.FullName;
            state.HasError = !state.IsPatientExists;
            state.ErrorCode = state.IsPatientExists ? state.ErrorCode : GeneralCode.APP_MESSAGE_4010.ToString();
        }

        /// <summary>
        /// Checks if a preliminary diagnosis already exists for the appointment.
        /// </summary>
        private void CheckPreliminaryDiagnosisExists(ExecutionState state)
        {
            if (!state.IsAppointmentValid || state.HasError) return;
            var existingRecord = _preliminaryDiagnosisRepository
                .FindByCondition(r => r.AppointmentId == state.AppointmentId, trackChanges: false)
                .FirstOrDefaultAsync()
                .GetAwaiter()
                .GetResult();
            state.IsPreliminaryDiagnosisExists = existingRecord != null;
            if (state.IsPreliminaryDiagnosisExists)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4027.ToString();
            }
        }

        #endregion

        #region Creation Steps

        /// <summary>
        /// Creates a preliminary diagnosis record with triage/screening information.
        /// This is stored separately from MedicalRecord with completely different fields.
        /// Business rules:
        /// - Pain level >= 8 automatically upgrades urgency to High
        /// </summary>
        private async Task CreatePreliminaryDiagnosisAsync(PreliminaryDiagnosisRequest request, ExecutionState state)
        {
            if (state.HasError || state.Appointment == null || state.DoctorProfile == null || state.PatientProfile == null) return;
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Business rule: Auto-upgrade urgency if pain level is high
                var urgencyLevel = request.UrgencyLevel;
                if (request.PainLevel.HasValue && request.PainLevel >= 8 && urgencyLevel != TriageUrgencyLevel.Emergency)
                {
                    urgencyLevel = TriageUrgencyLevel.High;
                }

                var preliminaryDiagnosis = new PreliminaryDiagnosis
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = state.AppointmentId,
                    PatientId = state.Appointment.PatientId,
                    DoctorId = state.DoctorProfile.Id,
                    // Triage fields
                    UrgencyLevel = urgencyLevel,
                    PainLevel = request.PainLevel,
                    QuickVisualAssessment = request.QuickVisualAssessment,
                    // Symptom flags
                    HasVisionChange = request.HasVisionChange,
                    HasEyeRedness = request.HasEyeRedness,
                    HasEyeDischarge = request.HasEyeDischarge,
                    HasLightSensitivity = request.HasLightSensitivity,
                    HasEyePain = request.HasEyePain,
                    HasHeadache = request.HasHeadache,
                    HasForeignBody = request.HasForeignBody,
                    // Actions
                    RecommendedAction = request.RecommendedAction,
                    IsReferralNeeded = request.IsReferralNeeded,
                    ReferralTo = request.ReferralTo,
                    FollowUpInstructions = request.FollowUpInstructions,
                    // Timestamps
                    CheckInTime = request.CheckInTime ?? DateTime.UtcNow,
                    TriageCompletedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.PreliminaryDiagnoses.Add(preliminaryDiagnosis);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                state.CreatedPreliminaryDiagnosis = preliminaryDiagnosis;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                state.IsExecutionSuccess = false;
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_5001.ToString();
            }
        }

        /// <summary>
        /// Updates appointment status after successful triage.
        /// - Emergency/High urgency: Move to IN_PROGRESS immediately
        /// - Medium/Low urgency: Move to ARRIVED for queueing
        /// </summary>
        private async Task UpdateAppointmentStatusAsync(ExecutionState state, PreliminaryDiagnosisRequest request)
        {
            if (state.HasError || state.Appointment == null) return;
            try
            {
                // Business rule: Set status based on urgency
                if (request.UrgencyLevel == TriageUrgencyLevel.Emergency || request.UrgencyLevel == TriageUrgencyLevel.High)
                {
                    state.Appointment.Status = AppointmentStatus.IN_PROGRESS;
                }
                else
                {
                    state.Appointment.Status = AppointmentStatus.ARRIVED;
                }
                state.Appointment.UpdatedAt = DateTime.UtcNow;
                _context.Appointments.Update(state.Appointment);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Non-critical error, just log
            }
        }

        #endregion

        #region Response Building

        /// <summary>
        /// Creates the API response based on execution state.
        /// </summary>
        private ApiResponse<PreliminaryDiagnosisResponse> CreateResponse(ExecutionState state)
        {
            if (state.HasError)
            {
                return ApiResponse<PreliminaryDiagnosisResponse>.Fail(
                    state.ErrorCode ?? GeneralCode.APP_MESSAGE_4001.ToString());
            }
            var response = new PreliminaryDiagnosisResponse
            {
                PreliminaryDiagnosisId = state.CreatedPreliminaryDiagnosis?.Id.ToString() ?? string.Empty,
                PatientName = state.PatientName,
                AppointmentDate = state.Appointment?.AppointmentDate.ToString("dd/MM/yyyy"),
                DoctorName = state.DoctorName,
                TriageCompletedAt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm"),
                UrgencyLevel = state.CreatedPreliminaryDiagnosis?.UrgencyLevel.ToString() ?? string.Empty,
                RecommendedAction = state.CreatedPreliminaryDiagnosis?.RecommendedAction,
                IsSuccess = true
            };
            return ApiResponse<PreliminaryDiagnosisResponse>.Success(
                GeneralCode.APP_MESSAGE_2005.ToString(), response);
        }

        #endregion
    }
}
