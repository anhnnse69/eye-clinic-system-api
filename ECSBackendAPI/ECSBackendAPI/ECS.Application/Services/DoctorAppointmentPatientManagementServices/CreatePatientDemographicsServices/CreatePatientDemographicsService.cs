using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.CreatePatientDemographicsServices
{
    /// <summary>
    /// Service implementation for creating patient medical demographics records.
    /// Based on UC36 - Create Patient Demographics
    /// BR1: Each patient may have only one medical demographics record; duplicates are not allowed.
    /// This endpoint handles ONLY medical/ophthalmology information that doctors enter.
    /// Administrative info (DOB, Gender, Address, etc.) are handled separately by Patient/Receptionist.
    /// </summary>
    public class CreatePatientDemographicsService : ICreatePatientDemographicsService
    {
        private readonly IRepositoryBaseAsync<PatientProfile, Guid, AppDbContext> _patientProfileRepository;
        private readonly IValidator<CreatePatientDemographicsRequest> _validator;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Constructor - injects all required dependencies for medical demographics creation.
        /// </summary>
        public CreatePatientDemographicsService(
            IRepositoryBaseAsync<PatientProfile, Guid, AppDbContext> patientProfileRepository,
            IValidator<CreatePatientDemographicsRequest> validator,
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _patientProfileRepository = patientProfileRepository;
            _validator = validator;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Main orchestration method - coordinates the entire medical demographics creation flow.
        /// Uses ExecutionState to track status and avoid if/else branching in the main flow.
        /// </summary>
        public async Task<ApiResponse<CreatePatientDemographicsResponse>> Process(CreatePatientDemographicsRequest request)
        {
            var state = new ExecutionState();
            
            // Step 1: Validate incoming request data
            ValidateRequest(request, state);
            
            // Step 2: Extract authenticated user ID from JWT token
            RetrieveAuthenticatedUserId(state);
            
            // Step 3: Parse and validate patient profile ID from request
            ParsePatientProfileId(request.PatientProfileId, state);
            
            // Step 4: Verify patient profile exists in the system
            GetPatientProfileAsync(state);
            
            // Step 5: Check if medical demographics already exists for this patient (BR1)
            CheckMedicalDemographicsExists(state);
            
            // Step 6: Update patient profile with medical demographics data
            UpdatePatientWithMedicalDemographics(request, state);
            
            // Step 7: Persist changes to database within a transaction
            await PersistDataAsync(state);
            
            // Step 8: Build and return the response
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
            public bool IsPatientProfileIdValid { get; set; } = true;
            public bool IsPatientExists { get; set; } = true;
            public bool IsMedicalDemographicsAlreadyExists { get; set; } = false;
            public bool IsExecutionSuccess { get; set; } = true;
            public bool HasError { get; set; } = false;
            
            public Guid ActiveUserId { get; set; }
            public Guid PatientProfileId { get; set; }
            public PatientProfile? PatientProfile { get; set; }
            
            public string? ErrorCode { get; set; }
            public string? PatientName { get; set; }
        }

        /// <summary>
        /// Validates the request using FluentValidation.
        /// Sets IsValidationPassed and ErrorCode based on validation result.
        /// </summary>
        private void ValidateRequest(CreatePatientDemographicsRequest request, ExecutionState state)
        {
            var result = _validator.Validate(request);
            state.IsValidationPassed = result.IsValid;
            state.HasError = !result.IsValid;
            state.ErrorCode = result.IsValid ? null : GeneralCode.APP_MESSAGE_4003.ToString();
        }

        /// <summary>
        /// Extracts and parses the authenticated user ID from the HTTP context.
        /// Used to identify which doctor is creating the medical demographics record.
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
        /// Parses the PatientProfileId string from the request into a Guid.
        /// Sets validation error if the format is invalid.
        /// </summary>
        private void ParsePatientProfileId(string patientProfileId, ExecutionState state)
        {
            var parseResult = Guid.TryParse(patientProfileId, out var parsedId);
            
            state.IsPatientProfileIdValid = parseResult;
            state.PatientProfileId = parseResult ? parsedId : Guid.Empty;
            state.HasError = state.HasError || !parseResult;
            state.ErrorCode = parseResult ? state.ErrorCode : GeneralCode.APP_MESSAGE_4019.ToString();
        }

        /// <summary>
        /// Retrieves the patient profile by ID.
        /// Sets IsPatientExists flag based on whether the patient was found.
        /// </summary>
        private void GetPatientProfileAsync(ExecutionState state)
        {
            var patientProfile = _patientProfileRepository
                .FindByCondition(p => p.Id == state.PatientProfileId, trackChanges: true)
                .FirstOrDefaultAsync()
                .GetAwaiter()
                .GetResult();

            state.PatientProfile = patientProfile;
            state.IsPatientExists = patientProfile != null;
            state.PatientName = patientProfile?.FullName;
            state.HasError = state.HasError || !state.IsPatientExists;
            state.ErrorCode = state.IsPatientExists ? state.ErrorCode : GeneralCode.APP_MESSAGE_4010.ToString();
        }

        /// <summary>
        /// Checks if medical demographics already exists for this patient (BR1).
        /// Each patient may have only one medical demographics record; duplicates are not allowed.
        /// </summary>
        private void CheckMedicalDemographicsExists(ExecutionState state)
        {
            if (!state.IsPatientExists || state.HasError)
            {
                return;
            }

            var patientProfile = state.PatientProfile!;
            
            // Check if medical demographics already exists using the HasMedicalDemographics flag
            state.IsMedicalDemographicsAlreadyExists = patientProfile.HasMedicalDemographics;
            if (state.IsMedicalDemographicsAlreadyExists)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4099.ToString(); // Medical Demographics record already exists
            }
        }

        /// <summary>
        /// Updates patient profile with medical demographics data from the request.
        /// Only updates medical/ophthalmology fields.
        /// </summary>
        private void UpdatePatientWithMedicalDemographics(CreatePatientDemographicsRequest request, ExecutionState state)
        {
            var patientProfile = state.PatientProfile;
            
            if (patientProfile == null || state.HasError)
            {
                return;
            }

            // === Medical Background Section ===
            if (!string.IsNullOrWhiteSpace(request.BloodType))
            {
                patientProfile.BloodType = request.BloodType.Trim();
            }
            if (!string.IsNullOrWhiteSpace(request.Allergies))
            {
                patientProfile.Allergies = request.Allergies.Trim();
            }
            if (!string.IsNullOrWhiteSpace(request.MedicalHistory))
            {
                patientProfile.MedicalHistory = request.MedicalHistory.Trim();
            }
            if (!string.IsNullOrWhiteSpace(request.FamilyHistory))
            {
                patientProfile.FamilyHistory = request.FamilyHistory.Trim();
            }
            if (!string.IsNullOrWhiteSpace(request.LifestyleFactors))
            {
                patientProfile.LifestyleFactors = request.LifestyleFactors.Trim();
            }

            // === Ophthalmology-specific fields ===
            if (!string.IsNullOrWhiteSpace(request.CurrentEyeMedications))
            {
                patientProfile.CurrentEyeMedications = request.CurrentEyeMedications.Trim();
            }
            if (!string.IsNullOrWhiteSpace(request.PreviousEyeSurgery))
            {
                patientProfile.PreviousEyeSurgery = request.PreviousEyeSurgery.Trim();
            }
            if (!string.IsNullOrWhiteSpace(request.EyeVisionHistory))
            {
                patientProfile.EyeVisionHistory = request.EyeVisionHistory.Trim();
            }

            // Mark medical demographics as created
            patientProfile.HasMedicalDemographics = true;
            patientProfile.UpdatedAt = DateTime.UtcNow;
            state.PatientProfile = patientProfile;
        }

        /// <summary>
        /// Persists all changes to the database within a transaction.
        /// Rolls back on any error and sets appropriate error codes.
        /// </summary>
        private async Task PersistDataAsync(ExecutionState state)
        {
            if (state.HasError)
            {
                return;
            }

            using var transactionScope = await _patientProfileRepository.BeginTransactionAsync();
            
            try
            {
                // Update patient profile with medical demographics
                if (state.PatientProfile != null)
                {
                    _context.PatientProfiles.Update(state.PatientProfile!);
                    await _context.SaveChangesAsync();
                }

                await transactionScope.CommitAsync();
            }
            catch
            {
                await transactionScope.RollbackAsync();
                state.IsExecutionSuccess = false;
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_5001.ToString();
            }
        }

        /// <summary>
        /// Creates the final response based on execution state.
        /// Returns error response if any step failed, otherwise returns success with medical demographics details.
        /// </summary>
        private ApiResponse<CreatePatientDemographicsResponse> CreateResponse(ExecutionState state)
        {
            if (state.HasError)
            {
                var errorCode = state.ErrorCode ?? GeneralCode.APP_MESSAGE_4001.ToString();
                return ApiResponse<CreatePatientDemographicsResponse>.Fail(errorCode);
            }

            var patientProfile = state.PatientProfile;
            
            var response = new CreatePatientDemographicsResponse
            {
                PatientProfileId = state.PatientProfileId,
                PatientName = patientProfile?.FullName,
                BloodType = patientProfile?.BloodType,
                Allergies = patientProfile?.Allergies,
                MedicalHistory = patientProfile?.MedicalHistory,
                FamilyHistory = patientProfile?.FamilyHistory,
                LifestyleFactors = patientProfile?.LifestyleFactors,
                CurrentEyeMedications = patientProfile?.CurrentEyeMedications,
                PreviousEyeSurgery = patientProfile?.PreviousEyeSurgery,
                EyeVisionHistory = patientProfile?.EyeVisionHistory,
                CreatedAt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm"),
                IsSuccess = true
            };

            return ApiResponse<CreatePatientDemographicsResponse>.Success(
                GeneralCode.APP_MESSAGE_2005.ToString(),
                response);
        }
    }
}
