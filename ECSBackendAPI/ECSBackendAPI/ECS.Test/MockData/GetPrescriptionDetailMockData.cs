using System.Security.Claims;
using ECS.Application.Services.PatientAppointmentManagementServices.GetPrescriptionDetailServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Moq;

namespace ECS.Test.MockData
{
    public static class GetPrescriptionDetailMockData
    {
        public static readonly Guid ValidUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid ValidPatientId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid ValidDoctorId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static readonly Guid ValidSlotId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        public static readonly Guid ValidServiceId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        public static readonly Guid ValidAppointmentId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        public static readonly Guid ValidClinicId = Guid.Parse("88888888-8888-8888-8888-888888888888");

        public static GetPrescriptionDetailRequest GetValidRequest(Guid? appointmentId = null)
            => new() { AppointmentId = appointmentId ?? ValidAppointmentId };

        public static Mock<IHttpContextAccessor> GetHttpContextAccessorMock(Guid? userId = null, bool isAuthenticated = true)
        {
            var mockAccessor = new Mock<IHttpContextAccessor>();

            if (!isAuthenticated || !userId.HasValue)
            {
                mockAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null!);
                return mockAccessor;
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.Value.ToString())
            };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var context = new DefaultHttpContext { User = claimsPrincipal };

            mockAccessor.Setup(x => x.HttpContext).Returns(context);
            return mockAccessor;
        }

        public static Appointment GetAppointmentWithDetails(
            Guid? id = null,
            Guid? patientId = null,
            Guid? doctorId = null,
            Guid? slotId = null,
            Guid? serviceId = null,
            Guid? createdById = null,
            Guid? clinicId = null,
            AppointmentStatus status = AppointmentStatus.COMPLETED)
        {
            var appointment = new Appointment
            {
                Id = id ?? ValidAppointmentId,
                PatientId = patientId ?? ValidPatientId,
                DoctorId = doctorId ?? ValidDoctorId,
                SlotId = slotId ?? ValidSlotId,
                ServiceId = serviceId ?? ValidServiceId,
                CreatedById = createdById,
                AppointmentDate = new DateTime(2025, 1, 1),
                CreatedAt = new DateTime(2025, 1, 1, 7, 0, 0),
                Status = status
            };

            appointment.Patient = new PatientProfile
            {
                Id = patientId ?? ValidPatientId,
                UserId = createdById ?? ValidUserId,
                FullName = "Nguyễn Văn A",
                PhoneNumber = "0123456789",
                Gender = Gender.MALE,
                Dob = new DateTime(1990, 1, 1),
                Address = "123 Đường ABC",
                User = new User
                {
                    Id = createdById ?? ValidUserId,
                    Email = "patient@example.com",
                    FullName = "Patient User",
                    Phone = "0123456789",
                    PasswordHash = "hash"
                }
            };

            appointment.Doctor = new DoctorProfile
            {
                Id = doctorId ?? ValidDoctorId,
                UserId = Guid.NewGuid(),
                ClinicId = clinicId ?? ValidClinicId,
                Title = "BS. CKII",
                User = new User
                {
                    Id = Guid.NewGuid(),
                    FullName = "Trần Văn B",
                    Email = "doctor@example.com",
                    Phone = "0987654321",
                    PasswordHash = "hash"
                },
                Clinic = new Clinic
                {
                    Id = clinicId ?? ValidClinicId,
                    Name = "Phòng khám mắt Sài Gòn",
                    Address = "456 Nguyễn Thị Minh Khai",
                    Phone = "028383838"
                }
            };

            return appointment;
        }

        public static void SeedUserAccess(AppDbContext context, Guid userId, Guid patientId)
        {
            context.PatientProfiles.Add(new PatientProfile
            {
                Id = patientId,
                UserId = userId,
                FullName = "Nguyễn Văn A",
                Gender = Gender.MALE,
                Dob = new DateTime(1990, 1, 1),
                PhoneNumber = "0123456789"
            });

            context.UserPatients.Add(new UserPatient
            {
                UserId = userId,
                PatientId = patientId,
                Relationship = "Bản thân"
            });

            context.SaveChanges();
        }
    }
}
