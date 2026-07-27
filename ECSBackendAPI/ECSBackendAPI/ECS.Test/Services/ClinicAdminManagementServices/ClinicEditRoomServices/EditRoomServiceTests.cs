using ECS.Application.Services.ClinicAdminManagementServices.ClinicEditRoomServices;
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

namespace ECS.Test.Services.ClinicAdminManagementServices.ClinicEditRoomServices
{
    /// <summary>
    /// Unit tests for <see cref="EditRoomService"/>.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// Goal: 100% line and ≥90% branch coverage on EditRoomService.cs.
    /// </summary>
    public class EditRoomServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<FacilityRoom, Guid, AppDbContext>> _roomRepoMock;
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly EditRoomService _sut;

        public EditRoomServiceTests()
        {
            _roomRepoMock = new Mock<IRepositoryBaseAsync<FacilityRoom, Guid, AppDbContext>>();
            _staffClinicRepoMock = new Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _sut = new EditRoomService(
                _roomRepoMock.Object,
                _staffClinicRepoMock.Object,
                _httpContextAccessorMock.Object);
        }

        // ── HttpContext helpers ────────────────────────────────────────────────

        /// <summary>
        /// Wires the http context accessor so that <see cref="ClaimTypes.NameIdentifier"/>
        /// resolves to a claim with the supplied raw value (used to exercise invalid Guid strings).
        /// Pass <c>null</c> to leave the principal with no claims.
        /// </summary>
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

        /// <summary>
        /// Sets up the room repo using a <see cref="Moq.MockSequence"/> to differentiate the
        /// two FindByCondition calls made by Process:
        ///   1st call → FetchRoomData (single FirstOrDefaultAsync)
        ///   2nd call → VerifyUniqueRoomName (AnyAsync duplicate check)
        /// </summary>
        private void SetupRoomRepoCalls(FacilityRoom? originalRoom, bool hasDuplicate)
        {
            var seq = new MockSequence();

            // 1st call: FetchRoomData — returns list with original room (or empty)
            var fetchList = originalRoom != null
                ? new List<FacilityRoom> { originalRoom }
                : new List<FacilityRoom>();
            var fetchQueryable = fetchList.BuildMockDbSet<FacilityRoom>();
            _roomRepoMock
                .InSequence(seq)
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<FacilityRoom, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(fetchQueryable.Object);

            // 2nd call: VerifyUniqueRoomName — returns whether duplicate exists
            var dupList = hasDuplicate
                ? new List<FacilityRoom>
                {
                    new FacilityRoom
                    {
                        Id = Guid.NewGuid(),
                        ClinicId = EditRoomMockData.TestClinicId,
                        RoomName = "Duplicate Room",
                        RoomType = "Consultation",
                        IsActive = true
                    }
                }
                : new List<FacilityRoom>();
            var dupQueryable = dupList.BuildMockDbSet<FacilityRoom>();
            _roomRepoMock
                .InSequence(seq)
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<FacilityRoom, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(dupQueryable.Object);
        }

        private void SetupRoomRepoOnlyFetch(FacilityRoom? originalRoom)
        {
            // For paths where VerifyUniqueRoomName short-circuits (FetchRoomData returns null
            // or isRoomExist=false), only the first FindByCondition call is invoked.
            var fetchList = originalRoom != null
                ? new List<FacilityRoom> { originalRoom }
                : new List<FacilityRoom>();
            var fetchQueryable = fetchList.BuildMockDbSet<FacilityRoom>();

            _roomRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<FacilityRoom, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(fetchQueryable.Object);
        }

