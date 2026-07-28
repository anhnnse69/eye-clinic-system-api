using System.Text.Json;
using ECS.Application.Services.ParaclinicalServices.GetLabResultsServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Infrastructure.Persistence.MongoDb;
using MongoDB.Bson;

namespace ECS.Test.MockData
{
    public static class GetLabResultsMockData
    {
        public static readonly Guid ValidRecordId = Guid.NewGuid();
        public static readonly Guid ValidDoctorUserId = Guid.NewGuid();
        public static readonly Guid ValidDoctorId = Guid.NewGuid();
        public static readonly Guid ValidPatientUserId = Guid.NewGuid();
        public static readonly Guid ValidPatientId = Guid.NewGuid();
        public static readonly Guid ValidStaffUserId = Guid.NewGuid();

        public static GetLabResultsRequest GetValidRequest(
            string? recordId = null,
            string? labType = null,
            string? side = null)
        {
            return new GetLabResultsRequest
            {
                RecordId = recordId ?? ValidRecordId.ToString(),
                LabType = labType,
                Side = side
            };
        }

        public static MedicalRecord GetMedicalRecord(Guid? recordId = null, Guid? doctorId = null, Guid? patientId = null)
        {
            return new MedicalRecord
            {
                Id = recordId ?? ValidRecordId,
                DoctorId = doctorId ?? ValidDoctorId,
                PatientId = patientId ?? ValidPatientId
            };
        }

        public static DoctorProfile GetDoctorProfile(Guid? userId = null, Guid? doctorId = null)
        {
            return new DoctorProfile
            {
                Id = doctorId ?? ValidDoctorId,
                UserId = userId ?? ValidDoctorUserId,
                IsActive = true
            };
        }

        public static PatientProfile GetPatientProfile(Guid? userId = null, Guid? patientId = null)
        {
            return new PatientProfile
            {
                Id = patientId ?? ValidPatientId,
                UserId = userId ?? ValidPatientUserId
            };
        }

        public static List<LabResultDocument> GetSampleMongoDocs()
        {
            return new List<LabResultDocument>
            {
                new LabResultDocument
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    RecordId = ValidRecordId.ToString(),
                    LabType = "OCT",
                    Side = "OD",
                    Status = "COMPLETED",
                    MachineName = "Zeiss Cirrus 5000",
                    ScanPattern = "Macula Cube",
                    ImageUrl = "https://cdn.clinic.com/oct1.jpg",
                    ClinicalConclusion = "Normal macula",
                    RequestedAt = DateTime.UtcNow.AddHours(-2),
                    PerformedAt = DateTime.UtcNow.AddHours(-1),
                    UpdatedAt = DateTime.UtcNow,
                    Measurements = BsonDocument.Parse("{\"rnflAverage\": 95, \"cmt\": 245}"),
                    AiPrediction = BsonDocument.Parse("{\"glaucomaRisk\": \"LOW\"}")
                },
                new LabResultDocument
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    RecordId = ValidRecordId.ToString(),
                    LabType = "VISUAL_FIELD",
                    Side = "OS",
                    Status = "REQUESTED",
                    RequestedAt = DateTime.UtcNow.AddMinutes(-30),
                    UpdatedAt = DateTime.UtcNow,
                    Measurements = new BsonDocument(),
                    AiPrediction = null // Phủ nhánh AiPrediction == null
                }
            };
        }
    }
}