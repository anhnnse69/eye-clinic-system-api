using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Application.Services.DoctorAppointmentPatientManagementServices.CompleteQueueServices;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.DoctorAppointmentPatientManagementServices.CompleteQueueServices
{
    /// <summary>
    /// Unit tests for <see cref="CompleteQueueService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line coverage on <c>CompleteQueueService.cs</c>.
    /// </summary>
    public class CompleteQueueServiceTests : IDisposable
    {
        private static readonly Guid QueueId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid AppointmentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid PatientId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly Guid DoctorUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        private static readonly Guid DoctorId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        private static readonly Guid ClinicId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        private static readonly Guid PreliminaryDiagnosisId = Guid.Parse("77777777-7777-7777-7777-777777777777");

        private readonly Mock<IRepositoryQueryBase<Queue, Guid, AppDbContext>> _queueRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<Appointment, Guid, AppDbContext>> _appointmentRepoMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly IValidator<CompleteQueueRequest> _validator = new CompleteQueueRequestValidator();
        private readonly AppDbContext _context;
        private readonly TextWriter _originalConsoleOut;
        private readonly CompleteQueueService _sut;

        public CompleteQueueServiceTests()
        {
            // Real AppDbContext with EF Core In-Memory provider — required because the
            // service uses _context.Queues.Update(...) and _context.SaveChangesAsync(),
            // and the DbSet properties on AppDbContext are not marked virtual (cannot be
            // mocked directly).
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _sut = new CompleteQueueService(
                _queueRepoMock.Object,
                _appointmentRepoMock.Object,
                _validator,
                _context,
                _httpContextAccessorMock.Object);

            // Suppress the [CompleteQueue] Console.WriteLine debug log during tests.
            _originalConsoleOut = Console.Out;
            Console.SetOut(TextWriter.Null);
        }

        public void Dispose()
        {
            Console.SetOut(_originalConsoleOut);
            _context.Database.EnsureDeleted();
            _context.Dispose();
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

        private static async Task InvokePrivateAsync(object target, string methodName, params object[] args)
        {
            var raw = InvokePrivate(target, methodName, args);
            raw.Should().NotBeNull();
            var task = (Task)raw!;
            await task;
        }

        private static object CreateState() =>
            Activator.CreateInstance(
                typeof(CompleteQueueService).GetNestedType(
                    "ExecutionState",
                    BindingFlags.NonPublic)!,
                nonPublic: true)!;

        private static void SetStateProperty(object state, string propName, object? value)
        {
            var prop = state.GetType().GetProperty(propName)!;
            prop.SetValue(state, value);
        }

        private static object? GetStateProperty(object state, string propName)
        {
            var prop = state.GetType().GetProperty(propName)!;
            return prop.GetValue(state);
        }

        // ─────────────────────────────────────────────────────────────────
        // Repository helpers
        // ─────────────────────────────────────────────────────────────────

        private void SetupQueueRepo(IEnumerable<Queue> queues)
        {
            var queryable = queues.ToList().BuildMockDbSet<Queue>();
            _queueRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Queue, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Expression<Func<Queue, object>>[]>()))
                .Returns(queryable.Object);
            _queueRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Queue, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(queryable.Object);
        }

        private void SetupEmptyQueueRepo()
            => SetupQueueRepo(Array.Empty<Queue>());

        private void SetupAppointmentRepo(IEnumerable<Appointment> appointments)
        {
            var queryable = appointments.ToList().BuildMockDbSet<Appointment>();
            _appointmentRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Appointment, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Expression<Func<Appointment, object>>[]>()))
                .Returns(queryable.Object);
            _appointmentRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Appointment, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(queryable.Object);
        }

        private void SetupEmptyAppointmentRepo()
            => SetupAppointmentRepo(Array.Empty<Appointment>());

        // ─────────────────────────────────────────────────────────────────
        // HttpContext helpers
        // ─────────────────────────────────────────────────────────────────

        private void SetupHttpContextClaim(string? rawClaimValue)
        {
            var httpContext = new DefaultHttpContext();
            if (rawClaimValue != null)
            {
                var claims = new[] { new Claim(ClaimTypes.NameIdentifier, rawClaimValue) };
                var identity = new ClaimsIdentity(claims, "TestAuth");
                httpContext.User = new ClaimsPrincipal(identity);
            }
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
        }

        private void SetupNullHttpContext()
        {
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext)null!);
        }

        private void SetupHttpContextUserId(Guid userId)
            => SetupHttpContextClaim(userId.ToString());

        // ─────────────────────────────────────────────────────────────────
        // Data factories
        // ─────────────────────────────────────────────────────────────────

        private static Queue MakeQueue(
            Guid id,
            Guid appointmentId,
            int queueNumber = 5,
            QueueStatus status = QueueStatus.CALLING) => new()
        {
            Id = id,
            AppointmentId = appointmentId,
            ClinicId = ClinicId,
            QueueNumber = queueNumber,
            Status = status,
            CreatedAt = new DateTime(2026, 7, 26, 10, 0, 0, DateTimeKind.Utc)
            // No Appointment navigation — assigning it would conflict with the
            // MockQueryable rows when EF traverses navigations during Update().
        };

        private static PatientProfile MakePatient(string fullName = "Nguyen Van Patient") => new()
        {
            Id = PatientId,
            UserId = Guid.NewGuid(),
            FullName = fullName,
            Gender = Gender.FEMALE,
            Dob = new DateTime(1990, 1, 1)
        };

        private static Appointment MakeAppointment(
            Guid id,
            PatientProfile patient,
            AppointmentStatus status = AppointmentStatus.IN_PROGRESS,
            PreliminaryDiagnosis? preliminaryDiagnosis = null) => new()
        {
            Id = id,
            PatientId = patient.Id,
            DoctorId = DoctorId,
            SlotId = Guid.NewGuid(),
            AppointmentDate = new DateTime(2026, 7, 26, 9, 0, 0, DateTimeKind.Utc),
            Status = status,
            BookingSource = "ONLINE",
            Patient = patient,
            PreliminaryDiagnosis = preliminaryDiagnosis
        };

        private static PreliminaryDiagnosis MakePreliminaryDiagnosis(Guid appointmentId, Guid patientId) => new()
        {
            Id = PreliminaryDiagnosisId,
            AppointmentId = appointmentId,
            PatientId = patientId,
            DoctorId = DoctorId,
            UrgencyLevel = TriageUrgencyLevel.Medium,
            CreatedAt = DateTime.UtcNow
        };

        /// <summary>
        /// Pre-attaches the queue and appointment to the in-memory context so that
        /// the service's <c>Update()</c> call sees them as existing rows. This
        /// avoids foreign-key violations during <c>SaveChangesAsync</c> when the
        /// in-memory provider tries to insert an entity whose principal doesn't
        /// exist yet.
        /// </summary>
        private async Task PreAttachAsync(Queue queue, Appointment appointment)
        {
            _context.Queues.Add(queue);
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            _context.Entry(queue).State = EntityState.Unchanged;
            _context.Entry(appointment).State = EntityState.Unchanged;
        }

        // ==================================================================
        // ====================== Process(...) tests ========================
        // ==================================================================

        /// <summary>
        /// TC-CQ-01: Happy path — valid Guid + valid user + queue + appointment +
        /// PreliminaryDiagnosis → success response with all fields populated.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_Returns2007SuccessWithAllFields()
        {
            //Arrange 1
            var request = new CompleteQueueRequest { QueueId = QueueId.ToString() };
            var queue = MakeQueue(QueueId, AppointmentId, queueNumber: 5, status: QueueStatus.CALLING);
            var patient = MakePatient("Nguyen Van Patient");
            var preliminary = MakePreliminaryDiagnosis(AppointmentId, PatientId);
            var appointment = MakeAppointment(AppointmentId, patient, status: AppointmentStatus.IN_PROGRESS, preliminaryDiagnosis: preliminary);

            //Arrange 2
            SetupHttpContextUserId(DoctorUserId);
            SetupQueueRepo(new[] { queue });
            SetupAppointmentRepo(new[] { appointment });
            await PreAttachAsync(queue, appointment);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2007.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.QueueId.Should().Be(QueueId.ToString());
            result.Data!.AppointmentId.Should().Be(AppointmentId.ToString());
            result.Data!.PatientName.Should().Be("Nguyen Van Patient");
            result.Data!.QueueNumber.Should().Be(5);
            result.Data!.PreviousStatus.Should().Be("CALLING");
            result.Data!.IsSuccess.Should().BeTrue();
            result.Data!.CompletedAt.Should().NotBeNullOrEmpty();

            // Verify queue was updated in the context
            var savedQueue = _context.Queues.Single();
            savedQueue.Status.Should().Be(QueueStatus.COMPLETED);
            savedQueue.CompletedAt.Should().NotBeNull();
        }

        /// <summary>
        /// TC-CQ-02: Empty QueueId → validator.NotEmpty fails → Fail(APP_MESSAGE_4019).
        /// Repos never called.
        /// </summary>
        [Fact]
        public async Task Process_EmptyQueueId_Returns4019ValidationFail()
        {
            //Arrange 1
            var request = new CompleteQueueRequest { QueueId = string.Empty };

            //Arrange 2
            SetupHttpContextUserId(DoctorUserId);
            SetupEmptyQueueRepo();
            SetupEmptyAppointmentRepo();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
            result.Data.Should().BeNull();

            _queueRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<Queue, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Expression<Func<Queue, object>>[]>()),
                Times.Never);
            _appointmentRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<Appointment, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Expression<Func<Appointment, object>>[]>()),
                Times.Never);
        }

        /// <summary>
        /// TC-CQ-03: Non-Guid QueueId → validator.BeAValidGuid fails → Fail(APP_MESSAGE_4019).
        /// </summary>
        [Fact]
        public async Task Process_NonGuidQueueId_Returns4019ValidationFail()
        {
            //Arrange 1
            var request = new CompleteQueueRequest { QueueId = "not-a-guid" };

            //Arrange 2
            SetupHttpContextUserId(DoctorUserId);
            SetupEmptyQueueRepo();
            SetupEmptyAppointmentRepo();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
            result.Data.Should().BeNull();
        }

        /// <summary>
        /// TC-CQ-04: HttpContext null → principalIdValue=null → parseResult=false →
        /// Fail(APP_MESSAGE_4033).
        /// </summary>
        [Fact]
        public async Task Process_NullHttpContext_Returns4033AuthFail()
        {
            //Arrange 1
            var request = new CompleteQueueRequest { QueueId = QueueId.ToString() };

            //Arrange 2
            SetupNullHttpContext();
            SetupEmptyQueueRepo();
            SetupEmptyAppointmentRepo();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            result.Data.Should().BeNull();
        }

        /// <summary>
        /// TC-CQ-05: Claim value is non-Guid → parseResult=false → Fail(APP_MESSAGE_4033).
        /// </summary>
        [Fact]
        public async Task Process_InvalidGuidClaim_Returns4033AuthFail()
        {
            //Arrange 1
            var request = new CompleteQueueRequest { QueueId = QueueId.ToString() };

            //Arrange 2
            SetupHttpContextClaim("not-a-guid");
            SetupEmptyQueueRepo();
            SetupEmptyAppointmentRepo();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            result.Data.Should().BeNull();
        }

        /// <summary>
        /// TC-CQ-06: Valid input but Queue repo empty → IsQueueValid=false →
        /// Fail(APP_MESSAGE_4052). Appointment repo never called.
        /// </summary>
        [Fact]
        public async Task Process_QueueNotFound_Returns4052QueueFail()
        {
            //Arrange 1
            var request = new CompleteQueueRequest { QueueId = QueueId.ToString() };

            //Arrange 2
            SetupHttpContextUserId(DoctorUserId);
            SetupEmptyQueueRepo();
            SetupEmptyAppointmentRepo();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4052.ToString());
            result.Data.Should().BeNull();

            _appointmentRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<Appointment, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Expression<Func<Appointment, object>>[]>()),
                Times.Never);
        }

        /// <summary>
        /// TC-CQ-07: Queue found but Appointment repo empty → IsAppointmentValid=false →
        /// Fail(APP_MESSAGE_4012).
        /// </summary>
        [Fact]
        public async Task Process_AppointmentNotFound_Returns4012AppointmentFail()
        {
            //Arrange 1
            var request = new CompleteQueueRequest { QueueId = QueueId.ToString() };
            var queue = MakeQueue(QueueId, AppointmentId);

            //Arrange 2
            SetupHttpContextUserId(DoctorUserId);
            SetupQueueRepo(new[] { queue });
            SetupEmptyAppointmentRepo();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4012.ToString());
            result.Data.Should().BeNull();
        }

        /// <summary>
        /// TC-CQ-08: Appointment with PreliminaryDiagnosis == null →
        /// IsMedicalRecordValid=false → Fail(APP_MESSAGE_4028).
        /// </summary>
        [Fact]
        public async Task Process_MissingPreliminaryDiagnosis_Returns4028MedicalRecordFail()
        {
            //Arrange 1
            var request = new CompleteQueueRequest { QueueId = QueueId.ToString() };
            var queue = MakeQueue(QueueId, AppointmentId);
            var patient = MakePatient("Patient A");
            var appointment = MakeAppointment(AppointmentId, patient, status: AppointmentStatus.IN_PROGRESS, preliminaryDiagnosis: null);

            //Arrange 2
            SetupHttpContextUserId(DoctorUserId);
            SetupQueueRepo(new[] { queue });
            SetupAppointmentRepo(new[] { appointment });

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4028.ToString());
            result.Data.Should().BeNull();
        }

        /// <summary>
        /// TC-CQ-09: SaveChangesAsync throws → catch → Fail(APP_MESSAGE_5001).
        /// Approach: build a separate <see cref="AppDbContext"/> whose
        /// <c>SaveChangesAsync</c> override throws unconditionally, point
        /// <see cref="CompleteQueueService"/> at it, and invoke the private
        /// <c>CompleteQueueAsync</c> directly via reflection.
        /// </summary>
        [Fact]
        public async Task CompleteQueueAsync_SaveChangesAsyncThrows_InternalError()
        {
            //Arrange 1
            // Build a fresh `AppDbContext` whose SaveChangesAsync always throws.
            var throwOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var throwingContext = new ThrowingDbContext(throwOptions);

            var queue = MakeQueue(QueueId, AppointmentId);
            queue.Status = QueueStatus.CALLING;
            var patient = MakePatient();
            var appointment = MakeAppointment(AppointmentId, patient, status: AppointmentStatus.IN_PROGRESS);

            // Build a service that uses the throwing context.
            var throwingService = new CompleteQueueService(
                _queueRepoMock.Object,
                _appointmentRepoMock.Object,
                _validator,
                throwingContext,
                _httpContextAccessorMock.Object);

            var state = CreateState();
            SetStateProperty(state, "Queue", queue);
            SetStateProperty(state, "Appointment", appointment);
            SetStateProperty(state, "ActiveUserId", DoctorUserId);
            SetStateProperty(state, "QueueId", QueueId);

            //Arrange 2

            //Act
            await InvokePrivateAsync(throwingService, "CompleteQueueAsync", state);

            //Assert
            ((bool)GetStateProperty(state, "HasError")!).Should().BeTrue();
            ((string)GetStateProperty(state, "ErrorCode")!).Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
            ((bool)GetStateProperty(state, "IsExecutionSuccess")!).Should().BeFalse();
        }

        /// <summary>
        /// Test-only <see cref="AppDbContext"/> whose <c>SaveChangesAsync</c>
        /// always throws an <see cref="InvalidOperationException"/>, used to
        /// drive the catch-block of <c>CompleteQueueAsync</c>.
        /// </summary>
        private sealed class ThrowingDbContext : AppDbContext
        {
            public ThrowingDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("simulated persistence failure");
        }

        /// <summary>
        /// TC-CQ-10: Appointment.Status already COMPLETED → CompleteQueueAsync only
        /// updates queue, skips appointment update.
        /// </summary>
        [Fact]
        public async Task Process_AppointmentAlreadyCompleted_SkipsAppointmentUpdate()
        {
            //Arrange 1
            var request = new CompleteQueueRequest { QueueId = QueueId.ToString() };
            var queue = MakeQueue(QueueId, AppointmentId, status: QueueStatus.CALLING);
            var patient = MakePatient();
            var preliminary = MakePreliminaryDiagnosis(AppointmentId, PatientId);
            var appointment = MakeAppointment(AppointmentId, patient, status: AppointmentStatus.COMPLETED, preliminaryDiagnosis: preliminary);

            //Arrange 2
            SetupHttpContextUserId(DoctorUserId);
            SetupQueueRepo(new[] { queue });
            SetupAppointmentRepo(new[] { appointment });
            await PreAttachAsync(queue, appointment);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2007.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.PreviousStatus.Should().Be("CALLING");

            var savedQueue = _context.Queues.Single();
            savedQueue.Status.Should().Be(QueueStatus.COMPLETED);
        }

        /// <summary>
        /// TC-CQ-11: Validation fails → GetQueueAsync's `if (state.HasError) return;`
        /// early-return guard fires → queue repo never invoked.
        /// </summary>
        [Fact]
        public async Task Process_QueueEarlyReturn_WhenHasErrorInPriorStep()
        {
            //Arrange 1
            var request = new CompleteQueueRequest { QueueId = "" };

            //Arrange 2
            SetupHttpContextUserId(DoctorUserId);
            SetupEmptyQueueRepo();
            SetupEmptyAppointmentRepo();

            //Act
            await _sut.Process(request);

            //Assert
            _queueRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<Queue, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Expression<Func<Queue, object>>[]>()),
                Times.Never);
            _queueRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<Queue, bool>>>(),
                    It.IsAny<bool>()),
                Times.Never);
        }

        // ==================================================================
        // ============ ValidateRequest(...) — private ======================
        // ==================================================================

        /// <summary>
        /// TC-CQ-12: Valid Guid → state.IsValidationPassed=true, ErrorCode=null.
        /// </summary>
        [Fact]
        public void ValidateRequest_ValidRequest_SetsIsValidationPassed()
        {
            //Arrange 1
            var request = new CompleteQueueRequest { QueueId = QueueId.ToString() };
            var state = CreateState();

            //Arrange 2

            //Act
            InvokePrivate(_sut, "ValidateRequest", request, state);

            //Assert
            ((bool)GetStateProperty(state, "IsValidationPassed")!).Should().BeTrue();
            ((bool)GetStateProperty(state, "HasError")!).Should().BeFalse();
            GetStateProperty(state, "ErrorCode").Should().BeNull();
        }

        /// <summary>
        /// TC-CQ-13: Empty QueueId → IsValidationPassed=false, ErrorCode=APP_MESSAGE_4019.
        /// </summary>
        [Fact]
        public void ValidateRequest_EmptyQueueId_SetsErrorCode4019()
        {
            //Arrange 1
            var request = new CompleteQueueRequest { QueueId = string.Empty };
            var state = CreateState();

            //Arrange 2

            //Act
            InvokePrivate(_sut, "ValidateRequest", request, state);

            //Assert
            ((bool)GetStateProperty(state, "IsValidationPassed")!).Should().BeFalse();
            ((bool)GetStateProperty(state, "HasError")!).Should().BeTrue();
            ((string)GetStateProperty(state, "ErrorCode")!).Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
        }

        /// <summary>
        /// TC-CQ-14: Non-Guid QueueId → IsValidationPassed=false, ErrorCode=APP_MESSAGE_4019.
        /// </summary>
        [Fact]
        public void ValidateRequest_NonGuidQueueId_SetsErrorCode4019()
        {
            //Arrange 1
            var request = new CompleteQueueRequest { QueueId = "not-a-guid" };
            var state = CreateState();

            //Arrange 2

            //Act
            InvokePrivate(_sut, "ValidateRequest", request, state);

            //Assert
            ((bool)GetStateProperty(state, "IsValidationPassed")!).Should().BeFalse();
            ((string)GetStateProperty(state, "ErrorCode")!).Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
        }

        // ==================================================================
        // ============ RetrieveAuthenticatedUserId(...) — private ==========
        // ==================================================================

        /// <summary>
        /// TC-CQ-15: Valid Guid claim → IsUserValid=true, ActiveUserId set.
        /// </summary>
        [Fact]
        public void RetrieveAuthenticatedUserId_ValidGuid_SetsIsUserValid()
        {
            //Arrange 1
            var state = CreateState();

            //Arrange 2
            SetupHttpContextUserId(DoctorUserId);

            //Act
            InvokePrivate(_sut, "RetrieveAuthenticatedUserId", state);

            //Assert
            ((bool)GetStateProperty(state, "IsUserValid")!).Should().BeTrue();
            ((Guid)GetStateProperty(state, "ActiveUserId")!).Should().Be(DoctorUserId);
            ((bool)GetStateProperty(state, "HasError")!).Should().BeFalse();
        }

        /// <summary>
        /// TC-CQ-16: HttpContext null → IsUserValid=false, ErrorCode=APP_MESSAGE_4033.
        /// </summary>
        [Fact]
        public void RetrieveAuthenticatedUserId_NullContext_SetsErrorCode4033()
        {
            //Arrange 1
            var state = CreateState();

            //Arrange 2
            SetupNullHttpContext();

            //Act
            InvokePrivate(_sut, "RetrieveAuthenticatedUserId", state);

            //Assert
            ((bool)GetStateProperty(state, "IsUserValid")!).Should().BeFalse();
            ((Guid)GetStateProperty(state, "ActiveUserId")!).Should().Be(Guid.Empty);
            ((string)GetStateProperty(state, "ErrorCode")!).Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        /// <summary>
        /// TC-CQ-17: Non-Guid claim value → IsUserValid=false, ErrorCode=APP_MESSAGE_4033.
        /// </summary>
        [Fact]
        public void RetrieveAuthenticatedUserId_InvalidGuid_SetsErrorCode4033()
        {
            //Arrange 1
            var state = CreateState();

            //Arrange 2
            SetupHttpContextClaim("not-a-guid");

            //Act
            InvokePrivate(_sut, "RetrieveAuthenticatedUserId", state);

            //Assert
            ((bool)GetStateProperty(state, "IsUserValid")!).Should().BeFalse();
            ((string)GetStateProperty(state, "ErrorCode")!).Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        // ==================================================================
        // ============ ParseQueueId(...) — private =========================
        // ==================================================================

        /// <summary>
        /// TC-CQ-18: Valid Guid → QueueId set, no error.
        /// </summary>
        [Fact]
        public void ParseQueueId_ValidGuid_SetsQueueId()
        {
            //Arrange 1
            var state = CreateState();

            //Arrange 2

            //Act
            InvokePrivate(_sut, "ParseQueueId", QueueId.ToString(), state);

            //Assert
            ((Guid)GetStateProperty(state, "QueueId")!).Should().Be(QueueId);
            ((bool)GetStateProperty(state, "HasError")!).Should().BeFalse();
        }

        /// <summary>
        /// TC-CQ-19: Non-Guid → QueueId=Guid.Empty, ErrorCode=APP_MESSAGE_4019.
        /// </summary>
        [Fact]
        public void ParseQueueId_InvalidGuid_SetsErrorCode4019()
        {
            //Arrange 1
            var state = CreateState();

            //Arrange 2

            //Act
            InvokePrivate(_sut, "ParseQueueId", "not-a-guid", state);

            //Assert
            ((Guid)GetStateProperty(state, "QueueId")!).Should().Be(Guid.Empty);
            ((bool)GetStateProperty(state, "HasError")!).Should().BeTrue();
            ((string)GetStateProperty(state, "ErrorCode")!).Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
        }

        /// <summary>
        /// TC-CQ-20: Prior error + valid Guid → ErrorCode preserved.
        /// </summary>
        [Fact]
        public void ParseQueueId_PreservesPriorErrorCode_WhenParseSucceeds()
        {
            //Arrange 1
            var state = CreateState();
            SetStateProperty(state, "HasError", true);
            SetStateProperty(state, "ErrorCode", GeneralCode.APP_MESSAGE_4033.ToString());

            //Arrange 2

            //Act
            InvokePrivate(_sut, "ParseQueueId", QueueId.ToString(), state);

            //Assert
            ((Guid)GetStateProperty(state, "QueueId")!).Should().Be(QueueId);
            ((bool)GetStateProperty(state, "HasError")!).Should().BeTrue();
            ((string)GetStateProperty(state, "ErrorCode")!).Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        // ==================================================================
        // ============ VerifyMedicalRecordExists(...) — private =============
        // ==================================================================

        /// <summary>
        /// TC-CQ-21: PreliminaryDiagnosis not null → IsMedicalRecordValid=true.
        /// </summary>
        [Fact]
        public void VerifyMedicalRecordExists_PreliminaryDiagnosisExists_SetsValid()
        {
            //Arrange 1
            var queue = MakeQueue(QueueId, AppointmentId);
            var patient = MakePatient();
            var preliminary = MakePreliminaryDiagnosis(AppointmentId, PatientId);
            var appointment = MakeAppointment(AppointmentId, patient, preliminaryDiagnosis: preliminary);
            var state = CreateState();
            SetStateProperty(state, "Queue", queue);
            SetStateProperty(state, "Appointment", appointment);

            //Arrange 2

            //Act
            InvokePrivate(_sut, "VerifyMedicalRecordExists", state);

            //Assert
            ((bool)GetStateProperty(state, "IsMedicalRecordValid")!).Should().BeTrue();
            ((bool)GetStateProperty(state, "HasError")!).Should().BeFalse();
        }

        /// <summary>
        /// TC-CQ-22: PreliminaryDiagnosis null → IsMedicalRecordValid=false,
        /// ErrorCode=APP_MESSAGE_4028.
        /// </summary>
        [Fact]
        public void VerifyMedicalRecordExists_PreliminaryDiagnosisNull_SetsErrorCode4028()
        {
            //Arrange 1
            var queue = MakeQueue(QueueId, AppointmentId);
            var patient = MakePatient();
            var appointment = MakeAppointment(AppointmentId, patient, preliminaryDiagnosis: null);
            var state = CreateState();
            SetStateProperty(state, "Queue", queue);
            SetStateProperty(state, "Appointment", appointment);

            //Arrange 2

            //Act
            InvokePrivate(_sut, "VerifyMedicalRecordExists", state);

            //Assert
            ((bool)GetStateProperty(state, "IsMedicalRecordValid")!).Should().BeFalse();
            ((string)GetStateProperty(state, "ErrorCode")!).Should().Be(GeneralCode.APP_MESSAGE_4028.ToString());
        }

        /// <summary>
        /// TC-CQ-23: HasError=true → early-return, no state change.
        /// </summary>
        [Fact]
        public void VerifyMedicalRecordExists_EarlyReturn_WhenHasError()
        {
            //Arrange 1
            var state = CreateState();
            SetStateProperty(state, "HasError", true);
            SetStateProperty(state, "ErrorCode", GeneralCode.APP_MESSAGE_4052.ToString());

            //Arrange 2

            //Act
            InvokePrivate(_sut, "VerifyMedicalRecordExists", state);

            //Assert
            ((bool)GetStateProperty(state, "HasError")!).Should().BeTrue();
            ((string)GetStateProperty(state, "ErrorCode")!).Should().Be(GeneralCode.APP_MESSAGE_4052.ToString());
        }

        /// <summary>
        /// TC-CQ-24: Appointment null → early-return, no state change.
        /// </summary>
        [Fact]
        public void VerifyMedicalRecordExists_EarlyReturn_WhenAppointmentNull()
        {
            //Arrange 1
            var queue = MakeQueue(QueueId, AppointmentId);
            var state = CreateState();
            SetStateProperty(state, "Queue", queue);
            SetStateProperty(state, "Appointment", null);

            //Arrange 2

            //Act
            InvokePrivate(_sut, "VerifyMedicalRecordExists", state);

            //Assert
            ((bool)GetStateProperty(state, "HasError")!).Should().BeFalse();
            GetStateProperty(state, "IsMedicalRecordValid").Should().Be(true);
        }

        // ==================================================================
        // ============ CreateResponse(...) — private =======================
        // ==================================================================

        /// <summary>
        /// TC-CQ-25: HasError=true → ApiResponse.Fail with ErrorCode.
        /// </summary>
        [Fact]
        public void CreateResponse_HasError_ReturnsFail()
        {
            //Arrange 1
            var state = CreateState();
            SetStateProperty(state, "HasError", true);
            SetStateProperty(state, "ErrorCode", GeneralCode.APP_MESSAGE_4019.ToString());

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "CreateResponse", state);
            var result = raw.Should().BeAssignableTo<ApiResponse<CompleteQueueResponse>>().Subject;

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
            result.Data.Should().BeNull();
        }

        /// <summary>
        /// TC-CQ-26: HasError=false → ApiResponse.Success(APP_MESSAGE_2007) with all
        /// fields populated from state.
        /// </summary>
        [Fact]
        public void CreateResponse_HappyPath_ReturnsSuccessWith2007()
        {
            //Arrange 1
            var queue = MakeQueue(QueueId, AppointmentId, queueNumber: 7);
            queue.Status = QueueStatus.CALLING;
            var patient = MakePatient("Tran Van X");
            var appointment = MakeAppointment(AppointmentId, patient);
            var state = CreateState();
            SetStateProperty(state, "Queue", queue);
            SetStateProperty(state, "Appointment", appointment);
            SetStateProperty(state, "PreviousStatus", "CALLING");

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "CreateResponse", state);
            var result = raw.Should().BeAssignableTo<ApiResponse<CompleteQueueResponse>>().Subject;

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2007.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.QueueId.Should().Be(QueueId.ToString());
            result.Data!.AppointmentId.Should().Be(AppointmentId.ToString());
            result.Data!.PatientName.Should().Be("Tran Van X");
            result.Data!.QueueNumber.Should().Be(7);
            result.Data!.PreviousStatus.Should().Be("CALLING");
            result.Data!.IsSuccess.Should().BeTrue();
            result.Data!.CompletedAt.Should().NotBeNullOrEmpty();
        }

        // ==================================================================
        // ===== Additional branch-coverage tests (added for 100% branch) ==
        // ==================================================================

        /// <summary>
        /// TC-CQ-27: <c>CreateResponse</c> with <c>HasError=true</c> and
        /// <c>ErrorCode=null</c> → falls back to APP_MESSAGE_4001.
        /// </summary>
        [Fact]
        public void CreateResponse_HasErrorWithNullErrorCode_ReturnsFailWithDefault4001()
        {
            //Arrange 1
            var state = CreateState();
            SetStateProperty(state, "HasError", true);
            SetStateProperty(state, "ErrorCode", null);

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "CreateResponse", state);
            var result = raw.Should().BeAssignableTo<ApiResponse<CompleteQueueResponse>>().Subject;

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
        }

        /// <summary>
        /// TC-CQ-28: <c>CreateResponse</c> happy-path with
        /// <c>state.PreviousStatus=null</c> → maps to empty string.
        /// </summary>
        [Fact]
        public void CreateResponse_HappyPath_NullPreviousStatus_MapsToEmpty()
        {
            //Arrange 1
            var queue = MakeQueue(QueueId, AppointmentId);
            var patient = MakePatient();
            var appointment = MakeAppointment(AppointmentId, patient);
            var state = CreateState();
            SetStateProperty(state, "Queue", queue);
            SetStateProperty(state, "Appointment", appointment);
            SetStateProperty(state, "PreviousStatus", null);

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "CreateResponse", state);
            var result = raw.Should().BeAssignableTo<ApiResponse<CompleteQueueResponse>>().Subject;

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2007.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.PreviousStatus.Should().Be(string.Empty);
        }

        /// <summary>
        /// TC-CQ-29: <c>CreateResponse</c> happy-path with
        /// <c>state.Queue=null</c> → every Queue-derived field defaults to empty / 0.
        /// Exercises the null-conditional branches of Queue?.Id / Queue?.AppointmentId /
        /// Queue?.QueueNumber / Appointment?.Patient.
        /// </summary>
        [Fact]
        public void CreateResponse_HappyPath_NullQueue_MapsAllQueueFieldsToDefaults()
        {
            //Arrange 1
            var patient = MakePatient();
            var appointment = MakeAppointment(AppointmentId, patient);
            var state = CreateState();
            SetStateProperty(state, "Queue", null);
            SetStateProperty(state, "Appointment", appointment);
            SetStateProperty(state, "PreviousStatus", null);

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "CreateResponse", state);
            var result = raw.Should().BeAssignableTo<ApiResponse<CompleteQueueResponse>>().Subject;

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2007.ToString());
            result.Data!.QueueId.Should().Be(string.Empty);
            result.Data!.AppointmentId.Should().Be(string.Empty);
            result.Data!.QueueNumber.Should().Be(0);
            result.Data!.PatientName.Should().Be("Nguyen Van Patient");
        }

        /// <summary>
        /// TC-CQ-30: <c>CreateResponse</c> happy-path with
        /// <c>state.Appointment=null</c> → PatientName = null.
        /// </summary>
        [Fact]
        public void CreateResponse_HappyPath_NullAppointment_LeavesPatientNameNull()
        {
            //Arrange 1
            var queue = MakeQueue(QueueId, AppointmentId);
            var state = CreateState();
            SetStateProperty(state, "Queue", queue);
            SetStateProperty(state, "Appointment", null);
            SetStateProperty(state, "PreviousStatus", "WAITING");

            //Arrange 2

            //Act
            var raw = InvokePrivate(_sut, "CreateResponse", state);
            var result = raw.Should().BeAssignableTo<ApiResponse<CompleteQueueResponse>>().Subject;

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2007.ToString());
            result.Data!.PatientName.Should().BeNull();
            result.Data!.PreviousStatus.Should().Be("WAITING");
        }

        /// <summary>
        /// TC-CQ-31: <c>RetrieveAuthenticatedUserId</c> with HttpContext present but
        /// no NameIdentifier claim → principalIdValue = null → parseResult = false →
        /// ErrorCode = APP_MESSAGE_4033. Exercises the null-conditional branch
        /// <c>FindFirst(...)?.Value</c> when FindFirst returns null.
        /// </summary>
        [Fact]
        public void RetrieveAuthenticatedUserId_NoClaimInContext_SetsErrorCode4033()
        {
            //Arrange 1
            var state = CreateState();

            //Arrange 2
            // HttpContext exists, but User has no NameIdentifier claim.
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            };
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

            //Act
            InvokePrivate(_sut, "RetrieveAuthenticatedUserId", state);

            //Assert
            ((bool)GetStateProperty(state, "IsUserValid")!).Should().BeFalse();
            ((Guid)GetStateProperty(state, "ActiveUserId")!).Should().Be(Guid.Empty);
            ((string)GetStateProperty(state, "ErrorCode")!).Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        /// <summary>
        /// TC-CQ-32: <c>VerifyMedicalRecordExists</c> early-return when
        /// <c>state.HasError=false</c> but <c>state.Queue==null</c>. The second
        /// disjunct of the guard fires.
        /// </summary>
        [Fact]
        public void VerifyMedicalRecordExists_EarlyReturn_WhenQueueNull()
        {
            //Arrange 1
            var patient = MakePatient();
            var appointment = MakeAppointment(AppointmentId, patient);
            var state = CreateState();
            SetStateProperty(state, "Queue", null);
            SetStateProperty(state, "Appointment", appointment);

            //Arrange 2

            //Act
            InvokePrivate(_sut, "VerifyMedicalRecordExists", state);

            //Assert
            ((bool)GetStateProperty(state, "IsMedicalRecordValid")!).Should().BeTrue();
            ((bool)GetStateProperty(state, "HasError")!).Should().BeFalse();
        }

        /// <summary>
        /// TC-CQ-33: <c>GetQueueAsync</c> direct call. <c>state.HasError=true</c> →
        /// early-return without invoking the queue repository.
        /// </summary>
        [Fact]
        public async Task GetQueueAsync_DirectReflection_HasErrorEarlyReturn_NoRepoCall()
        {
            //Arrange 1
            var state = CreateState();
            SetStateProperty(state, "HasError", true);

            //Arrange 2
            SetupEmptyQueueRepo();

            //Act
            await InvokePrivateAsync(_sut, "GetQueueAsync", state);

            //Assert
            _queueRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<Queue, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Expression<Func<Queue, object>>[]>()),
                Times.Never);

            // State untouched.
            ((bool)GetStateProperty(state, "IsQueueValid")!).Should().BeTrue();
        }

        /// <summary>
        /// TC-CQ-34: <c>GetQueueAsync</c> direct call. Empty repo → IsQueueValid=false,
        /// HasError=true, ErrorCode=APP_MESSAGE_4052.
        /// </summary>
        [Fact]
        public async Task GetQueueAsync_DirectReflection_EmptyRepo_SetsErrorCode4052()
        {
            //Arrange 1
            var state = CreateState();
            SetStateProperty(state, "QueueId", QueueId);

            //Arrange 2
            SetupEmptyQueueRepo();

            //Act
            await InvokePrivateAsync(_sut, "GetQueueAsync", state);

            //Assert
            ((Queue?)GetStateProperty(state, "Queue")).Should().BeNull();
            ((bool)GetStateProperty(state, "IsQueueValid")!).Should().BeFalse();
            ((bool)GetStateProperty(state, "HasError")!).Should().BeTrue();
            ((string)GetStateProperty(state, "ErrorCode")!).Should().Be(GeneralCode.APP_MESSAGE_4052.ToString());
        }

        /// <summary>
        /// TC-CQ-35: <c>GetAppointmentAsync</c> direct call. <c>state.HasError=true</c> →
        /// early-return without invoking the appointment repository.
        /// </summary>
        [Fact]
        public async Task GetAppointmentAsync_DirectReflection_HasErrorEarlyReturn_NoRepoCall()
        {
            //Arrange 1
            var state = CreateState();
            SetStateProperty(state, "HasError", true);
            SetStateProperty(state, "Queue", MakeQueue(QueueId, AppointmentId));

            //Arrange 2
            SetupEmptyAppointmentRepo();

            //Act
            await InvokePrivateAsync(_sut, "GetAppointmentAsync", state);

            //Assert
            _appointmentRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<Appointment, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Expression<Func<Appointment, object>>[]>()),
                Times.Never);
            ((bool)GetStateProperty(state, "IsAppointmentValid")!).Should().BeTrue();
        }

        /// <summary>
        /// TC-CQ-36: <c>GetAppointmentAsync</c> direct call. <c>state.Queue==null</c>
        /// (and HasError=false) → second disjunct of the guard fires, no repo call.
        /// </summary>
        [Fact]
        public async Task GetAppointmentAsync_DirectReflection_QueueNullEarlyReturn_NoRepoCall()
        {
            //Arrange 1
            var state = CreateState();
            SetStateProperty(state, "Queue", null);

            //Arrange 2
            SetupEmptyAppointmentRepo();

            //Act
            await InvokePrivateAsync(_sut, "GetAppointmentAsync", state);

            //Assert
            _appointmentRepoMock.Verify(
                r => r.FindByCondition(
                    It.IsAny<Expression<Func<Appointment, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Expression<Func<Appointment, object>>[]>()),
                Times.Never);
            ((bool)GetStateProperty(state, "IsAppointmentValid")!).Should().BeTrue();
        }

        /// <summary>
        /// TC-CQ-37: <c>GetAppointmentAsync</c> direct call. Valid queue + empty repo
        /// → IsAppointmentValid=false, ErrorCode=APP_MESSAGE_4012.
        /// </summary>
        [Fact]
        public async Task GetAppointmentAsync_DirectReflection_EmptyRepo_SetsErrorCode4012()
        {
            //Arrange 1
            var queue = MakeQueue(QueueId, AppointmentId);
            var state = CreateState();
            SetStateProperty(state, "Queue", queue);
            SetStateProperty(state, "QueueId", QueueId);

            //Arrange 2
            SetupEmptyAppointmentRepo();

            //Act
            await InvokePrivateAsync(_sut, "GetAppointmentAsync", state);

            //Assert
            ((Appointment?)GetStateProperty(state, "Appointment")).Should().BeNull();
            ((bool)GetStateProperty(state, "IsAppointmentValid")!).Should().BeFalse();
            ((string)GetStateProperty(state, "ErrorCode")!).Should().Be(GeneralCode.APP_MESSAGE_4012.ToString());
        }

        /// <summary>
        /// TC-CQ-38: <c>CompleteQueueAsync</c> early-return when <c>state.HasError=true</c>.
        /// </summary>
        [Fact]
        public async Task CompleteQueueAsync_DirectReflection_HasErrorEarlyReturn_NoSaveChanges()
        {
            //Arrange 1
            var queue = MakeQueue(QueueId, AppointmentId);
            var state = CreateState();
            SetStateProperty(state, "HasError", true);
            SetStateProperty(state, "Queue", queue);

            //Arrange 2

            //Act
            await InvokePrivateAsync(_sut, "CompleteQueueAsync", state);

            //Assert
            ((bool)GetStateProperty(state, "IsExecutionSuccess")!).Should().BeTrue();
            queue.Status.Should().Be(QueueStatus.CALLING); // unchanged
            _context.Queues.Should().BeEmpty();
        }

        /// <summary>
        /// TC-CQ-39: <c>CompleteQueueAsync</c> early-return when <c>state.Queue==null</c>.
        /// </summary>
        [Fact]
        public async Task CompleteQueueAsync_DirectReflection_QueueNullEarlyReturn_NoSaveChanges()
        {
            //Arrange 1
            var state = CreateState();
            SetStateProperty(state, "Queue", null);

            //Arrange 2

            //Act
            await InvokePrivateAsync(_sut, "CompleteQueueAsync", state);

            //Assert
            ((bool)GetStateProperty(state, "IsExecutionSuccess")!).Should().BeTrue();
            _context.Queues.Should().BeEmpty();
        }
    }
}