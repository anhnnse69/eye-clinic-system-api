using System.Linq.Expressions;
using ECS.Domain.Entities.Patient;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentAssertions;
using Moq;

namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetPatientsListServices.Tests
{
    public class ReceptionistGetPatientsListServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<PatientProfile, Guid, AppDbContext>> _patientQueryRepoMock;
        private readonly ReceptionistGetPatientsListService _sut;

        public ReceptionistGetPatientsListServiceTests()
        {
            _patientQueryRepoMock = new Mock<IRepositoryQueryBase<PatientProfile, Guid, AppDbContext>>();
            _sut = new ReceptionistGetPatientsListService(_patientQueryRepoMock.Object);
        }

        private void SetupPatientQueryRepo(List<PatientProfile> databaseData)
        {
            _patientQueryRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), false))
                .Returns((Expression<Func<PatientProfile, bool>> predicate, bool trackChanges) =>
                {
                    return databaseData.AsQueryable().Where(predicate);
                });
        }

        [Fact]
        public async Task Process_NoFilters_ReturnsAllPatientsPaginatedAndSortedDescending()
        {
            // Arrange
            var patients = ReceptionistGetPatientsListMockData.GetPatientProfilesList();
            SetupPatientQueryRepo(patients);
            var request = ReceptionistGetPatientsListMockData.GetDefaultRequest();

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.CodeMessage.Should().Be("APP_MESSAGE_2000");
            response.Data.Should().HaveCount(3);
            response.Meta.Should().NotBeNull();
            response.Meta!.Total.Should().Be(3);
            response.Data![0].FullName.Should().Be("Le Van C");
            response.Data[1].FullName.Should().Be("Tran Thi B");
            response.Data[2].FullName.Should().Be("Nguyen Van A");
            response.Data[2].Gender.Should().Be("MALE");
            response.Data[2].Dob.Should().Be("1990-01-01");
            response.Data[2].CreatedAt.Should().Be("2026-01-01T10:00:00Z");
        }

        [Fact]
        public async Task Process_FilterByNameOnly_ReturnsMatchingPatients()
        {
            // Arrange
            var patients = ReceptionistGetPatientsListMockData.GetPatientProfilesList();
            SetupPatientQueryRepo(patients);
            var request = ReceptionistGetPatientsListMockData.GetDefaultRequest();
            request.SearchName = "  van  ";

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.Data.Should().HaveCount(2);
            response.Meta!.Total.Should().Be(2);
            response.Data.Select(x => x.FullName).Should().Contain(new[] { "Nguyen Van A", "Le Van C" });
        }

        [Fact]
        public async Task Process_FilterByPhoneOnly_ReturnsMatchingPatients()
        {
            // Arrange
            var patients = ReceptionistGetPatientsListMockData.GetPatientProfilesList();
            SetupPatientQueryRepo(patients);
            var request = ReceptionistGetPatientsListMockData.GetDefaultRequest();
            request.SearchPhone = " 0912 ";

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.Data.Should().HaveCount(1);
            response.Data![0].FullName.Should().Be("Tran Thi B");
        }

        [Fact]
        public async Task Process_FilterByNameAndPhoneCombined_ReturnsMatchingPatients()
        {
            // Arrange
            var patients = ReceptionistGetPatientsListMockData.GetPatientProfilesList();
            SetupPatientQueryRepo(patients);
            var request = ReceptionistGetPatientsListMockData.GetDefaultRequest();
            request.SearchName = "Nguyen";
            request.SearchPhone = "0901";

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.Data.Should().HaveCount(1);
            response.Data![0].FullName.Should().Be("Nguyen Van A");
        }

        [Fact]
        public async Task Process_Pagination_ReturnsCorrectPageSlice()
        {
            // Arrange
            var patients = ReceptionistGetPatientsListMockData.GetPatientProfilesList();
            SetupPatientQueryRepo(patients);
            var request = new ReceptionistGetPatientsListRequest
            {
                CurrentUserId = ReceptionistGetPatientsListMockData.ReceptionistUserId,
                PageNumber = 2,
                PageSize = 2
            };

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.Data.Should().HaveCount(1);
            response.Meta!.Total.Should().Be(3);
            response.Data![0].FullName.Should().Be("Nguyen Van A");
        }

        [Fact]
        public async Task Process_EmptyDatabase_ReturnsEmptyListAndZeroMeta()
        {
            // Arrange
            SetupPatientQueryRepo(new List<PatientProfile>());
            var request = ReceptionistGetPatientsListMockData.GetDefaultRequest();

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.Data.Should().BeEmpty();
            response.Meta!.Total.Should().Be(0);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "   ")]
        public async Task Process_WhitespaceOrNullFilters_IgnoredAndReturnsAll(string? name, string? phone)
        {
            // Arrange
            var patients = ReceptionistGetPatientsListMockData.GetPatientProfilesList();
            SetupPatientQueryRepo(patients);
            var request = ReceptionistGetPatientsListMockData.GetDefaultRequest();
            request.SearchName = name;
            request.SearchPhone = phone;

            // Act
            var response = await _sut.Process(request);

            // Assert
            response.Should().NotBeNull();
            response.Data.Should().HaveCount(3);
        }
    }
}