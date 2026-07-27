using System.Linq.Expressions;
using ECS.Application.Common.Response;
using ECS.Application.Services.PatientAppointmentManagementServices.ClinicSlotServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.PatientAppointmentManagementServices.ClinicSlotServices
{
    /// <summary>
    /// Unit tests for <see cref="ClinicSlotService"/>.
    /// Target: 100% Line Coverage & 100% Branch Coverage.
    /// Naming pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// </summary>
    public class ClinicSlotServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<Clinic, Guid, AppDbContext>> _clinicRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepoMock = new();
        private readonly Mock<IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext>> _slotRepoMock = new();

        private static readonly Guid ValidClinicId = ClinicSlotMockData.ValidClinicId;
        private static readonly Guid ValidDoctorId = ClinicSlotMockData.ValidDoctorId;

        private ClinicSlotService CreateSut()
        {
            return new ClinicSlotService(
                _clinicRepoMock.Object,
                _doctorRepoMock.Object,
                _slotRepoMock.Object);
        }

        private void SetupClinics(IEnumerable<Clinic> clinics)
        {
            _clinicRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Clinic, bool>>>(),
                    It.IsAny<bool>()))
                .Returns((Expression<Func<Clinic, bool>> expression, bool trackChanges) =>
                {
                    var filtered = clinics.AsQueryable().Where(expression).ToList();
                    return filtered.BuildMockDbSet<Clinic>().Object;
                });
        }

        private void SetupDoctors(IEnumerable<DoctorProfile> doctors)
        {
            _doctorRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<DoctorProfile, bool>>>(),
                    It.IsAny<bool>()))
                .Returns((Expression<Func<DoctorProfile, bool>> expression, bool trackChanges) =>
                {
                    var filtered = doctors.AsQueryable().Where(expression).ToList();
                    return filtered.BuildMockDbSet<DoctorProfile>().Object;
                });
        }

        private void SetupSlots(IEnumerable<TimeSlot> slots)
        {
            _slotRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<TimeSlot, bool>>>(),
                    It.IsAny<bool>()))
                .Returns((Expression<Func<TimeSlot, bool>> expression, bool trackChanges) =>
                {
                    var filtered = slots.AsQueryable().Where(expression).ToList();
                    return filtered.BuildMockDbSet<TimeSlot>().Object;
                });
        }

        // ==================================================================
        // ====================== Process(...) Tests ========================
        // ==================================================================

        /// <summary>
        /// TC-CS-01: Clinic không tồn tại hoặc IsActive = false -> Trả về lỗi 4045
        /// </summary>
        [Fact]
        public async Task Process_ClinicNotFoundOrInactive_ReturnsFail4045()
        {
            // Arrange
            var sut = CreateSut();
            SetupClinics(Enumerable.Empty<Clinic>());

            // Act
            var result = await sut.Process(ValidClinicId, "2026-08-10");

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4045.ToString());
            result.Data.Should().BeNull();

            _doctorRepoMock.Verify(r => r.FindByCondition(
                It.IsAny<Expression<Func<DoctorProfile, bool>>>(),
                It.IsAny<bool>()), Times.Never);
        }

        /// <summary>
        /// TC-CS-02: Chuỗi ngày truyền vào không đúng định dạng -> Trả về lỗi 4052
        /// </summary>
        [Fact]
        public async Task Process_InvalidDateFormat_ReturnsFail4052()
        {
            // Arrange
            var sut = CreateSut();
            var clinic = ClinicSlotMockData.GetClinic();
            SetupClinics(new[] { clinic });
            SetupDoctors(Enumerable.Empty<DoctorProfile>());

            // Act
            var result = await sut.Process(ValidClinicId, "2026-13-45");

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4052.ToString());
            result.Data.Should().BeNull();
        }

        /// <summary>
        /// TC-CS-03: Phòng khám có sẵn nhưng không có Bác sĩ nào hoạt động
        /// </summary>
        [Fact]
        public async Task Process_ClinicHasNoActiveDoctors_ReturnsSlotsWithNoAvailableDoctor()
        {
            // Arrange
            var sut = CreateSut();
            var clinic = ClinicSlotMockData.GetClinic(openTime: new TimeOnly(8, 0), closeTime: new TimeOnly(9, 0));
            SetupClinics(new[] { clinic });
            SetupDoctors(Enumerable.Empty<DoctorProfile>());

            // Act
            var result = await sut.Process(ValidClinicId, "2026-08-10");

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2001.ToString());
            result.Data.Should().HaveCount(2);

            var firstSlot = result.Data!.First();
            firstSlot.StartTime.Should().Be("08:00");
            firstSlot.EndTime.Should().Be("08:30");
            firstSlot.IsAvailable.Should().BeFalse();
            firstSlot.HasAvailableDoctor.Should().BeFalse();
            firstSlot.DoctorId.Should().BeNull();

            _slotRepoMock.Verify(r => r.FindByCondition(
                It.IsAny<Expression<Func<TimeSlot, bool>>>(),
                It.IsAny<bool>()), Times.Never);
        }

        /// <summary>
        /// TC-CS-04: Phòng khám & Bác sĩ hợp lệ nhưng không tìm thấy Slot nào khả dụng trong DB
        /// </summary>
        [Fact]
        public async Task Process_NoAvailableSlotsInDb_ReturnsSlotsWithIsAvailableFalse()
        {
            // Arrange
            var sut = CreateSut();
            var clinic = ClinicSlotMockData.GetClinic(openTime: new TimeOnly(8, 0), closeTime: new TimeOnly(8, 30));
            var doctor = ClinicSlotMockData.GetDoctor();

            SetupClinics(new[] { clinic });
            SetupDoctors(new[] { doctor });
            SetupSlots(Enumerable.Empty<TimeSlot>());

            // Act
            var result = await sut.Process(ValidClinicId, "2026-08-10");

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2001.ToString());
            result.Data.Should().HaveCount(1);
            result.Data!.First().IsAvailable.Should().BeFalse();
            result.Data!.First().DoctorId.Should().BeNull();
        }

        /// <summary>
        /// TC-CS-05: Lấy danh sách slot thành công với Bác sĩ khả dụng
        /// </summary>
        [Fact]
        public async Task Process_ValidClinicDateAndSlotAvailable_ReturnsSuccessWithDoctorId()
        {
            // Arrange
            var sut = CreateSut();
            var dateStr = "2026-08-10";
            var selectedDate = DateTime.Parse(dateStr);

            var clinic = ClinicSlotMockData.GetClinic(openTime: new TimeOnly(8, 0), closeTime: new TimeOnly(8, 30));
            var doctor = ClinicSlotMockData.GetDoctor();

            var slotStartTime = selectedDate.Add(new TimeSpan(8, 0, 0));
            var timeSlot = ClinicSlotMockData.GetTimeSlot(doctorId: ValidDoctorId, startTime: slotStartTime, maxPatients: 2, currentPatients: 0);

            SetupClinics(new[] { clinic });
            SetupDoctors(new[] { doctor });
            SetupSlots(new[] { timeSlot });

            // Act
            var result = await sut.Process(ValidClinicId, dateStr);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2001.ToString());
            result.Data.Should().HaveCount(1);

            var slot = result.Data!.First();
            slot.Id.Should().Be($"clinic_{ValidClinicId}_2026-08-10_08:00");
            slot.StartTime.Should().Be("08:00");
            slot.EndTime.Should().Be("08:30");
            slot.IsAvailable.Should().BeTrue();
            slot.HasAvailableDoctor.Should().BeTrue();
            slot.DoctorId.Should().Be(ValidDoctorId);
            slot.ClinicId.Should().Be(ValidClinicId);
            slot.Date.Should().Be(dateStr);
        }

        /// <summary>
        /// TC-CS-06: Truyền serviceId tùy chọn (Optional parameter)
        /// </summary>
        [Fact]
        public async Task Process_WithServiceId_ExecutesSuccessfully()
        {
            // Arrange
            var sut = CreateSut();
            var serviceId = Guid.NewGuid();
            var clinic = ClinicSlotMockData.GetClinic();

            SetupClinics(new[] { clinic });
            SetupDoctors(Enumerable.Empty<DoctorProfile>());

            // Act
            var result = await sut.Process(ValidClinicId, "2026-08-10", serviceId);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2001.ToString());
        }

        /// <summary>
        /// TC-CS-07: Khung giờ hoạt động không chia hết cho 30 phút (VD: 08:00 -> 08:45)
        /// </summary>
        [Fact]
        public async Task Process_WorkingHoursWithIncompleteEndSlot_IgnoresOverlappingSlot()
        {
            // Arrange
            var sut = CreateSut();
            var clinic = ClinicSlotMockData.GetClinic(openTime: new TimeOnly(8, 0), closeTime: new TimeOnly(8, 45));

            SetupClinics(new[] { clinic });
            SetupDoctors(Enumerable.Empty<DoctorProfile>());

            // Act
            var result = await sut.Process(ValidClinicId, "2026-08-10");

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data!.First().StartTime.Should().Be("08:00");
            result.Data!.First().EndTime.Should().Be("08:30");
        }

        /// <summary>
        /// TC-CS-08: TimeSlot bị đầy (MaxPatients == CurrentPatients) hoặc Status != SlotStatus.AVAILABLE
        /// </summary>
        [Fact]
        public async Task Process_SlotIsFullOrNotAvailableStatus_ReturnsDoctorNotAvailable()
        {
            // Arrange
            var sut = CreateSut();
            var dateStr = "2026-08-10";
            var selectedDate = DateTime.Parse(dateStr);

            var clinic = ClinicSlotMockData.GetClinic(openTime: new TimeOnly(8, 0), closeTime: new TimeOnly(8, 30));
            var doctor = ClinicSlotMockData.GetDoctor();

            var slotStartTime = selectedDate.Add(new TimeSpan(8, 0, 0));
            var fullSlot = ClinicSlotMockData.GetTimeSlot(doctorId: ValidDoctorId, startTime: slotStartTime, status: SlotStatus.BOOKED, maxPatients: 1, currentPatients: 1);

            SetupClinics(new[] { clinic });
            SetupDoctors(new[] { doctor });
            SetupSlots(new[] { fullSlot });

            // Act
            var result = await sut.Process(ValidClinicId, dateStr);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data!.First().IsAvailable.Should().BeFalse();
        }
    }
}