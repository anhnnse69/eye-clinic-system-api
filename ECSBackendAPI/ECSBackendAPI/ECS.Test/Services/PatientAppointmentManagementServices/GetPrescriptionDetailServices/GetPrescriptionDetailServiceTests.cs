using ECS.Application.Common.Response;
using ECS.Application.Services.PatientAppointmentManagementServices.GetPrescriptionDetailServices;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Linq.Expressions;
using Xunit;

using MockQueryable.Moq;

namespace ECS.Test.Services.PatientAppointmentManagementServices.GetPrescriptionDetailServices
{
    public class GetPrescriptionDetailServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<Appointment, Guid, AppDbContext>> _appointmentRepositoryMock = new();

        private AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private GetPrescriptionDetailService CreateSut(IHttpContextAccessor httpContextAccessor, AppDbContext context)
        {
            return new GetPrescriptionDetailService(
                _appointmentRepositoryMock.Object,
                context,
                httpContextAccessor,
                mongo: null);
        }

        private void SetupAppointmentRepository(IEnumerable<Appointment> appointments)
        {
            _appointmentRepositoryMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<bool>()))
                .Returns((Expression<Func<Appointment, bool>> expression, bool trackChanges) =>
                {
                    var filtered = appointments.AsQueryable().Where(expression).ToList();
                    return filtered.BuildMockDbSet<Appointment>().Object;
                });
        }

        [Fact]
        public async Task Process_InvalidUserClaims_ReturnsFail4001()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var httpContextAccessorMock = GetPrescriptionDetailMockData.GetHttpContextAccessorMock(isAuthenticated: false);
            SetupAppointmentRepository(Enumerable.Empty<Appointment>());
            var sut = CreateSut(httpContextAccessorMock.Object, context);
            var request = GetPrescriptionDetailMockData.GetValidRequest();

            // Act
            var response = await sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            response.Data.Should().BeNull();
        }

        [Fact]
        public async Task Process_AppointmentNotFound_ReturnsFail4046()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var httpContextAccessorMock = GetPrescriptionDetailMockData.GetHttpContextAccessorMock(GetPrescriptionDetailMockData.ValidUserId);
            SetupAppointmentRepository(Enumerable.Empty<Appointment>());
            var sut = CreateSut(httpContextAccessorMock.Object, context);
            var request = GetPrescriptionDetailMockData.GetValidRequest();

            // Act
            var response = await sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4046.ToString());
            response.Data.Should().BeNull();
        }

        [Fact]
        public async Task Process_UnauthorizedAccess_ReturnsFail4053()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var httpContextAccessorMock = GetPrescriptionDetailMockData.GetHttpContextAccessorMock(GetPrescriptionDetailMockData.OtherUserId);
            var appointment = GetPrescriptionDetailMockData.GetAppointmentWithDetails();
            SetupAppointmentRepository(new[] { appointment });
            var sut = CreateSut(httpContextAccessorMock.Object, context);
            var request = GetPrescriptionDetailMockData.GetValidRequest();

            // Act
            var response = await sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4053.ToString());
            response.Data.Should().BeNull();
        }

        [Fact]
        public async Task Process_ValidAccess_ReturnsSuccess2000()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            GetPrescriptionDetailMockData.SeedUserAccess(context, GetPrescriptionDetailMockData.ValidUserId, GetPrescriptionDetailMockData.ValidPatientId);
            var httpContextAccessorMock = GetPrescriptionDetailMockData.GetHttpContextAccessorMock(GetPrescriptionDetailMockData.ValidUserId);
            var appointment = GetPrescriptionDetailMockData.GetAppointmentWithDetails();
            SetupAppointmentRepository(new[] { appointment });
            var sut = CreateSut(httpContextAccessorMock.Object, context);
            var request = GetPrescriptionDetailMockData.GetValidRequest();

            // Act
            var response = await sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            response.Data.Should().NotBeNull();
            response.Data!.AppointmentId.Should().Be(appointment.Id.ToString());
            response.Data.PatientName.Should().Be(appointment.Patient.FullName);
            response.Data.ClinicName.Should().Be(appointment.Doctor.Clinic.Name);
        }
    }
}
