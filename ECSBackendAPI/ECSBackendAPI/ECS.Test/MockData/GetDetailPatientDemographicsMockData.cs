using ECS.Application.Services.DoctorAppointmentPatientManagementServices.GetDetailPatientDemographicsServices;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    public static class GetDetailPatientDemographicsMockData
    {
        public static readonly Guid DefaultPatientId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        public static GetDetailPatientDemographicsRequest GetValidRequest() => new()
        {
            PatientId = DefaultPatientId
        };

        public static GetDetailPatientDemographicsRequest GetInvalidRequest() => new()
        {
            PatientId = Guid.Empty
        };

        public static PatientProfile GetPatientProfile(Guid? id = null) => new()
        {
            Id = id ?? DefaultPatientId,
            FullName = "Nguyen Van B",
            Dob = new DateTime(1990, 5, 20),
            Gender = Gender.MALE,
            PhoneNumber = "0987654321",
            IdentityNumber = "012345678901",
            BhytNumber = "DN4010123456789",
            Address = "123 Le Loi, Quan 1, TP.HCM",
            BloodType = "O+",
            Allergies = "Peanuts, Penicillin",
            MedicalHistory = "Hypertension diagnosed 2021",
            HasMedicalDemographics = true,
            CreatedAt = new DateTime(2025, 1, 1),
            UpdatedAt = new DateTime(2025, 1, 10)
        };
    }
}
