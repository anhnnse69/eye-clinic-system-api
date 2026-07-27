using System.Linq.Expressions;
using System.Reflection;
using ECS.Application.Services.DoctorScheduleManagementServices.CreateDoctorScheduleService;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.DoctorScheduleManagementServices.BatchCreateDoctorScheduleServices
{
    /// <summary>
    /// Unit tests for <see cref="BatchCreateDoctorScheduleService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line coverage on <c>BatchCreateDoctorScheduleService.cs</c>.
    /// </summary>
    /// <remarks>
    /// The service uses a real <see cref="AppDbContext"/> (in-memory) because:
    /// (a) <c>FetchRoomOccupancyAsync</c> queries <c>_dbContext.Set&lt;DoctorSchedule&gt;()</c>
    ///     with the shadow property <c>EF.Property&lt;Guid&gt;(s, "RoomId")</c>;
    /// (b) <c>CreateScheduleWithSlotsAsync</c> writes the shadow property via
    ///     <c>_dbContext.Entry(schedule).Property("RoomId").CurrentValue = roomId</c>.
    /// EF Core's in-memory provider honours navigation-property shadow FK conventions,
    /// so <c>RoomId</c> is automatically registered on <c>DoctorSchedule</c> from its
    /// <c>Room</c> navigation property when the model is built.
    /// </remarks>
    public class BatchCreateDoctorScheduleServiceTests : IDisposable
    {
        private static readonly Guid ReceptionistUserId = BatchCreateDoctorScheduleMockData.ReceptionistUserId;
        private static readonly Guid ClinicId = BatchCreateDoctorScheduleMockData.ClinicId;
        private static readonly Guid OtherClinicId = BatchCreateDoctorScheduleMockData.OtherClinicId;
        private static readonly Guid DoctorProfileId = BatchCreateDoctorScheduleMockData.DoctorProfileId;
        private static readonly Guid OtherDoctorProfileId = BatchCreateDoctorScheduleMockData.OtherDoctorProfileId;
        private static readonly Guid RoomId = BatchCreateDoctorScheduleMockData.RoomId;

        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext>> _roomRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<Clinic, Guid, AppDbContext>> _clinicRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<DoctorSchedule, Guid, AppDbContext>> _scheduleQueryRepoMock = new();
        private readonly Mock<IRepositoryBaseAsync<DoctorSchedule, Guid, AppDbContext>> _scheduleCommandRepoMock = new();
        private readonly Mock<IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext>> _slotCommandRepoMock = new();
        private readonly AppDbContext _context;
        private readonly BatchCreateDoctorScheduleService _sut;

        public BatchCreateDoctorScheduleServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);
            _sut = new BatchCreateDoctorScheduleService(
                _staffClinicRepoMock.Object,
                _doctorRepoMock.Object,
                _roomRepoMock.Object,
                _clinicRepoMock.Object,
                _scheduleQueryRepoMock.Object,
                _scheduleCommandRepoMock.Object,
                _slotCommandRepoMock.Object,
                _context);
            _scheduleCommandRepoMock.Setup(x => x.CreateAsync(It.IsAny<DoctorSchedule>())).Returns(Task.FromResult(Guid.Empty));
            _scheduleCommandRepoMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
            _slotCommandRepoMock.Setup(x => x.CreateAsync(It.IsAny<TimeSlot>())).Returns(Task.FromResult(Guid.Empty));
            _slotCommandRepoMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        }

        public void Dispose() => _context.Dispose();

        // ─────────────────────────────────────────────────────────────────
        // Repository helpers
        // ─────────────────────────────────────────────────────────────────

        private void SetupStaffClinic(IEnumerable<StaffClinic> staffClinics)
        {
            var list = staffClinics.ToList();
            _staffClinicRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<StaffClinic>().Object);
        }

        private void SetupClinic(IEnumerable<Clinic> clinics)
        {
            var list = clinics.ToList();
            _clinicRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<Clinic, bool>>>(), It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<Clinic>().Object);
        }

        private void SetupDoctor(IEnumerable<DoctorProfile> doctors)
        {
            var list = doctors.ToList();
            _doctorRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<DoctorProfile, bool>>>(), It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<DoctorProfile>().Object);
        }

        private void SetupRoom(IEnumerable<FacilityRoom> rooms)
        {
            var list = rooms.ToList();
            _roomRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<FacilityRoom, bool>>>(), It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<FacilityRoom>().Object);
        }

        private void SetupExistingSchedules(IEnumerable<DoctorSchedule> schedules)
        {
            var list = schedules.ToList();
            _scheduleQueryRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<DoctorSchedule, bool>>>(), It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<DoctorSchedule>().Object);
        }

        /// <summary>
        /// Standard "happy path" repo setup: valid receptionist staff clinic + active
        /// clinic + active doctor + active room. Empty existing-schedule list.
        /// </summary>
        private void SetupHappyPathRepos(
            Clinic? clinic = null,
            DoctorProfile? doctor = null,
            FacilityRoom? room = null)
        {
            SetupStaffClinic(new[] { BatchCreateDoctorScheduleMockData.GetStaffClinic() });
            SetupClinic(new[] { clinic ?? BatchCreateDoctorScheduleMockData.GetClinic() });
            SetupDoctor(new[] { doctor ?? BatchCreateDoctorScheduleMockData.GetDoctorProfile() });
            SetupRoom(new[] { room ?? BatchCreateDoctorScheduleMockData.GetFacilityRoom() });
            SetupExistingSchedules(Array.Empty<DoctorSchedule>());
        }

        // ==================================================================
        // ====================== Process(...) tests ========================
        // ==================================================================

        /// <summary>
        /// TC-BCDS-01: Assignments list is empty → ValidateBatchRequest throws
        /// <see cref="ArgumentException"/> with <c>APP_MESSAGE_4001</c>.
        /// Covers: <c>ValidateBatchRequest</c> <c>request.Assignments.Count == 0</c> branch.
        /// </summary>
        [Fact]
        public async Task Process_EmptyAssignments_ThrowsArgumentException()
        {
            //Arrange 1
            var request = new BatchCreateDoctorScheduleRequest
            {
                Assignments = new List<DoctorRoomAssignment>(),
                WorkDates = new List<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) },
                ShiftTypes = new List<string> { "MORNING" }
            };

            //Arrange 2
            // (no repo setup — validation runs first)

            //Act
            var act = () => _sut.Process(ReceptionistUserId, request);

            //Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4001.ToString());
        }

        /// <summary>
        /// TC-BCDS-02: Assignments is null → ValidateBatchRequest throws ArgumentException.
        /// Covers: <c>request.Assignments == null</c> branch.
        /// </summary>
        [Fact]
        public async Task Process_NullAssignments_ThrowsArgumentException()
        {
            //Arrange 1
            var request = new BatchCreateDoctorScheduleRequest
            {
                Assignments = null!,
                WorkDates = new List<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) },
                ShiftTypes = new List<string> { "MORNING" }
            };

            //Arrange 2

            //Act
            var act = () => _sut.Process(ReceptionistUserId, request);

            //Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4001.ToString());
        }

        /// <summary>
        /// TC-BCDS-03: WorkDates is empty → ScheduleHelper.ValidateWorkDates throws ArgumentException.
        /// Covers: <c>workDates.Count == 0</c> branch in ScheduleHelper.
        /// </summary>
        [Fact]
        public async Task Process_EmptyWorkDates_ThrowsArgumentException()
        {
            //Arrange 1
            var request = new BatchCreateDoctorScheduleRequest
            {
                Assignments = new List<DoctorRoomAssignment> { new() { DoctorId = DoctorProfileId, RoomId = RoomId } },
                WorkDates = new List<DateOnly>(),
                ShiftTypes = new List<string> { "MORNING" }
            };

            //Arrange 2

            //Act
            var act = () => _sut.Process(ReceptionistUserId, request);

            //Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4001.ToString());
        }

        /// <summary>
        /// TC-BCDS-04: WorkDates is null → ScheduleHelper.ValidateWorkDates throws ArgumentException.
        /// Covers: <c>workDates == null</c> branch in ScheduleHelper.
        /// </summary>
        [Fact]
        public async Task Process_NullWorkDates_ThrowsArgumentException()
        {
            //Arrange 1
            var request = new BatchCreateDoctorScheduleRequest
            {
                Assignments = new List<DoctorRoomAssignment> { new() { DoctorId = DoctorProfileId, RoomId = RoomId } },
                WorkDates = null!,
                ShiftTypes = new List<string> { "MORNING" }
            };

            //Arrange 2

            //Act
            var act = () => _sut.Process(ReceptionistUserId, request);

            //Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4001.ToString());
        }

        /// <summary>
        /// TC-BCDS-05: A work date is in the past → ScheduleHelper.ValidateWorkDates throws.
        /// Covers: <c>workDates.Any(d => d &lt; today)</c> branch.
        /// </summary>
        [Fact]
        public async Task Process_PastWorkDate_ThrowsArgumentException()
        {
            //Arrange 1
            var request = new BatchCreateDoctorScheduleRequest
            {
                Assignments = new List<DoctorRoomAssignment> { new() { DoctorId = DoctorProfileId, RoomId = RoomId } },
                WorkDates = new List<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)) },
                ShiftTypes = new List<string> { "MORNING" }
            };

            //Arrange 2

            //Act
            var act = () => _sut.Process(ReceptionistUserId, request);

            //Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4001.ToString());
        }

        /// <summary>
        /// TC-BCDS-06: ShiftTypes is empty → ScheduleHelper.ValidateShiftTypes throws ArgumentException.
        /// Covers: <c>shiftTypes.Count == 0</c> branch.
        /// </summary>
        [Fact]
        public async Task Process_EmptyShiftTypes_ThrowsArgumentException()
        {
            //Arrange 1
            var request = new BatchCreateDoctorScheduleRequest
            {
                Assignments = new List<DoctorRoomAssignment> { new() { DoctorId = DoctorProfileId, RoomId = RoomId } },
                WorkDates = new List<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) },
                ShiftTypes = new List<string>()
            };

            //Arrange 2

            //Act
            var act = () => _sut.Process(ReceptionistUserId, request);

            //Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4001.ToString());
        }

        /// <summary>
        /// TC-BCDS-07: ShiftTypes is null → ScheduleHelper.ValidateShiftTypes throws.
        /// Covers: <c>shiftTypes == null</c> branch.
        /// </summary>
        [Fact]
        public async Task Process_NullShiftTypes_ThrowsArgumentException()
        {
            //Arrange 1
            var request = new BatchCreateDoctorScheduleRequest
            {
                Assignments = new List<DoctorRoomAssignment> { new() { DoctorId = DoctorProfileId, RoomId = RoomId } },
                WorkDates = new List<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) },
                ShiftTypes = null!
            };

            //Arrange 2

            //Act
            var act = () => _sut.Process(ReceptionistUserId, request);

            //Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4001.ToString());
        }

        /// <summary>
        /// TC-BCDS-08: StaffClinic row for receptionist not found → ResolveReceptionistClinicIdAsync
        /// throws <see cref="KeyNotFoundException"/> with <c>APP_MESSAGE_4008</c>.
        /// Covers: <c>sc is null</c> branch.
        /// </summary>
        [Fact]
        public async Task Process_ReceptionistStaffClinicNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = BatchCreateDoctorScheduleMockData.GetValidRequest();

            //Arrange 2
            SetupStaffClinic(Array.Empty<StaffClinic>());

            //Act
            var act = () => _sut.Process(ReceptionistUserId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4008.ToString());
            _clinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Clinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>
        /// TC-BCDS-09: Clinic not found / inactive → ResolveClinicAsync throws
        /// <see cref="KeyNotFoundException"/> with <c>APP_MESSAGE_4008</c>.
        /// Covers: <c>clinic is null</c> branch.
        /// </summary>
        [Fact]
        public async Task Process_ClinicNotFoundOrInactive_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = BatchCreateDoctorScheduleMockData.GetValidRequest();

            //Arrange 2
            SetupStaffClinic(new[] { BatchCreateDoctorScheduleMockData.GetStaffClinic() });
            SetupClinic(Array.Empty<Clinic>());

            //Act
            var act = () => _sut.Process(ReceptionistUserId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4008.ToString());
        }

        /// <summary>
        /// TC-BCDS-10: Doctor profile belongs to a DIFFERENT clinic
        /// → ResolveActiveDoctorInClinicAsync returns null → assignment is silently
        /// skipped (no entry added to Results).
        /// Covers: <c>doctorProfile.ClinicId == clinicId</c> false branch in ResolveActiveDoctorInClinicAsync.
        /// </summary>
        [Fact]
        public async Task Process_DoctorFromForeignClinic_SilentlySkipsAssignment()
        {
            //Arrange 1
            var foreignDoctor = BatchCreateDoctorScheduleMockData.GetDoctorProfile(
                clinicId: OtherClinicId);
            var request = new BatchCreateDoctorScheduleRequest
            {
                Assignments = new List<DoctorRoomAssignment>
                {
                    new() { DoctorId = foreignDoctor.Id, RoomId = RoomId }
                },
                WorkDates = new List<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) },
                ShiftTypes = new List<string> { "MORNING" }
            };

            //Arrange 2
            SetupStaffClinic(new[] { BatchCreateDoctorScheduleMockData.GetStaffClinic() });
            SetupClinic(new[] { BatchCreateDoctorScheduleMockData.GetClinic() });
            SetupDoctor(new[] { foreignDoctor });
            SetupRoom(new[] { BatchCreateDoctorScheduleMockData.GetFacilityRoom() });
            SetupExistingSchedules(Array.Empty<DoctorSchedule>());

            //Act
            var result = await _sut.Process(ReceptionistUserId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Results.Should().BeEmpty();
            result.Data.TotalCreated.Should().Be(0);
            result.Data.TotalSkipped.Should().Be(0);
            _scheduleCommandRepoMock.Verify(x => x.CreateAsync(It.IsAny<DoctorSchedule>()), Times.Never);
        }

        /// <summary>
        /// TC-BCDS-11: Room belongs to a DIFFERENT clinic
        /// → ResolveActiveRoomInClinicAsync returns null → assignment is silently skipped.
        /// Covers: <c>ResolveActiveRoomInClinicAsync</c> empty-result branch.
        /// </summary>
        [Fact]
        public async Task Process_RoomFromForeignClinic_SilentlySkipsAssignment()
        {
            //Arrange 1
            var foreignRoom = BatchCreateDoctorScheduleMockData.GetFacilityRoom(
                id: BatchCreateDoctorScheduleMockData.OtherRoomId, clinicId: OtherClinicId, roomName: "Foreign Room");
            // Seed the in-memory DbSet so EF applies the predicate (clinicId != ClinicId → filtered out).
            _context.FacilityRooms.Add(foreignRoom);
            await _context.SaveChangesAsync();
            var request = new BatchCreateDoctorScheduleRequest
            {
                Assignments = new List<DoctorRoomAssignment>
                {
                    new() { DoctorId = DoctorProfileId, RoomId = foreignRoom.Id }
                },
                WorkDates = new List<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) },
                ShiftTypes = new List<string> { "MORNING" }
            };

            //Arrange 2
            SetupStaffClinic(new[] { BatchCreateDoctorScheduleMockData.GetStaffClinic() });
            SetupClinic(new[] { BatchCreateDoctorScheduleMockData.GetClinic() });
            SetupDoctor(new[] { BatchCreateDoctorScheduleMockData.GetDoctorProfile() });
            // Delegate to the in-memory DbContext so EF actually applies the predicate.
            _roomRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<FacilityRoom, bool>>>(), It.IsAny<bool>()))
                .Returns<Expression<Func<FacilityRoom, bool>>, bool>((expr, _) =>
                    _context.Set<FacilityRoom>().Where(expr));
            SetupExistingSchedules(Array.Empty<DoctorSchedule>());

            //Act
            var result = await _sut.Process(ReceptionistUserId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Results.Should().BeEmpty();
            _scheduleCommandRepoMock.Verify(x => x.CreateAsync(It.IsAny<DoctorSchedule>()), Times.Never);
        }

        /// <summary>
        /// TC-BCDS-12: Happy path — full valid seed with MORNING shift on tomorrow →
        /// schedule created, response has 1 Created + 0 Skipped; TotalCreated = 1,
        /// TotalSkipped = 0.
        /// Covers: <c>BuildBatchResultsAsync</c> happy path, <c>BuildDoctorResultAsync</c>
        /// happy path (no duplicate, within hours, no conflict), <c>CreateScheduleWithSlotsAsync</c>
        /// happy path, <c>CreateSuccessResponse</c>.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_CreatesScheduleAndSlots()
        {
            //Arrange 1
            var doctor = BatchCreateDoctorScheduleMockData.GetDoctorProfile();
            var request = new BatchCreateDoctorScheduleRequest
            {
                Assignments = new List<DoctorRoomAssignment>
                {
                    new() { DoctorId = doctor.Id, RoomId = RoomId }
                },
                WorkDates = new List<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) },
                ShiftTypes = new List<string> { "MORNING" }
            };

            //Arrange 2
            SetupHappyPathRepos(doctor: doctor);

            //Act
            var result = await _sut.Process(ReceptionistUserId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Results.Should().HaveCount(1);
            result.Data.TotalCreated.Should().Be(1);
            result.Data.TotalSkipped.Should().Be(0);
            var doctorResult = result.Data.Results[0];
            doctorResult.DoctorId.Should().Be(doctor.Id);
            doctorResult.DoctorName.Should().Be(doctor.User!.FullName);
            doctorResult.Created.Should().HaveCount(1);
            doctorResult.Skipped.Should().BeEmpty();
            doctorResult.Created[0].ShiftType.Should().Be(ShiftType.MORNING);
            doctorResult.Created[0].WorkDate.Should().Be(request.WorkDates[0]);
            doctorResult.Created[0].RoomName.Should().Be(BatchCreateDoctorScheduleMockData.GetFacilityRoom().RoomName);
        }

        /// <summary>
        /// TC-BCDS-13: Existing (date, shift) pair for the doctor → BuildDoctorResultAsync
        /// records a duplicate skip item; nothing is created.
        /// Covers: <c>existingPairs.Contains(...)</c> true branch.
        /// </summary>
        [Fact]
        public async Task Process_ExistingDuplicateSchedule_SkipsWithDuplicateReason()
        {
            //Arrange 1
            var doctor = BatchCreateDoctorScheduleMockData.GetDoctorProfile();
            var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
            var existingSchedule = new DoctorSchedule
            {
                Id = Guid.NewGuid(),
                DoctorId = doctor.Id,
                WorkDate = tomorrow.ToDateTime(TimeOnly.MinValue),
                ShiftType = ShiftType.MORNING,
                IsDeleted = false
            };
            var request = new BatchCreateDoctorScheduleRequest
            {
                Assignments = new List<DoctorRoomAssignment>
                {
                    new() { DoctorId = doctor.Id, RoomId = RoomId }
                },
                WorkDates = new List<DateOnly> { tomorrow },
                ShiftTypes = new List<string> { "MORNING" }
            };

            //Arrange 2
            SetupStaffClinic(new[] { BatchCreateDoctorScheduleMockData.GetStaffClinic() });
            SetupClinic(new[] { BatchCreateDoctorScheduleMockData.GetClinic() });
            SetupDoctor(new[] { doctor });
            SetupRoom(new[] { BatchCreateDoctorScheduleMockData.GetFacilityRoom() });
            SetupExistingSchedules(new[] { existingSchedule });

            //Act
            var result = await _sut.Process(ReceptionistUserId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Results.Should().HaveCount(1);
            result.Data.TotalCreated.Should().Be(0);
            result.Data.TotalSkipped.Should().Be(1);
            result.Data.Results[0].Skipped.Should().HaveCount(1);
            result.Data.Results[0].Skipped[0].Reason.Should().Contain("Đã tồn tại");
            _scheduleCommandRepoMock.Verify(x => x.CreateAsync(It.IsAny<DoctorSchedule>()), Times.Never);
        }

        /// <summary>
        /// TC-BCDS-14: Shift outside clinic hours (clinic open 12:00–13:00 with MORNING
        /// shift 08:00–12:00) → TryClipShiftToClinicHours returns false → skipped.
        /// Covers: <c>!TryClipShiftToClinicHours(...)</c> true branch.
        /// </summary>
        [Fact]
        public async Task Process_ShiftOutsideClinicHours_SkipsWithOutsideHoursReason()
        {
            //Arrange 1
            var narrowClinic = BatchCreateDoctorScheduleMockData.GetClinic(
                openTime: new TimeOnly(12, 0), closeTime: new TimeOnly(13, 0));
            var doctor = BatchCreateDoctorScheduleMockData.GetDoctorProfile();
            var request = new BatchCreateDoctorScheduleRequest
            {
                Assignments = new List<DoctorRoomAssignment>
                {
                    new() { DoctorId = doctor.Id, RoomId = RoomId }
                },
                WorkDates = new List<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) },
                ShiftTypes = new List<string> { "MORNING" }
            };

            //Arrange 2
            SetupStaffClinic(new[] { BatchCreateDoctorScheduleMockData.GetStaffClinic() });
            SetupClinic(new[] { narrowClinic });
            SetupDoctor(new[] { doctor });
            SetupRoom(new[] { BatchCreateDoctorScheduleMockData.GetFacilityRoom() });
            SetupExistingSchedules(Array.Empty<DoctorSchedule>());

            //Act
            var result = await _sut.Process(ReceptionistUserId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Results.Should().HaveCount(1);
            result.Data.TotalCreated.Should().Be(0);
            result.Data.TotalSkipped.Should().Be(1);
            result.Data.Results[0].Skipped.Should().HaveCount(1);
            result.Data.Results[0].Skipped[0].Reason.Should().Contain("nằm ngoài giờ hoạt động");
            _scheduleCommandRepoMock.Verify(x => x.CreateAsync(It.IsAny<DoctorSchedule>()), Times.Never);
        }

        /// <summary>
        /// TC-BCDS-15: Room already occupied by ANOTHER doctor on the same (date, shift)
        /// → FetchRoomOccupancyAsync returns the other doctor's ID →
        /// BuildDoctorResultAsync records a room-conflict skip.
        /// Covers: <c>roomOccupancy.TryGetValue(...)</c> true branch with
        /// <c>occupantDoctorId != doctorProfile.Id</c>.
        /// </summary>
        [Fact]
        public async Task Process_RoomOccupiedByAnotherDoctor_SkipsWithRoomConflictReason()
        {
            //Arrange 1
            var doctor = BatchCreateDoctorScheduleMockData.GetDoctorProfile();
            var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
            var room = BatchCreateDoctorScheduleMockData.GetFacilityRoom();
            // Seed the in-memory DbSet with another doctor's schedule for the same room/date/shift.
            // RoomId is a shadow property on DoctorSchedule; we set it via the EF API.
            var occupyingSchedule = new DoctorSchedule
            {
                Id = Guid.NewGuid(),
                DoctorId = OtherDoctorProfileId,
                WorkDate = tomorrow.ToDateTime(TimeOnly.MinValue),
                ShiftType = ShiftType.MORNING,
                IsDeleted = false
            };
            _context.DoctorSchedules.Add(occupyingSchedule);
            await _context.SaveChangesAsync();
            _context.Entry(occupyingSchedule).Property("RoomId").CurrentValue = room.Id;
            await _context.SaveChangesAsync();
            var request = new BatchCreateDoctorScheduleRequest
            {
                Assignments = new List<DoctorRoomAssignment>
                {
                    new() { DoctorId = doctor.Id, RoomId = room.Id }
                },
                WorkDates = new List<DateOnly> { tomorrow },
                ShiftTypes = new List<string> { "MORNING" }
            };

            //Arrange 2
            SetupStaffClinic(new[] { BatchCreateDoctorScheduleMockData.GetStaffClinic() });
            SetupClinic(new[] { BatchCreateDoctorScheduleMockData.GetClinic() });
            SetupDoctor(new[] { doctor });
            SetupRoom(new[] { room });
            SetupExistingSchedules(Array.Empty<DoctorSchedule>());

            //Act
            var result = await _sut.Process(ReceptionistUserId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Results.Should().HaveCount(1);
            result.Data.TotalCreated.Should().Be(0);
            result.Data.TotalSkipped.Should().Be(1);
            result.Data.Results[0].Skipped.Should().HaveCount(1);
            result.Data.Results[0].Skipped[0].Reason.Should().Contain("Phòng");
            result.Data.Results[0].Skipped[0].Reason.Should().Contain("bác sĩ khác");
            _scheduleCommandRepoMock.Verify(x => x.CreateAsync(It.IsAny<DoctorSchedule>()), Times.Never);
        }

        /// <summary>
        /// TC-BCDS-16: Two assignments for the same doctor using DIFFERENT rooms on the
        /// same date/shift — neither conflicts with the other (different rooms).
        /// Both schedules are created.
        /// Covers: <c>occupantDoctorId == doctorProfile.Id</c> is NOT in this path
        /// (since rooms differ); instead this covers the "two distinct assignments"
        /// branch and confirms the foreach loop processes every assignment.
        /// </summary>
        [Fact]
        public async Task Process_TwoAssignmentsForSameDoctorOnDifferentRooms_BothCreated()
        {
            //Arrange 1
            var doctor = BatchCreateDoctorScheduleMockData.GetDoctorProfile();
            var roomA = BatchCreateDoctorScheduleMockData.GetFacilityRoom(id: BatchCreateDoctorScheduleMockData.RoomId);
            var roomB = BatchCreateDoctorScheduleMockData.GetFacilityRoom(id: BatchCreateDoctorScheduleMockData.OtherRoomId, roomName: "Room 102");
            var request = new BatchCreateDoctorScheduleRequest
            {
                Assignments = new List<DoctorRoomAssignment>
                {
                    new() { DoctorId = doctor.Id, RoomId = roomA.Id },
                    new() { DoctorId = doctor.Id, RoomId = roomB.Id }
                },
                WorkDates = new List<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) },
                ShiftTypes = new List<string> { "MORNING" }
            };

            //Arrange 2
            SetupStaffClinic(new[] { BatchCreateDoctorScheduleMockData.GetStaffClinic() });
            SetupClinic(new[] { BatchCreateDoctorScheduleMockData.GetClinic() });
            SetupDoctor(new[] { doctor });
            SetupRoom(new[] { roomA, roomB });
            SetupExistingSchedules(Array.Empty<DoctorSchedule>());

            //Act
            var result = await _sut.Process(ReceptionistUserId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Results.Should().HaveCount(2);
            result.Data.TotalCreated.Should().Be(2);
            result.Data.TotalSkipped.Should().Be(0);
        }

        /// <summary>
        /// TC-BCDS-21: Two assignments for the SAME doctor + SAME room + same date/shift.
        /// The first assignment creates a schedule and updates roomOccupancy. The second
        /// assignment sees the room as already occupied by the SAME doctor (its own prior
        /// assignment) → not skipped, schedule is also created.
        /// Covers: <c>roomOccupancy.TryGetValue(...)</c> true branch with
        /// <c>occupantDoctorId == doctorProfile.Id</c> (same doctor no-conflict path).
        /// </summary>
        [Fact]
        public async Task Process_TwoAssignmentsSameDoctorSameRoom_BothCreatedDueToSameDoctor()
        {
            //Arrange 1
            var doctor = BatchCreateDoctorScheduleMockData.GetDoctorProfile();
            var room = BatchCreateDoctorScheduleMockData.GetFacilityRoom();
            var request = new BatchCreateDoctorScheduleRequest
            {
                Assignments = new List<DoctorRoomAssignment>
                {
                    new() { DoctorId = doctor.Id, RoomId = room.Id },
                    new() { DoctorId = doctor.Id, RoomId = room.Id }
                },
                WorkDates = new List<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) },
                ShiftTypes = new List<string> { "MORNING" }
            };

            //Arrange 2
            SetupStaffClinic(new[] { BatchCreateDoctorScheduleMockData.GetStaffClinic() });
            SetupClinic(new[] { BatchCreateDoctorScheduleMockData.GetClinic() });
            SetupDoctor(new[] { doctor });
            SetupRoom(new[] { room });
            SetupExistingSchedules(Array.Empty<DoctorSchedule>());

            //Act
            var result = await _sut.Process(ReceptionistUserId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Results.Should().HaveCount(2);
            // Both schedules created (no room conflict since same doctor)
            result.Data.TotalCreated.Should().Be(2);
            result.Data.TotalSkipped.Should().Be(0);
            _scheduleCommandRepoMock.Verify(x => x.CreateAsync(It.IsAny<DoctorSchedule>()), Times.Exactly(2));
        }

        /// <summary>
        /// TC-BCDS-17: Doctor has <c>User == null</c> → <c>DoctorName</c> defaults to
        /// <c>doctorProfile.Id.ToString()</c>.
        /// Covers: <c>doctorProfile.User?.FullName ?? doctorProfile.Id.ToString()</c>
        /// false branch (User is null → fall back to Id).
        /// </summary>
        [Fact]
        public async Task Process_DoctorWithNullUser_UsesDoctorIdAsName()
        {
            //Arrange 1
            var doctor = new DoctorProfile
            {
                Id = DoctorProfileId,
                UserId = Guid.NewGuid(),
                ClinicId = ClinicId,
                IsActive = true,
                User = null!
            };
            var request = BatchCreateDoctorScheduleMockData.GetValidRequest();

            //Arrange 2
            SetupHappyPathRepos(doctor: doctor);

            //Act
            var result = await _sut.Process(ReceptionistUserId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Results.Should().HaveCount(1);
            result.Data.Results[0].DoctorId.Should().Be(doctor.Id);
            result.Data.Results[0].DoctorName.Should().Be(doctor.Id.ToString());
        }

        /// <summary>
        /// TC-BCDS-18: Happy path — verifies CreateAsync is invoked on the schedule command
        /// repo exactly once and on the slot command repo for each generated 30-min slot
        /// (Morning shift 08:00–12:00 with clinic 08:00–20:00 → 8 slots).
        /// Covers: <c>CreateScheduleWithSlotsAsync</c> CreateAsync + SaveChangesAsync
        /// and the inner slot-generation loop.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_InvokesScheduleAndSlotCreate()
        {
            //Arrange 1
            var doctor = BatchCreateDoctorScheduleMockData.GetDoctorProfile();
            var request = BatchCreateDoctorScheduleMockData.GetValidRequest();

            //Arrange 2
            SetupHappyPathRepos(doctor: doctor);

            //Act
            var result = await _sut.Process(ReceptionistUserId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            _scheduleCommandRepoMock.Verify(x => x.CreateAsync(It.IsAny<DoctorSchedule>()), Times.Once);
            _scheduleCommandRepoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
            // Morning 08:00–12:00 = 8 slots × 30 minutes = 8 CreateAsync calls
            _slotCommandRepoMock.Verify(x => x.CreateAsync(It.IsAny<TimeSlot>()), Times.Exactly(8));
            _slotCommandRepoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        /// <summary>
        /// TC-BCDS-19: Happy path — response <c>CodeMessage</c> is <c>APP_MESSAGE_2000</c>
        /// and <c>Meta</c> is null because <c>CreateSuccessResponse</c> uses the 2-arg
        /// <c>Success(codeMessage, data)</c> overload (no meta).
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_ResponseCodeMessage2000()
        {
            //Arrange 1
            var request = BatchCreateDoctorScheduleMockData.GetValidRequest();

            //Arrange 2
            SetupHappyPathRepos();

            //Act
            var result = await _sut.Process(ReceptionistUserId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Meta.Should().BeNull();
        }

        /// <summary>
        /// TC-BCDS-20: GetDistinctRoomIds is a private static helper. Invoke it via
        /// reflection: 3 assignments with room IDs [A, B, A] → returns 2 distinct entries
        /// (A and B).
        /// Covers: <c>GetDistinctRoomIds</c> Distinct() + ToList() chain.
        /// </summary>
        [Fact]
        public void GetDistinctRoomIds_DuplicateRoomIds_ReturnsDistinct()
        {
            //Arrange 1
            var method = typeof(BatchCreateDoctorScheduleService).GetMethod(
                "GetDistinctRoomIds",
                BindingFlags.NonPublic | BindingFlags.Static);
            method.Should().NotBeNull();
            var assignments = new List<DoctorRoomAssignment>
            {
                new() { DoctorId = Guid.NewGuid(), RoomId = BatchCreateDoctorScheduleMockData.RoomId },
                new() { DoctorId = Guid.NewGuid(), RoomId = BatchCreateDoctorScheduleMockData.OtherRoomId },
                new() { DoctorId = Guid.NewGuid(), RoomId = BatchCreateDoctorScheduleMockData.RoomId } // duplicate
            };

            //Arrange 2

            //Act
            var result = (List<Guid>)method!.Invoke(null, new object[] { assignments })!;

            //Assert
            result.Should().HaveCount(2);
            result.Should().Contain(BatchCreateDoctorScheduleMockData.RoomId);
            result.Should().Contain(BatchCreateDoctorScheduleMockData.OtherRoomId);
        }
    }
}