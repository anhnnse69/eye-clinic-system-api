using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.SystemAdminServices.GetClinicLookupServices
{
    /// <summary>
    /// Implementation handler orchestrating lightweight identity extractions across data layer boundaries.
    /// </summary>
    public class GetClinicLookupService : IGetClinicLookupService
    {
        private readonly IRepositoryQueryBase<Clinic, Guid, AppDbContext> _clinicRepository;

        /// <summary>
        /// Initializes dependencies handling infrastructure query bindings.
        /// </summary>
        /// <param name="clinicRepository">Repository bound instance for querying underlying clinic data models.</param>
        public GetClinicLookupService(IRepositoryQueryBase<Clinic, Guid, AppDbContext> clinicRepository)
        {
            _clinicRepository = clinicRepository;
        }

        /// <summary>
        /// Processes the internal data pipeline workflow to parse active records and map output collections cleanly.
        /// </summary>
        /// <param name="request">The structural lookup boundary criteria definition settings context.</param>
        /// <returns>An <see cref="ApiResponse{List{GetClinicLookupResponse}}"/> enclosing descriptive state payloads.</returns>
        public async Task<ApiResponse<List<GetClinicLookupResponse>>> Process(GetClinicLookupRequest request)
        {
            // Step 1: Query underlying data structures filtering exclusive active configurations without staff
            var activeClinicsWithoutStaff = await FetchActiveClinicsWithoutStaffFromRepository();

            // Step 2: Map internal domain state model attributes onto decoupled serialized lookup schemas
            var outputDtoList = MapToResponseDtoList(activeClinicsWithoutStaff);

            // Step 3: Package structural items cleanly inside consistent transmission frames
            return CreateResponse(outputDtoList);
        }

        /// <summary>
        /// Queries the data store using explicit transactional tracking skips to grab active system entities with NO staff.
        /// </summary>
        /// <returns>The verified raw collection matching active clinic indicators with no assigned staff.</returns>
        private async Task<List<Clinic>> FetchActiveClinicsWithoutStaffFromRepository()
        {
            return await _clinicRepository
                .FindByCondition(
                    c => c.IsActive && (c.StaffClinics == null || !c.StaffClinics.Any()),
                    trackChanges: false
                )
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Transforms internal persistent context database model list directly onto business serialization object schemas.
        /// </summary>
        /// <param name="clinics">The physical domain core model list array returned directly out of database layers.</param>
        /// <returns>A structured target presentation lookup data collection instance context.</returns>
        private List<GetClinicLookupResponse> MapToResponseDtoList(List<Clinic> clinics)
        {
            return clinics.Select(c => new GetClinicLookupResponse
            {
                Id = c.Id,
                Name = c.Name
            }).ToList();
        }

        /// <summary>
        /// Packages structural responses cleanly inside consistent transmission frames.
        /// </summary>
        /// <param name="dtoList">The compiled lookup mapping data array contract object instance.</param>
        /// <returns>A configured standard API payload frame handling serialization workflows.</returns>
        private ApiResponse<List<GetClinicLookupResponse>> CreateResponse(List<GetClinicLookupResponse> dtoList)
        {
            return ApiResponse<List<GetClinicLookupResponse>>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                dtoList
            );
        }
    }
}