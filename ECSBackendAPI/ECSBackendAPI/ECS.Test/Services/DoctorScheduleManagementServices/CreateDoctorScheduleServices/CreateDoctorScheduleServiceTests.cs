using System.Linq.Expressions;
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

namespace ECS.Test.Services.DoctorScheduleManagementServices.CreateDoctorScheduleServices
{
    /// <summary>
    /// Unit tests for <see cref="CreateDoctorScheduleService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line AND branch coverage on <c>CreateDoctorScheduleService.cs</c>.
    /// </summary>
    /// <remarks>
    /// The service uses a real <see cref="AppDbContext"/> (in-memory) because
    /// <c>CreateScheduleWithSlotsAsync</c> writes a shadow property via
    /// <c>_dbContext.Entry(schedule).Property("RoomId").CurrentValue = roomId</c>.
    /// </remarks>
    public class CreateDoctorScheduleServiceTests : IDisposable
    {
        private static readonly Guid ReceptionistUserId = CreateDoctorScheduleMockData.ReceptionistUserId;
        private static readonly Guid ClinicId = CreateDoctorScheduleMockData.ClinicId;
        private static readonly Guid DoctorProfileId = CreateDoctorScheduleMockData.DoctorProfileId;
        private static readonly Guid RoomId = CreateDoctorScheduleMockData.RoomId;

        private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext>> _roomRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<Clinic, Guid, AppDbContext>> _clinicRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<DoctorSchedule, Guid, AppDbContext>> _scheduleQueryRepoMock = new();
        private readonly Mock<IRepositoryBaseAsync<DoctorSchedule, Guid, AppDbContext>> _scheduleCommandRepoMock = new();
        private readonly Mock<IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext>> _slotCommandRepoMock = new();
        private readonly AppDbContext _context;
        private readonly CreateDoctorScheduleService _sut;

