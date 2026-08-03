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

namespace ECS.Application.Services.PatientAppointmentManagementServices.GetPrescriptionDetailServices
{
    /// <summary>
    /// Service implementation for UC 22: View Prescription.
    /// Handles retrieving prescription records from MongoDB and SQL database.
    /// </summary>
    public class GetPrescriptionDetailService : IGetPrescriptionDetailService
    {
        private readonly IRepositoryQueryBase<Appointment, Guid, AppDbContext> _appointmentRepository;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMongoDbContext? _mongo;

        public GetPrescriptionDetailService(
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

        public async Task<ApiResponse<GetPrescriptionDetailResponse>> Process(GetPrescriptionDetailRequest request)
        {
            bool isUserValid = true;
            bool isAppointmentExist = true;
            bool isPermissionValid = true;

            // Step 1: Retrieve logged in user ID
            var userId = RetrieveUserId(ref isUserValid);

            // Step 2: Retrieve accessible profile IDs for user
            var accessibleProfileIds = await RetrieveLinkedProfileIds(userId, isUserValid);

            // Step 3: Fetch appointment details
            var (appointment, appointmentExists) = await RetrieveAppointment(request.AppointmentId);
            isAppointmentExist = appointmentExists;

            // Step 4: Validate user access permission
            (isPermissionValid, isAppointmentExist) = ValidatePermission(
                userId,
                accessibleProfileIds,
                appointment,
                isUserValid,
                isAppointmentExist);

            if (!isUserValid)
            {
                return ApiResponse<GetPrescriptionDetailResponse>.Fail(GeneralCode.APP_MESSAGE_4001.ToString());
            }

            if (!isAppointmentExist)
            {
                return ApiResponse<GetPrescriptionDetailResponse>.Fail(GeneralCode.APP_MESSAGE_4046.ToString());
            }

            if (!isPermissionValid)
            {
                return ApiResponse<GetPrescriptionDetailResponse>.Fail(GeneralCode.APP_MESSAGE_4053.ToString());
            }

            // Step 5: Map basic appointment, patient, doctor, clinic information to DTO
            var response = MapBaseInfo(appointment!);

            // Step 6: Attach detailed prescription data extracted from MongoDB / SQL Medical Record
            await PopulatePrescriptionDetailsAsync(appointment!.Id, response);

            return ApiResponse<GetPrescriptionDetailResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                response);
        }

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

        private async Task<(Appointment? Appointment, bool Exists)> RetrieveAppointment(Guid appointmentId)
        {
            var appointment = await _appointmentRepository
                .FindByCondition(a => a.Id == appointmentId, trackChanges: false)
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.Clinic)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Service)
                .Include(a => a.Slot)
                .FirstOrDefaultAsync();

            if (appointment == null)
            {
                return (null, false);
            }

