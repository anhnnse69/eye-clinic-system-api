using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Test.MockData
{
    public static class GetPatientProfilesForBookingMockData
    {
        public static readonly Guid ValidUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid ValidPatientId1 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid ValidPatientId2 = Guid.Parse("33333333-3333-3333-3333-333333333333");

        public static PatientProfile GetPatientProfile(
            Guid? id = null,
            Guid? userId = null,
            string fullName = "Nguyễn Văn A",
            Gender gender = Gender.MALE,
            DateTime? dob = null)
        {
            return new PatientProfile
            {
                Id = id ?? ValidPatientId1,
                UserId = userId ?? ValidUserId,
                FullName = fullName,
                Gender = gender,
                Dob = dob ?? new DateTime(1995, 5, 20),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static UserPatient GetUserPatient(
            Guid? userId = null,
            Guid? patientId = null,
            string relationship = "Mẹ")
        {
            return new UserPatient
            {
                UserId = userId ?? ValidUserId,
                PatientId = patientId ?? ValidPatientId2,
                Relationship = relationship
            };
        }
    }
}
