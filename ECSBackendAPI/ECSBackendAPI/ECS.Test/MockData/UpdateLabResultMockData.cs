using ECS.Application.Services.ParaclinicalServices.UpdateLabResultServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence.MongoDb;
using MongoDB.Bson;
using System.Text.Json;

namespace ECS.Test.MockData
{
    public static class UpdateLabResultMockData
    {
        public static readonly Guid ValidDoctorUserId = Guid.NewGuid();
        public static readonly Guid ValidDoctorId = Guid.NewGuid();
        public static readonly Guid ValidRecordId = Guid.NewGuid();
        public static readonly string ValidLabResultId = "507f1f77bcf86cd799439011";

        public static DoctorProfile GetDoctorProfile(Guid? doctorId = null, Guid? userId = null)
        {
            return new DoctorProfile
            {
                Id = doctorId ?? ValidDoctorId,
                UserId = userId ?? ValidDoctorUserId,
                IsActive = true
            };
        }

        public static MedicalRecord GetMedicalRecord(Guid? recordId = null, Guid? doctorId = null)
        {
            return new MedicalRecord
            {
                Id = recordId ?? ValidRecordId,
                DoctorId = doctorId ?? ValidDoctorId
            };
        }

        public static LabResultDocument GetLabResultDocument(string? id = null, string? recordId = null)
        {
            return new LabResultDocument
            {
                Id = id ?? ValidLabResultId,
                RecordId = (recordId ?? ValidRecordId.ToString()),
                Status = "PENDING",
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static UpdateLabResultRequest GetValidRequest()
        {
            using var doc = JsonDocument.Parse("{\"thickness\": 250}");
            return new UpdateLabResultRequest
            {
                LabResultId = ValidLabResultId,
                Status = "completed",
                ClinicalConclusion = "Normal",
                ImageUrl = "http://example.com/img.jpg",
                TechnicianName = "John Doe",
                MachineName = "OCT-3000",
                ScanPattern = "Macula",
                PerformedAt = DateTime.UtcNow,
                Measurements = doc.RootElement.Clone()
            };
        }
    }
}