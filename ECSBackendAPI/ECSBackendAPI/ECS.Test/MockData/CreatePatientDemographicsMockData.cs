using ECS.Application.Services.DoctorAppointmentPatientManagementServices.CreatePatientDemographicsServices;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    public static class CreatePatientDemographicsMockData
    {
        public static readonly Guid DefaultPatientProfileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid DefaultDoctorUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public static CreatePatientDemographicsRequest GetValidRequest(Guid? patientProfileId = null)
        {
            return new CreatePatientDemographicsRequest
            {
                PatientProfileId = (patientProfileId ?? DefaultPatientProfileId).ToString(),
                FullName = "Nguyen Van A",
                DateOfBirth = "1995-05-15",
                Gender = "MALE",
                PhoneNumber = "0987654321",
                IdentityNumber = "012345678901",
                BhytNumber = "DN1234567890",
                Address = "123 Main St, Hanoi",
                BloodType = "O+",
                Allergies = "Dust, Pollen",
                MedicalHistory = "No chronic diseases",
                FamilyHistory = "No genetic conditions",
                LifestyleFactors = "Non-smoker",
                CurrentEyeMedications = "Eye drops twice daily",
                PreviousEyeSurgery = "LASIK in 2020",
                EyeVisionHistory = "Mild myopia"
            };
        }

        public static PatientProfile GetPatientProfile(Guid? id = null, bool hasMedicalDemographics = false)
        {
            return new PatientProfile
            {
                Id = id ?? DefaultPatientProfileId,
                FullName = "Nguyen Van A",
                Dob = new DateTime(1995, 5, 15),
                Gender = Gender.MALE,
                PhoneNumber = "0987654321",
                IdentityNumber = "012345678901",
                BhytNumber = "DN1234567890",
                Address = "123 Main St, Hanoi",
                HasMedicalDemographics = hasMedicalDemographics,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}
