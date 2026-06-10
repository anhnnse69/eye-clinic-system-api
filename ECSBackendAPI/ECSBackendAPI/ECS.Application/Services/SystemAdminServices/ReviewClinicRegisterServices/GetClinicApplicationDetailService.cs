using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;

namespace ECS.Application.Services.SystemAdminServices.ReviewClinicRegisterServices
{
    /// <summary>
    /// Handles the business logic for retrieving details of a specific clinic registration application.
    /// </summary>
    public class GetClinicApplicationDetailService : IGetClinicApplicationDetailService
    {
        private readonly IRepositoryQueryBase<ClinicRegistrationRequest, Guid, AppDbContext> _queryRepo;

        /// <summary>
        /// Initializes a new instance of <see cref="GetClinicApplicationDetailService"/> with required dependencies.
        /// </summary>
        /// <param name="queryRepo">Repository for querying clinic registration requests.</param>
        public GetClinicApplicationDetailService(IRepositoryQueryBase<ClinicRegistrationRequest, Guid, AppDbContext> queryRepo)
        {
            _queryRepo = queryRepo;
        }

        /// <summary>
        /// Processes the request to get detailed information of a clinic application by its identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the clinic registration application.</param>
        /// <returns>An <see cref="ApiResponse{GetClinicApplicationDetailResponse}"/> containing the detailed application data.</returns>
        public async Task<ApiResponse<GetClinicApplicationDetailResponse>> Process(Guid id)
        {
            // Fetch application entity from storage or throw exception response if not found
            var application = await FetchApplicationOrThrow(id);
            // Map database entity onto UI responsive structure
            var responseData = MapToResponseDto(application);
            // Return standardized API envelope response
            return CreateApiResponse(responseData);
        }

        /// <summary>
        /// Retrieves the clinic registration request or throws a custom exception if not found.
        /// </summary>
        private async Task<ClinicRegistrationRequest> FetchApplicationOrThrow(Guid id)
        {
            var application = await _queryRepo.GetByIdAsync(id);
            if (application == null)
            {
                // Throw a custom exception with the error code, or handle the error according to the project's architecture.
                // This example assumes the system uses an exception to break the flow, but you can adjust the throw strategy if needed.
                throw new KeyNotFoundException(GeneralCode.APP_MESSAGE_4029.ToString());
            }
            return application;
        }

        /// <summary>
        /// Maps internal data persistence items onto explicit outbound properties.
        /// </summary>
        private GetClinicApplicationDetailResponse MapToResponseDto(ClinicRegistrationRequest app)
        {
            return new GetClinicApplicationDetailResponse
            {
                Id_clinic_registration = app.Id.ToString(),
                ClinicName = app.ClinicName,
                ClinicAddress = app.ClinicAddress,
                ContactName = app.ContactName,
                ContactPhone = app.ContactPhone,
                ContactEmail = app.ContactEmail,
                BusinessLicenseUrl = app.BusinessLicenseUrl,
                Status = app.Status,
                ReviewNote = app.ReviewNote,
                RequestedAt = app.RequestedAt.ToString("dd/MM/yyyy HH:mm")
            };
        }

        /// <summary>
        /// Packages mapped data objects into the official ApiResponse wrapper structure.
        /// </summary>
        private ApiResponse<GetClinicApplicationDetailResponse> CreateApiResponse(GetClinicApplicationDetailResponse data)
        {
            return ApiResponse<GetClinicApplicationDetailResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                data
            );
        }
    }
}