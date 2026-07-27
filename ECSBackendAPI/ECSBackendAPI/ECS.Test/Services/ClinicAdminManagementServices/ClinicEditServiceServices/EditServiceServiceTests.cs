using ECS.Application.Services.ClinicAdminManagementServices.EditServiceServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using MockQueryable.Moq;
using Moq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;

namespace ECS.Test.Services.ClinicAdminManagementServices.ClinicEditServiceServices
{
    public class EditServiceServiceTests
    {
        private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid ClinicId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid OtherClinicId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        private static readonly Guid ServiceId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly Guid OtherServiceId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        private readonly Mock<IRepositoryBaseAsync<Service, Guid, AppDbContext>> _serviceRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly EditServiceService _sut;

        public EditServiceServiceTests()
        {
            _sut = new EditServiceService(
                _serviceRepoMock.Object,
                _staffClinicRepoMock.Object,
                _httpContextAccessorMock.Object);
        }

        // -------------------------------------------------------------------
        // Reflection helpers for private methods
        // -------------------------------------------------------------------
        private static object? InvokePrivate(object target, string methodName, params object[] args)
        {
            var mi = target.GetType().GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            mi.Should().NotBeNull($"method '{methodName}' must exist on {target.GetType().Name}");
            return mi!.Invoke(target, args);
        }

        private static async Task<T> InvokePrivateAsync<T>(object target, string methodName, params object[] args)
        {
            var raw = InvokePrivate(target, methodName, args);
            raw.Should().NotBeNull();
            var task = (Task<T>)raw!;
            return await task;
        }

        private static async Task InvokePrivateTaskAsync(object target, string methodName, params object[] args)
        {
            var raw = InvokePrivate(target, methodName, args);
            raw.Should().NotBeNull();
            await (Task)raw!;
        }

