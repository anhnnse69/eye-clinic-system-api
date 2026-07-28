using System.Linq.Expressions;
using ECS.Application.Services.SystemAdminServices.GetClinicLookupServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.SystemAdminServices.GetClinicLookupServices
{
    /// <summary>
    /// Unit tests for <see cref="GetClinicLookupService"/>.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// </summary>
    public class GetClinicLookupServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<Clinic, Guid, AppDbContext>> _clinicRepoMock = new();
        private readonly GetClinicLookupService _sut;

        public GetClinicLookupServiceTests()
        {
            _sut = new GetClinicLookupService(_clinicRepoMock.Object);
        }

        private void SetupQueries(List<Clinic> clinics)
        {
            _clinicRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<Clinic, bool>>>(), false))
                .Returns((Expression<Func<Clinic, bool>> predicate, bool _) =>
                {
                    // Thêm .ToList() để chuyển từ IQueryable<Clinic> thành List<Clinic> (ICollection<Clinic>)
                    var filtered = clinics.AsQueryable().Where(predicate).ToList();
                    return filtered.BuildMockDbSet().Object;
                });
        }

        [Fact]
        public async Task Process_ActiveClinicsWithoutStaffExist_Returns2000WithFilteredAndSortedClinics()
        {
            // Arrange
            var request = SystemAdminClinicMockData.GetLookupRequest();
            var sampleClinics = SystemAdminClinicMockData.GetSampleClinicsForLookup();
            SetupQueries(sampleClinics);

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);

            // Kiểm tra sắp xếp theo Tên (A Clinic đứng trước B Clinic)
            result.Data![0].Id.Should().Be(SystemAdminClinicMockData.LookupClinic1Id);
            result.Data[0].Name.Should().Be("A Clinic");

            result.Data[1].Id.Should().Be(SystemAdminClinicMockData.LookupClinic2Id);
            result.Data[1].Name.Should().Be("B Clinic");

            _clinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Clinic, bool>>>(), false),
                Times.Once
            );
        }

        [Fact]
        public async Task Process_NoActiveClinicsWithoutStaff_Returns2000WithEmptyList()
        {
            // Arrange
            var request = SystemAdminClinicMockData.GetLookupRequest();
            SetupQueries(new List<Clinic>());

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();

            _clinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Clinic, bool>>>(), false),
                Times.Once
            );
        }
    }
}