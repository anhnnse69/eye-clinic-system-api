using System.Linq.Expressions;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.PatientAppointmentManagementServices.GetClinicServicesForBookingServices
{
    /// <summary>
    /// Handles the business logic for retrieving active services within a specific clinic, formatted for booking selections.
    /// </summary>
    public class GetClinicServicesForBookingService : IGetClinicServicesForBookingService
    {
        private readonly IRepositoryQueryBase<Clinic, Guid, AppDbContext> _clinicRepository;
        private readonly IRepositoryQueryBase<Service, Guid, AppDbContext> _serviceRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetClinicServicesForBookingService"/> class with required infrastructure boundaries.
        /// </summary>
        /// <param name="clinicRepository">Repository boundary instance for querying physical clinic records.</param>
        /// <param name="serviceRepository">Repository boundary instance for querying physical healthcare service records.</param>
        public GetClinicServicesForBookingService(
            IRepositoryQueryBase<Clinic, Guid, AppDbContext> clinicRepository,
            IRepositoryQueryBase<Service, Guid, AppDbContext> serviceRepository)
        {
            _clinicRepository = clinicRepository;
            _serviceRepository = serviceRepository;
        }

        /// <summary>
        /// Processes the internal data pipeline workflow to validate clinic status, parse service filters, and return available selections.
        /// </summary>
        /// <param name="clinicId">The unique identifier mapping the targeted underlying clinic entity.</param>
        /// <returns>An <see cref="ApiResponse{List{BookingServiceOption}}"/> enclosing descriptive state payloads alongside configuration boundaries.</returns>
        public async Task<ApiResponse<List<BookingServiceOption>>> Process(Guid clinicId)
        {
            // Step 1: Search operational relational databases to verify target clinic profile existence and status
            bool isClinicValid = await ValidateClinicProfile(clinicId);

            // Step 2: Synthesize a flexible criteria lambda matrix expression using contextual constraints
            var filterExpression = BuildFilterExpression(clinicId, isClinicValid);

            // Step 3: Query underlying target physical data storage blocks executing evaluation and ordering algorithms
            var services = await ExecuteServicesQuery(filterExpression, isClinicValid);

            // Step 4: Map internal domain state model attributes onto decoupled serialized response data schemas
            var result = MapToResponseDto(services);

            // Step 5: Evaluate processing parameters and package state structures dynamically to handle execution outcomes
            return CreateResponse(result, isClinicValid);
        }

        /// <summary>
        /// Verifies whether the specified clinic identifier maps onto an existing, active core business entity.
        /// </summary>
        /// <param name="clinicId">The unique enterprise primary reference coordinates identifier token.</param>
        /// <returns>A structure evaluating state indicator returning true if verification matches baseline benchmarks.</returns>
        private async Task<bool> ValidateClinicProfile(Guid clinicId)
        {
            return await _clinicRepository
                .FindByCondition(c => c.Id == clinicId && c.IsActive)
                .AnyAsync();
        }

        /// <summary>
        /// Assembles dynamic lambda filter structures mapping target constraints against persistent relational entity data rows.
        /// </summary>
        /// <param name="clinicId">The context parameter tracking structural identifier key vector maps.</param>
        /// <param name="isClinicValid">Guard execution flag assessing baseline tracking criteria states.</param>
        /// <returns>A composite queryable logic filter expression block.</returns>
        private Expression<Func<Service, bool>> BuildFilterExpression(Guid clinicId, bool isClinicValid)
        {
            if (!isClinicValid)
            {
                return x => false;
            }

            return s => s.ClinicId == clinicId && s.IsActive;
        }

        /// <summary>
        /// Executes physical data store evaluations using structural sorting configurations.
        /// </summary>
        /// <param name="filterExpression">The structured operational logic filter condition matrix maps.</param>
        /// <param name="isClinicValid">Guard condition preventing database load pipelines on validation failure states.</param>
        /// <returns>A structured list of physical domain service entity models matching core expressions.</returns>
        private async Task<List<Service>> ExecuteServicesQuery(
            Expression<Func<Service, bool>> filterExpression,
            bool isClinicValid)
        {
            if (!isClinicValid)
            {
                return new List<Service>();
            }

            return await _serviceRepository
                .FindByCondition(filterExpression, trackChanges: false)
                .OrderBy(s => s.ServiceName)
                .ToListAsync();
        }

        /// <summary>
        /// Transforms internal persistent context database model list directly onto business serialization object schemas.
        /// </summary>
        /// <param name="services">The physical domain core model list array returned directly out of backend operational layers.</param>
        /// <returns>A structured target presentation data payload collection instance context.</returns>
        private List<BookingServiceOption> MapToResponseDto(List<Service> services)
        {
            return services.Select(s => new BookingServiceOption
            {
                Id_service = s.Id,
                ServiceName = s.ServiceName,
                Price = s.Price,
                DurationMinutes = s.DurationMinutes
            })
            .ToList();
        }

        /// <summary>
        /// Analyzes state logic monitoring variables to determine outcome layout packaging choices.
        /// </summary>
        /// <param name="result">The structured data response payload list projected from database layers.</param>
        /// <param name="isClinicValid">Indicates whether clinic token extraction verification matched expectations successfully.</param>
        /// <returns>A standardized application payload container detailed for transport serialization layers.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if verification checkpoints uncover illegal data requests.</exception>
        private ApiResponse<List<BookingServiceOption>> CreateResponse(
            List<BookingServiceOption> result,
            bool isClinicValid)
        {
            if (!isClinicValid)
            {
                throw new KeyNotFoundException(GeneralCode.APP_MESSAGE_4045.ToString()); // Clinic Not Found or Inactive
            }

            return ApiResponse<List<BookingServiceOption>>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                result);
        }
    }
}