using System.Linq.Expressions;
using ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetAvailableSlotsServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.ReceptionistManagementServices.ReceptionistGetAvailableSlotsServices
{
    public class ReceptionistGetAvailableSlotsServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffQueryRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<DoctorSchedule, Guid, AppDbContext>> _scheduleQueryRepoMock = new();

        private readonly GetAvailableSlotsService _sut;

        public ReceptionistGetAvailableSlotsServiceTests()
        {
            _sut = new GetAvailableSlotsService(
                _staffQueryRepoMock.Object,
                _scheduleQueryRepoMock.Object);
        }

        private void SetupStaffRepository(List<StaffClinic> staffList)
        {
            var mockDbSet = staffList.BuildMockDbSet();

            _staffQueryRepoMock
                .Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<StaffClinic, bool>>>(),
                    It.IsAny<bool>()
                ))
                .Returns((Expression<Func<StaffClinic, bool>> predicate, bool _) =>
                    mockDbSet.Object.Where(predicate));
        }

        private void SetupScheduleRepository(List<DoctorSchedule> schedules)
        {
            var mockDbSet = schedules.BuildMockDbSet();

            _scheduleQueryRepoMock
                .Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<DoctorSchedule, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Expression<Func<DoctorSchedule, object>>[]>()
                ))
                .Returns((Expression<Func<DoctorSchedule, bool>> predicate, bool _, Expression<Func<DoctorSchedule, object>>[] _) =>
                    mockDbSet.Object.Where(predicate));
        }

        [Fact]
        public async Task Process_ReceptionistNotAssignedToClinic_ThrowsUnauthorizedAccessException()
        {
            SetupStaffRepository(new List<StaffClinic>());
            var request = ReceptionistGetAvailableSlotsMockData.GetValidRequest();

            Func<Task> act = async () => await _sut.Process(request);

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task Process_ValidRequest_ReturnsMappedSchedulesSuccessfully()
        {
            var staff = ReceptionistGetAvailableSlotsMockData.GetActiveStaffClinic();
            SetupStaffRepository(new List<StaffClinic> { staff });

            var schedule = ReceptionistGetAvailableSlotsMockData.GetDoctorSchedule(DateTime.Today);
            SetupScheduleRepository(new List<DoctorSchedule> { schedule });

            var request = ReceptionistGetAvailableSlotsMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.Should().NotBeNull();
            result.CodeMessage.Should().Be("APP_MESSAGE_2000");
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);

            var firstResponse = result.Data.First();
            firstResponse.DoctorId.Should().Be(schedule.DoctorId.ToString());
            firstResponse.DoctorName.Should().Be("John Doe");
            firstResponse.SpecialtyName.Should().Be("Ophthalmology");
            firstResponse.RoomName.Should().Be("Room 101");
            firstResponse.Slots.Should().HaveCount(1);
            firstResponse.Slots.First().Status.Should().Be("AVAILABLE");
        }

        [Fact]
        public async Task Process_SlotStartTimePassedMoreThan30Mins_StatusChangesToBlocked()
        {
            var staff = ReceptionistGetAvailableSlotsMockData.GetActiveStaffClinic();
            SetupStaffRepository(new List<StaffClinic> { staff });

            var pastSlotStartTime = DateTime.UtcNow.AddMinutes(-40);
            var schedule = ReceptionistGetAvailableSlotsMockData.GetDoctorSchedule(
                workDate: DateTime.Today,
                slotStartTime: pastSlotStartTime);

            SetupScheduleRepository(new List<DoctorSchedule> { schedule });

            var request = ReceptionistGetAvailableSlotsMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data.First().Slots.First().Status.Should().Be("BLOCKED");
        }

        [Fact]
        public async Task Process_SearchDoctorFilter_AppliesFilterCorrectly()
        {
            var staff = ReceptionistGetAvailableSlotsMockData.GetActiveStaffClinic();
            SetupStaffRepository(new List<StaffClinic> { staff });

            var schedule = ReceptionistGetAvailableSlotsMockData.GetDoctorSchedule(DateTime.Today);
            SetupScheduleRepository(new List<DoctorSchedule> { schedule });

            var request = ReceptionistGetAvailableSlotsMockData.GetValidRequest();
            request.SearchDoctor = "john";

            var result = await _sut.Process(request);

            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
        }

        [Fact]
        public async Task Process_FilterByShiftTypeAndSpecialty_ReturnsMatchingSchedules()
        {
            var staff = ReceptionistGetAvailableSlotsMockData.GetActiveStaffClinic();
            SetupStaffRepository(new List<StaffClinic> { staff });

            var schedule = ReceptionistGetAvailableSlotsMockData.GetDoctorSchedule(
                workDate: DateTime.Today,
                shiftType: ShiftType.MORNING);

            SetupScheduleRepository(new List<DoctorSchedule> { schedule });

            var request = ReceptionistGetAvailableSlotsMockData.GetValidRequest();
            request.ShiftType = "MORNING";
            request.SpecialtyId = ReceptionistGetAvailableSlotsMockData.SpecialtyId.ToString();

            var result = await _sut.Process(request);

            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
        }

        [Fact]
        public async Task Process_FilterBySpecialtyAll_IgnoresSpecialtyFilter()
        {
            var staff = ReceptionistGetAvailableSlotsMockData.GetActiveStaffClinic();
            SetupStaffRepository(new List<StaffClinic> { staff });

            var schedule = ReceptionistGetAvailableSlotsMockData.GetDoctorSchedule(DateTime.Today);
            SetupScheduleRepository(new List<DoctorSchedule> { schedule });

            var request = ReceptionistGetAvailableSlotsMockData.GetValidRequest();
            request.SpecialtyId = "All";

            var result = await _sut.Process(request);

            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
        }

        [Fact]
        public async Task Process_DoctorSpecialtyAndRoomAreNull_ReturnsDefaultFallbackStrings()
        {
            // Arrange
            var staff = ReceptionistGetAvailableSlotsMockData.GetActiveStaffClinic();
            SetupStaffRepository(new List<StaffClinic> { staff });

            var schedule = ReceptionistGetAvailableSlotsMockData.GetDoctorSchedule(DateTime.Today);
            // Nullify Specialty and Room to cover null-branch conditions
            schedule.Doctor.Specialty = null;
            schedule.Room = null;

            SetupScheduleRepository(new List<DoctorSchedule> { schedule });

            var request = ReceptionistGetAvailableSlotsMockData.GetValidRequest();

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            var firstResponse = result.Data!.First();
            firstResponse.SpecialtyName.Should().Be("Chưa phân khoa");
            firstResponse.RoomName.Should().Be("Chưa gán phòng trực");
            firstResponse.RoomId.Should().BeNull();
        }

        [Fact]
        public async Task Process_TimeSlotsIsNull_ReturnsEmptySlotsList()
        {
            // Arrange
            var staff = ReceptionistGetAvailableSlotsMockData.GetActiveStaffClinic();
            SetupStaffRepository(new List<StaffClinic> { staff });

            var schedule = ReceptionistGetAvailableSlotsMockData.GetDoctorSchedule(DateTime.Today);
            // Nullify TimeSlots collection to test null-coalescing operator (??)
            schedule.TimeSlots = null;

            SetupScheduleRepository(new List<DoctorSchedule> { schedule });

            var request = ReceptionistGetAvailableSlotsMockData.GetValidRequest();

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.Data!.First().Slots.Should().BeEmpty();
        }

        [Fact]
        public async Task Process_SlotStatusNotAvailable_KeepsOriginalStatusWithoutBlocking()
        {
            // Arrange
            var staff = ReceptionistGetAvailableSlotsMockData.GetActiveStaffClinic();
            SetupStaffRepository(new List<StaffClinic> { staff });

            var pastSlotStartTime = DateTime.UtcNow.AddMinutes(-40);
            var schedule = ReceptionistGetAvailableSlotsMockData.GetDoctorSchedule(
                workDate: DateTime.Today,
                slotStartTime: pastSlotStartTime);

            // Set status to BOOKED instead of AVAILABLE
            schedule.TimeSlots!.First().Status = SlotStatus.BOOKED;

            SetupScheduleRepository(new List<DoctorSchedule> { schedule });

            var request = ReceptionistGetAvailableSlotsMockData.GetValidRequest();

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.Data!.First().Slots.First().Status.Should().Be("BOOKED");
        }

        [Fact]
        public async Task Process_SlotStatusAvailableAndNotExpired_KeepsAvailableStatus()
        {
            // Arrange
            var staff = ReceptionistGetAvailableSlotsMockData.GetActiveStaffClinic();
            SetupStaffRepository(new List<StaffClinic> { staff });

            var futureSlotStartTime = DateTime.UtcNow.AddHours(1);
            var schedule = ReceptionistGetAvailableSlotsMockData.GetDoctorSchedule(
                workDate: DateTime.Today,
                slotStartTime: futureSlotStartTime);

            SetupScheduleRepository(new List<DoctorSchedule> { schedule });

            var request = ReceptionistGetAvailableSlotsMockData.GetValidRequest();

            // Act
            var result = await _sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.Data!.First().Slots.First().Status.Should().Be("AVAILABLE");
        }
    }
}