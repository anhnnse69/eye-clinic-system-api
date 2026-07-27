using ECS.Application.Services.ReceptionistManagementServices.ReceptionistCreateWalkinAppointmentServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;

namespace ECS.Test.MockData
{
    public static class ReceptionistCreateWalkinAppointmentMockData
    {
        public static readonly Guid ValidPatientId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid DoctorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid RoomId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid ClinicId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static readonly Guid ScheduleId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        public static readonly Guid SlotId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        public static readonly Guid ServiceId = Guid.Parse("77777777-7777-7777-7777-777777777777");

        public static PatientProfile GetPatientProfile()
        {
            return new PatientProfile
            {
                Id = ValidPatientId,
                FullName = "Test Patient",
                PhoneNumber = "0987654321"
            };
        }

        public static TimeSlot GetTimeSlot(bool withRoom = true)
        {
            return new TimeSlot
            {
                Id = SlotId,
                ScheduleId = ScheduleId,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(1),
                Schedule = new DoctorSchedule
                {
                    Id = ScheduleId,
                    DoctorId = DoctorId,
                    Room = withRoom ? new FacilityRoom { Id = RoomId, RoomName = "Room 101", ClinicId = ClinicId } : null,
                    Doctor = new DoctorProfile
                    {
                        Id = DoctorId,
                        ClinicId = ClinicId,
                        User = new User
                        {
                            Id = DoctorId,
                            FullName = "Dr. Strange",
                            Phone = "0123456789"
                        }
                    }
                }
            };
        }

        public static ReceptionistCreateWalkinAppointmentRequest GetValidRequest(string? symptoms = "Mild headache")
        {
            return new ReceptionistCreateWalkinAppointmentRequest
            {
                PatientProfileId = ValidPatientId,
                DoctorId = DoctorId,
                ServiceId = ServiceId,
                Symptoms = symptoms
            };
        }
    }
}