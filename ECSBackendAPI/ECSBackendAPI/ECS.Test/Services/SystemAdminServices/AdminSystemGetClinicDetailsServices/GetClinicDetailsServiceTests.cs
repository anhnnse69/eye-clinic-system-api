using ECS.Application.Services.SystemAdminServices.AdminSystemGetClinicDetailsServices;
using ECS.Domain.Entities.Clinics;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Moq;

namespace ECS.Test.Services.SystemAdminServices.AdminSystemGetClinicDetailsServices
{
    /// <summary>
    /// Unit tests for <see cref="GetClinicDetailsService"/>.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// </summary>
    public class GetClinicDetailsServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<Clinic, Guid, AppDbContext>> _queryRepoMock = new();
        private readonly GetClinicDetailsService _sut;

        public GetClinicDetailsServiceTests()
        {
            _sut = new GetClinicDetailsService(_queryRepoMock.Object);
        }

        private void SetupGetById(Clinic? clinic)
        {
            _queryRepoMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(clinic);
        }

        [Fact]
        public async Task Process_ClinicNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var clinicId = SystemAdminClinicMockData.ClinicId;

            //Arrange 2
            SetupGetById(null);

            //Act
            var act = () => _sut.Process(clinicId);

            //Assert
            var exception = await act.Should().ThrowAsync<KeyNotFoundException>();
            exception.Which.Message.Should().Be($"Không tìm thấy phòng khám với ID: {clinicId}");
        }

        [Fact]
        public async Task Process_ValidClinic_Returns2000WithMappedResponse()
        {
            //Arrange 1
            var clinic = SystemAdminClinicMockData.GetClinicWithFullDetails();

            //Arrange 2
            SetupGetById(clinic);

            //Act
            var result = await _sut.Process(clinic.Id);

            //Assert
            result.CodeMessage.Should().Be("APP_MESSAGE_2000");
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be(clinic.Name);
            result.Data.Address.Should().Be(clinic.Address);
            result.Data.Phone.Should().Be(clinic.Phone);
            result.Data.Email.Should().Be(clinic.Email);
            result.Data.LogoUrl.Should().Be(clinic.LogoUrl);
            result.Data.Description.Should().Be(clinic.Description);
            result.Data.IsActive.Should().BeTrue();
            result.Data.RatingAvg.Should().Be(clinic.RatingAvg ?? 0);
            result.Data.ReviewCount.Should().Be(clinic.ReviewCount ?? 0);
        }

        [Fact]
        public async Task Process_ClinicWithNullOptionalFields_MapsEmptyDefaults()
        {
            //Arrange 1
            var clinic = SystemAdminClinicMockData.GetClinicWithNullOptionalFields();

            //Arrange 2
            SetupGetById(clinic);

            //Act
            var result = await _sut.Process(clinic.Id);

            //Assert
            result.CodeMessage.Should().Be("APP_MESSAGE_2000");
            result.Data.Should().NotBeNull();
            result.Data!.Email.Should().Be("");
            result.Data.LogoUrl.Should().Be("");
            result.Data.Description.Should().Be("");
            result.Data.RatingAvg.Should().Be(0);
            result.Data.ReviewCount.Should().Be(0);
        }
    }
}
