using ECS.Application.Services.ClinicAdminManagementServices.ClinicDeleteRoomServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using MockQueryable.Moq;
using Moq;
using System.Linq.Expressions;
using System.Security.Claims;

namespace ECS.Test.Services.ClinicAdminManagementServices.ClinicDeleteRoomServices
{
    /// <summary>
    /// Unit tests for <see cref="DeleteRoomService"/>.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// Goal: 100% line and ≥90% branch coverage on DeleteRoomService.cs.
    /// </summary>
    public class DeleteRoomServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<FacilityRoom, Guid, AppDbContext>> _roomRepoMock;
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly DeleteRoomService _sut;

        public DeleteRoomServiceTests()
        {
            _roomRepoMock = new Mock<IRepositoryBaseAsync<FacilityRoom, Guid, AppDbContext>>();
            _staffClinicRepoMock = new Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _sut = new DeleteRoomService(
                _roomRepoMock.Object,
                _staffClinicRepoMock.Object,
                _httpContextAccessorMock.Object);
        }

        // ── HttpContext helpers ────────────────────────────────────────────────

        private void SetupHttpContextClaim(string? rawClaimValue)
        {
            var httpContext = new DefaultHttpContext();
            if (rawClaimValue != null)
            {
                var claims = new[] { new Claim(ClaimTypes.NameIdentifier, rawClaimValue) };
                var identity = new ClaimsIdentity(claims, "TestAuth");
                httpContext.User = new ClaimsPrincipal(identity);
            }
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
        }

        private void SetupHttpContextUserId(Guid userId)
            => SetupHttpContextClaim(userId.ToString());

        // ── Repository helpers ────────────────────────────────────────────────

        private void SetupStaffClinicRepo(StaffClinic? returnStaffClinic)
        {
            var rows = returnStaffClinic != null
                ? new List<StaffClinic> { returnStaffClinic }
                : new List<StaffClinic>();
            var mockQueryable = rows.BuildMockDbSet<StaffClinic>();

            _staffClinicRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<StaffClinic, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        private void SetupFacilityRoomRepo(FacilityRoom? existingRoom)
        {
            var rows = existingRoom != null
                ? new List<FacilityRoom> { existingRoom }
                : new List<FacilityRoom>();
            var mockQueryable = rows.BuildMockDbSet<FacilityRoom>();

            _roomRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<FacilityRoom, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        // ── Test Cases ─────────────────────────────────────────────────────────

        /// <summary>
        /// TC-DR-01: HttpContext accessor returns null → the `?.User.FindFirst(...)?.Value`
        /// chain short-circuits on the very first null-conditional → userIdClaim is null →
        /// Guid.TryParse fails → isUserValid=false. StaffClinic and FacilityRoom write paths
        /// must never execute.
        /// </summary>
        [Fact]
        public async Task Process_NullHttpContext_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = DeleteRoomMockData.GetDeactivateRequest();

            //Arrange 2
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
            SetupStaffClinicRepo(null);
            SetupFacilityRoomRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _roomRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<FacilityRoom, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _roomRepoMock.Verify(r => r.UpdateAsync(It.IsAny<FacilityRoom>()), Times.Never);
            _roomRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        /// <summary>
        /// TC-DR-02: HttpContext is non-null but the principal has no NameIdentifier claim →
        /// `User.FindFirst(ClaimTypes.NameIdentifier)?.Value` returns null → Guid.TryParse
        /// fails → isUserValid=false.
        /// </summary>
        [Fact]
        public async Task Process_MissingNameIdentifierClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = DeleteRoomMockData.GetDeactivateRequest();

            //Arrange 2
            SetupHttpContextClaim(null);
            SetupStaffClinicRepo(null);
            SetupFacilityRoomRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _roomRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<FacilityRoom, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _roomRepoMock.Verify(r => r.UpdateAsync(It.IsAny<FacilityRoom>()), Times.Never);
        }

        /// <summary>
        /// TC-DR-03: Claim value is present but is not a valid Guid →
        /// Guid.TryParse returns false → isUserValid=false.
        /// </summary>
        [Fact]
        public async Task Process_InvalidUserIdClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = DeleteRoomMockData.GetDeactivateRequest();

            //Arrange 2
            SetupHttpContextClaim("not-a-guid");
            SetupStaffClinicRepo(null);
            SetupFacilityRoomRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _roomRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<FacilityRoom, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _roomRepoMock.Verify(r => r.UpdateAsync(It.IsAny<FacilityRoom>()), Times.Never);
        }

