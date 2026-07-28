using System.Linq.Expressions;
using System.Security.Claims;
using ECS.Application.Services.MedicalRecordsServices.UpdateMedicalRecordServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
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
using MongoDB.Driver;
using Moq;

namespace ECS.Test.Services.MedicalRecordsServices.UpdateMedicalRecordServices
{
    /// <summary>
    /// Unit tests for <see cref="UpdateMedicalRecordService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line AND branch coverage on <c>UpdateMedicalRecordService.cs</c>.
    /// </summary>
    public class UpdateMedicalRecordServiceTests : IDisposable
    {
        private readonly Mock<IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext>> _medicalRecordQueryRepoMock = new();
        private readonly Mock<IRepositoryBaseAsync<MedicalRecord, Guid, AppDbContext>> _medicalRecordCommandRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepoMock = new();
        private readonly Mock<IMongoDbContext> _mongoMock = new();
        private readonly Mock<IMongoCollection<MedicalRecordDocument>> _mongoCollectionMock = new();
        private readonly Mock<IValidator<UpdateMedicalRecordRequest>> _validatorMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private AppDbContext _context;
        private UpdateMedicalRecordService _sut;

        public UpdateMedicalRecordServiceTests()
        {
            _context = NewInMemoryContext();
            _mongoMock.Setup(m => m.MedicalRecords).Returns(_mongoCollectionMock.Object);
            SetupMongoUpdateSuccess();
            SetupValidator(isValid: true);
            SetupHttpContextUser(UpdateMedicalRecordMockData.DoctorUserId);

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
        /// AppDbContext whose every call to SaveChangesAsync() throws.
        /// Used to trigger the catch block in UpdateMedicalRecordAsync (SQL failure + Mongo rollback).
        /// </summary>
        private class AlwaysThrowingOnSaveContext : AppDbContext
        {
            public AlwaysThrowingOnSaveContext(DbContextOptions<AppDbContext> options) : base(options) { }

            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("Simulated SQL failure");
        }

        private UpdateMedicalRecordService BuildSut() => new(
            _medicalRecordQueryRepoMock.Object,
            _medicalRecordCommandRepoMock.Object,
            _doctorRepoMock.Object,
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
                : new ValidationResult(new[] { new ValidationFailure("FormData", "invalid") });
            _validatorMock
                .Setup(v => v.Validate(It.IsAny<UpdateMedicalRecordRequest>()))
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

        private void SetupMedicalRecordQuery(IEnumerable<MedicalRecord> records)
        {
            var list = records.ToList();
            _medicalRecordQueryRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<MedicalRecord, bool>>>(), It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<MedicalRecord>().Object);
        }

        private void SetupDoctorQuery(IEnumerable<DoctorProfile> doctors)
        {
            var list = doctors.ToList();
            _doctorRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<DoctorProfile, bool>>>(), It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<DoctorProfile>().Object);
        }

