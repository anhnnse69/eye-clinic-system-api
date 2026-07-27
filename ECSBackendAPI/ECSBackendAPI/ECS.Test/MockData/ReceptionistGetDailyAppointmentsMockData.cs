using ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetDailyAppointmentsServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    public static class ReceptionistGetDailyAppointmentsMockData
    {
        public static readonly Guid ReceptionistUserId = Guid.NewGuid();
        public static readonly Guid ClinicId = Guid.NewGuid();
        public static readonly Guid DoctorId = Guid.NewGuid();
        public static readonly Guid PatientId = Guid.NewGuid();

        public static List<StaffClinic> GetStaffClinics()
        {
            return new List<StaffClinic>
            {
                new StaffClinic
                {
                    UserId = ReceptionistUserId,
                    ClinicId = ClinicId,
                    IsActive = true,
                    Role = StaffRole.RECEPTIONIST
                }
            };
        }

        public static List<Appointment> GetAppointments(DateTime targetDate)
        {
            var doctor = new DoctorProfile
            {
                Id = DoctorId,
                ClinicId = ClinicId,
                User = new User { FullName = "Dr. Gregory House" }
            };

            var room1 = new FacilityRoom { RoomName = "Room 101" };

            var scheduleMorning = new DoctorSchedule
            {
                ShiftType = ShiftType.MORNING,
                Room = room1
            };

            var scheduleAfternoon = new DoctorSchedule
            {
                ShiftType = ShiftType.AFTERNOON,
                Room = null
            };

            var slot1 = new TimeSlot
            {
                Id = Guid.NewGuid(),
                ScheduleId = Guid.NewGuid(),
                Schedule = scheduleMorning,
                StartTime = targetDate.AddHours(8),
                EndTime = targetDate.AddHours(9)
            };

            var slot2 = new TimeSlot
            {
                Id = Guid.NewGuid(),
                ScheduleId = Guid.NewGuid(),
                Schedule = scheduleAfternoon,
                StartTime = targetDate.AddHours(14),
                EndTime = targetDate.AddHours(15)
            };

            var patient1 = new PatientProfile
            {
                Id = PatientId,
                FullName = "Alice Smith",
                Gender = Gender.FEMALE,
                Dob = new DateTime(1995, 5, 5),
                PhoneNumber = "0988123456",
                BhytNumber = "BHYT12345"
            };

            var patient2 = new PatientProfile
            {
                Id = Guid.NewGuid(),
                FullName = "Bob Johnson",
                Gender = Gender.MALE,
                Dob = new DateTime(1990, 1, 1),
                PhoneNumber = null
            };

            return new List<Appointment>
            {
                new Appointment
                {
                    Id = Guid.NewGuid(),
                    PatientId = patient1.Id,
                    DoctorId = doctor.Id,
                    SlotId = slot1.Id,
                    AppointmentDate = targetDate,
                    Symptoms = "Fever",
                    Status = AppointmentStatus.ARRIVED,
                    DepositAmount = 100,
                    DepositPaid = true,
                    BookingSource = "WALKIN",
                    CreatedAt = DateTime.UtcNow,
                    Patient = patient1,
                    Doctor = doctor,
                    Slot = slot1,
                    Queue = new Queue
                    {
                        Id = Guid.NewGuid(),
                        QueueNumber = 1,
                        Status = QueueStatus.WAITING,
                        CalledAt = DateTime.UtcNow
                    }
                },
                new Appointment
                {
                    Id = Guid.NewGuid(),
                    PatientId = patient2.Id,
                    DoctorId = doctor.Id,
                    SlotId = slot2.Id,
                    AppointmentDate = targetDate,
                    Symptoms = null,
                    Status = AppointmentStatus.COMPLETED,
                    DepositAmount = 0,
                    DepositPaid = false,
                    BookingSource = "ONLINE",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                    Patient = patient2,
                    Doctor = doctor,
                    Slot = slot2,
                    Queue = null
                },
                new Appointment
                {
                    Id = Guid.NewGuid(),
                    PatientId = patient1.Id,
                    DoctorId = doctor.Id,
                    SlotId = slot1.Id,
                    AppointmentDate = targetDate,
                    Status = AppointmentStatus.CANCELLED,
                    BookingSource = "ONLINE",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-20),
                    Patient = patient1,
                    Doctor = doctor,
                    Slot = slot1,
                    Queue = null
                },
                new Appointment
                {
                    Id = Guid.NewGuid(),
                    PatientId = patient1.Id,
                    DoctorId = doctor.Id,
                    SlotId = slot1.Id,
                    AppointmentDate = targetDate,
                    Status = AppointmentStatus.IN_PROGRESS,
                    BookingSource = "ONLINE",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                    Patient = patient1,
                    Doctor = doctor,
                    Slot = slot1,
                    Queue = null
                }
            };
        }

        public static ReceptionistGetDailyAppointmentsRequest GetValidRequest(DateTime? targetDate = null)
        {
            return new ReceptionistGetDailyAppointmentsRequest
            {
                CurrentUserId = ReceptionistUserId,
                TargetDate = targetDate,
                PageNumber = 1,
                PageSize = 10
            };
        }
    }
}