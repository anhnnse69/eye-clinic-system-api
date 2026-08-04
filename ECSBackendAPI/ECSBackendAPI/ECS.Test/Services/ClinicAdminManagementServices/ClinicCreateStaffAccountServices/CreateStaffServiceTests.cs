using ECS.Application.Services.ClinicAdminManagementServices.CreateStaffAccountServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Storage;
using MockQueryable.Moq;
using Moq;
using System.Linq.Expressions;
using System.Security.Claims;

namespace ECS.Test.Services.ClinicAdminManagementServices.ClinicCreateStaffAccountServices
{
    /// <summary>
    /// Unit tests for <see cref="CreateStaffService"/>.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// Goal: 100% line and ≥90% branch coverage on CreateStaffService.cs.
    /// </summary>
    public class CreateStaffServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<User, Guid, AppDbContext>> _userRepoMock;
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly CreateStaffService _sut;

        public CreateStaffServiceTests()
        {
            _userRepoMock = new Mock<IRepositoryBaseAsync<User, Guid, AppDbContext>>();
            _staffClinicRepoMock = new Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _sut = new CreateStaffService(
                _userRepoMock.Object,
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

        /// <summary>
        /// Sets up the staff-clinic query repository. Pass <c>null</c> for an empty result set.
        /// </summary>
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
        /// Sets up the user repository so that the unique phone/email predicates resolve
        /// against distinct sets. <paramref name="phoneConflict"/> is returned when the
        /// predicate references <c>Phone</c>; <paramref name="emailConflict"/> is returned
        /// when the predicate references <c>Email</c>. Pass <c>null</c> for either to seed
        /// an empty list (i.e. no conflict).
        ///
        /// Note: MockQueryable.Moq's BuildMockDbSet does not honour the supplied predicate,
        /// so we route the two distinct queries to two different DbSet instances by
        /// inspecting the expression body string.
        /// </summary>
        private void SetupUserUniquenessRepo(User? phoneConflict, User? emailConflict)
        {
            var phoneRows = phoneConflict != null ? new List<User> { phoneConflict } : new List<User>();
            var phoneDbSet = phoneRows.BuildMockDbSet<User>();
            var emailRows = emailConflict != null ? new List<User> { emailConflict } : new List<User>();
            var emailDbSet = emailRows.BuildMockDbSet<User>();

            _userRepoMock
                .Setup(r => r.FindByCondition(
                    It.Is<Expression<Func<User, bool>>>(e => e.Body.ToString().Contains("Phone")),
                    It.IsAny<bool>()))
                .Returns(phoneDbSet.Object);

            _userRepoMock
                .Setup(r => r.FindByCondition(
                    It.Is<Expression<Func<User, bool>>>(e => e.Body.ToString().Contains("Email")),
                    It.IsAny<bool>()))
                .Returns(emailDbSet.Object);
        }

        /// <summary>
        /// Configures the user repository so that BeginTransactionAsync returns a stub transaction
        /// and CreateAsync / SaveChangesAsync complete without throwing.
        /// The return value of CreateAsync is intentionally discarded by the service, so any
        /// valid Guid is acceptable.
        /// </summary>
        private void SetupUserTransactionSuccess()
        {
            var transactionMock = new Mock<IDbContextTransaction>();
            _userRepoMock
                .Setup(r => r.BeginTransactionAsync())
                .ReturnsAsync(transactionMock.Object);
            _userRepoMock
                .Setup(r => r.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync(Guid.NewGuid());
            _userRepoMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.FromResult(1));
        }

        /// <summary>
        /// Configures the user repository so that the inner CreateAsync call throws,
        /// driving the catch branch of <c>PersistStaffDataGraph</c>.
        /// </summary>
        private void SetupUserTransactionFailure()
        {
            var transactionMock = new Mock<IDbContextTransaction>();
            _userRepoMock
                .Setup(r => r.BeginTransactionAsync())
                .ReturnsAsync(transactionMock.Object);
            _userRepoMock
                .Setup(r => r.CreateAsync(It.IsAny<User>()))
                .ThrowsAsync(new InvalidOperationException("simulated persistence failure"));
        }

        // ── Test Cases ─────────────────────────────────────────────────────────

        /// <summary>
        /// TC-CSA-01: HttpContext has no NameIdentifier claim → Guid.TryParse fails → isCurrentAdminValid=false.
        /// Covers: RetrieveAdminUserId (User != null but FindFirst returns null),
        /// RetrieveContextClinicId (isCurrentAdminValid=false short-circuit),
        /// CheckPhoneUniqueness / CheckEmailUniqueness still execute (no early short-circuit),
        /// ConstructUserEntityTree (adminState=false → returns null),
        /// PersistStaffDataGraph (early return),
        /// FilterSystemicValidationFailures (!adminState → APP_MESSAGE_4014).
        /// </summary>
        [Fact]
        public async Task Process_HttpContextHasNoNameIdentifierClaim_Returns4014AdminError()
        {
            //Arrange 1
            var request = CreateStaffMockData.GetValidReceptionistRequest();

            //Arrange 2
            SetupHttpContextClaim(null);
            // phone/email uniqueness still resolved against empty list → both unique
            SetupUserUniquenessRepo(phoneConflict: null, emailConflict: null);
            SetupStaffClinicRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4014.ToString());
            result.Data.Should().BeNull();

            _userRepoMock.Verify(
                r => r.BeginTransactionAsync(),
                Times.Never);
            _userRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<User>()),
                Times.Never);
            _userRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// TC-CSA-02: HttpContext accessor returns null → the `_httpContextAccessor.HttpContext?.User`
        /// null-conditional chain short-circuits on the very first null-conditional →
        /// Guid.TryParse never runs → isCurrentAdminValid=false.
        /// Covers: the HttpContext == null branch of RetrieveAdminUserId.
        /// </summary>
        [Fact]
        public async Task Process_NullHttpContext_Returns4014AdminError()
        {
            //Arrange 1
            var request = CreateStaffMockData.GetValidReceptionistRequest();

            //Arrange 2
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
            SetupUserUniquenessRepo(phoneConflict: null, emailConflict: null);
            SetupStaffClinicRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4014.ToString());
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _userRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<User>()),
                Times.Never);
        }

        /// <summary>
        /// TC-CSA-03: Valid Guid claim but no active StaffClinic row → clinicId = Guid.Empty →
        /// ConstructUserEntityTree returns null (targetClinicId == Guid.Empty guard) →
        /// PersistStaffDataGraph returns (null, false) → FilterSystemicValidationFailures
        /// cascades through admin/phone/email (all true) and falls through to the last
        /// guard (!successState → APP_MESSAGE_5001).
        /// </summary>
        [Fact]
        public async Task Process_ValidUserButNoStaffClinic_Returns5001GeneralError()
        {
            //Arrange 1
            var request = CreateStaffMockData.GetValidReceptionistRequest();

            //Arrange 2
            SetupHttpContextUserId(CreateStaffMockData.TestAdminUserId);
            SetupUserUniquenessRepo(phoneConflict: null, emailConflict: null);
            SetupStaffClinicRepo(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
            result.Data.Should().BeNull();

            // staff-clinic query is still issued because adminState is true at that point
            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _userRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<User>()),
                Times.Never);
        }

        /// <summary>
        /// TC-CSA-04: Phone already exists → CheckPhoneUniqueness returns false →
        /// ConstructUserEntityTree returns null (phoneState=false guard) →
        /// PersistStaffDataGraph early-returns → FilterSystemicValidationFailures
        /// surfaces !phoneState → APP_MESSAGE_4018.
        /// </summary>
        [Fact]
        public async Task Process_PhoneAlreadyExists_Returns4018PhoneError()
        {
            //Arrange 1
            var request = CreateStaffMockData.GetValidReceptionistRequest();
            var existingPhoneUser = CreateStaffMockData.GetUserWithPhone(request.Phone);
            var activeStaffClinic = CreateStaffMockData.GetActiveStaffClinic();

            //Arrange 2
            SetupHttpContextUserId(CreateStaffMockData.TestAdminUserId);
            SetupStaffClinicRepo(activeStaffClinic);
            SetupUserUniquenessRepo(phoneConflict: existingPhoneUser, emailConflict: null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4018.ToString());
            result.Data.Should().BeNull();

            _userRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<User>()),
                Times.Never);
            _userRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// TC-CSA-05: Email already exists → CheckEmailUniqueness returns false →
        /// ConstructUserEntityTree returns null (emailState=false guard) →
        /// PersistStaffDataGraph early-returns → FilterSystemicValidationFailures
        /// surfaces !emailState → APP_MESSAGE_4017.
        /// </summary>
        [Fact]
        public async Task Process_EmailAlreadyExists_Returns4017EmailError()
        {
            //Arrange 1
            var request = CreateStaffMockData.GetValidReceptionistRequest();
            var existingEmailUser = CreateStaffMockData.GetUserWithEmail(request.Email);
            var activeStaffClinic = CreateStaffMockData.GetActiveStaffClinic();

            //Arrange 2
            SetupHttpContextUserId(CreateStaffMockData.TestAdminUserId);
            SetupStaffClinicRepo(activeStaffClinic);
            SetupUserUniquenessRepo(phoneConflict: null, emailConflict: existingEmailUser);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4017.ToString());
            result.Data.Should().BeNull();

            _userRepoMock.Verify(
                r => r.CreateAsync(It.IsAny<User>()),
                Times.Never);
        }

        /// <summary>
        /// TC-CSA-06: Persistence layer throws → PersistStaffDataGraph catch branch executes
        /// RollbackAsync and returns (null, false) → FilterSystemicValidationFailures
        /// surfaces !successState → APP_MESSAGE_5001.
        /// </summary>
        [Fact]
        public async Task Process_PersistenceThrows_Returns5001GeneralError()
        {
            //Arrange 1
            var request = CreateStaffMockData.GetValidReceptionistRequest();
            var activeStaffClinic = CreateStaffMockData.GetActiveStaffClinic();

            //Arrange 2
            SetupHttpContextUserId(CreateStaffMockData.TestAdminUserId);
            SetupStaffClinicRepo(activeStaffClinic);
            SetupUserUniquenessRepo(phoneConflict: null, emailConflict: null);
            SetupUserTransactionFailure();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
            result.Data.Should().BeNull();

            // The transaction is opened but not committed (commit happens only on the success path).
            _userRepoMock.Verify(
                r => r.BeginTransactionAsync(),
                Times.Once);
            _userRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// TC-CSA-07: Happy path with StaffRole = RECEPTIONIST.
        /// Covers: switch(RECEPTIONIST) default branch where mappedUserRole keeps its initial value
        /// (UserRole.RECEPTIONIST), full PersistStaffDataGraph success path (Commit, no Rollback),
        /// MapToResponse → CreateResponse success branch (APP_MESSAGE_2000).
        /// </summary>
        [Fact]
        public async Task Process_ReceptionistRole_ValidRequest_ReturnsSuccessWithAssignedRole()
        {
            //Arrange 1
            var request = CreateStaffMockData.GetValidReceptionistRequest();
            var activeStaffClinic = CreateStaffMockData.GetActiveStaffClinic();

            //Arrange 2
            SetupHttpContextUserId(CreateStaffMockData.TestAdminUserId);
            SetupStaffClinicRepo(activeStaffClinic);
            SetupUserUniquenessRepo(phoneConflict: null, emailConflict: null);
            SetupUserTransactionSuccess();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            // UserId is locally generated inside ConstructUserEntityTree via Guid.NewGuid(); the
            // value returned from CreateAsync is discarded by the service.
            result.Data!.UserId.Should().NotBe(Guid.Empty);
            result.Data.StaffClinicId.Should().NotBe(Guid.Empty);
            result.Data.Phone.Should().Be(request.Phone);
            result.Data.AssignedRole.Should().Be(StaffRole.RECEPTIONIST.ToString());

            _userRepoMock.Verify(
                r => r.CreateAsync(It.Is<User>(u =>
                    u.Phone == request.Phone &&
                    u.Email == request.Email &&
                    u.FullName == request.FullName &&
                    u.Role == UserRole.RECEPTIONIST &&
                    u.IsActive &&
                    u.StaffClinics.Count == 1 &&
                    u.StaffClinics.First().ClinicId == CreateStaffMockData.TestClinicId &&
                    u.StaffClinics.First().Role == StaffRole.RECEPTIONIST)),
                Times.Once);
            _userRepoMock.Verify(
                r => r.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// TC-CSA-08: Happy path with StaffRole = DOCTOR.
        /// Covers: switch(DOCTOR) branch → mappedUserRole = UserRole.DOCTOR.
        /// </summary>
        [Fact]
        public async Task Process_DoctorRole_ValidRequest_PersistsAsDoctorRole()
        {
            //Arrange 1
            var request = CreateStaffMockData.GetValidDoctorRequest();
            var activeStaffClinic = CreateStaffMockData.GetActiveStaffClinic();

            //Arrange 2
            SetupHttpContextUserId(CreateStaffMockData.TestAdminUserId);
            SetupStaffClinicRepo(activeStaffClinic);
            SetupUserUniquenessRepo(phoneConflict: null, emailConflict: null);
            SetupUserTransactionSuccess();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.AssignedRole.Should().Be(StaffRole.DOCTOR.ToString());

            _userRepoMock.Verify(
                r => r.CreateAsync(It.Is<User>(u =>
                    u.Role == UserRole.DOCTOR &&
                    u.StaffClinics.First().Role == StaffRole.DOCTOR &&
                    u.DoctorProfiles != null &&
                    u.DoctorProfiles.Count == 1 &&
                    u.DoctorProfiles.First().ClinicId == CreateStaffMockData.TestClinicId)),
                Times.Once);
        }

        /// <summary>
        /// TC-CSA-09: Happy path with StaffRole = CLINIC_ADMIN.
        /// Covers: switch(CLINIC_ADMIN) branch → mappedUserRole = UserRole.CLINIC_ADMIN.
        /// </summary>
        [Fact]
        public async Task Process_ClinicAdminRole_ValidRequest_PersistsAsClinicAdminRole()
        {
            //Arrange 1
            var request = CreateStaffMockData.GetValidClinicAdminRequest();
            var activeStaffClinic = CreateStaffMockData.GetActiveStaffClinic();

            //Arrange 2
            SetupHttpContextUserId(CreateStaffMockData.TestAdminUserId);
            SetupStaffClinicRepo(activeStaffClinic);
            SetupUserUniquenessRepo(phoneConflict: null, emailConflict: null);
            SetupUserTransactionSuccess();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.AssignedRole.Should().Be(StaffRole.CLINIC_ADMIN.ToString());

            _userRepoMock.Verify(
                r => r.CreateAsync(It.Is<User>(u =>
                    u.Role == UserRole.CLINIC_ADMIN &&
                    u.StaffClinics.First().Role == StaffRole.CLINIC_ADMIN)),
                Times.Once);
        }
    }
}
