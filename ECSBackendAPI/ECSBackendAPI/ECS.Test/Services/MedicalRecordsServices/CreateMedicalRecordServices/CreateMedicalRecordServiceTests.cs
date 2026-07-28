using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;
using ECS.Application.Services.MedicalRecordsServices.CreateMedicalRecordServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Persistence.MongoDb;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;

namespace ECS.Test.Services.MedicalRecordsServices.CreateMedicalRecordServices
{
    /// <summary>
    /// Unit tests for <see cref="CreateMedicalRecordService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line AND branch coverage on <c>CreateMedicalRecordService.cs</c>.
    /// </summary>
    /// <remarks>
    /// Uses a real in-memory <see cref="AppDbContext"/> because the service writes
    /// directly via <c>_context.MedicalRecords.Add</c> / <c>_context.SaveChangesAsync</c>
    /// and <c>_context.Appointments</c> / <c>_context.Queues</c>. Mongo access is fully mocked.
    /// </remarks>
    public class CreateMedicalRecordServiceTests : IDisposable
    {
        private readonly Mock<IRepositoryQueryBase<Appointment, Guid, AppDbContext>> _appointmentRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext>> _medicalRecordQueryRepoMock = new();
        private readonly Mock<IRepositoryBaseAsync<MedicalRecord, Guid, AppDbContext>> _medicalRecordCommandRepoMock = new();
        private readonly Mock<IRepositoryBaseAsync<Queue, Guid, AppDbContext>> _queueRepoMock = new();
        private readonly Mock<IMongoDbContext> _mongoMock = new();
        private readonly Mock<IMongoCollection<MedicalRecordDocument>> _mongoCollectionMock = new();
        private readonly Mock<IValidator<CreateMedicalRecordRequest>> _validatorMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private AppDbContext _context;
        private CreateMedicalRecordService _sut;

        public CreateMedicalRecordServiceTests()
        {
            _context = NewInMemoryContext();
            _mongoMock.Setup(m => m.MedicalRecords).Returns(_mongoCollectionMock.Object);
            SetupMongoInsertSuccess();
            SetupMongoDeleteSuccess();
            SetupValidator(isValid: true);
            SetupHttpContextUser(CreateMedicalRecordMockData.DoctorUserId);

            _sut = BuildSut();
        }

        public void Dispose() => _context.Dispose();

        private static AppDbContext NewInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static DbContextOptions<AppDbContext> NewInMemoryOptions() =>
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        /// <summary>
        /// AppDbContext whose every call to SaveChangesAsync() throws, while Add()/other
        /// members behave normally. Used to deterministically hit the
        /// CreateMedicalRecordAsync catch-block (SQL save failure + Mongo rollback)
        /// without an unrelated ObjectDisposedException escaping earlier (e.g. from Add()).
        /// </summary>
        private class AlwaysThrowingOnSaveContext : AppDbContext
        {
            public AlwaysThrowingOnSaveContext(DbContextOptions<AppDbContext> options) : base(options) { }

            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("Simulated SQL failure");
        }

        /// <summary>
        /// AppDbContext where the FIRST SaveChangesAsync() call (inside
        /// CreateMedicalRecordAsync) succeeds normally, and the SECOND call (inside
        /// UpdateStatusesAsync) throws. Used to hit the swallowed catch in
        /// UpdateStatusesAsync while still exercising a successful record creation.
        /// </summary>
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

        private CreateMedicalRecordService BuildSut() => new(
            _appointmentRepoMock.Object,
            _doctorRepoMock.Object,
            _medicalRecordQueryRepoMock.Object,
            _medicalRecordCommandRepoMock.Object,
            _queueRepoMock.Object,
            _mongoMock.Object,
            _validatorMock.Object,
            _context,
            _httpContextAccessorMock.Object);

        // ─────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────

        private void SetupValidator(bool isValid)
        {
            var result = isValid
                ? new ValidationResult()
                : new ValidationResult(new[] { new ValidationFailure("AppointmentId", "invalid") });
            _validatorMock
                .Setup(v => v.Validate(It.IsAny<CreateMedicalRecordRequest>()))
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

        private void SetupExistingMedicalRecords(IEnumerable<MedicalRecord> records)
        {
            var list = records.ToList();
            _medicalRecordQueryRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<MedicalRecord, bool>>>(), It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<MedicalRecord>().Object);
        }

