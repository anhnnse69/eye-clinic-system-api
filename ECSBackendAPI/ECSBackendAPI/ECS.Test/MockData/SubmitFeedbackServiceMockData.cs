using ECS.Application.Services.PatientAppointmentManagementServices.SubmitFeedbackServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Feedbacks;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace ECS.Test.MockData
{
    public static class SubmitFeedbackServiceMockData
    {
        public static readonly Guid ValidUserId = Guid.NewGuid();
        public static readonly Guid DoctorUserId = Guid.NewGuid();
        public static readonly Guid PatientId = Guid.NewGuid();
        public static readonly Guid LinkedPatientId = Guid.NewGuid();
        public static readonly Guid OtherUserId = Guid.NewGuid();
        public static readonly Guid DoctorId = Guid.NewGuid();
        public static readonly Guid ClinicId = Guid.NewGuid();
        public static readonly Guid AppointmentId = Guid.NewGuid();

        public static Mock<IHttpContextAccessor> CreateMockHttpContextAccessor(Guid? userId)
        {
            var mockAccessor = new Mock<IHttpContextAccessor>();
            if (userId.HasValue)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())
                };
                var identity = new ClaimsIdentity(claims, "TestAuthType");
                var claimsPrincipal = new ClaimsPrincipal(identity);
                var httpContext = new DefaultHttpContext { User = claimsPrincipal };

                mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);
            }
            else
            {
                var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal() };
                mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);
            }

            return mockAccessor;
        }

        public static Clinic CreateClinic(Guid? id = null)
        {
            return new Clinic
            {
                Id = id ?? ClinicId,
                Name = "Test Clinic",
                Address = "123 Test St",
                Phone = "0123456789",
                RatingAvg = 0,
                ReviewCount = 0
            };
        }

        public static User CreateUser(Guid? id = null)
        {
            return new User
            {
                Id = id ?? DoctorUserId,
                Phone = "0901234567",
                Email = "doctor@test.com",
                PasswordHash = "hashed_password",
                FullName = "Dr. John Doe",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static DoctorProfile CreateDoctor(Guid? id = null, Guid? clinicId = null, Clinic clinic = null)
        {
            var cId = clinicId ?? ClinicId;
            var docUserId = DoctorUserId;
            return new DoctorProfile
            {
                Id = id ?? DoctorId,
                UserId = docUserId,
                User = CreateUser(docUserId),
                ClinicId = cId,
                Clinic = clinic ?? CreateClinic(cId),
                Title = "Dr.",
                ExperienceYears = 5,
                Bio = "Test Bio",
                IsActive = true,
                RatingAvg = 0,
                ReviewCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static Appointment CreateAppointment(
            Guid? id = null,
            Guid? patientId = null,
            Guid? createdById = null,
            AppointmentStatus status = AppointmentStatus.COMPLETED,
            DoctorProfile doctor = null,
            Feedback feedback = null)
        {
            var doc = doctor ?? CreateDoctor();
            return new Appointment
            {
                Id = id ?? AppointmentId,
                PatientId = patientId ?? PatientId,
                CreatedById = createdById ?? ValidUserId,
                DoctorId = doc.Id,
                Doctor = doc,
                Status = status,
                Feedback = feedback
            };
        }

        public static SubmitFeedbackRequest CreateValidRequest(Guid? appointmentId = null)
        {
            return new SubmitFeedbackRequest
            {
                AppointmentId = appointmentId ?? AppointmentId,
                RatingDoctor = 5,
                RatingClinic = 4,
                Comment = "Great service!",
                IsPublic = true
            };
        }
    }
}