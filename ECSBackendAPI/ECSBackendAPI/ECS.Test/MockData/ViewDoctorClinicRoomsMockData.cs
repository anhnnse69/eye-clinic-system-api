using ECS.Domain.Entities.Clinics;

namespace ECS.Test.MockData
{
    public static class ViewDoctorClinicRoomsMockData
    {
        public static readonly Guid DefaultDoctorUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid DefaultDoctorProfileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid DefaultClinicId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        public static DoctorProfile GetDoctorProfile(Guid? id = null, Guid? userId = null, Guid? clinicId = null, bool isActive = true)
        {
            return new DoctorProfile
            {
                Id = id ?? DefaultDoctorProfileId,
                UserId = userId ?? DefaultDoctorUserId,
                ClinicId = clinicId ?? DefaultClinicId,
                IsActive = isActive
            };
        }

        public static FacilityRoom GetFacilityRoom(
            Guid? id = null,
            Guid? clinicId = null,
            string roomName = "Phong Kham 101",
            string? roomType = "Consultation",
            bool isActive = true)
        {
            return new FacilityRoom
            {
                Id = id ?? Guid.NewGuid(),
                ClinicId = clinicId ?? DefaultClinicId,
                RoomName = roomName,
                RoomType = roomType,
                IsActive = isActive
            };
        }
    }
}
