using ECS.Application.Services.ClinicAdminManagementServices.ClinicViewListRoomServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using MockQueryable.Moq;
using Moq;
using System.Linq.Expressions;
using System.Security.Claims;

namespace ECS.Test.Services.ClinicAdminManagementServices.ClinicViewListRoomServices
{
    /// <summary>
    /// Unit tests for <see cref="GetClinicRoomsService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line coverage on GetClinicRoomsService.cs.
    /// </summary>
    public class GetClinicRoomsServiceTests
    {
        private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid ClinicId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        private readonly Mock<IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext>> _roomRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly GetClinicRoomsService _sut;

        public GetClinicRoomsServiceTests()
        {
            _sut = new GetClinicRoomsService(
                _roomRepoMock.Object,
                _staffClinicRepoMock.Object,
                _httpContextAccessorMock.Object);
        }

        // ── HttpContext helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Wires the http context accessor so that <see cref="ClaimTypes.NameIdentifier"/> resolves
        /// to a claim with the supplied raw value (used to test invalid Guid strings).
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

        // ── Repository helpers ───────────────────────────────────────────────────

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

        private void SetupRoomRepo(IEnumerable<FacilityRoom> rooms)
        {
            var list = rooms.ToList();
            var mockQueryable = list.BuildMockDbSet<FacilityRoom>();

            _roomRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<FacilityRoom, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        // ── Data factories ───────────────────────────────────────────────────────

        private static StaffClinic ActiveStaffClinic() => new()
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            ClinicId = ClinicId,
            IsActive = true
        };

        private static FacilityRoom Room(
            string roomName = "Room-101",
            string? roomType = "Consultation",
            bool isActive = true) => new()
        {
            Id = Guid.NewGuid(),
            ClinicId = ClinicId,
            RoomName = roomName,
            RoomType = roomType,
            IsActive = isActive
        };

        // ── Test Cases ───────────────────────────────────────────────────────────

        /// <summary>
        /// TC-GRS-01: HttpContext is null → first null-conditional short-circuits → userIdClaim is null
        /// → Guid.TryParse fails → isUserValid=false → APP_MESSAGE_4001.
        /// StaffClinic repo never reached. Room repo is still queried by ExecutePagedQuery in
        /// production code, so we must wire it up with an empty list to satisfy the async provider.
        /// </summary>
        [Fact]
        public async Task Process_NullHttpContext_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new GetClinicRoomsRequest();

            //Arrange 2
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
            SetupRoomRepo(Array.Empty<FacilityRoom>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            result.Meta.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _roomRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<FacilityRoom, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// TC-GRS-02: HttpContext is non-null but HttpContext.User is null → second null-conditional
        /// short-circuits → userIdClaim is null → Guid.TryParse fails → APP_MESSAGE_4001.
        /// ExecutePagedQuery is still invoked in production (unconditional), so wire the room repo.
        /// </summary>
        [Fact]
        public async Task Process_NullHttpContextUser_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new GetClinicRoomsRequest();

            //Arrange 2
            var httpContext = new DefaultHttpContext();
            httpContext.User = null;
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
            SetupRoomRepo(Array.Empty<FacilityRoom>());

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
                Times.Once);
        }

        /// <summary>
        /// TC-GRS-03: HttpContext has a User but no NameIdentifier claim → FindFirst returns null
        /// → userIdClaim is null → Guid.TryParse fails → APP_MESSAGE_4001.
        /// ExecutePagedQuery is still invoked in production (unconditional), so wire the room repo.
        /// </summary>
        [Fact]
        public async Task Process_MissingNameIdentifierClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new GetClinicRoomsRequest();

