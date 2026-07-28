using ECS.Application.Services.MedicalRecordsServices.GetMedicalRecordsServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Linq.Expressions;
using System.Security.Claims;

namespace ECS.Test.Services.MedicalRecordsServices.GetMedicalRecordsServices
{
    /// <summary>
    /// Unit tests for <see cref="GetMedicalRecordsService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line AND branch coverage on <c>GetMedicalRecordsService.cs</c>.
    /// </summary>
    /// <remarks>
    /// NOTE: <c>ApiResponse&lt;T&gt;.Meta</c> and <c>MetaResponse.TotalItems/PageNumber/PageSize</c>
    /// (used in TC-20) are guessed — neither type's source was available. Adjust the property
    /// names in <c>Process_PaginationMeta_ReflectsTotalRecordCount</c> to match the real
    /// <c>MetaResponse</c> shape if they differ.
    /// </remarks>
    /// <remarks>
    /// <see cref="GetMedicalRecordsService.RetrieveDoctorProfileId"/> queries
    /// <c>_context.Set&lt;DoctorProfile&gt;()</c> directly (not through a repository), so a real
    /// in-memory <see cref="AppDbContext"/> is used and seeded per test. The medical record
    /// repository, however, is fully mocked: <c>FindByCondition</c> is wired to apply the
    /// service's real filter <see cref="Expression"/> against an in-memory list via
    /// <c>list.AsQueryable().Where(expr)</c> — a plain LINQ-to-Objects provider — so the actual
    /// filter predicate built by <c>BuildFilterExpression</c> is exercised end-to-end, while
    /// <c>.Include()</c>/<c>.ThenInclude()</c> calls on top of it safely no-op (EF Core's Include
    /// only takes effect against an <c>EntityQueryProvider</c>; navigation properties are already
    /// populated on the in-memory mock objects regardless).
    /// </remarks>
    public class GetMedicalRecordsServiceTests : IDisposable
    {
        private readonly Mock<IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext>> _medicalRecordRepoMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly AppDbContext _context;
        private readonly GetMedicalRecordsService _sut;

        public GetMedicalRecordsServiceTests()
        {
            _context = NewInMemoryContext();
            SetupHttpContextUser(GetMedicalRecordsMockData.DoctorUserId);
            SetupMedicalRecords(Array.Empty<MedicalRecord>());

            _sut = new GetMedicalRecordsService(_medicalRecordRepoMock.Object, _context, _httpContextAccessorMock.Object);
        }

        public void Dispose() => _context.Dispose();

        // ─────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────

