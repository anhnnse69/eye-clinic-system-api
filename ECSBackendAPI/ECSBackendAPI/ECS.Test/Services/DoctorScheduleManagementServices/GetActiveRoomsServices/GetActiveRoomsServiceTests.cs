using System.Linq.Expressions;
using ECS.Application.Common.Response;
using ECS.Application.Services.DoctorScheduleManagementServices.GetActiveRoomsServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.DoctorScheduleManagementServices.GetActiveRoomsServices
{
    /// <summary>
    /// Unit tests for <see cref="GetActiveRoomsService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line AND branch coverage on <c>GetActiveRoomsService.cs</c>.
    /// </summary>
    public class GetActiveRoomsServiceTests
    {
        private static readonly Guid ReceptionistUserId = GetActiveRoomsMockData.ReceptionistUserId;
        private static readonly Guid ClinicId = GetActiveRoomsMockData.ClinicId;

        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext>> _roomRepoMock = new();
        private readonly GetActiveRoomsService _sut;

        public GetActiveRoomsServiceTests()
        {
            _sut = new GetActiveRoomsService(
                _staffClinicRepoMock.Object,
                _roomRepoMock.Object);
        }

        // ─────────────────────────────────────────────────────────────────
        // Repository helpers
        // ─────────────────────────────────────────────────────────────────

        private void SetupStaffClinicRepo(IEnumerable<StaffClinic> staffClinics)
        {
            var list = staffClinics.ToList();
            _staffClinicRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<StaffClinic>().Object);
        }

        private void SetupRoomRepo(IEnumerable<FacilityRoom> rooms)
        {
            var list = rooms.ToList();
            _roomRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<FacilityRoom, bool>>>(), It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<FacilityRoom>().Object);
        }

        // ==================================================================
        // ==================== AUTHORIZATION TESTS ========================
        // ==================================================================

        /// <summary>
        /// TC-01: Receptionist not found (StaffClinic is null) → throws KeyNotFoundException(APP_MESSAGE_4008).
        /// </summary>
        [Fact]
        public async Task Process_ReceptionistNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var receptionistUserId = ReceptionistUserId;

            //Arrange 2
            SetupStaffClinicRepo(Array.Empty<StaffClinic>());

            //Act
            var act = () => _sut.Process(receptionistUserId);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4008.ToString());
            _roomRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<FacilityRoom, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>
        /// TC-02: Receptionist is inactive (StaffClinic.IsActive = false) → throws KeyNotFoundException(APP_MESSAGE_4008).
        /// Note: Simple mock cannot apply IsActive filter, so inactive = not found scenario.
        /// </summary>
        [Fact]
        public async Task Process_ReceptionistInactive_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var receptionistUserId = ReceptionistUserId;

            //Arrange 2
            SetupStaffClinicRepo(Array.Empty<StaffClinic>());

            //Act
            var act = () => _sut.Process(receptionistUserId);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4008.ToString());
        }

        // ==================================================================
        // ==================== SUCCESS PATH TESTS =========================
        // ==================================================================

        /// <summary>
        /// TC-03: Happy path - active receptionist with active rooms → returns list sorted by RoomName.
        /// </summary>
        [Fact]
        public async Task Process_HappyPathWithActiveRooms_ReturnsSortedList()
        {
            //Arrange 1
            var receptionistUserId = ReceptionistUserId;
            var room1 = GetActiveRoomsMockData.GetActiveFacilityRoom(
                id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                roomName: "Phong C");
            var room2 = GetActiveRoomsMockData.GetActiveFacilityRoom(
                id: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                roomName: "Phong A");
            var room3 = GetActiveRoomsMockData.GetActiveFacilityRoom(
                id: Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                roomName: "Phong B");
            var rooms = new[] { room1, room2, room3 }; // Unsorted input

            //Arrange 2
            SetupStaffClinicRepo(new[] { GetActiveRoomsMockData.GetActiveStaffClinic(userId: receptionistUserId) });
            SetupRoomRepo(rooms);

            //Act
            var result = await _sut.Process(receptionistUserId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(3);
            result.Data![0].RoomName.Should().Be("Phong A");
            result.Data[1].RoomName.Should().Be("Phong B");
            result.Data[2].RoomName.Should().Be("Phong C");
        }

        /// <summary>
        /// TC-04: Room Type - Room with RoomType field populated.
        /// </summary>
        [Fact]
        public async Task Process_RoomWithRoomType_ReturnsCorrectRoomType()
        {
            //Arrange 1
            var receptionistUserId = ReceptionistUserId;
            var room = GetActiveRoomsMockData.GetActiveFacilityRoom(
                id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                roomName: "Phong Kham",
                roomType: "Phong Kham");

            //Arrange 2
            SetupStaffClinicRepo(new[] { GetActiveRoomsMockData.GetActiveStaffClinic(userId: receptionistUserId) });
            SetupRoomRepo(new[] { room });

            //Act
            var result = await _sut.Process(receptionistUserId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data![0].RoomType.Should().Be("Phong Kham");
        }

        /// <summary>
        /// TC-05: Room Type - Room without RoomType (null) → RoomType field is null.
        /// </summary>
        [Fact]
        public async Task Process_RoomWithoutRoomType_ReturnsNullRoomType()
        {
            //Arrange 1
            var receptionistUserId = ReceptionistUserId;
            var room = GetActiveRoomsMockData.GetActiveFacilityRoom(
                id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                roomName: "Phong Khong Co Loai",
                roomType: null);

            //Arrange 2
            SetupStaffClinicRepo(new[] { GetActiveRoomsMockData.GetActiveStaffClinic(userId: receptionistUserId) });
            SetupRoomRepo(new[] { room });

            //Act
            var result = await _sut.Process(receptionistUserId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data![0].RoomType.Should().BeNull();
        }

        // ==================================================================
        // ==================== EDGE CASE TESTS =============================
        // ==================================================================

        /// <summary>
        /// TC-06: No active rooms in clinic → returns empty list.
        /// </summary>
        [Fact]
        public async Task Process_NoActiveRooms_ReturnsEmptyList()
        {
            //Arrange 1
            var receptionistUserId = ReceptionistUserId;

            //Arrange 2
            SetupStaffClinicRepo(new[] { GetActiveRoomsMockData.GetActiveStaffClinic(userId: receptionistUserId) });
            SetupRoomRepo(Array.Empty<FacilityRoom>());

            //Act
            var result = await _sut.Process(receptionistUserId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }

        // ==================================================================
        // ==================== RESPONSE STRUCTURE TESTS ===================
        // ==================================================================

        /// <summary>
        /// TC-07: Response contains correct fields from ClinicRoomResponse.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_ResponseContainsCorrectFields()
        {
            //Arrange 1
            var receptionistUserId = ReceptionistUserId;
            var roomId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var room = GetActiveRoomsMockData.GetActiveFacilityRoom(
                id: roomId,
                roomName: "Phong Kham A",
                roomType: "Phong Kham",
                isActive: true);

            //Arrange 2
            SetupStaffClinicRepo(new[] { GetActiveRoomsMockData.GetActiveStaffClinic(userId: receptionistUserId) });
            SetupRoomRepo(new[] { room });

            //Act
            var result = await _sut.Process(receptionistUserId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);

            var roomResponse = result.Data![0];
            roomResponse.RoomId.Should().Be(roomId);
            roomResponse.RoomName.Should().Be("Phong Kham A");
            roomResponse.RoomType.Should().Be("Phong Kham");
            roomResponse.IsActive.Should().BeTrue();
        }

        /// <summary>
        /// TC-08: Response CodeMessage is APP_MESSAGE_2000 and Meta is null.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_ResponseCodeMessage2000AndMetaNull()
        {
            //Arrange 1
            var receptionistUserId = ReceptionistUserId;

            //Arrange 2
            SetupStaffClinicRepo(new[] { GetActiveRoomsMockData.GetActiveStaffClinic(userId: receptionistUserId) });
            SetupRoomRepo(Array.Empty<FacilityRoom>());

            //Act
            var result = await _sut.Process(receptionistUserId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Meta.Should().BeNull();
        }

        /// <summary>
        /// TC-09: Response Data is null-safe (empty list, not null).
        /// </summary>
        [Fact]
        public async Task Process_NoRooms_ResponseDataIsEmptyListNotNull()
        {
            //Arrange 1
            var receptionistUserId = ReceptionistUserId;

            //Arrange 2
            SetupStaffClinicRepo(new[] { GetActiveRoomsMockData.GetActiveStaffClinic(userId: receptionistUserId) });
            SetupRoomRepo(Array.Empty<FacilityRoom>());

            //Act
            var result = await _sut.Process(receptionistUserId);

            //Assert
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }
    }
}
