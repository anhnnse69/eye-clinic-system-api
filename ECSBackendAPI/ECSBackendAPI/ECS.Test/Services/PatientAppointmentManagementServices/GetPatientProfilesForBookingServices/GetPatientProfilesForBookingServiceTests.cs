using ECS.Application.Common.Response;
using ECS.Application.Services.PatientAppointmentManagementServices.GetPatientProfilesForBookingServices;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using Xunit;

namespace ECS.Test.Services.PatientAppointmentManagementServices.GetPatientProfilesForBookingServices
{
    public class GetPatientProfilesForBookingServiceTests : IDisposable
    {
        private readonly Mock<IRepositoryQueryBase<PatientProfile, Guid, AppDbContext>> _patientProfileRepoMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly AppDbContext _context;

        public GetPatientProfilesForBookingServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private GetPatientProfilesForBookingService CreateSut()
        {
            return new GetPatientProfilesForBookingService(
                _patientProfileRepoMock.Object,
                _context,
                _httpContextAccessorMock.Object);
        }

        private void SetupHttpContext(string? userIdClaim)
        {
            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext)null!);
                return;
            }

            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userIdClaim) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = claimsPrincipal };

            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        }

        private void SetupPatientProfileRepository(IEnumerable<PatientProfile> profiles)
        {
            _patientProfileRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()))
                .Returns((Expression<Func<PatientProfile, bool>> expression, bool trackChanges) =>
                {
                    var filteredList = profiles.AsQueryable().Where(expression).ToList();
                    return filteredList.BuildMockDbSet<PatientProfile>().Object;
                });
        }

        private object? InvokePrivate(GetPatientProfilesForBookingService sut, string methodName, params object[] args)
        {
            var method = typeof(GetPatientProfilesForBookingService).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Should().NotBeNull();
            return method!.Invoke(sut, args);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("invalid-guid")]
        public async Task Process_InvalidOrMissingUserClaim_ExecutesLines212To215_ReturnsFail4033(string? userIdClaim)
        {
            // Arrange: Thiết lập User Claim không hợp lệ
            SetupHttpContext(userIdClaim);

            // Mock Repository trả về DbSet rỗng để Helper/Repository không bị NullReferenceException
            SetupPatientProfileRepository(Enumerable.Empty<PatientProfile>());

            var sut = CreateSut();

            // Act: Chạy hàm Process() từ đầu đến cuối
            var result = await sut.Process();

            // Assert: Đảm bảo đi qua toàn bộ luồng đến CreateErrorResponse (dòng 212-215)
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            result.Data.Should().BeNull();
        }

        [Fact]
        public void CreateErrorResponse_DirectInvoke_ReturnsExpectedFailuresForEachBranch()
        {
            // Arrange
            var sut = CreateSut();

            // Act: Test bổ sung bằng Reflection trực tiếp vào CreateErrorResponse
            var invalidUserResponse = InvokePrivate(sut, "CreateErrorResponse", false, true) as ApiResponse<List<PatientProfileOption>>;
            var noDataScopeResponse = InvokePrivate(sut, "CreateErrorResponse", true, false) as ApiResponse<List<PatientProfileOption>>;
            var successResponse = InvokePrivate(sut, "CreateErrorResponse", true, true) as ApiResponse<List<PatientProfileOption>>;

            // Assert: Kiểm tra phủ chính xác dòng 212-215
            invalidUserResponse.Should().NotBeNull();
            invalidUserResponse!.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());

            noDataScopeResponse.Should().NotBeNull();
            noDataScopeResponse!.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());

            successResponse.Should().BeNull();
        }

        [Fact]
        public async Task Process_ValidUserWithNoAccessibleProfiles_ReturnsSuccessWithEmptyList()
        {
            // Arrange
            SetupHttpContext(GetPatientProfilesForBookingMockData.ValidUserId.ToString());
            SetupPatientProfileRepository(Enumerable.Empty<PatientProfile>());
            var sut = CreateSut();

            // Act
            var result = await sut.Process();

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task Process_ValidUserWithProfiles_ReturnsMappedProfilesAndRelationships()
        {
            // Arrange
            var userId = GetPatientProfilesForBookingMockData.ValidUserId;
            SetupHttpContext(userId.ToString());

            var selfProfile = GetPatientProfilesForBookingMockData.GetPatientProfile(
                id: GetPatientProfilesForBookingMockData.ValidPatientId1,
                userId: userId,
                fullName: "Trần Văn B",
                gender: Gender.MALE,
                dob: new DateTime(1990, 1, 15));

            var relativeProfile = GetPatientProfilesForBookingMockData.GetPatientProfile(
                id: GetPatientProfilesForBookingMockData.ValidPatientId2,
                userId: null,
                fullName: "Nguyễn Thị A",
                gender: Gender.FEMALE,
                dob: new DateTime(1965, 8, 25));

            var userPatientLink = GetPatientProfilesForBookingMockData.GetUserPatient(
                userId: userId,
                patientId: relativeProfile.Id,
                relationship: "Mẹ");

            await _context.Set<PatientProfile>().AddRangeAsync(selfProfile, relativeProfile);
            await _context.Set<UserPatient>().AddAsync(userPatientLink);
            await _context.SaveChangesAsync();

            SetupPatientProfileRepository(new[] { selfProfile, relativeProfile });

            var sut = CreateSut();

            // Act
            var result = await sut.Process();

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);

            var item1 = result.Data![0];
            item1.Id.Should().Be(relativeProfile.Id);
            item1.FullName.Should().Be("Nguyễn Thị A");
            item1.Gender.Should().Be(Gender.FEMALE.ToString());
            item1.Dob.Should().Be("25/08/1965");
            item1.Relationship.Should().Be("Mẹ");

            var item2 = result.Data[1];
            item2.Id.Should().Be(selfProfile.Id);
            item2.FullName.Should().Be("Trần Văn B");
            item2.Gender.Should().Be(Gender.MALE.ToString());
            item2.Dob.Should().Be("15/01/1990");
            item2.Relationship.Should().Be("Bản thân");
        }
    }
}