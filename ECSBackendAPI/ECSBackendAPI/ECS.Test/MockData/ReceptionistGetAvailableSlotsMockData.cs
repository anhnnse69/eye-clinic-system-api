using ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetAvailableSlotsServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    public static class ReceptionistGetAvailableSlotsMockData
    {
        public static readonly Guid ReceptionistUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid ClinicId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid DoctorId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid SpecialtyId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static readonly Guid RoomId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        public static readonly Guid ScheduleId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        public static StaffClinic GetActiveStaffClinic()
        {
            return new StaffClinic
            {
                Id = Guid.NewGuid(),
                UserId = ReceptionistUserId,
                ClinicId = ClinicId,
                IsActive = true
            };
        }

        public static GetAvailableSlotsRequest GetValidRequest()
        {
            return new GetAvailableSlotsRequest
            {
                CurrentUserId = ReceptionistUserId,
                WorkDate = DateTime.Today,
                SearchDoctor = null,
                ShiftType = null,
                SpecialtyId = null
            };
        }

        public static DoctorSchedule GetDoctorSchedule(
            DateTime workDate,
            ShiftType shiftType = ShiftType.MORNING,
            bool isDeleted = false,
            DateTime? slotStartTime = null)
        {
            var startTime = slotStartTime ?? DateTime.UtcNow.AddHours(2);

            return new DoctorSchedule
            {
                Id = ScheduleId,
                DoctorId = DoctorId,
                WorkDate = workDate,
                ShiftType = shiftType,
                IsDeleted = isDeleted,
                Room = new FacilityRoom
                {
                    Id = RoomId,
                    ClinicId = ClinicId,
                    RoomName = "Room 101"
                },
                Doctor = new DoctorProfile
                {
                    Id = DoctorId,
                    ClinicId = ClinicId,
                    SpecialtyId = SpecialtyId,
                    Title = "Dr.",
                    Specialty = new Specialty
                    {
                        Id = SpecialtyId,
                        Name = "Ophthalmology"
                    },
                    User = new User
                    {
                        Id = DoctorId,
                        FullName = "John Doe",
                        Phone = "0123456789"
                    }
                },
                TimeSlots = new List<TimeSlot>
                {
                    new TimeSlot
                    {
                        Id = Guid.NewGuid(),
                        ScheduleId = ScheduleId,
                        StartTime = startTime,
                        EndTime = startTime.AddMinutes(30),
                        MaxPatients = 5,
                        CurrentPatients = 1,
                        Status = SlotStatus.AVAILABLE
                    }
                }
            };
        }
    }
}