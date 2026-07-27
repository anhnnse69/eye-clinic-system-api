using System.Linq.Expressions;
using ECS.Application.Services.DoctorScheduleManagementServices.ClinicShiftServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.DoctorScheduleManagementServices.ClinicShiftServices
{
    /// <summary>
    /// Unit tests for <see cref="ClinicShiftService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line coverage on <c>ClinicShiftService.cs</c>.
    /// </summary>
    public class ClinicShiftServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock;
        private readonly Mock<IRepositoryQueryBase<Clinic, Guid, AppDbContext>> _clinicRepoMock;
        private readonly IClinicShiftService _sut;

        public ClinicShiftServiceTests()
        {
            _staffClinicRepoMock = new Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>>();
            _clinicRepoMock = new Mock<IRepositoryQueryBase<Clinic, Guid, AppDbContext>>();
            _sut = new ClinicShiftService(_staffClinicRepoMock.Object, _clinicRepoMock.Object);
        }

        // ─────────────────────────────────────────────────────────────────
        // Repository helpers
        // ─────────────────────────────────────────────────────────────────

        private void SetupStaffClinicRepo(StaffClinic? staffClinic)
        {
            var list = staffClinic != null
                ? new List<StaffClinic> { staffClinic }
                : new List<StaffClinic>();
            _staffClinicRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<StaffClinic, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<StaffClinic>().Object);
        }

        private void SetupClinicRepo(Clinic? clinic)
        {
            var list = clinic != null
                ? new List<Clinic> { clinic }
                : new List<Clinic>();
            _clinicRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Clinic, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<Clinic>().Object);
        }

        // ==================================================================
        // ====================== Process(...) tests ========================
        // ==================================================================

        /// <summary>
        /// TC-CSS-01: Valid receptionist with active clinic → returns all 3 shifts
        /// (Morning, Afternoon, Evening) ordered by StartTime.
        /// Clinic opens 08:00, closes 20:00 → covers full shift ranges.
        /// </summary>
        [Fact]
        public async Task Process_ValidReceptionistWithFullHoursClinic_ReturnsAllThreeShiftsOrderedByStartTime()
        {
            //Arrange 1
            var staffClinic = ClinicShiftMockData.GetActiveStaffClinic();
            var clinic = ClinicShiftMockData.GetActiveClinic_StandardHours();
            var expectedMessage = GeneralCode.APP_MESSAGE_2000.ToString();

            //Arrange 2
            SetupStaffClinicRepo(staffClinic);
            SetupClinicRepo(clinic);

            //Act
            var result = await _sut.Process(ClinicShiftMockData.ReceptionistUserId);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(expectedMessage);
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(3);
            result.Data![0].ShiftType.Should().Be(ShiftType.MORNING);
            result.Data![1].ShiftType.Should().Be(ShiftType.AFTERNOON);
            result.Data![2].ShiftType.Should().Be(ShiftType.EVENING);
            result.Data.Should().BeInAscendingOrder(s => s.StartTime);
            result.Data[0].StartTime.Should().Be(new TimeOnly(8, 0));
            result.Data[0].EndTime.Should().Be(new TimeOnly(12, 0));
            result.Data[1].StartTime.Should().Be(new TimeOnly(12, 0));
            result.Data[1].EndTime.Should().Be(new TimeOnly(17, 0));
            result.Data[2].StartTime.Should().Be(new TimeOnly(17, 0));
            result.Data[2].EndTime.Should().Be(new TimeOnly(20, 0));
            _staffClinicRepoMock.Verify(r => r.FindByCondition(
                It.IsAny<Expression<Func<StaffClinic, bool>>>(),
                It.IsAny<bool>()), Times.Once);
            _clinicRepoMock.Verify(r => r.FindByCondition(
                It.IsAny<Expression<Func<Clinic, bool>>>(),
                It.IsAny<bool>()), Times.Once);
        }

        /// <summary>
        /// TC-CSS-02: Receptionist's StaffClinic not found in database
        /// → ResolveReceptionistClinicAsync throws KeyNotFoundException(APP_MESSAGE_4008).
        /// Covers: line 65-66 (staffClinic is null check).
        /// </summary>
        [Fact]
        public async Task Process_ReceptionistStaffClinicNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var expectedMessage = GeneralCode.APP_MESSAGE_4008.ToString();

            //Arrange 2
            SetupStaffClinicRepo(null);

            //Act
            var act = () => _sut.Process(ClinicShiftMockData.ReceptionistUserId);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(expectedMessage);
            _staffClinicRepoMock.Verify(r => r.FindByCondition(
                It.IsAny<Expression<Func<StaffClinic, bool>>>(),
                It.IsAny<bool>()), Times.Once);
            _clinicRepoMock.Verify(r => r.FindByCondition(
                It.IsAny<Expression<Func<Clinic, bool>>>(),
                It.IsAny<bool>()), Times.Never);
        }

        /// <summary>
        /// TC-CSS-03: Clinic not found or inactive for receptionist
        /// → ResolveReceptionistClinicAsync throws KeyNotFoundException(APP_MESSAGE_4008).
        /// Covers: line 70-71 (clinic is null check).
        /// </summary>
        [Fact]
        public async Task Process_ClinicNotFoundOrInactive_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var staffClinic = ClinicShiftMockData.GetActiveStaffClinic();
            var expectedMessage = GeneralCode.APP_MESSAGE_4008.ToString();

            //Arrange 2
            SetupStaffClinicRepo(staffClinic);
            SetupClinicRepo(null);

            //Act
            var act = () => _sut.Process(ClinicShiftMockData.ReceptionistUserId);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(expectedMessage);
            _staffClinicRepoMock.Verify(r => r.FindByCondition(
                It.IsAny<Expression<Func<StaffClinic, bool>>>(),
                It.IsAny<bool>()), Times.Once);
            _clinicRepoMock.Verify(r => r.FindByCondition(
                It.IsAny<Expression<Func<Clinic, bool>>>(),
                It.IsAny<bool>()), Times.Once);
        }

        /// <summary>
        /// TC-CSS-04: Clinic with partial hours (07:30-17:30)
        /// → Evening shift clipped to clinic close time (17:00-17:30).
        /// Covers: MapToShiftRangeItems ordering and shift range calculation.
        /// </summary>
        [Fact]
        public async Task Process_ClinicWithPartialHours_ReturnsClippedEveningShift()
        {
            //Arrange 1
            var staffClinic = ClinicShiftMockData.GetActiveStaffClinic();
            var clinic = ClinicShiftMockData.GetActiveClinic_PartialHours();

            //Arrange 2
            SetupStaffClinicRepo(staffClinic);
            SetupClinicRepo(clinic);

            //Act
            var result = await _sut.Process(ClinicShiftMockData.ReceptionistUserId);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(3);
            result.Data.Should().BeInAscendingOrder(s => s.StartTime);
            result.Data[2].ShiftType.Should().Be(ShiftType.EVENING);
            result.Data[2].StartTime.Should().Be(new TimeOnly(17, 0));
            result.Data[2].EndTime.Should().Be(new TimeOnly(17, 30));
        }

        /// <summary>
        /// TC-CSS-05: Clinic open only morning hours (07:00-12:00)
        /// → Only Morning shift available (08:00-12:00).
        /// Covers: AddShift logic that excludes non-overlapping shifts.
        /// </summary>
        [Fact]
        public async Task Process_ClinicMorningOnlyHours_ReturnsOnlyMorningShift()
        {
            //Arrange 1
            var staffClinic = ClinicShiftMockData.GetActiveStaffClinic();
            var clinic = ClinicShiftMockData.GetActiveClinic_MorningOnly();

            //Arrange 2
            SetupStaffClinicRepo(staffClinic);
            SetupClinicRepo(clinic);

            //Act
            var result = await _sut.Process(ClinicShiftMockData.ReceptionistUserId);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data![0].ShiftType.Should().Be(ShiftType.MORNING);
            result.Data[0].StartTime.Should().Be(new TimeOnly(8, 0));
            result.Data[0].EndTime.Should().Be(new TimeOnly(12, 0));
        }

        /// <summary>
        /// TC-CSS-06: Clinic open only afternoon hours (12:00-17:00)
        /// → Only Afternoon shift available (12:00-17:00).
        /// Covers: Afternoon-only scenario without Morning or Evening.
        /// </summary>
        [Fact]
        public async Task Process_ClinicAfternoonOnlyHours_ReturnsOnlyAfternoonShift()
        {
            //Arrange 1
            var staffClinic = ClinicShiftMockData.GetActiveStaffClinic();
            var clinic = ClinicShiftMockData.GetActiveClinic_AfternoonOnly();

            //Arrange 2
            SetupStaffClinicRepo(staffClinic);
            SetupClinicRepo(clinic);

            //Act
            var result = await _sut.Process(ClinicShiftMockData.ReceptionistUserId);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data![0].ShiftType.Should().Be(ShiftType.AFTERNOON);
            result.Data[0].StartTime.Should().Be(new TimeOnly(12, 0));
            result.Data[0].EndTime.Should().Be(new TimeOnly(17, 0));
        }

        /// <summary>
        /// TC-CSS-07: Response Data is a new list instance (not null)
        /// → Verifies CreateSuccessResponse creates fresh list.
        /// </summary>
        [Fact]
        public async Task Process_ValidClinic_ReturnsNonNullDataList()
        {
            //Arrange 1
            var staffClinic = ClinicShiftMockData.GetActiveStaffClinic();
            var clinic = ClinicShiftMockData.GetActiveClinic_StandardHours();

            //Arrange 2
            SetupStaffClinicRepo(staffClinic);
            SetupClinicRepo(clinic);

            //Act
            var result = await _sut.Process(ClinicShiftMockData.ReceptionistUserId);

            //Assert
            result.Data.Should().NotBeNull();
            result.Data.Should().BeAssignableTo<List<ShiftRangeItem>>();
        }
    }
}
