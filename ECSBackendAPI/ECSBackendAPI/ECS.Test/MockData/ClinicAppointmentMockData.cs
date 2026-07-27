using ECS.Application.Services.ClinicAdminManagementServices.ClinicAppointmentServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Test.MockData
{
    /// <summary>
    /// Provides reusable mock data for ClinicAppointment unit tests.
    /// </summary>
    public static class ClinicAppointmentMockData
    {
        // ── Deterministic identifiers ──────────────────────────────────────────

        public static Guid TestClinicId => Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public static Guid TestDoctorUserId => Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static Guid TestDoctorProfileId => Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        public static Guid TestPatientProfileId => Guid.Parse("99999999-9999-9999-9999-999999999999");
        public static Guid TestServiceId => Guid.Parse("77777777-7777-7777-7777-777777777777");
        public static Guid TestSlotId => Guid.Parse("55555555-5555-5555-5555-555555555555");
        public static Guid TestStaffClinicId => Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        // ── Aggregate helpers ──────────────────────────────────────────────────

        public static Clinic GetTestClinic() => new Clinic
        {
            Id = TestClinicId,
            Name = "Bệnh viện Mắt Sài Gòn",
            Address = "123 Nguyen Hue, District 1, HCMC",
            Phone = "02873001234",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddYears(-2),
            UpdatedAt = DateTime.UtcNow.AddYears(-1)
        };

        /// <summary>
        /// Returns a deterministic <see cref="TimeSlot"/> with start/end times.
        /// </summary>
        public static TimeSlot GetTestTimeSlot(DateTime start, DateTime end) => new TimeSlot
        {
            Id = TestSlotId,
            StartTime = start,
            EndTime = end,
            MaxPatients = 1,
            CurrentPatients = 0,
            Status = SlotStatus.BOOKED
        };

        /// <summary>
        /// Returns a deterministic <see cref="Service"/> wired to the test clinic.
        /// </summary>
        public static Service GetTestService() => new Service
        {
            Id = TestServiceId,
            ClinicId = TestClinicId,
            ServiceName = "Eye Examination",
            Price = 200000m,
            DurationMinutes = 30,
            IsActive = true
        };

        /// <summary>
        /// Returns a deterministic <see cref="PatientProfile"/>.
        /// </summary>
        public static PatientProfile GetTestPatientProfile(
            string fullName = "Nguyen Van Patient",
            string phoneNumber = "0901111222") => new PatientProfile
        {
            Id = TestPatientProfileId,
            FullName = fullName,
            PhoneNumber = phoneNumber,
            Gender = Gender.MALE,
            Dob = new DateTime(1990, 5, 15),
            CreatedAt = DateTime.UtcNow.AddYears(-3),
            UpdatedAt = DateTime.UtcNow.AddMonths(-6)
        };

        /// <summary>
        /// Returns a deterministic <see cref="DoctorProfile"/> wired to the test clinic
        /// and a doctor <see cref="User"/>.
        /// </summary>
        public static DoctorProfile GetTestDoctorProfile(
            User doctorUser,
            Clinic clinic,
            string fullName = "BS. Le Van Doctor") => new DoctorProfile
        {
            Id = TestDoctorProfileId,
            UserId = doctorUser.Id,
            ClinicId = clinic.Id,
            Clinic = clinic,
            Title = "Senior Ophthalmologist",
            ExperienceYears = 12,
            Bio = "Experienced eye-care professional.",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddYears(-2),
            UpdatedAt = DateTime.UtcNow.AddYears(-1),
            User = new User
            {
                Id = doctorUser.Id,
                FullName = fullName,
                Phone = "0933334444",
                Email = "doctor@ECS.vn",
                PasswordHash = "x",
                Role = UserRole.DOCTOR,
                IsActive = true
            }
        };

        /// <summary>
        /// Returns an active <see cref="StaffClinic"/> row that links the given user to the test clinic.
        /// </summary>
        public static StaffClinic GetActiveStaffClinic(User user, Clinic clinic) => new StaffClinic
        {
            Id = TestStaffClinicId,
            UserId = user.Id,
            ClinicId = clinic.Id,
            Role = StaffRole.CLINIC_ADMIN,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddYears(-1),
            UpdatedAt = DateTime.UtcNow.AddMonths(-6)
        };

        /// <summary>
        /// Returns an <see cref="Appointment"/> fully wired with Patient, Doctor (User), Slot and (optional) Service.
        /// </summary>
        public static Appointment GetTestAppointment(
            DateTime appointmentDate,
            TimeSlot slot,
            DoctorProfile doctor,
            PatientProfile patient,
            Service? service = null,
            string? symptoms = null,
            AppointmentStatus status = AppointmentStatus.BOOKED,
            decimal depositAmount = 100000m,
            bool depositPaid = true,
            string bookingSource = "ONLINE")
        {
            return new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                Patient = patient,
                DoctorId = doctor.Id,
                Doctor = doctor,
                SlotId = slot.Id,
                Slot = slot,
                ServiceId = service?.Id,
                Service = service,
                AppointmentDate = appointmentDate,
                Symptoms = symptoms,
                Status = status,
                DepositAmount = depositAmount,
                DepositPaid = depositPaid,
                BookingSource = bookingSource,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
                CreatedById = null,
                NoteReason = null
            };
        }

        // ── Request factories ──────────────────────────────────────────────────

        /// <summary>
        /// Returns a default request with no filters and PageNumber=1, PageSize=10.
        /// </summary>
        public static GetClinicAppointmentsRequest GetDefaultRequest() => new GetClinicAppointmentsRequest
        {
            SearchTerm = null,
            Status = null,
            AppointmentDate = null,
            PageNumber = 1,
            PageSize = 10
        };

        /// <summary>
        /// Returns a request that exercises every filter branch (Status, AppointmentDate, SearchTerm).
        /// PageNumber=1, PageSize=10 so the single seeded appointment is returned (no Skip).
        /// </summary>
        public static GetClinicAppointmentsRequest GetFilteredRequest(
            DateTime appointmentDate,
            AppointmentStatus status,
            string searchTerm) => new GetClinicAppointmentsRequest
        {
            SearchTerm = searchTerm,
            Status = status,
            AppointmentDate = appointmentDate,
            PageNumber = 1,
            PageSize = 10
        };
    }
}