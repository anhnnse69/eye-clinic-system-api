using ECS.Application.Services.SystemAdminServices.AdminSystemGetDashboardServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;

namespace ECS.Test.MockData
{
    /// <summary>
    /// Reusable mock data helper for System Admin Dashboard unit tests.
    /// Provides seed data and request builder methods ensuring 100% test coverage.
    /// </summary>
    public static class SystemAdminDashboardMockData
    {
        // Static GUIDs for consistency
        public static readonly Guid DoctorUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid ClinicAdminUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid ReceptionistUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid PatientUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static readonly Guid PatientProfileId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        public static readonly Guid DoctorProfileId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        public static readonly Guid ServiceId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        public static readonly Guid SlotId = Guid.Parse("88888888-8888-8888-8888-888888888888");

        #region Request Factory Methods

        public static AdminSystemGetDashboardRequest GetDefaultRequest() => new();

        public static AdminSystemGetDashboardRequest GetClinicFilteredRequest(Guid clinicId) => new()
        {
            ClinicId = clinicId
        };

        public static AdminSystemGetDashboardRequest GetDateFilteredRequest(DateTime start, DateTime end) => new()
        {
            StartDate = start,
            EndDate = end
        };

        #endregion

        #region Database Seeder

        public static async Task SeedDashboardDataAsync(AppDbContext context)
        {
            // 1. Setup Clinics
            var primaryClinic = SystemAdminClinicMockData.GetClinicWithPublicationRequest();
            var secondaryClinic = SystemAdminClinicMockData.GetInactiveClinic();

            // 2. Setup Accounts & Staff Links
            var doctorUser = new User
            {
                Id = DoctorUserId,
                FullName = "Dr. John Doe",
                Phone = "0901000001",
                Email = "doctor@ecs.vn",
                PasswordHash = "hash",
                Role = UserRole.DOCTOR,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-1)
            };

            var clinicAdminUser = new User
            {
                Id = ClinicAdminUserId,
                FullName = "Clinic Admin",
                Phone = "0901000002",
                Email = "clinicadmin@ecs.vn",
                PasswordHash = "hash",
                Role = UserRole.CLINIC_ADMIN,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-1),
                StaffClinics = new List<StaffClinic>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        UserId = ClinicAdminUserId,
                        ClinicId = primaryClinic.Id,
                        Role = StaffRole.CLINIC_ADMIN,
                        IsActive = true
                    }
                }
            };

            var receptionistUser = new User
            {
                Id = ReceptionistUserId,
                FullName = "Receptionist Staff",
                Phone = "0901000003",
                Email = "receptionist@ecs.vn",
                PasswordHash = "hash",
                Role = UserRole.RECEPTIONIST,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-1),
                StaffClinics = new List<StaffClinic>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        UserId = ReceptionistUserId,
                        ClinicId = primaryClinic.Id,
                        Role = StaffRole.RECEPTIONIST,
                        IsActive = true
                    }
                }
            };

            var systemAdminUser = SystemAdminAccountMockData.GetSystemAdminUser();

            var patientUser = new User
            {
                Id = PatientUserId,
                FullName = "Patient User",
                Phone = "0901000004",
                Email = "patient@ecs.vn",
                PasswordHash = "hash",
                Role = UserRole.PATIENT,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-1)
            };

            // 3. Setup Patient Profile
            var patientProfile = new PatientProfile
            {
                Id = PatientProfileId,
                UserId = PatientUserId,
                FullName = "Nguyen Van Patient",
                PhoneNumber = "0901000004",
                CreatedAt = DateTime.UtcNow.AddYears(-1)
            };

            // 4. Setup Doctor Profile (Linked to Secondary Clinic so filtering Primary Clinic yields Doctor count = 0)
            var doctorProfile = new DoctorProfile
            {
                Id = DoctorProfileId,
                UserId = DoctorUserId,
                ClinicId = secondaryClinic.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-1)
            };

            // 5. Setup Service (Linked to Primary Clinic)
            var service = new Service
            {
                Id = ServiceId,
                ClinicId = primaryClinic.Id,
                ServiceName = "Comprehensive Eye Exam",
                Price = 350000m,
                IsActive = true
            };

            // 6. Setup TimeSlot
            var slot = new TimeSlot
            {
                Id = SlotId,
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddHours(2),
                Status = SlotStatus.BOOKED
            };

            // 7. Setup Appointments (2 in last 7 days, 1 outside 7 days)
            var inRangeDate = DateTime.UtcNow.Date.AddDays(-2);
            var outOfRangeDate = DateTime.UtcNow.Date.AddDays(-30);

            var appointments = new List<Appointment>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientProfile.Id,
                    DoctorId = doctorProfile.Id,
                    ServiceId = service.Id,
                    SlotId = slot.Id,
                    AppointmentDate = inRangeDate,
                    Status = AppointmentStatus.PENDING,
                    CreatedAt = inRangeDate
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientProfile.Id,
                    DoctorId = doctorProfile.Id,
                    ServiceId = service.Id,
                    SlotId = slot.Id,
                    AppointmentDate = inRangeDate,
                    Status = AppointmentStatus.COMPLETED,
                    CreatedAt = inRangeDate
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientProfile.Id,
                    DoctorId = doctorProfile.Id,
                    ServiceId = service.Id,
                    SlotId = slot.Id,
                    AppointmentDate = outOfRangeDate,
                    Status = AppointmentStatus.CANCELLED,
                    CreatedAt = outOfRangeDate
                }
            };

            // 8. Setup Clinic Registration Requests
            var pendingRequests = new List<ClinicRegistrationRequest>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ClinicName = "New Clinic A",
                    ClinicAddress = "123 Street A",
                    ContactName = "Owner A",
                    ContactPhone = "0900000001",
                    ContactEmail = "a@clinic.vn",
                    Status = "PENDING",
                    RequestedAt = DateTime.UtcNow.AddDays(-1)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ClinicName = "New Clinic B",
                    ClinicAddress = "456 Street B",
                    ContactName = "Owner B",
                    ContactPhone = "0900000002",
                    ContactEmail = "b@clinic.vn",
                    Status = "PENDING",
                    RequestedAt = DateTime.UtcNow.AddDays(-2)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ClinicName = "Approved Clinic",
                    ClinicAddress = "789 Street C",
                    ContactName = "Owner C",
                    ContactPhone = "0900000003",
                    ContactEmail = "c@clinic.vn",
                    Status = "APPROVED",
                    RequestedAt = DateTime.UtcNow.AddDays(-3)
                }
            };

            // Seed into In-Memory DbContext
            await context.Clinics.AddRangeAsync(primaryClinic, secondaryClinic);
            await context.Users.AddRangeAsync(doctorUser, clinicAdminUser, receptionistUser, systemAdminUser, patientUser);
            await context.PatientProfiles.AddAsync(patientProfile);
            await context.DoctorProfiles.AddAsync(doctorProfile);
            await context.Services.AddAsync(service);
            await context.TimeSlots.AddAsync(slot);
            await context.Appointments.AddRangeAsync(appointments);
            await context.ClinicRegistrationRequests.AddRangeAsync(pendingRequests);

            await context.SaveChangesAsync();
        }

        #endregion
    }
}