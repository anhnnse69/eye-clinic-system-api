using System.Linq.Expressions;
using ECS.Application.Common.Response;
using ECS.Application.Services.PatientAppointmentManagementServices.GetAppointmentHistoryServices;
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

namespace ECS.Test.Services.PatientAppointmentManagementServices.GetAppointmentHistoryServices
{
    public class GetAppointmentHistoryServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<Appointment, Guid, AppDbContext>> _appointmentRepoMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();

        private GetAppointmentHistoryService CreateSut(AppDbContext? context = null)
        {
            context ??= CreateContext();
            return new GetAppointmentHistoryService(
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

        [Fact]
        public async Task Process_InvalidUserClaim_ReturnsFail4001()
        {
            // Arrange
            var sut = CreateSut();
            var httpContextAccessor = GetAppointmentHistoryMockData.GetHttpContextAccessorMockWithClaim("not-a-guid");
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextAccessor.Object.HttpContext);
            SetupAppointmentRepository(new[] { GetAppointmentHistoryMockData.GetAppointment() });

            // Act
            var result = await sut.Process(GetAppointmentHistoryMockData.GetValidRequest());

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task Process_NoAccessibleProfiles_ReturnsSuccessWithEmptyList()
        {
            // Arrange
            var context = CreateContext();
            var sut = CreateSut(context);
            var httpContextAccessor = GetAppointmentHistoryMockData.GetHttpContextAccessorMock(GetAppointmentHistoryMockData.ValidUserId);
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextAccessor.Object.HttpContext);
            SetupAppointmentRepository(Array.Empty<Appointment>());

            // Act
            var result = await sut.Process(GetAppointmentHistoryMockData.GetValidRequest());

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
            result.Meta.Should().NotBeNull();
            result.Meta!.Total.Should().Be(0);
        }

        [Fact]
        public async Task Process_SearchTermAndStatusFilter_ReturnsFilteredAppointments()
        {
            // Arrange
            var context = CreateContext();
            GetAppointmentHistoryMockData.SeedUserAccess(context, GetAppointmentHistoryMockData.ValidUserId, GetAppointmentHistoryMockData.ValidPatientId);
            var sut = CreateSut(context);
            var httpContextAccessor = GetAppointmentHistoryMockData.GetHttpContextAccessorMock(GetAppointmentHistoryMockData.ValidUserId);
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextAccessor.Object.HttpContext);

            var appointment = GetAppointmentHistoryMockData.GetAppointment(
                id: Guid.NewGuid(),
                patientId: GetAppointmentHistoryMockData.ValidPatientId,
                status: AppointmentStatus.CONFIRMED,
                appointmentDate: new DateTime(2025, 2, 1));

            SetupAppointmentRepository(new[] { appointment });

            // Act
            var result = await sut.Process(new GetAppointmentHistoryRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SearchTerm = "phòng khám",
                Status = "CONFIRMED"
            });

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().HaveCount(1);
            result.Data![0].ClinicName.Should().Be("Phòng khám mắt");
            result.Data[0].Status.Should().Be(AppointmentStatus.CONFIRMED.ToString());
            result.Data[0].HasFeedback.Should().BeTrue();
        }

        [Fact]
        public async Task Process_UsesCreatedByIdWhenPatientIdNotAccessible_ReturnsAppointment()
        {
            // Arrange
            var context = CreateContext();
            GetAppointmentHistoryMockData.SeedUserAccess(context, GetAppointmentHistoryMockData.ValidUserId, GetAppointmentHistoryMockData.ValidPatientId);
            var sut = CreateSut(context);
            var httpContextAccessor = GetAppointmentHistoryMockData.GetHttpContextAccessorMock(GetAppointmentHistoryMockData.ValidUserId);
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextAccessor.Object.HttpContext);

            var appointment = GetAppointmentHistoryMockData.GetAppointment(
                id: Guid.NewGuid(),
                patientId: Guid.NewGuid(),
                createdById: GetAppointmentHistoryMockData.ValidUserId,
                appointmentDate: new DateTime(2025, 3, 1));

            SetupAppointmentRepository(new[] { appointment });

            // Act
            var result = await sut.Process(GetAppointmentHistoryMockData.GetValidRequest());

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().HaveCount(1);
            result.Data![0].Id_appointment.Should().Be(appointment.Id.ToString());
        }

        [Fact]
        public async Task Process_MapsNullOptionalRelationsToDefaults()
        {
            // Arrange
            var context = CreateContext();
            GetAppointmentHistoryMockData.SeedUserAccess(context, GetAppointmentHistoryMockData.ValidUserId, GetAppointmentHistoryMockData.ValidPatientId);
            var sut = CreateSut(context);
            var httpContextAccessor = GetAppointmentHistoryMockData.GetHttpContextAccessorMock(GetAppointmentHistoryMockData.ValidUserId);
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextAccessor.Object.HttpContext);

            var appointment = GetAppointmentHistoryMockData.GetAppointment(includeRelations: false);
            appointment.Patient = null!;
            appointment.Doctor = null!;
            appointment.Slot = GetAppointmentHistoryMockData.GetTimeSlot();
            appointment.Service = null;
            appointment.Feedback = null;

            SetupAppointmentRepository(new[] { appointment });

            // Act
            var result = await sut.Process(GetAppointmentHistoryMockData.GetValidRequest());

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().HaveCount(1);
            result.Data![0].TimeSlot.Should().Be("10:00 - 11:00");
            result.Data[0].DoctorName.Should().Be("N/A");
            result.Data[0].ClinicName.Should().Be("N/A");
            result.Data[0].ServiceName.Should().Be("Khám mắt tổng quát");
            result.Data[0].ServicePrice.Should().Be("Miễn phí");
            result.Data[0].HasFeedback.Should().BeFalse();
        }

        [Fact]
        public async Task Process_PaginatesAndUsesMetaResponse()
        {
            // Arrange
            var context = CreateContext();
            GetAppointmentHistoryMockData.SeedUserAccess(context, GetAppointmentHistoryMockData.ValidUserId, GetAppointmentHistoryMockData.ValidPatientId);
            var sut = CreateSut(context);
            var httpContextAccessor = GetAppointmentHistoryMockData.GetHttpContextAccessorMock(GetAppointmentHistoryMockData.ValidUserId);
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextAccessor.Object.HttpContext);

            var appointments = Enumerable.Range(1, 3)
                .Select(i => GetAppointmentHistoryMockData.GetAppointment(
                    id: Guid.NewGuid(),
                    patientId: GetAppointmentHistoryMockData.ValidPatientId,
                    appointmentDate: new DateTime(2025, 1, 20).AddDays(i)))
                .ToList();

            SetupAppointmentRepository(appointments);

            // Act
            var result = await sut.Process(new GetAppointmentHistoryRequest { PageNumber = 1, PageSize = 2 });

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().HaveCount(2);
            result.Meta.Should().NotBeNull();
            result.Meta!.Total.Should().Be(3);
            result.Meta.Page.Should().Be(1);
            result.Meta.Size.Should().Be(2);
        }
    }
}