            //Arrange 2
            SetupHttpContextClaim(null);
            SetupRoomRepo(Array.Empty<FacilityRoom>());

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
                Times.Once);
        }

        /// <summary>
        /// TC-GRS-04: Claim value is not a valid Guid → Guid.TryParse false branch → APP_MESSAGE_4001.
        /// ExecutePagedQuery is still invoked in production (unconditional), so wire the room repo.
        /// </summary>
        [Fact]
        public async Task Process_InvalidGuidClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new GetClinicRoomsRequest();

            //Arrange 2
            SetupHttpContextClaim("not-a-guid");
            SetupRoomRepo(Array.Empty<FacilityRoom>());

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
                Times.Once);
        }

        /// <summary>
        /// TC-GRS-05: Valid Guid claim but no active StaffClinic row → RetrieveClinicId returns null
        /// → APP_MESSAGE_4020. The room repo is still consulted by ExecutePagedQuery (unconditional
        /// in production), so it must be wired up with an empty list.
        /// </summary>
        [Fact]
        public async Task Process_ValidUserNoActiveClinic_Returns4020ClinicNotFound()
        {
            //Arrange 1
            var request = new GetClinicRoomsRequest();

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(null);
            SetupRoomRepo(Array.Empty<FacilityRoom>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
            result.Data.Should().BeNull();
            result.Meta.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _roomRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<FacilityRoom, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// TC-GRS-06: Happy path with empty room list → success with empty data, meta.Total=0.
        /// Covers ExecutePagedQuery zero-count path, MapToResponseDto empty-list path,
        /// BuildPaginationMeta with Total=0, CreateResponse success path.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_EmptyRoomList_Returns2000WithEmptyData()
        {
            //Arrange 1
            var request = new GetClinicRoomsRequest();

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupRoomRepo(Array.Empty<FacilityRoom>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Count.Should().Be(0);

            result.Meta.Should().NotBeNull();
            result.Meta!.Page.Should().Be(1);
            result.Meta.Size.Should().Be(10);
            result.Meta.Total.Should().Be(0);

            _roomRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<FacilityRoom, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// TC-GRS-07: SearchTerm is " ROOM-101 " → exercises Trim().ToLower() and the
        /// x.RoomName.ToLower().Contains(searchTerm) arm of the SearchTerm OR.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_SearchTermTrimsAndLowercases()
        {
            //Arrange 1
            var request = new GetClinicRoomsRequest
            {
                SearchTerm = " ROOM-101 "
            };
            var room = Room(roomName: "Room-101");

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupRoomRepo(new[] { room });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].RoomName.Should().Be("Room-101");
            result.Meta!.Total.Should().Be(1);
        }

        /// <summary>
        /// TC-GRS-08: SearchTerm is null → exercises request.SearchTerm?.Trim()?.ToLower() null-conditional
        /// and string.IsNullOrEmpty(searchTerm) true branch.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_SearchTermNull_SkipsNameMatching()
        {
            //Arrange 1
            var request = new GetClinicRoomsRequest
            {
                SearchTerm = null
            };
            var room = Room(roomName: "Room-202");

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupRoomRepo(new[] { room });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].RoomName.Should().Be("Room-202");
        }

        /// <summary>
        /// TC-GRS-09: RoomType filter is " consult " → exercises RoomType?.Trim().ToLower() and the
        /// (x.RoomType != null && x.RoomType.ToLower().Contains(typeFilter)) arm.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_RoomTypeFilter()
        {
            //Arrange 1
            var request = new GetClinicRoomsRequest
            {
                RoomType = " consult "
            };
            var room = Room(roomName: "Room-303", roomType: "Consultation");

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupRoomRepo(new[] { room });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].RoomType.Should().Be("Consultation");
        }

        /// <summary>
        /// TC-GRS-10: RoomType filter is null → exercises request.RoomType?.Trim()?.ToLower() null-conditional
        /// and string.IsNullOrEmpty(typeFilter) true branch.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_RoomTypeNull_SkipsTypeFilter()
        {
            //Arrange 1
            var request = new GetClinicRoomsRequest
            {
                RoomType = null
            };
            var room = Room(roomName: "Room-404", roomType: "Surgery");

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupRoomRepo(new[] { room });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].RoomType.Should().Be("Surgery");
        }

        /// <summary>
        /// TC-GRS-11: IsActive = true → exercises request.IsActive.HasValue true branch and
        /// x.IsActive == request.IsActive.Value true branch.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_IsActiveTrueFilter()
        {
            //Arrange 1
            var request = new GetClinicRoomsRequest
            {
                IsActive = true
            };
            var room = Room(roomName: "Room-505", isActive: true);

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupRoomRepo(new[] { room });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].IsActive.Should().BeTrue();
        }

        /// <summary>
        /// TC-GRS-12: IsActive = false → exercises request.IsActive.HasValue true branch and
        /// x.IsActive == request.IsActive.Value false branch.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_IsActiveFalseFilter()
        {
            //Arrange 1
            var request = new GetClinicRoomsRequest
            {
                IsActive = false
            };
            var room = Room(roomName: "Room-606", isActive: false);

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupRoomRepo(new[] { room });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            result.Data![0].IsActive.Should().BeFalse();
        }

        /// <summary>
        /// TC-GRS-13: IsActive = null → exercises request.IsActive.HasValue false branch (skip).
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_IsActiveNull_SkipsIsActiveFilter()
        {
            //Arrange 1
            var request = new GetClinicRoomsRequest
            {
                IsActive = null
            };
            var room = Room(roomName: "Room-707", isActive: true);

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupRoomRepo(new[] { room });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
        }

        /// <summary>
        /// TC-GRS-14: Multi-row dataset with PageNumber=2 / PageSize=5 → exercises OrderBy + Skip + Take
        /// + CountAsync chain and BuildPaginationMeta values.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_PaginationMeta_PopulatedFromRequest()
        {
            //Arrange 1
            var request = new GetClinicRoomsRequest
            {
                PageNumber = 2,
                PageSize = 5
            };
            var rows = Enumerable.Range(0, 20)
                .Select(i => Room(roomName: $"Room-{i:00}"))
                .ToList();

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupRoomRepo(rows);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(5);
            result.Meta!.Page.Should().Be(2);
            result.Meta.Size.Should().Be(5);
            result.Meta.Total.Should().Be(20);
        }

        /// <summary>
        /// TC-GRS-15: RoomType is null on the entity → exercises MapToResponseDto fallback to
        /// "General" via string.IsNullOrWhiteSpace(room.RoomType) true branch.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_MapToResponseDto_NullRoomType_DefaultsToGeneral()
        {
            //Arrange 1
            var request = new GetClinicRoomsRequest();
            var roomId = Guid.NewGuid();
            var room = new FacilityRoom
            {
                Id = roomId,
                ClinicId = ClinicId,
                RoomName = "Room-808",
                RoomType = null,
                IsActive = true
            };

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupRoomRepo(new[] { room });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            var dto = result.Data![0];
            dto.Id_room.Should().Be(roomId.ToString());
            dto.RoomName.Should().Be("Room-808");
            dto.RoomType.Should().Be("General");
            dto.IsActive.Should().BeTrue();
        }

        /// <summary>
        /// TC-GRS-16: RoomType is whitespace on the entity → exercises string.IsNullOrWhiteSpace
        /// true branch and verifies the "General" fallback. Also covers MapToResponseDto
        /// populated-IsActive=false path.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_MapToResponseDto_WhitespaceRoomType_DefaultsToGeneral()
        {
            //Arrange 1
            var request = new GetClinicRoomsRequest();
            var room = new FacilityRoom
            {
                Id = Guid.NewGuid(),
                ClinicId = ClinicId,
                RoomName = "Room-909",
                RoomType = "   ",
                IsActive = false
            };

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupRoomRepo(new[] { room });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            var dto = result.Data![0];
            dto.RoomName.Should().Be("Room-909");
            dto.RoomType.Should().Be("General");
            dto.IsActive.Should().BeFalse();
        }

        /// <summary>
        /// TC-GRS-17: RoomType is populated → exercises MapToResponseDto passthrough branch
        /// (string.IsNullOrWhiteSpace false). Single fully-mapped DTO with all fields copied.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_MapToResponseDto_PopulatedRoomType_PassedThrough()
        {
            //Arrange 1
            var request = new GetClinicRoomsRequest();
            var roomId = Guid.NewGuid();
            var room = new FacilityRoom
            {
                Id = roomId,
                ClinicId = ClinicId,
                RoomName = "Room-001",
                RoomType = "Surgery",
                IsActive = true
            };

            //Arrange 2
            SetupHttpContextUserId(UserId);
            SetupStaffClinicRepo(ActiveStaffClinic());
            SetupRoomRepo(new[] { room });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Count.Should().Be(1);
            var dto = result.Data![0];
            dto.Id_room.Should().Be(roomId.ToString());
            dto.RoomName.Should().Be("Room-001");
            dto.RoomType.Should().Be("Surgery");
            dto.IsActive.Should().BeTrue();
        }
    }
}
