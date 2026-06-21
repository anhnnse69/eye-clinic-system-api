using ECS.Domain.Entities.Patient;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Common.Helpers
{
    /// <summary>
    /// Provides shared domain processing helpers to determine and evaluate the authorization boundary matrices for patient profiles mapping onto user contexts.
    /// </summary>
    public static class PatientProfileAccessHelper
    {
        /// <summary>
        /// Resolves comprehensive, distinct collection of primary key references identifying all patient records a user is authorized to interact with.
        /// </summary>
        /// <param name="userId">The unique tracking authenticated principal operator identity vector token.</param>
        /// <param name="patientProfileRepository">Repository boundary instance for querying physical patient relationship profile records.</param>
        /// <param name="context">The underlying infrastructure entity framework core database contextual session unit.</param>
        /// <returns>A collection containing unique tracking structural primary key tokens evaluating to authorized profile entities.</returns>
        public static async Task<List<Guid>> GetAccessibleProfileIdsAsync(
            Guid userId,
            IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> patientProfileRepository,
            AppDbContext context)
        {
            // Gather profiles bound directly through physical core entity profile mappings
            var directProfileIds = await patientProfileRepository
                .FindByCondition(x => x.UserId == userId, trackChanges: false)
                .Select(x => x.Id)
                .ToListAsync();

            // Gather profiles bound indirectly through interconnected structural linkage maps
            var linkedProfileIds = await context.Set<UserPatient>()
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.PatientId)
                .ToListAsync();

            // Merge sets executing evaluate algorithms to remove duplicate references safely
            return directProfileIds.Union(linkedProfileIds).Distinct().ToList();
        }

        /// <summary>
        /// Enforces domain boundary isolation policies to verify whether a specific patient profile matches the active user's access matrix.
        /// </summary>
        /// <param name="userId">The unique enterprise primary reference coordinates verification identifier context token.</param>
        /// <param name="patientProfileId">The targeted physical structural patient identifier key validation block mapping details.</param>
        /// <param name="patientProfileRepository">Repository boundary instance for querying physical patient relationship profile records.</param>
        /// <param name="context">The underlying infrastructure entity framework core database contextual session unit.</param>
        /// <returns>A structure evaluating state indicator returning true if context verification matches baseline bounds successfully.</returns>
        public static async Task<bool> IsProfileAccessibleAsync(
            Guid userId,
            Guid patientProfileId,
            IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> patientProfileRepository,
            AppDbContext context)
        {
            var ids = await GetAccessibleProfileIdsAsync(userId, patientProfileRepository, context);
            return ids.Contains(patientProfileId);
        }
    }
}