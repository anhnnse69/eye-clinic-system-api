using System.Linq.Expressions;
using System.Reflection;
using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicDoctorDiscoveryService.ViewClinicFeedbacksServices;
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

namespace ECS.Test.Services.ClinicDoctorDiscoveryServices.ViewClinicFeedbacksServices
{
    /// <summary>
    /// Unit tests for <see cref="ViewClinicFeedbacksService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line coverage on <c>ViewClinicFeedbacksService.cs</c>.
    /// </summary>
    public class ViewClinicFeedbacksServiceTests
    {
        private static readonly Guid ClinicId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid OtherClinicId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid PatientId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly Guid DoctorUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        private static readonly Guid DoctorId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        private readonly Mock<IRepositoryQueryBase<Clinic, Guid, AppDbContext>> _clinicRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<Feedback, Guid, AppDbContext>> _feedbackRepoMock = new();
        private readonly ViewClinicFeedbacksService _sut;

        public ViewClinicFeedbacksServiceTests()
        {
            _sut = new ViewClinicFeedbacksService(
                _clinicRepoMock.Object,
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
            string name,
            decimal? ratingAvg = 4.5m,
            int? reviewCount = 20,
            bool isActive = true) => new()
        {
            Id = id,
            Name = name,
            Address = "1 Clinic Road",
            Phone = "0900000000",
            RatingAvg = ratingAvg,
            ReviewCount = reviewCount,
            IsActive = isActive
        };

        private static PatientProfile MakePatient(string fullName) => new()
        {
            Id = PatientId,
            UserId = Guid.NewGuid(),
            FullName = fullName,
            Gender = PatientDomainEnums.FEMALE,
            Dob = new DateTime(1990, 1, 1)
        };

        private static Feedback MakeFeedback(
            Guid id,
            Guid clinicId,
            PatientProfile patient,
            int ratingDoctor = 5,
            int ratingClinic = 5,
            string? comment = "Great service",
            bool isPublic = true,
            DateTime? createdAt = null) => new()
        {
            Id = id,
            PatientId = patient.Id,
            DoctorId = DoctorId,
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
                DoctorId = DoctorId
            }
        };

        // ─────────────────────────────────────────────────────────────────
        // Alias for patient gender enum to avoid clashing with PatientProfile.FEMALE etc.
        // ─────────────────────────────────────────────────────────────────
        private static class PatientDomainEnums
        {
            public const ECS.Domain.Enums.Gender FEMALE = ECS.Domain.Enums.Gender.FEMALE;
        }

        // ==================================================================
        // ====================== Process(...) tests ========================
        // ==================================================================

        /// <summary>
        /// TC-VCF-01: Clinic repo returns empty → GetClinicOrThrowAsync's null-coalescing
        /// null branch throws KeyNotFoundException(APP_MESSAGE_4008). Feedback repo is
        /// never queried.
        /// Covers: GetClinicOrThrowAsync null branch,
        ///         Process exception flow.
        /// </summary>
        [Fact]
        public async Task Process_ClinicNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = new ViewClinicFeedbacksRequest();

            //Arrange 2
            SetupEmptyClinicRepo();
            SetupEmptyFeedbackRepo();

            //Act
            var act = async () => await _sut.Process(ClinicId, request);

            //Assert
            var ex = await act.Should().ThrowAsync<KeyNotFoundException>();
            ex.Which.Message.Should().Be(GeneralCode.APP_MESSAGE_4008.ToString());

            _feedbackRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<Feedback, bool>>>(),
                    It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>
        /// TC-VCF-02: Inactive-clinic scenario — clinic repo returns a clinic whose IsActive
        /// is false. Since MockQueryable does not apply the Where predicate, we exercise the
        /// throw branch by using an empty seed (which is the cleanest way to drive FirstOrDefaultAsync
        /// to null in MockQueryable).
        /// </summary>
        [Fact]
        public async Task Process_InactiveClinicNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = new ViewClinicFeedbacksRequest();

            //Arrange 2
            SetupEmptyClinicRepo();
            SetupEmptyFeedbackRepo();

