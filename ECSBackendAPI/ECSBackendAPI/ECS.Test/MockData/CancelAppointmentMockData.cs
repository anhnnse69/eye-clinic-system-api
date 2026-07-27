using System.Security.Claims;
using ECS.Application.Services.PatientAppointmentManagementServices.CancelAppointmentServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Moq;

namespace ECS.Test.MockData
{
    public static class CancelAppointmentMockData
    {
        public static readonly Guid ValidUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid ValidAppointmentId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid ValidSlotId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        public static CancelAppointmentRequest GetValidRequest(
            Guid? appointmentId = null,
            string? reason = "Bệnh nhân bận việc đột xuất")
        {
            return new CancelAppointmentRequest
            {
                AppointmentId = appointmentId ?? ValidAppointmentId,
                Reason = reason
            };
        }

        public static Appointment GetAppointment(
            Guid? id = null,
            Guid? patientId = null,
            Guid? createdById = null,
            Guid? slotId = null,
            AppointmentStatus status = AppointmentStatus.PENDING,
            DateTime? appointmentDate = null)
        {
            return new Appointment
            {
                Id = id ?? ValidAppointmentId,
                PatientId = patientId ?? ValidUserId,
                CreatedById = createdById ?? ValidUserId,
                SlotId = slotId ?? ValidSlotId,
                Status = status,
                AppointmentDate = appointmentDate ?? DateTime.Now.AddHours(48), // Mặc định đủ điều kiện hủy (>24h)
                NoteReason = null,
                UpdatedAt = DateTime.Now
            };
        }

        public static TimeSlot GetTimeSlot(
            Guid? id = null,
            int currentPatients = 1,
            int maxPatients = 2,
            SlotStatus status = SlotStatus.BOOKED)
        {
            return new TimeSlot
            {
                Id = id ?? ValidSlotId,
                CurrentPatients = currentPatients,
                MaxPatients = maxPatients,
                Status = status
            };
        }

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
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var context = new DefaultHttpContext
            {
                User = claimsPrincipal
            };

            mockAccessor.Setup(x => x.HttpContext).Returns(context);
            return mockAccessor;
        }
        public static Mock<IHttpContextAccessor> GetHttpContextAccessorWithInvalidGuidMock()
        {
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, "not-a-valid-guid-12345") // Guid.TryParse sẽ thất bại
    };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var context = new DefaultHttpContext
            {
                User = claimsPrincipal
            };

            mockAccessor.Setup(x => x.HttpContext).Returns(context);
            return mockAccessor;
        }
    }
}