        // -------------------------------------------------------------------
        // Mock-data + setup helpers
        // -------------------------------------------------------------------
        private void SetupClaim(string? value)
        {
            var context = new DefaultHttpContext();
            if (value != null)
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, value) }, "TestAuth"));
            }

            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
        }

        private void SetupNullHttpContext()
        {
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        }

        private void SetupUserWithNullPrincipal()
        {
            var context = new DefaultHttpContext();
            // Explicitly null User.
            context.User = null!;
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
        }

        private void SetupStaffClinic(StaffClinic? staffClinic)
        {
            var rows = staffClinic == null ? new List<StaffClinic>() : new List<StaffClinic> { staffClinic };
            var queryable = rows.BuildMockDbSet();
            _staffClinicRepoMock.Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()))
                .Returns(queryable.Object);
        }

        private void SetupGetById(Service? service)
        {
            _serviceRepoMock
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(service);
        }

        private void SetupDuplicateNameQuery(bool hasConflict)
        {
            var rows = hasConflict
                ? new List<Service> { new Service { Id = OtherServiceId } }
                : new List<Service>();
            var queryable = rows.BuildMockDbSet();
            _serviceRepoMock.Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<Service, bool>>>(), It.IsAny<bool>()))
                .Returns(queryable.Object);
        }

        // Invokes a private method whose last parameter is `ref bool` (or any `ref`/`out`).
        // Returns the final value of the ref flag after invocation.
        private static bool InvokeRefBool(object target, string methodName, params object[] args)
        {
            var newArgs = new object?[args.Length + 1];
            Array.Copy(args, newArgs, args.Length);
            newArgs[args.Length] = true;
            var mi = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)!;
            mi.Invoke(target, newArgs);
            return (bool)newArgs[args.Length]!;
        }

        private static StaffClinic ActiveStaffClinic() => new()
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            ClinicId = ClinicId,
            IsActive = true
        };

        private static Service ExistingService(Guid? clinicIdOverride = null) => new()
        {
            Id = ServiceId,
            ClinicId = clinicIdOverride ?? ClinicId,
            ServiceName = "Old Name",
            Price = 100m,
            DurationMinutes = 15,
            IsActive = true
        };

        private static EditServiceRequest ValidRequest() => new()
        {
            ServiceId = ServiceId,
            ServiceName = "Updated Service",
            Price = 250m,
            DurationMinutes = 30
        };

        // ===================================================================
        // ====================== Process(...) tests =========================
        // ===================================================================

        [Fact]
        public async Task Process_NullHttpContext_ReturnsAuthenticationError()
        {
            //Arrange 1
            var request = ValidRequest();

            //Arrange 2
            SetupNullHttpContext();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            _staffClinicRepoMock.Verify(x => x.FindByCondition(
                It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()), Times.Never);
            _serviceRepoMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _serviceRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Service>()), Times.Never);
            _serviceRepoMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_MissingNameIdentifierClaim_ReturnsAuthenticationError()
        {
            //Arrange 1
            var request = ValidRequest();

            //Arrange 2
            SetupClaim(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            _staffClinicRepoMock.Verify(x => x.FindByCondition(
                It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()), Times.Never);
            _serviceRepoMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Process_InvalidGuidClaim_ReturnsAuthenticationError()
        {
            //Arrange 1
            var request = ValidRequest();

            //Arrange 2
            SetupClaim("not-a-guid");

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            _staffClinicRepoMock.Verify(x => x.FindByCondition(
                It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()), Times.Never);
            _serviceRepoMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Process_ValidUserNoActiveClinic_ReturnsClinicNotFoundError()
        {
            //Arrange 1
            var request = ValidRequest();

            //Arrange 2
            SetupClaim(UserId.ToString());
            SetupStaffClinic(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
            result.Data.Should().BeNull();
            _staffClinicRepoMock.Verify(x => x.FindByCondition(
                It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()), Times.Once);
            _serviceRepoMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _serviceRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Service>()), Times.Never);
            _serviceRepoMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_ValidClinicServiceNotFound_ReturnsServiceNotFoundError()
        {
            //Arrange 1
            var request = ValidRequest();

            //Arrange 2
            SetupClaim(UserId.ToString());
            SetupStaffClinic(ActiveStaffClinic());
            SetupGetById(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4012.ToString());
            result.Data.Should().BeNull();
            _serviceRepoMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Once);
            _serviceRepoMock.Verify(x => x.FindByCondition(
                It.IsAny<Expression<Func<Service, bool>>>(), It.IsAny<bool>()), Times.Never);
            _serviceRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Service>()), Times.Never);
            _serviceRepoMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_ServiceBelongsToAnotherClinic_ReturnsForbiddenError()
        {
            //Arrange 1
            var request = ValidRequest();
            var service = ExistingService(clinicIdOverride: OtherClinicId);

            //Arrange 2
            SetupClaim(UserId.ToString());
            SetupStaffClinic(ActiveStaffClinic());
            SetupGetById(service);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4014.ToString());
            result.Data.Should().BeNull();
            _serviceRepoMock.Verify(x => x.FindByCondition(
                It.IsAny<Expression<Func<Service, bool>>>(), It.IsAny<bool>()), Times.Never);
            _serviceRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Service>()), Times.Never);
            _serviceRepoMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_DuplicateNameInClinic_ReturnsDuplicateNameError()
        {
            //Arrange 1
            var request = ValidRequest();
            var service = ExistingService();

            //Arrange 2
            SetupClaim(UserId.ToString());
            SetupStaffClinic(ActiveStaffClinic());
            SetupGetById(service);
            SetupDuplicateNameQuery(true);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4015.ToString());
            result.Data.Should().BeNull();
            _serviceRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Service>()), Times.Never);
            _serviceRepoMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_ValidRequest_UpdatesAndReturnsSuccess()
        {
            //Arrange 1
            var request = ValidRequest();
            var service = ExistingService();

            //Arrange 2
            SetupClaim(UserId.ToString());
            SetupStaffClinic(ActiveStaffClinic());
            SetupGetById(service);
            SetupDuplicateNameQuery(false);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.ServiceId.Should().Be(ServiceId.ToString());
            result.Data.ClinicId.Should().Be(ClinicId.ToString());
            result.Data.ServiceName.Should().Be("Updated Service");
            result.Data.Price.Should().Be(250m);
            result.Data.DurationMinutes.Should().Be(30);
            result.Data.UpdatedAt.Should().NotBeNullOrEmpty();
            _serviceRepoMock.Verify(x => x.UpdateAsync(It.Is<Service>(s =>
                s.ServiceName == "Updated Service" && s.Price == 250m && s.DurationMinutes == 30)), Times.Once);
            _serviceRepoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Process_TrimsServiceNameBeforePersisting()
        {
            //Arrange 1
            var request = new EditServiceRequest
            {
                ServiceId = ServiceId,
                ServiceName = "   Spaced Name   ",
                Price = 10m,
                DurationMinutes = 20
            };
            var service = ExistingService();

            //Arrange 2
            SetupClaim(UserId.ToString());
            SetupStaffClinic(ActiveStaffClinic());
            SetupGetById(service);
            SetupDuplicateNameQuery(false);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.ServiceName.Should().Be("Spaced Name");
            _serviceRepoMock.Verify(x => x.UpdateAsync(It.Is<Service>(s =>
                s.ServiceName == "Spaced Name")), Times.Once);
            _serviceRepoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        // ===================================================================
        // ============ RetrieveUserId(out bool) – private ===================
        // ===================================================================

        [Fact]
        public void RetrieveUserId_NullHttpContext_SetsFlagFalse()
        {
            //Arrange 1
            object?[] boxed = { true };

            //Arrange 2
            SetupNullHttpContext();

            //Act
            var result = typeof(EditServiceService)
                .GetMethod("RetrieveUserId", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_sut, boxed);

            //Assert
            boxed[0].Should().Be(false);
            result.Should().Be(Guid.Empty);
        }

        [Fact]
        public void RetrieveUserId_NullUser_SetsFlagFalse()
        {
            //Arrange 1
            object?[] boxed = { true };

            //Arrange 2
            SetupUserWithNullPrincipal();

            //Act
            var result = typeof(EditServiceService)
                .GetMethod("RetrieveUserId", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_sut, boxed);

            //Assert
            boxed[0].Should().Be(false);
            result.Should().Be(Guid.Empty);
        }

        [Fact]
        public void RetrieveUserId_NoClaim_SetsFlagFalse()
        {
            //Arrange 1
            object?[] boxed = { true };

            //Arrange 2
            // ClaimsPrincipal with no NameIdentifier claim.
            var context = new DefaultHttpContext();
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

            //Act
            var result = typeof(EditServiceService)
                .GetMethod("RetrieveUserId", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_sut, boxed);

            //Assert
            boxed[0].Should().Be(false);
            result.Should().Be(Guid.Empty);
        }

        [Fact]
        public void RetrieveUserId_InvalidGuidClaim_SetsFlagFalse()
        {
            //Arrange 1
            object?[] boxed = { true };

            //Arrange 2
            SetupClaim("not-a-guid");

            //Act
            var result = typeof(EditServiceService)
                .GetMethod("RetrieveUserId", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_sut, boxed);

            //Assert
            boxed[0].Should().Be(false);
            result.Should().Be(Guid.Empty);
        }

        [Fact]
        public void RetrieveUserId_ValidGuidClaim_ReturnsParsedGuid()
        {
            //Arrange 1
            object?[] boxed = { false };

            //Arrange 2
            SetupClaim(UserId.ToString());

            //Act
            var result = typeof(EditServiceService)
                .GetMethod("RetrieveUserId", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_sut, boxed);

            //Assert
            boxed[0].Should().Be(true);
            result.Should().Be(UserId);
        }

        // ===================================================================
        // ============ RetrieveClinicId(...) – private ======================
        // ===================================================================

        [Fact]
        public async Task RetrieveClinicId_UserInvalid_ReturnsNullWithoutQueryingRepo()
        {
            //Arrange 1
            // (no request object needed; calling private helper directly)

            //Arrange 2
            // Staff repo never set up → would throw if invoked.

            //Act
            var result = await InvokePrivateAsync<Guid?>(_sut, "RetrieveClinicId", UserId, false);

            //Assert
            result.Should().BeNull();
            _staffClinicRepoMock.Verify(x => x.FindByCondition(
                It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task RetrieveClinicId_NoStaffRow_ReturnsNull()
        {
            //Arrange 1

            //Arrange 2
            SetupStaffClinic(null);

            //Act
            var result = await InvokePrivateAsync<Guid?>(_sut, "RetrieveClinicId", UserId, true);

            //Assert
            result.Should().BeNull();
            _staffClinicRepoMock.Verify(x => x.FindByCondition(
                It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task RetrieveClinicId_StaffRowExists_ReturnsClinicId()
        {
            //Arrange 1

            //Arrange 2
            SetupStaffClinic(ActiveStaffClinic());

            //Act
            var result = await InvokePrivateAsync<Guid?>(_sut, "RetrieveClinicId", UserId, true);

            //Assert
            result.Should().Be(ClinicId);
        }

        // ===================================================================
        // ============ ValidateClinicContext(...) – private =================
        // ===================================================================

        [Fact]
        public void ValidateClinicContext_NullClinicId_FlagsFalse()
        {
            //Arrange 1

            //Arrange 2

            //Act
            var flag = InvokeRefBool(_sut, "ValidateClinicContext", (Guid?)null);

            //Assert
            flag.Should().Be(false);
        }

        [Fact]
        public void ValidateClinicContext_HasClinicId_LeavesFlagTrue()
        {
            //Arrange 1

            //Arrange 2

            //Act
            var flag = InvokeRefBool(_sut, "ValidateClinicContext", (Guid?)ClinicId);

            //Assert
            flag.Should().Be(true);
        }

        // ===================================================================
        // ============ RetrieveServiceEntity(...) – private =================
        // ===================================================================

        [Fact]
        public async Task RetrieveServiceEntity_ClinicInvalid_ReturnsNullWithoutRepoCall()
        {
            //Arrange 1

            //Arrange 2
            // serviceRepo not set up → would throw if invoked.

            //Act
            var result = await InvokePrivateAsync<Service?>(_sut, "RetrieveServiceEntity", ServiceId, false);

            //Assert
            result.Should().BeNull();
            _serviceRepoMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task RetrieveServiceEntity_ClinicValid_ReturnsRepoResult()
        {
            //Arrange 1
            var service = ExistingService();

            //Arrange 2
            SetupGetById(service);

            //Act
            var result = await InvokePrivateAsync<Service?>(_sut, "RetrieveServiceEntity", ServiceId, true);

            //Assert
            result.Should().BeSameAs(service);
            _serviceRepoMock.Verify(x => x.GetByIdAsync(ServiceId), Times.Once);
        }

        // ===================================================================
        // ============ ValidateServiceExistence(...) – private ==============
        // ===================================================================

        [Fact]
        public void ValidateServiceExistence_NullService_FlagsFalse()
        {
            //Arrange 1

            //Arrange 2

            //Act
            var flag = InvokeRefBool(_sut, "ValidateServiceExistence", (Service?)null);

            //Assert
            flag.Should().Be(false);
        }

        [Fact]
        public void ValidateServiceExistence_NonNullService_LeavesFlagTrue()
        {
            //Arrange 1

            //Arrange 2

            //Act
            var flag = InvokeRefBool(_sut, "ValidateServiceExistence", ExistingService());

            //Assert
            flag.Should().Be(true);
        }

        // ===================================================================
        // ============ ValidateServiceOwnership(...) – private ==============
        // ===================================================================

        [Fact]
        public void ValidateServiceOwnership_ServiceNotExist_FlagsFalse()
        {
            //Arrange 1

            //Arrange 2

            //Act
            var flag = InvokeRefBool(_sut, "ValidateServiceOwnership", (Service?)null, (Guid?)ClinicId, false);

            //Assert
            flag.Should().Be(false);
        }

        [Fact]
        public void ValidateServiceOwnership_ClinicMismatch_FlagsFalse()
        {
            //Arrange 1
            var service = ExistingService(clinicIdOverride: OtherClinicId);

            //Arrange 2

            //Act
            var flag = InvokeRefBool(_sut, "ValidateServiceOwnership", service, (Guid?)ClinicId, true);

            //Assert
            flag.Should().Be(false);
        }

        [Fact]
        public void ValidateServiceOwnership_ServiceNullButExistFlagTrue_StillFlagsFalse()
        {
            //Arrange 1
            // After refactor: when isServiceExist=true but service is null,
            // the `service != null` check is false, so we keep the initial
            // `!isServiceExist` evaluation (which is false). ownershipBreached
            // stays false → isOwnershipValid remains true.

            //Arrange 2

            //Act
            var flag = InvokeRefBool(_sut, "ValidateServiceOwnership", (Service?)null, (Guid?)ClinicId, true);

            //Assert
            flag.Should().Be(true);
        }

        [Fact]
        public void ValidateServiceOwnership_ServiceValidButClinicIdNull_LeavesFlagTrue()
        {
            //Arrange 1
            // Exercises the `clinicId.HasValue` branch in the new
            // `isServiceExist && service != null && clinicId.HasValue` guard.
            // When clinicId is null, the guard short-circuits and the
            // lifted `!=` comparison is never evaluated, so ownershipBreached
            // stays at its initial value (false).
            var service = ExistingService();

            //Arrange 2

            //Act
            var flag = InvokeRefBool(_sut, "ValidateServiceOwnership", service, (Guid?)null, true);

            //Assert
            flag.Should().Be(true);
        }

        [Fact]
        public void ValidateServiceOwnership_AllValid_LeavesFlagTrue()
        {
            //Arrange 1
            var service = ExistingService();

            //Arrange 2

            //Act
            var flag = InvokeRefBool(_sut, "ValidateServiceOwnership", service, (Guid?)ClinicId, true);

            //Assert
            flag.Should().Be(true);
        }

        // ===================================================================
        // ============ VerifyServiceNameUniqueness(...) – private ===========
        // ===================================================================

        [Fact]
        public async Task VerifyServiceNameUniqueness_OwnershipInvalid_ReturnsFalseWithoutQuery()
        {
            //Arrange 1

            //Arrange 2
            // serviceRepo FindByCondition not set up → would throw if invoked.

            //Act
            var result = await InvokePrivateAsync<bool>(
                _sut, "VerifyServiceNameUniqueness",
                ServiceId, "Anything", (Guid?)ClinicId, false);

            //Assert
            result.Should().Be(false);
            _serviceRepoMock.Verify(x => x.FindByCondition(
                It.IsAny<Expression<Func<Service, bool>>>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task VerifyServiceNameUniqueness_DuplicateNameFound_ReturnsFalse()
        {
            //Arrange 1

            //Arrange 2
            SetupDuplicateNameQuery(true);

            //Act
            var result = await InvokePrivateAsync<bool>(
                _sut, "VerifyServiceNameUniqueness",
                ServiceId, "Updated Service", (Guid?)ClinicId, true);

            //Assert
            result.Should().Be(false);
        }

        [Fact]
        public async Task VerifyServiceNameUniqueness_NoConflict_ReturnsTrue()
        {
            //Arrange 1

            //Arrange 2
            SetupDuplicateNameQuery(false);

            //Act
            var result = await InvokePrivateAsync<bool>(
                _sut, "VerifyServiceNameUniqueness",
                ServiceId, "Updated Service", (Guid?)ClinicId, true);

            //Assert
            result.Should().Be(true);
        }

        // ===================================================================
        // ============ ApplyStateChanges(...) – private =====================
        // ===================================================================

        [Fact]
        public async Task ApplyStateChanges_CannotExecute_ReturnsWithoutCommit()
        {
            //Arrange 1
            var service = ExistingService();
            var beforeName = service.ServiceName;
            var beforePrice = service.Price;
            var beforeDuration = service.DurationMinutes;

            //Arrange 2

            //Act
            await InvokePrivateTaskAsync(_sut, "ApplyStateChanges",
                service, ValidRequest(), false);

            //Assert
            service.ServiceName.Should().Be(beforeName);
            service.Price.Should().Be(beforePrice);
            service.DurationMinutes.Should().Be(beforeDuration);
            _serviceRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Service>()), Times.Never);
            _serviceRepoMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task ApplyStateChanges_CanExecute_AppliesAndCommits()
        {
            //Arrange 1
            var service = ExistingService();
            var request = new EditServiceRequest
            {
                ServiceId = ServiceId,
                ServiceName = "  Final Name  ",
                Price = 999m,
                DurationMinutes = 60
            };

            //Arrange 2

            //Act
            await InvokePrivateTaskAsync(_sut, "ApplyStateChanges", service, request, true);

            //Assert
            service.ServiceName.Should().Be("Final Name");
            service.Price.Should().Be(999m);
            service.DurationMinutes.Should().Be(60);
            _serviceRepoMock.Verify(x => x.UpdateAsync(service), Times.Once);
            _serviceRepoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        // ===================================================================
        // ============ MapToResponseDto(...) – private ======================
        // ===================================================================

        [Fact]
        public void MapToResponseDto_NullService_ReturnsNull()
        {
            //Arrange 1

            //Arrange 2

            //Act
            var result = InvokePrivate(_sut, "MapToResponseDto", (Service?)null);

            //Assert
            result.Should().BeNull();
        }

        [Fact]
        public void MapToResponseDto_NonNullService_MapsAllFields()
        {
            //Arrange 1
            var service = ExistingService();

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "MapToResponseDto", service);
            var result = raw.Should().BeOfType<EditServiceResponse>().Subject;

            //Assert
            result.ServiceId.Should().Be(ServiceId.ToString());
            result.ClinicId.Should().Be(ClinicId.ToString());
            result.ServiceName.Should().Be("Old Name");
            result.Price.Should().Be(100m);
            result.DurationMinutes.Should().Be(15);
            result.UpdatedAt.Should().NotBeNullOrEmpty();
            // Format check: "dd/MM/yyyy HH:mm" → e.g., "26/07/2026 12:45"
            result.UpdatedAt.Should().MatchRegex(@"^\d{2}/\d{2}/\d{4} \d{2}:\d{2}$");
        }
    }
}