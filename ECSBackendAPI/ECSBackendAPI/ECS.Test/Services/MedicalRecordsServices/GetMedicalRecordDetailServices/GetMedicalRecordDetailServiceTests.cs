using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading;
using ECS.Application.Services.MedicalRecordsServices.GetMedicalRecordDetailServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Persistence.MongoDb;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using MockQueryable.Moq;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;

namespace ECS.Test.Services.MedicalRecordsServices.GetMedicalRecordDetailServices
{
    /// <summary>
    /// Unit tests for <see cref="GetMedicalRecordDetailService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line AND branch coverage on <c>GetMedicalRecordDetailService.cs</c>.
    /// </summary>
    /// <remarks>
    /// All SQL repositories (MedicalRecord / DoctorProfile / PatientProfile) are mocked
    /// via MockQueryable so <c>FindByCondition(...).AsNoTracking().Include(...)</c> chains
    /// resolve against an in-memory list. Mongo access is fully mocked through
    /// <see cref="IMongoCollection{TDocument}.Find"/> + a mocked <see cref="IAsyncCursor{TDocument}"/>,
    /// matching the real driver's cursor-based iteration used by <c>FirstOrDefaultAsync</c>.
    /// </remarks>
    public class GetMedicalRecordDetailServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext>> _medicalRecordRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<PatientProfile, Guid, AppDbContext>> _patientRepoMock = new();
        private readonly Mock<IMongoDbContext> _mongoMock = new();
        private readonly Mock<IMongoCollection<MedicalRecordDocument>> _mongoCollectionMock = new();
        private readonly Mock<IValidator<GetMedicalRecordDetailRequest>> _validatorMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly GetMedicalRecordDetailService _sut;

        public GetMedicalRecordDetailServiceTests()
        {
            _mongoMock.Setup(m => m.MedicalRecords).Returns(_mongoCollectionMock.Object);
            SetupValidator(isValid: true);
            SetupHttpContextUser(GetMedicalRecordDetailMockData.PatientUserId, nameof(UserRole.PATIENT));

            _sut = new GetMedicalRecordDetailService(
                _medicalRecordRepoMock.Object,
                _doctorRepoMock.Object,
                _patientRepoMock.Object,
                _mongoMock.Object,
                _validatorMock.Object,
                _httpContextAccessorMock.Object);
        }

        // ─────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────

        private void SetupValidator(bool isValid)
        {
            var result = isValid
                ? new ValidationResult()
                : new ValidationResult(new[] { new ValidationFailure("Id", "invalid") });
            _validatorMock
                .Setup(v => v.Validate(It.IsAny<GetMedicalRecordDetailRequest>()))
                .Returns(result);
        }

        private void SetupHttpContextUser(Guid? userId, string? role)
        {
            var claims = new List<Claim>();
            if (userId.HasValue) claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
            if (role != null) claims.Add(new Claim(ClaimTypes.Role, role));
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        }

        private void SetupMedicalRecord(IEnumerable<MedicalRecord> records)
        {
            var list = records.ToList();
            _medicalRecordRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<MedicalRecord, bool>>>(), It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<MedicalRecord>().Object);
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