            return (appointment, true);
        }

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

            var hasAccess = accessibleProfileIds.Contains(appointment.PatientId)
                            || appointment.CreatedById == userId
                            || appointment.PatientId == userId;

            return (hasAccess, isAppointmentExist);
        }

        private static GetPrescriptionDetailResponse MapBaseInfo(Appointment appointment)
        {
            var patient = appointment.Patient;
            var doctor = appointment.Doctor;
            var clinic = doctor?.Clinic;
            var doctorUser = doctor?.User;
            var patientUser = patient?.User;

            string doctorName = doctor != null
                ? $"{doctor.Title} {doctorUser?.FullName}".Trim()
                : "N/A";

            return new GetPrescriptionDetailResponse
            {
                AppointmentId = appointment.Id.ToString(),
                CreatedAt = appointment.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                ClinicName = clinic?.Name ?? "N/A",
                ClinicAddress = clinic?.Address ?? "N/A",
                ClinicPhone = clinic?.Phone ?? "N/A",
                DoctorName = doctorName,
                DoctorTitle = doctor?.Title ?? "N/A",
                PatientName = patient?.FullName ?? "N/A",
                PatientPhone = patient?.PhoneNumber ?? "N/A",
                PatientEmail = patientUser?.Email ?? "N/A",
                PatientDob = patient?.Dob.ToString("dd/MM/yyyy") ?? "N/A",
                PatientGender = patient?.Gender.ToString() ?? "N/A",
                PatientAddress = patient?.Address ?? "N/A",
                PrescribedDate = appointment.AppointmentDate.ToString("dd/MM/yyyy")
            };
        }

        private async Task PopulatePrescriptionDetailsAsync(Guid appointmentId, GetPrescriptionDetailResponse response)
        {
            try
            {
                var medicalRecord = await _context.Set<ECS.Domain.Entities.MedicalRecords.MedicalRecord>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.AppointmentId == appointmentId);

                if (medicalRecord != null)
                {
                    response.MedicalRecordId = medicalRecord.Id.ToString();
                    response.DiagnosisMain = medicalRecord.ChiefComplaint ?? medicalRecord.Summary ?? "Khám mắt chuyên khoa";
                    response.DoctorNotes = medicalRecord.Notes;
                }

                MedicalRecordDocument? mongoDoc = null;

                if (_mongo != null)
                {
                    // 1. Try finding document by MongoDocumentId pointer if available
                    if (medicalRecord != null && !string.IsNullOrEmpty(medicalRecord.MongoDocumentId))
                    {
                        mongoDoc = await _mongo.MedicalRecords
                            .Find(Builders<MedicalRecordDocument>.Filter.Eq(x => x.Id, medicalRecord.MongoDocumentId))
                            .FirstOrDefaultAsync();
                    }

                    // 2. Fallback: query MongoDB directly by AppointmentId string
                    if (mongoDoc == null)
                    {
                        mongoDoc = await _mongo.MedicalRecords
                            .Find(Builders<MedicalRecordDocument>.Filter.Eq(x => x.AppointmentId, appointmentId.ToString()))
                            .FirstOrDefaultAsync();
                    }
                }

                if (mongoDoc != null && mongoDoc.FormData != null)
                {
                    ParsePrescriptionFromBson(mongoDoc.FormData, response);
                }
            }
            catch
            {
                // Silently return what we have mapped if MongoDB parsing fails
            }
        }

        private static void ParsePrescriptionFromBson(BsonDocument formData, GetPrescriptionDetailResponse response)
        {
            var jsonStr = formData.ToJson(new MongoDB.Bson.IO.JsonWriterSettings { OutputMode = MongoDB.Bson.IO.JsonOutputMode.RelaxedExtendedJson });
            using var jsonDoc = JsonDocument.Parse(jsonStr);
            var root = jsonDoc.RootElement;

            JsonElement benhAnObj = default;
            if (root.TryGetProperty("benhAn", out var ba1)) benhAnObj = ba1;
            else if (root.TryGetProperty("BenhAn", out var ba2)) benhAnObj = ba2;

            if (benhAnObj.ValueKind == JsonValueKind.Object)
            {
                if (benhAnObj.TryGetProperty("chanDoanChinh", out var cdc) && cdc.ValueKind == JsonValueKind.String)
                    response.DiagnosisMain = cdc.GetString()!;
                else if (benhAnObj.TryGetProperty("diagnosisMain", out var dm) && dm.ValueKind == JsonValueKind.String)
                    response.DiagnosisMain = dm.GetString()!;

                if (benhAnObj.TryGetProperty("chanDoanKiem", out var cdk) && cdk.ValueKind == JsonValueKind.String)
                    response.DiagnosisComorbid = cdk.GetString();
                else if (benhAnObj.TryGetProperty("diagnosisComorbid", out var dcm) && dcm.ValueKind == JsonValueKind.String)
                    response.DiagnosisComorbid = dcm.GetString();
            }

            JsonElement prescriptionEl = default;

            // Search in root level
            if (root.TryGetProperty("prescription", out var pRoot)) prescriptionEl = pRoot;
            else if (root.TryGetProperty("donThuoc", out var dRoot)) prescriptionEl = dRoot;

            // Search in khamBenh
            if (prescriptionEl.ValueKind == JsonValueKind.Undefined && root.TryGetProperty("khamBenh", out var khamBenh) && khamBenh.ValueKind == JsonValueKind.Object)
            {
                if (khamBenh.TryGetProperty("donThuoc", out var dt)) prescriptionEl = dt;
                else if (khamBenh.TryGetProperty("prescription", out var rx)) prescriptionEl = rx;
            }

            // Search in benhAn
            if (prescriptionEl.ValueKind == JsonValueKind.Undefined && benhAnObj.ValueKind == JsonValueKind.Object)
            {
                if (benhAnObj.TryGetProperty("prescription", out var rx)) prescriptionEl = rx;
                else if (benhAnObj.TryGetProperty("donThuoc", out var dt)) prescriptionEl = dt;
            }

            if (prescriptionEl.ValueKind == JsonValueKind.Object)
            {
                // Prescription metadata
                if (prescriptionEl.TryGetProperty("ngayKeDon", out var nkd) && nkd.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(nkd.GetString()))
                {
                    response.PrescribedDate = nkd.GetString()!;
                }

                if (prescriptionEl.TryGetProperty("bacSiKeDon", out var bskd) && bskd.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(bskd.GetString()))
                {
                    response.DoctorName = bskd.GetString()!;
                }

                if (prescriptionEl.TryGetProperty("chanDoan", out var cd) && cd.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(cd.GetString()))
                {
                    response.DiagnosisMain = cd.GetString()!;
                }

                if (prescriptionEl.TryGetProperty("loiDan", out var ld) && ld.ValueKind == JsonValueKind.String)
                {
                    response.DoctorNotes = ld.GetString();
                }
                else if (prescriptionEl.TryGetProperty("ghiChuChung", out var gcc) && gcc.ValueKind == JsonValueKind.String && string.IsNullOrEmpty(response.DoctorNotes))
                {
                    response.DoctorNotes = gcc.GetString();
                }

                if (prescriptionEl.TryGetProperty("ngayTaiKham", out var ntk) && ntk.ValueKind == JsonValueKind.String)
                {
                    response.FollowUpDate = ntk.GetString();
                }

                if (prescriptionEl.TryGetProperty("giaTriDonThuoc", out var gtdt))
                {
                    if (gtdt.ValueKind == JsonValueKind.Number && gtdt.TryGetDecimal(out var val))
                    {
                        response.PrescriptionValue = val;
                    }
                    else if (gtdt.ValueKind == JsonValueKind.String && decimal.TryParse(gtdt.GetString(), out var parsedVal))
                    {
                        response.PrescriptionValue = parsedVal;
                    }
                }

                // Look for items array
                if (prescriptionEl.TryGetProperty("items", out var itemsArray) && itemsArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in itemsArray.EnumerateArray())
                    {
                        var parsedItem = ExtractPrescriptionItem(item);
                        if (!string.IsNullOrWhiteSpace(parsedItem.MedicineName))
                        {
                            response.Items.Add(parsedItem);
                        }
                    }
                }
            }
            else if (prescriptionEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in prescriptionEl.EnumerateArray())
                {
                    var parsedItem = ExtractPrescriptionItem(item);
                    if (!string.IsNullOrWhiteSpace(parsedItem.MedicineName))
                    {
                        response.Items.Add(parsedItem);
                    }
                }
            }
        }

        private static PrescriptionItemDto ExtractPrescriptionItem(JsonElement item)
        {
            string medicineName = GetStringProp(item, "tenThuoc", "medicineName", "name", "drugName");
            string dosage = GetStringProp(item, "hamLuong", "lieuDung", "dosage", "strength");
            string frequency = GetStringProp(item, "cachDung", "frequency", "tanSuat", "usage");
            string durationDays = GetStringProp(item, "soNgay", "durationDays", "duration");
            string quantity = GetStringProp(item, "soLuongMua", "soLuong", "quantity", "qty");
            string unit = GetStringProp(item, "donViTinh", "unit");
            string instruction = GetStringProp(item, "ghiChu", "huongDan", "instruction", "notes");

            return new PrescriptionItemDto
            {
                MedicineName = medicineName,
                Dosage = dosage,
                Frequency = frequency,
                DurationDays = durationDays,
                Quantity = quantity,
                Unit = unit,
                Instruction = instruction
            };
        }

        private static string GetStringProp(JsonElement element, params string[] propertyNames)
        {
            if (element.ValueKind != JsonValueKind.Object) return string.Empty;

            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.String) return prop.GetString() ?? string.Empty;
                    if (prop.ValueKind == JsonValueKind.Number) return prop.ToString();
                }
            }
            return string.Empty;
        }
    }
}