        private void SetupRoomUpdate()
        {
            _roomRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<FacilityRoom>()))
                .Returns(Task.CompletedTask);
            _roomRepoMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.FromResult(1));
        }

        // ── Test Cases ─────────────────────────────────────────────────────────

        /// <summary>
        /// TC-ER-01: HttpContext accessor returns null → the `?.User.FindFirst(...)?.Value`
        /// chain short-circuits on the very first null-conditional → userIdClaim is null →
        /// Guid.TryParse fails → isUserValid=false. StaffClinic and FacilityRoom write paths
        /// must never execute.
        /// </summary>
        [Fact]
        public async Task Process_NullHttpContext_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = EditRoomMockData.GetValidRequest();

            //Arrange 2
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
            SetupStaffClinicRepo(null);
            SetupRoomRepoOnlyFetch(null);

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
        /// TC-ER-02: HttpContext is non-null but the principal has no NameIdentifier claim →
        /// `User.FindFirst(ClaimTypes.NameIdentifier)?.Value` returns null → Guid.TryParse
        /// fails → isUserValid=false.
        /// </summary>
        [Fact]
        public async Task Process_MissingNameIdentifierClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = EditRoomMockData.GetValidRequest();

            //Arrange 2
            SetupHttpContextClaim(null);
            SetupStaffClinicRepo(null);
            SetupRoomRepoOnlyFetch(null);

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
        /// TC-ER-03: Claim value is present but is not a valid Guid →
        /// Guid.TryParse returns false → isUserValid=false.
        /// </summary>
        [Fact]
        public async Task Process_InvalidUserIdClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = EditRoomMockData.GetValidRequest();

            //Arrange 2
            SetupHttpContextClaim("not-a-guid");
            SetupStaffClinicRepo(null);
            SetupRoomRepoOnlyFetch(null);

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
        /// TC-ER-04: Valid Guid claim but no active StaffClinic row → clinicId = null →
        /// FetchRoomData short-circuits on `!clinicId.HasValue` → originalRoom = null →
        /// isRoomExist = false → VerifyUniqueRoomName short-circuits on `!isRoomExist` →
        /// ApplyRoomUpdates early-returns → APP_MESSAGE_4020. Room repo must not be queried.
        /// </summary>
        [Fact]
        public async Task Process_ValidUserWithoutActiveClinic_Returns4020RoomNotFoundError()
        {
            //Arrange 1
            var request = EditRoomMockData.GetValidRequest();

            //Arrange 2
            SetupHttpContextUserId(EditRoomMockData.TestUserId);
            SetupStaffClinicRepo(null);
            SetupRoomRepoOnlyFetch(null);

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
        /// TC-ER-05: Valid user + valid clinic, but the requested room is not present in
        /// this clinic → FetchRoomData returns null → isRoomExist = false →
        /// VerifyUniqueRoomName short-circuits → ApplyRoomUpdates early-returns →
        /// APP_MESSAGE_4020.
        /// </summary>
        [Fact]
        public async Task Process_RoomNotFoundInClinic_Returns4020RoomNotFoundError()
        {
            //Arrange 1
            var request = EditRoomMockData.GetValidRequest();

            //Arrange 2
            SetupHttpContextUserId(EditRoomMockData.TestUserId);
            SetupStaffClinicRepo(EditRoomMockData.GetActiveStaffClinic());
            SetupRoomRepoOnlyFetch(null);

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
        /// TC-ER-06: Happy path data but another room with the same name already exists in
        /// the same clinic → VerifyUniqueRoomName returns false → ApplyRoomUpdates early-returns
        /// → CreateErrorResponse (!isNameUnique → APP_MESSAGE_4019).
        /// </summary>
        [Fact]
        public async Task Process_DuplicateRoomName_Returns4019DuplicateNameError()
        {
            //Arrange 1
            var request = EditRoomMockData.GetRequestWithName("Consultation Room A2");
            var staffClinic = EditRoomMockData.GetActiveStaffClinic();
            var originalRoom = EditRoomMockData.GetFacilityRoom(roomName: "Consultation Room A1");

            //Arrange 2
            SetupHttpContextUserId(EditRoomMockData.TestUserId);
            SetupStaffClinicRepo(staffClinic);
            SetupRoomRepoCalls(originalRoom, hasDuplicate: true);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
            result.Data.Should().BeNull();

            _roomRepoMock.Verify(
                r => r.UpdateAsync(It.IsAny<FacilityRoom>()),
                Times.Never);
            _roomRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// TC-ER-07: Case-insensitive duplicate branch — request name has different casing
        /// versus existing record, VerifyUniqueRoomName still returns false due to ToLower
        /// normalization.
        /// </summary>
        [Fact]
        public async Task Process_DuplicateRoomNameCaseInsensitive_Returns4019DuplicateNameError()
        {
            //Arrange 1
            var request = EditRoomMockData.GetRequestWithName("CONSULTATION ROOM A2");
            var staffClinic = EditRoomMockData.GetActiveStaffClinic();
            var originalRoom = EditRoomMockData.GetFacilityRoom(roomName: "Consultation Room A1");

            //Arrange 2
            SetupHttpContextUserId(EditRoomMockData.TestUserId);
            SetupStaffClinicRepo(staffClinic);
            SetupRoomRepoCalls(originalRoom, hasDuplicate: true);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
            result.Data.Should().BeNull();

            _roomRepoMock.Verify(
                r => r.UpdateAsync(It.IsAny<FacilityRoom>()),
                Times.Never);
        }

        /// <summary>
        /// TC-ER-08: Happy path – valid user + clinic + existing room + unique new name.
        /// Exercises full flow: ApplyRoomUpdates (UpdateAsync + SaveChangesAsync),
        /// MapToResponseDto populates all 5 DTO fields, CreateResponse success branch.
        /// </summary>
        [Fact]
        public async Task Process_ValidRequest_ReturnsSuccessWithUpdatedRoom()
        {
            //Arrange 1
            var request = EditRoomMockData.GetRequestWithName("  Consultation Room A2  ");
            var staffClinic = EditRoomMockData.GetActiveStaffClinic();
            var originalRoom = EditRoomMockData.GetFacilityRoom(
                roomName: "Consultation Room A1",
                roomType: "Consultation",
                isActive: true);

            //Arrange 2
            SetupHttpContextUserId(EditRoomMockData.TestUserId);
            SetupStaffClinicRepo(staffClinic);
            SetupRoomRepoCalls(originalRoom, hasDuplicate: false);
            SetupRoomUpdate();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(EditRoomMockData.TestRoomId);
            result.Data.ClinicId.Should().Be(EditRoomMockData.TestClinicId);
            result.Data.RoomName.Should().Be("Consultation Room A2");
            result.Data.RoomType.Should().Be("Consultation");
            result.Data.IsActive.Should().BeTrue();

            _roomRepoMock.Verify(
                r => r.UpdateAsync(It.Is<FacilityRoom>(x =>
                    x.Id == EditRoomMockData.TestRoomId &&
                    x.RoomName == "Consultation Room A2" &&
                    x.RoomType == "Consultation")),
                Times.Once);
            _roomRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        /// <summary>
        /// TC-ER-09: Happy path with RoomType == null → exercises the assignment branch where
        /// the request RoomType is null. The room is still updated and persisted with the
        /// null RoomType value preserved.
        /// </summary>
        [Fact]
        public async Task Process_ValidRequestWithNullRoomType_ReturnsSuccessWithNullRoomType()
        {
            //Arrange 1
            var request = new EditRoomRequest
            {
                RoomId = EditRoomMockData.TestRoomId,
                RoomName = "Consultation Room A3",
                RoomType = null
            };
            var staffClinic = EditRoomMockData.GetActiveStaffClinic();
            var originalRoom = EditRoomMockData.GetFacilityRoom(
                roomName: "Consultation Room A1",
                roomType: "Consultation");

            //Arrange 2
            SetupHttpContextUserId(EditRoomMockData.TestUserId);
            SetupStaffClinicRepo(staffClinic);
            SetupRoomRepoCalls(originalRoom, hasDuplicate: false);
            SetupRoomUpdate();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.RoomName.Should().Be("Consultation Room A3");
            result.Data.RoomType.Should().BeNull();

            _roomRepoMock.Verify(
                r => r.UpdateAsync(It.Is<FacilityRoom>(x =>
                    x.Id == EditRoomMockData.TestRoomId &&
                    x.RoomName == "Consultation Room A3" &&
                    x.RoomType == null)),
                Times.Once);
            _roomRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}