        /// <summary>
        /// TC-DR-04: Valid Guid claim but no active StaffClinic row → clinicId = null →
        /// FetchRoomData short-circuits on `!clinicId.HasValue` → originalRoom = null →
        /// isRoomExist = false → APP_MESSAGE_4020. Room repo must not be queried.
        /// </summary>
        [Fact]
        public async Task Process_ValidUserWithoutActiveClinic_Returns4020RoomNotFoundError()
        {
            //Arrange 1
            var request = DeleteRoomMockData.GetDeactivateRequest();

            //Arrange 2
            SetupHttpContextUserId(DeleteRoomMockData.TestUserId);
            SetupStaffClinicRepo(null);
            SetupFacilityRoomRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _roomRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<FacilityRoom, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _roomRepoMock.Verify(r => r.UpdateAsync(It.IsAny<FacilityRoom>()), Times.Never);
            _roomRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        /// <summary>
        /// TC-DR-05: Valid user + valid clinic, but the requested room is not present in
        /// this clinic → FetchRoomData returns null → isRoomExist = false →
        /// ApplyStatusMutation early-returns (no Update/Save) → APP_MESSAGE_4020.
        /// </summary>
        [Fact]
        public async Task Process_ActiveRoomNotFoundInClinic_Returns4020RoomNotFoundError()
        {
            //Arrange 1
            var request = DeleteRoomMockData.GetDeactivateRequest();

            //Arrange 2
            SetupHttpContextUserId(DeleteRoomMockData.TestUserId);
            SetupStaffClinicRepo(DeleteRoomMockData.GetActiveStaffClinic());
            SetupFacilityRoomRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _roomRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<FacilityRoom, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _roomRepoMock.Verify(r => r.UpdateAsync(It.IsAny<FacilityRoom>()), Times.Never);
            _roomRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        /// <summary>
        /// TC-DR-06: Happy path – valid user + clinic + existing room + request IsActive=false.
        /// Exercises full flow: ApplyStatusMutation (UpdateAsync + SaveChangesAsync),
        /// MapToResponseDto populates all 5 DTO fields, CreateResponse success branch.
        /// </summary>
        [Fact]
        public async Task Process_ActiveRoomDeactivate_ReturnsSuccessWithInactiveRoom()
        {
            //Arrange 1
            var request = DeleteRoomMockData.GetDeactivateRequest();
            var staffClinic = DeleteRoomMockData.GetActiveStaffClinic();
            var room = DeleteRoomMockData.GetFacilityRoom(isActive: true);

            //Arrange 2
            SetupHttpContextUserId(DeleteRoomMockData.TestUserId);
            SetupStaffClinicRepo(staffClinic);
            SetupFacilityRoomRepo(room);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(DeleteRoomMockData.TestRoomId);
            result.Data.ClinicId.Should().Be(DeleteRoomMockData.TestClinicId);
            result.Data.RoomName.Should().Be("Consultation Room A1");
            result.Data.RoomType.Should().Be("Consultation");
            result.Data.IsActive.Should().BeFalse();

            _roomRepoMock.Verify(
                r => r.UpdateAsync(It.Is<FacilityRoom>(x => x.Id == DeleteRoomMockData.TestRoomId && !x.IsActive)),
                Times.Once);
            _roomRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        /// <summary>
        /// TC-DR-07: Reactivation branch – valid user + clinic + existing inactive room +
        /// request IsActive=true → room.IsActive flips to true → APP_MESSAGE_2000.
        /// </summary>
        [Fact]
        public async Task Process_InactiveRoomReactivate_ReturnsSuccessWithActiveRoom()
        {
            //Arrange 1
            var request = DeleteRoomMockData.GetReactivateRequest();
            var staffClinic = DeleteRoomMockData.GetActiveStaffClinic();
            var room = DeleteRoomMockData.GetFacilityRoom(isActive: false);

            //Arrange 2
            SetupHttpContextUserId(DeleteRoomMockData.TestUserId);
            SetupStaffClinicRepo(staffClinic);
            SetupFacilityRoomRepo(room);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(DeleteRoomMockData.TestRoomId);
            result.Data.IsActive.Should().BeTrue();

            _roomRepoMock.Verify(
                r => r.UpdateAsync(It.Is<FacilityRoom>(x => x.Id == DeleteRoomMockData.TestRoomId && x.IsActive)),
                Times.Once);
            _roomRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}