        public CreateDoctorScheduleServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);
            _sut = new CreateDoctorScheduleService(
                _doctorRepoMock.Object,
                _roomRepoMock.Object,
                _staffClinicRepoMock.Object,
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

        private void SetupEmptyExistingSchedules()
        {
            _scheduleQueryRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<DoctorSchedule, bool>>>(), It.IsAny<bool>()))
                .Returns(new List<DoctorSchedule>().BuildMockDbSet<DoctorSchedule>().Object);
        }

        private void SetupHappyPathRepos(
            Clinic? clinic = null,
            DoctorProfile? doctor = null,
            FacilityRoom? room = null)
        {
            SetupStaffClinic(new[] { CreateDoctorScheduleMockData.GetStaffClinic() });
            SetupClinic(new[] { clinic ?? CreateDoctorScheduleMockData.GetClinic() });
            SetupDoctor(new[] { doctor ?? CreateDoctorScheduleMockData.GetDoctorProfile() });
            SetupRoom(new[] { room ?? CreateDoctorScheduleMockData.GetFacilityRoom() });
            SetupExistingSchedules(Array.Empty<DoctorSchedule>());
        }

        // ==================================================================
        // ==================== VALIDATION TESTS =============================
        // ==================================================================

        /// <summary>
        /// TC-01: WorkDates is null → ValidateWorkDates throws ArgumentException(APP_MESSAGE_4001).
        /// </summary>
        [Fact]
        public async Task Process_NullWorkDates_ThrowsArgumentException()
        {
            //Arrange 1
            var request = new CreateDoctorScheduleRequest
            {
                WorkDates = null!,
                ShiftTypes = new List<string> { "MORNING" },
                RoomId = RoomId
            };

            //Arrange 2

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, request);

            //Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4001.ToString());
        }

        /// <summary>
        /// TC-02: WorkDates is empty → ValidateWorkDates throws ArgumentException(APP_MESSAGE_4001).
        /// </summary>
        [Fact]
        public async Task Process_EmptyWorkDates_ThrowsArgumentException()
        {
            //Arrange 1
            var request = new CreateDoctorScheduleRequest
            {
                WorkDates = new List<DateOnly>(),
                ShiftTypes = new List<string> { "MORNING" },
                RoomId = RoomId
            };

            //Arrange 2

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, request);

            //Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4001.ToString());
        }

        /// <summary>
        /// TC-03: WorkDates contains past date → ValidateWorkDates throws ArgumentException(APP_MESSAGE_4001).
        /// </summary>
        [Fact]
        public async Task Process_PastWorkDate_ThrowsArgumentException()
        {
            //Arrange 1
            var request = new CreateDoctorScheduleRequest
            {
                WorkDates = new List<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)) },
                ShiftTypes = new List<string> { "MORNING" },
                RoomId = RoomId
            };

            //Arrange 2

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, request);

            //Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4001.ToString());
        }

        /// <summary>
        /// TC-04: ShiftTypes is null → ValidateShiftTypes throws ArgumentException(APP_MESSAGE_4001).
        /// </summary>
        [Fact]
        public async Task Process_NullShiftTypes_ThrowsArgumentException()
        {
            //Arrange 1
            var request = new CreateDoctorScheduleRequest
            {
                WorkDates = new List<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) },
                ShiftTypes = null!,
                RoomId = RoomId
            };

            //Arrange 2

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, request);

            //Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4001.ToString());
        }

        /// <summary>
        /// TC-05: ShiftTypes is empty → ValidateShiftTypes throws ArgumentException(APP_MESSAGE_4001).
        /// </summary>
        [Fact]
        public async Task Process_EmptyShiftTypes_ThrowsArgumentException()
        {
            //Arrange 1
            var request = new CreateDoctorScheduleRequest
            {
                WorkDates = new List<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) },
                ShiftTypes = new List<string>(),
                RoomId = RoomId
            };

            //Arrange 2

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, request);

            //Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4001.ToString());
        }

        /// <summary>
        /// TC-06: All shift type strings are invalid → ParseShiftTypes returns empty.
        /// BuildScheduleResultsAsync nested loop executes 0 times (empty distinctShifts).
        /// </summary>
        [Fact]
        public async Task Process_AllInvalidShiftTypeStrings_ReturnsSuccessWithZeroCreated()
        {
            //Arrange 1
            var request = new CreateDoctorScheduleRequest
            {
                WorkDates = new List<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) },
                ShiftTypes = new List<string> { "INVALID", "INVALID2" },
                RoomId = RoomId
            };

            //Arrange 2
            SetupHappyPathRepos();

            //Act
            var result = await _sut.Process(ReceptionistUserId, DoctorProfileId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Created.Should().BeEmpty();
            result.Data.Skipped.Should().BeEmpty();
        }

        // ==================================================================
        // ================== AUTHORIZATION TESTS ============================
        // ==================================================================

        /// <summary>
        /// TC-07: Receptionist's StaffClinic not found → ResolveReceptionistClinicIdAsync throws KeyNotFoundException.
        /// </summary>
        [Fact]
        public async Task Process_ReceptionistStaffClinicNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = CreateDoctorScheduleMockData.GetValidRequest();

            //Arrange 2
            SetupStaffClinic(Array.Empty<StaffClinic>());

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4008.ToString());
            _clinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Clinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>
        /// TC-08: Doctor profile not found → ResolveActiveDoctorProfileAsync throws KeyNotFoundException.
        /// </summary>
        [Fact]
        public async Task Process_DoctorNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = CreateDoctorScheduleMockData.GetValidRequest();

            //Arrange 2
            SetupStaffClinic(new[] { CreateDoctorScheduleMockData.GetStaffClinic() });
            SetupDoctor(Array.Empty<DoctorProfile>());
            SetupExistingSchedules(Array.Empty<DoctorSchedule>());

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4008.ToString());
        }

        /// <summary>
        /// TC-09: Doctor inactive → returns empty (IsActive filter not applied by mock).
        /// Note: Simple mock cannot apply IsActive filter, so inactive = not found scenario.
        /// </summary>
        [Fact]
        public async Task Process_DoctorInactive_ReturnsEmpty_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = CreateDoctorScheduleMockData.GetValidRequest();
            // Don't include inactive doctor in mock - will return empty

            //Arrange 2
            SetupStaffClinic(new[] { CreateDoctorScheduleMockData.GetStaffClinic() });
            SetupDoctor(Array.Empty<DoctorProfile>()); // Empty = not found
            SetupExistingSchedules(Array.Empty<DoctorSchedule>());

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4008.ToString());
        }

        /// <summary>
        /// TC-10: Doctor belongs to DIFFERENT clinic → EnsureSameClinic throws UnauthorizedAccessException.
        /// </summary>
        [Fact]
        public async Task Process_DifferentClinic_ThrowsUnauthorizedAccessException()
        {
            //Arrange 1
            var request = CreateDoctorScheduleMockData.GetValidRequest();
            var foreignDoctor = CreateDoctorScheduleMockData.GetDoctorProfile(clinicId: CreateDoctorScheduleMockData.OtherClinicId);

            //Arrange 2
            SetupStaffClinic(new[] { CreateDoctorScheduleMockData.GetStaffClinic() });
            SetupClinic(new[] { CreateDoctorScheduleMockData.GetClinic() });
            SetupDoctor(new[] { foreignDoctor });

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, request);

            //Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4008.ToString());
        }

        /// <summary>
        /// TC-11: Room not found → ResolveActiveRoomAsync throws KeyNotFoundException(APP_MESSAGE_4004).
        /// </summary>
        [Fact]
        public async Task Process_RoomNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = CreateDoctorScheduleMockData.GetValidRequest();

            //Arrange 2
            SetupStaffClinic(new[] { CreateDoctorScheduleMockData.GetStaffClinic() });
            SetupClinic(new[] { CreateDoctorScheduleMockData.GetClinic() });
            SetupDoctor(new[] { CreateDoctorScheduleMockData.GetDoctorProfile() });
            SetupRoom(Array.Empty<FacilityRoom>());
            SetupExistingSchedules(Array.Empty<DoctorSchedule>());

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4004.ToString());
        }

        /// <summary>
        /// TC-12: Room inactive → returns empty (IsActive filter not applied by mock).
        /// Note: Simple mock cannot apply IsActive filter, so inactive = not found scenario.
        /// </summary>
        [Fact]
        public async Task Process_RoomInactive_ReturnsEmpty_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = CreateDoctorScheduleMockData.GetValidRequest();
            // Don't include inactive room in mock - will return empty

            //Arrange 2
            SetupStaffClinic(new[] { CreateDoctorScheduleMockData.GetStaffClinic() });
            SetupClinic(new[] { CreateDoctorScheduleMockData.GetClinic() });
            SetupDoctor(new[] { CreateDoctorScheduleMockData.GetDoctorProfile() });
            SetupRoom(Array.Empty<FacilityRoom>()); // Empty = not found
            SetupExistingSchedules(Array.Empty<DoctorSchedule>());

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4004.ToString());
        }

        /// <summary>
        /// TC-13: Room belongs to DIFFERENT clinic → returns empty (ClinicId filter not applied by mock).
        /// Note: Simple mock cannot apply ClinicId filter, so foreign clinic = not found scenario.
        /// </summary>
        [Fact]
        public async Task Process_RoomFromDifferentClinic_ReturnsEmpty_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = CreateDoctorScheduleMockData.GetValidRequest();
            // Don't include foreign room in mock - will return empty

            //Arrange 2
            SetupStaffClinic(new[] { CreateDoctorScheduleMockData.GetStaffClinic() });
            SetupClinic(new[] { CreateDoctorScheduleMockData.GetClinic() });
            SetupDoctor(new[] { CreateDoctorScheduleMockData.GetDoctorProfile() });
            SetupRoom(Array.Empty<FacilityRoom>()); // Empty = not found
            SetupExistingSchedules(Array.Empty<DoctorSchedule>());

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4004.ToString());
        }

        /// <summary>
        /// TC-14: Clinic not found → ResolveActiveClinicAsync throws KeyNotFoundException.
        /// </summary>
        [Fact]
        public async Task Process_ClinicNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = CreateDoctorScheduleMockData.GetValidRequest();

            //Arrange 2
            SetupStaffClinic(new[] { CreateDoctorScheduleMockData.GetStaffClinic() });
            SetupClinic(Array.Empty<Clinic>());
            SetupDoctor(new[] { CreateDoctorScheduleMockData.GetDoctorProfile() });
            SetupRoom(new[] { CreateDoctorScheduleMockData.GetFacilityRoom() });
            SetupExistingSchedules(Array.Empty<DoctorSchedule>());

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4008.ToString());
        }

        /// <summary>
        /// TC-15: Clinic inactive → returns empty (IsActive filter not applied by mock).
        /// Note: Simple mock cannot apply IsActive filter, so inactive = not found scenario.
        /// </summary>
        [Fact]
        public async Task Process_ClinicInactive_ReturnsEmpty_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = CreateDoctorScheduleMockData.GetValidRequest();
            // Don't include inactive clinic in mock - will return empty

            //Arrange 2
            SetupStaffClinic(new[] { CreateDoctorScheduleMockData.GetStaffClinic() });
            SetupClinic(Array.Empty<Clinic>()); // Empty = not found
            SetupDoctor(new[] { CreateDoctorScheduleMockData.GetDoctorProfile() });
            SetupRoom(new[] { CreateDoctorScheduleMockData.GetFacilityRoom() });
            SetupExistingSchedules(Array.Empty<DoctorSchedule>());

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4008.ToString());
        }

        // ==================================================================
        // ================ SKIP SCENARIO TESTS =============================
        // ==================================================================

        /// <summary>
        /// TC-16: Existing schedule for same (doctor, date, shift) → duplicate skip.
        /// </summary>
        [Fact]
        public async Task Process_ExistingDuplicateSchedule_SkipsWithDuplicateReason()
        {
            //Arrange 1
            var doctor = CreateDoctorScheduleMockData.GetDoctorProfile();
            var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
            var existingSchedule = new DoctorSchedule
            {
                Id = Guid.NewGuid(),
                DoctorId = doctor.Id,
                WorkDate = tomorrow.ToDateTime(TimeOnly.MinValue),
                ShiftType = ShiftType.MORNING,
                IsDeleted = false
            };
            var request = new CreateDoctorScheduleRequest
            {
                WorkDates = new List<DateOnly> { tomorrow },
                ShiftTypes = new List<string> { "MORNING" },
                RoomId = RoomId
            };

            //Arrange 2
            SetupStaffClinic(new[] { CreateDoctorScheduleMockData.GetStaffClinic() });
            SetupClinic(new[] { CreateDoctorScheduleMockData.GetClinic() });
            SetupDoctor(new[] { doctor });
            SetupRoom(new[] { CreateDoctorScheduleMockData.GetFacilityRoom() });
            SetupExistingSchedules(new[] { existingSchedule });

            //Act
            var result = await _sut.Process(ReceptionistUserId, doctor.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Created.Should().BeEmpty();
            result.Data.Skipped.Should().HaveCount(1);
            result.Data.Skipped[0].Reason.Should().Contain("Đã tồn tại");
            _scheduleCommandRepoMock.Verify(x => x.CreateAsync(It.IsAny<DoctorSchedule>()), Times.Never);
        }

        /// <summary>
        /// TC-17: Shift outside clinic hours (clinic opens at 12:00, MORNING shift is 8-12).
        /// TryClipShiftToClinicHours returns false → skip.
        /// </summary>
        [Fact]
        public async Task Process_ShiftOutsideClinicHours_SkipsWithOutsideHoursReason()
        {
            //Arrange 1
            var narrowClinic = CreateDoctorScheduleMockData.GetClinic(
                openTime: new TimeOnly(12, 0), closeTime: new TimeOnly(13, 0));
            var doctor = CreateDoctorScheduleMockData.GetDoctorProfile();
            var request = new CreateDoctorScheduleRequest
            {
                WorkDates = new List<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) },
                ShiftTypes = new List<string> { "MORNING" },
                RoomId = RoomId
            };

            //Arrange 2
            SetupStaffClinic(new[] { CreateDoctorScheduleMockData.GetStaffClinic() });
            SetupClinic(new[] { narrowClinic });
            SetupDoctor(new[] { doctor });
            SetupRoom(new[] { CreateDoctorScheduleMockData.GetFacilityRoom() });
            SetupExistingSchedules(Array.Empty<DoctorSchedule>());

            //Act
            var result = await _sut.Process(ReceptionistUserId, doctor.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Created.Should().BeEmpty();
            result.Data.Skipped.Should().HaveCount(1);
            result.Data.Skipped[0].Reason.Should().Contain("nằm ngoài giờ hoạt động");
            _scheduleCommandRepoMock.Verify(x => x.CreateAsync(It.IsAny<DoctorSchedule>()), Times.Never);
        }

        // ==================================================================
        // ==================== HAPPY PATH TESTS ===========================
        // ==================================================================

        /// <summary>
        /// TC-18: Happy path - MORNING shift within clinic hours → creates schedule and 8 slots.
        /// </summary>
        [Fact]
        public async Task Process_HappyPathMorningShift_CreatesScheduleAnd8Slots()
        {
            //Arrange 1
            var doctor = CreateDoctorScheduleMockData.GetDoctorProfile();
            var request = new CreateDoctorScheduleRequest
            {
                WorkDates = new List<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) },
                ShiftTypes = new List<string> { "MORNING" },
                RoomId = RoomId
            };

            //Arrange 2
            SetupHappyPathRepos(doctor: doctor);

            //Act
            var result = await _sut.Process(ReceptionistUserId, doctor.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Created.Should().HaveCount(1);
            result.Data.Created[0].ShiftType.Should().Be(ShiftType.MORNING);
            result.Data.Created[0].SlotCount.Should().Be(8); // 8:00-12:00 = 4 hours = 8 x 30min slots
            result.Data.Skipped.Should().BeEmpty();
        }

        /// <summary>
        /// TC-19: Happy path - AFTERNOON shift → creates 10 slots.
        /// </summary>
        [Fact]
        public async Task Process_HappyPathAfternoonShift_CreatesScheduleAnd10Slots()
        {
            //Arrange 1
            var doctor = CreateDoctorScheduleMockData.GetDoctorProfile();
            var request = new CreateDoctorScheduleRequest
            {
                WorkDates = new List<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) },
                ShiftTypes = new List<string> { "AFTERNOON" },
                RoomId = RoomId
            };

            //Arrange 2
            SetupHappyPathRepos(doctor: doctor);

            //Act
            var result = await _sut.Process(ReceptionistUserId, doctor.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Created.Should().HaveCount(1);
            result.Data.Created[0].ShiftType.Should().Be(ShiftType.AFTERNOON);
            result.Data.Created[0].SlotCount.Should().Be(10); // 12:00-17:00 = 5 hours = 10 x 30min slots
            result.Data.Skipped.Should().BeEmpty();
        }

        /// <summary>
        /// TC-20: Happy path - EVENING shift → creates 6 slots.
        /// </summary>
        [Fact]
        public async Task Process_HappyPathEveningShift_CreatesScheduleAnd6Slots()
        {
            //Arrange 1
            var doctor = CreateDoctorScheduleMockData.GetDoctorProfile();
            var request = new CreateDoctorScheduleRequest
            {
                WorkDates = new List<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) },
                ShiftTypes = new List<string> { "EVENING" },
                RoomId = RoomId
            };

            //Arrange 2
            SetupHappyPathRepos(doctor: doctor);

            //Act
            var result = await _sut.Process(ReceptionistUserId, doctor.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Created.Should().HaveCount(1);
            result.Data.Created[0].ShiftType.Should().Be(ShiftType.EVENING);
            result.Data.Created[0].SlotCount.Should().Be(6); // 17:00-20:00 = 3 hours = 6 x 30min slots
            result.Data.Skipped.Should().BeEmpty();
        }

        /// <summary>
        /// TC-21: Multiple dates and shifts → correct count in nested loop.
        /// </summary>
        [Fact]
        public async Task Process_MultipleDatesAndShifts_CreatesCorrectCount()
        {
            //Arrange 1
            var doctor = CreateDoctorScheduleMockData.GetDoctorProfile();
            var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
            var dayAfter = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
            var request = new CreateDoctorScheduleRequest
            {
                WorkDates = new List<DateOnly> { tomorrow, dayAfter },
                ShiftTypes = new List<string> { "MORNING", "AFTERNOON" },
                RoomId = RoomId
            };

            //Arrange 2
            SetupHappyPathRepos(doctor: doctor);

            //Act
            var result = await _sut.Process(ReceptionistUserId, doctor.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            // 2 dates x 2 shifts = 4 created
            result.Data!.Created.Should().HaveCount(4);
            result.Data.Skipped.Should().BeEmpty();
        }

        /// <summary>
        /// TC-22: Response CodeMessage is APP_MESSAGE_2000 and Meta is null.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_ResponseCodeMessage2000AndMetaNull()
        {
            //Arrange 1
            var request = CreateDoctorScheduleMockData.GetValidRequest();

            //Arrange 2
            SetupHappyPathRepos();

            //Act
            var result = await _sut.Process(ReceptionistUserId, DoctorProfileId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Meta.Should().BeNull();
        }

        // ==================================================================
        // ================ EDGE CASE TESTS =================================
        // ==================================================================

        /// <summary>
        /// TC-23: Duplicate dates in WorkDates → Distinct() removes them.
        /// </summary>
        [Fact]
        public async Task Process_DuplicateDatesInWorkDates_RemovesDuplicates()
        {
            //Arrange 1
            var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
            var doctor = CreateDoctorScheduleMockData.GetDoctorProfile();
            var request = new CreateDoctorScheduleRequest
            {
                WorkDates = new List<DateOnly> { tomorrow, tomorrow, tomorrow }, // 3 duplicates
                ShiftTypes = new List<string> { "MORNING" },
                RoomId = RoomId
            };

            //Arrange 2
            SetupHappyPathRepos(doctor: doctor);

            //Act
            var result = await _sut.Process(ReceptionistUserId, doctor.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            // Should only create 1 schedule, not 3
            result.Data!.Created.Should().HaveCount(1);
        }

        /// <summary>
        /// TC-24: Duplicate shift types in request → ParseShiftTypes Distinct() removes them.
        /// </summary>
        [Fact]
        public async Task Process_DuplicateShiftTypesInRequest_RemovesDuplicates()
        {
            //Arrange 1
            var doctor = CreateDoctorScheduleMockData.GetDoctorProfile();
            var request = new CreateDoctorScheduleRequest
            {
                WorkDates = new List<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) },
                ShiftTypes = new List<string> { "MORNING", "MORNING", "MORNING" }, // 3 duplicates
                RoomId = RoomId
            };

            //Arrange 2
            SetupHappyPathRepos(doctor: doctor);

            //Act
            var result = await _sut.Process(ReceptionistUserId, doctor.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            // Should only create 1 schedule, not 3
            result.Data!.Created.Should().HaveCount(1);
        }

        /// <summary>
        /// TC-25: Mix of valid and invalid shift type strings → filters out invalid ones.
        /// </summary>
        [Fact]
        public async Task Process_MixValidAndInvalidShiftTypes_FiltersInvalid()
        {
            //Arrange 1
            var doctor = CreateDoctorScheduleMockData.GetDoctorProfile();
            var request = new CreateDoctorScheduleRequest
            {
                WorkDates = new List<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) },
                ShiftTypes = new List<string> { "INVALID", "MORNING", "INVALID2", "AFTERNOON" },
                RoomId = RoomId
            };

            //Arrange 2
            SetupHappyPathRepos(doctor: doctor);

            //Act
            var result = await _sut.Process(ReceptionistUserId, doctor.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            // Should create 2 schedules (MORNING + AFTERNOON), INVALID strings filtered
            result.Data!.Created.Should().HaveCount(2);
        }

        // ==================================================================
        // ================ REPOSITORY VERIFICATION TESTS ===================
        // ==================================================================

        /// <summary>
        /// TC-26: Verify CreateAsync is called correctly on schedule and slot repos.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_InvokesScheduleAndSlotCreateCorrectly()
        {
            //Arrange 1
            var doctor = CreateDoctorScheduleMockData.GetDoctorProfile();
            var request = CreateDoctorScheduleMockData.GetValidRequest();

            //Arrange 2
            SetupHappyPathRepos(doctor: doctor);

            //Act
            var result = await _sut.Process(ReceptionistUserId, doctor.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            _scheduleCommandRepoMock.Verify(x => x.CreateAsync(It.IsAny<DoctorSchedule>()), Times.Once);
            _scheduleCommandRepoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
            // MORNING shift 08:00-12:00 = 8 slots
            _slotCommandRepoMock.Verify(x => x.CreateAsync(It.IsAny<TimeSlot>()), Times.Exactly(8));
            _slotCommandRepoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        /// <summary>
        /// TC-27: Response includes correct RoomName from the room.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_ResponseIncludesCorrectRoomName()
        {
            //Arrange 1
            var doctor = CreateDoctorScheduleMockData.GetDoctorProfile();
            var room = CreateDoctorScheduleMockData.GetFacilityRoom(roomName: "Examination Room A");
            var request = CreateDoctorScheduleMockData.GetValidRequest(roomId: room.Id);

            //Arrange 2
            SetupHappyPathRepos(doctor: doctor, room: room);

            //Act
            var result = await _sut.Process(ReceptionistUserId, doctor.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Created[0].RoomName.Should().Be("Examination Room A");
        }

        /// <summary>
        /// TC-28: Existing schedule for one shift, new for another → mixed created/skipped.
        /// </summary>
        [Fact]
        public async Task Process_MixedExistingAndNewSchedules_MixedCreatedAndSkipped()
        {
            //Arrange 1
            var doctor = CreateDoctorScheduleMockData.GetDoctorProfile();
            var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
            var existingSchedule = new DoctorSchedule
            {
                Id = Guid.NewGuid(),
                DoctorId = doctor.Id,
                WorkDate = tomorrow.ToDateTime(TimeOnly.MinValue),
                ShiftType = ShiftType.MORNING,
                IsDeleted = false
            };
            var request = new CreateDoctorScheduleRequest
            {
                WorkDates = new List<DateOnly> { tomorrow },
                ShiftTypes = new List<string> { "MORNING", "AFTERNOON" },
                RoomId = RoomId
            };

            //Arrange 2
            SetupStaffClinic(new[] { CreateDoctorScheduleMockData.GetStaffClinic() });
            SetupClinic(new[] { CreateDoctorScheduleMockData.GetClinic() });
            SetupDoctor(new[] { doctor });
            SetupRoom(new[] { CreateDoctorScheduleMockData.GetFacilityRoom() });
            SetupExistingSchedules(new[] { existingSchedule });

            //Act
            var result = await _sut.Process(ReceptionistUserId, doctor.Id, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Created.Should().HaveCount(1); // AFTERNOON created
            result.Data.Created[0].ShiftType.Should().Be(ShiftType.AFTERNOON);
            result.Data.Skipped.Should().HaveCount(1); // MORNING skipped (duplicate)
            result.Data.Skipped[0].Reason.Should().Contain("Đã tồn tại");
        }
    }
}
