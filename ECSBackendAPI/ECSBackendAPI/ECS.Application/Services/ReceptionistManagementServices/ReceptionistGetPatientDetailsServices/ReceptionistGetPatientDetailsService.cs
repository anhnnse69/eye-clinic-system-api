using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetPatientDetailsServices
{
    /// <summary>
    /// Handles the business logic for verifying data permissions, loading profiles, and filtering contextual appointment logs.
    /// </summary>
    public class ReceptionistGetPatientDetailsService : IReceptionistGetPatientDetailsService
    {
        private readonly IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> _patientQueryRepo;
        private readonly IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> _staffQueryRepo;
        private readonly IRepositoryQueryBase<Appointment, Guid, AppDbContext> _appointmentQueryRepo;

        /// <summary>
        /// Initializes a new instance of <see cref="ReceptionistGetPatientDetailsService"/> with structural query repositories.
        /// </summary>
        /// <param name="patientQueryRepo">The query repository for tracking patient records.</param>
        /// <param name="staffQueryRepo">The query repository for resolving receptionist assignments.</param>
        /// <param name="appointmentQueryRepo">The data repository tracking physical clinic schedules and slots allocations.</param>
        public ReceptionistGetPatientDetailsService(
            IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> patientQueryRepo,
            IRepositoryQueryBase<StaffClinic, Guid, AppDbContext> staffQueryRepo,
            IRepositoryQueryBase<Appointment, Guid, AppDbContext> appointmentQueryRepo)
        {
            _patientQueryRepo = patientQueryRepo;
            _staffQueryRepo = staffQueryRepo;
            _appointmentQueryRepo = appointmentQueryRepo;
        }

        /// <summary>
        /// Orchestrates secure data resolution chains targeting core profiles and isolated appointment timelines.
        /// </summary>
        /// <param name="request">The filtration and context parameters bundle for the processing query.</param>
        /// <returns>A structured <see cref="ApiResponse{ReceptionistGetPatientDetailsResponse}"/> packing target telemetry data blocks.</returns>
        public async Task<ApiResponse<ReceptionistGetPatientDetailsResponse>> Process(ReceptionistGetPatientDetailsRequest request)
        {
            // Step 1: Resolve the collection of clinic operational scopes assigned to the active receptionist
            var operatingClinicIds = ResolveReceptionistClinics(request.CurrentUserId);
            // Step 2: Fetch the fundamental patient aggregate root profile data from data layers
            var patientEntity = await FetchCorePatientProfile(request.PatientId);
            // Step 3: Query historical logs strictly bound to the active partitions assigned to the receptionist context
            var localizedAppointments = FilterSecureAppointments(request.PatientId, operatingClinicIds);
            // Step 4: Convert and map entity graphs into presentation data transfer objects matching interface schemas
            var finalizedDto = AssembleSecureResponseDto(patientEntity, localizedAppointments);
            // Step 5: Seal payloads inside standard message tracking architectures and return outcome data points
            return EvaluateResponseResult(finalizedDto);
        }

        /// <summary>
        /// Extracts all active clinic boundary registrations associated with the requesting context receptionist.
        /// </summary>
        /// <param name="staffUserId">The unique identifier of the target receptionist staff user context.</param>
        /// <returns>A list containing unique clinic identifiers assigned to the staff target.</returns>
        private List<Guid> ResolveReceptionistClinics(Guid staffUserId)
        {
            // NOTE: Cast underlying enum to raw integer to match exact index rules (3 = RECEPTIONIST) across database engine layouts
            return _staffQueryRepo.FindByCondition(
                sc => sc.UserId == staffUserId && sc.IsActive && (int)sc.Role == 3,
                trackChanges: false
            ).Select(sc => sc.ClinicId).ToList();
        }

        /// <summary>
        /// Retrieves the foundational patient profile tracking database record.
        /// </summary>
        /// <param name="patientId">The unique identifier of the requested target patient profile record.</param>
        /// <returns>The matched <see cref="PatientProfile"/> graph if found; otherwise, null.</returns>
        private async Task<PatientProfile?> FetchCorePatientProfile(Guid patientId)
        {
            return await _patientQueryRepo.GetByIdAsync(patientId, p => p.User!);
        }

        /// <summary>
        /// Restricts appointment historical lists to matches strictly located within the receptionist's assigned clinic boundary.
        /// </summary>
        /// <param name="patientId">The unique identifier of the requested patient profile target context.</param>
        /// <param name="clinicIds">The structural containment boundary filter scopes mapped to the operator user token context.</param>
        /// <returns>An ordered collection containing matching structured localized <see cref="Appointment"/> historical segments.</returns>
        private List<Appointment> FilterSecureAppointments(Guid patientId, List<Guid> clinicIds)
        {
            if (clinicIds == null || !clinicIds.Any())
            {
                return new List<Appointment>();
            }
            return _appointmentQueryRepo.FindByCondition(
                ap => ap.PatientId == patientId && clinicIds.Contains(ap.Doctor.ClinicId),
                trackChanges: false,
                ap => ap.Doctor,
                ap => ap.Doctor.User,
                ap => ap.Doctor.Specialty!
            ).OrderByDescending(ap => ap.AppointmentDate).ToList();
        }

        /// <summary>
        /// Validates objects and transforms safe raw tracking parameters down into flattened presentation arrays.
        /// </summary>
        /// <param name="patient">The aggregate database reference targeting patient primary profiles.</param>
        /// <param name="securedAppointments">The list of context-bound transactional medical records logs.</param>
        /// <returns>A mapped flat data structure detailing profile items if inputs exist; otherwise, null.</returns>
        private ReceptionistGetPatientDetailsResponse? AssembleSecureResponseDto(PatientProfile? patient, List<Appointment> securedAppointments)
        {
            if (patient == null)
            {
                return null;
            }
            return new ReceptionistGetPatientDetailsResponse
            {
                Id = patient.Id.ToString(),
                FullName = patient.FullName,
                Gender = patient.Gender.ToString().ToUpper(),
                Dob = patient.Dob.ToString("yyyy-MM-dd"),
                IdentityNumber = patient.IdentityNumber,
                Address = patient.Address,
                PhoneNumber = patient.PhoneNumber,
                BhytNumber = patient.BhytNumber,
                BloodType = patient.BloodType,
                Allergies = patient.Allergies,
                MedicalHistory = patient.MedicalHistory,
                CreatedAt = patient.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Email = patient.User?.Email,
                AvatarUrl = patient.User?.AvatarUrl,
                Appointments = securedAppointments.Select(ap => new ReceptionistAppointmentLogDto
                {
                    Id = ap.Id.ToString(),
                    ClinicId = ap.Doctor.ClinicId.ToString(),
                    AppointmentDate = ap.AppointmentDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    DoctorName = ap.Doctor.User.FullName,
                    SpecialtyName = ap.Doctor.Specialty?.Name ?? "Phòng khám chung",
                    Symptoms = ap.Symptoms,
                    Status = ap.Status.ToString().ToUpper(),
                    BookingSource = ap.BookingSource.ToUpper()
                }).ToList()
            };
        }

        /// <summary>
        /// Determines standard structure API return outcomes based on the internal object evaluation.
        /// </summary>
        /// <param name="outputDto">The compiled response model holding output metrics data items.</param>
        /// <returns>A normalized outcome structure wrapping corresponding tracking markers.</returns>
        private ApiResponse<ReceptionistGetPatientDetailsResponse> EvaluateResponseResult(ReceptionistGetPatientDetailsResponse? outputDto)
        {
            if (outputDto == null)
            {
                return ApiResponse<ReceptionistGetPatientDetailsResponse>.Fail("APP_MESSAGE_4004");
            }
            return ApiResponse<ReceptionistGetPatientDetailsResponse>.Success("APP_MESSAGE_2000", outputDto);
        }
    }
}