        private void SetupMongoInsertSuccess()
        {
            _mongoCollectionMock
                .Setup(x => x.InsertOneAsync(It.IsAny<MedicalRecordDocument>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        private void SetupMongoInsertThrows()
        {
            _mongoCollectionMock
                .Setup(x => x.InsertOneAsync(It.IsAny<MedicalRecordDocument>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("mongo unavailable"));
        }

        private void SetupMongoDeleteSuccess()
        {
            _mongoCollectionMock
                .Setup(x => x.DeleteOneAsync(It.IsAny<FilterDefinition<MedicalRecordDocument>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeleteResult.Acknowledged(1));
        }

        private void SetupMongoDeleteThrows()
        {
            _mongoCollectionMock
                .Setup(x => x.DeleteOneAsync(It.IsAny<FilterDefinition<MedicalRecordDocument>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("mongo rollback unavailable"));
        }

        /// <summary>Rebuilds the SUT against a specific AppDbContext instance (e.g. a controlled-throw context).</summary>
        private void UseContext(AppDbContext context)
        {
            _context = context;
            _sut = BuildSut();
        }

        /// <summary>Wires up repos for the happy path: appointment + doctor found, no duplicate record.</summary>
        private void SetupHappyPathRepos(Appointment? appointment = null, DoctorProfile? doctor = null)
        {
            var doctorProfile = doctor ?? CreateMedicalRecordMockData.GetDoctorProfile();
            SetupAppointment(new[] { appointment ?? CreateMedicalRecordMockData.GetAppointment(doctor: doctorProfile) });
            SetupDoctor(new[] { doctorProfile });
            SetupExistingMedicalRecords(Array.Empty<MedicalRecord>());
        }

        // ==================================================================
        // ==================== VALIDATION TESTS ============================
        // ==================================================================

        /// <summary>TC-01: FluentValidation reports invalid → APP_MESSAGE_4019, short-circuits.</summary>
        [Fact]
        public async Task Process_InvalidRequest_ReturnsFailWith4019()
        {
            //Arrange
            SetupValidator(isValid: false);
            var request = CreateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
            result.Data.Should().BeNull();
            _appointmentRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        // ==================================================================
        // ================== AUTHENTICATION TESTS ==========================
        // ==================================================================

        /// <summary>TC-02: No NameIdentifier claim on HttpContext.User → APP_MESSAGE_4033.</summary>
        [Fact]
        public async Task Process_NoAuthenticatedUserClaim_ReturnsFailWith4033()
        {
            //Arrange
            SetupHttpContextUser(userId: null);
            var request = CreateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        /// <summary>TC-03: HttpContext itself is null → APP_MESSAGE_4033.</summary>
        [Fact]
        public async Task Process_NullHttpContext_ReturnsFailWith4033()
        {
            //Arrange
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
            var request = CreateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        // ==================================================================
        // ==================== ID PARSING TESTS =============================
        // ==================================================================

        /// <summary>TC-04: AppointmentId is not a valid GUID → APP_MESSAGE_4019.</summary>
        [Fact]
        public async Task Process_InvalidAppointmentIdFormat_ReturnsFailWith4019()
        {
            //Arrange
            var request = CreateMedicalRecordMockData.GetValidRequest();
            request.AppointmentId = "not-a-guid";

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
        }

        /// <summary>TC-05: PatientId is not a valid GUID → APP_MESSAGE_4019.</summary>
        [Fact]
        public async Task Process_InvalidPatientIdFormat_ReturnsFailWith4019()
        {
            //Arrange
            var request = CreateMedicalRecordMockData.GetValidRequest();
            request.PatientId = "not-a-guid";

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
            _appointmentRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<bool>()),
                Times.Never);
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
            var request = CreateMedicalRecordMockData.GetValidRequest();

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
        public async Task Process_DoctorProfileNotFound_ReturnsFailWith4011()
        {
            //Arrange
            SetupAppointment(new[] { CreateMedicalRecordMockData.GetAppointment() });
            SetupDoctor(Array.Empty<DoctorProfile>());
            var request = CreateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4011.ToString());
            _medicalRecordQueryRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<MedicalRecord, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>TC-08: A medical record already exists for this appointment → APP_MESSAGE_4027.</summary>
        [Fact]
        public async Task Process_MedicalRecordAlreadyExists_ReturnsFailWith4027()
        {
            //Arrange
            var doctor = CreateMedicalRecordMockData.GetDoctorProfile();
            SetupAppointment(new[] { CreateMedicalRecordMockData.GetAppointment(doctor: doctor) });
            SetupDoctor(new[] { doctor });
            SetupExistingMedicalRecords(new[] { CreateMedicalRecordMockData.GetExistingMedicalRecord() });
            var request = CreateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4027.ToString());
            _mongoCollectionMock.Verify(
                x => x.InsertOneAsync(It.IsAny<MedicalRecordDocument>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // ==================================================================
        // ================ PERSISTENCE FAILURE TESTS =========================
        // ==================================================================

        /// <summary>
        /// TC-09: FormData is not a JSON object (e.g. a top-level array) so
        /// <c>BsonDocument.Parse</c> throws → APP_MESSAGE_4019. Validator is mocked
        /// to succeed regardless of FormData shape to isolate this branch.
        /// </summary>
        [Fact]
        public async Task Process_FormDataNotParsableAsBsonDocument_ReturnsFailWith4019()
        {
            //Arrange
            SetupHappyPathRepos();
            var request = CreateMedicalRecordMockData.GetValidRequest(formDataJson: "[1,2,3]");

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
            _mongoCollectionMock.Verify(
                x => x.InsertOneAsync(It.IsAny<MedicalRecordDocument>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>TC-10: Mongo insert throws → APP_MESSAGE_5001, no SQL row created.</summary>
        [Fact]
        public async Task Process_MongoInsertThrows_ReturnsFailWith5001()
        {
            //Arrange
            SetupHappyPathRepos();
            SetupMongoInsertThrows();
            var request = CreateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
            (await _context.MedicalRecords.CountAsync()).Should().Be(0);
        }

        /// <summary>
        /// TC-11: Mongo insert succeeds but SQL SaveChangesAsync fails → APP_MESSAGE_5001
        /// and the orphaned Mongo document is rolled back via DeleteOneAsync.
        /// Uses <see cref="AlwaysThrowingOnSaveContext"/> so that _context.MedicalRecords.Add(...)
        /// still succeeds normally and ONLY SaveChangesAsync() throws — this is what actually
        /// drives execution into the service's catch block (a disposed context instead throws
        /// on Add() too, which escapes uncaught and never hits the rollback code).
        /// </summary>
        [Fact]
        public async Task Process_SqlSaveFailsAfterMongoInsert_RollsBackMongoAndReturns5001()
        {
            //Arrange
            SetupHappyPathRepos();
            UseContext(new AlwaysThrowingOnSaveContext(NewInMemoryOptions()));
            var request = CreateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
            _mongoCollectionMock.Verify(
                x => x.InsertOneAsync(It.IsAny<MedicalRecordDocument>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _mongoCollectionMock.Verify(
                x => x.DeleteOneAsync(It.IsAny<FilterDefinition<MedicalRecordDocument>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// TC-11b: SQL save fails AND the Mongo rollback (DeleteOneAsync) also throws →
        /// the secondary cleanup failure is swallowed; Process still returns APP_MESSAGE_5001
        /// instead of letting the rollback exception propagate.
        /// </summary>
        [Fact]
        public async Task Process_SqlSaveFailsAndMongoRollbackAlsoFails_SwallowsSecondaryErrorReturns5001()
        {
            //Arrange
            SetupHappyPathRepos();
            SetupMongoDeleteThrows();
            UseContext(new AlwaysThrowingOnSaveContext(NewInMemoryOptions()));
            var request = CreateMedicalRecordMockData.GetValidRequest();

            //Act
            var act = async () => await _sut.Process(request);
            var result = await act.Should().NotThrowAsync();

            //Assert
            result.Subject.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
        }

        /// <summary>
        /// TC-11c: The record is created successfully, but the subsequent non-critical
        /// UpdateStatusesAsync (appointment/queue status update) throws. The failure is
        /// swallowed, so Process still returns a success response.
        /// </summary>
        [Fact]
        public async Task Process_UpdateStatusesAsyncThrows_StillReturnsSuccess()
        {
            //Arrange
            SetupHappyPathRepos();
            UseContext(new SecondSaveThrowsContext(NewInMemoryOptions()));
            var request = CreateMedicalRecordMockData.GetValidRequest();

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
            var doctor = CreateMedicalRecordMockData.GetDoctorProfile();
            var appointment = CreateMedicalRecordMockData.GetAppointment(doctor: doctor);
            SetupHappyPathRepos(appointment: appointment, doctor: doctor);
            var request = CreateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.IsSuccess.Should().BeTrue();
            result.Data.MedicalRecordId.Should().NotBeNullOrEmpty();
            result.Data.PatientName.Should().Be(appointment.Patient!.FullName);
            result.Data.DoctorName.Should().Be(doctor.User!.FullName);
            result.Data.RecordTypeLabel.Should().Be("Bệnh án mắt (Chấn thương)");
            result.Data.MongoDocumentId.Should().NotBeNullOrEmpty();

            _mongoCollectionMock.Verify(
                x => x.InsertOneAsync(It.IsAny<MedicalRecordDocument>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()),
                Times.Once);
            (await _context.MedicalRecords.CountAsync()).Should().Be(1);
        }

        /// <summary>TC-13: After success, appointment status becomes IN_PROGRESS.</summary>
        [Fact]
        public async Task Process_HappyPath_UpdatesAppointmentStatusToInProgress()
        {
            //Arrange
            var doctor = CreateMedicalRecordMockData.GetDoctorProfile();
            var appointment = CreateMedicalRecordMockData.GetAppointment(doctor: doctor);
            SetupHappyPathRepos(appointment: appointment, doctor: doctor);
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            var request = CreateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            var updated = await _context.Appointments.FindAsync(appointment.Id);
            updated!.Status.Should().Be(AppointmentStatus.IN_PROGRESS);
        }

        /// <summary>TC-14: Matching queue entry is marked COMPLETED after success.</summary>
        [Fact]
        public async Task Process_HappyPath_CompletesMatchingQueueEntry()
        {
            //Arrange
            var doctor = CreateMedicalRecordMockData.GetDoctorProfile();
            var appointment = CreateMedicalRecordMockData.GetAppointment(doctor: doctor);
            SetupHappyPathRepos(appointment: appointment, doctor: doctor);
            _context.Appointments.Add(appointment);
            var queue = new Queue
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointment.Id,
                Status = QueueStatus.WAITING
            };
            _context.Queues.Add(queue);
            await _context.SaveChangesAsync();
            var request = CreateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            var updatedQueue = await _context.Queues.FindAsync(queue.Id);
            updatedQueue!.Status.Should().Be(QueueStatus.COMPLETED);
            updatedQueue.CompletedAt.Should().NotBeNull();
        }

        // ==================================================================
        // ============ CHIEF COMPLAINT / SUMMARY EXTRACTION TESTS ============
        // ==================================================================

        /// <summary>TC-15: benhAn.summary present → used verbatim as Summary.</summary>
        [Fact]
        public async Task Process_HappyPath_SummaryPresent_UsesSummaryVerbatim()
        {
            //Arrange
            SetupHappyPathRepos();
            const string json = """
            { "benhAn": { "lyDoVaoVien": "Đau mắt", "summary": "Tóm tắt riêng" } }
            """;
            var request = CreateMedicalRecordMockData.GetValidRequest(formDataJson: json);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            var record = await _context.MedicalRecords.FirstAsync();
            record.ChiefComplaint.Should().Be("Đau mắt");
            record.Summary.Should().Be("Tóm tắt riêng");
        }

        /// <summary>TC-16: No summary field → Summary derived from ChiefComplaint (truncated to 200 chars).</summary>
        [Fact]
        public async Task Process_HappyPath_NoSummary_DerivesSummaryFromChiefComplaint()
        {
            //Arrange
            SetupHappyPathRepos();
            const string json = """
            { "benhAn": { "lyDoVaoVien": "Đau mắt đỏ 3 ngày, chảy nước mắt" } }
            """;
            var request = CreateMedicalRecordMockData.GetValidRequest(formDataJson: json);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            var record = await _context.MedicalRecords.FirstAsync();
            record.ChiefComplaint.Should().Be("Đau mắt đỏ 3 ngày, chảy nước mắt");
            record.Summary.Should().Be("Đau mắt đỏ 3 ngày, chảy nước mắt");
        }

        /// <summary>TC-17: No "benhAn" section at all → ChiefComplaint and Summary are both null.</summary>
        [Fact]
        public async Task Process_HappyPath_NoBenhAnSection_ChiefComplaintAndSummaryAreNull()
        {
            //Arrange
            SetupHappyPathRepos();
            const string json = """{ "khamBenh": { "thiLuc": "10/10" } }""";
            var request = CreateMedicalRecordMockData.GetValidRequest(formDataJson: json);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            var record = await _context.MedicalRecords.FirstAsync();
            record.ChiefComplaint.Should().BeNull();
            record.Summary.Should().BeNull();
        }

        // ==================================================================
        // ============ NULL NAVIGATION PROPERTY BRANCH TESTS =================
        // ==================================================================

        /// <summary>TC-18: appointment.Patient is null → PatientName is null, process still succeeds.</summary>
        [Fact]
        public async Task Process_HappyPath_AppointmentPatientIsNull_PatientNameIsNull()
        {
            //Arrange
            var doctor = CreateMedicalRecordMockData.GetDoctorProfile();
            var appointment = CreateMedicalRecordMockData.GetAppointment(doctor: doctor);
            appointment.Patient = null;
            SetupHappyPathRepos(appointment: appointment, doctor: doctor);
            var request = CreateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            result.Data!.PatientName.Should().BeNull();
        }

        /// <summary>TC-19: appointment.Doctor is null → DoctorName is null, process still succeeds.</summary>
        [Fact]
        public async Task Process_HappyPath_AppointmentDoctorIsNull_DoctorNameIsNull()
        {
            //Arrange
            var doctor = CreateMedicalRecordMockData.GetDoctorProfile();
            var appointment = CreateMedicalRecordMockData.GetAppointment(doctor: doctor);
            appointment.Doctor = null;
            SetupHappyPathRepos(appointment: appointment, doctor: doctor);
            var request = CreateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            result.Data!.DoctorName.Should().BeNull();
        }

        /// <summary>TC-20: appointment.Doctor.User is null → DoctorName is null, process still succeeds.</summary>
        [Fact]
        public async Task Process_HappyPath_AppointmentDoctorUserIsNull_DoctorNameIsNull()
        {
            //Arrange
            var doctor = CreateMedicalRecordMockData.GetDoctorProfile();
            var appointment = CreateMedicalRecordMockData.GetAppointment(doctor: doctor);
            appointment.Doctor!.User = null;
            SetupHappyPathRepos(appointment: appointment, doctor: doctor);
            var request = CreateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            result.Data!.DoctorName.Should().BeNull();
        }

        // ==================================================================
        // ======== ADDITIONAL CHIEF COMPLAINT / SUMMARY BRANCH TESTS =========
        // ==================================================================

        /// <summary>TC-21: "benhAn" present but no "lyDoVaoVien" key → ChiefComplaint and Summary both null.</summary>
        [Fact]
        public async Task Process_HappyPath_BenhAnPresentWithoutLyDoVaoVien_ChiefComplaintAndSummaryNull()
        {
            //Arrange
            SetupHappyPathRepos();
            const string json = """{ "benhAn": { "someOtherField": "x" } }""";
            var request = CreateMedicalRecordMockData.GetValidRequest(formDataJson: json);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            var record = await _context.MedicalRecords.FirstAsync();
            record.ChiefComplaint.Should().BeNull();
            record.Summary.Should().BeNull();
        }

        /// <summary>TC-22: "lyDoVaoVien" present but not a string (e.g. a number) → ChiefComplaint null.</summary>
        [Fact]
        public async Task Process_HappyPath_LyDoVaoVienNotAString_ChiefComplaintNull()
        {
            //Arrange
            SetupHappyPathRepos();
            const string json = """{ "benhAn": { "lyDoVaoVien": 123 } }""";
            var request = CreateMedicalRecordMockData.GetValidRequest(formDataJson: json);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            var record = await _context.MedicalRecords.FirstAsync();
            record.ChiefComplaint.Should().BeNull();
        }

        /// <summary>TC-23: "summary" present but not a string → falls back to deriving from ChiefComplaint.</summary>
        [Fact]
        public async Task Process_HappyPath_SummaryPresentButNotString_FallsBackToChiefComplaint()
        {
            //Arrange
            SetupHappyPathRepos();
            const string json = """{ "benhAn": { "lyDoVaoVien": "Đau mắt", "summary": 42 } }""";
            var request = CreateMedicalRecordMockData.GetValidRequest(formDataJson: json);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            var record = await _context.MedicalRecords.FirstAsync();
            record.ChiefComplaint.Should().Be("Đau mắt");
            record.Summary.Should().Be("Đau mắt");
        }

        /// <summary>TC-24: "lyDoVaoVien" is an empty string → Summary stays null (Length > 0 is false).</summary>
        [Fact]
        public async Task Process_HappyPath_LyDoVaoVienEmptyString_SummaryNull()
        {
            //Arrange
            SetupHappyPathRepos();
            const string json = """{ "benhAn": { "lyDoVaoVien": "" } }""";
            var request = CreateMedicalRecordMockData.GetValidRequest(formDataJson: json);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            var record = await _context.MedicalRecords.FirstAsync();
            record.ChiefComplaint.Should().Be(string.Empty);
            record.Summary.Should().BeNull();
        }

        // ==================================================================
        // ================== RECORD TYPE LABEL MAPPING =======================
        // ==================================================================

        [Theory]
        [InlineData("MS21_TRAUMA", "Bệnh án mắt (Chấn thương)")]
        [InlineData("MS22_ANTERIOR", "Bệnh án mắt (Bán phần trước)")]
        [InlineData("MS23_FUNDUS", "Bệnh án mắt (Đáy mắt)")]
        [InlineData("MS24_GLAUCOMA", "Bệnh án mắt (Glôcôm)")]
        [InlineData("MS25_STRABISMUS_PTOSIS", "Bệnh án mắt (Lác, sụp mi)")]
        [InlineData("MS26_PEDIATRIC", "Bệnh án mắt (Mắt trẻ em)")]
        public async Task Process_HappyPath_EachRecordType_ReturnsCorrectLabel(string recordType, string expectedLabel)
        {
            //Arrange
            SetupHappyPathRepos();
            var request = CreateMedicalRecordMockData.GetValidRequest(recordType: recordType);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            result.Data!.RecordTypeLabel.Should().Be(expectedLabel);
        }
        /// <summary>
        /// TC-25: RecordType is a numeric string that doesn't correspond to any declared member
        /// (e.g. "9999"). Enum.TryParse succeeds for arbitrary integers on non-[Flags] enums —
        /// it isn't restricted to declared names — so `recordType` becomes an unnamed value that
        /// matches none of the six labelled cases, and GetRecordTypeLabel falls through to
        /// "_ => recordType.ToString()", which renders as the raw number.
        /// This is a genuinely reachable path through the public API (the real validator's
        /// BeAValidRecordType would accept "9999" too, since it uses the same Enum.TryParse),
        /// so it is not dead code.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_NumericRecordTypeNotMatchingAnyMember_FallsBackToNumericToStringLabel()
        {
            //Arrange
            SetupHappyPathRepos();
            var request = CreateMedicalRecordMockData.GetValidRequest(recordType: "9999");

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2005.ToString());
            result.Data!.RecordTypeLabel.Should().Be("9999");
        }
    }
}