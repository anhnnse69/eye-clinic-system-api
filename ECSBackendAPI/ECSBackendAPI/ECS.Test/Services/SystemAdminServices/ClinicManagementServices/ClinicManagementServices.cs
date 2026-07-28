using System.Linq.Expressions;
using ECS.Application.Services.SystemAdminServices.ClinicManagementServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.SystemAdminServices.ClinicManagementServices
{
    /// <summary>
    /// Unit tests for <see cref="GetClinicsService"/>.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// </summary>
    public class GetClinicsServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<Clinic, Guid, AppDbContext>> _queryRepoMock = new();
        private readonly GetClinicsService _sut;

        public GetClinicsServiceTests()
        {
            _sut = new GetClinicsService(_queryRepoMock.Object);
        }

        private void SetupQueryRepo(List<Clinic> clinics)
        {
            _queryRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<Clinic, bool>>>(), It.IsAny<bool>()))
                .Returns((Expression<Func<Clinic, bool>> predicate, bool _) =>
                {
                    return clinics.Where(predicate.Compile()).ToList().BuildMockDbSet().Object;
                });
        }

        [Fact]
        public async Task Process_NoFilter_ReturnsAllClinicsWithPaginationMeta()
        {
            // Arrange
            var clinics = SystemAdminClinicMockData.GetListOfClinicsForGetClinics();
            SetupQueryRepo(clinics);
            var request = SystemAdminClinicMockData.GetDefaultGetClinicsRequest();

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().HaveCount(3);
            result.Meta.Should().NotBeNull();
        }

        [Fact]
        public async Task Process_FilterByActiveStatus_ReturnsOnlyActiveClinics()
        {
            // Arrange
            var clinics = SystemAdminClinicMockData.GetListOfClinicsForGetClinics();
            SetupQueryRepo(clinics);
            var request = SystemAdminClinicMockData.GetDefaultGetClinicsRequest();
            request.Status = "ACTIVE";

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data.Should().AllSatisfy(c => c.Status.Should().Be("ACTIVE"));
        }

        [Fact]
        public async Task Process_FilterByInactiveStatus_ReturnsOnlyInactiveClinics()
        {
            // Arrange
            var clinics = SystemAdminClinicMockData.GetListOfClinicsForGetClinics();
            SetupQueryRepo(clinics);
            var request = SystemAdminClinicMockData.GetDefaultGetClinicsRequest();
            request.Status = "INACTIVE";

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data.First().Status.Should().Be("INACTIVE");
        }

        [Fact]
        public async Task Process_FilterByStatusAndSearchTermName_ReturnsMatchingActiveClinic()
        {
            // Arrange
            var clinics = SystemAdminClinicMockData.GetListOfClinicsForGetClinics();
            SetupQueryRepo(clinics);
            var request = SystemAdminClinicMockData.GetDefaultGetClinicsRequest();
            request.Status = "ACTIVE";
            request.SearchTerm = "published";

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data.First().ClinicName.Should().Be("Published Eye Clinic");
        }

        [Fact]
        public async Task Process_FilterByStatusAndSearchTermId_ReturnsMatchingActiveClinic()
        {
            // Arrange
            var clinics = SystemAdminClinicMockData.GetListOfClinicsForGetClinics();
            SetupQueryRepo(clinics);
            var request = SystemAdminClinicMockData.GetDefaultGetClinicsRequest();
            request.Status = "ACTIVE";
            request.SearchTerm = SystemAdminClinicMockData.ClinicId.ToString().Substring(0, 8);

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data.First().Id_clinic.Should().Be(SystemAdminClinicMockData.ClinicId.ToString());
        }

        [Fact]
        public async Task Process_FilterBySearchTermNameOnly_ReturnsMatchingClinic()
        {
            // Arrange
            var clinics = SystemAdminClinicMockData.GetListOfClinicsForGetClinics();
            SetupQueryRepo(clinics);
            var request = SystemAdminClinicMockData.GetDefaultGetClinicsRequest();
            request.SearchTerm = "closed";

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data.First().ClinicName.Should().Be("Closed Eye Clinic");
        }

        [Fact]
        public async Task Process_FilterBySearchTermIdOnly_ReturnsMatchingClinic()
        {
            // Arrange
            var clinics = SystemAdminClinicMockData.GetListOfClinicsForGetClinics();
            SetupQueryRepo(clinics);
            var request = SystemAdminClinicMockData.GetDefaultGetClinicsRequest();
            request.SearchTerm = SystemAdminClinicMockData.SecondaryClinicId.ToString().Substring(0, 8);

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data.First().Id_clinic.Should().Be(SystemAdminClinicMockData.SecondaryClinicId.ToString());
        }

        [Fact]
        public async Task Process_ClinicWithNullOrEmptyContact_ReturnsNAForPhoneAndEmail()
        {
            // Arrange
            var clinics = SystemAdminClinicMockData.GetListOfClinicsForGetClinics();
            SetupQueryRepo(clinics);
            var request = SystemAdminClinicMockData.GetDefaultGetClinicsRequest();
            request.SearchTerm = "no contact info";

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            var dto = result.Data.First();
            dto.ContactPhone.Should().Be("N/A");
            dto.ContactEmail.Should().Be("N/A");
            dto.Status.Should().Be("ACTIVE");
        }

        [Fact]
        public async Task Process_Pagination_ReturnsCorrectPageAndPageSize()
        {
            // Arrange
            var clinics = SystemAdminClinicMockData.GetListOfClinicsForGetClinics();
            SetupQueryRepo(clinics);
            var request = new GetClinicsRequest
            {
                PageNumber = 2,
                PageSize = 1,
                Status = null,
                SearchTerm = null
            };

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Meta.Should().NotBeNull();
        }
    }
}