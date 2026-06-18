using ECS.Application.Common.Response;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistUpdatePatientProfileServices
{
    /// <summary>
    /// Handles business mutations and orchestration workflow loops ensuring data containment boundaries.
    /// </summary>
    public class ReceptionistUpdatePatientProfileService : IReceptionistUpdatePatientProfileService
    {
        private readonly IRepositoryBaseAsync<PatientProfile, Guid, AppDbContext> _patientRepository;

        /// <summary>
        /// Initializes a new instance of <see cref="ReceptionistUpdatePatientProfileService"/> with structural mutation repositories.
        /// </summary>
        /// <param name="patientRepository">The transactional repository for tracking and mutating patient records.</param>
        public ReceptionistUpdatePatientProfileService(IRepositoryBaseAsync<PatientProfile, Guid, AppDbContext> patientRepository)
        {
            _patientRepository = patientRepository;
        }

        /// <summary>
        /// Pipeline coordinator driving patient administrative field updates without structural branching rules.
        /// </summary>
        /// <param name="patientId">The unique structural identifier tracking the target patient database row context.</param>
        /// <param name="request">The filtration and context parameters bundle for the processing mutation request.</param>
        /// <returns>A structured <see cref="ApiResponse{ReceptionistUpdatePatientProfileResponse}"/> packing target transaction outcomes telemetry blocks.</returns>
        public async Task<ApiResponse<ReceptionistUpdatePatientProfileResponse>> Process(Guid patientId, ReceptionistUpdatePatientProfileRequest request)
        {
            // Step 1: Retrieve the target entity from database layer with tracked tracking contexts
            var patientEntity = await FetchCorePatientOrThrow(patientId);
            // Step 2: Mutate specific properties following exact order required by UI forms
            var mutatedEntity = ApplyFormMappingInOrder(patientEntity, request);
            // Step 3: Persist modified entity state frames into tracking layers
            await CommitDatabaseMutations(mutatedEntity);
            // Step 4: Map outcome aggregate data structures down into presentation blocks
            var responseDto = BuildFinalizedDto(mutatedEntity);
            // Step 5: Seal results inside standardized API packet wrappers
            return WrapSuccessOutcome(responseDto);
        }

        /// <summary>
        /// Retrieves the foundational patient profile tracking record, forcing tracking contexts or triggering error flows.
        /// </summary>
        /// <param name="patientId">The unique identifier of the requested target patient profile record.</param>
        /// <returns>The matched active <see cref="PatientProfile"/> graph context.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the patient context target cannot be located inside underlying layers.</exception>
        private async Task<PatientProfile> FetchCorePatientOrThrow(Guid patientId)
        {
            var entity = await _patientRepository.FindAll(trackChanges: true)
                .FirstOrDefaultAsync(p => p.Id == patientId);
            return entity ?? throw new KeyNotFoundException("APP_MESSAGE_4004"); // Patient Not Found
        }

        /// <summary>
        /// Transcribes incoming data fields down onto the aggregate record matching the input layout flow sequence.
        /// </summary>
        /// <param name="entity">The tracking aggregate database record targeting patient profiles.</param>
        /// <param name="request">The raw presentation parameters model holding requested mutation metrics.</param>
        /// <returns>The mutated <see cref="PatientProfile"/> aggregate graph ready for serialization mappings.</returns>
        private PatientProfile ApplyFormMappingInOrder(PatientProfile entity, ReceptionistUpdatePatientProfileRequest request)
        {
            // Mutate strictly following UI input layout flow
            entity.FullName = request.FullName.Trim();
            entity.Gender = Enum.Parse<Gender>(request.Gender.ToUpper(), true);
            entity.Dob = DateTime.ParseExact(request.Dob, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None);
            entity.PhoneNumber = request.PhoneNumber?.Trim();
            entity.Address = request.Address?.Trim();
            entity.IdentityNumber = request.IdentityNumber?.Trim();
            entity.BhytNumber = request.BhytNumber?.Trim()?.ToUpper();
            // System baseline logs bookkeeping
            entity.UpdatedAt = DateTime.UtcNow;
            return entity;
        }

        /// <summary>
        /// Forces downstream state flushes persisting mutated tracking frames down into database layers.
        /// </summary>
        /// <param name="entity">The updated aggregate profile reference targeting actual transactional records.</param>
        private async Task CommitDatabaseMutations(PatientProfile entity)
        {
            await _patientRepository.UpdateAsync(entity);
            await _patientRepository.SaveChangesAsync();
        }

        /// <summary>
        /// Converts and maps aggregate entity tracking graphs into brief presentation tracking feedback models.
        /// </summary>
        /// <param name="entity">The finalized modified core patient database entity model graph.</param>
        /// <returns>A mapped flat data structure detailing transactional feedback telemetry.</returns>
        private ReceptionistUpdatePatientProfileResponse BuildFinalizedDto(PatientProfile entity)
        {
            return new ReceptionistUpdatePatientProfileResponse
            {
                Id = entity.Id.ToString(),
                FullName = entity.FullName,
                UpdatedAt = entity.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        /// <summary>
        /// Determines standard structure API return outcomes based on the internal mutation evaluation.
        /// </summary>
        /// <param name="dto">The compiled response model holding output metrics data items.</param>
        /// <returns>A normalized outcome structure wrapping corresponding tracking markers.</returns>
        private ApiResponse<ReceptionistUpdatePatientProfileResponse> WrapSuccessOutcome(ReceptionistUpdatePatientProfileResponse dto)
        {
            return ApiResponse<ReceptionistUpdatePatientProfileResponse>.Success("APP_MESSAGE_2000", dto);
        }
    }
}