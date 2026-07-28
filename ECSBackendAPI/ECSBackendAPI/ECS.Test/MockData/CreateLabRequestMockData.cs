using System.Text.Json;
using ECS.Application.Services.ParaclinicalServices.CreateLabRequestServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;

namespace ECS.Test.MockData
{
    /// <summary>
    /// Test data factory for <see cref="CreateLabRequestService"/> tests.
    /// </summary>
    public static class CreateLabRequestMockData
    {
        public static readonly Guid ValidUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid ValidRecordId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid ValidDoctorId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid ValidAppointmentId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static readonly Guid ValidPatientId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        public static CreateLabRequestRequest GetValidRequest(
            string? recordId = null,
            string labType = "OCT",
            string? side = "OD",
            string? status = "REQUESTED",
            JsonElement? measurements = null)
        {
            var defaultJson = JsonDocument.Parse("{\"rnflAverageOd\": 95.5}").RootElement;

            return new CreateLabRequestRequest
            {
                RecordId = recordId ?? ValidRecordId.ToString(),
                LabType = labType,
                Side = side,
                Indication = "Glaucoma screening",
                TechnicianId = "tech-01",
                TechnicianName = "John Tech",
                MachineName = "Cirrus HD-OCT",
                ScanPattern = "Macula Cube 512x128",
                ImageUrl = "https://storage.example.com/oct/1.png",
                ClinicalConclusion = "Normal thickness",
                Measurements = measurements ?? defaultJson,
                Status = status
            };
        }

        public static MedicalRecord GetMedicalRecord(Guid? id = null)
        {
            return new MedicalRecord
            {
                Id = id ?? ValidRecordId,
                AppointmentId = ValidAppointmentId,
                PatientId = ValidPatientId
            };
        }

        public static DoctorProfile GetDoctorProfile(Guid? userId = null, bool isActive = true)
        {
            return new DoctorProfile
            {
                Id = ValidDoctorId,
                UserId = userId ?? ValidUserId,
                IsActive = isActive
            };
        }
    }
}