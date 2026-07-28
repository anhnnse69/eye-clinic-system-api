using System.Linq.Expressions;
using System.Security.Claims;
using ECS.Application.Services.SystemAdminServices.AdminSystemListAccountServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.SystemAdminServices.AdminSystemListAccountServices
{
    /// <summary>
    /// Unit tests for <see cref="GetAccountsService"/>.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// </summary>
    public class GetAccountsServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<User, Guid, AppDbContext>> _userRepoMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextMock = new();
        private readonly GetAccountsService _sut;

        public GetAccountsServiceTests()
        {
            _sut = new GetAccountsService(_userRepoMock.Object, _httpContextMock.Object);
        }

        private void SetupHttpContext(params Claim[] claims)
        {
            var context = new DefaultHttpContext();
            if (claims.Length > 0)
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            }

            _httpContextMock.Setup(x => x.HttpContext).Returns(context);
        }

        private void SetupSystemAdminContext()
        {
            SetupHttpContext(
                new Claim(ClaimTypes.NameIdentifier, SystemAdminAccountMockData.SystemAdminUserId.ToString()),
                new Claim(ClaimTypes.Role, UserRole.SYSTEM_ADMIN.ToString()));
        }

        private void SetupUsers(params User[] users)
        {
            var dbSet = users.ToList().BuildMockDbSet<User>();
            _userRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<bool>()))
                .Returns(dbSet.Object);
        }

        [Fact]
        public async Task Process_NullHttpContext_Returns4014()
        {
            //Arrange 1
            var request = SystemAdminAccountMockData.GetValidGetAccountsRequest();

            //Arrange 2
            _httpContextMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
            SetupUsers();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4014.ToString());
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task Process_MissingNameIdentifierClaim_Returns4014()
        {
            //Arrange 1
            var request = SystemAdminAccountMockData.GetValidGetAccountsRequest();

            //Arrange 2
            SetupHttpContext(new Claim(ClaimTypes.Role, UserRole.SYSTEM_ADMIN.ToString()));
            SetupUsers();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4014.ToString());
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task Process_MalformedNameIdentifierClaim_Returns4014()
        {
            //Arrange 1
            var request = SystemAdminAccountMockData.GetValidGetAccountsRequest();

            //Arrange 2
            SetupHttpContext(
                new Claim(ClaimTypes.NameIdentifier, "not-a-guid"),
                new Claim(ClaimTypes.Role, UserRole.SYSTEM_ADMIN.ToString()));
            SetupUsers();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4014.ToString());
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task Process_MissingSystemAdminRole_Returns4014()
        {
            //Arrange 1
            var request = SystemAdminAccountMockData.GetValidGetAccountsRequest();

            //Arrange 2
            SetupHttpContext(new Claim(ClaimTypes.NameIdentifier, SystemAdminAccountMockData.SystemAdminUserId.ToString()));
            SetupUsers();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4014.ToString());
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task Process_WrongRoleClaim_Returns4014()
        {
            //Arrange 1
            var request = SystemAdminAccountMockData.GetValidGetAccountsRequest();

            //Arrange 2
            SetupHttpContext(
                new Claim(ClaimTypes.NameIdentifier, SystemAdminAccountMockData.SystemAdminUserId.ToString()),
                new Claim(ClaimTypes.Role, UserRole.CLINIC_ADMIN.ToString()));
            SetupUsers();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4014.ToString());
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task Process_ValidSystemAdmin_Returns2000WithPagedAccounts()
        {
            //Arrange 1
            var request = SystemAdminAccountMockData.GetValidGetAccountsRequest();
            var account = SystemAdminAccountMockData.GetTargetAccount();

            //Arrange 2
            SetupSystemAdminContext();
            SetupUsers(account);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().HaveCount(1);
            result.Data![0].Id.Should().Be(account.Id.ToString());
            result.Data[0].Phone.Should().Be(account.Phone);
            result.Data[0].Email.Should().Be(account.Email);
            result.Data[0].FullName.Should().Be(account.FullName);
            result.Data[0].Role.Should().Be(account.Role.ToString());
            result.Data[0].IsActive.Should().Be(account.IsActive);
            result.Meta.Should().NotBeNull();
            result.Meta!.Total.Should().Be(1);
            result.Meta.Page.Should().Be(request.PageNumber);
            result.Meta.Size.Should().Be(request.PageSize);
        }

        [Fact]
        public async Task Process_ValidSystemAdmin_AppliesFilterExpression()
        {
            //Arrange 1
            var request = new GetAccountsRequest
            {
                PageNumber = 1,
                PageSize = 10,
                Role = UserRole.PATIENT,
                IsActive = true,
                SearchTerm = "nguyen"
            };
            var account = SystemAdminAccountMockData.GetTargetAccount();
            Expression<Func<User, bool>>? capturedPredicate = null;
            var dbSet = new List<User> { account }.BuildMockDbSet<User>();

            //Arrange 2
            SetupSystemAdminContext();
            _userRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<bool>()))
                .Callback<Expression<Func<User, bool>>, bool>((predicate, _) => capturedPredicate = predicate)
                .Returns(dbSet.Object);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            capturedPredicate.Should().NotBeNull();
            var predicate = capturedPredicate!.Compile();
            predicate(account).Should().BeTrue();
            predicate(new User
            {
                Id = Guid.NewGuid(),
                FullName = "Other User",
                Phone = "0900000000",
                Role = UserRole.DOCTOR,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }).Should().BeFalse();
        }
    }
}
