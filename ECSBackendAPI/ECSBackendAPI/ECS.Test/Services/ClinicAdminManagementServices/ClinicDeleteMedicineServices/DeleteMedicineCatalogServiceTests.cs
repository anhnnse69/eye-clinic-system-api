using ECS.Application.Services.ClinicAdminManagementServices.ClinicDeleteMedicineServices;
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

namespace ECS.Test.Services.ClinicAdminManagementServices.ClinicDeleteMedicineServices
{
    public class DeleteMedicineCatalogServiceTests
    {
        private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid ClinicId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid MedicineId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        private readonly Mock<IRepositoryBaseAsync<MedicineCatalog, Guid, AppDbContext>> _medicineRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly DeleteMedicineCatalogService _sut;

        public DeleteMedicineCatalogServiceTests()
        {
            _sut = new DeleteMedicineCatalogService(
                _medicineRepoMock.Object,
                _staffClinicRepoMock.Object,
                _httpContextAccessorMock.Object);
        }

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

        private void SetupStaffClinic(StaffClinic? staffClinic)
        {
            var rows = staffClinic == null ? new List<StaffClinic>() : new List<StaffClinic> { staffClinic };
            var queryable = rows.BuildMockDbSet();
            _staffClinicRepoMock.Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()))
                .Returns(queryable.Object);
        }

        private void SetupMedicine(MedicineCatalog? medicine)
        {
            var rows = medicine == null ? new List<MedicineCatalog>() : new List<MedicineCatalog> { medicine };
            var queryable = rows.BuildMockDbSet();
            _medicineRepoMock.Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<MedicineCatalog, bool>>>(), It.IsAny<bool>()))
                .Returns(queryable.Object);
        }

        private static StaffClinic ActiveStaffClinic() => new()
        {
            Id = Guid.NewGuid(), UserId = UserId, ClinicId = ClinicId, IsActive = true
        };

        private static MedicineCatalog Medicine(bool isActive) => new()
        {
            Id = MedicineId, ClinicId = ClinicId, MedicineName = "Paracetamol", IsActive = isActive
        };

        [Fact]
        public async Task Process_NullHttpContext_ReturnsAuthenticationError()
        {
            //Arrange 1
            var request = new DeleteMedicineCatalogRequest { Id = MedicineId };

            //Arrange 2
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            _staffClinicRepoMock.Verify(x => x.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()), Times.Never);
            _medicineRepoMock.Verify(x => x.UpdateAsync(It.IsAny<MedicineCatalog>()), Times.Never);
            _medicineRepoMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_MissingNameIdentifierClaim_ReturnsAuthenticationError()
        {
            //Arrange 1
            var request = new DeleteMedicineCatalogRequest { Id = MedicineId };

            //Arrange 2
            SetupClaim(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            _staffClinicRepoMock.Verify(x => x.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Process_InvalidUserIdClaim_ReturnsAuthenticationError()
        {
            //Arrange 1
            var request = new DeleteMedicineCatalogRequest { Id = MedicineId };

            //Arrange 2
            SetupClaim("invalid-guid");

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            _medicineRepoMock.Verify(x => x.FindByCondition(It.IsAny<Expression<Func<MedicineCatalog, bool>>>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Process_ValidUserWithoutActiveClinic_ReturnsClinicNotFoundError()
        {
            //Arrange 1
            var request = new DeleteMedicineCatalogRequest { Id = MedicineId };

            //Arrange 2
            SetupClaim(UserId.ToString());
            SetupStaffClinic(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
            result.Data.Should().BeNull();
            _staffClinicRepoMock.Verify(x => x.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()), Times.Once);
            _medicineRepoMock.Verify(x => x.FindByCondition(It.IsAny<Expression<Func<MedicineCatalog, bool>>>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Process_ValidClinicWithoutMedicine_ReturnsMedicineNotFoundError()
        {
            //Arrange 1
            var request = new DeleteMedicineCatalogRequest { Id = MedicineId };

            //Arrange 2
            SetupClaim(UserId.ToString());
            SetupStaffClinic(ActiveStaffClinic());
            SetupMedicine(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
            result.Data.Should().BeNull();
            _medicineRepoMock.Verify(x => x.FindByCondition(It.IsAny<Expression<Func<MedicineCatalog, bool>>>(), It.IsAny<bool>()), Times.Once);
            _medicineRepoMock.Verify(x => x.UpdateAsync(It.IsAny<MedicineCatalog>()), Times.Never);
            _medicineRepoMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_ActiveMedicine_TogglesInactiveAndReturnsSuccess()
        {
            //Arrange 1
            var request = new DeleteMedicineCatalogRequest { Id = MedicineId };
            var medicine = Medicine(true);
            var before = DateTime.UtcNow;

            //Arrange 2
            SetupClaim(UserId.ToString());
            SetupStaffClinic(ActiveStaffClinic());
            SetupMedicine(medicine);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(MedicineId);
            result.Data.MedicineName.Should().Be("Paracetamol");
            result.Data.IsActive.Should().BeFalse();
            result.Data.UpdatedAt.Should().BeOnOrAfter(before);
            _medicineRepoMock.Verify(x => x.UpdateAsync(It.Is<MedicineCatalog>(m => !m.IsActive)), Times.Once);
            _medicineRepoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Process_InactiveMedicine_TogglesActiveAndReturnsSuccess()
        {
            //Arrange 1
            var request = new DeleteMedicineCatalogRequest { Id = MedicineId };
            var medicine = Medicine(false);

            //Arrange 2
            SetupClaim(UserId.ToString());
            SetupStaffClinic(ActiveStaffClinic());
            SetupMedicine(medicine);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.IsActive.Should().BeTrue();
            _medicineRepoMock.Verify(x => x.UpdateAsync(It.Is<MedicineCatalog>(m => m.IsActive)), Times.Once);
            _medicineRepoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }
    }
}