        private static AppDbContext NewInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
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
            var identity = new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, rawClaimValue) }, "TestAuth");
            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        }

        /// <summary>Applies the service's real filter expression against an in-memory list (LINQ-to-Objects).</summary>
        private void SetupMedicalRecords(IEnumerable<MedicalRecord> records)
        {
            var list = records.ToList();
            _medicalRecordRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<MedicalRecord, bool>>>(), It.IsAny<bool>()))
                .Returns((Expression<Func<MedicalRecord, bool>> expr, bool _) => list.AsQueryable().Where(expr));
        }

        /// <summary>Bypasses the real filter expression entirely, returning the raw list unfiltered — used to
        /// exercise mapping branches (e.g. "not the current doctor") that are otherwise unreachable because the
        /// service's own filter always constrains results to <c>DoctorId == doctorProfileId</c>.</summary>
        private void SetupMedicalRecordsIgnoringFilter(IEnumerable<MedicalRecord> records)
        {
            var list = records.ToList();
            _medicalRecordRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<MedicalRecord, bool>>>(), It.IsAny<bool>()))
                .Returns(list.AsQueryable());
        }

        private void SeedDoctor(DoctorProfile doctor)
        {
            _context.Add(doctor);
            _context.SaveChanges();
        }

        // ==================================================================
        // ================== AUTHENTICATION / PROFILE TESTS ===================
        // ==================================================================

        /// <summary>TC-01: No NameIdentifier claim on HttpContext.User → APP_MESSAGE_4033.</summary>
        [Fact]
        public async Task Process_NoAuthenticatedUserClaim_ReturnsFailWith4033()
        {
            //Arrange
            SetupHttpContextUser(userId: null);
            var request = GetMedicalRecordsMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
            result.Data.Should().BeNull();
        }

        /// <summary>TC-02: HttpContext itself is null → APP_MESSAGE_4033.</summary>
        [Fact]
        public async Task Process_NullHttpContext_ReturnsFailWith4033()
        {
            //Arrange
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
            var request = GetMedicalRecordsMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        /// <summary>TC-03: NameIdentifier claim is present but not a parsable GUID → APP_MESSAGE_4033.</summary>
        [Fact]
        public async Task Process_NonGuidClaimValue_ReturnsFailWith4033()
        {
            //Arrange
            SetupHttpContextUserWithRawClaim("not-a-guid");
            var request = GetMedicalRecordsMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        /// <summary>TC-04: No DoctorProfile row matches the authenticated user → APP_MESSAGE_4033.</summary>
        [Fact]
        public async Task Process_DoctorProfileNotFound_ReturnsFailWith4033()
        {
            //Arrange
            var request = GetMedicalRecordsMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        /// <summary>TC-05: A DoctorProfile row exists for the user but is inactive → APP_MESSAGE_4033.</summary>
        [Fact]
        public async Task Process_DoctorProfileInactive_ReturnsFailWith4033()
        {
            //Arrange
            SeedDoctor(GetMedicalRecordsMockData.GetDoctorProfile(isActive: false));
            var request = GetMedicalRecordsMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString());
        }

        /// <summary>TC-06: Valid doctor with no matching records → success with an empty list.</summary>
        [Fact]
        public async Task Process_ValidDoctorNoRecords_ReturnsSuccessWithEmptyList()
        {
            //Arrange
            SeedDoctor(GetMedicalRecordsMockData.GetDoctorProfile());
            var request = GetMedicalRecordsMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().BeEmpty();
        }

        // ==================================================================
        // ==================== FILTER EXPRESSION TESTS ========================
        // ==================================================================

        /// <summary>TC-07: RecordType filter only returns records matching the requested type.</summary>
        [Fact]
        public async Task Process_FilterByRecordType_OnlyReturnsMatchingType()
        {
            //Arrange
            var doctor = GetMedicalRecordsMockData.GetDoctorProfile();
            SeedDoctor(doctor);
            var matching = GetMedicalRecordsMockData.GetMedicalRecord(
                id: Guid.NewGuid(), doctor: doctor, recordType: RecordType.MS22_ANTERIOR);
            var nonMatching = GetMedicalRecordsMockData.GetMedicalRecord(
                id: Guid.NewGuid(), doctor: doctor, recordType: RecordType.MS23_FUNDUS);
            SetupMedicalRecords(new[] { matching, nonMatching });
            var request = GetMedicalRecordsMockData.GetValidRequest(recordType: RecordType.MS22_ANTERIOR);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data.Should().ContainSingle(x => x.Id == matching.Id);
        }

        /// <summary>TC-08: StartDate filter excludes records created before the given date.</summary>
        [Fact]
        public async Task Process_FilterByStartDate_ExcludesRecordsBeforeStartDate()
        {
            //Arrange
            var doctor = GetMedicalRecordsMockData.GetDoctorProfile();
            SeedDoctor(doctor);
            var before = GetMedicalRecordsMockData.GetMedicalRecord(
                id: Guid.NewGuid(), doctor: doctor, createdAt: new DateTime(2026, 7, 1));
            var after = GetMedicalRecordsMockData.GetMedicalRecord(
                id: Guid.NewGuid(), doctor: doctor, createdAt: new DateTime(2026, 7, 10));
            SetupMedicalRecords(new[] { before, after });
            var request = GetMedicalRecordsMockData.GetValidRequest(startDate: new DateTime(2026, 7, 5));

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data.Should().ContainSingle(x => x.Id == after.Id);
        }

        /// <summary>TC-09: EndDate filter is inclusive through the end of that calendar day (EndDate + 1 day boundary).</summary>
        [Fact]
        public async Task Process_FilterByEndDate_IsInclusiveOfEndDate()
        {
            //Arrange
            var doctor = GetMedicalRecordsMockData.GetDoctorProfile();
            SeedDoctor(doctor);
            var onEndDate = GetMedicalRecordsMockData.GetMedicalRecord(
                id: Guid.NewGuid(), doctor: doctor, createdAt: new DateTime(2026, 7, 10, 23, 0, 0));
            var afterEndDate = GetMedicalRecordsMockData.GetMedicalRecord(
                id: Guid.NewGuid(), doctor: doctor, createdAt: new DateTime(2026, 7, 12));
            SetupMedicalRecords(new[] { onEndDate, afterEndDate });
            var request = GetMedicalRecordsMockData.GetValidRequest(endDate: new DateTime(2026, 7, 10));

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data.Should().ContainSingle(x => x.Id == onEndDate.Id);
        }

        /// <summary>TC-10: SearchTerm matches the patient's full name (case-insensitive).</summary>
        [Fact]
        public async Task Process_SearchTermMatchesPatientFullName_ReturnsMatch()
        {
            //Arrange
            var doctor = GetMedicalRecordsMockData.GetDoctorProfile();
            SeedDoctor(doctor);
            var patient = GetMedicalRecordsMockData.GetPatientProfile(fullName: "Tran Thi B");
            var record = GetMedicalRecordsMockData.GetMedicalRecord(doctor: doctor, patient: patient);
            SetupMedicalRecords(new[] { record });
            var request = GetMedicalRecordsMockData.GetValidRequest(searchTerm: "tran thi");

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data.Should().ContainSingle(x => x.Id == record.Id);
        }

        /// <summary>TC-11: SearchTerm matches the AppointmentId (as a string, e.g. partial GUID search).</summary>
        [Fact]
        public async Task Process_SearchTermMatchesAppointmentId_ReturnsMatch()
        {
            //Arrange
            var doctor = GetMedicalRecordsMockData.GetDoctorProfile();
            SeedDoctor(doctor);
            var appointment = GetMedicalRecordsMockData.GetAppointment(id: Guid.NewGuid());
            var record = GetMedicalRecordsMockData.GetMedicalRecord(
                doctor: doctor, appointmentId: appointment.Id, appointment: appointment);
            SetupMedicalRecords(new[] { record });
            var partialAppointmentId = appointment.Id.ToString().Substring(0, 8);
            var request = GetMedicalRecordsMockData.GetValidRequest(searchTerm: partialAppointmentId);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data.Should().ContainSingle(x => x.Id == record.Id);
        }

        /// <summary>TC-12: SearchTerm matches the ChiefComplaint field.</summary>
        [Fact]
        public async Task Process_SearchTermMatchesChiefComplaint_ReturnsMatch()
        {
            //Arrange
            var doctor = GetMedicalRecordsMockData.GetDoctorProfile();
            SeedDoctor(doctor);
            var record = GetMedicalRecordsMockData.GetMedicalRecord(doctor: doctor, chiefComplaint: "Nhìn mờ đột ngột");
            SetupMedicalRecords(new[] { record });
            var request = GetMedicalRecordsMockData.GetValidRequest(searchTerm: "nhìn mờ");

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data.Should().ContainSingle(x => x.Id == record.Id);
        }

        /// <summary>TC-13: SearchTerm matches the Summary field.</summary>
        [Fact]
        public async Task Process_SearchTermMatchesSummary_ReturnsMatch()
        {
            //Arrange
            var doctor = GetMedicalRecordsMockData.GetDoctorProfile();
            SeedDoctor(doctor);
            var record = GetMedicalRecordsMockData.GetMedicalRecord(doctor: doctor, summary: "Glôcôm góc đóng");
            SetupMedicalRecords(new[] { record });
            var request = GetMedicalRecordsMockData.GetValidRequest(searchTerm: "glôcôm");

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data.Should().ContainSingle(x => x.Id == record.Id);
        }

        /// <summary>TC-14: SearchTerm with no match on any searchable field → empty result list.</summary>
        [Fact]
        public async Task Process_SearchTermNoMatch_ReturnsEmptyList()
        {
            //Arrange
            var doctor = GetMedicalRecordsMockData.GetDoctorProfile();
            SeedDoctor(doctor);
            var record = GetMedicalRecordsMockData.GetMedicalRecord(doctor: doctor);
            SetupMedicalRecords(new[] { record });
            var request = GetMedicalRecordsMockData.GetValidRequest(searchTerm: "zzz-no-such-term-zzz");

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data.Should().BeEmpty();
        }

        /// <summary>TC-15: A null/empty ChiefComplaint and Summary don't blow up the search predicate (null-guard branch).</summary>
        [Fact]
        public async Task Process_SearchTerm_RecordsWithNullChiefComplaintAndSummary_DoNotThrow()
        {
            //Arrange
            var doctor = GetMedicalRecordsMockData.GetDoctorProfile();
            SeedDoctor(doctor);
            var record = GetMedicalRecordsMockData.GetMedicalRecord(doctor: doctor, chiefComplaint: null, summary: null);
            SetupMedicalRecords(new[] { record });
            var request = GetMedicalRecordsMockData.GetValidRequest(searchTerm: "anything");

            //Act
            var act = async () => await _sut.Process(request);

            //Assert
            await act.Should().NotThrowAsync();
        }

        /// <summary>TC-16: Explicit DoctorId filter that differs from the authenticated doctor yields no results,
        /// because the service always ANDs the filter with <c>DoctorId == doctorProfileId</c>.</summary>
        [Fact]
        public async Task Process_FilterDoctorIdDiffersFromCurrentDoctor_ReturnsEmptyList()
        {
            //Arrange
            var doctor = GetMedicalRecordsMockData.GetDoctorProfile();
            SeedDoctor(doctor);
            var record = GetMedicalRecordsMockData.GetMedicalRecord(doctor: doctor);
            SetupMedicalRecords(new[] { record });
            var request = GetMedicalRecordsMockData.GetValidRequest(doctorId: GetMedicalRecordsMockData.OtherDoctorProfileId);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data.Should().BeEmpty();
        }

        /// <summary>TC-17: Explicit DoctorId filter matching the authenticated doctor still returns the record.</summary>
        [Fact]
        public async Task Process_FilterDoctorIdMatchesCurrentDoctor_ReturnsRecord()
        {
            //Arrange
            var doctor = GetMedicalRecordsMockData.GetDoctorProfile();
            SeedDoctor(doctor);
            var record = GetMedicalRecordsMockData.GetMedicalRecord(doctor: doctor);
            SetupMedicalRecords(new[] { record });
            var request = GetMedicalRecordsMockData.GetValidRequest(doctorId: doctor.Id);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data.Should().ContainSingle(x => x.Id == record.Id);
        }

        // ==================================================================
        // ================ ORDERING / PAGINATION TESTS ========================
        // ==================================================================

        /// <summary>TC-18: Results are ordered by CreatedAt descending (most recent first).</summary>
        [Fact]
        public async Task Process_OrdersByCreatedAtDescending()
        {
            //Arrange
            var doctor = GetMedicalRecordsMockData.GetDoctorProfile();
            SeedDoctor(doctor);
            var older = GetMedicalRecordsMockData.GetMedicalRecord(
                id: Guid.NewGuid(), doctor: doctor, createdAt: new DateTime(2026, 7, 1));
            var newer = GetMedicalRecordsMockData.GetMedicalRecord(
                id: Guid.NewGuid(), doctor: doctor, createdAt: new DateTime(2026, 7, 20));
            SetupMedicalRecords(new[] { older, newer });
            var request = GetMedicalRecordsMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data!.Select(x => x.Id).Should().ContainInOrder(newer.Id, older.Id);
        }

        /// <summary>TC-19: Skip/Take correctly slices the requested page.</summary>
        [Fact]
        public async Task Process_Pagination_ReturnsCorrectPageSlice()
        {
            //Arrange
            var doctor = GetMedicalRecordsMockData.GetDoctorProfile();
            SeedDoctor(doctor);
            var records = Enumerable.Range(0, 5)
                .Select(i => GetMedicalRecordsMockData.GetMedicalRecord(
                    id: Guid.NewGuid(), doctor: doctor, createdAt: new DateTime(2026, 7, 1).AddDays(i)))
                .ToList();
            SetupMedicalRecords(records);
            // Newest-first order is [day4, day3, day2, day1, day0]; page 2 of size 2 → [day2, day1]
            var request = GetMedicalRecordsMockData.GetValidRequest(pageNumber: 2, pageSize: 2);

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data.Should().HaveCount(2);
            result.Data![0].Id.Should().Be(records[2].Id);
            result.Data[1].Id.Should().Be(records[1].Id);
        }

        ///// <summary>TC-20: Pagination metadata reflects the total matching record count, not just the returned page size.</summary>
        //[Fact]
        //public async Task Process_PaginationMeta_ReflectsTotalRecordCount()
        //{
        //    //Arrange
        //    var doctor = GetMedicalRecordsMockData.GetDoctorProfile();
        //    SeedDoctor(doctor);
        //    var records = Enumerable.Range(0, 5)
        //        .Select(i => GetMedicalRecordsMockData.GetMedicalRecord(id: Guid.NewGuid(), doctor: doctor))
        //        .ToList();
        //    SetupMedicalRecords(records);
        //    var request = GetMedicalRecordsMockData.GetValidRequest(pageNumber: 1, pageSize: 2);

        //    //Act
        //    var result = await _sut.Process(request);

        //    //Assert
        //    result.Data.Should().HaveCount(2);
        //    result.Meta!.TotalItems.Should().Be(5);
        //    result.Meta.PageNumber.Should().Be(1);
        //    result.Meta.PageSize.Should().Be(2);
        //}

        // ==================================================================
        // ================ PERMISSION FLAG (canEdit/canViewOnly) TESTS =========
        // ==================================================================

        /// <summary>TC-21: Today's appointment, current doctor, unlocked → CanEdit true, no restriction reason.</summary>
        [Fact]
        public async Task Process_TodayAppointmentCurrentDoctorUnlocked_CanEditTrue()
        {
            //Arrange
            var doctor = GetMedicalRecordsMockData.GetDoctorProfile();
            SeedDoctor(doctor);
            var appointment = GetMedicalRecordsMockData.GetAppointment(appointmentDate: DateTime.UtcNow.Date);
            var record = GetMedicalRecordsMockData.GetMedicalRecord(doctor: doctor, appointment: appointment, isLocked: false);
            SetupMedicalRecords(new[] { record });
            var request = GetMedicalRecordsMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            var dto = result.Data!.Single();
            dto.CanEdit.Should().BeTrue();
            dto.CanViewOnly.Should().BeFalse();
            dto.EditRestrictionReason.Should().BeNull();
        }

        /// <summary>TC-22: Locked record → CanEdit false with the "locked" restriction reason, regardless of date/ownership.</summary>
        [Fact]
        public async Task Process_LockedRecord_CanEditFalseWithLockedReason()
        {
            //Arrange
            var doctor = GetMedicalRecordsMockData.GetDoctorProfile();
            SeedDoctor(doctor);
            var appointment = GetMedicalRecordsMockData.GetAppointment(appointmentDate: DateTime.UtcNow.Date);
            var record = GetMedicalRecordsMockData.GetMedicalRecord(doctor: doctor, appointment: appointment, isLocked: true);
            SetupMedicalRecords(new[] { record });
            var request = GetMedicalRecordsMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            var dto = result.Data!.Single();
            dto.CanEdit.Should().BeFalse();
            dto.CanViewOnly.Should().BeTrue();
            dto.EditRestrictionReason.Should().Be("Hồ sơ đã bị khóa");
        }

        /// <summary>TC-23: Appointment date is in the past → CanEdit false with the "past appointment" restriction reason.</summary>
        [Fact]
        public async Task Process_PastAppointment_CanEditFalseWithPastReason()
        {
            //Arrange
            var doctor = GetMedicalRecordsMockData.GetDoctorProfile();
            SeedDoctor(doctor);
            var appointment = GetMedicalRecordsMockData.GetAppointment(appointmentDate: DateTime.UtcNow.Date.AddDays(-3));
            var record = GetMedicalRecordsMockData.GetMedicalRecord(doctor: doctor, appointment: appointment, isLocked: false);
            SetupMedicalRecords(new[] { record });
            var request = GetMedicalRecordsMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            var dto = result.Data!.Single();
            dto.CanEdit.Should().BeFalse();
            dto.EditRestrictionReason.Should().Be("Cuộc hẹn đã qua ngày khám");
        }

        /// <summary>TC-24: Appointment date is in the future → CanEdit false with the "future appointment" restriction reason.</summary>
        [Fact]
        public async Task Process_FutureAppointment_CanEditFalseWithFutureReason()
        {
            //Arrange
            var doctor = GetMedicalRecordsMockData.GetDoctorProfile();
            SeedDoctor(doctor);
            var appointment = GetMedicalRecordsMockData.GetAppointment(appointmentDate: DateTime.UtcNow.Date.AddDays(3));
            var record = GetMedicalRecordsMockData.GetMedicalRecord(doctor: doctor, appointment: appointment, isLocked: false);
            SetupMedicalRecords(new[] { record });
            var request = GetMedicalRecordsMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            var dto = result.Data!.Single();
            dto.CanEdit.Should().BeFalse();
            dto.EditRestrictionReason.Should().Be("Cuộc hẹn chưa đến ngày khám");
        }

        /// <summary>
        /// TC-25: A record belonging to a different doctor → CanEdit false with the "not treating doctor"
        /// restriction reason. Reached via <see cref="SetupMedicalRecordsIgnoringFilter"/> because the service's
        /// own filter (<c>DoctorId == doctorProfileId</c>) would otherwise make this combination unreachable
        /// through the public API — the mapping branch is still real, reachable code, so it is covered directly.
        /// </summary>
        [Fact]
        public async Task Process_RecordBelongsToDifferentDoctor_CanEditFalseWithNotTreatingDoctorReason()
        {
            //Arrange
            var doctor = GetMedicalRecordsMockData.GetDoctorProfile();
            SeedDoctor(doctor);
            var otherDoctor = GetMedicalRecordsMockData.GetDoctorProfile(
                id: GetMedicalRecordsMockData.OtherDoctorProfileId, userId: GetMedicalRecordsMockData.OtherDoctorUserId);
            var appointment = GetMedicalRecordsMockData.GetAppointment(appointmentDate: DateTime.UtcNow.Date);
            var record = GetMedicalRecordsMockData.GetMedicalRecord(doctor: otherDoctor, appointment: appointment, isLocked: false);
            SetupMedicalRecordsIgnoringFilter(new[] { record });
            var request = GetMedicalRecordsMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            var dto = result.Data!.Single();
            dto.CanEdit.Should().BeFalse();
            dto.EditRestrictionReason.Should().Be("Bạn không phải bác sĩ điều trị của hồ sơ này");
        }

        // ==================================================================
        // ==================== RESPONSE MAPPING TESTS =========================
        // ==================================================================

        /// <summary>TC-26: Full happy path → every mapped field matches the source record graph.</summary>
        [Fact]
        public async Task Process_HappyPath_ReturnsFullyPopulatedResponse()
        {
            //Arrange
            var doctor = GetMedicalRecordsMockData.GetDoctorProfile();
            SeedDoctor(doctor);
            var record = GetMedicalRecordsMockData.GetMedicalRecord(doctor: doctor);
            SetupMedicalRecords(new[] { record });
            var request = GetMedicalRecordsMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            var dto = result.Data!.Single();
            dto.Id.Should().Be(record.Id);
            dto.AppointmentId.Should().Be(record.AppointmentId);
            dto.PatientId.Should().Be(record.PatientId);
            dto.PatientFullName.Should().Be(record.Patient.FullName);
            dto.PatientDob.Should().Be(record.Patient.Dob.ToString("dd/MM/yyyy"));
            dto.PatientPhone.Should().Be(record.Patient.PhoneNumber);
            dto.DoctorId.Should().Be(record.DoctorId);
            dto.DoctorFullName.Should().Be(record.Doctor.User!.FullName);
            dto.AppointmentDate.Should().Be(record.Appointment.AppointmentDate);
            dto.RecordType.Should().Be(record.RecordType.ToString());
            dto.ChiefComplaint.Should().Be(record.ChiefComplaint);
            dto.DiagnosisMain.Should().Be(record.Summary);
            dto.TreatmentPlan.Should().Be(record.Notes);
            dto.IsLocked.Should().Be(record.IsLocked);
            dto.CreatedAt.Should().Be(record.CreatedAt);
            dto.UpdatedAt.Should().Be(record.UpdatedAt);
        }

        /// <summary>TC-27: Doctor's linked User is null → DoctorFullName falls back to "N/A".</summary>
        [Fact]
        public async Task Process_DoctorUserIsNull_DoctorFullNameDefaultsToNA()
        {
            //Arrange
            var doctor = GetMedicalRecordsMockData.GetDoctorProfile();
            doctor.User = null;
            SeedDoctor(doctor);
            var record = GetMedicalRecordsMockData.GetMedicalRecord(doctor: doctor);
            SetupMedicalRecords(new[] { record });
            var request = GetMedicalRecordsMockData.GetValidRequest();

            //Act
            var result = await _sut.Process(request);

            //Assert
            result.Data!.Single().DoctorFullName.Should().Be("N/A");
        }
    }
}