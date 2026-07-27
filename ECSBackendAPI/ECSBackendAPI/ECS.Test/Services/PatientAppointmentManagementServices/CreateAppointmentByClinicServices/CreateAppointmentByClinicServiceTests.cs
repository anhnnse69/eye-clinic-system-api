using System.Linq.Expressions;
using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Application.Services.PatientAppointmentManagementServices.CreateAppointmentByClinicServices;
using ECS.Application.Services.PatientAppointmentManagementServices.CreateAppointmentServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.PatientAppointmentManagementServices.CreateAppointmentByClinicServices
{
    /// <summary>
    /// Unit tests for <see cref="CreateAppointmentByClinicService"/>.
    /// Target: 100% Line Coverage & 100% Branch Coverage.
    /// Pattern: [Feature]_[Scenario]_[ExpectedResult].
    /// </summary>
    public class CreateAppointmentByClinicServiceTests
    {
        private readonly Mock<ICreateAppointmentService> _createAppointmentServiceMock = new();
        private readonly Mock<IRepositoryQueryBase<Clinic, Guid, AppDbContext>> _clinicRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepoMock = new();
        private readonly Mock<IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext>> _slotRepoMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();

        private static readonly Guid ValidClinicId = CreateAppointmentByClinicMockData.ValidClinicId;
        private static readonly Guid ValidDoctorId = CreateAppointmentByClinicMockData.ValidDoctorId;

        private CreateAppointmentByClinicService CreateSut()
        {
            return new CreateAppointmentByClinicService(
                _createAppointmentServiceMock.Object,
                _clinicRepoMock.Object,
                _doctorRepoMock.Object,
                _slotRepoMock.Object,
                _httpContextAccessorMock.Object);
        }

        private void SetupHttpContext(string? userIdClaim = null)
        {
            if (userIdClaim == null)
            {
                _httpContextAccessorMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);
                return;
            }

            var claims = new List<Claim>();
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userIdClaim));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = claimsPrincipal };

            _httpContextAccessorMock.Setup(h => h.HttpContext).Returns(httpContext);
        }

        private void SetupClinics(IEnumerable<Clinic> clinics)
        {
            _clinicRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Clinic, bool>>>(),
                    It.IsAny<bool>()))
                .Returns((Expression<Func<Clinic, bool>> expression, bool trackChanges) =>
                {
                    var filteredList = clinics.AsQueryable().Where(expression).ToList();
                    // TRUYỀN THẲNG List<Clinic> (ICollection/IEnumerable) VÀO BuildMockDbSet
                    return filteredList.BuildMockDbSet().Object;
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
                    var filteredList = doctors.AsQueryable().Where(expression).ToList();
                    // TRUYỀN THẲNG List<DoctorProfile> VÀO BuildMockDbSet
                    return filteredList.BuildMockDbSet().Object;
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
                    var filteredList = slots.AsQueryable().Where(expression).ToList();
                    // TRUYỀN THẲNG List<TimeSlot> VÀO BuildMockDbSet
                    return filteredList.BuildMockDbSet().Object;
                });
        }

        // ==================================================================
        // ================= HAPPY PATH / SUCCESS TESTS =====================
        // ==================================================================

        /// <summary>
        /// TC-CABCS-01: Luồng thành công hoàn chỉnh (Happy Path)
        /// Người dùng đã đăng nhập, clinic hoạt động, slot đúng định dạng + đúng giờ mở cửa,
        /// tìm thấy doctor có slot rảnh, tạo lịch khám thành công qua Delegate.
        /// </summary>
        [Fact]
        public async Task Process_AllValid_ReturnsSuccessResponse()
        {
            // Arrange
            var sut = CreateSut();
            var currentUserId = Guid.NewGuid();
            SetupHttpContext(currentUserId.ToString());

            var request = CreateAppointmentByClinicMockData.GetValidRequest();
            var clinic = CreateAppointmentByClinicMockData.GetClinic();
            var doctor = CreateAppointmentByClinicMockData.GetDoctor();
            var timeSlot = CreateAppointmentByClinicMockData.GetTimeSlot(doctorId: ValidDoctorId);

            SetupClinics(new[] { clinic });
            SetupDoctors(new[] { doctor });
            SetupSlots(new[] { timeSlot });

            var delegateResponse = ApiResponse<CreateAppointmentResponse>.Success(
                GeneralCode.APP_MESSAGE_2001.ToString(),
                CreateAppointmentByClinicMockData.GetCreateAppointmentResponse());

            _createAppointmentServiceMock
                .Setup(s => s.Process(It.IsAny<CreateAppointmentRequest>()))
                .ReturnsAsync(delegateResponse);

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2001.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.DoctorName.Should().Be("BS. Nguyễn Văn A");
            result.Data.ClinicName.Should().Be("Phòng Khám Mắt EyeCare");
            result.Data.Id_appointment.Should().Be(CreateAppointmentByClinicMockData.ValidAppointmentId);

            _createAppointmentServiceMock.Verify(s => s.Process(It.Is<CreateAppointmentRequest>(r =>
                r.PatientId == request.PatientId &&
                r.DoctorId == doctor.Id &&
                r.SlotId == timeSlot.Id &&
                r.ServiceId == request.ServiceId &&
                r.Symptoms == request.Symptoms
            )), Times.Once);
        }

        // ==================================================================
        // ==================== STEP 1: USER CLAIM TESTS ====================
        // ==================================================================

        /// <summary>
        /// TC-CABCS-02: User Claim không tồn tại hoặc HttpContext null -> RetrieveUserId trả về Guid.Empty nhưng luồng vẫn tiếp tục chạy bình thường
        /// </summary>
        [Fact]
        public async Task Process_HttpContextNullOrNoUserClaim_ExecutesWithEmptyUserId()
        {
            // Arrange
            var sut = CreateSut();
            SetupHttpContext(null); // HttpContext = null

            var request = CreateAppointmentByClinicMockData.GetValidRequest();
            var clinic = CreateAppointmentByClinicMockData.GetClinic();
            var doctor = CreateAppointmentByClinicMockData.GetDoctor();
            var timeSlot = CreateAppointmentByClinicMockData.GetTimeSlot();

            SetupClinics(new[] { clinic });
            SetupDoctors(new[] { doctor });
            SetupSlots(new[] { timeSlot });

            _createAppointmentServiceMock
                .Setup(s => s.Process(It.IsAny<CreateAppointmentRequest>()))
                .ReturnsAsync(ApiResponse<CreateAppointmentResponse>.Success(
                    GeneralCode.APP_MESSAGE_2001.ToString(),
                    CreateAppointmentByClinicMockData.GetCreateAppointmentResponse()));

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2001.ToString());
        }

        /// <summary>
        /// TC-CABCS-03: User Claim không phải là Guid hợp lệ -> Guid.TryParse thất bại
        /// </summary>
        [Fact]
        public async Task Process_InvalidUserIdClaimFormat_ExecutesWithEmptyUserId()
        {
            // Arrange
            var sut = CreateSut();
            SetupHttpContext("invalid-guid-string");

            var request = CreateAppointmentByClinicMockData.GetValidRequest();
            var clinic = CreateAppointmentByClinicMockData.GetClinic();
            var doctor = CreateAppointmentByClinicMockData.GetDoctor();
            var timeSlot = CreateAppointmentByClinicMockData.GetTimeSlot();

            SetupClinics(new[] { clinic });
            SetupDoctors(new[] { doctor });
            SetupSlots(new[] { timeSlot });

            _createAppointmentServiceMock
                .Setup(s => s.Process(It.IsAny<CreateAppointmentRequest>()))
                .ReturnsAsync(ApiResponse<CreateAppointmentResponse>.Success(
                    GeneralCode.APP_MESSAGE_2001.ToString(),
                    CreateAppointmentByClinicMockData.GetCreateAppointmentResponse()));

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2001.ToString());
        }

        // ==================================================================
        // ================= STEP 2: CLINIC VALIDATION TESTS ================
        // ==================================================================

        /// <summary>
        /// TC-CABCS-04: Clinic không tìm thấy hoặc IsActive = false -> Trả về APP_MESSAGE_4045
        /// </summary>
        [Fact]
        public async Task Process_ClinicNotFoundOrInactive_ReturnsFail4045()
        {
            // Arrange
            var sut = CreateSut();
            SetupHttpContext(Guid.NewGuid().ToString());
            var request = CreateAppointmentByClinicMockData.GetValidRequest();

            SetupClinics(Enumerable.Empty<Clinic>()); // Clinic rỗng

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4045.ToString());
            result.Data.Should().BeNull();
        }

        // ==================================================================
        // ================== STEP 3: SLOT PARSING TESTS ====================
        // ==================================================================

        /// <summary>
        /// TC-CABCS-05: SlotId null, rỗng hoặc chỉ toàn khoảng trắng -> Trả về APP_MESSAGE_4052
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Process_NullOrEmptySlotId_ReturnsFail4052(string? emptySlotId)
        {
            // Arrange
            var sut = CreateSut();
            SetupHttpContext(Guid.NewGuid().ToString());
            var request = CreateAppointmentByClinicMockData.GetValidRequest();

            // Gán trực tiếp giá trị test (null, "", "   ") vào SlotId
            request.SlotId = emptySlotId!;

            var clinic = CreateAppointmentByClinicMockData.GetClinic();
            SetupClinics(new[] { clinic });

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4052.ToString());
        }
        /// <summary>
        /// TC-CABCS-06: SlotId sai số lượng phần tử hoặc phần đầu không phải "clinic" -> Trả về APP_MESSAGE_4052
        /// </summary>
        [Theory]
        [InlineData("invalid_slot_format")]
        [InlineData("hospital_11111111-1111-1111-1111-111111111111_2026-08-10_08:00")]
        [InlineData("clinic_11111111-1111-1111-1111-111111111111_2026-08-10")]
        public async Task Process_InvalidSlotPrefixOrPartLength_ReturnsFail4052(string invalidSlotId)
        {
            // Arrange
            var sut = CreateSut();
            SetupHttpContext(Guid.NewGuid().ToString());
            var request = CreateAppointmentByClinicMockData.GetValidRequest(slotId: invalidSlotId);
            var clinic = CreateAppointmentByClinicMockData.GetClinic();

            SetupClinics(new[] { clinic });

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4052.ToString());
        }

        /// <summary>
        /// TC-CABCS-07: ClinicId trong SlotId không khớp với request.ClinicId hoặc không đúng định dạng Guid -> Trả về APP_MESSAGE_4052
        /// </summary>
        [Theory]
        [InlineData("clinic_not-a-guid_2026-08-10_08:00")]
        [InlineData("clinic_99999999-9999-9999-9999-999999999999_2026-08-10_08:00")] // Không khớp ClinicId
        public async Task Process_MismatchedOrInvalidClinicIdInSlot_ReturnsFail4052(string invalidSlotId)
        {
            // Arrange
            var sut = CreateSut();
            SetupHttpContext(Guid.NewGuid().ToString());
            var request = CreateAppointmentByClinicMockData.GetValidRequest(slotId: invalidSlotId);
            var clinic = CreateAppointmentByClinicMockData.GetClinic();

            SetupClinics(new[] { clinic });

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4052.ToString());
        }

        /// <summary>
        /// TC-CABCS-08: Ngày hoặc Giờ trong SlotId sai định dạng DateTime/TimeSpan -> Trả về APP_MESSAGE_4052
        /// </summary>
        [Theory]
        [InlineData("clinic_11111111-1111-1111-1111-111111111111_invalid-date_08:00")]
        [InlineData("clinic_11111111-1111-1111-1111-111111111111_2026-08-10_invalid-time")]
        public async Task Process_InvalidDateOrTimeInSlot_ReturnsFail4052(string invalidSlotId)
        {
            // Arrange
            var sut = CreateSut();
            SetupHttpContext(Guid.NewGuid().ToString());
            var request = CreateAppointmentByClinicMockData.GetValidRequest(slotId: invalidSlotId);
            var clinic = CreateAppointmentByClinicMockData.GetClinic();

            SetupClinics(new[] { clinic });

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4052.ToString());
        }

        // ==================================================================
        // ============ STEP 4: WORKING HOURS VALIDATION TESTS =============
        // ==================================================================

        /// <summary>
        /// TC-CABCS-09: Thời gian slot nằm ngoài giờ làm việc của Clinic (Trước giờ mở cửa hoặc Sau giờ đóng cửa) -> Trả về APP_MESSAGE_4052
        /// </summary>
        [Theory]
        [InlineData("clinic_11111111-1111-1111-1111-111111111111_2026-08-10_07:30")] // Trước OpenTime (08:00)
        [InlineData("clinic_11111111-1111-1111-1111-111111111111_2026-08-10_17:00")] // Đúng bằng/sau CloseTime (17:00)
        public async Task Process_SlotTimeOutsideWorkingHours_ReturnsFail4052(string outOfHoursSlotId)
        {
            // Arrange
            var sut = CreateSut();
            SetupHttpContext(Guid.NewGuid().ToString());
            var request = CreateAppointmentByClinicMockData.GetValidRequest(slotId: outOfHoursSlotId);

            // Clinic mở cửa 08:00 -> 17:00
            var clinic = CreateAppointmentByClinicMockData.GetClinic(openTime: new TimeOnly(8, 0), closeTime: new TimeOnly(17, 0));
            SetupClinics(new[] { clinic });

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4052.ToString());
        }

        // ==================================================================
        // ============= STEP 5 & 6: DOCTOR & SLOT AVAILABILITY =============
        // ==================================================================

        /// <summary>
        /// TC-CABCS-10: Clinic không có Bác sĩ nào hoạt động (IsActive = true) -> Trả về APP_MESSAGE_4011
        /// </summary>
        [Fact]
        public async Task Process_NoActiveDoctorsInClinic_ReturnsFail4011()
        {
            // Arrange
            var sut = CreateSut();
            SetupHttpContext(Guid.NewGuid().ToString());
            var request = CreateAppointmentByClinicMockData.GetValidRequest();
            var clinic = CreateAppointmentByClinicMockData.GetClinic();

            SetupClinics(new[] { clinic });
            SetupDoctors(Enumerable.Empty<DoctorProfile>()); // Không có bác sĩ

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4011.ToString());
        }

        /// <summary>
        /// TC-CABCS-11: Có bác sĩ nhưng không bác sĩ nào có TimeSlot trống/AVAILABLE đúng giờ đó -> Trả về APP_MESSAGE_4011
        /// </summary>
        [Fact]
        public async Task Process_DoctorsExistButNoAvailableSlot_ReturnsFail4011()
        {
            // Arrange
            var sut = CreateSut();
            SetupHttpContext(Guid.NewGuid().ToString());
            var request = CreateAppointmentByClinicMockData.GetValidRequest();
            var clinic = CreateAppointmentByClinicMockData.GetClinic();
            var doctor = CreateAppointmentByClinicMockData.GetDoctor();

            SetupClinics(new[] { clinic });
            SetupDoctors(new[] { doctor });
            SetupSlots(Enumerable.Empty<TimeSlot>()); // Không có Slot phù hợp trong DB

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4011.ToString());
        }

        /// <summary>
        /// TC-CABCS-12: Bác sĩ đầu tiên bận (không có slot), bác sĩ thứ 2 có slot trống
        /// -> Phủ vòng lặp foreach trong FindAvailableDoctor
        /// </summary>
        [Fact]
        public async Task Process_FirstDoctorBusySecondDoctorAvailable_ReturnsSuccess()
        {
            // Arrange
            var sut = CreateSut();
            SetupHttpContext(Guid.NewGuid().ToString());

            var request = CreateAppointmentByClinicMockData.GetValidRequest();
            var clinic = CreateAppointmentByClinicMockData.GetClinic();

            var doctor1 = CreateAppointmentByClinicMockData.GetDoctor(id: Guid.NewGuid());
            var doctor2 = CreateAppointmentByClinicMockData.GetDoctor(id: Guid.NewGuid());

            // Slot chỉ thuộc về Bác sĩ 2
            var timeSlotDoc2 = CreateAppointmentByClinicMockData.GetTimeSlot(doctorId: doctor2.Id);

            SetupClinics(new[] { clinic });
            SetupDoctors(new[] { doctor1, doctor2 });
            SetupSlots(new[] { timeSlotDoc2 });

            _createAppointmentServiceMock
                .Setup(s => s.Process(It.IsAny<CreateAppointmentRequest>()))
                .ReturnsAsync(ApiResponse<CreateAppointmentResponse>.Success(
                    GeneralCode.APP_MESSAGE_2001.ToString(),
                    CreateAppointmentByClinicMockData.GetCreateAppointmentResponse()));

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2001.ToString());

            _createAppointmentServiceMock.Verify(s => s.Process(It.Is<CreateAppointmentRequest>(r =>
                r.DoctorId == doctor2.Id
            )), Times.Once);
        }

        /// <summary>
        /// TC-CABCS-13: Trường hợp hy hữu FindAvailableDoctor thấy slot nhưng khi RetrieveAvailableTimeSlot bị null
        /// </summary>
        [Fact]
        public async Task Process_TimeSlotDisappearsBetweenFindAndRetrieve_ReturnsFail4011()
        {
            // Arrange
            var sut = CreateSut();
            SetupHttpContext(Guid.NewGuid().ToString());

            var request = CreateAppointmentByClinicMockData.GetValidRequest();
            var clinic = CreateAppointmentByClinicMockData.GetClinic();
            var doctor = CreateAppointmentByClinicMockData.GetDoctor();
            var timeSlot = CreateAppointmentByClinicMockData.GetTimeSlot(doctorId: doctor.Id);

            SetupClinics(new[] { clinic });
            SetupDoctors(new[] { doctor });

            // Lần 1 (AnyAsync): trả về list có timeSlot. Lần 2 (FirstOrDefaultAsync): trả về list rỗng
            int callCount = 0;
            _slotRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<TimeSlot, bool>>>(), It.IsAny<bool>()))
                .Returns((Expression<Func<TimeSlot, bool>> expr, bool track) =>
                {
                    callCount++;
                    var list = callCount == 1 ? new List<TimeSlot> { timeSlot } : new List<TimeSlot>();

                    // Gọi trực tiếp BuildMockDbSet() trên List<TimeSlot>
                    return list.BuildMockDbSet().Object;
                });

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4011.ToString());
        }

        // ==================================================================
        // ============= STEP 7 & 8: DELEGATION & RESPONSE TESTS ============
        // ==================================================================

        /// <summary>
        /// TC-CABCS-14: DelegateAppointmentCreation trả về null hoặc result.Data null -> Trả về lỗi 5000
        /// </summary>
        [Fact]
        public async Task Process_DelegateServiceReturnsNullData_ReturnsFail5000()
        {
            // Arrange
            var sut = CreateSut();
            SetupHttpContext(Guid.NewGuid().ToString());

            var request = CreateAppointmentByClinicMockData.GetValidRequest();
            var clinic = CreateAppointmentByClinicMockData.GetClinic();
            var doctor = CreateAppointmentByClinicMockData.GetDoctor();
            var timeSlot = CreateAppointmentByClinicMockData.GetTimeSlot();

            SetupClinics(new[] { clinic });
            SetupDoctors(new[] { doctor });
            SetupSlots(new[] { timeSlot });

            // Trả về Fail hoặc Data = null từ service con
            _createAppointmentServiceMock
                .Setup(s => s.Process(It.IsAny<CreateAppointmentRequest>()))
                .ReturnsAsync(ApiResponse<CreateAppointmentResponse>.Fail("ANY_ERROR"));

            // Act
            var result = await sut.Process(request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5000.ToString());
            result.Data.Should().BeNull();
        }
        /// <summary>
        /// TC-CABCS-15: Gọi trực tiếp ValidateClinicProfile với currentClinicState = false
        /// Đảm bảo phủ 100% Line & Branch coverage tại dòng 112 - 114.
        /// </summary>
        [Fact]
        public async Task ValidateClinicProfile_WhenCurrentClinicStateIsFalse_ReturnsNullAndFalse()
        {
            // Arrange
            var sut = CreateSut();
            var clinicId = Guid.NewGuid();

            // Dùng Reflection để truy cập hàm private ValidateClinicProfile
            var methodInfo = typeof(CreateAppointmentByClinicService)
                .GetMethod("ValidateClinicProfile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            methodInfo.Should().NotBeNull("Phương thức ValidateClinicProfile phải tồn tại trong Service");

            // Act
            // Tham số truyền vào: clinicId, currentClinicState = false
            var task = (Task<(Clinic? ClinicNode, bool IsClinicValid)>)methodInfo!.Invoke(sut, new object[] { clinicId, false })!;
            var result = await task;

            // Assert
            result.ClinicNode.Should().BeNull();
            result.IsClinicValid.Should().BeFalse();
        }
    }
}
