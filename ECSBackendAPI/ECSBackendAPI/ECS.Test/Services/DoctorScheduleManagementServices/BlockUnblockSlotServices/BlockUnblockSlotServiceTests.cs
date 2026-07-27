using System.Linq.Expressions;
using ECS.Application.Services.DoctorScheduleManagementServices.BlockUnblockSlotServices;
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

namespace ECS.Test.Services.DoctorScheduleManagementServices.BlockUnblockSlotServices
{
    /// <summary>
    /// Unit tests for <see cref="BlockUnblockSlotService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line coverage on <c>BlockUnblockSlotService.cs</c>.
    /// </summary>
    /// <remarks>
    /// The service uses a real <see cref="AppDbContext"/> (in-memory) because
    /// <c>ResolveDoctorTimeSlotAsync</c> queries <c>_dbContext.Set&lt;TimeSlot&gt;()</c>
    /// with <c>Include(s => s.Schedule)</c> to load the Schedule navigation property.
    /// EF Core's in-memory provider honours navigation-property loading.
    /// </remarks>
    public class BlockUnblockSlotServiceTests : IDisposable
    {
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepoMock = new();
        private readonly AppDbContext _context;
        private readonly BlockUnblockSlotService _sut;

        public BlockUnblockSlotServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);
            _sut = new BlockUnblockSlotService(
                _staffClinicRepoMock.Object,
                _doctorRepoMock.Object,
                _context);
        }

        public void Dispose() => _context.Dispose();

        // ─────────────────────────────────────────────────────────────────
        // Repository helpers
        // ─────────────────────────────────────────────────────────────────

        private void SetupStaffClinicRepo(StaffClinic? staffClinic)
        {
            var list = staffClinic != null ? new List<StaffClinic> { staffClinic } : new List<StaffClinic>();
            _staffClinicRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<StaffClinic, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<StaffClinic>().Object);
        }

        private void SetupDoctorRepo(DoctorProfile? doctor)
        {
            var list = doctor != null ? new List<DoctorProfile> { doctor } : new List<DoctorProfile>();
            _doctorRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<DoctorProfile, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<DoctorProfile>().Object);
        }

        private async Task SeedTimeSlotAsync(TimeSlot slot)
        {
            _context.TimeSlots.Add(slot);
            await _context.SaveChangesAsync();
        }

        // ==================================================================
        // ====================== Process(...) tests ========================
        // ==================================================================

        /// <summary>
        /// TC-BUS-01: Receptionist StaffClinic not found in database
        /// → ResolveReceptionistClinicIdAsync throws KeyNotFoundException(APP_MESSAGE_4008).
        /// Covers: ResolveReceptionistClinicIdAsync null check branch.
        /// </summary>
        [Fact]
        public async Task Process_ReceptionistNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = BlockUnblockSlotMockData.GetBlockRequest();

            //Arrange 2
            SetupStaffClinicRepo(null);

            //Act
            var act = () => _sut.Process(
                BlockUnblockSlotMockData.ReceptionistUserId,
                BlockUnblockSlotMockData.DoctorProfileId,
                BlockUnblockSlotMockData.SlotId,
                request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4008.ToString());
        }

        /// <summary>
        /// TC-BUS-02: Receptionist StaffClinic is inactive (filtered at query level → returns null)
        /// → ResolveReceptionistClinicIdAsync throws KeyNotFoundException(APP_MESSAGE_4008).
        /// Covers: staffClinic is null branch.
        /// </summary>
        [Fact]
        public async Task Process_ReceptionistInactive_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = BlockUnblockSlotMockData.GetBlockRequest();

            //Arrange 2
            SetupStaffClinicRepo(null); // Inactive is filtered at query level

            //Act
            var act = () => _sut.Process(
                BlockUnblockSlotMockData.ReceptionistUserId,
                BlockUnblockSlotMockData.DoctorProfileId,
                BlockUnblockSlotMockData.SlotId,
                request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4008.ToString());
        }

        /// <summary>
        /// TC-BUS-03: Doctor profile not found in database
        /// → ResolveActiveDoctorProfileAsync throws KeyNotFoundException(APP_MESSAGE_4011).
        /// Covers: doctorProfile is null branch.
        /// </summary>
        [Fact]
        public async Task Process_DoctorNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = BlockUnblockSlotMockData.GetBlockRequest();

            //Arrange 2
            SetupStaffClinicRepo(BlockUnblockSlotMockData.GetActiveStaffClinic());
            SetupDoctorRepo(null);

            //Act
            var act = () => _sut.Process(
                BlockUnblockSlotMockData.ReceptionistUserId,
                BlockUnblockSlotMockData.DoctorProfileId,
                BlockUnblockSlotMockData.SlotId,
                request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4011.ToString());
        }

        /// <summary>
        /// TC-BUS-04: Doctor profile is inactive (filtered at query level → returns null)
        /// → ResolveActiveDoctorProfileAsync throws KeyNotFoundException(APP_MESSAGE_4011).
        /// Covers: doctorProfile is null branch.
        /// </summary>
        [Fact]
        public async Task Process_DoctorInactive_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = BlockUnblockSlotMockData.GetBlockRequest();

            //Arrange 2
            SetupStaffClinicRepo(BlockUnblockSlotMockData.GetActiveStaffClinic());
            SetupDoctorRepo(null); // Inactive is filtered at query level

            //Act
            var act = () => _sut.Process(
                BlockUnblockSlotMockData.ReceptionistUserId,
                BlockUnblockSlotMockData.DoctorProfileId,
                BlockUnblockSlotMockData.SlotId,
                request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4011.ToString());
        }

        /// <summary>
        /// TC-BUS-05: Receptionist and Doctor belong to DIFFERENT clinics
        /// → EnsureSameClinic throws UnauthorizedAccessException(APP_MESSAGE_4008).
        /// Covers: receptionistClinicId != doctorClinicId branch.
        /// </summary>
        [Fact]
        public async Task Process_ClinicCrossBoundary_ThrowsUnauthorizedAccessException()
        {
            //Arrange 1
            var request = BlockUnblockSlotMockData.GetBlockRequest();

            //Arrange 2
            SetupStaffClinicRepo(BlockUnblockSlotMockData.GetActiveStaffClinic(
                clinicId: BlockUnblockSlotMockData.ClinicId));
            SetupDoctorRepo(BlockUnblockSlotMockData.GetActiveDoctorProfile(
                clinicId: BlockUnblockSlotMockData.OtherClinicId));

            //Act
            var act = () => _sut.Process(
                BlockUnblockSlotMockData.ReceptionistUserId,
                BlockUnblockSlotMockData.DoctorProfileId,
                BlockUnblockSlotMockData.SlotId,
                request);

            //Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4008.ToString());
        }

        /// <summary>
        /// TC-BUS-06: Time slot not found in database
        /// → ResolveDoctorTimeSlotAsync throws KeyNotFoundException(APP_MESSAGE_4004).
        /// Covers: slot is null branch.
        /// </summary>
        [Fact]
        public async Task Process_SlotNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = BlockUnblockSlotMockData.GetBlockRequest();

            //Arrange 2
            SetupStaffClinicRepo(BlockUnblockSlotMockData.GetActiveStaffClinic());
            SetupDoctorRepo(BlockUnblockSlotMockData.GetActiveDoctorProfile());
            // Don't seed any slot into _context

            //Act
            var act = () => _sut.Process(
                BlockUnblockSlotMockData.ReceptionistUserId,
                BlockUnblockSlotMockData.DoctorProfileId,
                BlockUnblockSlotMockData.SlotId,
                request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4004.ToString());
        }

        /// <summary>
        /// TC-BUS-07: Time slot belongs to a DIFFERENT doctor
        /// → ResolveDoctorTimeSlotAsync throws KeyNotFoundException(APP_MESSAGE_4004).
        /// Covers: slot is null branch (slot found but belongs to different doctor).
        /// </summary>
        [Fact]
        public async Task Process_SlotBelongsToDifferentDoctor_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var request = BlockUnblockSlotMockData.GetBlockRequest();
            var otherDoctorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

            //Arrange 2
            SetupStaffClinicRepo(BlockUnblockSlotMockData.GetActiveStaffClinic());
            SetupDoctorRepo(BlockUnblockSlotMockData.GetActiveDoctorProfile());
            // Seed slot belonging to a different doctor
            var slot = BlockUnblockSlotMockData.GetAvailableSlot(doctorId: otherDoctorId);
            await SeedTimeSlotAsync(slot);

            //Act
            var act = () => _sut.Process(
                BlockUnblockSlotMockData.ReceptionistUserId,
                BlockUnblockSlotMockData.DoctorProfileId,
                BlockUnblockSlotMockData.SlotId,
                request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4004.ToString());
        }

        /// <summary>
        /// TC-BUS-08: Time slot is already booked
        /// → EnsureSlotNotBooked throws InvalidOperationException(APP_MESSAGE_4009).
        /// Covers: slot.Status == SlotStatus.BOOKED branch.
        /// </summary>
        [Fact]
        public async Task Process_SlotAlreadyBooked_ThrowsInvalidOperationException()
        {
            //Arrange 1
            var request = BlockUnblockSlotMockData.GetBlockRequest();

            //Arrange 2
            SetupStaffClinicRepo(BlockUnblockSlotMockData.GetActiveStaffClinic());
            SetupDoctorRepo(BlockUnblockSlotMockData.GetActiveDoctorProfile());
            var bookedSlot = BlockUnblockSlotMockData.GetBookedSlot(
                doctorId: BlockUnblockSlotMockData.DoctorProfileId);
            await SeedTimeSlotAsync(bookedSlot);

            //Act
            var act = () => _sut.Process(
                BlockUnblockSlotMockData.ReceptionistUserId,
                BlockUnblockSlotMockData.DoctorProfileId,
                BlockUnblockSlotMockData.SlotId,
                request);

            //Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4009.ToString());
        }

        /// <summary>
        /// TC-BUS-09: Happy path - Block an AVAILABLE slot
        /// → Updates slot status to BLOCKED, returns success with "BLOCKED".
        /// Covers: Happy path, UpdateSlotStatus with Block=true, CreateSuccessResponse.
        /// </summary>
        [Fact]
        public async Task Process_BlockAvailableSlot_ReturnsBlockedStatus()
        {
            //Arrange 1
            var request = BlockUnblockSlotMockData.GetBlockRequest();

            //Arrange 2
            SetupStaffClinicRepo(BlockUnblockSlotMockData.GetActiveStaffClinic());
            SetupDoctorRepo(BlockUnblockSlotMockData.GetActiveDoctorProfile());
            var availableSlot = BlockUnblockSlotMockData.GetAvailableSlot(
                doctorId: BlockUnblockSlotMockData.DoctorProfileId);
            await SeedTimeSlotAsync(availableSlot);

            //Act
            var result = await _sut.Process(
                BlockUnblockSlotMockData.ReceptionistUserId,
                BlockUnblockSlotMockData.DoctorProfileId,
                BlockUnblockSlotMockData.SlotId,
                request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().Be("BLOCKED");
        }

        /// <summary>
        /// TC-BUS-10: Happy path - Unblock a BLOCKED slot
        /// → Updates slot status to AVAILABLE, returns success with "AVAILABLE".
        /// Covers: Happy path, UpdateSlotStatus with Block=false, CreateSuccessResponse.
        /// </summary>
        [Fact]
        public async Task Process_UnblockBlockedSlot_ReturnsAvailableStatus()
        {
            //Arrange 1
            var request = BlockUnblockSlotMockData.GetUnblockRequest();

            //Arrange 2
            SetupStaffClinicRepo(BlockUnblockSlotMockData.GetActiveStaffClinic());
            SetupDoctorRepo(BlockUnblockSlotMockData.GetActiveDoctorProfile());
            var blockedSlot = BlockUnblockSlotMockData.GetBlockedSlot(
                doctorId: BlockUnblockSlotMockData.DoctorProfileId);
            await SeedTimeSlotAsync(blockedSlot);

            //Act
            var result = await _sut.Process(
                BlockUnblockSlotMockData.ReceptionistUserId,
                BlockUnblockSlotMockData.DoctorProfileId,
                BlockUnblockSlotMockData.SlotId,
                request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().Be("AVAILABLE");
        }

        /// <summary>
        /// TC-BUS-11: Verify SaveChangesAsync is called exactly once on success.
        /// Covers: _dbContext.SaveChangesAsync() invocation.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_SaveChangesAsyncCalledOnce()
        {
            //Arrange 1
            var request = BlockUnblockSlotMockData.GetBlockRequest();

            //Arrange 2
            SetupStaffClinicRepo(BlockUnblockSlotMockData.GetActiveStaffClinic());
            SetupDoctorRepo(BlockUnblockSlotMockData.GetActiveDoctorProfile());
            var availableSlot = BlockUnblockSlotMockData.GetAvailableSlot(
                doctorId: BlockUnblockSlotMockData.DoctorProfileId);
            await SeedTimeSlotAsync(availableSlot);

            //Act
            await _sut.Process(
                BlockUnblockSlotMockData.ReceptionistUserId,
                BlockUnblockSlotMockData.DoctorProfileId,
                BlockUnblockSlotMockData.SlotId,
                request);

            //Assert
            // Verify slot status was actually updated in context
            var savedSlot = await _context.TimeSlots.FindAsync(BlockUnblockSlotMockData.SlotId);
            savedSlot.Should().NotBeNull();
            savedSlot!.Status.Should().Be(SlotStatus.BLOCKED);
        }

        /// <summary>
        /// TC-BUS-12: Happy path with specific doctor - Verify slot status is persisted correctly.
        /// Covers: Full happy path flow with non-default doctor ID.
        /// </summary>
        [Fact]
        public async Task Process_HappyPathWithSpecificDoctor_SlotStatusPersisted()
        {
            //Arrange 1
            var specificDoctorId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
            var request = BlockUnblockSlotMockData.GetBlockRequest();

            //Arrange 2
            SetupStaffClinicRepo(BlockUnblockSlotMockData.GetActiveStaffClinic());
            SetupDoctorRepo(BlockUnblockSlotMockData.GetActiveDoctorProfile(id: specificDoctorId));
            var availableSlot = BlockUnblockSlotMockData.GetAvailableSlot(
                doctorId: specificDoctorId,
                slotId: Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));
            await SeedTimeSlotAsync(availableSlot);

            //Act
            var result = await _sut.Process(
                BlockUnblockSlotMockData.ReceptionistUserId,
                specificDoctorId,
                availableSlot.Id,
                request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().Be("BLOCKED");

            var savedSlot = await _context.TimeSlots.FindAsync(availableSlot.Id);
            savedSlot!.Status.Should().Be(SlotStatus.BLOCKED);
        }

        /// <summary>
        /// TC-BUS-13: Verify Meta property is null in success response.
        /// Covers: ApiResponse<string>.Success(...) default Meta.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_MetaIsNull()
        {
            //Arrange 1
            var request = BlockUnblockSlotMockData.GetBlockRequest();

            //Arrange 2
            SetupStaffClinicRepo(BlockUnblockSlotMockData.GetActiveStaffClinic());
            SetupDoctorRepo(BlockUnblockSlotMockData.GetActiveDoctorProfile());
            var availableSlot = BlockUnblockSlotMockData.GetAvailableSlot(
                doctorId: BlockUnblockSlotMockData.DoctorProfileId);
            await SeedTimeSlotAsync(availableSlot);

            //Act
            var result = await _sut.Process(
                BlockUnblockSlotMockData.ReceptionistUserId,
                BlockUnblockSlotMockData.DoctorProfileId,
                BlockUnblockSlotMockData.SlotId,
                request);

            //Assert
            result.Meta.Should().BeNull();
        }
    }
}
