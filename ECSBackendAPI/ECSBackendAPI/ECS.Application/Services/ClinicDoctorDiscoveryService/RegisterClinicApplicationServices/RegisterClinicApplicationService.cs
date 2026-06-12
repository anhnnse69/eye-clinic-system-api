using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.AuthServices.RegisterClinicApplicationServices
{
    /// <summary>
    /// Handles clinic application registration process.
    /// </summary>
    public class RegisterClinicApplicationService
        : IRegisterClinicApplicationService
    {
        private readonly IRepositoryQueryBase<
            ClinicRegistrationRequest,
            Guid,
            AppDbContext> _clinicApplicationQueryRepository;
        private readonly IRepositoryBaseAsync<
            ClinicRegistrationRequest,
            Guid,
            AppDbContext> _clinicApplicationRepository;
        private readonly IValidator<
            RegisterClinicApplicationRequest> _validator;
        public RegisterClinicApplicationService(
            IRepositoryQueryBase<
                ClinicRegistrationRequest,
                Guid,
                AppDbContext> clinicApplicationQueryRepository,
            IRepositoryBaseAsync<
                ClinicRegistrationRequest,
                Guid,
                AppDbContext> clinicApplicationRepository,
            IValidator<RegisterClinicApplicationRequest> validator)
        {
            _clinicApplicationQueryRepository =
                clinicApplicationQueryRepository;
            _clinicApplicationRepository =
                clinicApplicationRepository;
            _validator = validator;
        }

        /// <inheritdoc/>
        public async Task<ApiResponse<bool>> Process(
            RegisterClinicApplicationRequest request)
        {
            // Validate request data format
            var validationResult = await ValidateRequestAsync(request);
            // Check duplicate pending application
            var duplicateResult = await CheckPendingApplicationAsync(request, validationResult.IsPassed);
            // Assemble response or persist
            return await CreateResponse(validationResult, duplicateResult, request);
        }

        /// <summary>
        /// Validates the incoming request payload
        /// against defined business rules.
        /// </summary>
        private async Task<ValidationResult> ValidateRequestAsync(
            RegisterClinicApplicationRequest request)
        {
            var result = await _validator.ValidateAsync(request);
            if (!result.IsValid)
            {
                return new ValidationResult(
                    false,
                    result.Errors.First().ErrorCode);
            }
            return new ValidationResult(true, null);
        }

        /// <summary>
        /// Checks whether a pending application already exists
        /// with the same contact email.
        /// </summary>
        private async Task<DuplicateResult> CheckPendingApplicationAsync(
            RegisterClinicApplicationRequest request,
            bool isValidationPassed)
        {
            if (!isValidationPassed)
            {
                return new DuplicateResult(false);
            }
            var normalizedEmail =
                request.ContactEmail.Trim().ToLower();
            var exists = await _clinicApplicationQueryRepository
                .FindByCondition(x =>
                    x.ContactEmail.ToLower() == normalizedEmail &&
                    x.Status == "PENDING")
                .AnyAsync();
            return new DuplicateResult(exists);
        }

        /// <summary>
        /// Assembles the final API response based on
        /// validation and duplicate check results.
        /// </summary>
        private async Task<ApiResponse<bool>> CreateResponse(
            ValidationResult validationResult,
            DuplicateResult duplicateResult,
            RegisterClinicApplicationRequest request)
        {
            var errorResponse = CreateErrorResponse(
                validationResult,
                duplicateResult);
            if (errorResponse != null)
            {
                return errorResponse;
            }
            return await CreateClinicApplicationAsync(request);
        }

        /// <summary>
        /// Creates an error response based on
        /// validation and duplicate check failure flags.
        /// </summary>
        private ApiResponse<bool>? CreateErrorResponse(
            ValidationResult validationResult,
            DuplicateResult duplicateResult)
        {
            if (!validationResult.IsPassed)
            {
                return ApiResponse<bool>.Fail(
                    validationResult.ErrorCode!);
            }
            if (duplicateResult.Exists)
            {
                return ApiResponse<bool>.Fail(
                    GeneralCode.APP_MESSAGE_4023.ToString());
            }
            return null;
        }

        /// <summary>
        /// Persists a new clinic application entity
        /// and returns a success response.
        /// </summary>
        private async Task<ApiResponse<bool>> CreateClinicApplicationAsync(
            RegisterClinicApplicationRequest request)
        {
            var application = BuildClinicApplication(request);
            await _clinicApplicationRepository
                .CreateAsync(application);
            await _clinicApplicationRepository
                .SaveChangesAsync();
            return ApiResponse<bool>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                true);
        }

        /// <summary>
        /// Maps the registration request to a
        /// <see cref="ClinicRegistrationRequest"/> entity.
        /// </summary>
        private static ClinicRegistrationRequest BuildClinicApplication(
            RegisterClinicApplicationRequest request)
        {
            return new ClinicRegistrationRequest
            {
                Id = Guid.NewGuid(),
                ClinicName = request.ClinicName.Trim(),
                ClinicAddress = request.ClinicAddress.Trim(),
                ContactName = request.ContactName.Trim(),
                ContactPhone = request.ContactPhone.Trim(),
                ContactEmail = request.ContactEmail.Trim().ToLower(),
                BusinessLicenseUrl = request.BusinessLicenseUrl,
                Status = "PENDING",
                RequestedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Represents the result of a format validation operation.
        /// </summary>
        private record ValidationResult(bool IsPassed, string? ErrorCode);

        /// <summary>
        /// Represents the result of a duplicate pending application check.
        /// </summary>
        private record DuplicateResult(bool Exists);
    }
}