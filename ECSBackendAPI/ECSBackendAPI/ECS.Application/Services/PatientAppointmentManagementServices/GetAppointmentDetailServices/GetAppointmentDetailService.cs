using System.Security.Claims;
using System.Text.Json;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Persistence.MongoDb;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ECS.Application.Services.PatientAppointmentManagementServices.GetAppointmentDetailServices
{
    /// <summary>
    /// Handles the business logic for retrieving comprehensive appointment details for the authorized patient.
    /// </summary>
    public class GetAppointmentDetailService : IGetAppointmentDetailService
    {
        private readonly IRepositoryQueryBase<Appointment, Guid, AppDbContext> _appointmentRepository;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMongoDbContext? _mongo;

        public GetAppointmentDetailService(
            IRepositoryQueryBase<Appointment, Guid, AppDbContext> appointmentRepository,
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor,
            IMongoDbContext? mongo = null)
        {
            _appointmentRepository = appointmentRepository;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _mongo = mongo;
        }

        public async Task<ApiResponse<GetAppointmentDetailResponse>> Process(GetAppointmentDetailRequest request)
        {
            // Initialize status tracking flags
            bool isUserValid = true;
            bool isAppointmentExist = true;
            bool isPermissionValid = true;

            // Step 1: Extract identity information metrics from active token pipelines
            var userId = RetrieveUserId(ref isUserValid);

            // Step 2: Retrieve accessible profile IDs for the user
            var accessibleProfileIds = await RetrieveLinkedProfileIds(userId, isUserValid);

            // Step 3: Retrieve the appointment with all necessary navigation properties
            var (appointment, appointmentExists) = await RetrieveAppointment(request.AppointmentId);
            isAppointmentExist = appointmentExists;

            // Step 4: Validate user permission to view this appointment
            (isPermissionValid, isAppointmentExist) = ValidatePermission(
                userId,
                accessibleProfileIds,
                appointment,
                isUserValid,
                isAppointmentExist);

            // Step 5: Map internal domain state segments to serialized outcome presentation representations
            var result = MapToResponseDto(appointment!);

            // Step 5.1: Attach prescription if medical record exists for this appointment
            if (isPermissionValid && appointment != null)
            {
                await AttachPrescriptionAsync(appointment.Id, result);
            }

            // Step 6: Package contextual payloads dynamically to manage outcome states
            return CreateResponse(result, isUserValid, isAppointmentExist, isPermissionValid);
        }

        private async Task AttachPrescriptionAsync(Guid appointmentId, GetAppointmentDetailResponse result)
        {
            try
            {
                var medicalRecord = await _context.Set<ECS.Domain.Entities.MedicalRecords.MedicalRecord>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.AppointmentId == appointmentId);

                if (medicalRecord == null) return;

                var prescriptionDto = new PatientPrescriptionDto
                {
                    DiagnosisMain = medicalRecord.ChiefComplaint ?? medicalRecord.Summary ?? "Khám mắt chuyên khoa",
                    DoctorNotes = medicalRecord.Notes
                };

                if (_mongo != null && !string.IsNullOrEmpty(medicalRecord.MongoDocumentId))
                {
                    var mongoDoc = await _mongo.MedicalRecords
                        .Find(Builders<MedicalRecordDocument>.Filter.Eq(x => x.Id, medicalRecord.MongoDocumentId))
                        .FirstOrDefaultAsync();

                    if (mongoDoc != null && mongoDoc.FormData != null)
                    {
                        var jsonStr = mongoDoc.FormData.ToJson(new MongoDB.Bson.IO.JsonWriterSettings { OutputMode = MongoDB.Bson.IO.JsonOutputMode.RelaxedExtendedJson });
                        using var jsonDoc = JsonDocument.Parse(jsonStr);
                        var root = jsonDoc.RootElement;

                        if (root.TryGetProperty("benhAn", out var benhAnObj))
                        {
                            if (benhAnObj.TryGetProperty("chanDoanChinh", out var cdc)) prescriptionDto.DiagnosisMain = cdc.GetString() ?? prescriptionDto.DiagnosisMain;
                            else if (benhAnObj.TryGetProperty("diagnosisMain", out var dm)) prescriptionDto.DiagnosisMain = dm.GetString() ?? prescriptionDto.DiagnosisMain;

                            if (benhAnObj.TryGetProperty("chanDoanKiem", out var cdk)) prescriptionDto.DiagnosisComorbid = cdk.GetString();
                            else if (benhAnObj.TryGetProperty("diagnosisComorbid", out var dcm)) prescriptionDto.DiagnosisComorbid = dcm.GetString();
                        }

                        JsonElement prescriptionEl = default;
                        if (root.TryGetProperty("khamBenh", out var khamBenh))
                        {
                            if (khamBenh.TryGetProperty("donThuoc", out var dt)) prescriptionEl = dt;
                            else if (khamBenh.TryGetProperty("prescription", out var rx)) prescriptionEl = rx;
                        }
                        if (prescriptionEl.ValueKind == JsonValueKind.Undefined && root.TryGetProperty("benhAn", out var benhAn))
                        {
                            if (benhAn.TryGetProperty("donThuoc", out var dt)) prescriptionEl = dt;
                            else if (benhAn.TryGetProperty("prescription", out var rx)) prescriptionEl = rx;
                        }

                        if (prescriptionEl.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in prescriptionEl.EnumerateArray())
                            {
                                prescriptionDto.Items.Add(ExtractPrescriptionItem(item));
                            }
                        }
                        else if (prescriptionEl.ValueKind == JsonValueKind.Object && prescriptionEl.TryGetProperty("items", out var itemsArray) && itemsArray.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in itemsArray.EnumerateArray())
                            {
                                prescriptionDto.Items.Add(ExtractPrescriptionItem(item));
                            }
                        }
                    }
                }

                result.Prescription = prescriptionDto;
            }
            catch
            {
                // Silently ignore if prescription extraction encounters format errors
            }
        }

        private static PatientPrescriptionItemDto ExtractPrescriptionItem(JsonElement item)
        {
            return new PatientPrescriptionItemDto
            {
                MedicineName = item.TryGetProperty("tenThuoc", out var tn) ? tn.GetString() ?? "" : (item.TryGetProperty("medicineName", out var mn) ? mn.GetString() ?? "" : ""),
                Dosage = item.TryGetProperty("lieuDung", out var ld) ? ld.GetString() ?? "" : (item.TryGetProperty("dosage", out var d) ? d.GetString() ?? "" : ""),
                Frequency = item.TryGetProperty("tanSuat", out var ts) ? ts.GetString() ?? "" : (item.TryGetProperty("frequency", out var f) ? f.GetString() ?? "" : ""),
                DurationDays = item.TryGetProperty("soNgay", out var sn) ? sn.ToString() : (item.TryGetProperty("durationDays", out var dd) ? dd.ToString() : ""),
                Quantity = item.TryGetProperty("soLuong", out var sl) ? sl.ToString() : (item.TryGetProperty("quantity", out var q) ? q.ToString() : ""),
                Instruction = item.TryGetProperty("huongDan", out var hd) ? hd.GetString() ?? "" : (item.TryGetProperty("instruction", out var inst) ? inst.GetString() ?? "" : "")
            };
        }

        /// <summary>
        /// Resolves the logged-in user credentials via claims identity mapping streams.
        /// </summary>
        /// <param name="isUserValid">Guard state flag modified by reference to track authentication integrity.</param>
        /// <returns>The extracted structural global unique identifiers token block mapping the active user account context.</returns>
        private Guid RetrieveUserId(ref bool isUserValid)
        {
            var userIdClaim = _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                isUserValid = false;
                return Guid.Empty;
            }

            return userId;
        }

        /// <summary>
        /// Probes many-to-many link tables to extract authorized patient metrics identifiers.
        /// </summary>
        /// <param name="userId">The system reference credentials tracking structural context nodes.</param>
        /// <param name="isUserValid">Guard validation state assessing token evaluation integrity parameters.</param>
        /// <returns>A list tracking unique entity vector addresses mapping authorized relational profiles data blocks.</returns>
        private async Task<List<Guid>> RetrieveLinkedProfileIds(Guid userId, bool isUserValid)
        {
            if (!isUserValid) return new List<Guid>();

            var directProfileIds = await _context.Set<PatientProfile>()
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.Id)
                .ToListAsync();

            var linkedProfileIds = await _context.Set<UserPatient>()
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.PatientId)
                .ToListAsync();

            return directProfileIds.Union(linkedProfileIds).Distinct().ToList();
        }

        /// <summary>
        /// Retrieves the appointment by ID with all necessary navigation properties.
        /// </summary>
        /// <param name="appointmentId">The appointment ID to retrieve.</param>
        /// <returns>A tuple containing the appointment entity and a flag indicating existence.</returns>
        private async Task<(Appointment? Appointment, bool Exists)> RetrieveAppointment(Guid appointmentId)
        {
            var appointment = await _appointmentRepository
                .FindByCondition(a => a.Id == appointmentId, trackChanges: false)
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.Clinic)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.User)
                .Include(a => a.Service)
                .Include(a => a.Slot)
                .Include(a => a.Feedback)
                .FirstOrDefaultAsync();

            if (appointment == null)
            {
                return (null, false);
            }

            return (appointment, true);
        }

        /// <summary>
        /// Validates whether the current user has permission to view the appointment.
        /// </summary>
        /// <param name="userId">The authenticated user ID.</param>
        /// <param name="accessibleProfileIds">List of profile IDs the user can access.</param>
        /// <param name="appointment">The appointment entity.</param>
        /// <param name="isUserValid">Guard state monitoring authentication pipeline checkpoints.</param>
        /// <param name="isAppointmentExist">Guard state monitoring appointment existence.</param>
        /// <returns>A tuple containing permission validation flag and appointment existence flag.</returns>
        private (bool IsPermissionValid, bool IsAppointmentExist) ValidatePermission(
            Guid userId,
            List<Guid> accessibleProfileIds,
            Appointment? appointment,
            bool isUserValid,
            bool isAppointmentExist)
        {
            if (!isUserValid || !isAppointmentExist || appointment == null)
            {
                return (false, false);
            }

            // Check if the user has access to this patient profile
            var hasAccess = accessibleProfileIds.Contains(appointment.PatientId)
                            || appointment.CreatedById == userId
                            || appointment.PatientId == userId;

            return (hasAccess, isAppointmentExist);
        }

        /// <summary>
        /// Transforms persistent domain models properties directly into target presentation DTO schemas.
        /// </summary>
        /// <param name="appointment">The physical contextual database model structure.</param>
        /// <returns>A presentation data representation optimized for target serializations layers.</returns>
        private GetAppointmentDetailResponse MapToResponseDto(Appointment appointment)
        {
            var patient = appointment.Patient;
            var doctor = appointment.Doctor;
            var clinic = doctor?.Clinic;
            var doctorUser = doctor?.User;
            var patientUser = patient?.User;

            string timeSlotFormatted = appointment.Slot != null
                ? $"{appointment.Slot.StartTime:HH:mm} - {appointment.Slot.EndTime:HH:mm}"
                : "N/A";

            string doctorName = doctor != null
                ? $"{doctor.Title} {doctorUser?.FullName}".Trim()
                : "N/A";

            var response = new GetAppointmentDetailResponse
            {
                // Basic Info
                Id_appointment = appointment.Id.ToString(),
                Status = appointment.Status.ToString(),
                CreatedAt = appointment.CreatedAt.ToString("dd/MM/yyyy HH:mm"),

                // Patient Info
                PatientName = patient?.FullName ?? "N/A",
                PatientPhone = patient?.PhoneNumber ?? "N/A",
                PatientEmail = patientUser?.Email ?? "N/A",
                PatientDob = patient?.Dob.ToString("dd/MM/yyyy") ?? "N/A",
                PatientGender = patient?.Gender.ToString() ?? "N/A",

                // Appointment Info
                ClinicName = clinic?.Name ?? "N/A",
                ClinicAddress = clinic?.Address ?? "N/A",
                ClinicPhone = clinic?.Phone ?? "N/A",
                DoctorName = doctorName,
                DoctorTitle = doctor?.Title ?? "N/A",
                ServiceName = appointment.Service?.ServiceName ?? "Khám mắt tổng quát",
                AppointmentDate = appointment.AppointmentDate.ToString("dd/MM/yyyy"),
                TimeSlot = timeSlotFormatted,
                Symptoms = appointment.Symptoms,
                NoteReason = appointment.NoteReason
            };

            // Map Feedback if exists
            if (appointment.Feedback != null)
            {
                response.Feedback = new FeedbackDetail
                {
                    RatingDoctor = appointment.Feedback.RatingDoctor,
                    RatingClinic = appointment.Feedback.RatingClinic,
                    Comment = appointment.Feedback.Comment,
                    IsPublic = appointment.Feedback.IsPublic,
                    CreatedAt = appointment.Feedback.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                };
            }

            return response;
        }

        /// <summary>
        /// Resolves transaction outcome wrappers packing serialization nodes safely.
        /// </summary>
        /// <param name="result">The internal serializable structure returned out of core projection chains.</param>
        /// <param name="isUserValid">Guard context parameter evaluating token claim validity bounds.</param>
        /// <param name="isAppointmentExist">Guard monitoring parameter checking appointment existence.</param>
        /// <param name="isPermissionValid">Guard monitoring parameter checking permission boundaries.</param>
        /// <returns>A structured envelope holding operational response outcomes ready for presentation nodes.</returns>
        private ApiResponse<GetAppointmentDetailResponse> CreateResponse(
            GetAppointmentDetailResponse result,
            bool isUserValid,
            bool isAppointmentExist,
            bool isPermissionValid)
        {
            var errorResponse = CreateErrorResponse(isUserValid, isAppointmentExist, isPermissionValid);
            if (errorResponse != null)
            {
                return errorResponse;
            }

            return ApiResponse<GetAppointmentDetailResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                result);
        }

        /// <summary>
        /// Evaluates functional exceptions sequences to render failure metadata nodes.
        /// </summary>
        /// <param name="isUserValid">Guard indicating whether authorization checkpoints cleared successfully.</param>
        /// <param name="isAppointmentExist">Guard indicating whether appointment exists.</param>
        /// <param name="isPermissionValid">Guard indicating whether permission validation cleared successfully.</param>
        /// <returns>A failure configuration block, or null if execution tracks meet standard benchmarks.</returns>
        private ApiResponse<GetAppointmentDetailResponse>? CreateErrorResponse(
            bool isUserValid,
            bool isAppointmentExist,
            bool isPermissionValid)
        {
            if (!isUserValid)
            {
                return ApiResponse<GetAppointmentDetailResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4001.ToString());
            }

            if (!isAppointmentExist)
            {
                return ApiResponse<GetAppointmentDetailResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4046.ToString());
            }

            if (!isPermissionValid)
            {
                return ApiResponse<GetAppointmentDetailResponse>.Fail(
                    GeneralCode.APP_MESSAGE_4053.ToString());
            }

            return null;
        }
    }
}