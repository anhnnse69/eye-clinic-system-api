using ECS.Application.Services.ClinicAdminManagementServices.ClinicDashboardServices;
using ECS.Domain.Entities.Auth;
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
using System.Linq.Expressions;
using System.Security.Claims;

namespace ECS.Test.Services.ClinicAdminManagementServices.ClinicDashboardServices
{
    /// <summary>
    /// Unit tests for <see cref="ViewClinicDashboardService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line coverage on ViewClinicDashboardService.cs.
    /// </summary>
    public class ViewClinicDashboardServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<Appointment, Guid, AppDbContext>> _appointmentRepoMock;
        private readonly Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>> _staffClinicRepoMock;
        private readonly Mock<IRepositoryQueryBase<Service, Guid, AppDbContext>> _serviceRepoMock;
        private readonly Mock<IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext>> _roomRepoMock;
        private readonly Mock<IRepositoryQueryBase<MedicineCatalog, Guid, AppDbContext>> _medicineRepoMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly ViewClinicDashboardService _sut;

        public ViewClinicDashboardServiceTests()
        {
            _appointmentRepoMock = new Mock<IRepositoryQueryBase<Appointment, Guid, AppDbContext>>();
            _staffClinicRepoMock = new Mock<IRepositoryQueryBase<StaffClinic, Guid, AppDbContext>>();
            _serviceRepoMock = new Mock<IRepositoryQueryBase<Service, Guid, AppDbContext>>();
            _roomRepoMock = new Mock<IRepositoryQueryBase<FacilityRoom, Guid, AppDbContext>>();
            _medicineRepoMock = new Mock<IRepositoryQueryBase<MedicineCatalog, Guid, AppDbContext>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _sut = new ViewClinicDashboardService(
                _appointmentRepoMock.Object,
                _staffClinicRepoMock.Object,
                _serviceRepoMock.Object,
                _roomRepoMock.Object,
                _medicineRepoMock.Object,
                _httpContextAccessorMock.Object);
        }

        // ── HttpContext helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Wires the http context accessor so that <see cref="ClaimTypes.NameIdentifier"/> resolves
        /// to a claim with the supplied raw value (used to test invalid Guid strings).
        /// Pass <c>null</c> to leave the principal with no claims.
        /// </summary>
        private void SetupHttpContextClaim(string? rawClaimValue)
        {
            var httpContext = new DefaultHttpContext();
            if (rawClaimValue != null)
            {
                var claims = new[] { new Claim(ClaimTypes.NameIdentifier, rawClaimValue) };
                var identity = new ClaimsIdentity(claims, "TestAuth");
                httpContext.User = new ClaimsPrincipal(identity);
            }
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
        }

        private void SetupHttpContextUserId(Guid userId)
            => SetupHttpContextClaim(userId.ToString());

