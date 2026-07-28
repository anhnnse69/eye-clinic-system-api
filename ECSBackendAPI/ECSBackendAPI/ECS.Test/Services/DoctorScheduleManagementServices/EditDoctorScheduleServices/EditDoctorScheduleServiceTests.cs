using System.Linq.Expressions;
using System.Reflection;
using ECS.Application.Services.DoctorScheduleManagementServices.EditDoctorScheduleServices;
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

namespace ECS.Test.Services.DoctorScheduleManagementServices.EditDoctorScheduleServices
{
    /// <summary>
    /// Unit tests for <see cref="EditDoctorScheduleService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line AND branch coverage on <c>EditDoctorScheduleService.cs</c>.
    /// </summary>
    /// <remarks>
    /// The service uses a real <see cref="AppDbContext"/> (in-memory) because
    /// multiple methods access the database directly:
    /// - ResolveOwnedScheduleAsync: _dbContext.Set&lt;DoctorSchedule&gt;().Include(...)
    /// - GetCurrentRoomId: _dbContext.Entry(schedule).Property("RoomId")
    /// - EnsureNoRoomConflictAsync: _dbContext.Set&lt;DoctorSchedule&gt;().Where(...)
    /// - ApplyChanges: _dbContext.Entry(schedule).Property("RoomId")
    /// </remarks>
    public class EditDoctorScheduleServiceTests : IDisposable
    {
        private static readonly Guid ReceptionistUserId = EditDoctorScheduleMockData.ReceptionistUserId;
        private static readonly Guid ClinicId = EditDoctorScheduleMockData.ClinicId;
        private static readonly Guid OtherClinicId = EditDoctorScheduleMockData.OtherClinicId;
        private static readonly Guid DoctorProfileId = EditDoctorScheduleMockData.DoctorProfileId;
        private static readonly Guid ScheduleId = EditDoctorScheduleMockData.ScheduleId;
        private static readonly Guid RoomId = EditDoctorScheduleMockData.RoomId;
        private static readonly Guid NewRoomId = EditDoctorScheduleMockData.NewRoomId;

        private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext>> _roomRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<DoctorSchedule, Guid, AppDbContext>> _scheduleQueryRepoMock = new();
        private readonly AppDbContext _context;
        private readonly EditDoctorScheduleService _sut;

        public EditDoctorScheduleServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);
            _sut = new EditDoctorScheduleService(
                _doctorRepoMock.Object,
                _roomRepoMock.Object,
                _staffClinicRepoMock.Object,
                _scheduleQueryRepoMock.Object,
                _context);
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

