using System.Linq.Expressions;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.PatientAppointmentManagementServices.GetClinicBookingOptionsServices
{
    /// <summary>
    /// Handles the business logic for retrieving active doctors within a specific clinic, formatted for booking selections.
    /// </summary>
    public class GetClinicDoctorsForBookingService : IGetClinicDoctorsForBookingService
    {
        private readonly IRepositoryQueryBase<Clinic, Guid, AppDbContext> _clinicRepository;
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> _doctorRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetClinicDoctorsForBookingService"/> class with required infrastructure boundaries.
        /// </summary>
        /// <param name="clinicRepository">Repository boundary instance for querying physical clinic records.</param>
        /// <param name="doctorRepository">Repository boundary instance for querying physical doctor profile records.</param>
        public GetClinicDoctorsForBookingService(
            IRepositoryQueryBase<Clinic, Guid, AppDbContext> clinicRepository,
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepository)
        {
            _clinicRepository = clinicRepository;
            _doctorRepository = doctorRepository;
        }

        /// <summary>
        /// Processes the internal data pipeline workflow to validate clinic status, parse doctor filters, and return available selections.
        /// </summary>
        /// <param name="clinicId">The unique identifier mapping the targeted underlying clinic entity.</param>
        /// <returns>An <see cref="ApiResponse{List{BookingDoctorOption}}"/> enclosing descriptive state payloads alongside configuration boundaries.</returns>
        public async Task<ApiResponse<List<BookingDoctorOption>>> Process(Guid clinicId)
        {
            // Step 1: Search operational relational databases to verify target clinic profile existence and status
            bool isClinicValid = await ValidateClinicProfile(clinicId);

            // Step 2: Synthesize a flexible criteria lambda matrix expression using contextual constraints
            var filterExpression = BuildFilterExpression(clinicId, isClinicValid);

            // Step 3: Query underlying target physical data storage blocks executing evaluation and ordering algorithms
            var doctors = await ExecuteDoctorsQuery(filterExpression, isClinicValid);

            // Step 4: Map internal domain state model attributes onto decoupled serialized response data schemas
            var result = MapToResponseDto(doctors);

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
        private Expression<Func<DoctorProfile, bool>> BuildFilterExpression(Guid clinicId, bool isClinicValid)
        {
            if (!isClinicValid)
            {
                return x => false;
            }

            return d => d.ClinicId == clinicId && d.IsActive;
        }

        /// <summary>
        /// Executes physical data store evaluations using structural sorting and relational eager loading configurations.
        /// </summary>
        /// <param name="filterExpression">The structured operational logic filter condition matrix maps.</param>
        /// <param name="isClinicValid">Guard condition preventing database load pipelines on validation failure states.</param>
        /// <returns>A structured list of physical domain doctor profile entity models matching core expressions.</returns>
        private async Task<List<DoctorProfile>> ExecuteDoctorsQuery(
            Expression<Func<DoctorProfile, bool>> filterExpression,
            bool isClinicValid)
        {
            if (!isClinicValid)
            {
                return new List<DoctorProfile>();
            }

            return await _doctorRepository
                .FindByCondition(filterExpression, trackChanges: false)
                .Include(d => d.User)
                .Include(d => d.Specialty)
                .OrderBy(d => d.User.FullName)
                .ToListAsync();
        }

        /// <summary>
        /// Transforms internal persistent context database model list directly onto business serialization object schemas.
        /// </summary>
        /// <param name="doctors">The physical domain core model list array returned directly out of backend operational layers.</param>
        /// <returns>A structured target presentation data payload collection instance context.</returns>
        private List<BookingDoctorOption> MapToResponseDto(List<DoctorProfile> doctors)
        {
            return doctors.Select(d => new BookingDoctorOption
            {
                Id_doctor = d.Id,
                FullName = d.User.FullName,
                Title = d.Title,
                SpecialtyName = d.Specialty != null ? d.Specialty.Name : null,
                ExperienceYears = d.ExperienceYears
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
        private ApiResponse<List<BookingDoctorOption>> CreateResponse(
            List<BookingDoctorOption> result,
            bool isClinicValid)
        {
            if (!isClinicValid)
            {
                throw new KeyNotFoundException(GeneralCode.APP_MESSAGE_4045.ToString()); // Clinic Not Found or Inactive
            }

            return ApiResponse<List<BookingDoctorOption>>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                result);
        }
    }
}