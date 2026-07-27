using ECS.Application.Services.PatientAppointmentManagementServices.SubmitFeedbackServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Feedbacks;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace ECS.Test.Services.PatientAppointmentManagementServices.SubmitFeedbackServices
{
    public class SubmitFeedbackServiceTest
    {
        private readonly Mock<IRepositoryBaseAsync<Feedback, Guid, AppDbContext>> _mockFeedbackRepository = new();
        private readonly Mock<IRepositoryBaseAsync<Appointment, Guid, AppDbContext>> _mockAppointmentRepository = new();
        private readonly Mock<IRepositoryBaseAsync<DoctorProfile, Guid, AppDbContext>> _mockDoctorRepository = new();
        private readonly Mock<IRepositoryBaseAsync<Clinic, Guid, AppDbContext>> _mockClinicRepository = new();
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor = new();

        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private SubmitFeedbackService CreateSut(AppDbContext context, IHttpContextAccessor? httpContextAccessor = null)
        {
            httpContextAccessor ??= _mockHttpContextAccessor.Object;
            return new SubmitFeedbackService(
                _mockFeedbackRepository.Object,
                _mockAppointmentRepository.Object,
                _mockDoctorRepository.Object,
                _mockClinicRepository.Object,
                context,
                httpContextAccessor);
        }

        private void SetupAppointmentRepository(IEnumerable<Appointment> appointments)
        {
            _mockAppointmentRepository
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<bool>()))
                .Returns((Expression<Func<Appointment, bool>> expression, bool trackChanges) =>
                {
                    var filteredList = appointments.AsQueryable().Where(expression).ToList();
                    return filteredList.BuildMockDbSet().Object;
                });
        }

        #region Error Scenarios (Validation Checks)

        [Fact]
        public async Task Process_ShouldReturn4001_WhenUserIsNotAuthenticatedOrUserIdInvalid()
        {
            // Arrange
            var context = CreateContext();
            var httpContextAccessor = SubmitFeedbackServiceMockData.CreateMockHttpContextAccessor(null).Object;
            SetupAppointmentRepository(Array.Empty<Appointment>());

            var sut = CreateSut(context, httpContextAccessor);
            var request = SubmitFeedbackServiceMockData.CreateValidRequest();

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task Process_ShouldReturn4046_WhenAppointmentDoesNotExist()
        {
            // Arrange
            var context = CreateContext();
            var httpContextAccessor = SubmitFeedbackServiceMockData
                .CreateMockHttpContextAccessor(SubmitFeedbackServiceMockData.ValidUserId).Object;

            SetupAppointmentRepository(Array.Empty<Appointment>());

            var sut = CreateSut(context, httpContextAccessor);
            var request = SubmitFeedbackServiceMockData.CreateValidRequest();

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4046.ToString());
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task Process_ShouldReturn4053_WhenUserHasNoAccessToAppointment()
        {
            // Arrange
            var context = CreateContext();
            var userId = SubmitFeedbackServiceMockData.ValidUserId;
            var otherUserId = SubmitFeedbackServiceMockData.OtherUserId;
            var patientId = SubmitFeedbackServiceMockData.PatientId;

            var httpContextAccessor = SubmitFeedbackServiceMockData.CreateMockHttpContextAccessor(userId).Object;

            var appointment = SubmitFeedbackServiceMockData.CreateAppointment(
                patientId: patientId,
                createdById: otherUserId);

            SetupAppointmentRepository(new[] { appointment });

            var sut = CreateSut(context, httpContextAccessor);
            var request = SubmitFeedbackServiceMockData.CreateValidRequest();

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4053.ToString());
        }

        [Fact]
        public async Task Process_ShouldReturn4055_WhenAppointmentStatusIsNotCompleted()
        {
            // Arrange
            var context = CreateContext();
            var userId = SubmitFeedbackServiceMockData.ValidUserId;
            var httpContextAccessor = SubmitFeedbackServiceMockData.CreateMockHttpContextAccessor(userId).Object;

            var appointment = SubmitFeedbackServiceMockData.CreateAppointment(
                createdById: userId,
                status: AppointmentStatus.PENDING);

            SetupAppointmentRepository(new[] { appointment });

            var sut = CreateSut(context, httpContextAccessor);
            var request = SubmitFeedbackServiceMockData.CreateValidRequest();

            // Act
            var result = await sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4055.ToString());
        }

        [Fact]
        public async Task Process_ShouldReturn4055_WhenFeedbackAlreadyExists()
        {
            // Arrange
            var context = CreateContext();
            var userId = SubmitFeedbackServiceMockData.ValidUserId;
            var httpContextAccessor = SubmitFeedbackServiceMockData.CreateMockHttpContextAccessor(userId).Object;

            var existingFeedback = new Feedback { Id = Guid.NewGuid() };
            var appointment = SubmitFeedbackServiceMockData.CreateAppointment(
                createdById: userId,
                feedback: existingFeedback);

            SetupAppointmentRepository(new[] { appointment });

            var sut = CreateSut(context, httpContextAccessor);
            var request = SubmitFeedbackServiceMockData.CreateValidRequest();

            // Act
            var result = await sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4055.ToString());
        }

        [Theory]
        [InlineData(0, 5)]  // Doctor rating < 1
        [InlineData(6, 5)]  // Doctor rating > 5
        [InlineData(5, 0)]  // Clinic rating < 1
        [InlineData(5, 6)]  // Clinic rating > 5
        public async Task Process_ShouldReturn4055_WhenRatingIsInvalid(int doctorRating, int clinicRating)
        {
            // Arrange
            var context = CreateContext();
            var userId = SubmitFeedbackServiceMockData.ValidUserId;
            var httpContextAccessor = SubmitFeedbackServiceMockData.CreateMockHttpContextAccessor(userId).Object;

            var appointment = SubmitFeedbackServiceMockData.CreateAppointment(createdById: userId);

            SetupAppointmentRepository(new[] { appointment });

            var sut = CreateSut(context, httpContextAccessor);
            var request = new SubmitFeedbackRequest
            {
                AppointmentId = appointment.Id,
                RatingDoctor = doctorRating,
                RatingClinic = clinicRating
            };

            // Act
            var result = await sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4055.ToString());
        }

        #endregion

        #region Success Scenarios & Permission Access Variants

        [Fact]
        public async Task Process_ShouldSucceed_WhenUserMatchesPatientIdDirectly()
        {
            // Arrange
            var context = CreateContext();
            var userId = SubmitFeedbackServiceMockData.ValidUserId;
            var httpContextAccessor = SubmitFeedbackServiceMockData.CreateMockHttpContextAccessor(userId).Object;

            var appointment = SubmitFeedbackServiceMockData.CreateAppointment(
                patientId: userId,
                createdById: SubmitFeedbackServiceMockData.OtherUserId);

            SetupSuccessDependencies(appointment);

            var sut = CreateSut(context, httpContextAccessor);
            var request = SubmitFeedbackServiceMockData.CreateValidRequest(appointment.Id);

            // Act
            var result = await sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2009.ToString());
            result.Data.Should().NotBeNull();
            result.Data.RatingDoctor.Should().Be(5);
        }

        [Fact]
        public async Task Process_ShouldSucceed_WhenUserHasAccessViaPatientProfileOrUserPatient()
        {
            // Arrange
            var context = CreateContext();
            var userId = SubmitFeedbackServiceMockData.ValidUserId;
            var directProfileId = Guid.NewGuid();
            var linkedPatientId = Guid.NewGuid();

            // 1. Seed User
            var user = SubmitFeedbackServiceMockData.CreateUser(userId);
            context.Set<User>().Add(user);

            // 2. Seed PatientProfile sở hữu trực tiếp
            context.Set<PatientProfile>().Add(new PatientProfile
            {
                Id = directProfileId,
                UserId = userId,
                User = user,
                FullName = "Patient Direct",
                Gender = ECS.Domain.Enums.Gender.MALE
            });

            // 3. Seed PatientProfile liên kết (mẹ, con, người thân...)
            var linkedPatient = new PatientProfile
            {
                Id = linkedPatientId,
                FullName = "Linked Patient",
                Gender = ECS.Domain.Enums.Gender.FEMALE
            };
            context.Set<PatientProfile>().Add(linkedPatient);

            // 4. Seed UserPatient (Sử dụng Composite Key UserId + PatientId, bỏ thuộc tính Id)
            context.Set<UserPatient>().Add(new UserPatient
            {
                UserId = userId,
                PatientId = linkedPatientId,
                User = user,
                Patient = linkedPatient,
                Relationship = "Family",
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            var httpContextAccessor = SubmitFeedbackServiceMockData.CreateMockHttpContextAccessor(userId).Object;

            var appointment = SubmitFeedbackServiceMockData.CreateAppointment(
                patientId: linkedPatientId,
                createdById: SubmitFeedbackServiceMockData.OtherUserId);

            SetupSuccessDependencies(appointment);

            var sut = CreateSut(context, httpContextAccessor);
            var request = SubmitFeedbackServiceMockData.CreateValidRequest(appointment.Id);

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2009.ToString());
            result.Data.Should().NotBeNull();
            result.Data.AppointmentId.Should().Be(appointment.Id.ToString());
        }

        [Fact]
        public async Task Process_ShouldUpdateDoctorAndClinicRatingsCorrectly_WhenFeedbacksExist()
        {
            // Arrange
            var context = CreateContext();
            var userId = SubmitFeedbackServiceMockData.ValidUserId;
            var httpContextAccessor = SubmitFeedbackServiceMockData.CreateMockHttpContextAccessor(userId).Object;

            var doctor = SubmitFeedbackServiceMockData.CreateDoctor();
            var clinic = doctor.Clinic;
            var appointment = SubmitFeedbackServiceMockData.CreateAppointment(createdById: userId, doctor: doctor);

            SetupSuccessDependencies(appointment);

            _mockDoctorRepository.Setup(r => r.GetByIdAsync(doctor.Id)).ReturnsAsync(doctor);
            _mockClinicRepository.Setup(r => r.GetByIdAsync(clinic.Id)).ReturnsAsync(clinic);

            context.Set<Feedback>().Add(new Feedback
            {
                Id = Guid.NewGuid(),
                DoctorId = doctor.Id,
                ClinicId = clinic.Id,
                RatingDoctor = 3,
                RatingClinic = 2
            });
            await context.SaveChangesAsync();

            var sut = CreateSut(context, httpContextAccessor);
            var request = new SubmitFeedbackRequest
            {
                AppointmentId = appointment.Id,
                RatingDoctor = 5,
                RatingClinic = 4,
                Comment = "Feedback with ratings",
                IsPublic = true
            };

            // Act
            var result = await sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2009.ToString());

            _mockDoctorRepository.Verify(r => r.UpdateAsync(It.Is<DoctorProfile>(d =>
                d.RatingAvg == 3m && d.ReviewCount == 1)), Times.Once);

            _mockClinicRepository.Verify(r => r.UpdateAsync(It.Is<Clinic>(c =>
                c.RatingAvg == 2m && c.ReviewCount == 1)), Times.Once);
        }

        [Fact]
        public async Task Process_ShouldSetRatingsToZero_WhenDoctorAndClinicHaveNoFeedbacksInDb()
        {
            // Arrange
            var context = CreateContext();
            var userId = SubmitFeedbackServiceMockData.ValidUserId;
            var httpContextAccessor = SubmitFeedbackServiceMockData.CreateMockHttpContextAccessor(userId).Object;

            var doctor = SubmitFeedbackServiceMockData.CreateDoctor();
            var clinic = doctor.Clinic;
            var appointment = SubmitFeedbackServiceMockData.CreateAppointment(createdById: userId, doctor: doctor);

            SetupSuccessDependencies(appointment);

            _mockDoctorRepository.Setup(r => r.GetByIdAsync(doctor.Id)).ReturnsAsync(doctor);
            _mockClinicRepository.Setup(r => r.GetByIdAsync(clinic.Id)).ReturnsAsync(clinic);

            var sut = CreateSut(context, httpContextAccessor);
            var request = SubmitFeedbackServiceMockData.CreateValidRequest(appointment.Id);

            // Act
            var result = await sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2009.ToString());

            _mockDoctorRepository.Verify(r => r.UpdateAsync(It.Is<DoctorProfile>(d =>
                d.RatingAvg == 0 && d.ReviewCount == 0)), Times.Once);

            _mockClinicRepository.Verify(r => r.UpdateAsync(It.Is<Clinic>(c =>
                c.RatingAvg == 0 && c.ReviewCount == 0)), Times.Once);
        }

        [Fact]
        public async Task Process_ShouldNotFail_WhenDoctorOrClinicNotFoundInRepository()
        {
            // Arrange
            var context = CreateContext();
            var userId = SubmitFeedbackServiceMockData.ValidUserId;
            var httpContextAccessor = SubmitFeedbackServiceMockData.CreateMockHttpContextAccessor(userId).Object;

            var doctor = SubmitFeedbackServiceMockData.CreateDoctor();
            var appointment = SubmitFeedbackServiceMockData.CreateAppointment(createdById: userId, doctor: doctor);

            SetupSuccessDependencies(appointment);

            _mockDoctorRepository.Setup(r => r.GetByIdAsync(doctor.Id)).ReturnsAsync((DoctorProfile)null);
            _mockClinicRepository.Setup(r => r.GetByIdAsync(doctor.ClinicId)).ReturnsAsync((Clinic)null);

            var sut = CreateSut(context, httpContextAccessor);
            var request = SubmitFeedbackServiceMockData.CreateValidRequest(appointment.Id);

            // Act
            var result = await sut.Process(request);

            // Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2009.ToString());
            _mockDoctorRepository.Verify(r => r.UpdateAsync(It.IsAny<DoctorProfile>()), Times.Never);
            _mockClinicRepository.Verify(r => r.UpdateAsync(It.IsAny<Clinic>()), Times.Never);
        }

        #endregion

        #region Transaction Rollback Scenario

        [Fact]
        public async Task Process_ShouldRollbackAndThrow_WhenExceptionOccursDuringTransaction()
        {
            // Arrange
            var context = CreateContext();
            var userId = SubmitFeedbackServiceMockData.ValidUserId;
            var httpContextAccessor = SubmitFeedbackServiceMockData.CreateMockHttpContextAccessor(userId).Object;

            var appointment = SubmitFeedbackServiceMockData.CreateAppointment(createdById: userId);

            var mockTransaction = new Mock<IDbContextTransaction>();
            _mockFeedbackRepository.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(mockTransaction.Object);

            SetupAppointmentRepository(new[] { appointment });

            _mockFeedbackRepository
                .Setup(r => r.CreateAsync(It.IsAny<Feedback>()))
                .ThrowsAsync(new InvalidOperationException("Database connection failure"));

            var sut = CreateSut(context, httpContextAccessor);
            var request = SubmitFeedbackServiceMockData.CreateValidRequest(appointment.Id);

            // Act
            Func<Task> act = async () => await sut.Process(request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Database connection failure");
            mockTransaction.Verify(t => t.RollbackAsync(default), Times.Once);
        }

        #endregion

        #region Helper Setup Methods

        private void SetupSuccessDependencies(Appointment appointment)
        {
            var mockTransaction = new Mock<IDbContextTransaction>();
            _mockFeedbackRepository.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(mockTransaction.Object);

            SetupAppointmentRepository(new[] { appointment });

            _mockDoctorRepository
                .Setup(r => r.GetByIdAsync(appointment.DoctorId))
                .ReturnsAsync(appointment.Doctor);

            _mockClinicRepository
                .Setup(r => r.GetByIdAsync(appointment.Doctor.ClinicId))
                .ReturnsAsync(appointment.Doctor.Clinic);
        }

        #endregion
    }
}