using ECS.Application.Services.ClinicAdminManagementServices.ClinicCreateRoomSevices;
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

namespace ECS.Test.Services.ClinicAdminManagementServices.ClinicCreateRoomServices
{
    /// <summary>
    /// Unit tests for <see cref="CreateRoomService"/>.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// Goal: 100% line and 100% branch coverage on CreateRoomService.cs.
    /// </summary>
    public class CreateRoomServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<FacilityRoom, Guid, AppDbContext>> _roomRepoMock;
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly CreateRoomService _sut;

        public CreateRoomServiceTests()
        {
            _roomRepoMock = new Mock<IRepositoryBaseAsync<FacilityRoom, Guid, AppDbContext>>();
            _staffClinicRepoMock = new Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _sut = new CreateRoomService(
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
        /// TC-CR-01: HttpContext has no NameIdentifier claim → Guid.TryParse fails → isUserValid=false.
        /// Covers: RetrieveUserId (User != null but FindFirst returns null), RetrieveClinicId
        /// (isUserValid=false short-circuit), ValidateRoomNameUniquenessAsync (isClinicValid=false short-circuit),
        /// SaveFacilityRoomRecordAsync (early return), CreateResponse → CreateErrorResponse (!isUserValid).
        /// StaffClinic and FacilityRoom write paths must never execute.
        /// </summary>
        /// </summary>
        [Fact]
        public async Task Process_NullUserClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = CreateRoomMockData.GetValidRequest();

            //Arrange 2
            SetupHttpContextClaim(null);
            // The room repo is still queried indirectly via InitializeRoomEntity → save attempt;
            // however the guard in SaveFacilityRoomRecordAsync prevents create/save calls.
            SetupFacilityRoomRepo(null);
            SetupStaffClinicRepo(null);

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
                r => r.CreateAsync(It.IsAny<FacilityRoom>()),
                Times.Never);
            _roomRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// TC-CR-02: Valid Guid claim but no active StaffClinic row → clinicId = Guid.Empty → isClinicValid=false.
        /// Covers: RetrieveClinicId (staffClinic == null branch), EvaluateClinicValidity false branch,
        /// ValidateRoomNameUniquenessAsync (isClinicValid=false short-circuit),
        /// SaveFacilityRoomRecordAsync (early return), CreateErrorResponse (!isClinicValid → APP_MESSAGE_4020).
        /// </summary>
        [Fact]
        public async Task Process_NoActiveStaffClinic_Returns4020ClinicError()
        {
            //Arrange 1
            var request = CreateRoomMockData.GetValidRequest();

            //Arrange 2
            SetupHttpContextUserId(CreateRoomMockData.TestUserId);
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
            _roomRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<FacilityRoom>()),
                Times.Never);
            _roomRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// TC-CR-03: Existing room with the same name (normalized) → isNameUnique=false.
        /// Covers: ValidateRoomNameUniquenessAsync (full path → AnyAsync returns true → returns false),
        /// SaveFacilityRoomRecordAsync (early return), CreateErrorResponse (!isNameUnique → APP_MESSAGE_4021).
        /// </summary>
        [Fact]
        public async Task Process_DuplicateRoomName_Returns4021DuplicateError()
        {
            //Arrange 1
            var request = CreateRoomMockData.GetValidRequest();
            var staffClinic = CreateRoomMockData.GetActiveStaffClinic();
            var existingRoom = CreateRoomMockData.GetExistingFacilityRoom("Consultation Room A1");

            //Arrange 2
            SetupHttpContextUserId(CreateRoomMockData.TestUserId);
            SetupStaffClinicRepo(staffClinic);
            SetupFacilityRoomRepo(existingRoom);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4021.ToString());
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _roomRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<FacilityRoom, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _roomRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<FacilityRoom>()),
                Times.Never);
            _roomRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// TC-CR-04: HttpContext accessor returns null → the `?.User.FindFirst(...)?.Value` chain short-circuits
        /// on the very first null-conditional → userIdClaim is null → Guid.TryParse fails → isUserValid=false.
        /// Covers: RetrieveUserId (HttpContext == null branch of the `?.User` chain). This drives the
        /// 50% branch in `RetrieveUserId` condition #13 (the `_httpContextAccessor.HttpContext == null` path).
        /// </summary>
        [Fact]
        public async Task Process_NullHttpContext_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = CreateRoomMockData.GetValidRequest();

            //Arrange 2
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
            SetupFacilityRoomRepo(null);
            SetupStaffClinicRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _roomRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<FacilityRoom>()),
                Times.Never);
            _roomRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// TC-CR-05: Happy path with RoomType == null → exercises the `request.RoomType?.Trim()` null branch
        /// inside InitializeRoomEntity (condition #52 of line 155 in the service). The room is still created
        /// and saved with RoomType persisted as null.
        /// </summary>
        [Fact]
        public async Task Process_ValidRequestWithNullRoomType_PersistsRoomWithNullRoomType()
        {
            //Arrange 1
            var request = new CreateRoomRequest
            {
                RoomName = "Consultation Room B2",
                RoomType = null
            };
            var staffClinic = CreateRoomMockData.GetActiveStaffClinic();
            var generatedRoomId = CreateRoomMockData.TestRoomId;

            //Arrange 2
            SetupHttpContextUserId(CreateRoomMockData.TestUserId);
            SetupStaffClinicRepo(staffClinic);
            SetupFacilityRoomRepo(null);
            _roomRepoMock
                .Setup(r => r.CreateAsync(It.IsAny<FacilityRoom>()))
                .ReturnsAsync(generatedRoomId);
            _roomRepoMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.FromResult(1));

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(generatedRoomId);
            result.Data.RoomType.Should().BeNull();

            _roomRepoMock.Verify(
                r => r.CreateAsync(It.Is<FacilityRoom>(room =>
                    room.ClinicId == CreateRoomMockData.TestClinicId &&
                    room.RoomName == "Consultation Room B2" &&
                    room.RoomType == null &&
                    room.IsActive)),
                Times.Once);
            _roomRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        /// <summary>
        /// TC-CR-06: Happy path → all validations pass → facility room is created and saved.
        /// Covers: full Process flow including InitializeRoomEntity (Trim and RoomType.Trim-with-non-null),
        /// MapToResponseDto mapping, CreateResponse success branch.
        /// </summary>
        [Fact]
        public async Task Process_ValidRequestWithUniqueRoom_ReturnsSuccessAndPersistsTrimmedRoom()
        {
            //Arrange 1
            var request = CreateRoomMockData.GetValidRequest();
            var staffClinic = CreateRoomMockData.GetActiveStaffClinic();
            var generatedRoomId = CreateRoomMockData.TestRoomId;

            //Arrange 2
            SetupHttpContextUserId(CreateRoomMockData.TestUserId);
            SetupStaffClinicRepo(staffClinic);
            SetupFacilityRoomRepo(null);
            _roomRepoMock
                .Setup(r => r.CreateAsync(It.IsAny<FacilityRoom>()))
                .ReturnsAsync(generatedRoomId);
            _roomRepoMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.FromResult(1));

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(generatedRoomId);
            result.Data.ClinicId.Should().Be(CreateRoomMockData.TestClinicId);
            result.Data.RoomName.Should().Be("Consultation Room A1");
            result.Data.RoomType.Should().Be("Consultation");
            result.Data.IsActive.Should().BeTrue();

            _roomRepoMock.Verify(
                r => r.CreateAsync(It.Is<FacilityRoom>(room =>
                    room.ClinicId == CreateRoomMockData.TestClinicId &&
                    room.RoomName == "Consultation Room A1" &&
                    room.RoomType == "Consultation" &&
                    room.IsActive)),
                Times.Once);
            _roomRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}