        /// <summary>Wires up a Mongo cursor that yields a single document (or none, when <paramref name="doc"/> is null).</summary>
        private void SetupMongoFind(MedicalRecordDocument? doc)
        {
            var items = doc == null ? new List<MedicalRecordDocument>() : new List<MedicalRecordDocument> { doc };
            var cursorMock = new Mock<IAsyncCursor<MedicalRecordDocument>>();
            cursorMock.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(items.Count > 0)
                .ReturnsAsync(false);
            cursorMock.Setup(x => x.Current).Returns(items);

            // NOTE: the fluent `.Find(filter)` the service calls is entirely an *extension
            // method* (IMongoCollectionExtensions.Find) — IMongoCollection<T> has no instance
            // "Find" member at all, so Moq cannot intercept it directly. The IFindFluent object
            // that extension returns executes by internally calling the driver's one real
            // interface member, FindAsync(...), so that is what we mock here.
            _mongoCollectionMock
                .Setup(x => x.FindAsync(
                    It.IsAny<FilterDefinition<MedicalRecordDocument>>(),
                    It.IsAny<FindOptions<MedicalRecordDocument, MedicalRecordDocument>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(cursorMock.Object);
        }

        private void SetupMongoFindThrows()
        {
            _mongoCollectionMock
                .Setup(x => x.FindAsync(
                    It.IsAny<FilterDefinition<MedicalRecordDocument>>(),
                    It.IsAny<FindOptions<MedicalRecordDocument, MedicalRecordDocument>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("mongo unavailable"));
        }

        private static MedicalRecordDocument BuildMongoDoc(string formDataJson, string? sha256Checksum = null)
        {
            var bson = BsonDocument.Parse(formDataJson);
            var checksum = sha256Checksum ?? MongoDbContext.ComputeSha256(bson.ToJson());
            return new MedicalRecordDocument
            {
                Id = GetMedicalRecordDetailMockData.ValidMongoDocumentId,
                FormData = bson,
                Sha256Checksum = checksum
            };
        }

        /// <summary>Wires up repos for a patient (owner) requesting their own unlocked record.</summary>
        private MedicalRecord SetupHappyPathAsPatientOwner()
        {
            var patient = GetMedicalRecordDetailMockData.GetPatientProfile();
            SetupHttpContextUser(patient.UserId, nameof(UserRole.PATIENT));
            SetupPatient(new[] { patient });
            var record = GetMedicalRecordDetailMockData.GetMedicalRecord(patient: patient);
            SetupMedicalRecord(new[] { record });
            SetupMongoFind(BuildMongoDoc(GetMedicalRecordDetailMockData.ValidFormDataJson));
            return record;
        }

        // ==================================================================
        // ==================== VALIDATION TESTS ============================
        // ==================================================================

        /// <summary>TC-01: FluentValidation reports invalid → APP_MESSAGE_4003, short-circuits.</summary>
        [Fact]
        public async Task Process_InvalidRequest_ReturnsFailWith4003()
        {
            //Arrange
            SetupValidator(isValid: false);
            var request = GetMedicalRecordDetailMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4003.ToString());
            result.Data.Should().BeNull();
            _medicalRecordRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<MedicalRecord, bool>>>(), It.IsAny<bool>()),
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
            SetupHttpContextUser(userId: null, role: nameof(UserRole.PATIENT));
            var request = GetMedicalRecordDetailMockData.GetValidRequest();

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
            var request = GetMedicalRecordDetailMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        // ==================================================================
        // ================ USER PROFILE RESOLUTION TESTS =====================
        // ==================================================================

        /// <summary>TC-04: Role is PATIENT but no matching PatientProfile exists → APP_MESSAGE_4033.</summary>
        [Fact]
        public async Task Process_PatientProfileNotFound_ReturnsFailWith4033()
        {
            //Arrange
            SetupHttpContextUser(GetMedicalRecordDetailMockData.PatientUserId, nameof(UserRole.PATIENT));
            SetupPatient(Array.Empty<PatientProfile>());
            var request = GetMedicalRecordDetailMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            _medicalRecordRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<MedicalRecord, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>TC-05: Role is DOCTOR but no active DoctorProfile exists for the user → APP_MESSAGE_4033.</summary>
        [Fact]
        public async Task Process_DoctorProfileNotFoundOrInactive_ReturnsFailWith4033()
        {
            //Arrange
            SetupHttpContextUser(GetMedicalRecordDetailMockData.DoctorUserId, nameof(UserRole.DOCTOR));
            SetupDoctor(Array.Empty<DoctorProfile>());
            var request = GetMedicalRecordDetailMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            _medicalRecordRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<MedicalRecord, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>TC-06: Role does not match PATIENT/DOCTOR/CLINIC_ADMIN/RECEPTIONIST/SYSTEM_ADMIN → APP_MESSAGE_4033.</summary>
        [Fact]
        public async Task Process_UnrecognizedRole_ReturnsFailWith4033()
        {
            //Arrange
            SetupHttpContextUser(GetMedicalRecordDetailMockData.PatientUserId, "SOME_UNKNOWN_ROLE");
            var request = GetMedicalRecordDetailMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        /// <summary>TC-07: Staff roles (CLINIC_ADMIN / RECEPTIONIST / SYSTEM_ADMIN) skip profile lookup entirely.</summary>
        [Theory]
        [InlineData(nameof(UserRole.CLINIC_ADMIN))]
        [InlineData(nameof(UserRole.RECEPTIONIST))]
        [InlineData(nameof(UserRole.SYSTEM_ADMIN))]
        public async Task Process_StaffRole_SkipsProfileLookup_ReturnsSuccess(string staffRole)
        {
            //Arrange
            SetupHttpContextUser(Guid.NewGuid(), staffRole);
            var record = GetMedicalRecordDetailMockData.GetMedicalRecord();
            SetupMedicalRecord(new[] { record });
            SetupMongoFind(BuildMongoDoc(GetMedicalRecordDetailMockData.ValidFormDataJson));
            var request = GetMedicalRecordDetailMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.CanEdit.Should().BeTrue();
            result.Data.CanViewOnly.Should().BeFalse();
            _doctorRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<DoctorProfile, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _patientRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        // ==================================================================
        // ==================== RECORD LOOKUP TESTS ===========================
        // ==================================================================

        /// <summary>TC-08: No MedicalRecord matches the requested Id → APP_MESSAGE_4028.</summary>
        [Fact]
        public async Task Process_RecordNotFound_ReturnsFailWith4028()
        {
            //Arrange
            var patient = GetMedicalRecordDetailMockData.GetPatientProfile();
            SetupHttpContextUser(patient.UserId, nameof(UserRole.PATIENT));
            SetupPatient(new[] { patient });
            SetupMedicalRecord(Array.Empty<MedicalRecord>());
            var request = GetMedicalRecordDetailMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4028.ToString());
        }

        // ==================================================================
        // ==================== ACCESS CONTROL TESTS ==========================
        // ==================================================================

        /// <summary>TC-09: A patient requesting a record that belongs to a different patient → APP_MESSAGE_4014.</summary>
        [Fact]
        public async Task Process_PatientAccessingOthersRecord_ReturnsFailWith4014()
        {
            //Arrange
            var patient = GetMedicalRecordDetailMockData.GetPatientProfile();
            SetupHttpContextUser(patient.UserId, nameof(UserRole.PATIENT));
            SetupPatient(new[] { patient });
            var record = GetMedicalRecordDetailMockData.GetMedicalRecord(
                patientId: GetMedicalRecordDetailMockData.OtherProfileId);
            SetupMedicalRecord(new[] { record });
            var request = GetMedicalRecordDetailMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4014.ToString());
            _mongoCollectionMock.Verify(
                x => x.FindAsync(
                    It.IsAny<FilterDefinition<MedicalRecordDocument>>(),
                    It.IsAny<FindOptions<MedicalRecordDocument, MedicalRecordDocument>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>TC-10: A doctor requesting a record that belongs to a different doctor → APP_MESSAGE_4014.</summary>
        [Fact]
        public async Task Process_DoctorAccessingOthersRecord_ReturnsFailWith4014()
        {
            //Arrange
            var doctor = GetMedicalRecordDetailMockData.GetDoctorProfile();
            SetupHttpContextUser(doctor.UserId, nameof(UserRole.DOCTOR));
            SetupDoctor(new[] { doctor });
            var record = GetMedicalRecordDetailMockData.GetMedicalRecord(
                doctorId: GetMedicalRecordDetailMockData.OtherProfileId);
            SetupMedicalRecord(new[] { record });
            var request = GetMedicalRecordDetailMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4014.ToString());
        }

        /// <summary>TC-11: Staff can view any record, bypassing patient/doctor ownership checks.</summary>
        [Fact]
        public async Task Process_StaffAccessingAnyRecord_BypassesOwnershipCheck_ReturnsSuccess()
        {
            //Arrange
            SetupHttpContextUser(Guid.NewGuid(), nameof(UserRole.SYSTEM_ADMIN));
            var record = GetMedicalRecordDetailMockData.GetMedicalRecord(
                patientId: GetMedicalRecordDetailMockData.OtherProfileId,
                doctorId: GetMedicalRecordDetailMockData.OtherProfileId);
            SetupMedicalRecord(new[] { record });
            SetupMongoFind(BuildMongoDoc(GetMedicalRecordDetailMockData.ValidFormDataJson));
            var request = GetMedicalRecordDetailMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
        }

        // ==================================================================
        // ==================== MONGO FORM DATA TESTS ==========================
        // ==================================================================

        /// <summary>TC-12: Legacy record with no MongoDocumentId → FormData resolves to an empty JSON object.</summary>
        [Fact]
        public async Task Process_NoMongoDocumentId_ReturnsEmptyFormDataObject()
        {
            //Arrange
            var patient = GetMedicalRecordDetailMockData.GetPatientProfile();
            SetupHttpContextUser(patient.UserId, nameof(UserRole.PATIENT));
            SetupPatient(new[] { patient });
            var record = GetMedicalRecordDetailMockData.GetMedicalRecord(patient: patient, mongoDocumentId: null);
            SetupMedicalRecord(new[] { record });
            var request = GetMedicalRecordDetailMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.FormData.Should().NotBeNull();
            result.Data.FormData!.Value.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Object);
            result.Data.FormData.Value.EnumerateObject().Should().BeEmpty();
            _mongoCollectionMock.Verify(
                x => x.FindAsync(
                    It.IsAny<FilterDefinition<MedicalRecordDocument>>(),
                    It.IsAny<FindOptions<MedicalRecordDocument, MedicalRecordDocument>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>TC-13: MongoDocumentId is set but no matching document is found → APP_MESSAGE_5001.</summary>
        [Fact]
        public async Task Process_MongoDocNotFound_ReturnsFailWith5001()
        {
            //Arrange
            var patient = GetMedicalRecordDetailMockData.GetPatientProfile();
            SetupHttpContextUser(patient.UserId, nameof(UserRole.PATIENT));
            SetupPatient(new[] { patient });
            var record = GetMedicalRecordDetailMockData.GetMedicalRecord(patient: patient);
            SetupMedicalRecord(new[] { record });
            SetupMongoFind(doc: null);
            var request = GetMedicalRecordDetailMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
        }

        /// <summary>TC-14: Mongo query throws → the exception is swallowed and APP_MESSAGE_5001 is returned.</summary>
        [Fact]
        public async Task Process_MongoFindThrows_ReturnsFailWith5001()
        {
            //Arrange
            var patient = GetMedicalRecordDetailMockData.GetPatientProfile();
            SetupHttpContextUser(patient.UserId, nameof(UserRole.PATIENT));
            SetupPatient(new[] { patient });
            var record = GetMedicalRecordDetailMockData.GetMedicalRecord(patient: patient);
            SetupMedicalRecord(new[] { record });
            SetupMongoFindThrows();
            var request = GetMedicalRecordDetailMockData.GetValidRequest();

            //Act
            var act = async () => await _sut.Process(request);
            var result = await act.Should().NotThrowAsync();

            //Assert
            result.Subject.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
        }

        /// <summary>TC-15: Mongo document found and checksum matches → FormData populated, IntegrityValid true.</summary>
        [Fact]
        public async Task Process_MongoDocFound_ChecksumMatches_ReturnsFormDataWithIntegrityValidTrue()
        {
            //Arrange
            var record = SetupHappyPathAsPatientOwner();
            var request = GetMedicalRecordDetailMockData.GetValidRequest(record.Id);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.FormData.Should().NotBeNull();
            result.Data.IntegrityValid.Should().BeTrue();
        }

        /// <summary>TC-16: Mongo document found but the stored checksum no longer matches the recomputed one → IntegrityValid false.</summary>
        [Fact]
        public async Task Process_MongoDocFound_ChecksumMismatch_ReturnsIntegrityValidFalse()
        {
            //Arrange
            var patient = GetMedicalRecordDetailMockData.GetPatientProfile();
            SetupHttpContextUser(patient.UserId, nameof(UserRole.PATIENT));
            SetupPatient(new[] { patient });
            var record = GetMedicalRecordDetailMockData.GetMedicalRecord(patient: patient);
            SetupMedicalRecord(new[] { record });
            SetupMongoFind(BuildMongoDoc(GetMedicalRecordDetailMockData.ValidFormDataJson, sha256Checksum: "tampered-checksum"));
            var request = GetMedicalRecordDetailMockData.GetValidRequest(record.Id);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.IntegrityValid.Should().BeFalse();
        }

        /// <summary>TC-17: Access-denied responses never reach the Mongo fetch step.</summary>
        [Fact]
        public async Task Process_AccessDenied_DoesNotQueryMongo()
        {
            //Arrange
            var patient = GetMedicalRecordDetailMockData.GetPatientProfile();
            SetupHttpContextUser(patient.UserId, nameof(UserRole.PATIENT));
            SetupPatient(new[] { patient });
            var record = GetMedicalRecordDetailMockData.GetMedicalRecord(
                patientId: GetMedicalRecordDetailMockData.OtherProfileId);
            SetupMedicalRecord(new[] { record });
            var request = GetMedicalRecordDetailMockData.GetValidRequest(record.Id);

            //Act
            await _sut.Process(request);

            //Assert
            _mongoCollectionMock.Verify(
                x => x.FindAsync(
                    It.IsAny<FilterDefinition<MedicalRecordDocument>>(),
                    It.IsAny<FindOptions<MedicalRecordDocument, MedicalRecordDocument>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // ==================================================================
        // ================ PERMISSION FLAG (canEdit/canViewOnly) TESTS =========
        // ==================================================================

        /// <summary>TC-18: The owning doctor of an unlocked record can edit; no restriction reason is set.</summary>
        [Fact]
        public async Task Process_DoctorOwnsUnlockedRecord_CanEditTrueNoRestrictionReason()
        {
            //Arrange
            var doctor = GetMedicalRecordDetailMockData.GetDoctorProfile();
            SetupHttpContextUser(doctor.UserId, nameof(UserRole.DOCTOR));
            SetupDoctor(new[] { doctor });
            var record = GetMedicalRecordDetailMockData.GetMedicalRecord(doctor: doctor, isLocked: false);
            SetupMedicalRecord(new[] { record });
            SetupMongoFind(BuildMongoDoc(GetMedicalRecordDetailMockData.ValidFormDataJson));
            var request = GetMedicalRecordDetailMockData.GetValidRequest(record.Id);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data!.CanEdit.Should().BeTrue();
            result.Data.CanViewOnly.Should().BeFalse();
            result.Data.EditRestrictionReason.Should().BeNull();
        }

        /// <summary>TC-19: The owning doctor of a locked record cannot edit and gets a restriction reason.</summary>
        [Fact]
        public async Task Process_DoctorOwnsLockedRecord_CanEditFalseWithRestrictionReason()
        {
            //Arrange
            var doctor = GetMedicalRecordDetailMockData.GetDoctorProfile();
            SetupHttpContextUser(doctor.UserId, nameof(UserRole.DOCTOR));
            SetupDoctor(new[] { doctor });
            var record = GetMedicalRecordDetailMockData.GetMedicalRecord(doctor: doctor, isLocked: true);
            SetupMedicalRecord(new[] { record });
            SetupMongoFind(BuildMongoDoc(GetMedicalRecordDetailMockData.ValidFormDataJson));
            var request = GetMedicalRecordDetailMockData.GetValidRequest(record.Id);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data!.CanEdit.Should().BeFalse();
            result.Data.CanViewOnly.Should().BeTrue();
            result.Data.EditRestrictionReason.Should().Be("Hồ sơ đã bị khóa");
        }

        /// <summary>TC-20: A patient viewing their own record always gets view-only access (never CanEdit).</summary>
        [Fact]
        public async Task Process_PatientViewingOwnRecord_CanEditFalseCanViewOnlyTrue()
        {
            //Arrange
            var record = SetupHappyPathAsPatientOwner();
            var request = GetMedicalRecordDetailMockData.GetValidRequest(record.Id);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data!.CanEdit.Should().BeFalse();
            result.Data.CanViewOnly.Should().BeTrue();
            result.Data.EditRestrictionReason.Should().BeNull();
        }

        // ==================================================================
        // ==================== RESPONSE MAPPING TESTS =========================
        // ==================================================================

        /// <summary>TC-21: Full happy path → every field on the response is mapped from the record graph.</summary>
        [Fact]
        public async Task Process_HappyPath_ReturnsFullyPopulatedResponse()
        {
            //Arrange
            var record = SetupHappyPathAsPatientOwner();
            var request = GetMedicalRecordDetailMockData.GetValidRequest(record.Id);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            var data = result.Data!;
            data.Id.Should().Be(record.Id);
            data.AppointmentId.Should().Be(record.AppointmentId);
            data.PatientId.Should().Be(record.PatientId);
            data.DoctorId.Should().Be(record.DoctorId);
            data.RecordType.Should().Be(record.RecordType.ToString());
            data.Status.Should().Be(record.Status.ToString());
            data.ChiefComplaint.Should().Be(record.ChiefComplaint);
            data.Summary.Should().Be(record.Summary);
            data.Notes.Should().Be(record.Notes);
            data.IsLocked.Should().Be(record.IsLocked);

            data.PatientFullName.Should().Be(record.Patient.FullName);
            data.PatientDob.Should().Be(record.Patient.Dob.ToString("dd/MM/yyyy"));
            data.PatientPhone.Should().Be(record.Patient.PhoneNumber);
            data.PatientEmail.Should().Be(record.Patient.User!.Email);
            data.PatientGender.Should().Be(record.Patient.Gender.ToString());
            data.PatientAddress.Should().Be(record.Patient.Address);
            data.PatientIdentityNumber.Should().Be(record.Patient.IdentityNumber);

            data.DoctorFullName.Should().Be(record.Doctor.User!.FullName);
            data.DoctorTitle.Should().Be(record.Doctor.Title);
            data.DoctorSpecialty.Should().Be(record.Doctor.Specialty!.Name);

            data.AppointmentDate.Should().Be(record.Appointment.AppointmentDate);
            data.AppointmentStatus.Should().Be(record.Appointment.Status.ToString());
            data.AppointmentNotes.Should().Be(record.Appointment.NoteReason);

            data.MongoDocumentId.Should().Be(record.MongoDocumentId);
            data.RecordDataSchemaVersion.Should().Be(record.RecordDataSchemaVersion);
            data.RecordDataVersion.Should().Be(record.RecordDataVersion);
            data.RecordDataSizeBytes.Should().Be(record.RecordDataSizeBytes);
        }

        /// <summary>TC-22: Patient's linked User is null → PatientEmail falls back to an empty string.</summary>
        [Fact]
        public async Task Process_PatientUserIsNull_PatientEmailDefaultsToEmpty()
        {
            //Arrange
            var patient = GetMedicalRecordDetailMockData.GetPatientProfile();
            patient.User = null;
            SetupHttpContextUser(patient.UserId, nameof(UserRole.PATIENT));
            SetupPatient(new[] { patient });
            var record = GetMedicalRecordDetailMockData.GetMedicalRecord(patient: patient);
            SetupMedicalRecord(new[] { record });
            SetupMongoFind(BuildMongoDoc(GetMedicalRecordDetailMockData.ValidFormDataJson));
            var request = GetMedicalRecordDetailMockData.GetValidRequest(record.Id);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data!.PatientEmail.Should().Be(string.Empty);
        }

        /// <summary>TC-23: Doctor's linked User is null → DoctorFullName falls back to "N/A".</summary>
        [Fact]
        public async Task Process_DoctorUserIsNull_DoctorFullNameDefaultsToNA()
        {
            //Arrange
            var doctor = GetMedicalRecordDetailMockData.GetDoctorProfile();
            doctor.User = null;
            SetupHttpContextUser(Guid.NewGuid(), nameof(UserRole.SYSTEM_ADMIN));
            var record = GetMedicalRecordDetailMockData.GetMedicalRecord(doctor: doctor);
            SetupMedicalRecord(new[] { record });
            SetupMongoFind(BuildMongoDoc(GetMedicalRecordDetailMockData.ValidFormDataJson));
            var request = GetMedicalRecordDetailMockData.GetValidRequest(record.Id);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data!.DoctorFullName.Should().Be("N/A");
        }

        /// <summary>TC-24: Doctor has no assigned Specialty → DoctorSpecialty falls back to an empty string.</summary>
        [Fact]
        public async Task Process_DoctorSpecialtyIsNull_DoctorSpecialtyDefaultsToEmpty()
        {
            //Arrange
            var doctor = GetMedicalRecordDetailMockData.GetDoctorProfile(specialty: null);
            doctor.Specialty = null;
            SetupHttpContextUser(Guid.NewGuid(), nameof(UserRole.SYSTEM_ADMIN));
            var record = GetMedicalRecordDetailMockData.GetMedicalRecord(doctor: doctor);
            SetupMedicalRecord(new[] { record });
            SetupMongoFind(BuildMongoDoc(GetMedicalRecordDetailMockData.ValidFormDataJson));
            var request = GetMedicalRecordDetailMockData.GetValidRequest(record.Id);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data!.DoctorSpecialty.Should().Be(string.Empty);
        }
    }
}