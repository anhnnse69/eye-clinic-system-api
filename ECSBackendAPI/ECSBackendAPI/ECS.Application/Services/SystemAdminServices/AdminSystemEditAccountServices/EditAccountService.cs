using Microsoft.EntityFrameworkCore;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ECS.Application.Services.SystemAdminServices.AdminSystemEditAccountServices
{
    /// <summary>
    /// Handles user profile mutation operations by verifying global context constraints and persistence protocols.
    /// </summary>
    public class EditAccountService : IEditAccountService
    {
        private readonly IRepositoryBaseAsync<User, Guid, AppDbContext> _userRepository;

        /// <summary>
        /// Initializes a new instance of <see cref="EditAccountService"/> injectively parsing underlying database bridges.
        /// </summary>
        /// <param name="userRepository">Repository boundary instance governing core identity security profile rows.</param>
        public EditAccountService(IRepositoryBaseAsync<User, Guid, AppDbContext> userRepository)
        {
            _userRepository = userRepository;
        }

        /// <summary>
        /// Sequentially processes user update metrics while tracking execution integrity states without conditional branch blocks.
        /// </summary>
        /// <param name="request">The specific criteria parameter modifications details container model.</param>
        /// <returns>An encapsulated <see cref="ApiResponse{EditAccountResponse}"/> payload containing operations outcomes metrics.</returns>
        public async Task<ApiResponse<EditAccountResponse>> Process(EditAccountRequest request)
        {
            // Step 1: Extract historical user account entity records matching request keys using Tuple outputs instead of prohibited ref parameters
            var accountDetail = await FetchUserEntity(request.Id);
            var systemUser = accountDetail.User;
            bool isUserExist = accountDetail.IsExist;

            // Step 2: Check data store constraints to guarantee uniqueness on phone and email records via standard Tuple pipelines
            var constraintDetail = await VerifyUniqueConstraints(request, isUserExist);
            bool isPhoneDuplicate = constraintDetail.IsPhoneDuplicate;
            bool isEmailDuplicate = constraintDetail.IsEmailDuplicate;

            // Step 3: Perform transactional persistence operations updating internal tracking parameters models
            var updatedUser = await UpdatePersistedData(systemUser, request, isUserExist, isPhoneDuplicate, isEmailDuplicate);

            // Step 4: Synthesize standard transport envelope responses packaging tracked parameters variables outcomes
            return CreateResponse(updatedUser, isUserExist, isPhoneDuplicate, isEmailDuplicate);
        }

        /// <summary>
        /// Queries the physical storage database layers targeting active records explicitly mapping back tracking identifiers via explicit EF extension utilities.
        /// </summary>
        private async Task<(User? User, bool IsExist)> FetchUserEntity(Guid userId)
        {
            var query = _userRepository.FindByCondition(x => x.Id == userId);

            // Explicitly utilize EntityFrameworkQueryableExtensions to bypass third party AsyncEnumerable conflicts cleanly
            var user = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(query);

            return (user, user != null);
        }

        /// <summary>
        /// Interrogates underlying domain indices ensuring alternate accounts do not cross-claim vital properties coordinates.
        /// </summary>
        private async Task<(bool IsPhoneDuplicate, bool IsEmailDuplicate)> VerifyUniqueConstraints(EditAccountRequest request, bool proceedCheck)
        {
            if (!proceedCheck) return (false, false);

            // Evaluate phone number uniqueness across alternative account identities via dedicated EF method resolution bridges
            var phoneQuery = _userRepository.FindByCondition(x => x.Id != request.Id && x.Phone == request.Phone);
            bool isPhoneDuplicate = await EntityFrameworkQueryableExtensions.AnyAsync(phoneQuery);

            bool isEmailDuplicate = false;
            // Evaluate email address uniqueness across alternative account identities if provided
            if (!string.IsNullOrEmpty(request.Email))
            {
                var emailQuery = _userRepository.FindByCondition(x => x.Id != request.Id && x.Email == request.Email);
                isEmailDuplicate = await EntityFrameworkQueryableExtensions.AnyAsync(emailQuery);
            }

            return (isPhoneDuplicate, isEmailDuplicate);
        }

        /// <summary>
        /// Maps parameters updates onto internal tracking properties rows invoking backend tracking units save routines.
        /// </summary>
        private async Task<User?> UpdatePersistedData(User? user, EditAccountRequest request, bool isUserExist, bool isPhoneDuplicate, bool isEmailDuplicate)
        {
            if (!isUserExist || isPhoneDuplicate || isEmailDuplicate || user == null)
            {
                return null;
            }

            // Sync modifications into the tracking domain object (status field is handled separately via soft-delete later)
            user.Phone = request.Phone;
            user.Email = request.Email;
            user.FullName = request.FullName;
            user.Role = request.Role;
            user.AvatarUrl = request.AvatarUrl;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();
            return user;
        }

        /// <summary>
        /// Evaluates overall state flags to issue systematic failed specifications structures or successful final structures.
        /// </summary>
        private ApiResponse<EditAccountResponse> CreateResponse(User? updatedUser, bool isUserExist, bool isPhoneDuplicate, bool isEmailDuplicate)
        {
            var errorResponse = EvaluateValidationState(isUserExist, isPhoneDuplicate, isEmailDuplicate);
            if (errorResponse != null)
            {
                return errorResponse;
            }

            return ApiResponse<EditAccountResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                MapToResponse(updatedUser!));
        }

        /// <summary>
        /// Analyzes runtime condition boundaries maps to construct explicit systemic client schemas failures descriptors.
        /// </summary>
        private ApiResponse<EditAccountResponse>? EvaluateValidationState(bool isUserExist, bool isPhoneDuplicate, bool isEmailDuplicate)
        {
            if (!isUserExist)
            {
                return ApiResponse<EditAccountResponse>.Fail(GeneralCode.APP_MESSAGE_4020.ToString());
            }
            if (isPhoneDuplicate)
            {
                return ApiResponse<EditAccountResponse>.Fail(GeneralCode.APP_MESSAGE_4018.ToString());
            }
            if (isEmailDuplicate)
            {
                return ApiResponse<EditAccountResponse>.Fail(GeneralCode.APP_MESSAGE_4017.ToString());
            }
            return null;
        }

        /// <summary>
        /// Transforms persistent physical infrastructure data contexts configurations directly onto business serialization object schemas.
        /// </summary>
        private EditAccountResponse MapToResponse(User user)
        {
            return new EditAccountResponse
            {
                // Mapping the physical persistent account index reference key coordinates
                Id = user.Id,
                // Projecting contact reference phone strings parameters fields details
                Phone = user.Phone,
                // Mapping fallback mailing credentials metrics properties definitions
                Email = user.Email,
                // Assigning textual localized user display name label values
                FullName = user.FullName,
                // Transforming programmatic enum states properties directly onto string structures descriptions
                Role = user.Role.ToString(),
                // Copying structural web graphics link parameters attributes
                AvatarUrl = user.AvatarUrl,
                // Converting operational timezone benchmarks details into standard representation tracking strings
                UpdatedAt = user.UpdatedAt.ToString("dd/MM/yyyy HH:mm")
            };
        }
    }
}