        // ── Repository helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Wires <c>_staffClinicRepoMock</c> so that the
        /// <c>FindByCondition(...).FirstOrDefaultAsync()</c> chain used inside
        /// <c>RetrieveClinicId</c> resolves to the supplied StaffClinic (or null).
        /// </summary>
        private void SetupStaffClinicLookupRepo(StaffClinic? returnStaffClinic)
        {
            var rows = returnStaffClinic != null
                ? new List<StaffClinic> { returnStaffClinic }
                : new List<StaffClinic>();
            var mockQueryable = rows.BuildMockDbSet<StaffClinic>();

            _staffClinicRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<StaffClinic, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        /// <summary>
        /// Wires the appointment repository so that the
        /// <c>FindByCondition(...).Include(...).ToListAsync()</c> chain resolves to the
        /// supplied list.
        /// </summary>
        private void SetupAppointmentRepo(IEnumerable<Appointment> appointments)
        {
            var list = appointments.ToList();
            var mockQueryable = list.BuildMockDbSet<Appointment>();

            _appointmentRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Appointment, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        /// <summary>
        /// Wires <c>_staffClinicRepoMock</c> so that BOTH the
        /// <c>FindByCondition(...).FirstOrDefaultAsync()</c> call inside
        /// <c>RetrieveClinicId</c> AND the <c>FindByCondition(...).CountAsync()</c> call
        /// inside <c>RetrieveDashboardData.TotalStaffs</c> resolve consistently:
        /// <list type="bullet">
        ///   <item>The first row has <c>UserId == ownerUserId</c> + <c>ClinicId == TestClinicId</c>
        ///         and is active, so <c>RetrieveClinicId</c> returns the test clinic id.</item>
        ///   <item>The total number of rows (including the first) equals <paramref name="staffCount"/>,
        ///         so <c>RetrieveDashboardData.TotalStaffs</c> returns <paramref name="staffCount"/>.</item>
        /// </list>
        /// </summary>
        private void SetupStaffClinicRepo(Guid ownerUserId, int staffCount)
        {
            var rows = Enumerable.Range(0, staffCount).Select((i) =>
            {
                var userId = i == 0 ? ownerUserId : Guid.NewGuid();
                return new StaffClinic
                {
                    Id = Guid.NewGuid(),
                    ClinicId = ClinicDashboardMockData.TestClinicId,
                    UserId = userId,
                    IsActive = true
                };
            }).ToList();
            var mockQueryable = rows.BuildMockDbSet<StaffClinic>();

            _staffClinicRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<StaffClinic, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        /// <summary>
        /// Wires <c>_serviceRepoMock</c> so the <c>CountAsync()</c> call returns the supplied count.
        /// </summary>
        private void SetupServiceCountRepo(int count)
        {
            var rows = Enumerable.Range(0, count).Select(_ => ClinicDashboardMockData.GetTestService()).ToList();
            var mockQueryable = rows.BuildMockDbSet<Service>();

            _serviceRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Service, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        /// <summary>
        /// Wires <c>_roomRepoMock</c> so the <c>CountAsync()</c> call returns the supplied count.
        /// </summary>
        private void SetupRoomCountRepo(int count)
        {
            var rows = Enumerable.Range(0, count).Select(i => ClinicDashboardMockData.GetTestRoom($"Room {i}")).ToList();
            var mockQueryable = rows.BuildMockDbSet<FacilityRoom>();

            _roomRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<FacilityRoom, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        /// <summary>
        /// Wires <c>_medicineRepoMock</c> so the <c>CountAsync()</c> call returns the supplied count.
        /// </summary>
        private void SetupMedicineCountRepo(int count)
        {
            var rows = Enumerable.Range(0, count).Select(i => ClinicDashboardMockData.GetTestMedicine($"Med {i}")).ToList();
            var mockQueryable = rows.BuildMockDbSet<MedicineCatalog>();

            _medicineRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<MedicineCatalog, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(mockQueryable.Object);
        }

        /// <summary>
        /// Builds a single appointment scheduled at the supplied UTC date.
        /// </summary>
        private static Appointment BuildAppointment(
            DateTime date,
            AppointmentStatus status = AppointmentStatus.BOOKED,
            decimal depositAmount = 100000m,
            bool depositPaid = true)
        {
            var clinic = ClinicDashboardMockData.GetTestClinic();
            var doctorUser = new User
            {
                Id = ClinicDashboardMockData.TestDoctorUserId,
                FullName = "BS. Le Van Doctor",
                Phone = "0933334444",
                Email = "doctor@ECS.vn",
                PasswordHash = "x",
                Role = UserRole.DOCTOR,
                IsActive = true
            };
            var doctor = ClinicDashboardMockData.GetTestDoctorProfile(doctorUser, clinic);
            var patient = ClinicDashboardMockData.GetTestPatientProfile();
            var slot = ClinicDashboardMockData.GetTestTimeSlot(
                date.Date.AddHours(9),
                date.Date.AddHours(9).AddMinutes(30));
            return ClinicDashboardMockData.GetTestAppointment(
                appointmentDate: date,
                slot: slot,
                doctor: doctor,
                patient: patient,
                service: null,
                symptoms: "Eye redness",
                status: status,
                depositAmount: depositAmount,
                depositPaid: depositPaid);
        }

        // ── Test Cases ───────────────────────────────────────────────────────────

        /// <summary>
        /// TC-VCD-01: HttpContext has no NameIdentifier claim → Guid.TryParse fails →
        /// isUserValid=false; StaffClinic repo never reached; Returns APP_MESSAGE_4001.
        /// Covers RetrieveUserId (User != null AND FindFirst returns null branch),
        /// RetrieveClinicId (`!isUserValid` short-circuit → returns null),
        /// RetrieveDashboardData (clinicId null → returns null), ValidateRetrievedData
        /// (data null → isClinicExist=false), CreateErrorResponse (!isUserValid → APP_MESSAGE_4001).
        /// </summary>
        [Fact]
        public async Task Process_HttpContextHasNoNameIdentifierClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            // HttpContext will be set with empty principal (User has no claims).

            //Arrange 2
            SetupHttpContextClaim(null);
            // In this path the appointment repo is still queried (since RetrieveDashboardData is
            // always invoked before the early-return check kicks in). We seed an empty list to
            // satisfy the Include().ToListAsync() chain.
            SetupAppointmentRepo(Array.Empty<Appointment>());

            //Act
            var result = await _sut.Process();

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _appointmentRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>
        /// TC-VCD-02: HttpContext accessor returns null → the `?.User.FindFirst(...)?.Value`
        /// chain short-circuits on the first null-conditional → userIdClaim is null →
        /// Guid.TryParse fails → isUserValid=false → APP_MESSAGE_4001.
        /// Covers RetrieveUserId (HttpContext null branch of the `?.User` chain).
        /// </summary>
        [Fact]
        public async Task Process_NullHttpContext_Returns4001AuthenticationError()
        {
            //Arrange 1
            // No request inputs.

            //Arrange 2
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
            // RetrieveDashboardData is short-circuited at the `!clinicId.HasValue` branch because
            // userId returns Guid.Empty and then StaffClinic lookup fails — the staffClinic mock
            // has no setup, so we instead make the appointment repo return an empty list to be
            // safe if execution reaches it. In this path it is NEVER reached because
            // RetrieveClinicId short-circuits on isUserValid=false.
            _appointmentRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Appointment, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(new List<Appointment>().BuildMockDbSet<Appointment>().Object);

            //Act
            var result = await _sut.Process();

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _appointmentRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>
        /// TC-VCD-03: HttpContext is non-null but HttpContext.User is null → the
        /// `?.FindFirst(...)` chain short-circuits at the second null-conditional → userIdClaim
        /// is null → Guid.TryParse fails → isUserValid=false → APP_MESSAGE_4001.
        /// Covers RetrieveUserId (HttpContext != null AND User == null branch).
        /// </summary>
        [Fact]
        public async Task Process_NullHttpContextUser_Returns4001AuthenticationError()
        {
            //Arrange 1
            // HttpContext will be non-null but User == null.

            //Arrange 2
            var httpContext = new DefaultHttpContext { User = null! };
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
            _appointmentRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Appointment, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(new List<Appointment>().BuildMockDbSet<Appointment>().Object);

            //Act
            var result = await _sut.Process();

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _appointmentRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>
        /// TC-VCD-04: HttpContext contains a NameIdentifier claim that is NOT a valid Guid →
        /// Guid.TryParse fails → isUserValid=false → APP_MESSAGE_4001.
        /// Covers RetrieveUserId (Guid.TryParse false branch via non-Guid string).
        /// </summary>
        [Fact]
        public async Task Process_NonGuidClaim_Returns4001AuthenticationError()
        {
            //Arrange 1
            // Claim value is not a Guid string.

            //Arrange 2
            SetupHttpContextClaim("not-a-guid");
            _appointmentRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Appointment, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(new List<Appointment>().BuildMockDbSet<Appointment>().Object);

            //Act
            var result = await _sut.Process();

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4001.ToString());
            result.Data.Should().BeNull();
            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _appointmentRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>
        /// TC-VCD-05: Valid Guid claim but no active StaffClinic row → RetrieveClinicId
        /// returns null → RetrieveDashboardData (clinicId null) returns null →
        /// ValidateRetrievedData sets isClinicExist=false → APP_MESSAGE_4020.
        /// Covers RetrieveClinicId (FirstOrDefaultAsync returns null branch), RetrieveDashboardData
        /// (!clinicId.HasValue short-circuit), CreateErrorResponse (!isClinicExist → APP_MESSAGE_4020).
        /// </summary>
        [Fact]
        public async Task Process_ValidUserButNoStaffClinic_Returns4020ClinicNotFound()
        {
            //Arrange 1
            var userId = Guid.NewGuid();

            //Arrange 2
            SetupHttpContextUserId(userId);
            SetupStaffClinicLookupRepo(null);
            SetupAppointmentRepo(Array.Empty<Appointment>());

            //Act
            var result = await _sut.Process();

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4020.ToString());
            result.Data.Should().BeNull();
            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            // RetrieveDashboardData is NOT entered because clinicId has no value.
            _appointmentRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>
        /// TC-VCD-06: Happy path with empty appointments — all numeric metrics are zero, and
        /// the rolling 7-day timeline is populated with empty entries. Covers BuildWeeklyStatistics
        /// (7-iteration for-loop), MapToResponse, CreateResponse success path → APP_MESSAGE_2000.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_EmptyAppointments_ReturnsSuccessWithZeroMetrics()
        {
            //Arrange 1
            var userId = Guid.NewGuid();
            SetupHttpContextUserId(userId);

            //Arrange 2
            // Same StaffClinic repository is queried twice: once via FirstOrDefaultAsync (lookup)
            // and once via CountAsync (TotalStaffs). The same MockQueryable is consumed by both.
            SetupStaffClinicRepo(userId, 3);
            SetupAppointmentRepo(Array.Empty<Appointment>());
            SetupServiceCountRepo(2);
            SetupRoomCountRepo(4);
            SetupMedicineCountRepo(5);

            //Act
            var result = await _sut.Process();

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();

            var dashboard = result.Data!;
            dashboard.TotalAppointments.Should().Be(0);
            dashboard.CompletedAppointments.Should().Be(0);
            dashboard.CancelledAppointments.Should().Be(0);
            dashboard.TotalRevenue.Should().Be(0m);
            dashboard.TotalStaffs.Should().Be(3);
            dashboard.TotalServices.Should().Be(2);
            dashboard.TotalRooms.Should().Be(4);
            dashboard.TotalMedicines.Should().Be(5);
            dashboard.WeeklyStatistics.Should().NotBeNull();
            dashboard.WeeklyStatistics.Should().HaveCount(7);
            dashboard.WeeklyStatistics.Should().OnlyContain(item => item.Appointments == 0 && item.Revenue == 0m);

            _staffClinicRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<StaffClinic, bool>>>(), It.IsAny<bool>()),
                Times.Exactly(2));
            _appointmentRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// TC-VCD-07: Happy path with 3 today appointments (1 COMPLETED paid, 1 CANCELLED paid,
        /// 1 PENDING unpaid). Verifies all status-based aggregation branches and the
        /// `DepositPaid -> DepositAmount -> Revenue` accumulation branch.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_TodayCompletedAndCancelledAppointments_ComputedMetrics()
        {
            //Arrange 1
            var userId = Guid.NewGuid();
            var today = DateTime.UtcNow.Date;
            SetupHttpContextUserId(userId);

            // Three today appointments:
            //   - 1 COMPLETED, deposit paid → counts as TotalAppointments, CompletedAppointments, TotalRevenue (+)
            //   - 1 CANCELLED, deposit paid → counts as TotalAppointments, CancelledAppointments, TotalRevenue (+)
            //   - 1 PENDING, deposit NOT paid → counts as TotalAppointments only
            var completed = BuildAppointment(today.AddHours(9), AppointmentStatus.COMPLETED, depositAmount: 200000m, depositPaid: true);
            var cancelled = BuildAppointment(today.AddHours(10), AppointmentStatus.CANCELLED, depositAmount: 150000m, depositPaid: true);
            var pendingUnpaid = BuildAppointment(today.AddHours(11), AppointmentStatus.PENDING, depositAmount: 100000m, depositPaid: false);

            //Arrange 2
            SetupStaffClinicRepo(userId, 2);
            SetupAppointmentRepo(new[] { completed, cancelled, pendingUnpaid });
            SetupServiceCountRepo(0);
            SetupRoomCountRepo(0);
            SetupMedicineCountRepo(0);

            //Act
            var result = await _sut.Process();

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            var dashboard = result.Data!;
            dashboard.TotalAppointments.Should().Be(3);
            dashboard.CompletedAppointments.Should().Be(1);
            dashboard.CancelledAppointments.Should().Be(1);
            // Only PAID deposits contribute to revenue: 200000 + 150000 = 350000.
            dashboard.TotalRevenue.Should().Be(350000m);
            dashboard.TotalStaffs.Should().Be(2);
            dashboard.TotalServices.Should().Be(0);
            dashboard.TotalRooms.Should().Be(0);
            dashboard.TotalMedicines.Should().Be(0);
        }

        /// <summary>
        /// TC-VCD-08: An appointment scheduled for tomorrow UTC (NOT today) must not be
        /// counted in today metrics. Covers the todayAppointments `Where(x => x.AppointmentDate.Date == today)`
        /// filter excluding non-matching dates.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_AppointmentFromDifferentDay_ExcludedFromTodayMetrics()
        {
            //Arrange 1
            var userId = Guid.NewGuid();
            var tomorrow = DateTime.UtcNow.Date.AddDays(1);
            SetupHttpContextUserId(userId);

            var futureAppt = BuildAppointment(tomorrow.AddHours(9), AppointmentStatus.COMPLETED, depositAmount: 999999m, depositPaid: true);

            //Arrange 2
            SetupStaffClinicRepo(userId, 1);
            SetupAppointmentRepo(new[] { futureAppt });
            SetupServiceCountRepo(0);
            SetupRoomCountRepo(0);
            SetupMedicineCountRepo(0);

            //Act
            var result = await _sut.Process();

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            var dashboard = result.Data!;
            dashboard.TotalAppointments.Should().Be(0);
            dashboard.CompletedAppointments.Should().Be(0);
            dashboard.CancelledAppointments.Should().Be(0);
            dashboard.TotalRevenue.Should().Be(0m);
        }

        /// <summary>
        /// TC-VCD-09: An appointment where DepositPaid=false must NOT contribute to revenue.
        /// Covers the `Where(x => x.DepositPaid)` filter inside the revenue accumulator.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_NonPaidAppointments_ExcludedFromRevenue()
        {
            //Arrange 1
            var userId = Guid.NewGuid();
            var today = DateTime.UtcNow.Date;
            SetupHttpContextUserId(userId);

            // Today COMPLETED but DepositPaid=false → counts towards TotalAppointments but NOT TotalRevenue.
            var unpaidCompleted = BuildAppointment(today.AddHours(9), AppointmentStatus.COMPLETED, depositAmount: 500000m, depositPaid: false);

            //Arrange 2
            SetupStaffClinicRepo(userId, 1);
            SetupAppointmentRepo(new[] { unpaidCompleted });
            SetupServiceCountRepo(0);
            SetupRoomCountRepo(0);
            SetupMedicineCountRepo(0);

            //Act
            var result = await _sut.Process();

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            var dashboard = result.Data!;
            dashboard.TotalAppointments.Should().Be(1);
            dashboard.CompletedAppointments.Should().Be(1);
            dashboard.TotalRevenue.Should().Be(0m);
        }

        /// <summary>
        /// TC-VCD-10: Verify that the four CountAsync branches (TotalStaffs, TotalServices,
        /// TotalRooms, TotalMedicines) reflect the supplied counts and that each respective
        /// repository FindByCondition is invoked exactly once.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_CountsFromAllResourceRepos_ReflectedInTotals()
        {
            //Arrange 1
            var userId = Guid.NewGuid();
            SetupHttpContextUserId(userId);

            //Arrange 2
            SetupStaffClinicRepo(userId, 7);
            SetupAppointmentRepo(Array.Empty<Appointment>());
            SetupServiceCountRepo(11);
            SetupRoomCountRepo(3);
            SetupMedicineCountRepo(42);

            //Act
            var result = await _sut.Process();

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            var dashboard = result.Data!;
            dashboard.TotalStaffs.Should().Be(7);
            dashboard.TotalServices.Should().Be(11);
            dashboard.TotalRooms.Should().Be(3);
            dashboard.TotalMedicines.Should().Be(42);

            _serviceRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Service, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _roomRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<FacilityRoom, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _medicineRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<MedicineCatalog, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// TC-VCD-11: Seed one paid appointment per day across the rolling 7-day window.
        /// Verifies BuildWeeklyStatistics produces a 7-item timeline with matching daily counts
        /// and revenue, and that appointment dates are formatted as yyyy-MM-dd.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_WeeklyStatistics_BuildsSevenDayTimeline()
        {
            //Arrange 1
            var userId = Guid.NewGuid();
            SetupHttpContextUserId(userId);

            var startDate = DateTime.UtcNow.Date.AddDays(-6);
            var appointments = Enumerable.Range(0, 7).Select(i =>
            {
                var day = startDate.AddDays(i);
                return BuildAppointment(day, AppointmentStatus.COMPLETED, depositAmount: 10000m * (i + 1), depositPaid: true);
            }).ToList();

            //Arrange 2
            SetupStaffClinicRepo(userId, 1);
            SetupAppointmentRepo(appointments);
            SetupServiceCountRepo(0);
            SetupRoomCountRepo(0);
            SetupMedicineCountRepo(0);

            //Act
            var result = await _sut.Process();

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            var dashboard = result.Data!;
            dashboard.WeeklyStatistics.Should().HaveCount(7);
            dashboard.WeeklyStatistics[0].Date.Should().Be(startDate.ToString("yyyy-MM-dd"));
            dashboard.WeeklyStatistics[6].Date.Should().Be(DateTime.UtcNow.Date.ToString("yyyy-MM-dd"));
            dashboard.WeeklyStatistics[0].Revenue.Should().Be(10000m);
            dashboard.WeeklyStatistics[6].Revenue.Should().Be(70000m);
            dashboard.WeeklyStatistics.Should().OnlyContain(item => item.Appointments == 1);
        }
    }
}
