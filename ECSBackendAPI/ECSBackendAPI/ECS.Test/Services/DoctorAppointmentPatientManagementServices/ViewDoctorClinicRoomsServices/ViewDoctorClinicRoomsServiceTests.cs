using System.Linq.Expressions;
using ECS.Application.Common.Response;
using ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewDoctorClinicRoomsServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.DoctorAppointmentPatientManagementServices.ViewDoctorClinicRoomsServices
{
    public class ViewDoctorClinicRoomsServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepoMock;
        private readonly Mock<IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext>> _roomRepoMock;
        private readonly ViewDoctorClinicRoomsService _service;

        public ViewDoctorClinicRoomsServiceTests()
        {
            _doctorRepoMock = new Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>>();
            _roomRepoMock = new Mock<IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext>>();
            _service = new ViewDoctorClinicRoomsService(_doctorRepoMock.Object, _roomRepoMock.Object);
        }

        private void SetupDoctorProfileRepo(DoctorProfile? profile)
        {
            var list = profile != null ? new List<DoctorProfile> { profile } : new List<DoctorProfile>();
            var mockQueryable = list.BuildMockDbSet();
            _doctorRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<DoctorProfile, bool>>>(), It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        private void SetupFacilityRoomRepo(IEnumerable<FacilityRoom> rooms)
        {
            var mockQueryable = rooms.ToList().BuildMockDbSet();
            _roomRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<FacilityRoom, bool>>>(), It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        [Fact]
        public async Task Process_DoctorProfileNotFoundOrInactive_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var userId = Guid.NewGuid();

            //Arrange 2
            SetupDoctorProfileRepo(null);

            //Act
            Func<Task> act = async () => await _service.Process(userId);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4008.ToString());
            _doctorRepoMock.Verify(r => r.FindByCondition(It.IsAny<Expression<Func<DoctorProfile, bool>>>(), It.IsAny<bool>()), Times.Once);
            _roomRepoMock.Verify(r => r.FindByCondition(It.IsAny<Expression<Func<FacilityRoom, bool>>>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Process_DoctorValidWithNoActiveRooms_ReturnsSuccessWithEmptyList()
        {
            //Arrange 1
            var doctor = ViewDoctorClinicRoomsMockData.GetDoctorProfile();

            //Arrange 2
            SetupDoctorProfileRepo(doctor);
            SetupFacilityRoomRepo(Enumerable.Empty<FacilityRoom>());

            //Act
            var result = await _service.Process(doctor.UserId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
            _doctorRepoMock.Verify(r => r.FindByCondition(It.IsAny<Expression<Func<DoctorProfile, bool>>>(), It.IsAny<bool>()), Times.Once);
            _roomRepoMock.Verify(r => r.FindByCondition(It.IsAny<Expression<Func<FacilityRoom, bool>>>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task Process_DoctorValidWithActiveRoomsAndNonNullRoomType_ReturnsMappedRoomsOrderedByName()
        {
            //Arrange 1
            var doctor = ViewDoctorClinicRoomsMockData.GetDoctorProfile();
            var room1 = ViewDoctorClinicRoomsMockData.GetFacilityRoom(
                clinicId: doctor.ClinicId, roomName: "Phong 102", roomType: "Kham Ngoai");
            var room2 = ViewDoctorClinicRoomsMockData.GetFacilityRoom(
                clinicId: doctor.ClinicId, roomName: "Phong 101", roomType: "Kham Noi");

            //Arrange 2
            SetupDoctorProfileRepo(doctor);
            SetupFacilityRoomRepo(new[] { room1, room2 });

            //Act
            var result = await _service.Process(doctor.UserId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);

            result.Data![0].RoomId.Should().Be(room2.Id);
            result.Data[0].RoomName.Should().Be("Phong 101");
            result.Data[0].RoomType.Should().Be("Kham Noi");
            result.Data[0].IsActive.Should().BeTrue();

            result.Data[1].RoomId.Should().Be(room1.Id);
            result.Data[1].RoomName.Should().Be("Phong 102");
            result.Data[1].RoomType.Should().Be("Kham Ngoai");
            result.Data[1].IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task Process_DoctorValidWithNullOrEmptyRoomType_DefaultsRoomTypeToGeneral()
        {
            //Arrange 1
            var doctor = ViewDoctorClinicRoomsMockData.GetDoctorProfile();
            var roomNullType = ViewDoctorClinicRoomsMockData.GetFacilityRoom(
                clinicId: doctor.ClinicId, roomName: "Phong A", roomType: null);
            var roomEmptyType = ViewDoctorClinicRoomsMockData.GetFacilityRoom(
                clinicId: doctor.ClinicId, roomName: "Phong B", roomType: "");
            var roomWhitespaceType = ViewDoctorClinicRoomsMockData.GetFacilityRoom(
                clinicId: doctor.ClinicId, roomName: "Phong C", roomType: "   ");

            //Arrange 2
            SetupDoctorProfileRepo(doctor);
            SetupFacilityRoomRepo(new[] { roomNullType, roomEmptyType, roomWhitespaceType });

            //Act
            var result = await _service.Process(doctor.UserId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(3);
            result.Data![0].RoomType.Should().Be("General");
            result.Data[1].RoomType.Should().Be("General");
            result.Data[2].RoomType.Should().Be("General");
        }
    }
}
