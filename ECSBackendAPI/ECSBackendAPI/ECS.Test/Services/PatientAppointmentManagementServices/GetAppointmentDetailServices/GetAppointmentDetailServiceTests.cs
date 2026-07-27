using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Application.Services.PatientAppointmentManagementServices.GetAppointmentDetailServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Feedbacks;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.PatientAppointmentManagementServices.GetAppointmentDetailServices
{
    public class GetAppointmentDetailServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<Appointment, Guid, AppDbContext>> _appointmentRepoMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();

        private GetAppointmentDetailService CreateSut(AppDbContext? context = null)
        {
            context ??= CreateContext();
            return new GetAppointmentDetailService(
                _appointmentRepoMock.Object,
                context,
                _httpContextAccessorMock.Object);
        }

        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private void SetupHttpContext(string? userIdString)
        {
            if (string.IsNullOrWhiteSpace(userIdString))
            {
                _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext)null!);
                return;
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userIdString)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };

            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        }

        private void SetupAppointmentRepository(IEnumerable<Appointment> appointments)
        {
            _appointmentRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<bool>()))
                .Returns((Expression<Func<Appointment, bool>> expression, bool trackChanges) =>
                {
                    var filteredList = appointments.AsQueryable().Where(expression).ToList();
                    return filteredList.BuildMockDbSet<Appointment>().Object;
                });
        }

        private object? InvokePrivate(GetAppointmentDetailService sut, string methodName, params object[] args)
        {
            var method = typeof(GetAppointmentDetailService).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Should().NotBeNull();
            return method!.Invoke(sut, args);
        }

        [Fact]
        public async Task Process_InvalidUserClaim_ThrowsNullReferenceException()
        {
            // Arrange
            var context = CreateContext();
            var sut = CreateSut(context);
            SetupHttpContext("not-a-guid");
            SetupAppointmentRepository(new[] { GetAppointmentDetailMockData.GetAppointment(id: Guid.NewGuid()) });

            // Act
            Func<Task> act = async () => await sut.Process(new GetAppointmentDetailRequest { AppointmentId = Guid.NewGuid() });

            // Assert
            await act.Should().ThrowAsync<NullReferenceException>();
        }

        [Fact]
        public async Task Process_AppointmentNotFound_ThrowsNullReferenceException()
        {
            // Arrange
            var context = CreateContext();
            var sut = CreateSut(context);
            var userId = Guid.NewGuid();
            SetupHttpContext(userId.ToString());
            SetupAppointmentRepository(Array.Empty<Appointment>());

            // Act
            Func<Task> act = async () => await sut.Process(new GetAppointmentDetailRequest { AppointmentId = Guid.NewGuid() });

            // Assert
            await act.Should().ThrowAsync<NullReferenceException>();
        }

        [Fact]
        public async Task Process_UserHasNoPermission_ReturnsFail4053()
        {
            // Arrange
            var context = CreateContext();
            var sut = CreateSut(context);
            var userId = Guid.NewGuid();
            SetupHttpContext(userId.ToString());

            var appointment = GetAppointmentDetailMockData.GetAppointment(
                id: Guid.NewGuid(),
                patientId: Guid.NewGuid(),
                doctorId: Guid.NewGuid(),
                slotId: Guid.NewGuid(),
                createdById: Guid.NewGuid(),
                appointmentDate: DateTime.Today,
                createdAt: DateTime.UtcNow);

            SetupAppointmentRepository(new[] { appointment });

            // Act
            var result = await sut.Process(new GetAppointmentDetailRequest { AppointmentId = appointment.Id });

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4053.ToString());
            result.Data.Should().BeNull();
        }

        [Fact]
        public void CreateErrorResponse_ReturnsExpectedFailuresForEachBranch()
        {
            // Arrange
            var context = CreateContext();
            var sut = CreateSut(context);

            // Act
            var invalidUserResponse = InvokePrivate(sut, "CreateErrorResponse", false, true, true) as ApiResponse<GetAppointmentDetailResponse>;
            var missingAppointmentResponse = InvokePrivate(sut, "CreateErrorResponse", true, false, true) as ApiResponse<GetAppointmentDetailResponse>;
            var permissionResponse = InvokePrivate(sut, "CreateErrorResponse", true, true, false) as ApiResponse<GetAppointmentDetailResponse>;
            var successResponse = InvokePrivate(sut, "CreateErrorResponse", true, true, true) as ApiResponse<GetAppointmentDetailResponse>;

            // Assert
            invalidUserResponse.Should().NotBeNull();
            invalidUserResponse!.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());

            missingAppointmentResponse.Should().NotBeNull();
            missingAppointmentResponse!.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4046.ToString());

            permissionResponse.Should().NotBeNull();
            permissionResponse!.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4053.ToString());

            successResponse.Should().BeNull();
        }

        [Fact]
        public void CreateResponse_ReturnsSuccessOrErrorResponseBasedOnFlags()
        {
            // Arrange
            var context = CreateContext();
            var sut = CreateSut(context);
            var result = new GetAppointmentDetailResponse();

            // Act
            var successResponse = InvokePrivate(sut, "CreateResponse", result, true, true, true) as ApiResponse<GetAppointmentDetailResponse>;
            var errorResponse = InvokePrivate(sut, "CreateResponse", result, false, true, true) as ApiResponse<GetAppointmentDetailResponse>;

            // Assert
            successResponse.Should().NotBeNull();
            successResponse!.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            successResponse.Data.Should().BeSameAs(result);

            errorResponse.Should().NotBeNull();
            errorResponse!.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
        }

        [Fact]
        public async Task Process_ValidRequestWithFeedback_ReturnsSuccessWithMappedData()
        {
            // Arrange
            var context = CreateContext();
            var sut = CreateSut(context);
            var userId = GetAppointmentDetailMockData.ValidUserId;
            var patientId = GetAppointmentDetailMockData.ValidPatientId;
            var appointmentId = GetAppointmentDetailMockData.ValidAppointmentId;
            var doctorId = GetAppointmentDetailMockData.ValidDoctorId;
            var slotId = GetAppointmentDetailMockData.ValidSlotId;
            var serviceId = GetAppointmentDetailMockData.ValidServiceId;
            SetupHttpContext(userId.ToString());
            GetAppointmentDetailMockData.SeedUserAccess(context, userId, patientId);

            var appointment = GetAppointmentDetailMockData.GetAppointmentWithDetails(
                id: appointmentId,
                patientId: patientId,
                doctorId: doctorId,
                slotId: slotId,
                serviceId: serviceId,
                createdById: userId,
                appointmentDate: new DateTime(2025, 1, 1),
                createdAt: new DateTime(2025, 1, 1, 7, 0, 0),
                feedback: GetAppointmentDetailMockData.GetFeedback());

            SetupAppointmentRepository(new[] { appointment });

            // Act
            var result = await sut.Process(new GetAppointmentDetailRequest { AppointmentId = appointmentId });

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.PatientName.Should().Be("Nguyễn Văn A");
            result.Data.DoctorName.Should().Be("BS Dr. Hoa");
            result.Data.TimeSlot.Should().Be("10:00 - 11:00");
            result.Data.Feedback.Should().NotBeNull();
            result.Data.Feedback!.Comment.Should().Be("Rất tốt");
            result.Data.ServiceName.Should().Be("Khám mắt tổng quát");
        }

        [Fact]
        public async Task Process_ValidRequestWithoutOptionalData_ReturnsSuccessWithDefaultValues()
        {
            // Arrange
            var context = CreateContext();
            var sut = CreateSut(context);
            var userId = Guid.NewGuid();
            var appointmentId = Guid.NewGuid();
            SetupHttpContext(userId.ToString());

            var appointment = GetAppointmentDetailMockData.GetAppointment(
                id: appointmentId,
                patientId: userId,
                doctorId: Guid.NewGuid(),
                slotId: Guid.NewGuid(),
                appointmentDate: new DateTime(2025, 2, 1),
                createdAt: new DateTime(2025, 1, 1, 8, 0, 0));
            appointment.Symptoms = null;
            appointment.NoteReason = null;
            appointment.Patient = null!;
            appointment.Doctor = null!;
            appointment.Slot = null!;
            appointment.Service = null;
            appointment.Feedback = null;
            appointment.CreatedById = null;

            SetupAppointmentRepository(new[] { appointment });

            // Act
            var result = await sut.Process(new GetAppointmentDetailRequest { AppointmentId = appointmentId });

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.PatientName.Should().Be("N/A");
            result.Data.DoctorName.Should().Be("N/A");
            result.Data.TimeSlot.Should().Be("N/A");
            result.Data.ServiceName.Should().Be("Khám mắt tổng quát");
            result.Data.ClinicName.Should().Be("N/A");
            result.Data.Feedback.Should().BeNull();
        }
    }
}
