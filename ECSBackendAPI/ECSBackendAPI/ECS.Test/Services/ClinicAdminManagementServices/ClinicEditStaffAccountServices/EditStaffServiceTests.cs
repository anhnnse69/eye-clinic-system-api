using System.Linq.Expressions;
using System.Security.Claims;
using ECS.Application.Services.ClinicAdminManagementServices.EditStaffAccountServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Storage;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.ClinicAdminManagementServices.ClinicEditStaffAccountServices;

public class EditStaffServiceTests
{
    private readonly Mock<IRepositoryBaseAsync<User, Guid, AppDbContext>> _userRepository = new();
    private readonly Mock<IRepositoryBaseAsync<StaffClinic, Guid, AppDbContext>> _staffRepository = new();
    private readonly Mock<IHttpContextAccessor> _httpAccessor = new();
    private readonly EditStaffService _sut;
    private readonly Guid _adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _staffId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _clinicId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public EditStaffServiceTests()
    {
        _sut = new EditStaffService(_userRepository.Object, _staffRepository.Object, _httpAccessor.Object);
    }

    [Fact]
    public async Task Process_NullHttpContext_ReturnsAdminValidationFailure()
    {
        //Arrange 1
        var request = Request(StaffRole.DOCTOR.ToString());
        //Arrange 2
        _httpAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        SetupQueries(null, null);
        //Act
        var result = await _sut.Process(request);
        //Assert
        result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4014.ToString());
        _userRepository.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task Process_InvalidGuidClaim_ReturnsAdminValidationFailure()
    {
        //Arrange 1
        var request = Request(StaffRole.DOCTOR.ToString());
        //Arrange 2
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") }));
        _httpAccessor.Setup(x => x.HttpContext).Returns(context);
        SetupQueries(null, null);
        //Act
        var result = await _sut.Process(request);
        //Assert
        result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4014.ToString());
        _userRepository.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task Process_TargetStaffUserIdEmpty_ReturnsStaffValidationFailure()
    {
        //Arrange 1
        var request = Request(StaffRole.DOCTOR.ToString());
        request.StaffUserId = Guid.Empty;
        //Arrange 2
        SetupAdminContext();
        SetupQueries(null, AdminClinic());
        //Act
        var result = await _sut.Process(request);
        //Assert
        result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
        _userRepository.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task Process_TargetUserNotFound_ReturnsStaffValidationFailure()
    {
        //Arrange 1
        var request = Request(StaffRole.DOCTOR.ToString());
        //Arrange 2
        SetupAdminContext();
        SetupQueries(null, AdminClinic());
        //Act
        var result = await _sut.Process(request);
        //Assert
        result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
    }

    [Fact]
    public async Task Process_AdminAndTargetHaveDifferentClinics_ReturnsBoundaryFailure()
    {
        //Arrange 1
        var request = Request(StaffRole.DOCTOR.ToString());
        var user = TargetUser(UserRole.RECEPTIONIST);
        //Arrange 2
        SetupAdminContext();
        SetupQueries(user, AdminClinic(), TargetClinic(Guid.NewGuid()));
        //Act
        var result = await _sut.Process(request);
        //Assert
        result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
        _userRepository.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task Process_AdminClinicLookupReturnsEmpty_ReturnsBoundaryFailure()
    {
        //Arrange 1
        var request = Request(StaffRole.DOCTOR.ToString());
        var user = TargetUser(UserRole.RECEPTIONIST);
        //Arrange 2
        SetupAdminContext();
        SetupQueries(user, null);
        //Act
        var result = await _sut.Process(request);
        //Assert
        result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
        _userRepository.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task Process_PhoneConflict_ReturnsPhoneUniquenessFailure()
    {
        //Arrange 1
        var request = Request(StaffRole.DOCTOR.ToString());
        var user = TargetUser(UserRole.RECEPTIONIST);
        //Arrange 2
        SetupAdminContext();
        SetupQueries(user, AdminClinic(), TargetClinic(_clinicId));
        SetupUniqueness(phoneConflict: new User { Id = Guid.NewGuid() }, emailConflict: null, targetUser: user);
        //Act
        var result = await _sut.Process(request);
        //Assert
        result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4018.ToString());
    }

    [Fact]
    public async Task Process_EmailConflict_ReturnsEmailUniquenessFailure()
    {
        //Arrange 1
        var request = Request(StaffRole.DOCTOR.ToString());
        var user = TargetUser(UserRole.RECEPTIONIST);
        //Arrange 2
        SetupAdminContext();
        SetupQueries(user, AdminClinic(), TargetClinic(_clinicId));
        SetupUniqueness(null, new User { Id = Guid.NewGuid() }, user);
        //Act
        var result = await _sut.Process(request);
        //Assert
        result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4017.ToString());
    }

    [Fact]
    public async Task Process_NonAdminPromotedToClinicAdmin_ReturnsPersistenceFailure()
    {
        //Arrange 1
        var request = Request(StaffRole.CLINIC_ADMIN.ToString());
        var user = TargetUser(UserRole.DOCTOR);
        //Arrange 2
        SetupAdminContext();
        SetupQueries(user, AdminClinic(), TargetClinic(_clinicId));
        SetupUniqueness(null, null, user);
        //Act
        var result = await _sut.Process(request);
        //Assert
        result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
        _userRepository.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task Process_InvalidStaffRoleString_ReturnsPersistenceFailure()
    {
        //Arrange 1
        var request = Request(StaffRole.DOCTOR.ToString());
        request.StaffRole = "INVALID_ROLE";
        var user = TargetUser(UserRole.CLINIC_ADMIN);
        //Arrange 2
        SetupAdminContext();
        SetupQueries(user, AdminClinic(), TargetClinic(_clinicId));
        SetupUniqueness(null, null, user);
        //Act
        var result = await _sut.Process(request);
        //Assert
        result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
        _userRepository.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task Process_UserHasNoStaffClinicMappings_CommitsUserOnly()
    {
        //Arrange 1
        var request = Request(StaffRole.DOCTOR.ToString());
        var user = TargetUser(UserRole.CLINIC_ADMIN);
        //Arrange 2
        SetupAdminContext();
        SetupQueries(user, AdminClinic(), TargetClinic(_clinicId));
        user.StaffClinics = new List<StaffClinic>();
        SetupUniqueness(null, null, user);
        SetupTransaction();
        //Act
        var result = await _sut.Process(request);
        //Assert
        result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2006.ToString());
        result.Data.Should().NotBeNull();
        _userRepository.Verify(x => x.UpdateAsync(user), Times.Once);
        _staffRepository.Verify(x => x.UpdateAsync(It.IsAny<StaffClinic>()), Times.Never);
        _userRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        _staffRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Process_UserHasNullStaffClinicMappings_CommitsUserOnly()
    {
        //Arrange 1
        var request = Request(StaffRole.DOCTOR.ToString());
        var user = TargetUser(UserRole.CLINIC_ADMIN);
        //Arrange 2
        SetupAdminContext();
        SetupQueries(user, AdminClinic(), TargetClinic(_clinicId));
        user.StaffClinics = null;
        SetupUniqueness(null, null, user);
        SetupTransaction();
        //Act
        var result = await _sut.Process(request);
        //Assert
        result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2006.ToString());
        result.Data.Should().NotBeNull();
        _userRepository.Verify(x => x.UpdateAsync(user), Times.Once);
        _staffRepository.Verify(x => x.UpdateAsync(It.IsAny<StaffClinic>()), Times.Never);
    }

    [Fact]
    public async Task Process_HttpContextPresentButUserPrincipalIsNull_ReturnsAdminValidationFailure()
    {
        //Arrange 1
        var request = Request(StaffRole.DOCTOR.ToString());
        //Arrange 2
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };
        _httpAccessor.Setup(x => x.HttpContext).Returns(context);
        SetupQueries(null, null);
        //Act
        var result = await _sut.Process(request);
        //Assert
        result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4014.ToString());
        _userRepository.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }

