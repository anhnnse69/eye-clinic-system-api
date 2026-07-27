using System.Linq.Expressions;
using System.Reflection;
using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicDoctorDiscoveryService.ViewClinicProfileServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Feedbacks;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.ClinicDoctorDiscoveryServices.ViewClinicProfileServices
{
    /// <summary>
    /// Unit tests for <see cref="ViewClinicProfileService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line coverage on <c>ViewClinicProfileService.cs</c>.
    /// </summary>
    public class ViewClinicProfileServiceTests
    {
        private static readonly Guid ClinicId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid PatientId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid Doctor1UserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly Guid Doctor2UserId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        private static readonly Guid Doctor3UserId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        private static readonly Guid Doctor1Id = Guid.Parse("66666666-6666-6666-6666-666666666666");
        private static readonly Guid Doctor2Id = Guid.Parse("77777777-7777-7777-7777-777777777777");
        private static readonly Guid Doctor3Id = Guid.Parse("88888888-8888-8888-8888-888888888888");
        private static readonly Guid SpecialtyId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        private readonly Mock<IRepositoryQueryBase<Clinic, Guid, AppDbContext>> _clinicRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<Service, Guid, AppDbContext>> _serviceRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<Feedback, Guid, AppDbContext>> _feedbackRepoMock = new();
        private readonly ViewClinicProfileService _sut;

        public ViewClinicProfileServiceTests()
        {
            _sut = new ViewClinicProfileService(
                _clinicRepoMock.Object,
                _doctorRepoMock.Object,
                _serviceRepoMock.Object,
                _feedbackRepoMock.Object);
        }

        // ─────────────────────────────────────────────────────────────────
        // Reflection helpers
        // ─────────────────────────────────────────────────────────────────

        private static object? InvokePrivate(object target, string methodName, params object[] args)
        {
            var mi = target.GetType().GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
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

        // ─────────────────────────────────────────────────────────────────
        // Repository helpers
        // ─────────────────────────────────────────────────────────────────

        private void SetupClinicRepo(IEnumerable<Clinic> clinics)
        {
            var list = clinics.ToList();
            var queryable = list.BuildMockDbSet<Clinic>();
            _clinicRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Clinic, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(queryable.Object);
        }

        private void SetupEmptyClinicRepo()
            => SetupClinicRepo(Array.Empty<Clinic>());

        private void SetupDoctorRepo(IEnumerable<DoctorProfile> doctors)
        {
            var list = doctors.ToList();
            var queryable = list.BuildMockDbSet<DoctorProfile>();
            _doctorRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<DoctorProfile, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(queryable.Object);
        }

        private void SetupEmptyDoctorRepo()
            => SetupDoctorRepo(Array.Empty<DoctorProfile>());

        private void SetupServiceRepo(IEnumerable<Service> services)
        {
            var list = services.ToList();
            var queryable = list.BuildMockDbSet<Service>();
            _serviceRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Service, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(queryable.Object);
        }

        private void SetupEmptyServiceRepo()
            => SetupServiceRepo(Array.Empty<Service>());

        private void SetupFeedbackRepo(IEnumerable<Feedback> feedbacks)
        {
            var list = feedbacks.ToList();
            var queryable = list.BuildMockDbSet<Feedback>();
            _feedbackRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Feedback, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(queryable.Object);
        }

        private void SetupEmptyFeedbackRepo()
            => SetupFeedbackRepo(Array.Empty<Feedback>());

        // ─────────────────────────────────────────────────────────────────
        // Data factories
        // ─────────────────────────────────────────────────────────────────

        private static Clinic MakeClinic(
            Guid id,
            string name = "Saigon Eye Clinic",
            string address = "1 Cong Hoa Street",
            string phone = "0900000000",
            string? email = "clinic@example.com",
            string? logoUrl = "https://example.com/logo.png",
            string? description = "Top eye clinic in Saigon",
            decimal? ratingAvg = 4.5m,
            int? reviewCount = 100,
            bool isActive = true) => new()
        {
            Id = id,
            Name = name,
            Address = address,
            Phone = phone,
            Email = email,
            LogoUrl = logoUrl,
            Description = description,
            RatingAvg = ratingAvg,
            ReviewCount = reviewCount,
            IsActive = isActive
        };

        private static User MakeUser(Guid id, string fullName, string? avatarUrl = null) => new()
        {
            Id = id,
            Phone = "0901111111",
            PasswordHash = "hashed",
            FullName = fullName,
            Role = UserRole.DOCTOR,
            IsActive = true,
            AvatarUrl = avatarUrl
        };

        private static DoctorProfile MakeDoctor(
            Guid id,
            Guid userId,
            Guid clinicId,
            string fullName,
            string? title = "Senior Ophthalmologist",
            int experienceYears = 10,
            Specialty? specialty = null,
            decimal? ratingAvg = 4.7m,
            int? reviewCount = 50) => new()
        {
            Id = id,
            UserId = userId,
            ClinicId = clinicId,
            SpecialtyId = specialty?.Id,
            Title = title,
            ExperienceYears = experienceYears,
            IsActive = true,
            RatingAvg = ratingAvg,
            ReviewCount = reviewCount,
            User = MakeUser(userId, fullName),
            Specialty = specialty
        };

        private static Service MakeService(
            Guid id,
            Guid clinicId,
            string serviceName,
            decimal? price = 100000m,
            int durationMinutes = 30) => new()
        {
            Id = id,
            ClinicId = clinicId,
            ServiceName = serviceName,
            Price = price,
            DurationMinutes = durationMinutes,
            IsActive = true
        };

        private static PatientProfile MakePatient(string fullName) => new()
        {
            Id = PatientId,
            UserId = Guid.NewGuid(),
            FullName = fullName,
            Gender = Gender.FEMALE,
            Dob = new DateTime(1990, 1, 1)
        };

        private static Feedback MakeFeedback(
            Guid id,
            Guid clinicId,
            PatientProfile patient,
            int ratingDoctor = 5,
            int ratingClinic = 5,
            string? comment = "Great",
            bool isPublic = true,
            DateTime? createdAt = null) => new()
        {
            Id = id,
            PatientId = patient.Id,
            DoctorId = Doctor1Id,
            ClinicId = clinicId,
            RatingDoctor = ratingDoctor,
            RatingClinic = ratingClinic,
            Comment = comment,
            IsPublic = isPublic,
            CreatedAt = createdAt ?? new DateTime(2026, 7, 26, 10, 0, 0, DateTimeKind.Utc),
            Patient = patient,
            Appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                Status = AppointmentStatus.COMPLETED,
                AppointmentDate = new DateTime(2026, 7, 25, 9, 0, 0, DateTimeKind.Utc),
                PatientId = patient.Id,
                DoctorId = Doctor1Id
            }
        };

        // ==================================================================
        // ====================== Process(...) tests ========================
        // ==================================================================

        /// <summary>
        /// TC-VCPR-01: Clinic repo returns empty → GetClinicOrThrowAsync null-coalescing
        /// branch throws KeyNotFoundException(APP_MESSAGE_4008). Doctor/service/feedback
        /// repos are never invoked.
        /// </summary>
        [Fact]
        public async Task Process_ClinicNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange 1

            //Arrange 2
            SetupEmptyClinicRepo();
            SetupEmptyDoctorRepo();
            SetupEmptyServiceRepo();
            SetupEmptyFeedbackRepo();

            //Act
            Func<Task> act = async () => await _sut.Process(ClinicId);

            //Assert
            var ex = await act.Should().ThrowAsync<KeyNotFoundException>();
            ex.Which.Message.Should().Be(GeneralCode.APP_MESSAGE_4008.ToString());

            _doctorRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<DoctorProfile, bool>>>(),
                    It.IsAny<bool>()),
                Times.Never);
            _serviceRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<Service, bool>>>(),
                    It.IsAny<bool>()),
                Times.Never);
            _feedbackRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<Feedback, bool>>>(),
                    It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>
        /// TC-VCPR-02: Clinic exists, all related repos are empty → response contains
        /// all clinic scalar fields + empty Doctors/Services/Feedbacks lists.
        /// </summary>
        [Fact]
        public async Task Process_EmptyClinicProfile_Returns2000WithEmptyLists()
        {
            //Arrange 1
            var clinic = MakeClinic(ClinicId);

            //Arrange 2
            SetupClinicRepo(new[] { clinic });
            SetupEmptyDoctorRepo();
            SetupEmptyServiceRepo();
            SetupEmptyFeedbackRepo();

            //Act
            var result = await _sut.Process(ClinicId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(ClinicId);
            result.Data!.Name.Should().Be("Saigon Eye Clinic");
            result.Data!.Address.Should().Be("1 Cong Hoa Street");
            result.Data!.Phone.Should().Be("0900000000");
            result.Data!.Email.Should().Be("clinic@example.com");
            result.Data!.LogoUrl.Should().Be("https://example.com/logo.png");
            result.Data!.Description.Should().Be("Top eye clinic in Saigon");
            result.Data!.RatingAvg.Should().Be(4.5m);
            result.Data!.ReviewCount.Should().Be(100);
            result.Data!.Doctors.Should().BeEmpty();
            result.Data!.Services.Should().BeEmpty();
            result.Data!.Feedbacks.Should().BeEmpty();
        }

        /// <summary>
        /// TC-VCPR-03: Happy path with all four repos populated → response contains
        /// all populated collections with correct mapping.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_Returns2000WithPopulatedLists()
        {
            //Arrange 1
            var clinic = MakeClinic(ClinicId);
            var specialty = new Specialty { Id = SpecialtyId, Name = "Ophthalmology", IsActive = true };
            var doctor1 = MakeDoctor(Doctor1Id, Doctor1UserId, ClinicId, "Nguyen Van A", specialty: specialty);
            var doctor2 = MakeDoctor(Doctor2Id, Doctor2UserId, ClinicId, "Tran Van B", specialty: null);
            var service1 = MakeService(Guid.NewGuid(), ClinicId, "Eye Examination", price: 100000m, durationMinutes: 30);
            var service2 = MakeService(Guid.NewGuid(), ClinicId, "Cataract Surgery", price: 500000m, durationMinutes: 60);
            var service3 = MakeService(Guid.NewGuid(), ClinicId, "Glasses Fitting", price: 50000m, durationMinutes: 15);
            var patient = MakePatient("Le Van Patient");
            var feedback1 = MakeFeedback(Guid.NewGuid(), ClinicId, patient, ratingDoctor: 5, ratingClinic: 5, comment: "Excellent");
            var feedback2 = MakeFeedback(Guid.NewGuid(), ClinicId, patient, ratingDoctor: 4, ratingClinic: 5, comment: "Good");
            var feedback3 = MakeFeedback(Guid.NewGuid(), ClinicId, patient, ratingDoctor: 5, ratingClinic: 4, comment: "Satisfied");

            //Arrange 2
            SetupClinicRepo(new[] { clinic });
            SetupDoctorRepo(new[] { doctor1, doctor2 });
            SetupServiceRepo(new[] { service1, service2, service3 });
            SetupFeedbackRepo(new[] { feedback1, feedback2, feedback3 });

            //Act
            var result = await _sut.Process(ClinicId);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();

            result.Data!.Feedbacks.Count.Should().Be(3);
            result.Data!.Feedbacks[0].PatientName.Should().Be("Le Van Patient");
            result.Data!.Feedbacks[0].RatingDoctor.Should().Be(5);
            result.Data!.Feedbacks[0].RatingClinic.Should().Be(5);

            result.Data!.Services.Count.Should().BeGreaterThan(0);
            result.Data!.Doctors.Count.Should().BeGreaterThan(0);
        }

        /// <summary>
        /// TC-VCPR-04: Doctor with non-null Specialty → projection populates Specialty
        /// from d.Specialty.Name (true branch of the ternary).
        /// </summary>
        [Fact]
        public async Task Process_DoctorWithSpecialty_ProjectSpecialtyName()
        {
            //Arrange 1
            var clinic = MakeClinic(ClinicId);
            var specialty = new Specialty { Id = SpecialtyId, Name = "Ophthalmology", IsActive = true };
            var doctor = MakeDoctor(Doctor1Id, Doctor1UserId, ClinicId, "Nguyen Van A", specialty: specialty);

            //Arrange 2
            SetupClinicRepo(new[] { clinic });
            SetupDoctorRepo(new[] { doctor });
            SetupEmptyServiceRepo();
            SetupEmptyFeedbackRepo();

            //Act
            var result = await _sut.Process(ClinicId);

            //Assert
            result.Data!.Doctors.Should().HaveCount(1);
            result.Data!.Doctors[0].Specialty.Should().Be("Ophthalmology");
        }

        /// <summary>
        /// TC-VCPR-05: Doctor with Specialty = null → projection populates Specialty
        /// as null (false branch of the ternary).
        /// </summary>
        [Fact]
        public async Task Process_DoctorWithoutSpecialty_ProjectSpecialtyNull()
        {
            //Arrange 1
            var clinic = MakeClinic(ClinicId);
            var doctor = MakeDoctor(Doctor1Id, Doctor1UserId, ClinicId, "Nguyen Van A", specialty: null);

            //Arrange 2
            SetupClinicRepo(new[] { clinic });
            SetupDoctorRepo(new[] { doctor });
            SetupEmptyServiceRepo();
            SetupEmptyFeedbackRepo();

            //Act
            var result = await _sut.Process(ClinicId);

            //Assert
            result.Data!.Doctors.Should().HaveCount(1);
            result.Data!.Doctors[0].Specialty.Should().BeNull();
        }

        /// <summary>
        /// TC-VCPR-06: 25 public feedbacks → only the 20 newest are returned.
        /// </summary>
        [Fact]
        public async Task Process_FetchFeedbacksTake20_ReturnsNewest20()
        {
            //Arrange 1
            var clinic = MakeClinic(ClinicId);
            var patient = MakePatient("Le Van Patient");
            var rows = Enumerable.Range(0, 25).Select(i => new Feedback
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                DoctorId = Doctor1Id,
                ClinicId = ClinicId,
                RatingDoctor = 5,
                RatingClinic = 5,
                Comment = $"Feedback #{i}",
                IsPublic = true,
                CreatedAt = new DateTime(2026, 7, 26, 10, 0, 0, DateTimeKind.Utc).AddMinutes(-i),
                Patient = patient,
                Appointment = new Appointment
                {
                    Id = Guid.NewGuid(),
                    Status = AppointmentStatus.COMPLETED,
                    AppointmentDate = DateTime.UtcNow,
                    PatientId = patient.Id,
                    DoctorId = Doctor1Id
                }
            }).ToList();

            //Arrange 2
            SetupClinicRepo(new[] { clinic });
            SetupEmptyDoctorRepo();
            SetupEmptyServiceRepo();
            SetupFeedbackRepo(rows);

            //Act
            var result = await _sut.Process(ClinicId);

            //Assert
            result.Data!.Feedbacks.Count.Should().Be(20);
        }

        /// <summary>
        /// TC-VCPR-07: Orchestration — clinic, doctor, service, feedback repos each
        /// invoked exactly once in the happy path.
        /// </summary>
        [Fact]
        public async Task Process_AllRepositoryInvocationsAreExecuted()
        {
            //Arrange 1
            var clinic = MakeClinic(ClinicId);

            //Arrange 2
            SetupClinicRepo(new[] { clinic });
            SetupEmptyDoctorRepo();
            SetupEmptyServiceRepo();
            SetupEmptyFeedbackRepo();

            //Act
            await _sut.Process(ClinicId);

            //Assert
            _clinicRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<Clinic, bool>>>(),
                    It.IsAny<bool>()),
                Times.Once);
            _doctorRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<DoctorProfile, bool>>>(),
                    It.IsAny<bool>()),
                Times.Once);
            _serviceRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<Service, bool>>>(),
                    It.IsAny<bool>()),
                Times.Once);
            _feedbackRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<Feedback, bool>>>(),
                    It.IsAny<bool>()),
                Times.Once);
        }

        // ==================================================================
        // ============== FetchDoctorsAsync(...) — private ===================
        // ==================================================================

        /// <summary>
        /// TC-VCPR-08: Direct reflection call. Doctor with all fields + Specialty →
        /// asserts every ClinicDoctorItem field is correctly populated.
        /// </summary>
        [Fact]
        public async Task FetchDoctorsAsync_HappyPath_ProjectsAllFieldsWithSpecialty()
        {
            //Arrange 1
            var specialty = new Specialty { Id = SpecialtyId, Name = "Ophthalmology", IsActive = true };
            var doctor = MakeDoctor(
                Doctor1Id,
                Doctor1UserId,
                ClinicId,
                fullName: "Nguyen Van A",
                title: "Senior",
                experienceYears: 12,
                specialty: specialty,
                ratingAvg: 4.8m,
                reviewCount: 80);

            //Arrange 2
            SetupDoctorRepo(new[] { doctor });

            //Act
            var result = await InvokePrivateAsync<List<ClinicDoctorItem>>(
                _sut, "FetchDoctorsAsync", ClinicId);

            //Assert
            result.Should().NotBeNull();
            result.Count.Should().Be(1);
            var item = result[0];
            item.Id.Should().Be(Doctor1Id);
            item.FullName.Should().Be("Nguyen Van A");
            item.Title.Should().Be("Senior");
            item.Specialty.Should().Be("Ophthalmology");
            item.ExperienceYears.Should().Be(12);
            item.RatingAvg.Should().Be(4.8m);
            item.ReviewCount.Should().Be(80);
        }

        /// <summary>
        /// TC-VCPR-09: Direct reflection call. Doctor without Specialty → Specialty is null.
        /// </summary>
        [Fact]
        public async Task FetchDoctorsAsync_NoSpecialty_ProjectSpecialtyAsNull()
        {
            //Arrange 1
            var doctor = MakeDoctor(Doctor1Id, Doctor1UserId, ClinicId, "Nguyen Van A", specialty: null);

            //Arrange 2
            SetupDoctorRepo(new[] { doctor });

            //Act
            var result = await InvokePrivateAsync<List<ClinicDoctorItem>>(
                _sut, "FetchDoctorsAsync", ClinicId);

            //Assert
            result.Should().HaveCount(1);
            result[0].Specialty.Should().BeNull();
        }

        /// <summary>
        /// TC-VCPR-10: Direct reflection call. Three doctors with unsorted FullName →
        /// MockQueryable honors OrderBy(d.User.FullName), ascending.
        /// </summary>
        [Fact]
        public async Task FetchDoctorsAsync_OrdersByUserFullName()
        {
            //Arrange 1
            var doctorA = MakeDoctor(Guid.NewGuid(), Doctor1UserId, ClinicId, "Tran Thi C");
            var doctorB = MakeDoctor(Guid.NewGuid(), Doctor2UserId, ClinicId, "Nguyen Van A");
            var doctorC = MakeDoctor(Guid.NewGuid(), Doctor3UserId, ClinicId, "Le Van B");

            //Arrange 2
            SetupDoctorRepo(new[] { doctorA, doctorB, doctorC });

            //Act
            var result = await InvokePrivateAsync<List<ClinicDoctorItem>>(
                _sut, "FetchDoctorsAsync", ClinicId);

            //Assert
            result.Should().HaveCount(3);
            result[0].FullName.Should().Be("Le Van B");
            result[1].FullName.Should().Be("Nguyen Van A");
            result[2].FullName.Should().Be("Tran Thi C");
        }

        // ==================================================================
        // ============ FetchServicesAsync(...) — private ===================
        // ==================================================================

        /// <summary>
        /// TC-VCPR-11: Direct reflection call. Service with all fields → asserts every
        /// ClinicServiceItem field is correctly populated.
        /// </summary>
        [Fact]
        public async Task FetchServicesAsync_HappyPath_ProjectsEveryField()
        {
            //Arrange 1
            var serviceId = Guid.Parse("99999999-9999-9999-9999-999999999999");
            var service = new Service
            {
                Id = serviceId,
                ClinicId = ClinicId,
                ServiceName = "Eye Examination",
                Price = 250000m,
                DurationMinutes = 45,
                IsActive = true
            };

            //Arrange 2
            SetupServiceRepo(new[] { service });

            //Act
            var result = await InvokePrivateAsync<List<ClinicServiceItem>>(
                _sut, "FetchServicesAsync", ClinicId);

            //Assert
            result.Should().HaveCount(1);
            var item = result[0];
            item.Id.Should().Be(serviceId);
            item.ServiceName.Should().Be("Eye Examination");
            item.Price.Should().Be(250000m);
            item.DurationMinutes.Should().Be(45);
        }

        /// <summary>
        /// TC-VCPR-12: Direct reflection call. Three unsorted services → ascending order.
        /// </summary>
        [Fact]
        public async Task FetchServicesAsync_OrdersByServiceName()
        {
            //Arrange 1
            var s1 = MakeService(Guid.NewGuid(), ClinicId, "Cataract Surgery");
            var s2 = MakeService(Guid.NewGuid(), ClinicId, "Eye Examination");
            var s3 = MakeService(Guid.NewGuid(), ClinicId, "Glasses Fitting");

            //Arrange 2
            SetupServiceRepo(new[] { s1, s2, s3 });

            //Act
            var result = await InvokePrivateAsync<List<ClinicServiceItem>>(
                _sut, "FetchServicesAsync", ClinicId);

            //Assert
            result.Should().HaveCount(3);
            result[0].ServiceName.Should().Be("Cataract Surgery");
            result[1].ServiceName.Should().Be("Eye Examination");
            result[2].ServiceName.Should().Be("Glasses Fitting");
        }

        // ==================================================================
        // ============ FetchFeedbacksAsync(...) — private ===================
        // ==================================================================

        /// <summary>
        /// TC-VCPR-13: Direct reflection call. Single feedback with all fields populated
        /// → asserts every ClinicFeedbackItem field is mapped correctly.
        /// </summary>
        [Fact]
        public async Task FetchFeedbacksAsync_HappyPath_ProjectsEveryField()
        {
            //Arrange 1
            var patient = MakePatient("Le Van Patient");
            var feedbackId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var feedback = MakeFeedback(
                feedbackId,
                ClinicId,
                patient,
                ratingDoctor: 4,
                ratingClinic: 5,
                comment: "Decent",
                createdAt: new DateTime(2026, 7, 25, 9, 0, 0, DateTimeKind.Utc));

            //Arrange 2
            SetupFeedbackRepo(new[] { feedback });

            //Act
            var result = await InvokePrivateAsync<List<ClinicFeedbackItem>>(
                _sut, "FetchFeedbacksAsync", ClinicId);

            //Assert
            result.Should().HaveCount(1);
            var item = result[0];
            item.Id.Should().Be(feedbackId);
            item.PatientName.Should().Be("Le Van Patient");
            item.RatingDoctor.Should().Be(4);
            item.RatingClinic.Should().Be(5);
            item.Comment.Should().Be("Decent");
            item.CreatedAt.Should().Be(new DateTime(2026, 7, 25, 9, 0, 0, DateTimeKind.Utc));
        }

        /// <summary>
        /// TC-VCPR-14: Direct reflection call. 25 seeded feedbacks → only 20 returned,
        /// ordered descending by CreatedAt.
        /// </summary>
        [Fact]
        public async Task FetchFeedbacksAsync_Takes20AndOrdersByDescendingCreatedAt()
        {
            //Arrange 1
            var patient = MakePatient("Le Van Patient");
            var rows = Enumerable.Range(0, 25).Select(i => new Feedback
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                DoctorId = Doctor1Id,
                ClinicId = ClinicId,
                RatingDoctor = 5,
                RatingClinic = 5,
                Comment = $"#{i}",
                IsPublic = true,
                CreatedAt = new DateTime(2026, 7, 26, 10, 0, 0, DateTimeKind.Utc).AddMinutes(-i),
                Patient = patient,
                Appointment = new Appointment
                {
                    Id = Guid.NewGuid(),
                    Status = AppointmentStatus.COMPLETED,
                    AppointmentDate = DateTime.UtcNow,
                    PatientId = patient.Id,
                    DoctorId = Doctor1Id
                }
            }).ToList();

            //Arrange 2
            SetupFeedbackRepo(rows);

            //Act
            var result = await InvokePrivateAsync<List<ClinicFeedbackItem>>(
                _sut, "FetchFeedbacksAsync", ClinicId);

            //Assert
            result.Count.Should().Be(20);
            result[0].CreatedAt.Should().Be(new DateTime(2026, 7, 26, 10, 0, 0, DateTimeKind.Utc));
            result[19].CreatedAt.Should().Be(new DateTime(2026, 7, 26, 10, 0, 0, DateTimeKind.Utc).AddMinutes(-19));
        }

        // ==================================================================
        // =========== GetClinicOrThrowAsync(...) — private ==================
        // ==================================================================

        /// <summary>
        /// TC-VCPR-15: Seed clinic → GetClinicOrThrowAsync returns it.
        /// </summary>
        [Fact]
        public async Task GetClinicOrThrowAsync_ClinicFound_ReturnsClinic()
        {
            //Arrange 1
            var clinic = MakeClinic(ClinicId);

            //Arrange 2
            SetupClinicRepo(new[] { clinic });

            //Act
            var result = await InvokePrivateAsync<Clinic>(_sut, "GetClinicOrThrowAsync", ClinicId);

            //Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(ClinicId);
        }

        /// <summary>
        /// TC-VCPR-16: Empty seed → GetClinicOrThrowAsync throws KeyNotFoundException.
        /// </summary>
        [Fact]
        public async Task GetClinicOrThrowAsync_ClinicNotFound_ThrowsKeyNotFound()
        {
            //Arrange 1

            //Arrange 2
            SetupEmptyClinicRepo();

            //Act
            Func<Task> act = async () =>
                await InvokePrivateAsync<Clinic>(_sut, "GetClinicOrThrowAsync", ClinicId);

            //Assert
            var ex = await act.Should().ThrowAsync<KeyNotFoundException>();
            ex.Which.Message.Should().Be(GeneralCode.APP_MESSAGE_4008.ToString());
        }

        // ==================================================================
        // ============== BuildResponse(...) — private static ================
        // ==================================================================

        /// <summary>
        /// TC-VCPR-17: Direct reflection call. Asserts every field of the response is
        /// copied from the clinic + collections.
        /// </summary>
        [Fact]
        public void BuildResponse_AssignsEveryField()
        {
            //Arrange 1
            var clinic = new Clinic
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                Name = "Saigon Eye Clinic",
                Address = "1 Cong Hoa",
                Phone = "0900000000",
                Email = "c@ex.com",
                LogoUrl = "https://x.com/logo.png",
                Description = "Best",
                RatingAvg = 4.7m,
                ReviewCount = 99
            };
            var doctors = new List<ClinicDoctorItem> { new() { Id = Guid.NewGuid(), FullName = "D1" } };
            var services = new List<ClinicServiceItem> { new() { Id = Guid.NewGuid(), ServiceName = "S1" } };
            var feedbacks = new List<ClinicFeedbackItem> { new() { Id = Guid.NewGuid(), PatientName = "P1" } };

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "BuildResponse", clinic, doctors, services, feedbacks)!;
            var response = raw.Should().BeAssignableTo<ViewClinicProfileResponse>().Subject;

            //Assert
            response.Id.Should().Be(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
            response.Name.Should().Be("Saigon Eye Clinic");
            response.Address.Should().Be("1 Cong Hoa");
            response.Phone.Should().Be("0900000000");
            response.Email.Should().Be("c@ex.com");
            response.LogoUrl.Should().Be("https://x.com/logo.png");
            response.Description.Should().Be("Best");
            response.RatingAvg.Should().Be(4.7m);
            response.ReviewCount.Should().Be(99);
            response.Doctors.Should().BeSameAs(doctors);
            response.Services.Should().BeSameAs(services);
            response.Feedbacks.Should().BeSameAs(feedbacks);
        }

        // ==================================================================
        // ========== CreateSuccessResponse(...) — private static ============
        // ==================================================================

        /// <summary>
        /// TC-VCPR-18: Direct reflection call. Wraps the response in ApiResponse<T>.Success
        /// with APP_MESSAGE_2000 and no Meta.
        /// </summary>
        [Fact]
        public void CreateSuccessResponse_WrapsWithCodeMessage2000()
        {
            //Arrange 1
            var payload = new ViewClinicProfileResponse { Id = Guid.NewGuid(), Name = "X" };

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "CreateSuccessResponse", payload);
            var result = raw.Should().BeAssignableTo<ApiResponse<ViewClinicProfileResponse>>().Subject;

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().BeSameAs(payload);
            result.Meta.Should().BeNull();
        }
    }
}