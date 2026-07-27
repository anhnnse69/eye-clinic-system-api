using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    public static class ClinicShiftMockData
    {
        public static readonly Guid ReceptionistUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid ClinicId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public static StaffClinic GetActiveStaffClinic()
        {
            return new StaffClinic
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                UserId = ReceptionistUserId,
                ClinicId = ClinicId,
                Role = StaffRole.RECEPTIONIST,
                IsActive = true
            };
        }

        public static Clinic GetActiveClinic(TimeOnly openTime, TimeOnly closeTime)
        {
            return new Clinic
            {
                Id = ClinicId,
                Name = "ECS Eye Clinic",
                Address = "123 Nguyen Trai, Ha Noi",
                Phone = "02412345678",
                Email = "contact@ecs-clinic.vn",
                IsActive = true,
                OpenTime = openTime,
                CloseTime = closeTime
            };
        }

        public static Clinic GetActiveClinic_StandardHours()
        {
            return GetActiveClinic(new TimeOnly(8, 0), new TimeOnly(20, 0));
        }

        public static Clinic GetActiveClinic_MorningOnly()
        {
            return GetActiveClinic(new TimeOnly(7, 0), new TimeOnly(12, 0));
        }

        public static Clinic GetActiveClinic_AfternoonOnly()
        {
            return GetActiveClinic(new TimeOnly(12, 0), new TimeOnly(17, 0));
        }

        public static Clinic GetActiveClinic_PartialHours()
        {
            return GetActiveClinic(new TimeOnly(7, 30), new TimeOnly(17, 30));
        }
    }
}