        private void SetupScheduleQueryRepo(IEnumerable<DoctorSchedule> schedules)
        {
            var list = schedules.ToList();
            _scheduleQueryRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<DoctorSchedule, bool>>>(), It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<DoctorSchedule>().Object);
        }

        private void SetupScheduleInContext(DoctorSchedule schedule, Guid? roomId)
        {
            if (schedule != null)
            {
                _context.Set<DoctorSchedule>().Add(schedule);
                _context.Entry(schedule).Property("RoomId").CurrentValue = roomId ?? RoomId;
                _context.SaveChanges();
            }
        }

        private void SetupAnotherDoctorScheduleInContext(Guid doctorId, Guid scheduleId, Guid roomId, DateTime workDate, ShiftType shiftType)
        {
            var schedule = new DoctorSchedule
            {
                Id = scheduleId,
                DoctorId = doctorId,
                WorkDate = workDate,
                ShiftType = shiftType,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TimeSlots = new List<TimeSlot>()
            };
            _context.Set<DoctorSchedule>().Add(schedule);
            _context.Entry(schedule).Property("RoomId").CurrentValue = roomId;
            _context.SaveChanges();
        }

        private void SetupHappyPathRepos(
            DoctorProfile? doctor = null,
            FacilityRoom? room = null,
            DoctorSchedule? schedule = null)
        {
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(new[] { doctor ?? EditDoctorScheduleMockData.GetActiveDoctorProfile() });
            SetupRoom(new[] { room ?? EditDoctorScheduleMockData.GetActiveRoom() });
            SetupScheduleQueryRepo(Array.Empty<DoctorSchedule>());

            var targetRoomId = room?.Id ?? RoomId;
            if (schedule != null)
            {
                SetupScheduleInContext(schedule, targetRoomId);
            }
            else
            {
                var defaultSchedule = EditDoctorScheduleMockData.GetScheduleWithNoBookedSlots(roomId: targetRoomId);
                SetupScheduleInContext(defaultSchedule, targetRoomId);
            }
        }

        // ==================================================================
        // ==================== HAPPY PATH TESTS ===========================
        // ==================================================================

        /// <summary>
        /// TC-01: Happy path - change room only → success.
        /// Covers: ResolveReceptionistClinicIdAsync, ResolveActiveDoctorProfileAsync,
        ///         EnsureSameClinic, ResolveOwnedScheduleAsync, EnsureNoBookedSlots,
        ///         ResolveTargetRoomAsync, GetCurrentRoomId, EnsureNoDuplicateOnNewDateAsync (early return),
        ///         EnsureNoRoomConflictAsync, ApplyChanges, BuildResponse, CreateSuccessResponse.
        /// </summary>
        [Fact]
        public async Task Process_ChangeRoomOnly_ReturnsSuccess()
        {
            //Arrange 1
            var doctor = EditDoctorScheduleMockData.GetActiveDoctorProfile();
            var newRoom = EditDoctorScheduleMockData.GetActiveRoom(id: NewRoomId, roomName: "New Room");
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeRoomOnly(NewRoomId);
            var schedule = EditDoctorScheduleMockData.GetScheduleWithNoBookedSlots();

            //Arrange 2
            SetupHappyPathRepos(doctor: doctor, room: newRoom, schedule: schedule);

            //Act
            var result = await _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.ScheduleId.Should().Be(ScheduleId);
            result.Data.RoomName.Should().Be("New Room");
            result.Data.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// TC-02: Happy path - change date only → success.
        /// Covers: EnsureNoDuplicateOnNewDateAsync with actual duplicate check.
        /// </summary>
        [Fact]
        public async Task Process_ChangeDateOnly_ReturnsSuccess()
        {
            //Arrange 1
            var newDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeDateOnly(newDate);
            var schedule = EditDoctorScheduleMockData.GetScheduleWithNoBookedSlots();

            //Arrange 2
            SetupHappyPathRepos(schedule: schedule);
            SetupScheduleQueryRepo(Array.Empty<DoctorSchedule>());

            //Act
            var result = await _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.WorkDate.Should().Be(newDate);
        }

        /// <summary>
        /// TC-03: Happy path - change both room and date → success.
        /// </summary>
        [Fact]
        public async Task Process_ChangeBothRoomAndDate_ReturnsSuccess()
        {
            //Arrange 1
            var newDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
            var newRoom = EditDoctorScheduleMockData.GetActiveRoom(id: NewRoomId, roomName: "Updated Room");
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeBoth(newDate, NewRoomId);
            var schedule = EditDoctorScheduleMockData.GetScheduleWithNoBookedSlots();

            //Arrange 2
            SetupHappyPathRepos(doctor: null, room: newRoom, schedule: schedule);
            SetupScheduleQueryRepo(Array.Empty<DoctorSchedule>());

            //Act
            var result = await _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.WorkDate.Should().Be(newDate);
            result.Data.RoomName.Should().Be("Updated Room");
        }

        /// <summary>
        /// TC-04: Response CodeMessage is APP_MESSAGE_2000 and Meta is null.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_ResponseCodeMessage2000AndMetaNull()
        {
            //Arrange 1
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeRoomOnly(NewRoomId);

            //Arrange 2
            var newRoom = EditDoctorScheduleMockData.GetActiveRoom(id: NewRoomId);
            SetupHappyPathRepos(room: newRoom);

            //Act
            var result = await _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Meta.Should().BeNull();
        }

        /// <summary>
        /// TC-05: Verify schedule UpdatedAt is set correctly.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_ScheduleUpdatedAtIsSet()
        {
            //Arrange 1
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeRoomOnly(NewRoomId);
            var newRoom = EditDoctorScheduleMockData.GetActiveRoom(id: NewRoomId);

            //Arrange 2
            SetupHappyPathRepos(room: newRoom);

            //Act
            var result = await _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            result.Data!.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        // ==================================================================
        // ================ RECEPTIONIST ERROR TESTS ======================
        // ==================================================================

        /// <summary>
        /// TC-06: Receptionist StaffClinic not found → ResolveReceptionistClinicIdAsync throws KeyNotFoundException.
        /// </summary>
        [Fact]
        public async Task Process_ReceptionistStaffClinicNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeRoomOnly();

            //Arrange 2
            SetupStaffClinic(Array.Empty<StaffClinic>());

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4008.ToString());
        }

        /// <summary>
        /// TC-07: Receptionist inactive → StaffClinic.IsActive=false filtered → returns null → throws KeyNotFoundException.
        /// Note: Mock cannot apply IsActive filter, so we return empty list to simulate filtered result.
        /// </summary>
        [Fact]
        public async Task Process_ReceptionistInactive_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeRoomOnly();

            //Arrange 2
            SetupStaffClinic(Array.Empty<StaffClinic>()); // Empty = filtered out inactive
            SetupDoctor(Array.Empty<DoctorProfile>());

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4008.ToString());
        }

        // ==================================================================
        // ==================== DOCTOR ERROR TESTS =========================
        // ==================================================================

        /// <summary>
        /// TC-08: Doctor profile not found → ResolveActiveDoctorProfileAsync throws KeyNotFoundException.
        /// </summary>
        [Fact]
        public async Task Process_DoctorNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeRoomOnly();

            //Arrange 2
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(Array.Empty<DoctorProfile>());

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4011.ToString());
        }

        /// <summary>
        /// TC-09: Doctor inactive → DoctorProfile.IsActive=false filtered → returns null → throws KeyNotFoundException.
        /// Note: Mock cannot apply IsActive filter, so we return empty list to simulate filtered result.
        /// </summary>
        [Fact]
        public async Task Process_DoctorInactive_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeRoomOnly();

            //Arrange 2
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(Array.Empty<DoctorProfile>()); // Empty = filtered out inactive

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4011.ToString());
        }

        // ==================================================================
        // ================ AUTHORIZATION ERROR TESTS ======================
        // ==================================================================

        /// <summary>
        /// TC-10: Doctor belongs to different clinic → EnsureSameClinic throws UnauthorizedAccessException.
        /// </summary>
        [Fact]
        public async Task Process_DifferentClinic_ThrowsUnauthorizedAccessException()
        {
            //Arrange 1
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeRoomOnly();
            var foreignDoctor = EditDoctorScheduleMockData.GetActiveDoctorProfile(clinicId: OtherClinicId);

            //Arrange 2
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(new[] { foreignDoctor });

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4008.ToString());
        }

        // ==================================================================
        // ================ SCHEDULE ERROR TESTS ===========================
        // ==================================================================

        /// <summary>
        /// TC-11: Schedule not found → ResolveOwnedScheduleAsync returns null → throws KeyNotFoundException.
        /// </summary>
        [Fact]
        public async Task Process_ScheduleNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeRoomOnly();

            //Arrange 2
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(new[] { EditDoctorScheduleMockData.GetActiveDoctorProfile() });
            SetupRoom(new[] { EditDoctorScheduleMockData.GetActiveRoom() });
            // Don't add any schedule to context

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4012.ToString());
        }

        /// <summary>
        /// TC-12: Schedule owned by different doctor → ResolveOwnedScheduleAsync returns null → throws KeyNotFoundException.
        /// </summary>
        [Fact]
        public async Task Process_ScheduleOwnedByDifferentDoctor_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeRoomOnly();

            //Arrange 2
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(new[] { EditDoctorScheduleMockData.GetActiveDoctorProfile() });
            SetupRoom(new[] { EditDoctorScheduleMockData.GetActiveRoom() });
            SetupScheduleQueryRepo(Array.Empty<DoctorSchedule>());
            // Add schedule owned by different doctor
            var foreignSchedule = EditDoctorScheduleMockData.GetScheduleOwnedByDifferentDoctor();
            SetupScheduleInContext(foreignSchedule, RoomId);

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4012.ToString());
        }

        /// <summary>
        /// TC-13: Schedule already deleted → ResolveOwnedScheduleAsync returns null → throws KeyNotFoundException.
        /// </summary>
        [Fact]
        public async Task Process_ScheduleAlreadyDeleted_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeRoomOnly();

            //Arrange 2
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(new[] { EditDoctorScheduleMockData.GetActiveDoctorProfile() });
            SetupRoom(new[] { EditDoctorScheduleMockData.GetActiveRoom() });
            SetupScheduleQueryRepo(Array.Empty<DoctorSchedule>());
            // Add deleted schedule
            var deletedSchedule = EditDoctorScheduleMockData.GetDeletedSchedule();
            SetupScheduleInContext(deletedSchedule, RoomId);

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4012.ToString());
        }

        // ==================================================================
        // ================ BOOKED SLOTS ERROR TESTS ======================
        // ==================================================================

        /// <summary>
        /// TC-14: Schedule has at least one booked slot → EnsureNoBookedSlots throws InvalidOperationException.
        /// </summary>
        [Fact]
        public async Task Process_HasBookedSlots_ThrowsInvalidOperationException()
        {
            //Arrange 1
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeRoomOnly();
            var scheduleWithBooked = EditDoctorScheduleMockData.GetScheduleWithBookedSlots();

            //Arrange 2
            SetupHappyPathRepos(schedule: scheduleWithBooked);

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4013.ToString());
        }

        /// <summary>
        /// TC-15: Schedule has null TimeSlots collection → EnsureNoBookedSlots handles via ?? [].
        /// </summary>
        [Fact]
        public async Task Process_NullTimeSlots_ReturnsSuccess()
        {
            //Arrange 1
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeRoomOnly();
            var scheduleWithNullSlots = EditDoctorScheduleMockData.GetScheduleWithNullTimeSlots();

            //Arrange 2
            SetupHappyPathRepos(schedule: scheduleWithNullSlots);

            //Act
            var result = await _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
        }

        /// <summary>
        /// TC-16: Schedule has empty TimeSlots collection → success (no booked slots).
        /// </summary>
        [Fact]
        public async Task Process_EmptyTimeSlots_ReturnsSuccess()
        {
            //Arrange 1
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeRoomOnly();
            var scheduleWithEmptySlots = new DoctorSchedule
            {
                Id = ScheduleId,
                DoctorId = DoctorProfileId,
                WorkDate = DateTime.UtcNow.Date.AddDays(1),
                ShiftType = ShiftType.MORNING,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TimeSlots = new List<TimeSlot>()
            };

            //Arrange 2
            SetupHappyPathRepos(schedule: scheduleWithEmptySlots);

            //Act
            var result = await _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
        }

        // ==================================================================
        // ==================== ROOM ERROR TESTS ===========================
        // ==================================================================

        /// <summary>
        /// TC-17: Room not found → ResolveTargetRoomAsync throws KeyNotFoundException.
        /// </summary>
        [Fact]
        public async Task Process_RoomNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var newRoomId = Guid.NewGuid();
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeRoomOnly(newRoomId);

            //Arrange 2
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(new[] { EditDoctorScheduleMockData.GetActiveDoctorProfile() });
            SetupRoom(Array.Empty<FacilityRoom>()); // No room found
            SetupScheduleQueryRepo(Array.Empty<DoctorSchedule>());
            SetupScheduleInContext(EditDoctorScheduleMockData.GetScheduleWithNoBookedSlots(), RoomId);

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());
        }

        /// <summary>
        /// TC-18: Room inactive → Room.IsActive=false filtered → returns null → throws KeyNotFoundException.
        /// Note: Mock cannot apply IsActive filter, so we return empty list to simulate filtered result.
        /// </summary>
        [Fact]
        public async Task Process_RoomInactive_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var newRoomId = Guid.NewGuid();
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeRoomOnly(newRoomId);

            //Arrange 2
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(new[] { EditDoctorScheduleMockData.GetActiveDoctorProfile() });
            SetupRoom(Array.Empty<FacilityRoom>()); // Empty = filtered out inactive
            SetupScheduleQueryRepo(Array.Empty<DoctorSchedule>());
            SetupScheduleInContext(EditDoctorScheduleMockData.GetScheduleWithNoBookedSlots(), RoomId);

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());
        }

        /// <summary>
        /// TC-19: Room belongs to different clinic → ClinicId filter returns null → throws KeyNotFoundException.
        /// Note: Mock cannot apply ClinicId filter, so we return empty list to simulate filtered result.
        /// </summary>
        [Fact]
        public async Task Process_RoomFromDifferentClinic_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var newRoomId = Guid.NewGuid();
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeRoomOnly(newRoomId);

            //Arrange 2
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(new[] { EditDoctorScheduleMockData.GetActiveDoctorProfile() });
            SetupRoom(Array.Empty<FacilityRoom>()); // Empty = filtered out (different clinic)
            SetupScheduleQueryRepo(Array.Empty<DoctorSchedule>());
            SetupScheduleInContext(EditDoctorScheduleMockData.GetScheduleWithNoBookedSlots(), RoomId);

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4019.ToString());
        }

        /// <summary>
        /// TC-20: Schedule has null RoomId shadow property and request.RoomId is null → ResolveTargetRoomAsync throws InvalidOperationException.
        /// Covers: GetCurrentRoomId returns null → targetRoomId is null → throws.
        /// </summary>
        [Fact]
        public async Task Process_ScheduleRoomIdNull_ThrowsInvalidOperationException()
        {
            //Arrange 1
            var request = new EditDoctorScheduleRequest { WorkDate = null, RoomId = null };

            //Arrange 2
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(new[] { EditDoctorScheduleMockData.GetActiveDoctorProfile() });
            SetupRoom(new[] { EditDoctorScheduleMockData.GetActiveRoom() });
            SetupScheduleQueryRepo(Array.Empty<DoctorSchedule>());
            // Setup schedule WITHOUT setting RoomId shadow property
            var schedule = EditDoctorScheduleMockData.GetScheduleWithNoBookedSlots();
            _context.Set<DoctorSchedule>().Add(schedule);
            // Don't set RoomId shadow property
            _context.SaveChanges();

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4008.ToString());
        }

        // ==================================================================
        // ================ DUPLICATE SCHEDULE ERROR TESTS =================
        // ==================================================================

        /// <summary>
        /// TC-21: Changing date creates duplicate schedule → EnsureNoDuplicateOnNewDateAsync throws InvalidOperationException.
        /// </summary>
        [Fact]
        public async Task Process_DuplicateScheduleOnNewDate_ThrowsInvalidOperationException()
        {
            //Arrange 1
            var newDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeDateOnly(newDate);
            var existingDuplicate = new DoctorSchedule
            {
                Id = Guid.NewGuid(),
                DoctorId = DoctorProfileId,
                WorkDate = newDate.ToDateTime(TimeOnly.MinValue),
                ShiftType = ShiftType.MORNING,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TimeSlots = new List<TimeSlot>()
            };
            var schedule = EditDoctorScheduleMockData.GetScheduleWithNoBookedSlots();

            //Arrange 2
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(new[] { EditDoctorScheduleMockData.GetActiveDoctorProfile() });
            SetupRoom(new[] { EditDoctorScheduleMockData.GetActiveRoom() });
            SetupScheduleQueryRepo(new[] { existingDuplicate });
            SetupScheduleInContext(schedule, RoomId);

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4015.ToString());
        }

        /// <summary>
        /// TC-22: No duplicate when changing date → EnsureNoDuplicateOnNewDateAsync passes.
        /// Covers early return path when newWorkDate is null.
        /// </summary>
        [Fact]
        public async Task Process_NoWorkDateChange_SkipsDuplicateCheck()
        {
            //Arrange 1
            var request = new EditDoctorScheduleRequest { WorkDate = null, RoomId = NewRoomId };

            //Arrange 2
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(new[] { EditDoctorScheduleMockData.GetActiveDoctorProfile() });
            SetupRoom(new[] { EditDoctorScheduleMockData.GetActiveRoom(id: NewRoomId) });
            SetupScheduleQueryRepo(Array.Empty<DoctorSchedule>());
            SetupScheduleInContext(EditDoctorScheduleMockData.GetScheduleWithNoBookedSlots(), RoomId);

            //Act
            var result = await _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
        }

        // ==================================================================
        // ================ ROOM CONFLICT ERROR TESTS ======================
        // ==================================================================

        /// <summary>
        /// TC-23: Room conflict with other doctor on same date/shift → EnsureNoRoomConflictAsync throws InvalidOperationException.
        /// </summary>
        [Fact]
        public async Task Process_RoomConflictWithOtherDoctor_ThrowsInvalidOperationException()
        {
            //Arrange 1
            var newDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
            var conflictRoomId = Guid.NewGuid();
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeBoth(newDate, conflictRoomId);
            var schedule = EditDoctorScheduleMockData.GetScheduleWithNoBookedSlots(
                workDate: DateTime.UtcNow.AddDays(-1),
                shiftType: ShiftType.MORNING);

            //Arrange 2
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(new[] { EditDoctorScheduleMockData.GetActiveDoctorProfile() });
            SetupRoom(new[] { EditDoctorScheduleMockData.GetActiveRoom(id: conflictRoomId) });
            SetupScheduleQueryRepo(Array.Empty<DoctorSchedule>());
            SetupScheduleInContext(schedule, RoomId);
            // Add conflict schedule from different doctor
            SetupAnotherDoctorScheduleInContext(
                Guid.NewGuid(), Guid.NewGuid(), conflictRoomId,
                newDate.ToDateTime(TimeOnly.MinValue), ShiftType.MORNING);

            //Act
            var act = () => _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4058.ToString());
        }

        /// <summary>
        /// TC-24: No room conflict (same doctor uses room on different shift) → success.
        /// </summary>
        [Fact]
        public async Task Process_NoRoomConflict_ReturnsSuccess()
        {
            //Arrange 1
            var newDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
            var conflictRoomId = Guid.NewGuid();
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeBoth(newDate, conflictRoomId);
            var schedule = EditDoctorScheduleMockData.GetScheduleWithNoBookedSlots(
                workDate: DateTime.UtcNow.AddDays(-1),
                shiftType: ShiftType.MORNING);

            //Arrange 2
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(new[] { EditDoctorScheduleMockData.GetActiveDoctorProfile() });
            SetupRoom(new[] { EditDoctorScheduleMockData.GetActiveRoom(id: conflictRoomId) });
            SetupScheduleQueryRepo(Array.Empty<DoctorSchedule>());
            SetupScheduleInContext(schedule, RoomId);
            // Same doctor, different shift - no conflict
            SetupAnotherDoctorScheduleInContext(
                DoctorProfileId, Guid.NewGuid(), conflictRoomId,
                newDate.ToDateTime(TimeOnly.MinValue), ShiftType.AFTERNOON);

            //Act
            var result = await _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
        }

        // ==================================================================
        // ================ SLOT SHIFTING TESTS ============================
        // ==================================================================

        /// <summary>
        /// TC-25: Change date → ShiftSlotsToNewDate updates slot times correctly.
        /// </summary>
        [Fact]
        public async Task Process_ChangeDate_ShiftsSlotsCorrectly()
        {
            //Arrange 1
            var newDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeDateOnly(newDate);
            var slotStart = DateTime.UtcNow.Date.AddDays(1).AddHours(8);
            var schedule = new DoctorSchedule
            {
                Id = ScheduleId,
                DoctorId = DoctorProfileId,
                WorkDate = DateTime.UtcNow.Date.AddDays(1),
                ShiftType = ShiftType.MORNING,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TimeSlots = new List<TimeSlot>
                {
                    new TimeSlot
                    {
                        Id = Guid.NewGuid(),
                        ScheduleId = ScheduleId,
                        StartTime = slotStart,
                        EndTime = slotStart.AddMinutes(30),
                        Status = SlotStatus.AVAILABLE,
                        MaxPatients = 1,
                        CurrentPatients = 0
                    },
                    new TimeSlot
                    {
                        Id = Guid.NewGuid(),
                        ScheduleId = ScheduleId,
                        StartTime = slotStart.AddHours(1),
                        EndTime = slotStart.AddHours(1).AddMinutes(30),
                        Status = SlotStatus.AVAILABLE,
                        MaxPatients = 1,
                        CurrentPatients = 0
                    }
                }
            };

            //Arrange 2
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(new[] { EditDoctorScheduleMockData.GetActiveDoctorProfile() });
            SetupRoom(new[] { EditDoctorScheduleMockData.GetActiveRoom() });
            SetupScheduleQueryRepo(Array.Empty<DoctorSchedule>());
            SetupScheduleInContext(schedule, RoomId);

            //Act
            var result = await _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            var updatedSchedule = await _context.Set<DoctorSchedule>()
                .Include(s => s.TimeSlots)
                .FirstAsync(s => s.Id == ScheduleId);
            updatedSchedule.TimeSlots.Should().OnlyContain(slot =>
                slot.StartTime.Date == newDate.ToDateTime(TimeOnly.MinValue).Date);
        }

        /// <summary>
        /// TC-26: No date change → ShiftSlotsToNewDate not called (empty foreach).
        /// </summary>
        [Fact]
        public async Task Process_NoDateChange_SlotsUnchanged()
        {
            //Arrange 1
            var request = new EditDoctorScheduleRequest { WorkDate = null, RoomId = NewRoomId };

            //Arrange 2
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(new[] { EditDoctorScheduleMockData.GetActiveDoctorProfile() });
            SetupRoom(new[] { EditDoctorScheduleMockData.GetActiveRoom(id: NewRoomId) });
            SetupScheduleQueryRepo(Array.Empty<DoctorSchedule>());
            SetupScheduleInContext(EditDoctorScheduleMockData.GetScheduleWithNoBookedSlots(), RoomId);

            //Act
            var result = await _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
        }

        // ==================================================================
        // ================ SHIFT TYPE TESTS ===============================
        // ==================================================================

        /// <summary>
        /// TC-27: AFTERNOON shift schedule edited successfully.
        /// </summary>
        [Fact]
        public async Task Process_AfternoonShift_ReturnsSuccess()
        {
            //Arrange 1
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeRoomOnly(NewRoomId);
            var afternoonSchedule = new DoctorSchedule
            {
                Id = ScheduleId,
                DoctorId = DoctorProfileId,
                WorkDate = DateTime.UtcNow.Date.AddDays(1),
                ShiftType = ShiftType.AFTERNOON,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TimeSlots = new List<TimeSlot> { EditDoctorScheduleMockData.GetAvailableTimeSlot() }
            };

            //Arrange 2
            var newRoom = EditDoctorScheduleMockData.GetActiveRoom(id: NewRoomId);
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(new[] { EditDoctorScheduleMockData.GetActiveDoctorProfile() });
            SetupRoom(new[] { newRoom });
            SetupScheduleQueryRepo(Array.Empty<DoctorSchedule>());
            SetupScheduleInContext(afternoonSchedule, RoomId);

            //Act
            var result = await _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.ShiftType.Should().Be(ShiftType.AFTERNOON);
        }

        /// <summary>
        /// TC-28: EVENING shift schedule edited successfully.
        /// </summary>
        [Fact]
        public async Task Process_EveningShift_ReturnsSuccess()
        {
            //Arrange 1
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeRoomOnly(NewRoomId);
            var eveningSchedule = new DoctorSchedule
            {
                Id = ScheduleId,
                DoctorId = DoctorProfileId,
                WorkDate = DateTime.UtcNow.Date.AddDays(1),
                ShiftType = ShiftType.EVENING,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TimeSlots = new List<TimeSlot> { EditDoctorScheduleMockData.GetAvailableTimeSlot() }
            };

            //Arrange 2
            var newRoom = EditDoctorScheduleMockData.GetActiveRoom(id: NewRoomId);
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(new[] { EditDoctorScheduleMockData.GetActiveDoctorProfile() });
            SetupRoom(new[] { newRoom });
            SetupScheduleQueryRepo(Array.Empty<DoctorSchedule>());
            SetupScheduleInContext(eveningSchedule, RoomId);

            //Act
            var result = await _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.ShiftType.Should().Be(ShiftType.EVENING);
        }

        // ==================================================================
        // ================ EnsureNoRoomConflictAsync BRANCH TESTS ==========
        // ==================================================================

        /// <summary>
        /// TC-29: No conflict schedules exist → EnsureNoRoomConflictAsync passes.
        /// Covers: conflictSchedules.Any() returns false.
        /// </summary>
        [Fact]
        public async Task Process_NoConflictSchedules_ReturnsSuccess()
        {
            //Arrange 1
            var newDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeBoth(newDate, NewRoomId);
            var schedule = EditDoctorScheduleMockData.GetScheduleWithNoBookedSlots(
                workDate: DateTime.UtcNow.AddDays(-1),
                shiftType: ShiftType.MORNING);

            //Arrange 2
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(new[] { EditDoctorScheduleMockData.GetActiveDoctorProfile() });
            SetupRoom(new[] { EditDoctorScheduleMockData.GetActiveRoom(id: NewRoomId) });
            SetupScheduleQueryRepo(Array.Empty<DoctorSchedule>());
            SetupScheduleInContext(schedule, RoomId);
            // Add schedules from different doctor on different date
            SetupAnotherDoctorScheduleInContext(
                Guid.NewGuid(), Guid.NewGuid(), NewRoomId,
                DateTime.UtcNow.AddDays(-10), ShiftType.MORNING);

            //Act
            var result = await _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
        }

        /// <summary>
        /// TC-30: No conflict (same date but different shift) → EnsureNoRoomConflictAsync passes.
        /// </summary>
        [Fact]
        public async Task Process_SameDateDifferentShift_NoConflict()
        {
            //Arrange 1
            var newDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeBoth(newDate, NewRoomId);
            var schedule = EditDoctorScheduleMockData.GetScheduleWithNoBookedSlots(
                workDate: DateTime.UtcNow.AddDays(-1),
                shiftType: ShiftType.MORNING);

            //Arrange 2
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(new[] { EditDoctorScheduleMockData.GetActiveDoctorProfile() });
            SetupRoom(new[] { EditDoctorScheduleMockData.GetActiveRoom(id: NewRoomId) });
            SetupScheduleQueryRepo(Array.Empty<DoctorSchedule>());
            SetupScheduleInContext(schedule, RoomId);
            // Add conflict schedule from different doctor on same date but DIFFERENT shift
            SetupAnotherDoctorScheduleInContext(
                Guid.NewGuid(), Guid.NewGuid(), NewRoomId,
                newDate.ToDateTime(TimeOnly.MinValue), ShiftType.AFTERNOON);

            //Act
            var result = await _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
        }

        /// <summary>
        /// TC-31: ApplyChanges updates RoomId correctly.
        /// </summary>
        [Fact]
        public async Task Process_ApplyChanges_UpdatesRoomIdAndUpdatedAt()
        {
            //Arrange 1
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeRoomOnly(NewRoomId);
            var schedule = EditDoctorScheduleMockData.GetScheduleWithNoBookedSlots();

            //Arrange 2
            var newRoom = EditDoctorScheduleMockData.GetActiveRoom(id: NewRoomId);
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(new[] { EditDoctorScheduleMockData.GetActiveDoctorProfile() });
            SetupRoom(new[] { newRoom });
            SetupScheduleQueryRepo(Array.Empty<DoctorSchedule>());
            SetupScheduleInContext(schedule, RoomId);

            //Act
            var result = await _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            var updatedSchedule = await _context.Set<DoctorSchedule>().FirstAsync(s => s.Id == ScheduleId);
            var roomIdValue = (Guid?)_context.Entry(updatedSchedule).Property("RoomId").CurrentValue;
            roomIdValue.Should().Be(NewRoomId);
            updatedSchedule.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        // ==================================================================
        // ================ NULL/EMPTY BRANCH COVERAGE TESTS ==============
        // ==================================================================

        /// <summary>
        /// TC-32: Schedule with non-null TimeSlots (no booked) → EnsureNoBookedSlots non-null branch covered.
        /// This test explicitly exercises the non-null branch of `schedule.TimeSlots ?? []` in EnsureNoBookedSlots.
        /// </summary>
        [Fact]
        public async Task Process_NonNullTimeSlotsNoBooked_CoversNonNullBranch()
        {
            //Arrange 1
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeRoomOnly(NewRoomId);
            // Schedule with non-null TimeSlots (only AVAILABLE slots, no BOOKED)
            var scheduleWithOnlyAvailableSlots = new DoctorSchedule
            {
                Id = ScheduleId,
                DoctorId = DoctorProfileId,
                WorkDate = DateTime.UtcNow.Date.AddDays(1),
                ShiftType = ShiftType.MORNING,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TimeSlots = new List<TimeSlot>
                {
                    new TimeSlot
                    {
                        Id = Guid.NewGuid(),
                        ScheduleId = ScheduleId,
                        StartTime = DateTime.UtcNow.Date.AddDays(1).AddHours(8),
                        EndTime = DateTime.UtcNow.Date.AddDays(1).AddHours(9),
                        Status = SlotStatus.AVAILABLE,
                        MaxPatients = 1,
                        CurrentPatients = 0
                    }
                }
            };

            //Arrange 2
            var newRoom = EditDoctorScheduleMockData.GetActiveRoom(id: NewRoomId);
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(new[] { EditDoctorScheduleMockData.GetActiveDoctorProfile() });
            SetupRoom(new[] { newRoom });
            SetupScheduleQueryRepo(Array.Empty<DoctorSchedule>());
            SetupScheduleInContext(scheduleWithOnlyAvailableSlots, RoomId);

            //Act
            var result = await _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
        }

        /// <summary>
        /// TC-33: Null TimeSlots with date change → ShiftSlotsToNewDate null branch covered.
        /// This test explicitly exercises the null branch of `schedule.TimeSlots ?? []` in ShiftSlotsToNewDate.
        /// Uses reflection to set TimeSlots to null after adding to context.
        /// </summary>
        [Fact]
        public async Task Process_NullTimeSlotsWithDateChange_CoversNullBranch()
        {
            //Arrange 1
            var newDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeDateOnly(newDate);
            // Schedule with null TimeSlots
            var scheduleWithNullSlots = new DoctorSchedule
            {
                Id = ScheduleId,
                DoctorId = DoctorProfileId,
                WorkDate = DateTime.UtcNow.Date.AddDays(1),
                ShiftType = ShiftType.MORNING,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TimeSlots = null!
            };

            //Arrange 2
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(new[] { EditDoctorScheduleMockData.GetActiveDoctorProfile() });
            SetupRoom(new[] { EditDoctorScheduleMockData.GetActiveRoom() });
            SetupScheduleQueryRepo(Array.Empty<DoctorSchedule>());
            SetupScheduleInContext(scheduleWithNullSlots, RoomId);

            // Force TimeSlots to be null using reflection (bypass EF Core's auto-initialization)
            var trackedSchedule = await _context.Set<DoctorSchedule>().FirstAsync(s => s.Id == ScheduleId);
            var timeSlotsProperty = typeof(DoctorSchedule).GetProperty("TimeSlots");
            timeSlotsProperty?.SetValue(trackedSchedule, null);

            //Act
            var result = await _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
        }

        /// <summary>
        /// TC-34: Null TimeSlots in EnsureNoBookedSlots → covers null coalescing branch.
        /// Uses reflection to force TimeSlots to null after entity is tracked.
        /// </summary>
        [Fact]
        public async Task Process_NullTimeSlotsInEnsureNoBookedSlots_CoversNullCoalescingBranch()
        {
            //Arrange 1
            var request = EditDoctorScheduleMockData.GetValidRequest_ChangeRoomOnly(NewRoomId);
            var newRoom = EditDoctorScheduleMockData.GetActiveRoom(id: NewRoomId);
            // Schedule initially with empty list, will be set to null via reflection
            var scheduleWithSlots = new DoctorSchedule
            {
                Id = ScheduleId,
                DoctorId = DoctorProfileId,
                WorkDate = DateTime.UtcNow.Date.AddDays(1),
                ShiftType = ShiftType.MORNING,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TimeSlots = new List<TimeSlot>()
            };

            //Arrange 2
            SetupStaffClinic(new[] { EditDoctorScheduleMockData.GetActiveStaffClinic() });
            SetupDoctor(new[] { EditDoctorScheduleMockData.GetActiveDoctorProfile() });
            SetupRoom(new[] { newRoom });
            SetupScheduleQueryRepo(Array.Empty<DoctorSchedule>());
            SetupScheduleInContext(scheduleWithSlots, RoomId);

            // Force TimeSlots to be null using reflection
            var trackedSchedule = await _context.Set<DoctorSchedule>().FirstAsync(s => s.Id == ScheduleId);
            var timeSlotsProperty = typeof(DoctorSchedule).GetProperty("TimeSlots");
            timeSlotsProperty?.SetValue(trackedSchedule, null);

            //Act
            var result = await _sut.Process(ReceptionistUserId, DoctorProfileId, ScheduleId, request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
        }

        // ==================================================================
        // ================ HasBookedSlot BRANCH TESTS =====================
        // ==================================================================

        /// <summary>
        /// TC-35: HasBookedSlot with null TimeSlots → returns false.
        /// Tests null branch of TimeSlots check in HasBookedSlot.
        /// </summary>
        [Fact]
        public void HasBookedSlot_NullTimeSlots_ReturnsFalse()
        {
            //Arrange 1
            var schedule = new DoctorSchedule
            {
                Id = ScheduleId,
                TimeSlots = null!
            };

            //Act
            var result = InvokeHasBookedSlot(schedule);

            //Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// TC-36: HasBookedSlot with non-null TimeSlots containing BOOKED slot → returns true.
        /// Tests the BOOKED slot detection branch.
        /// </summary>
        [Fact]
        public void HasBookedSlot_WithBookedSlot_ReturnsTrue()
        {
            //Arrange 1
            var schedule = new DoctorSchedule
            {
                Id = ScheduleId,
                TimeSlots = new List<TimeSlot>
                {
                    new TimeSlot { Status = SlotStatus.BOOKED }
                }
            };

            //Act
            var result = InvokeHasBookedSlot(schedule);

            //Assert
            result.Should().BeTrue();
        }

        /// <summary>
        /// TC-37: HasBookedSlot with non-null TimeSlots but no BOOKED slots → returns false.
        /// Tests the no-BOOKED branch of HasBookedSlot.
        /// </summary>
        [Fact]
        public void HasBookedSlot_NoBookedSlots_ReturnsFalse()
        {
            //Arrange 1
            var schedule = new DoctorSchedule
            {
                Id = ScheduleId,
                TimeSlots = new List<TimeSlot>
                {
                    new TimeSlot { Status = SlotStatus.AVAILABLE },
                    new TimeSlot { Status = SlotStatus.BLOCKED }
                }
            };

            //Act
            var result = InvokeHasBookedSlot(schedule);

            //Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// TC-38: ShiftSlotsToNewDate with null TimeSlots → early return.
        /// Tests null branch of TimeSlots check in ShiftSlotsToNewDate.
        /// </summary>
        [Fact]
        public void ShiftSlotsToNewDate_NullTimeSlots_EarlyReturn()
        {
            //Arrange 1
            var schedule = new DoctorSchedule
            {
                Id = ScheduleId,
                TimeSlots = null!,
                WorkDate = DateTime.UtcNow.Date
            };
            var newDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

            //Act & Assert - Should not throw
            var act = () => InvokeShiftSlotsToNewDate(schedule, newDate);
            act.Should().NotThrow();
        }

        // ==================================================================
        // ================ HELPER METHODS FOR PRIVATE TESTING ================
        // ==================================================================

        private static bool InvokeHasBookedSlot(DoctorSchedule schedule)
        {
            var method = typeof(EditDoctorScheduleService)
                .GetMethod("HasBookedSlot", BindingFlags.NonPublic | BindingFlags.Static);
            return (bool)(method?.Invoke(null, new object[] { schedule })!);
        }

        private static void InvokeShiftSlotsToNewDate(DoctorSchedule schedule, DateOnly newDate)
        {
            var method = typeof(EditDoctorScheduleService)
                .GetMethod("ShiftSlotsToNewDate", BindingFlags.NonPublic | BindingFlags.Static);
            method?.Invoke(null, new object[] { schedule, newDate });
        }
    }
}
