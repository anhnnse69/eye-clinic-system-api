using System.Linq.Expressions;
using System.Security.Claims;
using ECS.Application.Services.PatientProfileManagementServices.ViewMyFeedbackHistoryServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Feedbacks;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.PatientProfileManagementServices.ViewMyFeedbackHistoryServices
{
    public class ViewMyFeedbackHistoryServiceTests : IDisposable
    {
        private readonly Mock<IRepositoryQueryBase<Feedback, Guid, AppDbContext>> _feedbackRepositoryMock = new();
        private readonly Mock<IRepositoryQueryBase<PatientProfile, Guid, AppDbContext>> _patientProfileRepositoryMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly AppDbContext _context;
        private readonly ViewMyFeedbackHistoryService _sut;

        public ViewMyFeedbackHistoryServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _sut = new ViewMyFeedbackHistoryService(
                _feedbackRepositoryMock.Object,
                _patientProfileRepositoryMock.Object,
                _context,
                _httpContextAccessorMock.Object);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private void SetupHttpContextClaim(string? claimValue)
        {
            var context = new DefaultHttpContext();
            if (claimValue != null)
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, claimValue) },
                    "TestAuth"));
            }

            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
        }

        private void SetupPatientProfileRepository(List<PatientProfile> rows)
        {
            var queryable = rows.BuildMockDbSet();

            _patientProfileRepositoryMock
                .Setup(x => x.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()))
                .Returns((Expression<Func<PatientProfile, bool>> predicate, bool _) =>
                    queryable.Object.Where(predicate));
        }

        private void SetupFeedbackRepository(List<Feedback> rows)
        {
            var queryable = rows.BuildMockDbSet();

            _feedbackRepositoryMock
                .Setup(x => x.FindByCondition(It.IsAny<Expression<Func<Feedback, bool>>>(), It.IsAny<bool>()))
                .Returns((Expression<Func<Feedback, bool>> predicate, bool _) =>
                    queryable.Object.Where(predicate));
        }

        private void SeedUserPatients(params UserPatient[] links)
        {
            _context.Set<UserPatient>().AddRange(links);
            _context.SaveChanges();
        }

        private static Feedback BuildFeedback(
            Guid patientId,
            string patientName = "Nguyen Van A",
            string doctorName = "Tran Van B",
            string clinicName = "ECS Eye Clinic",
            string? comment = "Rat tot",
            DateTime? createdAt = null,
            DateTime? appointmentDate = null)
        {
            var appointmentId = Guid.NewGuid();

            return new Feedback
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId,
                PatientId = patientId,
                DoctorId = Guid.NewGuid(),
                ClinicId = Guid.NewGuid(),
                RatingDoctor = 5,
                RatingClinic = 4,
                Comment = comment,
                IsPublic = true,
                CreatedAt = createdAt ?? new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc),
                Patient = new PatientProfile
                {
                    Id = patientId,
                    FullName = patientName,
                    Gender = Gender.MALE,
                    Dob = new DateTime(1990, 1, 1)
                },
                Doctor = new DoctorProfile
                {
                    Id = Guid.NewGuid(),
                    User = new User { FullName = doctorName }
                },
                Clinic = new Clinic
                {
                    Id = Guid.NewGuid(),
                    Name = clinicName,
                    Address = "123 Le Loi",
                    Phone = "0900000000"
                },
                Appointment = new Appointment
                {
                    Id = appointmentId,
                    AppointmentDate = appointmentDate ?? new DateTime(2026, 1, 5)
                }
            };
        }

        [Fact]
        public async Task Process_NullHttpContext_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new ViewMyFeedbackHistoryRequest();

            //Arrange 2
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
            SetupFeedbackRepository(new List<Feedback>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            _patientProfileRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            // isUserValid = false -> isDataScopeExist vẫn giữ true mặc định (ValidateDataContext return sớm)
            // nên ExecutePagedQuery vẫn gọi vào feedbackRepository (với filter luôn false do patientIds rỗng).
            _feedbackRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<Feedback, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task Process_MissingNameIdentifierClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new ViewMyFeedbackHistoryRequest();

            //Arrange 2
            SetupHttpContextClaim(null);
            SetupFeedbackRepository(new List<Feedback>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task Process_MalformedNameIdentifierClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            var request = new ViewMyFeedbackHistoryRequest();

            //Arrange 2
            SetupHttpContextClaim("not-a-guid");
            SetupFeedbackRepository(new List<Feedback>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task Process_ValidUserWithoutAnyPatientScope_Returns2000WithEmptyListAndDefaultMeta()
        {
            //Arrange 1
            var request = new ViewMyFeedbackHistoryRequest();

            //Arrange 2
            SetupHttpContextClaim(Guid.NewGuid().ToString());
            SetupPatientProfileRepository(new List<PatientProfile>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().BeEmpty();
            result.Meta!.Page.Should().Be(1);
            result.Meta.Size.Should().Be(10);
            result.Meta.Total.Should().Be(0);
            // không có patient scope -> isDataScopeExist = false -> ExecutePagedQuery short-circuit, không gọi feedbackRepository
            _feedbackRepositoryMock.Verify(
                x => x.FindByCondition(It.IsAny<Expression<Func<Feedback, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task Process_ValidUserWithFeedback_Returns2000WithMappedFeedback()
        {
            //Arrange 1
            var request = new ViewMyFeedbackHistoryRequest();
            var patientId = Guid.NewGuid();
            var patient = new PatientProfile { Id = patientId, UserId = Guid.NewGuid(), FullName = "Nguyen Van A", Gender = Gender.MALE, Dob = new DateTime(1990, 1, 1) };
            var feedback = BuildFeedback(patientId);

            //Arrange 2
            SetupHttpContextClaim(patient.UserId!.Value.ToString());
            SetupPatientProfileRepository(new List<PatientProfile> { patient });
            SetupFeedbackRepository(new List<Feedback> { feedback });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().ContainSingle();
            var dto = result.Data![0];
            dto.FeedbackId.Should().Be(feedback.Id.ToString());
            dto.AppointmentId.Should().Be(feedback.AppointmentId.ToString());
            dto.PatientName.Should().Be(feedback.Patient.FullName);
            dto.DoctorName.Should().Be(feedback.Doctor.User.FullName);
            dto.ClinicName.Should().Be(feedback.Clinic.Name);
            dto.RatingDoctor.Should().Be(feedback.RatingDoctor);
            dto.RatingClinic.Should().Be(feedback.RatingClinic);
            dto.Comment.Should().Be(feedback.Comment);
            dto.IsPublic.Should().Be(feedback.IsPublic);
            dto.AppointmentDate.Should().Be(feedback.Appointment.AppointmentDate.ToString("dd/MM/yyyy"));
            dto.CreatedAt.Should().Be(feedback.CreatedAt.ToString("dd/MM/yyyy HH:mm"));
            result.Meta!.Total.Should().Be(1);
        }

        [Fact]
        public async Task Process_SearchTermMatchesPatientDoctorClinicOrComment_ReturnsOnlyMatchingFeedback()
        {
            //Arrange 1
            var patientId = Guid.NewGuid();
            var patient = new PatientProfile { Id = patientId, UserId = Guid.NewGuid(), FullName = "Nguyen Van A", Gender = Gender.MALE, Dob = new DateTime(1990, 1, 1) };
            var matching = BuildFeedback(patientId, clinicName: "ECS Eye Clinic");
            var nonMatching = BuildFeedback(patientId, patientName: "Le Thi C", doctorName: "Pham Van D", clinicName: "Other Clinic", comment: "Binh thuong");
            var request = new ViewMyFeedbackHistoryRequest { SearchTerm = "ecs eye clinic" };

            //Arrange 2
            SetupHttpContextClaim(patient.UserId!.Value.ToString());
            SetupPatientProfileRepository(new List<PatientProfile> { patient });
            SetupFeedbackRepository(new List<Feedback> { matching, nonMatching });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data.Should().ContainSingle();
            result.Data![0].FeedbackId.Should().Be(matching.Id.ToString());
            result.Meta!.Total.Should().Be(1);
        }

        [Fact]
        public async Task Process_SearchTermMatchesComment_ReturnsMatchingFeedback()
        {
            //Arrange 1
            var patientId = Guid.NewGuid();
            var patient = new PatientProfile { Id = patientId, UserId = Guid.NewGuid(), FullName = "Nguyen Van A", Gender = Gender.MALE, Dob = new DateTime(1990, 1, 1) };
            var matching = BuildFeedback(patientId, comment: "Bac si rat tan tam");
            var nonMatching = BuildFeedback(patientId, comment: "Binh thuong");
            var request = new ViewMyFeedbackHistoryRequest { SearchTerm = "tan tam" };

            //Arrange 2
            SetupHttpContextClaim(patient.UserId!.Value.ToString());
            SetupPatientProfileRepository(new List<PatientProfile> { patient });
            SetupFeedbackRepository(new List<Feedback> { matching, nonMatching });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data.Should().ContainSingle();
            result.Data![0].FeedbackId.Should().Be(matching.Id.ToString());
        }

        [Fact]
        public async Task Process_SearchTermNoMatch_ReturnsEmptyListWithZeroTotal()
        {
            //Arrange 1
            var patientId = Guid.NewGuid();
            var patient = new PatientProfile { Id = patientId, UserId = Guid.NewGuid(), FullName = "Nguyen Van A", Gender = Gender.MALE, Dob = new DateTime(1990, 1, 1) };
            var feedback = BuildFeedback(patientId);
            var request = new ViewMyFeedbackHistoryRequest { SearchTerm = "khong-ton-tai" };

            //Arrange 2
            SetupHttpContextClaim(patient.UserId!.Value.ToString());
            SetupPatientProfileRepository(new List<PatientProfile> { patient });
            SetupFeedbackRepository(new List<Feedback> { feedback });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().BeEmpty();
            result.Meta!.Total.Should().Be(0);
        }

        [Fact]
        public async Task Process_CommentNull_SearchTermDoesNotMatchOnNullComment()
        {
            //Arrange 1
            var patientId = Guid.NewGuid();
            var patient = new PatientProfile { Id = patientId, UserId = Guid.NewGuid(), FullName = "Nguyen Van A", Gender = Gender.MALE, Dob = new DateTime(1990, 1, 1) };
            var feedback = BuildFeedback(patientId, comment: null);
            var request = new ViewMyFeedbackHistoryRequest { SearchTerm = "tan tam" };

            //Arrange 2
            SetupHttpContextClaim(patient.UserId!.Value.ToString());
            SetupPatientProfileRepository(new List<PatientProfile> { patient });
            SetupFeedbackRepository(new List<Feedback> { feedback });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data.Should().BeEmpty();
            result.Meta!.Total.Should().Be(0);
        }

        [Fact]
        public async Task Process_Paging_ReturnsCorrectPageOrderedByCreatedAtDescending()
        {
            //Arrange 1
            var patientId = Guid.NewGuid();
            var patient = new PatientProfile { Id = patientId, UserId = Guid.NewGuid(), FullName = "Nguyen Van A", Gender = Gender.MALE, Dob = new DateTime(1990, 1, 1) };
            var older = BuildFeedback(patientId, createdAt: new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc));
            var newer = BuildFeedback(patientId, createdAt: new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc));
            var request = new ViewMyFeedbackHistoryRequest { PageNumber = 1, PageSize = 1 };

            //Arrange 2
            SetupHttpContextClaim(patient.UserId!.Value.ToString());
            SetupPatientProfileRepository(new List<PatientProfile> { patient });
            SetupFeedbackRepository(new List<Feedback> { older, newer });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data.Should().ContainSingle();
            result.Data![0].FeedbackId.Should().Be(newer.Id.ToString()); // most recent first
            result.Meta!.Total.Should().Be(2);
            result.Meta.Page.Should().Be(1);
            result.Meta.Size.Should().Be(1);
            result.Meta.TotalPages.Should().Be(2);
            result.Meta.HasNext.Should().BeTrue();
            result.Meta.HasPrevious.Should().BeFalse();
        }

        [Fact]
        public async Task Process_ValidUser_ExcludesFeedbackOfInaccessiblePatient()
        {
            //Arrange 1
            var accessiblePatientId = Guid.NewGuid();
            var foreignPatientId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var patient = new PatientProfile { Id = accessiblePatientId, UserId = userId, FullName = "Nguyen Van A", Gender = Gender.MALE, Dob = new DateTime(1990, 1, 1) };
            var accessibleFeedback = BuildFeedback(accessiblePatientId);
            var foreignFeedback = BuildFeedback(foreignPatientId);
            var request = new ViewMyFeedbackHistoryRequest();

            //Arrange 2
            SetupHttpContextClaim(userId.ToString());
            SetupPatientProfileRepository(new List<PatientProfile> { patient });
            SetupFeedbackRepository(new List<Feedback> { accessibleFeedback, foreignFeedback });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data.Should().ContainSingle();
            result.Data![0].FeedbackId.Should().Be(accessibleFeedback.Id.ToString());
            result.Meta!.Total.Should().Be(1);
        }

        [Fact]
        public async Task Process_LinkedPatientViaUserPatient_IncludesFeedbackOfLinkedPatient()
        {
            //Arrange 1
            var userId = Guid.NewGuid();
            var linkedPatientId = Guid.NewGuid();
            var link = new UserPatient { UserId = userId, PatientId = linkedPatientId, Relationship = "Con" };
            var feedback = BuildFeedback(linkedPatientId);
            var request = new ViewMyFeedbackHistoryRequest();

            //Arrange 2
            SetupHttpContextClaim(userId.ToString());
            SeedUserPatients(link);
            SetupPatientProfileRepository(new List<PatientProfile>()); // không có profile trực tiếp
            SetupFeedbackRepository(new List<Feedback> { feedback });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().ContainSingle();
            result.Data![0].FeedbackId.Should().Be(feedback.Id.ToString());
        }
    }
}