    [Theory]
    [InlineData(StaffRole.DOCTOR)]
    [InlineData(StaffRole.RECEPTIONIST)]
    [InlineData(StaffRole.CLINIC_ADMIN)]  
    public async Task Process_ValidRequest_CommitsUserAndStaffClinicChanges(StaffRole role)
    {
        //Arrange 1
        var request = Request(role.ToString());
        var user = TargetUser(UserRole.CLINIC_ADMIN);
        var mapping = TargetClinic(_clinicId);
        //Arrange 2
        SetupAdminContext();
        SetupQueries(user, AdminClinic(), mapping);
        SetupUniqueness(null, null, user);
        SetupTransaction();
        //Act
        var result = await _sut.Process(request);
        //Assert
        result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2006.ToString());
        result.Data.Should().NotBeNull();
        result.Data!.UpdatedRole.Should().Be(role.ToString());
        _userRepository.Verify(x => x.UpdateAsync(user), Times.Once);
        _staffRepository.Verify(x => x.UpdateAsync(mapping), Times.Once);
        _userRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        _staffRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Process_UserUpdateThrows_RollsBackAndReturnsPersistenceFailure()
    {
        //Arrange 1
        var request = Request(StaffRole.DOCTOR.ToString());
        var user = TargetUser(UserRole.CLINIC_ADMIN);
        //Arrange 2
        SetupAdminContext();
        SetupQueries(user, AdminClinic(), TargetClinic(_clinicId));
        SetupUniqueness(null, null, user);
        SetupTransaction();
        _userRepository.Setup(x => x.UpdateAsync(It.IsAny<User>())).ThrowsAsync(new InvalidOperationException());
        //Act
        var result = await _sut.Process(request);
        //Assert
        result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
        _userRepository.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task Process_StaffClinicSaveThrows_RollsBackAndReturnsPersistenceFailure()
    {
        //Arrange 1
        var request = Request(StaffRole.DOCTOR.ToString());
        var user = TargetUser(UserRole.CLINIC_ADMIN);
        //Arrange 2
        SetupAdminContext();
        SetupQueries(user, AdminClinic(), TargetClinic(_clinicId));
        SetupUniqueness(null, null, user);
        SetupTransaction();
        _staffRepository.Setup(x => x.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException());
        //Act
        var result = await _sut.Process(request);
        //Assert
        result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
        _userRepository.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task Process_RepositorySaveThrowsBeforeClinicUpdate_RollsBackAndReturnsPersistenceFailure()
    {
        //Arrange 1
        var request = Request(StaffRole.DOCTOR.ToString());
        var user = TargetUser(UserRole.CLINIC_ADMIN);
        //Arrange 2
        SetupAdminContext();
        SetupQueries(user, AdminClinic(), TargetClinic(_clinicId));
        SetupUniqueness(null, null, user);
        SetupTransaction();
        _userRepository.Setup(x => x.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException());
        //Act
        var result = await _sut.Process(request);
        //Assert
        result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
        _userRepository.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task Process_TransactionCommitThrows_RollsBackAndReturnsPersistenceFailure()
    {
        //Arrange 1
        var request = Request(StaffRole.DOCTOR.ToString());
        var user = TargetUser(UserRole.CLINIC_ADMIN);
        //Arrange 2
        SetupAdminContext();
        SetupQueries(user, AdminClinic(), TargetClinic(_clinicId));
        SetupUniqueness(null, null, user);
        var transaction = new Mock<IDbContextTransaction>();
        transaction.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());
        _userRepository.Setup(x => x.BeginTransactionAsync()).ReturnsAsync(transaction.Object);
        _userRepository.Setup(x => x.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _staffRepository.Setup(x => x.UpdateAsync(It.IsAny<StaffClinic>())).Returns(Task.CompletedTask);
        _userRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _staffRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        //Act
        var result = await _sut.Process(request);
        //Assert
        result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
        _userRepository.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }

    private EditStaffRequest Request(string role) => new()
    {
        StaffUserId = _staffId,
        Phone = "0987654321",
        Email = "staff@example.com",
        FullName = "Updated Staff",
        StaffRole = role,
        IsActive = true
    };

    private User TargetUser(UserRole role) => new() { Id = _staffId, Phone = "0123456789", Email = "old@example.com", FullName = "Staff", Role = role, StaffClinics = new List<StaffClinic>() };
    private StaffClinic AdminClinic() => new() { Id = Guid.NewGuid(), UserId = _adminId, ClinicId = _clinicId, IsActive = true, UpdatedAt = DateTime.UtcNow.AddMinutes(-2) };
    private StaffClinic TargetClinic(Guid clinic) => new() { Id = Guid.NewGuid(), UserId = _staffId, ClinicId = clinic, IsActive = true, UpdatedAt = DateTime.UtcNow };

    private void SetupAdminContext()
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, _adminId.ToString()) }));
        _httpAccessor.Setup(x => x.HttpContext).Returns(context);
    }

    private void SetupQueries(User? user, StaffClinic? admin, StaffClinic? target = null)
    {
        var users = user == null ? new List<User>() : new List<User> { user };
        var userSet = users.BuildMockDbSet<User>();
        _userRepository.Setup(x => x.FindByCondition(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<bool>())).Returns(userSet.Object);

        var mappings = new List<StaffClinic>();
        if (admin != null) mappings.Add(admin);
        if (target != null) mappings.Add(target);
        if (user != null && target != null) user.StaffClinics = new List<StaffClinic> { target };
        var mappingSet = mappings.BuildMockDbSet<StaffClinic>();
        _staffRepository.Setup(x => x.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>())).Returns(mappingSet.Object);
    }

    private void SetupUniqueness(User? phoneConflict, User? emailConflict, User? targetUser = null)
    {
        var phoneRows = phoneConflict == null ? new List<User>() : new List<User> { phoneConflict };
        var emailRows = emailConflict == null ? new List<User>() : new List<User> { emailConflict };
        var phoneSet = phoneRows.BuildMockDbSet<User>();
        var emailSet = emailRows.BuildMockDbSet<User>();
        var targetSet = (targetUser == null ? new List<User>() : new List<User> { targetUser }).BuildMockDbSet<User>();
        _userRepository.Setup(x => x.FindByCondition(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<bool>()))
            .Returns((Expression<Func<User, bool>> predicate, bool _) =>
            {
                var text = predicate.Body.ToString();
                if (text.Contains("Phone")) return phoneSet.Object;
                if (text.Contains("Email")) return emailSet.Object;
                return targetSet.Object;
            });
    }

    private void SetupTransaction()
    {
        var transaction = new Mock<IDbContextTransaction>();
        _userRepository.Setup(x => x.BeginTransactionAsync()).ReturnsAsync(transaction.Object);
        _userRepository.Setup(x => x.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _staffRepository.Setup(x => x.UpdateAsync(It.IsAny<StaffClinic>())).Returns(Task.CompletedTask);
        _userRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _staffRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
    }
}