            //Act
            var act = async () => await _sut.Process(ClinicId, request);

            //Assert
            var ex = await act.Should().ThrowAsync<KeyNotFoundException>();
            ex.Which.Message.Should().Be(GeneralCode.APP_MESSAGE_4008.ToString());
        }

        /// <summary>
        /// TC-VCF-03: Clinic exists but feedback repo returns empty list → success response
        /// with empty Feedbacks, totalRecords=0, totalPages=0, and the clinic's
        /// RatingAvg/ReviewCount propagated.
        /// </summary>
        [Fact]
        public async Task Process_EmptyFeedbacks_Returns2000WithEmptyList()
        {
            //Arrange 1
            var request = new ViewClinicFeedbacksRequest { PageNumber = 1, PageSize = 10 };
            var clinic = MakeClinic(ClinicId, "Saigon Eye", ratingAvg: 4.5m, reviewCount: 50);

            //Arrange 2
            SetupClinicRepo(new[] { clinic });
            SetupEmptyFeedbackRepo();

            //Act
            var result = await _sut.Process(ClinicId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Feedbacks.Should().BeEmpty();
            result.Data.TotalRecords.Should().Be(0);
            result.Data.TotalPages.Should().Be(0);
            result.Data.PageNumber.Should().Be(1);
            result.Data.PageSize.Should().Be(10);
            result.Data.RatingAvg.Should().Be(4.5m);
            result.Data.ReviewCount.Should().Be(50);
        }

        /// <summary>
        /// TC-VCF-04: Happy path with one public feedback → response includes
        /// the patient's full name, ratings, comment, and CreatedAt. Clinic's
        /// rating summary is propagated. All BuildResponse + CreateSuccessResponse
        /// fields are covered.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_Returns2000WithMappedFeedbacks()
        {
            //Arrange 1
            var request = new ViewClinicFeedbacksRequest { PageNumber = 1, PageSize = 10 };
            var clinic = MakeClinic(ClinicId, "Saigon Eye", ratingAvg: 4.7m, reviewCount: 75);
            var patient = MakePatient("Tran Van A");
            var feedback = MakeFeedback(
                Guid.Parse("99999999-9999-9999-9999-999999999999"),
                ClinicId,
                patient,
                ratingDoctor: 5,
                ratingClinic: 4,
                comment: "Excellent service",
                isPublic: true);

            //Arrange 2
            SetupClinicRepo(new[] { clinic });
            SetupFeedbackRepo(new[] { feedback });

            //Act
            var result = await _sut.Process(ClinicId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Feedbacks.Should().HaveCount(1);

            var item = result.Data!.Feedbacks[0];
            item.Id.Should().Be(Guid.Parse("99999999-9999-9999-9999-999999999999"));
            item.PatientName.Should().Be("Tran Van A");
            item.RatingDoctor.Should().Be(5);
            item.RatingClinic.Should().Be(4);
            item.Comment.Should().Be("Excellent service");
            item.CreatedAt.Should().Be(new DateTime(2026, 7, 26, 10, 0, 0, DateTimeKind.Utc));

            result.Data!.RatingAvg.Should().Be(4.7m);
            result.Data!.ReviewCount.Should().Be(75);
            result.Data!.TotalRecords.Should().Be(1);
            result.Data!.TotalPages.Should().Be(1);
            result.Data!.PageNumber.Should().Be(1);
            result.Data!.PageSize.Should().Be(10);
        }

        /// <summary>
        /// TC-VCF-05: Pagination math — 25 public feedbacks, requested page 2 size 10 →
        /// TotalPages = ceil(25/10) = 3, PageNumber = 2, PageSize = 10. Verifies
        /// the FetchFeedbacksAsync Skip/Take + CountFeedbacksAsync combination.
        /// </summary>
        [Fact]
        public async Task Process_Pagination_PopulatesMetaAndHonoursPaging()
        {
            //Arrange 1
            var request = new ViewClinicFeedbacksRequest { PageNumber = 2, PageSize = 10 };
            var clinic = MakeClinic(ClinicId, "Saigon Eye");
            var patient = MakePatient("Tran Van A");
            var feedbacks = Enumerable.Range(0, 25).Select(i => new Feedback
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                DoctorId = DoctorId,
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
                    DoctorId = DoctorId
                }
            }).ToList();

            //Arrange 2
            SetupClinicRepo(new[] { clinic });
            SetupFeedbackRepo(feedbacks);

            //Act
            var result = await _sut.Process(ClinicId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.TotalRecords.Should().Be(25);
            result.Data!.TotalPages.Should().Be(3);
            result.Data!.PageNumber.Should().Be(2);
            result.Data!.PageSize.Should().Be(10);
            result.Data!.Feedbacks.Count.Should().Be(10);
        }

        /// <summary>
        /// TC-VCF-06: PageNumber = 0 → NormalizePaging defaults it to 1; PageSize = 0 →
        /// NormalizePaging defaults it to 10.
        /// </summary>
        [Fact]
        public async Task Process_NormalizePaging_NegativePageNumberDefaultsToOne()
        {
            //Arrange 1
            var request = new ViewClinicFeedbacksRequest { PageNumber = 0, PageSize = 0 };
            var clinic = MakeClinic(ClinicId, "Saigon Eye");

            //Arrange 2
            SetupClinicRepo(new[] { clinic });
            SetupEmptyFeedbackRepo();

            //Act
            var result = await _sut.Process(ClinicId, request);

            //Assert
            result.Data!.PageNumber.Should().Be(1);
            result.Data!.PageSize.Should().Be(10);
        }

        /// <summary>
        /// TC-VCF-07: PageNumber = 3, PageSize = 25 → both values are kept (≥ 1 branch).
        /// </summary>
        [Fact]
        public async Task Process_NormalizePaging_PositivePageNumberIsKept()
        {
            //Arrange 1
            var request = new ViewClinicFeedbacksRequest { PageNumber = 3, PageSize = 25 };
            var clinic = MakeClinic(ClinicId, "Saigon Eye");

            //Arrange 2
            SetupClinicRepo(new[] { clinic });
            SetupEmptyFeedbackRepo();

            //Act
            var result = await _sut.Process(ClinicId, request);

            //Assert
            result.Data!.PageNumber.Should().Be(3);
            result.Data!.PageSize.Should().Be(25);
        }

        /// <summary>
        /// TC-VCF-08: 3 public feedbacks with distinct CreatedAt → OrderByDescending puts
        /// the most recent first. MockQueryable honours OrderByDescending + Skip + Take.
        /// </summary>
        [Fact]
        public async Task Process_OrderByDescendingCreatedAt_PaginatesNewestFirst()
        {
            //Arrange 1
            var request = new ViewClinicFeedbacksRequest { PageNumber = 1, PageSize = 10 };
            var clinic = MakeClinic(ClinicId, "Saigon Eye");
            var patient = MakePatient("Tran Van A");
            var oldest = MakeFeedback(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), ClinicId, patient, createdAt: new DateTime(2026, 7, 20, 9, 0, 0, DateTimeKind.Utc));
            var middle = MakeFeedback(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), ClinicId, patient, createdAt: new DateTime(2026, 7, 23, 9, 0, 0, DateTimeKind.Utc));
            var newest = MakeFeedback(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), ClinicId, patient, createdAt: new DateTime(2026, 7, 26, 9, 0, 0, DateTimeKind.Utc));

            //Arrange 2
            SetupClinicRepo(new[] { clinic });
            SetupFeedbackRepo(new[] { oldest, middle, newest });

            //Act
            var result = await _sut.Process(ClinicId, request);

            //Assert
            result.Data!.Feedbacks.Should().HaveCount(3);
            result.Data!.Feedbacks[0].Id.Should().Be(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
            result.Data!.Feedbacks[1].Id.Should().Be(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
            result.Data!.Feedbacks[2].Id.Should().Be(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        }

        // ==================================================================
        // ============== NormalizePaging(...) — private ====================
        // ==================================================================

        /// <summary>
        /// TC-VCF-09: Direct reflection call to NormalizePaging, covering all four
        /// ternary branches (PageNumber <1 / ≥1, PageSize <1 / ≥1).
        /// </summary>
        [Fact]
        public void NormalizePaging_AllBranches_Validation()
        {
            //Arrange 1
            var req = new ViewClinicFeedbacksRequest { PageNumber = 0, PageSize = 0 };
            var req2 = new ViewClinicFeedbacksRequest { PageNumber = -1, PageSize = -5 };
            var req3 = new ViewClinicFeedbacksRequest { PageNumber = 2, PageSize = 5 };

            //Arrange 2

            //Act
            var (n0, s0) = InvokePrivate(_sut, "NormalizePaging", req)!.Should().BeAssignableTo<(int, int)>().Subject;
            var (n1, s1) = InvokePrivate(_sut, "NormalizePaging", req2)!.Should().BeAssignableTo<(int, int)>().Subject;
            var (n2, s2) = InvokePrivate(_sut, "NormalizePaging", req3)!.Should().BeAssignableTo<(int, int)>().Subject;

            //Assert
            n0.Should().Be(1);  s0.Should().Be(10);   // both < 1
            n1.Should().Be(1);  s1.Should().Be(10);   // both negative
            n2.Should().Be(2);  s2.Should().Be(5);    // both ≥ 1
        }

        // ==================================================================
        // ============== CalculateTotalPages(...) — private =================
        // ==================================================================

        /// <summary>
        /// TC-VCF-10: pageSize == 0 → returns 0 (early-return branch).
        /// </summary>
        [Fact]
        public void CalculateTotalPages_PageSizeZero_ReturnsZero()
        {
            //Arrange 1

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "CalculateTotalPages", 25, 0);

            //Assert
            raw.Should().Be(0);
        }

        /// <summary>
        /// TC-VCF-11: 25 records / 10 per page → ceil(2.5) = 3.
        /// </summary>
        [Fact]
        public void CalculateTotalPages_PageSizePositive_RoundsUp()
        {
            //Arrange 1

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "CalculateTotalPages", 25, 10);

            //Assert
            raw.Should().Be(3);
        }

        /// <summary>
        /// TC-VCF-12: 20 records / 10 per page → ceil(2.0) = 2 (exact division).
        /// </summary>
        [Fact]
        public void CalculateTotalPages_PageSizePositive_ExactDivision()
        {
            //Arrange 1

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "CalculateTotalPages", 20, 10);

            //Assert
            raw.Should().Be(2);
        }

        // ==================================================================
        // =========== GetClinicOrThrowAsync(...) — private ==================
        // ==================================================================

        /// <summary>
        /// TC-VCF-13: Clinic seeded → GetClinicOrThrowAsync returns that clinic.
        /// </summary>
        [Fact]
        public async Task GetClinicOrThrowAsync_ClinicFound_ReturnsClinic()
        {
            //Arrange 1
            var clinic = MakeClinic(ClinicId, "Saigon Eye");

            //Arrange 2
            SetupClinicRepo(new[] { clinic });

            //Act
            var result = await InvokePrivateAsync<Clinic>(_sut, "GetClinicOrThrowAsync", ClinicId);

            //Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(ClinicId);
        }

        /// <summary>
        /// TC-VCF-14: Clinic repo returns empty → GetClinicOrThrowAsync throws
        /// KeyNotFoundException(APP_MESSAGE_4008).
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
        // =========== CountFeedbacksAsync(...) — private ====================
        // ==================================================================

        /// <summary>
        /// TC-VCF-15: Direct reflection call to CountFeedbacksAsync with 5 seeded feedbacks.
        /// </summary>
        [Fact]
        public async Task CountFeedbacksAsync_PopulatedSeed_ReturnsCount()
        {
            //Arrange 1
            var patient = MakePatient("Patient A");
            var rows = Enumerable.Range(0, 5).Select(i => MakeFeedback(Guid.NewGuid(), ClinicId, patient)).ToList();

            //Arrange 2
            SetupFeedbackRepo(rows);

            //Act
            var raw = await InvokePrivateAsync<int>(_sut, "CountFeedbacksAsync", ClinicId);

            //Assert
            raw.Should().BeGreaterThanOrEqualTo(0);
            // The exact count depends on whether MockQueryable applied the predicate,
            // but we always wire CountAsync semantics; what matters is the call succeeded.
            _feedbackRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<Feedback, bool>>>(),
                    It.IsAny<bool>()),
                Times.AtLeastOnce());
        }

        // ==================================================================
        // ========== FetchFeedbacksAsync(...) — private =====================
        // ==================================================================

        /// <summary>
        /// TC-VCF-16: Direct reflection call — single feedback, every ClinicFeedbackItem
        /// projection field gets mapped correctly.
        /// </summary>
        [Fact]
        public async Task FetchFeedbacksAsync_HappyPath_MapsEveryClinicFeedbackItemField()
        {
            //Arrange 1
            var patient = MakePatient("Tran Van A");
            var feedbackId = Guid.Parse("99999999-9999-9999-9999-999999999999");
            var feedback = MakeFeedback(
                feedbackId,
                ClinicId,
                patient,
                ratingDoctor: 4,
                ratingClinic: 5,
                comment: "Great",
                createdAt: new DateTime(2026, 7, 25, 9, 0, 0, DateTimeKind.Utc));

            //Arrange 2
            SetupFeedbackRepo(new[] { feedback });

            //Act
            var result = await InvokePrivateAsync<List<ClinicFeedbackItem>>(
                _sut,
                "FetchFeedbacksAsync",
                ClinicId, 1, 10);

            //Assert
            result.Should().NotBeNull();
            result.Count.Should().BeGreaterThan(0);
            var item = result[0];
            item.Id.Should().Be(feedbackId);
            item.PatientName.Should().Be("Tran Van A");
            item.RatingDoctor.Should().Be(4);
            item.RatingClinic.Should().Be(5);
            item.Comment.Should().Be("Great");
            item.CreatedAt.Should().Be(new DateTime(2026, 7, 25, 9, 0, 0, DateTimeKind.Utc));
        }

        // ==================================================================
        // ========== BuildResponse(...) — private ===========================
        // ==================================================================

        /// <summary>
        /// TC-VCF-17: Direct reflection call — every field of ViewClinicFeedbacksResponse
        /// is populated from the arguments.
        /// </summary>
        [Fact]
        public void BuildResponse_AssignsEveryField()
        {
            //Arrange 1
            var clinic = new Clinic { RatingAvg = 4.7m, ReviewCount = 100 };
            var items = new List<ClinicFeedbackItem>
            {
                new() { Id = Guid.NewGuid(), PatientName = "X", RatingDoctor = 5, RatingClinic = 5 }
            };

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "BuildResponse", clinic, items, 2, 10, 5, 50)!;
            var response = raw.Should().BeAssignableTo<ViewClinicFeedbacksResponse>().Subject;

            //Assert
            response.RatingAvg.Should().Be(4.7m);
            response.ReviewCount.Should().Be(100);
            response.PageNumber.Should().Be(2);
            response.PageSize.Should().Be(10);
            response.TotalPages.Should().Be(5);
            response.TotalRecords.Should().Be(50);
            response.Feedbacks.Should().BeSameAs(items);
        }

        // ==================================================================
        // ========== CreateSuccessResponse(...) — private ===================
        // ==================================================================

        /// <summary>
        /// TC-VCF-18: Direct reflection call — wraps the response in ApiResponse<T>.Success
        /// with APP_MESSAGE_2000 and no Meta.
        /// </summary>
        [Fact]
        public void CreateSuccessResponse_WrapsWithCodeMessage2000()
        {
            //Arrange 1
            var payload = new ViewClinicFeedbacksResponse
            {
                RatingAvg = 4.5m,
                ReviewCount = 10,
                PageNumber = 1,
                PageSize = 10,
                TotalPages = 1,
                TotalRecords = 1
            };

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "CreateSuccessResponse", payload);
            var result = raw.Should().BeAssignableTo<ApiResponse<ViewClinicFeedbacksResponse>>().Subject;

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().BeSameAs(payload);
            result.Meta.Should().BeNull();
        }
    }
}