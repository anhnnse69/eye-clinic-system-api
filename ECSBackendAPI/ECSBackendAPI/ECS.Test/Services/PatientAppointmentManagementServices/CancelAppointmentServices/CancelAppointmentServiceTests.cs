using System.Linq.Expressions;
using ECS.Application.Common.Response;
using ECS.Application.Services.PatientAppointmentManagementServices.CancelAppointmentServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Storage;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.PatientAppointmentManagementServices.CancelAppointmentServices
{
    /// <summary>
    /// Unit tests for <see cref="CancelAppointmentService"/>.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// Goal: 100% code coverage on <c>CancelAppointmentService.cs</c>.
    /// </summary>
    public class CancelAppointmentServiceTests
    {
        private static readonly Guid ValidUserId = CancelAppointmentMockData.ValidUserId;
        private static readonly Guid OtherUserId = CancelAppointmentMockData.OtherUserId;
        private static readonly Guid ValidAppointmentId = CancelAppointmentMockData.ValidAppointmentId;
        private static readonly Guid ValidSlotId = CancelAppointmentMockData.ValidSlotId;

        private readonly Mock<IRepositoryBaseAsync<Appointment, Guid, AppDbContext>> _appointmentRepoMock = new();
        private readonly Mock<IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext>> _slotRepoMock = new();
        private readonly Mock<IDbContextTransaction> _transactionMock = new();

        public CancelAppointmentServiceTests()
        {
            // Mặc định setup Transaction thành công
            _appointmentRepoMock
                .Setup(r => r.BeginTransactionAsync())
                .ReturnsAsync(_transactionMock.Object);
        }

        private CancelAppointmentService CreateSut(IHttpContextAccessor httpContextAccessor)
        {
            return new CancelAppointmentService(
                _appointmentRepoMock.Object,
                _slotRepoMock.Object,
                httpContextAccessor);
        }

        private void SetupAppointments(IEnumerable<Appointment> appointments)
        {
            var list = appointments.ToList();
            _appointmentRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<Appointment>().Object);
        }

        private void SetupSlots(IEnumerable<TimeSlot> slots)
        {
            var list = slots.ToList();
            _slotRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<TimeSlot, bool>>>(), It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<TimeSlot>().Object);
        }

        // ==================================================================
        // ====================== Process(...) Tests ========================
        // ==================================================================

        /// <summary>
        /// TC-CA-01: User claim không hợp lệ / không đăng nhập → Trả về lỗi APP_MESSAGE_4001.
        /// </summary>
        [Fact]
        public async Task CancelAppointment_UserNotAuthenticated_ReturnsFail4001()
        {
            // Arrange
            var httpContextMock = CancelAppointmentMockData.GetHttpContextAccessorMock(isAuthenticated: false);
            var sut = CreateSut(httpContextMock.Object);
            var request = CancelAppointmentMockData.GetValidRequest();

            SetupAppointments(Enumerable.Empty<Appointment>());

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
        }

        /// <summary>
        /// TC-CA-01B: User không đăng nhập / claim lỗi NHƯNG Lịch hẹn TỒN TẠI trong DB
        /// -> Phủ 100% dòng code 'if (!isUserValid)' ở cả Step 3 và Step 5.
        /// </summary>
        [Fact]
        public async Task CancelAppointment_UserNotAuthenticated_WithExistingAppointment_ReturnsFail4001()
        {
            // Arrange
            var httpContextMock = CancelAppointmentMockData.GetHttpContextAccessorMock(isAuthenticated: false);
            var sut = CreateSut(httpContextMock.Object);
            var request = CancelAppointmentMockData.GetValidRequest();

            // Setup Appointment TỒN TẠI để isAppointmentExist = true
            var appointment = CancelAppointmentMockData.GetAppointment();
            SetupAppointments(new[] { appointment });

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();

            // Verify không thực hiện update hay commit db
            _appointmentRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Appointment>()), Times.Never);
            _transactionMock.Verify(t => t.CommitAsync(default), Times.Never);
        }

        /// <summary>
        /// TC-CA-02: Không tìm thấy Lịch hẹn theo AppointmentId → Trả về lỗi APP_MESSAGE_4046.
        /// </summary>
        [Fact]
        public async Task CancelAppointment_AppointmentNotFound_ReturnsFail4046()
        {
            // Arrange
            var httpContextMock = CancelAppointmentMockData.GetHttpContextAccessorMock(ValidUserId);
            var sut = CreateSut(httpContextMock.Object);
            var request = CancelAppointmentMockData.GetValidRequest();

            SetupAppointments(Enumerable.Empty<Appointment>());

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4046.ToString());
            result.Data.Should().BeNull();
        }

        /// <summary>
        /// TC-CA-03: Lịch hẹn không thuộc về PatientId hoặc CreatedById của user hiện tại → Trả về lỗi APP_MESSAGE_4053.
        /// </summary>
        [Fact]
        public async Task CancelAppointment_UserHasNoPermission_ReturnsFail4053()
        {
            // Arrange
            var httpContextMock = CancelAppointmentMockData.GetHttpContextAccessorMock(ValidUserId);
            var sut = CreateSut(httpContextMock.Object);
            var request = CancelAppointmentMockData.GetValidRequest();

            var appointment = CancelAppointmentMockData.GetAppointment(
                patientId: OtherUserId,
                createdById: OtherUserId);

            SetupAppointments(new[] { appointment });

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4053.ToString());
            result.Data.Should().BeNull();
        }

        /// <summary>
        /// TC-CA-04: Lịch hẹn đã bị hủy trước đó (Status = CANCELLED) → Trả về lỗi APP_MESSAGE_4050.
        /// </summary>
        [Fact]
        public async Task CancelAppointment_AlreadyCancelled_ReturnsFail4050()
        {
            // Arrange
            var httpContextMock = CancelAppointmentMockData.GetHttpContextAccessorMock(ValidUserId);
            var sut = CreateSut(httpContextMock.Object);
            var request = CancelAppointmentMockData.GetValidRequest();

            var appointment = CancelAppointmentMockData.GetAppointment(status: AppointmentStatus.CANCELLED);
            SetupAppointments(new[] { appointment });

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4050.ToString());
        }

        /// <summary>
        /// TC-CA-05: Lịch hẹn đã hoàn thành (Status = COMPLETED) → Trả về lỗi APP_MESSAGE_4050.
        /// </summary>
        [Fact]
        public async Task CancelAppointment_AlreadyCompleted_ReturnsFail4050()
        {
            // Arrange
            var httpContextMock = CancelAppointmentMockData.GetHttpContextAccessorMock(ValidUserId);
            var sut = CreateSut(httpContextMock.Object);
            var request = CancelAppointmentMockData.GetValidRequest();

            var appointment = CancelAppointmentMockData.GetAppointment(status: AppointmentStatus.COMPLETED);
            SetupAppointments(new[] { appointment });

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4050.ToString());
        }

        /// <summary>
        /// TC-CA-06: Lịch hẹn đang khám (Status = IN_PROGRESS) → Trả về lỗi APP_MESSAGE_4050.
        /// </summary>
        [Fact]
        public async Task CancelAppointment_InProgress_ReturnsFail4050()
        {
            // Arrange
            var httpContextMock = CancelAppointmentMockData.GetHttpContextAccessorMock(ValidUserId);
            var sut = CreateSut(httpContextMock.Object);
            var request = CancelAppointmentMockData.GetValidRequest();

            var appointment = CancelAppointmentMockData.GetAppointment(status: AppointmentStatus.IN_PROGRESS);
            SetupAppointments(new[] { appointment });

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4050.ToString());
        }

        /// <summary>
        /// TC-CA-07: Hủy lịch sát giờ khám (dưới 24h) → Trả về lỗi APP_MESSAGE_4050.
        /// </summary>
        [Fact]
        public async Task CancelAppointment_LessThan24HoursBefore_ReturnsFail4050()
        {
            // Arrange
            var httpContextMock = CancelAppointmentMockData.GetHttpContextAccessorMock(ValidUserId);
            var sut = CreateSut(httpContextMock.Object);
            var request = CancelAppointmentMockData.GetValidRequest();

            var appointment = CancelAppointmentMockData.GetAppointment(
                appointmentDate: DateTime.Now.AddHours(10)); // Còn 10h nữa là khám
            SetupAppointments(new[] { appointment });

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4050.ToString());
        }

        /// <summary>
        /// TC-CA-08: Lịch hẹn có trạng thái không phù hợp để hủy (Ví dụ trạng thái không phải PENDING) → Trả về lỗi APP_MESSAGE_4050.
        /// </summary>
        [Fact]
        public async Task CancelAppointment_NotPendingStatus_ReturnsFail4050()
        {
            // Arrange
            var httpContextMock = CancelAppointmentMockData.GetHttpContextAccessorMock(ValidUserId);
            var sut = CreateSut(httpContextMock.Object);
            var request = CancelAppointmentMockData.GetValidRequest();

            // Giả lập 1 status bất kỳ ngoài PENDING (chẳng hạn CONFIRMED nếu có Enum)
            var appointment = CancelAppointmentMockData.GetAppointment(status: (AppointmentStatus)999);
            SetupAppointments(new[] { appointment });

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4050.ToString());
        }

        /// <summary>
        /// TC-CA-09: Hủy thành công với lý do tùy chỉnh → Đổi trạng thái thành CANCELLED, giải phóng Slot và commit transaction.
        /// </summary>
        [Fact]
        public async Task CancelAppointment_ValidRequestWithCustomReason_ReturnsSuccess2004()
        {
            // Arrange
            var httpContextMock = CancelAppointmentMockData.GetHttpContextAccessorMock(ValidUserId);
            var sut = CreateSut(httpContextMock.Object);
            var customReason = "Cần chuyển ngày khám khác";
            var request = CancelAppointmentMockData.GetValidRequest(reason: customReason);

            var appointment = CancelAppointmentMockData.GetAppointment();
            var slot = CancelAppointmentMockData.GetTimeSlot(currentPatients: 2, maxPatients: 2, status: SlotStatus.BOOKED);

            SetupAppointments(new[] { appointment });
            SetupSlots(new[] { slot });

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2004.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.AppointmentId.Should().Be(appointment.Id);
            result.Data.Status.Should().Be(AppointmentStatus.CANCELLED.ToString());
            result.Data.CancellationReason.Should().Be(customReason);

            // Verify db updates
            _appointmentRepoMock.Verify(r => r.UpdateAsync(It.Is<Appointment>(a =>
                a.Status == AppointmentStatus.CANCELLED &&
                a.NoteReason == customReason)), Times.Once);

            _slotRepoMock.Verify(r => r.UpdateAsync(It.Is<TimeSlot>(s =>
                s.CurrentPatients == 1 &&
                s.Status == SlotStatus.AVAILABLE)), Times.Once);

            _transactionMock.Verify(t => t.CommitAsync(default), Times.Once);
        }

        /// <summary>
        /// TC-CA-10: Hủy thành công với Reason null → Sử dụng lý do mặc định "Bệnh nhân hủy lịch hẹn".
        /// </summary>
        [Fact]
        public async Task CancelAppointment_NullReason_UsesDefaultReasonAndReturnsSuccess()
        {
            // Arrange
            var httpContextMock = CancelAppointmentMockData.GetHttpContextAccessorMock(ValidUserId);
            var sut = CreateSut(httpContextMock.Object);
            var request = CancelAppointmentMockData.GetValidRequest(reason: null);

            var appointment = CancelAppointmentMockData.GetAppointment();
            var slot = CancelAppointmentMockData.GetTimeSlot();

            SetupAppointments(new[] { appointment });
            SetupSlots(new[] { slot });

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2004.ToString());
            result.Data!.CancellationReason.Should().Be("Bệnh nhân hủy lịch hẹn");
        }

        /// <summary>
        /// TC-CA-11: Lịch hẹn hợp lệ nhưng SlotId không tồn tại trong DB → Vẫn cập nhật Lịch hẹn thành công (ReleaseSlot bỏ qua).
        /// </summary>
        [Fact]
        public async Task CancelAppointment_SlotNotFound_CancelsAppointmentWithoutUpdatingSlot()
        {
            // Arrange
            var httpContextMock = CancelAppointmentMockData.GetHttpContextAccessorMock(ValidUserId);
            var sut = CreateSut(httpContextMock.Object);
            var request = CancelAppointmentMockData.GetValidRequest();

            var appointment = CancelAppointmentMockData.GetAppointment();
            SetupAppointments(new[] { appointment });
            SetupSlots(Enumerable.Empty<TimeSlot>()); // Không tìm thấy slot

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2004.ToString());
            _slotRepoMock.Verify(r => r.UpdateAsync(It.IsAny<TimeSlot>()), Times.Never);
            _transactionMock.Verify(t => t.CommitAsync(default), Times.Once);
        }

        /// <summary>
        /// TC-CA-12: Người tạo lịch hẹn (CreatedById) khớp nhưng không phải PatientId → Cho phép hủy thành công (Kiểm tra điều kiện Or).
        /// </summary>
        [Fact]
        public async Task CancelAppointment_UserIsCreatedBy_ReturnsSuccess()
        {
            // Arrange
            var httpContextMock = CancelAppointmentMockData.GetHttpContextAccessorMock(ValidUserId);
            var sut = CreateSut(httpContextMock.Object);
            var request = CancelAppointmentMockData.GetValidRequest();

            var appointment = CancelAppointmentMockData.GetAppointment(
                patientId: OtherUserId,
                createdById: ValidUserId); // CreatedById khớp

            var slot = CancelAppointmentMockData.GetTimeSlot();

            SetupAppointments(new[] { appointment });
            SetupSlots(new[] { slot });

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2004.ToString());
        }

        /// <summary>
        /// TC-CA-13: Lỗi DB xảy ra trong quá trình xử lý Transaction → Catch exception, Rollback và throw lỗi.
        /// </summary>
        [Fact]
        public async Task CancelAppointment_DatabaseException_RollbacksTransactionAndThrows()
        {
            // Arrange
            var httpContextMock = CancelAppointmentMockData.GetHttpContextAccessorMock(ValidUserId);
            var sut = CreateSut(httpContextMock.Object);
            var request = CancelAppointmentMockData.GetValidRequest();

            var appointment = CancelAppointmentMockData.GetAppointment();
            SetupAppointments(new[] { appointment });

            _appointmentRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<Appointment>()))
                .ThrowsAsync(new Exception("Database connection error"));

            // Act
            var act = () => sut.Process(request);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Database connection error");
            _transactionMock.Verify(t => t.RollbackAsync(default), Times.Once);
        }
        /// <summary>
        /// TC-CA-14: User Claim tồn tại nhưng không đúng định dạng GUID (isUserValid = false) 
        /// VÀ Lịch hẹn TỒN TẠI (isAppointmentExist = true)
        /// -> Phủ dòng: if (!isUserValid) return (false, false); trong ValidateCancellationPermissions
        /// -> Phủ dòng: if (!isUserValid) return Fail(4001); trong CreateErrorResponse
        /// </summary>
        [Fact]
        public async Task CancelAppointment_InvalidGuidUserIdWithExistingAppointment_HitsUserInvalidBranchesAndReturnsFail4001()
        {
            // Arrange
            var httpContextMock = CancelAppointmentMockData.GetHttpContextAccessorWithInvalidGuidMock();
            var sut = CreateSut(httpContextMock.Object);
            var request = CancelAppointmentMockData.GetValidRequest();

            // SETUP QUAN TRỌNG: Phải setup appointment tồn tại để isAppointmentExist = true
            var appointment = CancelAppointmentMockData.GetAppointment();
            SetupAppointments(new[] { appointment });

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();

            // Đảm bảo không gọi vào DB update hay transaction
            _appointmentRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Appointment>()), Times.Never);
            _transactionMock.Verify(t => t.CommitAsync(default), Times.Never);
        }

        /// <summary>
        /// TC-CA-15: HttpContext hoặc User Claim hoàn toàn null (isUserValid = false)
        /// VÀ Lịch hẹn TỒN TẠI (isAppointmentExist = true)
        /// -> Đảm bảo hoàn toàn luồng unauthenticated / invalid user
        /// </summary>
        [Fact]
        public async Task CancelAppointment_NullHttpContextWithExistingAppointment_ReturnsFail4001()
        {
            // Arrange
            var httpContextMock = CancelAppointmentMockData.GetHttpContextAccessorMock(isAuthenticated: false);
            var sut = CreateSut(httpContextMock.Object);
            var request = CancelAppointmentMockData.GetValidRequest();

            // Setup appointment tồn tại
            var appointment = CancelAppointmentMockData.GetAppointment();
            SetupAppointments(new[] { appointment });

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
        }
    }
}