using ECS.Application.Common.Response;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.SystemAdminServices.AdminSystemCreateAccountServices
{
    /// <summary>
    /// Service execution block managing validation controls and transaction pipelines for creating account entities.
    /// </summary>
    public class CreateAccountService : ICreateAccountService
    {
        private readonly IRepositoryBaseAsync<User, Guid, AppDbContext> _userRepository;

        /// <summary>
        /// Initializes a new instance of <see cref="CreateAccountService"/> with persistence storage abstractions.
        /// </summary>
        /// <param name="userRepository">Repository context reference manipulating backend user records layer bounds.</param>
        public CreateAccountService(IRepositoryBaseAsync<User, Guid, AppDbContext> userRepository)
        {
            _userRepository = userRepository;
        }

        /// <summary>
        /// Handles the main linear logic execution workflow block preventing structural branching via explicit code segments.
        /// </summary>
        /// <param name="request">The incoming core registration criteria parameters package token.</param>
        /// <returns>An encapsulated standard execution payload tracking confirmation data matrices.</returns>
        public async Task<ApiResponse<CreateAccountResponse>> Process(CreateAccountRequest request)
        {
            // Step 1: Initialize sequential control tracking validation status signals
            bool isPhoneDuplicated = false;
            bool isEmailDuplicated = false;

            // Step 2: Query background configurations testing whether the phone entity metadata matches active records
            isPhoneDuplicated = await CheckPhoneExistence(request.Phone);

            // Step 3: Probes existing context tables tracing whether incoming non-null emails conflict with active fields
            isEmailDuplicated = await CheckEmailExistence(request.Email);

            // Step 4: Map business payload criteria onto new physical domain model layout data
            var userEntity = MapToEntity(request);

            // Step 5: Execute database record insertions persisting context modifications onto active storage blocks
            var executionResult = await SaveUserRecord(userEntity, isPhoneDuplicated, isEmailDuplicated);

            // Step 6: Direct internal data attributes transformation targets mapping properties into serialized output fields
            var responsePayload = MapToResponseDto(userEntity);

            // Step 7: Synthesize structural output payload structures mapping control flags accurately across boundary interfaces
            return CreateResponse(responsePayload, isPhoneDuplicated, isEmailDuplicated);
        }

        /// <summary>
        /// Checks whether the phone number is already registered inside active operational tables.
        /// </summary>
        /// <param name="phone">The phone context string parameter tracking identity strings.</param>
        /// <returns>True if the matching identity details map structural conflicts, otherwise false.</returns>
        private async Task<bool> CheckPhoneExistence(string phone)
        {
            return await _userRepository
                .FindByCondition(x => x.Phone == phone, trackChanges: false)
                .AnyAsync();
        }

        /// <summary>
        /// Checks whether the specified non-null email already occupies record cells inside database configurations.
        /// </summary>
        /// <param name="email">The email signature criteria tracing identification structures.</param>
        /// <returns>True if a validation overlap matches the targeted input block, otherwise false.</returns>
        private async Task<bool> CheckEmailExistence(string? email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return false;
            }
            return await _userRepository
                .FindByCondition(x => x.Email == email, trackChanges: false)
                .AnyAsync();
        }

        /// <summary>
        /// Executes asynchronous persistence tracking insertions guarding executions using precondition flag statuses.
        /// </summary>
        /// <param name="user">The physical model entity target instance mapping operations data lines.</param>
        /// <param name="isPhoneDuplicated">Flag capturing structural baseline matching collisions regarding user phone fields.</param>
        /// <param name="isEmailDuplicated">Flag capturing record identity matching collisions regarding user email rows.</param>
        /// <returns>A runtime tracking task returning absolute persisted database record confirmation signatures.</returns>
        private async Task<Guid> SaveUserRecord(User user, bool isPhoneDuplicated, bool isEmailDuplicated)
        {
            if (isPhoneDuplicated || isEmailDuplicated)
            {
                return Guid.Empty;
            }
            await _userRepository.CreateAsync(user);
            await _userRepository.SaveChangesAsync();
            return user.Id;
        }

        /// <summary>
        /// Transforms incoming business parameters directly onto underlying storage domain layouts.
        /// </summary>
        /// <param name="request">The data container detailing incoming setup inputs parameters.</param>
        /// <returns>A ready physical domain core entity structure package configuration.</returns>
        private User MapToEntity(CreateAccountRequest request)
        {
            return new User
            {
                Phone = request.Phone.Trim(),
                Email = request.Email?.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName.Trim(),
                Role = request.Role,
                IsActive = true,
                AvatarUrl = request.AvatarUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Projects system infrastructure entities configurations back into decoupled application transport representations.
        /// </summary>
        /// <param name="user">The internal physical domain source model metadata tracking block.</param>
        /// <returns>A standardized clean serializable model representation instance context.</returns>
        private CreateAccountResponse MapToResponseDto(User user)
        {
            return new CreateAccountResponse
            {
                // Transforming entity layout properties onto output data representation boundaries
                Id = user.Id,
                Phone = user.Phone,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                IsActive = user.IsActive,
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            };
        }

        /// <summary>
        /// Builds structured result encapsulation envelopes parsing runtime metric signals safely.
        /// </summary>
        /// <param name="result">The finalized response representation body target projected fields context.</param>
        /// <param name="isPhoneDuplicated">Validation flag mapping system duplicate identification metrics.</param>
        /// <param name="isEmailDuplicated">Validation flag mapping system duplicate metadata tracks.</param>
        /// <returns>The standardized unified transport wrapper enclosing transaction outputs.</returns>
        private ApiResponse<CreateAccountResponse> CreateResponse(CreateAccountResponse result, bool isPhoneDuplicated, bool isEmailDuplicated)
        {
            var errorResponse = CreateErrorResponse(isPhoneDuplicated, isEmailDuplicated);
            if (errorResponse != null)
            {
                return errorResponse;
            }
            return ApiResponse<CreateAccountResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                result);
        }

        /// <summary>
        /// Isolates logical checkpoint calculations emitting tailored domain validation standard error envelopes.
        /// </summary>
        /// <param name="isPhoneDuplicated">Indicator identifying telephone property collision events.</param>
        /// <param name="isEmailDuplicated">Indicator identifying mail address properties conflict matches.</param>
        /// <returns>A failed API standard variant descriptor token if validation thresholds trip; otherwise null properties.</returns>
        private ApiResponse<CreateAccountResponse>? CreateErrorResponse(bool isPhoneDuplicated, bool isEmailDuplicated)
        {
            if (isPhoneDuplicated)
            {
                return ApiResponse<CreateAccountResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4018.ToString());
            }
            if (isEmailDuplicated)
            {
                return ApiResponse<CreateAccountResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4017.ToString());
            }
            return null;
        }
    }
}