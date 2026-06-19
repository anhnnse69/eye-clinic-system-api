using ECS.Application.Common.Response;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistSearchAccountServices
{
    /// <summary>
    /// Handles data layer discovery pipelines and query orchestration boundaries for user identity tracking.
    /// </summary>
    public class ReceptionistSearchAccountService : IReceptionistSearchAccountService
    {
        private readonly IRepositoryQueryBase<User, Guid, AppDbContext> _userQueryRepository;

        /// <summary>
        /// Initializes a new instance of <see cref="ReceptionistSearchAccountService"/> with structural read-only repositories.
        /// </summary>
        /// <param name="userQueryRepository">The query-only tracking repository interface for retrieving user records.</param>
        public ReceptionistSearchAccountService(IRepositoryQueryBase<User, Guid, AppDbContext> userQueryRepository)
        {
            _userQueryRepository = userQueryRepository;
        }

        /// <summary>
        /// Pipeline coordinator driving account search workflow without ANY if conditions or query assembly.
        /// </summary>
        /// <param name="request">The filtration and context parameters bundle for the processing lookup request.</param>
        /// <returns>A structured <see cref="ApiResponse{T}"/> packing matched transaction presentation data matrices.</returns>
        public async Task<ApiResponse<List<ReceptionistSearchAccountResponse>>> Process(ReceptionistSearchAccountRequest request)
        {
            // Step 1: Delegate query evaluation processes down to underlying data layer streams (Scanning mixed role contexts)
            var matchedUsers = await ExecuteFilteredUserQuery(request);
            // Step 2: Transcribe internal aggregate models up into destination DTO layout representations
            var outputPayload = MapToResponseList(matchedUsers);
            // Step 3: Seal results inside standardized API packet wrappers
            return ApiResponse<List<ReceptionistSearchAccountResponse>>.Success("APP_MESSAGE_2000", outputPayload);
        }

        /// <summary>
        /// Initializes, structures, and triggers dynamic user entity lookup pipelines.
        /// </summary>
        /// <param name="request">The raw presentation parameters model holding filtering parameters data metrics.</param>
        /// <returns>A finalized list mapping active <see cref="User"/> graph segments.</returns>
        private async Task<List<User>> ExecuteFilteredUserQuery(ReceptionistSearchAccountRequest request)
        {
            // Scan through legacy database variants encompassing mixed role data layout expressions ("0" and "PATIENT")
            var query = _userQueryRepository.FindByCondition(
                u => u.IsActive && (u.Role.ToString() == "0" || u.Role.ToString().ToUpper() == "PATIENT"),
                trackChanges: false
            );
            // Append variable query filtering conditions based on incoming client-side payloads
            string? nameFilter = request.FullName?.Trim().ToLower();
            string? phoneFilter = request.Phone?.Trim();
            string? emailFilter = request.Email?.Trim().ToLower();
            query = query
                .Where(u => string.IsNullOrEmpty(nameFilter) || u.FullName.ToLower().Contains(nameFilter))
                .Where(u => string.IsNullOrEmpty(phoneFilter) || u.Phone.Contains(phoneFilter))
                .Where(u => string.IsNullOrEmpty(emailFilter) || (u.Email != null && u.Email.ToLower().Contains(emailFilter)));
            return await query.ToListAsync();
        }

        /// <summary>
        /// Converts and flattens database entity structural collections down into brief flat presentation feedback arrays.
        /// </summary>
        /// <param name="users">The target list graph collection tracking core matching database users.</param>
        /// <returns>A mapped list data structure detailing matched user metrics items.</returns>
        private List<ReceptionistSearchAccountResponse> MapToResponseList(List<User> users)
        {
            return users.Select(u => new ReceptionistSearchAccountResponse
            {
                Id = u.Id.ToString(),
                FullName = u.FullName,
                Phone = u.Phone,
                Email = u.Email
            }).ToList();
        }
    }
}