using ECS.Application.Services.DoctorScheduleManagementServices.ViewDoctorPersonalScheduleServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    public static class ViewDoctorPersonalScheduleMockData
    {
        public static readonly Guid DoctorUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public static readonly Guid DoctorProfileId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public static readonly Guid ClinicId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        public static readonly Guid RoomId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        public static readonly Guid ScheduleId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        public static readonly Guid PatientId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        public static readonly Guid SlotId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid AppointmentId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public static DoctorProfile GetActiveDoctorProfile(
            Guid? id = null,
            Guid? userId = null) => new()
        {
            Id = id ?? DoctorProfileId,
            UserId = userId ?? DoctorUserId,
            ClinicId = ClinicId,
            IsActive = true
        };

        public static DoctorProfile GetInactiveDoctorProfile(
            Guid? id = null,
            Guid? userId = null) => new()
        {
            Id = id ?? DoctorProfileId,
            UserId = userId ?? DoctorUserId,
            ClinicId = ClinicId,
            IsActive = false
        };

        public static FacilityRoom GetActiveRoom(
            Guid? id = null,
            string roomName = "Phong Kham 1") => new()
        {
            Id = id ?? RoomId,
            ClinicId = ClinicId,
            RoomName = roomName,
            IsActive = true
        };

        public static TimeSlot GetTimeSlot(
            Guid? scheduleId = null,
            DateTime? startTime = null,
            DateTime? endTime = null,
            SlotStatus status = SlotStatus.AVAILABLE,
            int maxPatients = 1,
            int currentPatients = 0,
            ICollection<Appointment>? appointments = null) => new()
        {
            Id = SlotId,
            ScheduleId = scheduleId ?? ScheduleId,
            StartTime = startTime ?? DateTime.UtcNow.Date.AddHours(8),
            EndTime = endTime ?? DateTime.UtcNow.Date.AddHours(9),
            Status = status,
            MaxPatients = maxPatients,
            CurrentPatients = currentPatients,
            Appointments = appointments
        };

        public static TimeSlot GetTimeSlotWithNullAppointments(Guid? scheduleId = null) => new()
        {
            Id = SlotId,
            ScheduleId = scheduleId ?? ScheduleId,
            StartTime = DateTime.UtcNow.Date.AddHours(8),
            EndTime = DateTime.UtcNow.Date.AddHours(9),
            Status = SlotStatus.AVAILABLE,
            MaxPatients = 1,
            CurrentPatients = 0,
            Appointments = null!
        };

        public static PatientProfile GetPatient(
            Guid? id = null,
            string fullName = "Nguyen Van B",
            string? phoneNumber = "0909123456") => new()
        {
            Id = id ?? PatientId,
            FullName = fullName,
            PhoneNumber = phoneNumber
        };

        public static Appointment GetAppointment(
            Guid? id = null,
            Guid? patientId = null,
            AppointmentStatus status = AppointmentStatus.PENDING,
            string? symptoms = "Dau mat") => new()
        {
            Id = id ?? AppointmentId,
            PatientId = patientId ?? PatientId,
            SlotId = SlotId,
            AppointmentDate = DateTime.UtcNow.AddDays(1),
            Status = status,
            Symptoms = symptoms,
            Patient = GetPatient()
        };

        public static DoctorSchedule GetSchedule(
            Guid? id = null,
            Guid? doctorId = null,
            DateTime? workDate = null,
            ShiftType shiftType = ShiftType.MORNING,
            FacilityRoom? room = null,
            ICollection<TimeSlot>? timeSlots = null,
            string? note = "Schedule note") => new()
        {
            Id = id ?? ScheduleId,
            DoctorId = doctorId ?? DoctorProfileId,
            WorkDate = workDate ?? DateTime.UtcNow.Date,
            ShiftType = shiftType,
            Note = note,
            IsDeleted = false,
            Room = room,
            TimeSlots = timeSlots
        };

        public static DoctorSchedule GetScheduleWithNullTimeSlots(
            Guid? id = null,
            Guid? doctorId = null) => new()
        {
            Id = id ?? ScheduleId,
            DoctorId = doctorId ?? DoctorProfileId,
            WorkDate = DateTime.UtcNow.Date,
            ShiftType = ShiftType.MORNING,
            IsDeleted = false,
            Room = GetActiveRoom(),
            TimeSlots = null!
        };

        public static DoctorSchedule GetScheduleWithoutRoom(
            Guid? id = null,
            Guid? doctorId = null) => new()
        {
            Id = id ?? ScheduleId,
            DoctorId = doctorId ?? DoctorProfileId,
            WorkDate = DateTime.UtcNow.Date,
            ShiftType = ShiftType.MORNING,
            IsDeleted = false,
            Room = null,
            TimeSlots = new List<TimeSlot> { GetTimeSlot() }
        };

        public static ViewDoctorPersonalScheduleRequest GetRequest(
            DateOnly? workDate = null,
            ShiftType? shiftType = null) => new()
        {
            WorkDate = workDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            ShiftType = shiftType
        };

        public static List<Appointment> GetMixedAppointments()
        {
            var activeAppointment = GetAppointment(
                id: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                status: AppointmentStatus.CONFIRMED);
            var cancelledAppointment = GetAppointment(
                id: Guid.Parse("44444444-4444-4444-4444-444444444444"),
                status: AppointmentStatus.CANCELLED);
            var completedAppointment = GetAppointment(
                id: Guid.Parse("55555555-5555-5555-5555-555555555555"),
                status: AppointmentStatus.COMPLETED);

            return new List<Appointment> { activeAppointment, cancelledAppointment, completedAppointment };
        }

        public static List<Appointment> GetAllCancelledAppointments()
        {
            var cancelled1 = GetAppointment(
                id: Guid.Parse("66666666-6666-6666-6666-666666666666"),
                status: AppointmentStatus.CANCELLED);
            var cancelled2 = GetAppointment(
                id: Guid.Parse("77777777-7777-7777-7777-777777777777"),
                status: AppointmentStatus.CANCELLED);

            return new List<Appointment> { cancelled1, cancelled2 };
        }
    }
}
