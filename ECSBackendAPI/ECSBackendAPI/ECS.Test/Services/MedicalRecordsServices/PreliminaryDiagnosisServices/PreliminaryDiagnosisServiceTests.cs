using System.Linq.Expressions;
using System.Security.Claims;
using ECS.Application.Services.MedicalRecordsServices.PreliminaryDiagnosisServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.MedicalRecordsServices.PreliminaryDiagnosisServices
{
    /// <summary>
    /// Unit tests for <see cref="PreliminaryDiagnosisService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line AND branch coverage on <c>PreliminaryDiagnosisService.cs</c>.
    /// </summary>
    /// <remarks>
    /// Appointment / Doctor / Patient / PreliminaryDiagnosis lookups go through fully mocked
    /// repositories (MockQueryable), matching the pattern used for
    /// <c>CreateMedicalRecordServiceTests</c>. The actual write path
    /// (<c>_context.PreliminaryDiagnoses.Add</c>, <c>_context.Appointments.Update</c>, and the
    /// real EF transaction) runs against a real in-memory <see cref="AppDbContext"/>, so tests
    /// that assert on the persisted <c>Appointment.Status</c> seed the SAME <see cref="Appointment"/>
    /// instance into both the context and the mocked repository — reusing one tracked object
    /// avoids EF's "already tracked with a different instance" conflict on <c>Update()</c>.
    /// InMemory transaction-not-supported warnings are ignored since the service always wraps
    /// its write in <c>Database.BeginTransactionAsync()</c>.
    /// </remarks>
    public class PreliminaryDiagnosisServiceTests : IDisposable
    {
        private readonly Mock<IRepositoryQueryBase<Appointment, Guid, AppDbContext>> _appointmentRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<PatientProfile, Guid, AppDbContext>> _patientRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<PreliminaryDiagnosis, Guid, AppDbContext>> _preliminaryDiagnosisRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext>> _medicalRecordRepoMock = new();
        private readonly Mock<IValidator<PreliminaryDiagnosisRequest>> _validatorMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly Mock<ILogger<PreliminaryDiagnosisService>> _loggerMock = new();
        private AppDbContext _context;
        private PreliminaryDiagnosisService _sut;

        public PreliminaryDiagnosisServiceTests()
        {
            _context = NewInMemoryContext();
            SetupValidator(isValid: true);
            SetupHttpContextUser(PreliminaryDiagnosisMockData.DoctorUserId);
            SetupPreliminaryDiagnoses(Array.Empty<PreliminaryDiagnosis>());

            _sut = BuildSut();
        }

        public void Dispose() => _context.Dispose();

        // ─────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────

        private static AppDbContext NewInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new AppDbContext(options);
        }

        private static DbContextOptions<AppDbContext> NewInMemoryOptions() =>
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

        /// <summary>AppDbContext where every SaveChangesAsync() call throws — used to force the
        /// CreatePreliminaryDiagnosisAsync catch-block (SQL save failure + rollback) deterministically.</summary>
        private class AlwaysThrowingOnSaveContext : AppDbContext
        {
            public AlwaysThrowingOnSaveContext(DbContextOptions<AppDbContext> options) : base(options) { }

            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("Simulated SQL failure");
        }

        /// <summary>AppDbContext where the FIRST SaveChangesAsync() (inside CreatePreliminaryDiagnosisAsync)
        /// succeeds, and the SECOND (inside UpdateAppointmentStatusAsync) throws — used to hit the
        /// swallowed catch in the non-critical status-update step while still exercising a successful create.</summary>
        private class SecondSaveThrowsContext : AppDbContext
        {
            private int _saveCount;
            public SecondSaveThrowsContext(DbContextOptions<AppDbContext> options) : base(options) { }

            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                _saveCount++;
                if (_saveCount == 2) throw new InvalidOperationException("Simulated failure on status update");
                return base.SaveChangesAsync(cancellationToken);
            }
        }

        private PreliminaryDiagnosisService BuildSut() => new(
            _appointmentRepoMock.Object,
            _patientRepoMock.Object,
            _doctorRepoMock.Object,
            _preliminaryDiagnosisRepoMock.Object,
            _medicalRecordRepoMock.Object,
            _validatorMock.Object,
            _context,
            _httpContextAccessorMock.Object,
            _loggerMock.Object);

        /// <summary>Rebuilds the SUT against a specific AppDbContext instance (e.g. a controlled-throw context).</summary>
        private void UseContext(AppDbContext context)
        {
            _context = context;
            _sut = BuildSut();
        }

        private void SetupValidator(bool isValid)
        {
            var result = isValid
                ? new ValidationResult()
                : new ValidationResult(new[] { new ValidationFailure("AppointmentId", "invalid") });
            _validatorMock
                .Setup(v => v.Validate(It.IsAny<PreliminaryDiagnosisRequest>()))
                .Returns(result);
        }

        private void SetupHttpContextUser(Guid? userId)
        {
            var identity = userId.HasValue
                ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }, "TestAuth")
                : new ClaimsIdentity();
            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        }

        private void SetupHttpContextUserWithRawClaim(string rawClaimValue)
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, rawClaimValue) }, "TestAuth");
            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        }

        private void SetupAppointment(IEnumerable<Appointment> appointments)
        {
            var list = appointments.ToList();
            _appointmentRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<Appointment>().Object);
        }

        private void SetupDoctor(IEnumerable<DoctorProfile> doctors)
        {
            var list = doctors.ToList();
            _doctorRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<DoctorProfile, bool>>>(), It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<DoctorProfile>().Object);
        }

        private void SetupPatient(IEnumerable<PatientProfile> patients)
        {
            var list = patients.ToList();
            _patientRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<PatientProfile>().Object);
        }

        private void SetupPreliminaryDiagnoses(IEnumerable<PreliminaryDiagnosis> records)
        {
            var list = records.ToList();
            _preliminaryDiagnosisRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<PreliminaryDiagnosis, bool>>>(), It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<PreliminaryDiagnosis>().Object);
        }

        /// <summary>Wires up repos for the happy path: appointment (seeded into the SAME context instance so
        /// UpdateAppointmentStatusAsync can attach it without a tracking conflict) + active doctor + patient,
        /// no existing preliminary diagnosis.</summary>
        private (Appointment Appointment, DoctorProfile Doctor, PatientProfile Patient) SetupHappyPathRepos()
        {
            var doctor = PreliminaryDiagnosisMockData.GetDoctorProfile();
            var patient = PreliminaryDiagnosisMockData.GetPatientProfile();
            var appointment = PreliminaryDiagnosisMockData.GetAppointment(patientId: patient.Id);

            _context.Appointments.Add(appointment);
            _context.SaveChanges();

            SetupAppointment(new[] { appointment });
            SetupDoctor(new[] { doctor });
            SetupPatient(new[] { patient });
            SetupPreliminaryDiagnoses(Array.Empty<PreliminaryDiagnosis>());

            return (appointment, doctor, patient);
        }

        // ==================================================================
        // ================== VALIDATION / AUTH TESTS ==========================
        // ==================================================================

        /// <summary>TC-01: Validation fails AND auth also fails → final code is 4033, because
        /// <c>RetrieveAuthenticatedUserId</c> unconditionally overwrites <c>HasError</c>/<c>ErrorCode</c>
        /// regardless of the earlier validation outcome.</summary>
        [Fact]
        public async Task Process_ValidationFailsAndAuthFails_ReturnsFailWith4033()
        {
            //Arrange
            SetupValidator(isValid: false);
            SetupHttpContextUser(userId: null);
            var request = PreliminaryDiagnosisMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        /// <summary>TC-02: Validation passes but no NameIdentifier claim on HttpContext.User → APP_MESSAGE_4033.</summary>
        [Fact]
        public async Task Process_NoAuthenticatedUserClaim_ReturnsFailWith4033()
        {
            //Arrange
            SetupHttpContextUser(userId: null);
            var request = PreliminaryDiagnosisMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        /// <summary>TC-03: NameIdentifier claim present but not a parsable GUID → APP_MESSAGE_4033.</summary>
        [Fact]
        public async Task Process_NonGuidUserClaim_ReturnsFailWith4033()
        {
            //Arrange
            SetupHttpContextUserWithRawClaim("not-a-guid");
            var request = PreliminaryDiagnosisMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        /// <summary>TC-04: Auth succeeds but AppointmentId string is not a parsable GUID → APP_MESSAGE_4019.</summary>
        [Fact]
        public async Task Process_AppointmentIdNotValidGuid_ReturnsFailWith4019()
        {
            //Arrange
            var request = PreliminaryDiagnosisMockData.GetValidRequest();
            request.AppointmentId = "not-a-guid";

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
        }

        /// <summary>
        /// TC-05: Validation fails, but auth AND AppointmentId parsing both succeed. Because
        /// <c>RetrieveAuthenticatedUserId</c> unconditionally resets <c>HasError</c>/<c>ErrorCode</c>
        /// based only on its own outcome, the earlier validation failure is silently cleared and
        /// processing continues as if the request were valid. This test documents that real,
        /// reachable behavior rather than asserting what the validator "should" have blocked.
        /// </summary>
        [Fact]
        public async Task Process_ValidationFailsButAuthAndAppointmentIdSucceed_ValidationErrorIsSilentlyOverwritten()
        {
            //Arrange
            SetupValidator(isValid: false);
            SetupHappyPathRepos();
            var request = PreliminaryDiagnosisMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            result.Data!.IsSuccess.Should().BeTrue();
        }

        // ==================================================================
        // ================== ENTITY LOOKUP TESTS ============================
        // ==================================================================

        /// <summary>TC-06: Appointment not found → APP_MESSAGE_4012.</summary>
        [Fact]
        public async Task Process_AppointmentNotFound_ReturnsFailWith4012()
        {
            //Arrange
            SetupAppointment(Array.Empty<Appointment>());
            var request = PreliminaryDiagnosisMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4012.ToString());
            _doctorRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<DoctorProfile, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>TC-07: Doctor profile for the authenticated user not found (or inactive) → APP_MESSAGE_4011.</summary>
        [Fact]
        public async Task Process_DoctorProfileNotFoundOrInactive_ReturnsFailWith4011()
        {
            //Arrange
            SetupAppointment(new[] { PreliminaryDiagnosisMockData.GetAppointment() });
            SetupDoctor(Array.Empty<DoctorProfile>());
            var request = PreliminaryDiagnosisMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4011.ToString());
            _patientRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>TC-08: Patient profile referenced by the appointment is not found → APP_MESSAGE_4010.</summary>
        [Fact]
        public async Task Process_PatientProfileNotFound_ReturnsFailWith4010()
        {
            //Arrange
            SetupAppointment(new[] { PreliminaryDiagnosisMockData.GetAppointment() });
            SetupDoctor(new[] { PreliminaryDiagnosisMockData.GetDoctorProfile() });
            SetupPatient(Array.Empty<PatientProfile>());
            var request = PreliminaryDiagnosisMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4010.ToString());
            _preliminaryDiagnosisRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<PreliminaryDiagnosis, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>TC-09: A preliminary diagnosis already exists for this appointment → APP_MESSAGE_4027.</summary>
        [Fact]
        public async Task Process_PreliminaryDiagnosisAlreadyExists_ReturnsFailWith4027()
        {
            //Arrange
            SetupAppointment(new[] { PreliminaryDiagnosisMockData.GetAppointment() });
            SetupDoctor(new[] { PreliminaryDiagnosisMockData.GetDoctorProfile() });
            SetupPatient(new[] { PreliminaryDiagnosisMockData.GetPatientProfile() });
            SetupPreliminaryDiagnoses(new[] { PreliminaryDiagnosisMockData.GetExistingPreliminaryDiagnosis() });
            var request = PreliminaryDiagnosisMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4027.ToString());
        }

        // ==================================================================
        // ================ PERSISTENCE FAILURE TESTS =========================
        // ==================================================================

        /// <summary>TC-10: SaveChangesAsync throws while creating the record → APP_MESSAGE_5001, transaction rolled back.</summary>
        [Fact]
        public async Task Process_SqlSaveFails_ReturnsFailWith5001()
        {
            //Arrange
            var doctor = PreliminaryDiagnosisMockData.GetDoctorProfile();
            var patient = PreliminaryDiagnosisMockData.GetPatientProfile();
            var appointment = PreliminaryDiagnosisMockData.GetAppointment(patientId: patient.Id);
            SetupAppointment(new[] { appointment });
            SetupDoctor(new[] { doctor });
            SetupPatient(new[] { patient });
            UseContext(new AlwaysThrowingOnSaveContext(NewInMemoryOptions()));
            var request = PreliminaryDiagnosisMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
        }

        /// <summary>
        /// TC-11: The preliminary diagnosis is created successfully, but the subsequent non-critical
        /// UpdateAppointmentStatusAsync throws. The failure is swallowed, so Process still returns success.
        /// </summary>
        [Fact]
        public async Task Process_UpdateAppointmentStatusThrows_StillReturnsSuccess()
        {
            //Arrange
            var doctor = PreliminaryDiagnosisMockData.GetDoctorProfile();
            var patient = PreliminaryDiagnosisMockData.GetPatientProfile();
            var appointment = PreliminaryDiagnosisMockData.GetAppointment(patientId: patient.Id);
            SetupAppointment(new[] { appointment });
            SetupDoctor(new[] { doctor });
            SetupPatient(new[] { patient });
            UseContext(new SecondSaveThrowsContext(NewInMemoryOptions()));
            var request = PreliminaryDiagnosisMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            result.Data!.IsSuccess.Should().BeTrue();
        }

        // ==================================================================
        // ==================== HAPPY PATH TESTS =============================
        // ==================================================================

        /// <summary>TC-12: Full happy path → success response with all fields populated.</summary>
        [Fact]
        public async Task Process_HappyPath_ReturnsSuccessWithPopulatedResponse()
        {
            //Arrange
            var (appointment, doctor, patient) = SetupHappyPathRepos();
            var request = PreliminaryDiagnosisMockData.GetValidRequest(recommendedAction: "Chuyển khoa Glôcôm");

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            var data = result.Data!;
            data.IsSuccess.Should().BeTrue();
            data.PreliminaryDiagnosisId.Should().NotBeNullOrEmpty();
            data.PatientName.Should().Be(patient.FullName);
            data.DoctorName.Should().Be(doctor.User!.FullName);
            data.AppointmentDate.Should().Be(appointment.AppointmentDate.ToString("dd/MM/yyyy"));
            data.UrgencyLevel.Should().Be(TriageUrgencyLevel.Medium.ToString());
            data.RecommendedAction.Should().Be("Chuyển khoa Glôcôm");
        }

        /// <summary>TC-12b: Doctor found and active, but its linked User navigation is null → DoctorName is
        /// null (the <c>doctorProfile?.User?.FullName</c> chain short-circuits on the second link, not just
        /// the first). This is a distinct branch from "doctor not found" (TC-07).</summary>
        [Fact]
        public async Task Process_DoctorFoundButUserIsNull_DoctorNameIsNull()
        {
            //Arrange
            var doctor = PreliminaryDiagnosisMockData.GetDoctorProfile();
            doctor.User = null;
            var patient = PreliminaryDiagnosisMockData.GetPatientProfile();
            var appointment = PreliminaryDiagnosisMockData.GetAppointment(patientId: patient.Id);
            _context.Appointments.Add(appointment);
            _context.SaveChanges();
            SetupAppointment(new[] { appointment });
            SetupDoctor(new[] { doctor });
            SetupPatient(new[] { patient });
            var request = PreliminaryDiagnosisMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            result.Data!.DoctorName.Should().BeNull();
        }

        /// <summary>TC-13: CheckInTime not provided in the request → the created record defaults it to "now".</summary>
        [Fact]
        public async Task Process_CheckInTimeNotProvided_DefaultsToUtcNow()
        {
            //Arrange
            SetupHappyPathRepos();
            var before = DateTime.UtcNow;
            var request = PreliminaryDiagnosisMockData.GetValidRequest(checkInTime: null);

            //Act
            var result = await _sut.Process(request);
            var after = DateTime.UtcNow;

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            var saved = _context.PreliminaryDiagnoses.Single();
            saved.CheckInTime.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        }

        /// <summary>TC-14: An explicit CheckInTime in the request is preserved as-is (no default applied).</summary>
        [Fact]
        public async Task Process_CheckInTimeProvided_IsPreserved()
        {
            //Arrange
            SetupHappyPathRepos();
            var explicitCheckIn = new DateTime(2026, 7, 20, 8, 30, 0, DateTimeKind.Utc);
            var request = PreliminaryDiagnosisMockData.GetValidRequest(checkInTime: explicitCheckIn);

            //Act
            await _sut.Process(request);

            //Assert
            var saved = _context.PreliminaryDiagnoses.Single();
            saved.CheckInTime.Should().Be(explicitCheckIn);
        }

        // ==================================================================
        // ============ URGENCY AUTO-UPGRADE BUSINESS RULE TESTS ===============
        // ==================================================================

        /// <summary>TC-15: PainLevel >= 8 with a non-Emergency urgency auto-upgrades to High.</summary>
        [Fact]
        public async Task Process_PainLevelAtLeast8_UpgradesUrgencyToHigh()
        {
            //Arrange
            SetupHappyPathRepos();
            var request = PreliminaryDiagnosisMockData.GetValidRequest(urgencyLevel: TriageUrgencyLevel.Medium, painLevel: 9);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data!.UrgencyLevel.Should().Be(TriageUrgencyLevel.High.ToString());
        }

        /// <summary>TC-16: PainLevel >= 8 while urgency is already Emergency stays Emergency (no downgrade to High).</summary>
        [Fact]
        public async Task Process_PainLevelAtLeast8_AlreadyEmergency_StaysEmergency()
        {
            //Arrange
            SetupHappyPathRepos();
            var request = PreliminaryDiagnosisMockData.GetValidRequest(urgencyLevel: TriageUrgencyLevel.Emergency, painLevel: 10);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data!.UrgencyLevel.Should().Be(TriageUrgencyLevel.Emergency.ToString());
        }

        /// <summary>TC-17: PainLevel below 8 does not trigger the auto-upgrade.</summary>
        [Fact]
        public async Task Process_PainLevelBelow8_DoesNotUpgradeUrgency()
        {
            //Arrange
            SetupHappyPathRepos();
            var request = PreliminaryDiagnosisMockData.GetValidRequest(urgencyLevel: TriageUrgencyLevel.Low, painLevel: 5);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data!.UrgencyLevel.Should().Be(TriageUrgencyLevel.Low.ToString());
        }

        /// <summary>TC-18: A null PainLevel never triggers the auto-upgrade branch.</summary>
        [Fact]
        public async Task Process_PainLevelNull_DoesNotUpgradeUrgency()
        {
            //Arrange
            SetupHappyPathRepos();
            var request = PreliminaryDiagnosisMockData.GetValidRequest(urgencyLevel: TriageUrgencyLevel.Medium, painLevel: null);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data!.UrgencyLevel.Should().Be(TriageUrgencyLevel.Medium.ToString());
        }

        // ==================================================================
        // ============ APPOINTMENT STATUS TRANSITION TESTS ====================
        // ==================================================================

        /// <summary>TC-19: Emergency or High urgency moves the appointment straight to IN_PROGRESS.</summary>
        [Theory]
        [InlineData(TriageUrgencyLevel.Emergency)]
        [InlineData(TriageUrgencyLevel.High)]
        public async Task Process_EmergencyOrHighUrgency_SetsAppointmentStatusInProgress(TriageUrgencyLevel urgency)
        {
            //Arrange
            var (appointment, _, _) = SetupHappyPathRepos();
            var request = PreliminaryDiagnosisMockData.GetValidRequest(urgencyLevel: urgency);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            var updated = await _context.Appointments.FindAsync(appointment.Id);
            updated!.Status.Should().Be(AppointmentStatus.IN_PROGRESS);
        }

        /// <summary>TC-20: Low or Medium urgency queues the appointment via ARRIVED status.</summary>
        [Theory]
        [InlineData(TriageUrgencyLevel.Low)]
        [InlineData(TriageUrgencyLevel.Medium)]
        public async Task Process_LowOrMediumUrgency_SetsAppointmentStatusArrived(TriageUrgencyLevel urgency)
        {
            //Arrange
            var (appointment, _, _) = SetupHappyPathRepos();
            var request = PreliminaryDiagnosisMockData.GetValidRequest(urgencyLevel: urgency);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            var updated = await _context.Appointments.FindAsync(appointment.Id);
            updated!.Status.Should().Be(AppointmentStatus.ARRIVED);
        }
    }
}