        private void SetupMongoUpdateSuccess(long matchedCount = 1, long modifiedCount = 1)
        {
            var result = new UpdateResult.Acknowledged(matchedCount, modifiedCount, null);
            _mongoCollectionMock
                .Setup(x => x.UpdateOneAsync(
                    It.IsAny<FilterDefinition<MedicalRecordDocument>>(),
                    It.IsAny<UpdateDefinition<MedicalRecordDocument>>(),
                    It.IsAny<UpdateOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);
        }

        private void SetupMongoUpdateThrows()
        {
            _mongoCollectionMock
                .Setup(x => x.UpdateOneAsync(
                    It.IsAny<FilterDefinition<MedicalRecordDocument>>(),
                    It.IsAny<UpdateDefinition<MedicalRecordDocument>>(),
                    It.IsAny<UpdateOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("mongo unavailable"));
        }

        private void UseContext(AppDbContext context)
        {
            _context = context;
            _sut = BuildSut();
        }

        private void SetupHappyPathRepos(MedicalRecord? existingRecord = null, DoctorProfile? doctor = null)
        {
            var doctorProfile = doctor ?? UpdateMedicalRecordMockData.GetDoctorProfile();
            var record = existingRecord ?? UpdateMedicalRecordMockData.GetExistingMedicalRecord(doctor: doctorProfile);

            SetupMedicalRecordQuery(new[] { record });
            SetupDoctorQuery(new[] { doctorProfile });

            // Ensure SQL in-memory context has the record tracking if updated
            _context.MedicalRecords.Add(record);
            _context.SaveChanges();
        }

        // ==================================================================
        // ==================== VALIDATION TESTS ============================
        // ==================================================================

        /// <summary>TC-01: FluentValidation reports invalid -> APP_MESSAGE_4019.</summary>
        [Fact]
        public async Task Process_InvalidRequest_ReturnsFailWith4019()
        {
            //Arrange
            SetupValidator(isValid: false);
            var request = UpdateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(UpdateMedicalRecordMockData.RecordId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
            result.Data.Should().BeNull();
            _medicalRecordQueryRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<MedicalRecord, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        // ==================================================================
        // ================== AUTHENTICATION TESTS ==========================
        // ==================================================================

        /// <summary>TC-02: No NameIdentifier claim on HttpContext.User -> APP_MESSAGE_4033.</summary>
        [Fact]
        public async Task Process_NoAuthenticatedUserClaim_ReturnsFailWith4033()
        {
            //Arrange
            SetupHttpContextUser(userId: null);
            var request = UpdateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(UpdateMedicalRecordMockData.RecordId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        /// <summary>TC-03: HttpContext is null -> APP_MESSAGE_4033.</summary>
        [Fact]
        public async Task Process_NullHttpContext_ReturnsFailWith4033()
        {
            //Arrange
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
            var request = UpdateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(UpdateMedicalRecordMockData.RecordId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        // ==================================================================
        // ================== RECORD LOOKUP TESTS ===========================
        // ==================================================================

        /// <summary>TC-04: Record not found in database -> APP_MESSAGE_4004.</summary>
        [Fact]
        public async Task Process_RecordNotFound_ReturnsFailWith4004()
        {
            //Arrange
            SetupMedicalRecordQuery(Array.Empty<MedicalRecord>());
            var request = UpdateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(UpdateMedicalRecordMockData.RecordId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4004.ToString());
        }

        // ==================================================================
        // ================== DOCTOR AUTHORIZATION TESTS ====================
        // ==================================================================

        /// <summary>TC-05: Authenticated doctor profile not found -> APP_MESSAGE_4011.</summary>
        [Fact]
        public async Task Process_DoctorProfileNotFound_ReturnsFailWith4011()
        {
            //Arrange
            SetupMedicalRecordQuery(new[] { UpdateMedicalRecordMockData.GetExistingMedicalRecord() });
            SetupDoctorQuery(Array.Empty<DoctorProfile>());
            var request = UpdateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(UpdateMedicalRecordMockData.RecordId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4011.ToString());
        }

        /// <summary>TC-06: Doctor does not own the record -> APP_MESSAGE_4014.</summary>
        [Fact]
        public async Task Process_DoctorNotOwner_ReturnsFailWith4014()
        {
            //Arrange
            var ownerDoctor = UpdateMedicalRecordMockData.GetDoctorProfile(id: UpdateMedicalRecordMockData.DoctorProfileId);
            var otherDoctor = UpdateMedicalRecordMockData.GetDoctorProfile(
                id: UpdateMedicalRecordMockData.OtherDoctorProfileId,
                userId: UpdateMedicalRecordMockData.OtherUserId);

            SetupHttpContextUser(UpdateMedicalRecordMockData.OtherUserId);
            SetupMedicalRecordQuery(new[] { UpdateMedicalRecordMockData.GetExistingMedicalRecord(doctor: ownerDoctor) });
            SetupDoctorQuery(new[] { otherDoctor });

            var request = UpdateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(UpdateMedicalRecordMockData.RecordId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4014.ToString());
        }

        // ==================================================================
        // ==================== LOCK STATUS TESTS ===========================
        // ==================================================================

        /// <summary>TC-07: Medical record is locked -> APP_MESSAGE_4028.</summary>
        [Fact]
        public async Task Process_RecordIsLocked_ReturnsFailWith4028()
        {
            //Arrange
            var doctor = UpdateMedicalRecordMockData.GetDoctorProfile();
            var record = UpdateMedicalRecordMockData.GetExistingMedicalRecord(doctor: doctor, isLocked: true);

            SetupMedicalRecordQuery(new[] { record });
            SetupDoctorQuery(new[] { doctor });

            var request = UpdateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(record.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4028.ToString());
        }

        // ==================================================================
        // ================ PERSISTENCE / MONGO TESTS ======================
        // ==================================================================

        /// <summary>TC-08: FormData cannot be parsed into BsonDocument -> APP_MESSAGE_4019.</summary>
        [Fact]
        public async Task Process_FormDataInvalidBson_ReturnsFailWith4019()
        {
            //Arrange
            SetupHappyPathRepos();
            var request = UpdateMedicalRecordMockData.GetValidRequest(formDataJson: "[1,2,3]");

            //Act
            var result = await _sut.Process(UpdateMedicalRecordMockData.RecordId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
        }

        /// <summary>TC-09: Mongo MatchedCount == 0 (document not found in Mongo) -> APP_MESSAGE_4004.</summary>
        [Fact]
        public async Task Process_MongoMatchedCountZero_ReturnsFailWith4004()
        {
            //Arrange
            SetupHappyPathRepos();
            SetupMongoUpdateSuccess(matchedCount: 0);
            var request = UpdateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(UpdateMedicalRecordMockData.RecordId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4004.ToString());
        }

        /// <summary>TC-10: Mongo update throws Exception -> APP_MESSAGE_5001.</summary>
        [Fact]
        public async Task Process_MongoUpdateThrows_ReturnsFailWith5001()
        {
            //Arrange
            SetupHappyPathRepos();
            SetupMongoUpdateThrows();
            var request = UpdateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(UpdateMedicalRecordMockData.RecordId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
        }

        /// <summary>TC-11: SQL save throws -> rolls back Mongo version & returns APP_MESSAGE_5001.</summary>
        [Fact]
        public async Task Process_SqlSaveFails_RollsBackMongoAndReturns5001()
        {
            //Arrange
            SetupHappyPathRepos();
            UseContext(new AlwaysThrowingOnSaveContext(NewInMemoryOptions()));
            var request = UpdateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(UpdateMedicalRecordMockData.RecordId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());

            // Verify secondary rollback call was executed on Mongo
            _mongoCollectionMock.Verify(
                x => x.UpdateOneAsync(
                    It.IsAny<FilterDefinition<MedicalRecordDocument>>(),
                    It.IsAny<UpdateDefinition<MedicalRecordDocument>>(),
                    It.IsAny<UpdateOptions>(),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        /// <summary>TC-12: SQL save fails and Mongo rollback also throws -> swallows rollback exception and returns 5001.</summary>
        [Fact]
        public async Task Process_SqlSaveFailsAndMongoRollbackThrows_SwallowsExceptionAndReturns5001()
        {
            //Arrange
            SetupHappyPathRepos();

            // First call to UpdateOneAsync succeeds, second call (rollback) throws
            _mongoCollectionMock
                .SetupSequence(x => x.UpdateOneAsync(
                    It.IsAny<FilterDefinition<MedicalRecordDocument>>(),
                    It.IsAny<UpdateDefinition<MedicalRecordDocument>>(),
                    It.IsAny<UpdateOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null))
                .ThrowsAsync(new Exception("Mongo rollback failed"));

            UseContext(new AlwaysThrowingOnSaveContext(NewInMemoryOptions()));
            var request = UpdateMedicalRecordMockData.GetValidRequest();

            //Act
            var act = async () => await _sut.Process(UpdateMedicalRecordMockData.RecordId, request);
            var result = await act.Should().NotThrowAsync();

            //Assert
            result.Subject.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
        }

        // ==================================================================
        // ==================== HAPPY PATH TESTS ============================
        // ==================================================================

        /// <summary>TC-13: Happy path -> updates record and returns success APP_MESSAGE_2006.</summary>
        [Fact]
        public async Task Process_HappyPath_ReturnsSuccessWithUpdatedData()
        {
            //Arrange
            var doctor = UpdateMedicalRecordMockData.GetDoctorProfile();
            var record = UpdateMedicalRecordMockData.GetExistingMedicalRecord(doctor: doctor);
            SetupHappyPathRepos(existingRecord: record, doctor: doctor);

            var request = UpdateMedicalRecordMockData.GetValidRequest(notes: "Ghi chú mới");

            //Act
            var result = await _sut.Process(record.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2006.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.IsSuccess.Should().BeTrue();
            result.Data.MedicalRecordId.Should().Be(record.Id.ToString());
            result.Data.PatientName.Should().Be(record.Appointment!.Patient!.FullName);
            result.Data.DoctorName.Should().Be(doctor.User!.FullName);
            result.Data.RecordTypeLabel.Should().Be("Bệnh án mắt (Chấn thương)");

            var dbRecord = await _context.MedicalRecords.FindAsync(record.Id);
            dbRecord!.Notes.Should().Be("Ghi chú mới");
            dbRecord.RecordDataVersion.Should().Be(2);
            dbRecord.ChiefComplaint.Should().Be("Cập nhật: Đau mắt đỏ 5 ngày");
        }

        /// <summary>TC-14: Happy path with null Notes -> preserves existing Notes.</summary>
        [Fact]
        public async Task Process_HappyPath_NullNotes_PreservesExistingNotes()
        {
            //Arrange
            var doctor = UpdateMedicalRecordMockData.GetDoctorProfile();
            var record = UpdateMedicalRecordMockData.GetExistingMedicalRecord(doctor: doctor);
            record.Notes = "Ghi chú cũ ban đầu";
            SetupHappyPathRepos(existingRecord: record, doctor: doctor);

            var request = UpdateMedicalRecordMockData.GetValidRequest(notes: null);

            //Act
            var result = await _sut.Process(record.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2006.ToString());
            var dbRecord = await _context.MedicalRecords.FindAsync(record.Id);
            dbRecord!.Notes.Should().Be("Ghi chú cũ ban đầu");
        }

        // ==================================================================
        // ============ CHIEF COMPLAINT / SUMMARY EXTRACTION TESTS ============
        // ==================================================================

        /// <summary>TC-15: benhAn.summary present -> used verbatim as Summary.</summary>
        [Fact]
        public async Task Process_HappyPath_SummaryPresent_UsesSummaryVerbatim()
        {
            //Arrange
            SetupHappyPathRepos();
            const string json = """
            { "benhAn": { "lyDoVaoVien": "Đau mắt", "summary": "Tóm tắt riêng" } }
            """;
            var request = UpdateMedicalRecordMockData.GetValidRequest(formDataJson: json);

            //Act
            var result = await _sut.Process(UpdateMedicalRecordMockData.RecordId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2006.ToString());
            var record = await _context.MedicalRecords.FirstAsync();
            record.ChiefComplaint.Should().Be("Đau mắt");
            record.Summary.Should().Be("Tóm tắt riêng");
        }

        /// <summary>TC-16: No summary field -> Summary derived from ChiefComplaint.</summary>
        [Fact]
        public async Task Process_HappyPath_NoSummary_DerivesSummaryFromChiefComplaint()
        {
            //Arrange
            SetupHappyPathRepos();
            const string json = """
            { "benhAn": { "lyDoVaoVien": "Đau mắt đỏ 5 ngày" } }
            """;
            var request = UpdateMedicalRecordMockData.GetValidRequest(formDataJson: json);

            //Act
            var result = await _sut.Process(UpdateMedicalRecordMockData.RecordId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2006.ToString());
            var record = await _context.MedicalRecords.FirstAsync();
            record.ChiefComplaint.Should().Be("Đau mắt đỏ 5 ngày");
            record.Summary.Should().Be("Đau mắt đỏ 5 ngày");
        }

        /// <summary>TC-17: No benhAn section -> ChiefComplaint & Summary null.</summary>
        [Fact]
        public async Task Process_HappyPath_NoBenhAnSection_ChiefComplaintAndSummaryNull()
        {
            //Arrange
            SetupHappyPathRepos();
            const string json = """{ "khamBenh": { "thiLuc": "10/10" } }""";
            var request = UpdateMedicalRecordMockData.GetValidRequest(formDataJson: json);

            //Act
            var result = await _sut.Process(UpdateMedicalRecordMockData.RecordId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2006.ToString());
            var record = await _context.MedicalRecords.FirstAsync();
            record.ChiefComplaint.Should().BeNull();
            record.Summary.Should().BeNull();
        }

        /// <summary>TC-18: benhAn present without lyDoVaoVien -> ChiefComplaint & Summary null.</summary>
        [Fact]
        public async Task Process_HappyPath_BenhAnWithoutLyDoVaoVien_ChiefComplaintAndSummaryNull()
        {
            //Arrange
            SetupHappyPathRepos();
            const string json = """{ "benhAn": { "otherKey": "val" } }""";
            var request = UpdateMedicalRecordMockData.GetValidRequest(formDataJson: json);

            //Act
            var result = await _sut.Process(UpdateMedicalRecordMockData.RecordId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2006.ToString());
            var record = await _context.MedicalRecords.FirstAsync();
            record.ChiefComplaint.Should().BeNull();
            record.Summary.Should().BeNull();
        }

        /// <summary>TC-19: lyDoVaoVien not string -> ChiefComplaint null.</summary>
        [Fact]
        public async Task Process_HappyPath_LyDoVaoVienNotString_ChiefComplaintNull()
        {
            //Arrange
            SetupHappyPathRepos();
            const string json = """{ "benhAn": { "lyDoVaoVien": 999 } }""";
            var request = UpdateMedicalRecordMockData.GetValidRequest(formDataJson: json);

            //Act
            var result = await _sut.Process(UpdateMedicalRecordMockData.RecordId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2006.ToString());
            var record = await _context.MedicalRecords.FirstAsync();
            record.ChiefComplaint.Should().BeNull();
        }

        /// <summary>TC-20: summary present but not string -> falls back to ChiefComplaint.</summary>
        [Fact]
        public async Task Process_HappyPath_SummaryNotString_FallsBackToChiefComplaint()
        {
            //Arrange
            SetupHappyPathRepos();
            const string json = """{ "benhAn": { "lyDoVaoVien": "Cần khám", "summary": true } }""";
            var request = UpdateMedicalRecordMockData.GetValidRequest(formDataJson: json);

            //Act
            var result = await _sut.Process(UpdateMedicalRecordMockData.RecordId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2006.ToString());
            var record = await _context.MedicalRecords.FirstAsync();
            record.ChiefComplaint.Should().Be("Cần khám");
            record.Summary.Should().Be("Cần khám");
        }

        /// <summary>TC-21: lyDoVaoVien is empty string -> Summary null.</summary>
        [Fact]
        public async Task Process_HappyPath_LyDoVaoVienEmptyString_SummaryNull()
        {
            //Arrange
            SetupHappyPathRepos();
            const string json = """{ "benhAn": { "lyDoVaoVien": "" } }""";
            var request = UpdateMedicalRecordMockData.GetValidRequest(formDataJson: json);

            //Act
            var result = await _sut.Process(UpdateMedicalRecordMockData.RecordId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2006.ToString());
            var record = await _context.MedicalRecords.FirstAsync();
            record.ChiefComplaint.Should().Be(string.Empty);
            record.Summary.Should().BeNull();
        }

        // ==================================================================
        // ============ NULL NAVIGATION PROPERTY BRANCH TESTS =================
        // ==================================================================

        /// <summary>TC-22: Navigation properties null -> PatientName & DoctorName null.</summary>
        [Fact]
        public async Task Process_HappyPath_NullNavProperties_PatientAndDoctorNamesNull()
        {
            //Arrange
            var doctor = UpdateMedicalRecordMockData.GetDoctorProfile();
            var record = UpdateMedicalRecordMockData.GetExistingMedicalRecord(doctor: doctor);

            record.Appointment!.Patient = null;
            record.Doctor!.User = null;

            SetupHappyPathRepos(existingRecord: record, doctor: doctor);
            var request = UpdateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(record.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2006.ToString());
            result.Data!.PatientName.Should().BeNull();
            result.Data.DoctorName.Should().BeNull();
        }

        // ==================================================================
        // ================== RECORD TYPE LABEL MAPPING =======================
        // ==================================================================

        [Theory]
        [InlineData(RecordType.MS21_TRAUMA, "Bệnh án mắt (Chấn thương)")]
        [InlineData(RecordType.MS22_ANTERIOR, "Bệnh án mắt (Bán phần trước)")]
        [InlineData(RecordType.MS23_FUNDUS, "Bệnh án mắt (Đáy mắt)")]
        [InlineData(RecordType.MS24_GLAUCOMA, "Bệnh án mắt (Glôcôm)")]
        [InlineData(RecordType.MS25_STRABISMUS_PTOSIS, "Bệnh án mắt (Lác, sụp mi)")]
        [InlineData(RecordType.MS26_PEDIATRIC, "Bệnh án mắt (Mắt trẻ em)")]
        public async Task Process_HappyPath_EachRecordType_ReturnsCorrectLabel(RecordType type, string expectedLabel)
        {
            //Arrange
            var doctor = UpdateMedicalRecordMockData.GetDoctorProfile();
            var record = UpdateMedicalRecordMockData.GetExistingMedicalRecord(doctor: doctor, recordType: type);
            SetupHappyPathRepos(existingRecord: record, doctor: doctor);

            var request = UpdateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(record.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2006.ToString());
            result.Data!.RecordTypeLabel.Should().Be(expectedLabel);
        }

        /// <summary>TC-23: Unmapped enum value -> falls back to Enum.ToString().</summary>
        [Fact]
        public async Task Process_HappyPath_UnmappedRecordType_FallsBackToStringLabel()
        {
            //Arrange
            var doctor = UpdateMedicalRecordMockData.GetDoctorProfile();
            var record = UpdateMedicalRecordMockData.GetExistingMedicalRecord(doctor: doctor, recordType: (RecordType)9999);
            SetupHappyPathRepos(existingRecord: record, doctor: doctor);

            var request = UpdateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(record.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2006.ToString());
            result.Data!.RecordTypeLabel.Should().Be("9999");
        }
        // ==================================================================
        // ============ ADDITIONAL NULL-PROP BRANCH TESTS (FULL COVERAGE) ====
        // ==================================================================

        /// <summary>
        /// Cover branch: record != null, but record.Appointment is null
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_RecordAppointmentIsNull_PatientNameIsNull()
        {
            //Arrange
            var doctor = UpdateMedicalRecordMockData.GetDoctorProfile();
            var record = UpdateMedicalRecordMockData.GetExistingMedicalRecord(doctor: doctor);

            // Set Appointment to null explicitly
            record.Appointment = null;

            SetupHappyPathRepos(existingRecord: record, doctor: doctor);
            var request = UpdateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(record.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2006.ToString());
            result.Data!.PatientName.Should().BeNull();
        }

        /// <summary>
        /// Cover branch: record != null, record.Appointment != null, but record.Appointment.Patient is null
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_AppointmentPatientIsNull_PatientNameIsNull()
        {
            //Arrange
            var doctor = UpdateMedicalRecordMockData.GetDoctorProfile();
            var record = UpdateMedicalRecordMockData.GetExistingMedicalRecord(doctor: doctor);

            // Set Patient to null explicitly
            record.Appointment!.Patient = null;

            SetupHappyPathRepos(existingRecord: record, doctor: doctor);
            var request = UpdateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(record.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2006.ToString());
            result.Data!.PatientName.Should().BeNull();
        }

        /// <summary>
        /// Cover branch: record != null, record.Doctor != null, but record.Doctor.User is null
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_DoctorUserIsNull_DoctorNameIsNull()
        {
            //Arrange
            var doctor = UpdateMedicalRecordMockData.GetDoctorProfile();
            var record = UpdateMedicalRecordMockData.GetExistingMedicalRecord(doctor: doctor);

            // Set User to null on Doctor
            record.Doctor!.User = null;

            SetupHappyPathRepos(existingRecord: record, doctor: doctor);
            var request = UpdateMedicalRecordMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(record.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2006.ToString());
            result.Data!.DoctorName.Should().BeNull();
        }
    }
}