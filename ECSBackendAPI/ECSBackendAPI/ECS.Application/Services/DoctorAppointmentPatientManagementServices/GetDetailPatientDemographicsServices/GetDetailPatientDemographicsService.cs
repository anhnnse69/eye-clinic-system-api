using ECS.Application.Common.Response;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.GetDetailPatientDemographicsServices
{
    /// <summary>
    /// Handles the business logic for retrieving detailed patient demographics information with access control.
    /// </summary>
    public class GetDetailPatientDemographicsService : IGetDetailPatientDemographicsService
    {
        private readonly IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> _patientProfileRepository;
        private readonly AppDbContext _context;
        private readonly IValidator<GetDetailPatientDemographicsRequest> _validator;

        /// <summary>
        /// Initializes a new instance of <see cref="GetDetailPatientDemographicsService"/> with required dependencies.
        /// </summary>
        /// <param name="patientProfileRepository">Repository for querying patient profile data.</param>
        /// <param name="context">The underlying persistence database context.</param>
        /// <param name="validator">Validator for request data.</param>
        public GetDetailPatientDemographicsService(
            IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> patientProfileRepository,
            AppDbContext context,
            IValidator<GetDetailPatientDemographicsRequest> validator)
        {
            _patientProfileRepository = patientProfileRepository;
            _context = context;
            _validator = validator;
        }

        /// <summary>
        /// Processes the request to retrieve patient demographics details with authorization checks.
        /// </summary>
        /// <param name="request">The request containing the patient identifier.</param>
        /// <returns>An <see cref="ApiResponse{GetPatientDemographicsResponse}"/> containing patient demographics details or error.</returns>
        public async Task<ApiResponse<GetDetailPatientDemographicsResponse>> Process(GetDetailPatientDemographicsRequest request)
        {
            // Initialize status tracking flags
            bool isValidationPassed = true;
            string? validationErrorCode = null;
            // Step 1: Validate request data format
            ValidateRequest(request, ref isValidationPassed, ref validationErrorCode);
            // Step 2: Retrieve patient profile from database
            var patientProfile = await RetrievePatientProfile(request.PatientId);
            // Step 3: Assemble API payload or generate error response
            return CreateResponse(patientProfile, isValidationPassed, validationErrorCode);
        }

        /// <summary>
        /// Validates the incoming request payload against defined business rules.
        /// </summary>
        /// <param name="request">The request payload to validate.</param>
        /// <param name="isValidationPassed">Flag updated to <c>false</c> if validation fails.</param>
        /// <param name="validationErrorCode">Stores the first validation error code encountered.</param>
        private void ValidateRequest(
            GetDetailPatientDemographicsRequest request,
            ref bool isValidationPassed,
            ref string? validationErrorCode)
        {
            var result = _validator.Validate(request);
            if (!result.IsValid)
            {
                isValidationPassed = false;
                validationErrorCode = result.Errors.First().ErrorCode;
            }
        }

        /// <summary>
        /// Retrieves the patient profile from the database by identifier.
        /// </summary>
        /// <param name="patientId">The unique identifier of the patient profile.</param>
        /// <returns>The matched <see cref="PatientProfile"/> if found; otherwise, null.</returns>
        private async Task<PatientProfile?> RetrievePatientProfile(Guid patientId)
        {
            return await _patientProfileRepository
                .FindByCondition(x => x.Id == patientId, trackChanges: false)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Generates an API response payload based on validation status flags.
        /// </summary>
        /// <param name="patientProfile">The resolved patient profile instance.</param>
        /// <param name="isValidationPassed">Indicates whether request format validation succeeded.</param>
        /// <param name="validationErrorCode">The error code from validation failure, if any.</param>
        /// <returns>A configured <see cref="ApiResponse{GetDetailPatientDemographicsResponse}"/>.</returns>
        private ApiResponse<GetDetailPatientDemographicsResponse> CreateResponse(
            PatientProfile? patientProfile,
            bool isValidationPassed,
            string? validationErrorCode)
        {
            var errorResponse = CreateErrorResponse(patientProfile, isValidationPassed, validationErrorCode);
            return errorResponse ?? ApiResponse<GetDetailPatientDemographicsResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                MapToResponse(patientProfile!));
        }

        /// <summary>
        /// Creates an error response based on validation failure flags.
        /// </summary>
        /// <param name="patientProfile">The retrieved patient profile.</param>
        /// <param name="isValidationPassed">Indicates whether request validation succeeded.</param>
        /// <param name="validationErrorCode">The error code from validation failure.</param>
        /// <returns>A failed <see cref="ApiResponse{GetDetailPatientDemographicsResponse}"/> variant if errors are found; otherwise <c>null</c>.</returns>
        private ApiResponse<GetDetailPatientDemographicsResponse>? CreateErrorResponse(
            PatientProfile? patientProfile,
            bool isValidationPassed,
            string? validationErrorCode)
        {
            if (!isValidationPassed)
                return ApiResponse<GetDetailPatientDemographicsResponse>.Fail(validationErrorCode!);

            if (patientProfile == null)
                return ApiResponse<GetDetailPatientDemographicsResponse>.Fail(GeneralCode.APP_MESSAGE_4010.ToString());

            return null;
        }

        /// <summary>
        /// Maps the patient profile entity to the response DTO.
        /// </summary>
        /// <param name="patient">The patient profile entity.</param>
        /// <returns>A <see cref="GetDetailPatientDemographicsResponse"/> with formatted patient demographics data.</returns>
        private static GetDetailPatientDemographicsResponse MapToResponse(PatientProfile patient)
        {
            return new GetDetailPatientDemographicsResponse
            {
                FullName = patient.FullName,
                Dob = patient.Dob.ToString("yyyy-MM-dd"),
                Gender = patient.Gender.ToString(),
                PhoneNumber = patient.PhoneNumber,
                IdentityNumber = patient.IdentityNumber,
                BhytNumber = patient.BhytNumber,
                Address = patient.Address,
                BloodType = patient.BloodType,
                Allergies = patient.Allergies,
                MedicalHistory = patient.MedicalHistory
            };
        }
    }
}
