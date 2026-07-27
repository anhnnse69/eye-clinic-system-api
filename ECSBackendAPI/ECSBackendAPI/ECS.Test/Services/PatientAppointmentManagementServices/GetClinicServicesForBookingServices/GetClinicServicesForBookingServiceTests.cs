using System.Linq.Expressions;
using ECS.Application.Common.Response;
using ECS.Application.Services.PatientAppointmentManagementServices.GetClinicServicesForBookingServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace ECS.Test.Services.PatientAppointmentManagementServices.GetClinicServicesForBookingServices
{
    public class GetClinicServicesForBookingServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<Clinic, Guid, AppDbContext>> _clinicRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<Service, Guid, AppDbContext>> _serviceRepoMock = new();

        private GetClinicServicesForBookingService CreateSut()
        {
            return new GetClinicServicesForBookingService(
                _clinicRepoMock.Object,
                _serviceRepoMock.Object);
        }

        private void SetupClinicRepository(IEnumerable<Clinic> clinics)
        {
            _clinicRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<Clinic, bool>>>(), It.IsAny<bool>()))
                .Returns((Expression<Func<Clinic, bool>> expression, bool trackChanges) =>
                {
                    var filteredList = clinics.AsQueryable().Where(expression).ToList();
                    return filteredList.BuildMockDbSet().Object;
                });
        }

        private void SetupServiceRepository(IEnumerable<Service> services)
        {
            _serviceRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<Service, bool>>>(), It.IsAny<bool>()))
                .Returns((Expression<Func<Service, bool>> expression, bool trackChanges) =>
                {
                    var filteredList = services.AsQueryable().Where(expression).ToList();
                    return filteredList.BuildMockDbSet().Object;
                });
        }

        [Fact]
        public async Task Process_InactiveOrMissingClinic_ThrowsKeyNotFoundException()
        {
            // Arrange: Clinic không tồn tại (hoặc IsActive = false)
            SetupClinicRepository(Enumerable.Empty<Clinic>());
            SetupServiceRepository(Enumerable.Empty<Service>());
            var sut = CreateSut();

            // Act
            Func<Task> act = async () => await sut.Process(GetClinicServicesForBookingMockData.ValidClinicId);

            // Assert: ValidateClinicProfile trả về false -> Phủ nhánh !isClinicValid ở BuildFilterExpression, ExecuteServicesQuery & CreateResponse
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4045.ToString());
        }

        [Fact]
        public async Task Process_ValidClinicWithNoServices_ReturnsSuccessWithEmptyList()
        {
            // Arrange: Clinic hợp lệ nhưng chưa có dịch vụ nào
            var clinic = GetClinicServicesForBookingMockData.GetClinic();
            SetupClinicRepository(new[] { clinic });
            SetupServiceRepository(Enumerable.Empty<Service>());
            var sut = CreateSut();

            // Act
            var result = await sut.Process(clinic.Id);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task Process_ValidClinicWithServices_ReturnsMappedAndOrderedServiceOptions()
        {
            // Arrange: Clinic hợp lệ và có danh sách dịch vụ chưa sắp xếp
            var clinic = GetClinicServicesForBookingMockData.GetClinic();
            var services = new[]
            {
                GetClinicServicesForBookingMockData.GetService(
                    id: GetClinicServicesForBookingMockData.ValidServiceId1,
                    clinicId: clinic.Id,
                    serviceName: "Tầm soát cườm khô",
                    price: 500000m,
                    durationMinutes: 45),
                GetClinicServicesForBookingMockData.GetService(
                    id: GetClinicServicesForBookingMockData.ValidServiceId2,
                    clinicId: clinic.Id,
                    serviceName: "Đo khúc xạ mắt",
                    price: 150000m,
                    durationMinutes: 20)
            };

            SetupClinicRepository(new[] { clinic });
            SetupServiceRepository(services);
            var sut = CreateSut();

            // Act
            var result = await sut.Process(clinic.Id);

            // Assert: Phủ MapToResponseDto và việc sắp xếp OrderBy(s => s.ServiceName)
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);

            // Kiểm tra dịch vụ 1 sau khi OrderBy (Chữ "Đ" đứng trước chữ "T")
            result.Data![0].Id_service.Should().Be(GetClinicServicesForBookingMockData.ValidServiceId2);
            result.Data[0].ServiceName.Should().Be("Đo khúc xạ mắt");
            result.Data[0].Price.Should().Be(150000m);
            result.Data[0].DurationMinutes.Should().Be(20);

            // Kiểm tra dịch vụ 2
            result.Data[1].Id_service.Should().Be(GetClinicServicesForBookingMockData.ValidServiceId1);
            result.Data[1].ServiceName.Should().Be("Tầm soát cườm khô");
            result.Data[1].Price.Should().Be(500000m);
            result.Data[1].DurationMinutes.Should().Be(45);
        }
    }
}