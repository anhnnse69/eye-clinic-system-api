using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Application.Services.ClinicAdminManagementServices.ClinicFeedbackServices;
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
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.ClinicAdminManagementServices.ClinicFeedbackServices
{
    /// <summary>
    /// Unit tests for <see cref="GetClinicFeedbacksService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line coverage on GetClinicFeedbacksService.cs.
    /// </summary>
    /// <remarks>
    /// MockQueryable's in-memory provider does NOT reliably apply the Where()
    /// predicate when chained with multiple <c>.Include().Include().ThenInclude().Include()</c>
    /// calls. Therefore, filter-exclusion assertions through <c>Process(...)</c> are
    /// covered by directly compiling the <see cref="Expression{TDelegate}"/> produced
    /// by the private <c>BuildFilterExpression</c> helper. See
    /// <see cref="GetClinicAppointmentsServiceTests"/> TC-GCA-05b for the same caveat.
    /// </remarks>
    public class GetClinicFeedbacksServiceTests
    {
        private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid ClinicId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid OtherClinicId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        private static readonly Guid FeedbackId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly Guid OtherFeedbackId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        private static readonly Guid DoctorId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        private static readonly Guid DoctorUserId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        private static readonly Guid PatientId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        private static readonly Guid AppointmentId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        private readonly Mock<IRepositoryQueryBase<Feedback, Guid, AppDbContext>> _feedbackRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly GetClinicFeedbacksService _sut;

        public GetClinicFeedbacksServiceTests()
        {
            _sut = new GetClinicFeedbacksService(
                _feedbackRepoMock.Object,
                _staffClinicRepoMock.Object,
                _httpContextAccessorMock.Object);
        }

        // ─────────────────────────────────────────────────────────────────
        // Reflection helpers for private methods
        // ─────────────────────────────────────────────────────────────────

        private static object? InvokePrivate(object target, string methodName, params object[] args)
        {
            var mi = target.GetType().GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);
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

        private static async Task InvokePrivateTaskAsync(object target, string methodName, params object[] args)
        {
            var raw = InvokePrivate(target, methodName, args);
            raw.Should().NotBeNull();
            await (Task)raw!;
        }

        // ─────────────────────────────────────────────────────────────────
        // Setup helpers
        // ─────────────────────────────────────────────────────────────────

        private void SetupClaim(string? value)
        {
            var context = new DefaultHttpContext();
            if (value != null)
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, value) }, "TestAuth"));
            }

            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
        }

        private void SetupNullHttpContext()
        {
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        }

        private void SetupUserWithNullPrincipal()
        {
            var context = new DefaultHttpContext();
            context.User = null!;
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
        }

        private void SetupStaffClinic(StaffClinic? staffClinic)
        {
            var rows = staffClinic == null ? new List<StaffClinic>() : new List<StaffClinic> { staffClinic };
            var queryable = rows.BuildMockDbSet<StaffClinic>();
            _staffClinicRepoMock.Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()))
                .Returns(queryable.Object);
        }

        private void SetupFeedbacks(IEnumerable<Feedback> feedbacks)
        {
            var list = feedbacks.ToList();
            var queryable = list.BuildMockDbSet<Feedback>();
            _feedbackRepoMock.Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<Feedback, bool>>>(), It.IsAny<bool>()))
                .Returns(queryable.Object);
        }

        private static StaffClinic ActiveStaffClinic() => new()
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            ClinicId = ClinicId,
            IsActive = true
        };

        private static User DoctorUser() => new()
        {
            Id = DoctorUserId,
            Phone = "0900000001",
            Email = "doctor@test.vn",
            PasswordHash = "hash",
            FullName = "BS. Nguyen Van Doctor",
            Role = UserRole.DOCTOR,
            IsActive = true
        };

        private static PatientProfile Patient(string fullName = "Nguyen Thi Patient") => new()
        {
            Id = PatientId,
            UserId = Guid.NewGuid(),
            FullName = fullName,
            Gender = Gender.FEMALE,
            Dob = new DateTime(1990, 1, 1)
        };

        private static DoctorProfile Doctor(User doctorUser) => new()
        {
            Id = DoctorId,
            UserId = doctorUser.Id,
            ClinicId = ClinicId,
            Title = "Senior Ophthalmologist",
            ExperienceYears = 10,
            IsActive = true,
            User = doctorUser
        };

        private static Feedback BuildFeedback(
            Guid id,
            Guid clinicId,
            PatientProfile patient,
            DoctorProfile doctor,
            AppointmentStatus appointmentStatus = AppointmentStatus.COMPLETED,
            bool isPublic = true,
            int ratingDoctor = 5,
            int ratingClinic = 5,
            string? comment = "Great service",
            DateTime? createdAt = null,
            DateTime? appointmentDate = null)
        {
            return new Feedback
            {
                Id = id,
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                ClinicId = clinicId,
                RatingDoctor = ratingDoctor,
                RatingClinic = ratingClinic,
                Comment = comment,
                IsPublic = isPublic,
                CreatedAt = createdAt ?? new DateTime(2026, 7, 26, 10, 0, 0, DateTimeKind.Utc),
                Patient = patient,
                Doctor = doctor,
                Appointment = new Appointment
                {
                    Id = AppointmentId,
                    Status = appointmentStatus,
                    AppointmentDate = appointmentDate ?? new DateTime(2026, 7, 25, 9, 0, 0, DateTimeKind.Utc),
                    PatientId = patient.Id,
                    DoctorId = doctor.Id
                }
            };
        }

        // ==================================================================
        // ====================== Process(...) tests ========================
        // ==================================================================

        [Fact]
        public async Task Process_NullHttpContext_ReturnsAuthenticationError()
        {
            //Arrange 1
            var request = new GetClinicFeedbacksRequest();

            //Arrange 2
            // Feedback repo is still consulted because isClinicExist stays true → BuildFilterExpression
            // returns `x => false` and ExecutePagedQuery reaches FindByCondition(...).CountAsync().
            // Seed an empty list so the IAsyncQueryProvider resolves cleanly.
            SetupNullHttpContext();
            SetupFeedbacks(new List<Feedback>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            result.Meta.Should().BeNull();
            _staffClinicRepoMock.Verify(x => x.FindByCondition(
                It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()), Times.Never);
            _feedbackRepoMock.Verify(x => x.FindByCondition(
                It.IsAny<Expression<Func<Feedback, bool>>>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task Process_MissingNameIdentifierClaim_ReturnsAuthenticationError()
        {
            //Arrange 1
            var request = new GetClinicFeedbacksRequest();

            //Arrange 2
            SetupClaim(null);
            SetupFeedbacks(new List<Feedback>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            _staffClinicRepoMock.Verify(x => x.FindByCondition(
                It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()), Times.Never);
            _feedbackRepoMock.Verify(x => x.FindByCondition(
                It.IsAny<Expression<Func<Feedback, bool>>>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task Process_InvalidGuidClaim_ReturnsAuthenticationError()
        {
            //Arrange 1
            var request = new GetClinicFeedbacksRequest();

            //Arrange 2
            SetupClaim("not-a-guid");
            SetupFeedbacks(new List<Feedback>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            _staffClinicRepoMock.Verify(x => x.FindByCondition(
                It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()), Times.Never);
            _feedbackRepoMock.Verify(x => x.FindByCondition(
                It.IsAny<Expression<Func<Feedback, bool>>>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task Process_ValidUserWithoutActiveClinic_ReturnsClinicNotFoundError()
        {
            //Arrange 1
            var request = new GetClinicFeedbacksRequest();

            //Arrange 2
            SetupClaim(UserId.ToString());
            SetupStaffClinic(null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
            result.Data.Should().BeNull();
            result.Meta.Should().BeNull();
            _staffClinicRepoMock.Verify(x => x.FindByCondition(
                It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()), Times.Once);
            _feedbackRepoMock.Verify(x => x.FindByCondition(
                It.IsAny<Expression<Func<Feedback, bool>>>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Process_ValidClinicWithNoFeedbacks_ReturnsSuccessWithEmptyList()
        {
            //Arrange 1
            var request = new GetClinicFeedbacksRequest { PageNumber = 1, PageSize = 10 };

            //Arrange 2
            SetupClaim(UserId.ToString());
            SetupStaffClinic(ActiveStaffClinic());
            SetupFeedbacks(new List<Feedback>());

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
            result.Meta.Should().NotBeNull();
            result.Meta!.Page.Should().Be(1);
            result.Meta.Size.Should().Be(10);
            result.Meta.Total.Should().Be(0);
        }

        [Fact]
        public async Task Process_HappyPath_ReturnsMappedFeedback()
        {
            //Arrange 1
            var request = new GetClinicFeedbacksRequest { PageNumber = 1, PageSize = 10 };
            var patient = Patient("Tran Van Patient");
            var doctorUser = DoctorUser();
            var doctor = Doctor(doctorUser);
            var feedback = BuildFeedback(
                FeedbackId,
                ClinicId,
                patient,
                doctor,
                comment: "Excellent!",
                createdAt: new DateTime(2026, 7, 26, 10, 0, 0, DateTimeKind.Utc),
                appointmentDate: new DateTime(2026, 7, 25, 9, 0, 0, DateTimeKind.Utc));

            //Arrange 2
            SetupClaim(UserId.ToString());
            SetupStaffClinic(ActiveStaffClinic());
            SetupFeedbacks(new List<Feedback> { feedback });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(1);
            var dto = result.Data![0];
            dto.Id_feedback.Should().Be(FeedbackId.ToString());
            dto.PatientName.Should().Be("Tran Van Patient");
            dto.DoctorName.Should().Be("BS. Nguyen Van Doctor");
            dto.RatingDoctor.Should().Be(5);
            dto.RatingClinic.Should().Be(5);
            dto.Comment.Should().Be("Excellent!");
            dto.IsPublic.Should().BeTrue();
            dto.AppointmentDate.Should().Be("25/07/2026");
            dto.FeedbackDate.Should().Be("26/07/2026 10:00");
            result.Meta!.Page.Should().Be(1);
            result.Meta.Size.Should().Be(10);
            result.Meta.Total.Should().Be(1);
        }

        [Fact]
        public async Task Process_PublicCompletedFeedbackFromOwnClinic_ReturnsSuccessWithFeedback()
        {
            //Arrange 1
            var request = new GetClinicFeedbacksRequest();
            var patient = Patient("Tran Van A");
            var doctorUser = DoctorUser();
            var doctor = Doctor(doctorUser);
            var publicFb = BuildFeedback(FeedbackId, ClinicId, patient, doctor, isPublic: true);

            //Arrange 2
            SetupClaim(UserId.ToString());
            SetupStaffClinic(ActiveStaffClinic());
            SetupFeedbacks(new List<Feedback> { publicFb });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Should().HaveCount(1);
            result.Data![0].IsPublic.Should().BeTrue();
        }

        [Fact]
        public async Task Process_MultipleItemsPassThroughPagination()
        {
            //Arrange 1
            // Two items, no filter exclusion (both COMPLETED+public+own clinic), default pagination returns both.
            var request = new GetClinicFeedbacksRequest();
            var patient = Patient("Tran Van A");
            var doctorUser = DoctorUser();
            var doctor = Doctor(doctorUser);
            var fb1 = BuildFeedback(FeedbackId, ClinicId, patient, doctor,
                createdAt: new DateTime(2026, 7, 26, 10, 0, 0));
            var fb2 = BuildFeedback(OtherFeedbackId, ClinicId, patient, doctor,
                createdAt: new DateTime(2026, 7, 27, 10, 0, 0));

            //Arrange 2
            SetupClaim(UserId.ToString());
            SetupStaffClinic(ActiveStaffClinic());
            SetupFeedbacks(new List<Feedback> { fb1, fb2 });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Should().HaveCount(2);
            result.Meta!.Total.Should().Be(2);
        }

        [Fact]
        public async Task Process_PaginationSkipsEarlierItems()
        {
            //Arrange 1
            // Page 2 with size 1 should skip the first (newer) item and return the second.
            // MockQueryable's Skip/Take/OrderByDescending DO work reliably; filtering does not.
            var request = new GetClinicFeedbacksRequest { PageNumber = 2, PageSize = 1 };
            var patient = Patient("Tran Van A");
            var doctorUser = DoctorUser();
            var doctor = Doctor(doctorUser);
            var newer = BuildFeedback(FeedbackId, ClinicId, patient, doctor, createdAt: new DateTime(2026, 7, 27, 10, 0, 0));
            var older = BuildFeedback(OtherFeedbackId, ClinicId, patient, doctor, createdAt: new DateTime(2026, 7, 26, 10, 0, 0));

            //Arrange 2
            SetupClaim(UserId.ToString());
            SetupStaffClinic(ActiveStaffClinic());
            SetupFeedbacks(new List<Feedback> { newer, older });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Should().HaveCount(1);
            result.Data![0].Id_feedback.Should().Be(OtherFeedbackId.ToString());
            result.Meta!.Page.Should().Be(2);
            result.Meta.Size.Should().Be(1);
            result.Meta.Total.Should().Be(2);
        }

        // ==================================================================
        // ================== RetrieveUserId(...) private ===================
        // ==================================================================

        [Fact]
        public void RetrieveUserId_NullHttpContext_SetsFlagFalseAndReturnsEmpty()
        {
            //Arrange 1
            object?[] boxed = { true };

            //Arrange 2
            SetupNullHttpContext();

            //Act
            var result = typeof(GetClinicFeedbacksService)
                .GetMethod("RetrieveUserId", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_sut, boxed);

            //Assert
            boxed[0].Should().Be(false);
            result.Should().Be(Guid.Empty);
        }

        [Fact]
        public void RetrieveUserId_NullUserPrincipal_SetsFlagFalseAndReturnsEmpty()
        {
            //Arrange 1
            object?[] boxed = { true };

            //Arrange 2
            SetupUserWithNullPrincipal();

            //Act
            var result = typeof(GetClinicFeedbacksService)
                .GetMethod("RetrieveUserId", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_sut, boxed);

            //Assert
            boxed[0].Should().Be(false);
            result.Should().Be(Guid.Empty);
        }

        [Fact]
        public void RetrieveUserId_NoNameIdentifierClaim_SetsFlagFalseAndReturnsEmpty()
        {
            //Arrange 1
            object?[] boxed = { true };

            //Arrange 2
            var context = new DefaultHttpContext();
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

            //Act
            var result = typeof(GetClinicFeedbacksService)
                .GetMethod("RetrieveUserId", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_sut, boxed);

            //Assert
            boxed[0].Should().Be(false);
            result.Should().Be(Guid.Empty);
        }

        [Fact]
        public void RetrieveUserId_InvalidGuidClaim_SetsFlagFalseAndReturnsEmpty()
        {
            //Arrange 1
            object?[] boxed = { true };

            //Arrange 2
            SetupClaim("not-a-guid");

            //Act
            var result = typeof(GetClinicFeedbacksService)
                .GetMethod("RetrieveUserId", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_sut, boxed);

            //Assert
            boxed[0].Should().Be(false);
            result.Should().Be(Guid.Empty);
        }

        [Fact]
        public void RetrieveUserId_ValidGuidClaim_ReturnsParsedUserId()
        {
            //Arrange 1
            // isUserValid starts as true (matching Process's initial state); if Guid parse succeeds,
            // the flag remains true (RetrieveUserId does not mutate it on success).
            object?[] boxed = { true };

            //Arrange 2
            SetupClaim(UserId.ToString());

            //Act
            var result = typeof(GetClinicFeedbacksService)
                .GetMethod("RetrieveUserId", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_sut, boxed);

            //Assert
            boxed[0].Should().Be(true);
            result.Should().Be(UserId);
        }

        // ==================================================================
        // ================ RetrieveClinicData(...) private =================
        // ==================================================================

        [Fact]
        public async Task RetrieveClinicData_UserInvalid_ReturnsNullWithoutQueryingRepo()
        {
            //Arrange 1

            //Arrange 2
            // staff-clinic repo not set up → would throw if invoked.

            //Act
            var result = await InvokePrivateAsync<StaffClinic?>(_sut, "RetrieveClinicData", UserId, false);

            //Assert
            result.Should().BeNull();
            _staffClinicRepoMock.Verify(x => x.FindByCondition(
                It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task RetrieveClinicData_NoStaffRow_ReturnsNull()
        {
            //Arrange 1

            //Arrange 2
            SetupStaffClinic(null);

            //Act
            var result = await InvokePrivateAsync<StaffClinic?>(_sut, "RetrieveClinicData", UserId, true);

            //Assert
            result.Should().BeNull();
            _staffClinicRepoMock.Verify(x => x.FindByCondition(
                It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task RetrieveClinicData_StaffRowExists_ReturnsStaffClinic()
        {
            //Arrange 1

            //Arrange 2
            var staff = ActiveStaffClinic();
            SetupStaffClinic(staff);

            //Act
            var result = await InvokePrivateAsync<StaffClinic?>(_sut, "RetrieveClinicData", UserId, true);

            //Assert
            result.Should().BeSameAs(staff);
        }

        // ==================================================================
        // ============== ValidateClinicContext(...) private =================
        // ==================================================================

        [Fact]
        public void ValidateClinicContext_UserInvalid_LeavesClinicFlagUnchanged()
        {
            //Arrange 1
            var staff = ActiveStaffClinic();
            var mi = typeof(GetClinicFeedbacksService)
                .GetMethod("ValidateClinicContext", BindingFlags.NonPublic | BindingFlags.Instance)!;

            //Arrange 2

            //Act
            var args = new object?[] { staff, false, true };
            mi.Invoke(_sut, args);

            //Assert
            args[2].Should().Be(true);
        }

        [Fact]
        public void ValidateClinicContext_NullStaffClinic_SetsClinicFalse()
        {
            //Arrange 1
            var mi = typeof(GetClinicFeedbacksService)
                .GetMethod("ValidateClinicContext", BindingFlags.NonPublic | BindingFlags.Instance)!;

            //Arrange 2

            //Act
            var args = new object?[] { null, true, true };
            mi.Invoke(_sut, args);

            //Assert
            args[2].Should().Be(false);
        }

        [Fact]
        public void ValidateClinicContext_NonNullStaffClinic_LeavesClinicFlagTrue()
        {
            //Arrange 1
            var staff = ActiveStaffClinic();
            var mi = typeof(GetClinicFeedbacksService)
                .GetMethod("ValidateClinicContext", BindingFlags.NonPublic | BindingFlags.Instance)!;

            //Arrange 2

            //Act
            var args = new object?[] { staff, true, true };
            mi.Invoke(_sut, args);

            //Assert
            args[2].Should().Be(true);
        }

        // ==================================================================
        // ============== BuildFilterExpression(...) private =================
        // ==================================================================

        [Fact]
        public void BuildFilterExpression_ClinicDoesNotExist_ReturnsFalseExpression()
        {
            //Arrange 1

            //Arrange 2

            //Act
            var expr = (Expression<Func<Feedback, bool>>)InvokePrivate(
                _sut, "BuildFilterExpression",
                ActiveStaffClinic(),
                new GetClinicFeedbacksRequest(),
                false)!;

            //Assert
            expr.Should().NotBeNull();
            var compiled = expr.Compile();
            var patient = Patient();
            var doctor = Doctor(DoctorUser());
            var fb = BuildFeedback(FeedbackId, ClinicId, patient, doctor);
            compiled(fb).Should().BeFalse();
        }

        [Fact]
        public void BuildFilterExpression_StaffClinicNull_ReturnsFalseExpression()
        {
            //Arrange 1

            //Arrange 2

            //Act
            var expr = (Expression<Func<Feedback, bool>>)InvokePrivate(
                _sut, "BuildFilterExpression",
                (StaffClinic?)null,
                new GetClinicFeedbacksRequest(),
                true)!;

            //Assert
            expr.Should().NotBeNull();
            var compiled = expr.Compile();
            var patient = Patient();
            var doctor = Doctor(DoctorUser());
            var fb = BuildFeedback(FeedbackId, ClinicId, patient, doctor);
            compiled(fb).Should().BeFalse();
        }

        [Fact]
        public void BuildFilterExpression_HappyPath_BuildsCompositeFilter()
        {
            //Arrange 1
            var request = new GetClinicFeedbacksRequest { SearchTerm = "  PATIENT " };

            //Arrange 2

            //Act
            var expr = (Expression<Func<Feedback, bool>>)InvokePrivate(
                _sut, "BuildFilterExpression",
                ActiveStaffClinic(),
                request,
                true)!;

            //Assert
            var compiled = expr.Compile();
            var patient = Patient("Nguyen Van Patient");
            var doctor = Doctor(DoctorUser());
            var ownClinicCompletedPublic = BuildFeedback(FeedbackId, ClinicId, patient, doctor);
            var otherClinicMatch = BuildFeedback(OtherFeedbackId, OtherClinicId, patient, doctor);
            var privateMatch = BuildFeedback(OtherFeedbackId, ClinicId, patient, doctor, isPublic: false);
            var pendingMatch = BuildFeedback(OtherFeedbackId, ClinicId, patient, doctor, appointmentStatus: AppointmentStatus.PENDING);

            compiled(ownClinicCompletedPublic).Should().BeTrue();
            compiled(otherClinicMatch).Should().BeFalse();
            compiled(privateMatch).Should().BeFalse();
            compiled(pendingMatch).Should().BeFalse();
        }

        [Fact]
        public void BuildFilterExpression_NullSearchTerm_FallsThroughBranch()
        {
            //Arrange 1
            var request = new GetClinicFeedbacksRequest { SearchTerm = null };

            //Arrange 2

            //Act
            var expr = (Expression<Func<Feedback, bool>>)InvokePrivate(
                _sut, "BuildFilterExpression",
                ActiveStaffClinic(),
                request,
                true)!;

            //Assert
            expr.Should().NotBeNull();
            var compiled = expr.Compile();
            var patient = Patient();
            var doctor = Doctor(DoctorUser());
            var fb = BuildFeedback(FeedbackId, ClinicId, patient, doctor);
            compiled(fb).Should().BeTrue();
        }

        [Fact]
        public void BuildFilterExpression_EmptySearchTerm_FallsThroughBranch()
        {
            //Arrange 1
            var request = new GetClinicFeedbacksRequest { SearchTerm = "" };

            //Arrange 2

            //Act
            var expr = (Expression<Func<Feedback, bool>>)InvokePrivate(
                _sut, "BuildFilterExpression",
                ActiveStaffClinic(),
                request,
                true)!;

            //Assert
            expr.Should().NotBeNull();
            var compiled = expr.Compile();
            var patient = Patient();
            var doctor = Doctor(DoctorUser());
            var fb = BuildFeedback(FeedbackId, ClinicId, patient, doctor);
            compiled(fb).Should().BeTrue();
        }

        [Fact]
        public void BuildFilterExpression_SearchTermMatchesNullCommentBranch()
        {
            //Arrange 1
            var request = new GetClinicFeedbacksRequest { SearchTerm = "patient" };
            var patient = Patient("Nguyen Van Patient");
            var doctor = Doctor(DoctorUser());
            var fbNoComment = BuildFeedback(FeedbackId, ClinicId, patient, doctor, comment: null);

            //Arrange 2

            //Act
            var expr = (Expression<Func<Feedback, bool>>)InvokePrivate(
                _sut, "BuildFilterExpression",
                ActiveStaffClinic(),
                request,
                true)!;

            //Assert
            var compiled = expr.Compile();
            // The expression contains a `x.Comment != null && x.Comment.ToLower().Contains(searchTerm)`
            // branch; when Comment is null, the branch is false, but other SearchTerm-match arms
            // (PatientName, DoctorName) can still evaluate to true.
            compiled(fbNoComment).Should().BeTrue();
        }

        // ==================================================================
        // ============== ExecutePagedQuery(...) private =====================
        // ==================================================================

        [Fact]
        public async Task ExecutePagedQuery_ClinicDoesNotExist_ReturnsEmptyResultWithoutRepoCall()
        {
            //Arrange 1
            Expression<Func<Feedback, bool>> expr = f => true;

            //Arrange 2
            // feedback repo not set up → would throw if invoked.

            //Act
            var result = await InvokePrivateAsync<(List<Feedback> Feedbacks, int TotalRecords)>(
                _sut, "ExecutePagedQuery", expr, new GetClinicFeedbacksRequest(), false);

            //Assert
            result.Feedbacks.Should().BeEmpty();
            result.TotalRecords.Should().Be(0);
            _feedbackRepoMock.Verify(x => x.FindByCondition(
                It.IsAny<Expression<Func<Feedback, bool>>>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task ExecutePagedQuery_HappyPath_PaginatesAndOrdersDescending()
        {
            //Arrange 1
            Expression<Func<Feedback, bool>> expr = f => true;
            var request = new GetClinicFeedbacksRequest { PageNumber = 1, PageSize = 2 };
            var patient = Patient();
            var doctor = Doctor(DoctorUser());
            var fb1 = BuildFeedback(FeedbackId, ClinicId, patient, doctor, createdAt: new DateTime(2026, 7, 25, 10, 0, 0));
            var fb2 = BuildFeedback(OtherFeedbackId, ClinicId, patient, doctor, createdAt: new DateTime(2026, 7, 26, 10, 0, 0));

            //Arrange 2
            SetupFeedbacks(new List<Feedback> { fb1, fb2 });

            //Act
            var result = await InvokePrivateAsync<(List<Feedback> Feedbacks, int TotalRecords)>(
                _sut, "ExecutePagedQuery", expr, request, true);

            //Assert
            result.TotalRecords.Should().Be(2);
            result.Feedbacks.Should().HaveCount(2);
            result.Feedbacks[0].Id.Should().Be(OtherFeedbackId); // newer first
            result.Feedbacks[1].Id.Should().Be(FeedbackId);
        }

        // ==================================================================
        // ================= MapToResponseDto(...) private ===================
        // ==================================================================

        [Fact]
        public void MapToResponseDto_EmptyList_ReturnsEmptyList()
        {
            //Arrange 1

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "MapToResponseDto", new List<Feedback>());
            var result = raw.Should().BeAssignableTo<List<GetClinicFeedbackResponse>>().Subject;

            //Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void MapToResponseDto_PopulatedList_MapsAllFields()
        {
            //Arrange 1
            var patient = Patient("Nguyen Van A");
            var doctorUser = DoctorUser(); // "BS. Nguyen Van Doctor"
            var doctor = Doctor(doctorUser);
            var feedback = BuildFeedback(
                FeedbackId,
                ClinicId,
                patient,
                doctor,
                comment: "Great service",
                createdAt: new DateTime(2026, 7, 26, 10, 0, 0, DateTimeKind.Utc),
                appointmentDate: new DateTime(2026, 7, 25, 9, 30, 0, DateTimeKind.Utc));

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "MapToResponseDto", new List<Feedback> { feedback });
            var result = raw.Should().BeAssignableTo<List<GetClinicFeedbackResponse>>().Subject;

            //Assert
            result.Should().HaveCount(1);
            var dto = result[0];
            dto.Id_feedback.Should().Be(FeedbackId.ToString());
            dto.PatientName.Should().Be("Nguyen Van A");
            dto.DoctorName.Should().Be("BS. Nguyen Van Doctor");
            dto.RatingDoctor.Should().Be(5);
            dto.RatingClinic.Should().Be(5);
            dto.Comment.Should().Be("Great service");
            dto.IsPublic.Should().BeTrue();
            dto.AppointmentDate.Should().Be("25/07/2026");
            dto.FeedbackDate.Should().Be("26/07/2026 10:00");
        }

        [Fact]
        public void MapToResponseDto_NullComment_ReplacedWithNA()
        {
            //Arrange 1
            var patient = Patient();
            var doctor = Doctor(DoctorUser());
            var feedback = BuildFeedback(FeedbackId, ClinicId, patient, doctor, comment: null);

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "MapToResponseDto", new List<Feedback> { feedback });
            var result = raw.Should().BeAssignableTo<List<GetClinicFeedbackResponse>>().Subject;

            //Assert
            result[0].Comment.Should().Be("N/A");
        }

        [Fact]
        public void MapToResponseDto_WhitespaceComment_ReplacedWithNA()
        {
            //Arrange 1
            var patient = Patient();
            var doctor = Doctor(DoctorUser());
            var feedback = BuildFeedback(FeedbackId, ClinicId, patient, doctor, comment: "   ");

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "MapToResponseDto", new List<Feedback> { feedback });
            var result = raw.Should().BeAssignableTo<List<GetClinicFeedbackResponse>>().Subject;

            //Assert
            result[0].Comment.Should().Be("N/A");
        }

        // ==================================================================
        // ============== BuildPaginationMeta(...) private ===================
        // ==================================================================

        [Fact]
        public void BuildPaginationMeta_PopulatesFieldsCorrectly()
        {
            //Arrange 1
            var request = new GetClinicFeedbacksRequest { PageNumber = 3, PageSize = 25 };

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "BuildPaginationMeta", request, 75);
            var meta = raw.Should().BeOfType<MetaResponse>().Subject;

            //Assert
            meta.Page.Should().Be(3);
            meta.Size.Should().Be(25);
            meta.Total.Should().Be(75);
        }

        // ==================================================================
        // ============== CreateErrorResponse(...) private ===================
        // ==================================================================

        [Fact]
        public void CreateErrorResponse_UserInvalid_ReturnsAuthenticationFailure()
        {
            //Arrange 1

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "CreateErrorResponse", false, true);
            var result = raw.Should().BeAssignableTo<ApiResponse<List<GetClinicFeedbackResponse>>>().Subject;

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
        }

        [Fact]
        public void CreateErrorResponse_ClinicMissing_ReturnsClinicFailure()
        {
            //Arrange 1

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "CreateErrorResponse", true, false);
            var result = raw.Should().BeAssignableTo<ApiResponse<List<GetClinicFeedbackResponse>>>().Subject;

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
        }

        [Fact]
        public void CreateErrorResponse_AllValid_ReturnsNull()
        {
            //Arrange 1

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "CreateErrorResponse", true, true);

            //Assert
            raw.Should().BeNull();
        }
    }
}
