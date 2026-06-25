using ECS.Application.Common.Response;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.SystemAdminServices.AdminSystemDeleteAccountServices
{
    /// <summary>
    /// Handles user account soft-deletion and status mutation flows using dedicated transactions.
    /// </summary>
    public class DeleteAccountService : IDeleteAccountService
    {
        private readonly IRepositoryBaseAsync<User, Guid, AppDbContext> _userRepository;

        /// <summary>
        /// Initializes a new instance of <see cref="DeleteAccountService"/> with state mutation repositories.
        /// </summary>
        /// <param name="userRepository">Repository wrapper exposing asynchronous physical record manipulation channels.</param>
        public DeleteAccountService(IRepositoryBaseAsync<User, Guid, AppDbContext> userRepository)
        {
            _userRepository = userRepository;
        }

        /// <summary>
        /// Processes account state changes sequentially utilizing abstract logical sub-blocks without condition forks.
        /// </summary>
        /// <param name="request">The tracking parameter context containing targets and status criteria indicators.</param>
        /// <returns>An encapsulated <see cref="ApiResponse{DeleteAccountResponse}"/> indicating success state execution flags.</returns>
        public async Task<ApiResponse<DeleteAccountResponse>> Process(DeleteAccountRequest request)
        {
            // Step 1: Initialize sequential persistence tracking control status validation variables
            bool isUserExist = true;

            // Step 2: Query the underlying master database table to retrieve the exact user context matching request tokens
            var user = await RetrieveUserEntity(request.UserId);

            // Step 3: Evaluate target record persistence states and set layout constraint flags dynamically
            ValidateEntityState(user, ref isUserExist);

            // Step 4: Apply mutations directly over internal parameters and persist records down to transaction units
            await MutateAndSaveStatus(user!, request.IsActive, isUserExist);

            // Step 5: Map internal domain layout fields safely into serialization presentation business objects
            var payload = MapToResponse(user, request.IsActive, isUserExist);

            // Step 6: Formulate application envelope payload tracking structures to compile unified response blocks
            return CreateResponse(payload, isUserExist);
        }

        /// <summary>
        /// Extracts matching user records from active relational entity sets using explicitly specified type lookups.
        /// </summary>
        /// <param name="userId">The tracking physical resource identifier key parameter token.</param>
        /// <returns>A target operational <see cref="User"/> entity layout reference match if present; otherwise null.</returns>
        private async Task<User?> RetrieveUserEntity(Guid userId)
        {
            return await _userRepository
                .FindByCondition(x => x.Id == userId, trackChanges: true)
                .FirstOrDefaultAsync<User>();
        }

        /// <summary>
        /// Analyzes current entity references to flag non-existent items without breaking global method layout workflows.
        /// </summary>
        /// <param name="user">The active tracking user domain object model entity representation reference graph.</param>
        /// <param name="isUserExist">Status variable flag targeted for modification upon parsing misses.</param>
        private void ValidateEntityState(User? user, ref bool isUserExist)
        {
            if (user == null)
            {
                isUserExist = false;
            }
        }

        /// <summary>
        /// Alters active configuration states on tracked nodes and commits operational pipeline adjustments safely.
        /// </summary>
        /// <param name="user">The active entity structure boundary to undergo modifications.</param>
        /// <param name="targetStatus">The input target activation parameters to assign over target objects.</param>
        /// <param name="isUserExist">Precondition execution check metric protecting write pathways.</param>
        /// <returns>A generic asynchronous state task model tracker instance.</returns>
        private async Task MutateAndSaveStatus(User user, bool targetStatus, bool isUserExist)
        {
            if (!isUserExist)
            {
                return;
            }

            user.IsActive = targetStatus;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();
        }

        /// <summary>
        /// Projects model values into dedicated transport schemas tracking exact modified coordinates safely.
        /// </summary>
        /// <param name="user">The primary storage node containing operational data properties.</param>
        /// <param name="fallbackStatus">The targeted structural status tracking value used if nodes are null.</param>
        /// <param name="isUserExist">Execution validation tracking metric flags checking lookup outcomes.</param>
        /// <returns>A decoupled business presentation model snapshot reflecting state modifications.</returns>
        private DeleteAccountResponse MapToResponse(User? user, bool fallbackStatus, bool isUserExist)
        {
            if (!isUserExist || user == null)
            {
                return new DeleteAccountResponse
                {
                    UserId = Guid.Empty,
                    IsActive = fallbackStatus,
                    UpdatedAt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm")
                };
            }

            return new DeleteAccountResponse
            {
                // Mapping physical core system user keys onto the response entity fields
                UserId = user.Id,
                IsActive = user.IsActive,
                UpdatedAt = user.UpdatedAt.ToString("dd/MM/yyyy HH:mm")
            };
        }

        /// <summary>
        /// Assembles appropriate standard error descriptors or successful execution payload packaging layers.
        /// </summary>
        /// <param name="payload">The serializable data data response payload projected from service layers.</param>
        /// <param name="isUserExist">Validation controller mapping token tracking physical verification items.</param>
        /// <returns>A final completed application core transportation capsule.</returns>
        private ApiResponse<DeleteAccountResponse> CreateResponse(DeleteAccountResponse payload, bool isUserExist)
        {
            if (!isUserExist)
            {
                return ApiResponse<DeleteAccountResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4020.ToString());
            }

            return ApiResponse<DeleteAccountResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                payload);
        }
    }
}