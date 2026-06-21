    using ECS.Application.Common.Response;
    using ECS.Domain.Entities.Clinics;
    using ECS.Domain.Enums;
    using ECS.Infrastructure.Persistence;
    using ECS.Infrastructure.Repositories.Interfaces;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using System.Security.Claims;

    namespace ECS.Application.Services.ClinicAdminManagementServices.MedicineCatalogServices.Delete
    {
        /// <summary>
        /// Processes application layer operational logic to toggle availability/lock states on targeted medicine records.
        /// </summary>
        public class DeleteMedicineCatalogService : IDeleteMedicineCatalogService
        {
            private readonly IRepositoryBaseAsync<MedicineCatalog, Guid, AppDbContext> _medicineCatalogRepository;
            private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffClinicRepository;
            private readonly IHttpContextAccessor _httpContextAccessor;

            /// <summary>
            /// Initializes a new instance of <see cref="DeleteMedicineCatalogService"/> alongside required data boundaries.
            /// </summary>
            /// <param name="medicineCatalogRepository">Write/Read repository variant managing target item datasets.</param>
            /// <param name="staffClinicRepository">Query abstraction interface assessing administrator organizational bindings.</param>
            /// <param name="httpContextAccessor">Context provider identifying incoming claim profiles securely.</param>
            public DeleteMedicineCatalogService(
                IRepositoryBaseAsync<MedicineCatalog, Guid, AppDbContext> medicineCatalogRepository,
                IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffClinicRepository,
                IHttpContextAccessor httpContextAccessor)
            {
                _medicineCatalogRepository = medicineCatalogRepository;
                _staffClinicRepository = staffClinicRepository;
                _httpContextAccessor = httpContextAccessor;
            }

            /// <summary>
            /// Orchestrates state changes across isolated validation boundaries without utilizing inline branching constructs.
            /// </summary>
            /// <param name="request">Contextual entity model tracking target identification parameter metrics.</param>
            /// <returns>A unified data wrapper capturing operational success or specific contextual error tracking tokens.</returns>
            public async Task<ApiResponse<DeleteMedicineCatalogResponse>> Process(DeleteMedicineCatalogRequest request)
            {
                // Step 1: Initialize sequential control tracking variables to evaluate logical health
                bool isUserValid = true;
                bool isClinicValid = true;
                bool isItemExist = true;

                // Step 2: Safe claim parsing pipeline extraction fetching the active unique user identity reference coordinates
                Guid userId = RetrieveUserId(ref isUserValid);

                // Step 3: Search operational multi-tenant context allocations isolating bound workspace environments
                Guid? clinicId = await RetrieveClinicId(userId, isUserValid);

                // Step 4: Validate clinic presence context and update conditional metric tracking indicators
                ValidateClinicContext(clinicId, isUserValid, ref isClinicValid);

                // Step 5: Probing persistence data units to isolate the correct target object structure records
                MedicineCatalog? catalogItem = await FindCatalogEntity(request.Id, clinicId, isClinicValid);

                // Step 6: Validate underlying entity record resolution scores synchronously
                ValidateItemExistence(catalogItem, isClinicValid, ref isItemExist);

                // Step 7: Execute atomic context model updates shifting functional parameters to apply toggle rules
                MedicineCatalog? mutatedItem = await ExecuteStateToggle(catalogItem, isUserValid, isClinicValid, isItemExist);

                // Step 8: Delegate internal dataset objects onto target presentation structures via structural mapping operations
                DeleteMedicineCatalogResponse responseDto = MapToResponse(mutatedItem);

                // Step 9: Finalize response building pipelines passing tracked status criteria parameters explicitly
                return CreateResponse(responseDto, isUserValid, isClinicValid, isItemExist);
            }

            /// <summary>
            /// Extracts user identity pointers from authentication context matrices safely.
            /// </summary>
            /// <param name="isUserValid">Flag reference tracking token context parsing health conditions.</param>
            /// <returns>A tracking Guid token tracking active systems users; otherwise Guid.Empty.</returns>
            private Guid RetrieveUserId(ref bool isUserValid)
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    isUserValid = false;
                    return Guid.Empty;
                }
                return userId;
            }

            /// <summary>
            /// Determines tenant clinic mapping associations linked to the validated account.
            /// </summary>
            /// <param name="userId">The unique identifier tracking active system actors.</param>
            /// <param name="isUserValid">Guarding parameter bypassing lookups on missing session context environments.</param>
            /// <returns>An optional Guid tracing clinic data bounds; otherwise null.</returns>
            private async Task<Guid?> RetrieveClinicId(Guid userId, bool isUserValid)
            {
                if (!isUserValid)
                {
                    return null;
                }

                var staffClinic = await _staffClinicRepository
                    .FindByCondition(x => x.UserId == userId && x.IsActive)
                    .FirstOrDefaultAsync<StaffClinic>(); // Giải quyết thành công nhờ có namespace Microsoft.EntityFrameworkCore

                return staffClinic?.ClinicId;
            }

            /// <summary>
            /// Validates tenant clinic context associations mapping tracking parameters safely.
            /// </summary>
            /// <param name="clinicId">Isolated tracking parameters indicating active tenant layers.</param>
            /// <param name="isUserValid">Guarding pre-condition marker assessing prior layer evaluation scores.</param>
            /// <param name="isClinicValid">Flag tracking environment presence mapping variables.</param>
            private void ValidateClinicContext(Guid? clinicId, bool isUserValid, ref bool isClinicValid)
            {
                if (!isUserValid || !clinicId.HasValue)
                {
                    isClinicValid = false;
                }
            }

            /// <summary>
            /// Targets data rows enforcing ownership metrics boundary constraints strictly.
            /// </summary>
            /// <param name="id">Target identity tracking parameters mapping medicine rows.</param>
            /// <param name="clinicId">Isolated tracking parameters indicating active tenant layers.</param>
            /// <param name="isClinicValid">Guarding pre-condition marker assessing prior layer evaluation scores.</param>
            /// <returns>The active item model matching contextual criteria; otherwise null.</returns>
            private async Task<MedicineCatalog?> FindCatalogEntity(Guid id, Guid? clinicId, bool isClinicValid)
            {
                if (!isClinicValid || !clinicId.HasValue)
                {
                    return null;
                }

                return await _medicineCatalogRepository
                    .FindByCondition(x => x.Id == id && x.ClinicId == clinicId.Value)
                    .FirstOrDefaultAsync<MedicineCatalog>();
            }

            /// <summary>
            /// Evaluates resolved target item record structures checking boundary validations.
            /// </summary>
            /// <param name="item">The physical persistence configuration item entity tracker.</param>
            /// <param name="isClinicValid">Guarding pre-condition marker assessing prior layer evaluation scores.</param>
            /// <param name="isItemExist">Flag tracking entity record resolution scores.</param>
            private void ValidateItemExistence(MedicineCatalog? item, bool isClinicValid, ref bool isItemExist)
            {
                if (!isClinicValid || item == null)
                {
                    isItemExist = false;
                }
            }

            /// <summary>
            /// Performs atomic data mutations processing soft toggle logical inversions.
            /// </summary>
            /// <param name="catalogItem">The physical data layer persistence entity model tracker.</param>
            /// <param name="isUserValid">Operational state gate verifying token signatures.</param>
            /// <param name="isClinicValid">Operational state gate tracking active infrastructure scopes.</param>
            /// <param name="isItemExist">Operational state gate tracking persistence matches.</param>
            /// <returns>The modified context instance structure updated via backend tracking systems.</returns>
            private async Task<MedicineCatalog?> ExecuteStateToggle(MedicineCatalog? catalogItem, bool isUserValid, bool isClinicValid, bool isItemExist)
            {
                if (!isUserValid || !isClinicValid || !isItemExist || catalogItem == null)
                {
                    return null;
                }

                // Perform logical active-state inversion (implements locking/unlocking smoothly)
                catalogItem.IsActive = !catalogItem.IsActive;
                catalogItem.UpdatedAt = DateTime.UtcNow;

                await _medicineCatalogRepository.UpdateAsync(catalogItem);
                await _medicineCatalogRepository.SaveChangesAsync();

                return catalogItem;
            }

            /// <summary>
            /// Transforms persistent business configurations onto serialization boundaries.
            /// </summary>
            /// <param name="item">The mutated internal state tracking domain matrix model instance.</param>
            /// <returns>A mapped decoupled DTO representation data wrapper context container structure.</returns>
            private DeleteMedicineCatalogResponse MapToResponse(MedicineCatalog? item)
            {
                if (item == null)
                {
                    return new DeleteMedicineCatalogResponse();
                }

                return new DeleteMedicineCatalogResponse
                {
                    // Mapping explicit persistent storage primary tracking references
                    Id = item.Id,
                    // Passing targeted domain naming text specifications
                    MedicineName = item.MedicineName,
                    // Assigning freshly altered structural active validation states
                    IsActive = item.IsActive,
                    // Providing explicit localized trace system timing snapshots
                    UpdatedAt = item.UpdatedAt
                };
            }

            /// <summary>
            /// Assembles transportation frameworks analyzing conditional verification execution scores.
            /// </summary>
            /// <param name="responseDto">The data transport payload projected from mutation layers.</param>
            /// <param name="isUserValid">Status parameter measuring authentication claims context scores.</param>
            /// <param name="isClinicValid">Status parameter measuring ecosystem association configurations.</param>
            /// <param name="isItemExist">Status parameter tracking domain row isolation confirmations.</param>
            /// <returns>A tailored standard API framework collection transport module response container.</returns>
            private ApiResponse<DeleteMedicineCatalogResponse> CreateResponse(
                DeleteMedicineCatalogResponse responseDto, bool isUserValid, bool isClinicValid, bool isItemExist)
            {
                if (!isUserValid)
                {
                    return ApiResponse<DeleteMedicineCatalogResponse>.Fail(GeneralCode.APP_MESSAGE_4001.ToString());
                }

                if (!isClinicValid)
                {
                    return ApiResponse<DeleteMedicineCatalogResponse>.Fail(GeneralCode.APP_MESSAGE_4020.ToString());
                }

                if (!isItemExist)
                {
                    return ApiResponse<DeleteMedicineCatalogResponse>.Fail(GeneralCode.APP_MESSAGE_4019.ToString());
                }

                return ApiResponse<DeleteMedicineCatalogResponse>.Success(
                    GeneralCode.APP_MESSAGE_2000.ToString(),
                    responseDto);
            }
        }
    }