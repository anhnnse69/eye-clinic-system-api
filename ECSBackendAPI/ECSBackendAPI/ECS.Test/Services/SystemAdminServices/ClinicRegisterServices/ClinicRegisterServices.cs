using System.Linq.Expressions;
using ECS.Application.Common.Response;
using ECS.Application.Services.SystemAdminServices.ClinicRegisterServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.SystemAdminServices.ClinicRegisterServices
{
    /// <summary>
    /// Unit tests for <see cref="GetClinicApplicationService"/>.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// Target: 100% Line & Branch Coverage.
    /// </summary>
    public class GetClinicApplicationServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<ClinicRegistrationRequest, Guid, AppDbContext>> _queryRepoMock = new();
        private readonly GetClinicApplicationService _sut;

        public GetClinicApplicationServiceTests()
        {
            _sut = new GetClinicApplicationService(_queryRepoMock.Object);
        }

        private void SetupRepository(List<ClinicRegistrationRequest> data)
        {
            var mockDbSet = data.BuildMockDbSet();

            _queryRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<ClinicRegistrationRequest, bool>>>(),
                    It.IsAny<bool>()))
                .Returns((Expression<Func<ClinicRegistrationRequest, bool>> predicate, bool _) =>
                {
                    return mockDbSet.Object.Where(predicate);
                });
        }

        [Fact]
        public async Task Process_NoStatusAndNoSearchTerm_ReturnsAllSortedAndPaginated()
        {
            // Arrange 1
            var request = SystemAdminClinicMockData.GetDefaultGetClinicApplicationsRequest();
            request.PageSize = 2; // Test pagination limit
            var dataset = SystemAdminClinicMockData.GetListOfClinicRegistrationRequests();

            // Arrange 2
            SetupRepository(dataset);

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().HaveCount(2);

            // Verify Descending Order by RequestedAt: Hanoi (-1 day) -> Saigon (-2 days)
            result.Data![0].ClinicName.Should().Be("Hanoi Dental Care");
            result.Data[0].Status.Should().Be("APPROVED");
            result.Data[1].ClinicName.Should().Be("Saigon Eye Clinic");
            result.Data[1].Status.Should().Be("PENDING");

            // Verify Metadata via MetaResponse instance comparison
            result.Meta.Should().NotBeNull();
            result.Meta.Should().BeEquivalentTo(new MetaResponse(1, 2, 3));
        }

        [Fact]
        public async Task Process_StatusOnlyFilter_ReturnsMatchingStatusApplications()
        {
            // Arrange 1
            var request = SystemAdminClinicMockData.GetDefaultGetClinicApplicationsRequest();
            request.Status = "approved"; // Test ToUpper conversion
            var dataset = SystemAdminClinicMockData.GetListOfClinicRegistrationRequests();

            // Arrange 2
            SetupRepository(dataset);

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().HaveCount(1);
            result.Data![0].ClinicName.Should().Be("Hanoi Dental Care");
            result.Data[0].Status.Should().Be("APPROVED");

            // Verify Metadata
            result.Meta.Should().NotBeNull();
            result.Meta.Should().BeEquivalentTo(new MetaResponse(1, 10, 1));
        }

        [Fact]
        public async Task Process_SearchTermOnlyMatchingClinicName_ReturnsMatchingApplications()
        {
            // Arrange 1
            var request = SystemAdminClinicMockData.GetDefaultGetClinicApplicationsRequest();
            request.SearchTerm = "saigon"; // Case-insensitive search
            var dataset = SystemAdminClinicMockData.GetListOfClinicRegistrationRequests();

            // Arrange 2
            SetupRepository(dataset);

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().HaveCount(1);
            result.Data![0].ClinicName.Should().Be("Saigon Eye Clinic");
            result.Data[0].ContactEmail.Should().Be("contact@saigoneye.vn");

            // Verify Metadata
            result.Meta.Should().NotBeNull();
            result.Meta.Should().BeEquivalentTo(new MetaResponse(1, 10, 1));
        }

        [Fact]
        public async Task Process_SearchTermOnlyMatchingGuidId_ReturnsMatchingApplications()
        {
            // Arrange 1
            var targetId = SystemAdminClinicMockData.ApplicationId;
            var request = SystemAdminClinicMockData.GetDefaultGetClinicApplicationsRequest();
            request.SearchTerm = targetId.ToString().Substring(0, 8); // Search by partial GUID
            var dataset = SystemAdminClinicMockData.GetListOfClinicRegistrationRequests();

            // Arrange 2
            SetupRepository(dataset);

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().HaveCount(1);
            result.Data![0].Id_clinic_registration.Should().Be(targetId.ToString());

            // Verify Metadata
            result.Meta.Should().NotBeNull();
            result.Meta.Should().BeEquivalentTo(new MetaResponse(1, 10, 1));
        }

        [Fact]
        public async Task Process_BothStatusAndSearchTerm_ReturnsMatchingApplications()
        {
            // Arrange 1
            var request = SystemAdminClinicMockData.GetDefaultGetClinicApplicationsRequest();
            request.Status = "PENDING";
            request.SearchTerm = "da nang";
            var dataset = SystemAdminClinicMockData.GetListOfClinicRegistrationRequests();

            // Arrange 2
            SetupRepository(dataset);

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().HaveCount(1);
            result.Data![0].ClinicName.Should().Be("Da Nang Health Center");
            result.Data[0].Status.Should().Be("PENDING");

            // Verify Metadata
            result.Meta.Should().NotBeNull();
            result.Meta.Should().BeEquivalentTo(new MetaResponse(1, 10, 1));
        }
    }
}