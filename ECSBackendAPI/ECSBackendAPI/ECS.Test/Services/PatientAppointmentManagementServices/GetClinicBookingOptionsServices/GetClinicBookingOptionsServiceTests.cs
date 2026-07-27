using System.Linq.Expressions;
using ECS.Application.Common.Response;
using ECS.Application.Services.PatientAppointmentManagementServices.GetClinicBookingOptionsServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.PatientAppointmentManagementServices.GetClinicBookingOptionsServices
{
    public class GetClinicDoctorsForBookingServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<Clinic, Guid, AppDbContext>> _clinicRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepoMock = new();

        private GetClinicDoctorsForBookingService CreateSut()
        {
            return new GetClinicDoctorsForBookingService(_clinicRepoMock.Object, _doctorRepoMock.Object);
        }

        private void SetupClinicRepository(IEnumerable<Clinic> clinics)
        {
            _clinicRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<Clinic, bool>>>(), It.IsAny<bool>()))
                .Returns((Expression<Func<Clinic, bool>> expression, bool trackChanges) =>
                {
                    var filteredList = clinics.AsQueryable().Where(expression).ToList();
                    return filteredList.BuildMockDbSet<Clinic>().Object;
                });
        }

        private void SetupDoctorRepository(IEnumerable<DoctorProfile> doctors)
        {
            _doctorRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<DoctorProfile, bool>>>(), It.IsAny<bool>()))
                .Returns((Expression<Func<DoctorProfile, bool>> expression, bool trackChanges) =>
                {
                    var filteredList = doctors.AsQueryable().Where(expression).ToList();
                    return filteredList.BuildMockDbSet<DoctorProfile>().Object;
                });
        }

        [Fact]
        public async Task Process_InactiveOrMissingClinic_ThrowsKeyNotFoundException()
        {
            // Arrange
            SetupClinicRepository(Array.Empty<Clinic>());
            SetupDoctorRepository(Array.Empty<DoctorProfile>());
            var sut = CreateSut();

            // Act
            Func<Task> act = async () => await sut.Process(GetClinicBookingOptionsMockData.ValidClinicId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task Process_ValidClinicWithNoDoctors_ReturnsSuccessWithEmptyList()
        {
            // Arrange
            var clinic = GetClinicBookingOptionsMockData.GetClinic();
            SetupClinicRepository(new[] { clinic });
            SetupDoctorRepository(Array.Empty<DoctorProfile>());
            var sut = CreateSut();

            // Act
            var result = await sut.Process(clinic.Id);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task Process_ValidClinicWithDoctors_ReturnsMappedDoctorOptions()
        {
            // Arrange
            var clinic = GetClinicBookingOptionsMockData.GetClinic();
            var doctors = new[]
            {
                GetClinicBookingOptionsMockData.GetDoctorProfile(
                    id: Guid.NewGuid(),
                    clinicId: clinic.Id,
                    userId: Guid.NewGuid(),
                    fullName: "Dr. B",
                    specialtyName: "Mắt",
                    experienceYears: 7),
                GetClinicBookingOptionsMockData.GetDoctorProfile(
                    id: Guid.NewGuid(),
                    clinicId: clinic.Id,
                    userId: Guid.NewGuid(),
                    fullName: "Dr. A",
                    specialtyName: "Nội tiết",
                    experienceYears: 3)
            };

            SetupClinicRepository(new[] { clinic });
            SetupDoctorRepository(doctors);
            var sut = CreateSut();

            // Act
            var result = await sut.Process(clinic.Id);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data![0].FullName.Should().Be("Dr. A");
            result.Data[0].SpecialtyName.Should().Be("Nội tiết");
            result.Data[0].ExperienceYears.Should().Be(3);
            result.Data[1].FullName.Should().Be("Dr. B");
            result.Data[1].SpecialtyName.Should().Be("Mắt");
            result.Data[1].ExperienceYears.Should().Be(7);
        }
        [Fact]
        public async Task Process_ValidClinicWithDoctorHavingNullSpecialty_ExecutesNullSpecialtyBranch_ReturnsDoctorOptionWithNullSpecialtyName()
        {
            // Arrange
            var clinic = GetClinicBookingOptionsMockData.GetClinic();

            // Tạo DoctorProfile không truyền specialtyName => Specialty sẽ bị NULL
            var doctorWithoutSpecialty = GetClinicBookingOptionsMockData.GetDoctorProfile(
                id: Guid.NewGuid(),
                clinicId: clinic.Id,
                userId: Guid.NewGuid(),
                fullName: "Dr. No Specialty",
                specialtyName: null,
                experienceYears: 4);

            SetupClinicRepository(new[] { clinic });
            SetupDoctorRepository(new[] { doctorWithoutSpecialty });
            var sut = CreateSut();

            // Act
            var result = await sut.Process(clinic.Id);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);

            // Phủ nhánh d.Specialty == null -> SpecialtyName nhận giá trị null (dòng 113)
            result.Data![0].FullName.Should().Be("Dr. No Specialty");
            result.Data[0].SpecialtyName.Should().BeNull();
        }
    }
}
