using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Application.Services.DoctorAppointmentPatientManagementServices.GetMyQueueListServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ECS.Test.Services.DoctorAppointmentPatientManagementServices.GetMyQueueListServices
{
    public class GetMyQueueListServiceTests : IDisposable
    {
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly AppDbContext _context;
        private readonly GetMyQueueListService _service;

        public GetMyQueueListServiceTests()
        {
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _service = new GetMyQueueListService(
                _httpContextAccessorMock.Object,
                _context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void SetupHttpContext(string? userIdClaim = null, bool setNullContext = false, bool setNullClaim = false)
        {
            if (setNullContext)
            {
                _httpContextAccessorMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);
                return;
            }

            var claims = new List<Claim>();
            if (userIdClaim != null && !setNullClaim)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userIdClaim));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };

            _httpContextAccessorMock.Setup(h => h.HttpContext).Returns(httpContext);
        }

        // ── Test Cases ────────────────────────────────────────────────────────────

        [Fact]
        public async Task Process_HttpContextNull_ReturnsUnauthorized4011()
        {
            //Arrange 1
            var testDate = new DateOnly(2025, 5, 10);

            //Arrange 2
            SetupHttpContext(setNullContext: true);

            //Act
            var result = await _service.Process(testDate);

            //Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4011.ToString());
        }

        [Fact]
        public async Task Process_UserClaimMissing_ReturnsUnauthorized4011()
        {
            //Arrange 1
            var testDate = new DateOnly(2025, 5, 10);

            //Arrange 2
            SetupHttpContext(setNullClaim: true);

            //Act
            var result = await _service.Process(testDate);

            //Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4011.ToString());
        }

        [Fact]
        public async Task Process_UserClaimInvalidGuid_ReturnsUnauthorized4011()
        {
            //Arrange 1
            var testDate = new DateOnly(2025, 5, 10);

            //Arrange 2
            SetupHttpContext(userIdClaim: "invalid-guid-string");

            //Act
            var result = await _service.Process(testDate);

            //Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4011.ToString());
        }

        [Fact]
        public async Task Process_DoctorNotFoundOrInactive_ReturnsUnauthorized4011()
        {
            //Arrange 1
            var testDate = new DateOnly(2025, 5, 10);
            var doctorUserGuid = Guid.NewGuid();

            //Arrange 2
            SetupHttpContext(userIdClaim: doctorUserGuid.ToString());

            //Act
            var result = await _service.Process(testDate);

            //Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4011.ToString());
        }

        [Fact]
        public async Task Process_ValidDoctorWithQueues_ReturnsSuccess2001WithMappedQueueItemsAndCounts()
        {
            //Arrange 1
            var testDate = new DateOnly(2025, 5, 10);
            var targetDateTime = testDate.ToDateTime(TimeOnly.MinValue);

            var doctorProfile = GetMyQueueListMockData.GetActiveDoctorProfile();
            _context.Set<DoctorProfile>().Add(doctorProfile);

            var patient = GetMyQueueListMockData.GetPatientProfile();
            _context.Set<PatientProfile>().Add(patient);

            var room = GetMyQueueListMockData.GetRoom();
            var service = GetMyQueueListMockData.GetService();
            var slot = GetMyQueueListMockData.GetTimeSlot();

            var appt1 = GetMyQueueListMockData.GetAppointment(doctorProfile.Id, targetDateTime, patient, service, slot, true, true);
            var appt2 = GetMyQueueListMockData.GetAppointment(doctorProfile.Id, targetDateTime, patient, service, slot, false, false);
            var appt3 = GetMyQueueListMockData.GetAppointment(doctorProfile.Id, targetDateTime, patient, service, slot, true, false);

            var queue1 = GetMyQueueListMockData.GetQueueItem(appt1, room, 1, QueueStatus.WAITING);
            var queue2 = GetMyQueueListMockData.GetQueueItem(appt2, room, 2, QueueStatus.CALLING);
            var queue3 = GetMyQueueListMockData.GetQueueItem(appt3, room, 3, QueueStatus.COMPLETED);

            _context.Set<Appointment>().AddRange(appt1, appt2, appt3);
            _context.Set<Queue>().AddRange(queue1, queue2, queue3);
            await _context.SaveChangesAsync();

            //Arrange 2
            SetupHttpContext(userIdClaim: GetMyQueueListMockData.DefaultDoctorUserId.ToString());

            //Act
            var result = await _service.Process(testDate);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2001.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Date.Should().Be(testDate);
            result.Data.TotalPatients.Should().Be(3);
            result.Data.WaitingCount.Should().Be(1);
            result.Data.InProgressCount.Should().Be(1);
            result.Data.CompletedCount.Should().Be(1);
            result.Data.Items.Should().HaveCount(3);

            var item1 = result.Data.Items.First(i => i.QueueNumber == 1);
            item1.PatientName.Should().Be("Tran Van C");
            item1.Status.Should().Be(QueueStatus.WAITING);
            item1.StatusText.Should().Be("Đang chờ");
            item1.HasMedicalRecord.Should().BeTrue();
            item1.HasPreliminaryDiagnosis.Should().BeTrue();

            var item2 = result.Data.Items.First(i => i.QueueNumber == 2);
            item2.Status.Should().Be(QueueStatus.CALLING);
            item2.StatusText.Should().Be("Đang khám");
            item2.HasMedicalRecord.Should().BeFalse();
            item2.HasPreliminaryDiagnosis.Should().BeFalse();

            var item3 = result.Data.Items.First(i => i.QueueNumber == 3);
            item3.Status.Should().Be(QueueStatus.COMPLETED);
            item3.StatusText.Should().Be("Đã khám xong");
        }

        /// <summary>
        /// TC added for the 6-step EMR workflow: a queue item with IN_PROGRESS
        /// status (i.e. the doctor has saved the medical record — Step 3 of the
        /// EMR workflow — but has NOT yet done the medical record summary +
        /// prescription) must be reported as "DangKham" / Examination in
        /// progress (NOT as "Completed") and counted in <c>InProgressCount</c>,
        /// never in <c>CompletedCount</c>.
        /// </summary>
        [Fact]
        public async Task Process_QueueItemInProgress_ShowsDangKhamNotDakhamXong()
        {
            //Arrange
            var testDate = new DateOnly(2025, 5, 10);
            var targetDateTime = testDate.ToDateTime(TimeOnly.MinValue);

            var doctorProfile = GetMyQueueListMockData.GetActiveDoctorProfile();
            _context.Set<DoctorProfile>().Add(doctorProfile);

            var patient = GetMyQueueListMockData.GetPatientProfile();
            _context.Set<PatientProfile>().Add(patient);
            var room = GetMyQueueListMockData.GetRoom();
            _context.Set<FacilityRoom>().Add(room);
            var service = GetMyQueueListMockData.GetService();
            _context.Set<Service>().Add(service);
            var slot = GetMyQueueListMockData.GetTimeSlot();
            _context.Set<TimeSlot>().Add(slot);

            // apptWithRecord: Step 3 of the EMR workflow is done — the medical record
            // has been saved. We use includeMedicalRecord: true so the helper
            // attaches a fresh MedicalRecord to the appointment entity.
            var apptWithRecord = GetMyQueueListMockData.GetAppointment(doctorProfile.Id, targetDateTime, patient, service, slot, true, false);
            var queue = GetMyQueueListMockData.GetQueueItem(apptWithRecord, room, 1, QueueStatus.IN_PROGRESS);

            _context.Set<Appointment>().Add(apptWithRecord);
            _context.Set<Queue>().Add(queue);
            await _context.SaveChangesAsync();

            SetupHttpContext(userIdClaim: GetMyQueueListMockData.DefaultDoctorUserId.ToString());

            //Act
            var result = await _service.Process(testDate);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2001.ToString());
            result.Data!.WaitingCount.Should().Be(0);
            result.Data.InProgressCount.Should().Be(1);
            result.Data.CompletedCount.Should().Be(0);

            var item = result.Data.Items.First();
            item.Status.Should().Be(QueueStatus.IN_PROGRESS);
            item.StatusText.Should().Be("Đang khám");
            // Critical guard: we must never display "Completed" to the UI until
            // Step 5 (summary) + Step 6 (prescription) are both filled in.
            item.StatusText.Should().NotBe("Đã khám xong");
            item.HasMedicalRecord.Should().BeTrue();
            item.CompletedAt.Should().BeNull("IN_PROGRESS means the doctor is still waiting on Step 5+6 — no CompletedAt stamp yet");
        }

        [Fact]
        public async Task Process_ValidDoctorWithMinimalQueueItemsAndUnknownStatus_ReturnsSuccess2001WithDefaultValues()
        {
            //Arrange 1
            var testDate = new DateOnly(2025, 5, 10);
            var targetDateTime = testDate.ToDateTime(TimeOnly.MinValue);

            var doctorProfile = GetMyQueueListMockData.GetActiveDoctorProfile();
            _context.Set<DoctorProfile>().Add(doctorProfile);

            var minimalPatient = new PatientProfile
            {
                Id = Guid.NewGuid(),
                FullName = "", // Empty name to trigger "Unknown" fallback
                Dob = DateTime.MinValue,
                Gender = Gender.FEMALE
            };
            _context.Set<PatientProfile>().Add(minimalPatient);

            var apptMinimal = GetMyQueueListMockData.GetAppointment(doctorProfile.Id, targetDateTime, patient: minimalPatient, service: null, slot: null, includeMedicalRecord: false, includePreliminaryDiagnosis: false);
            apptMinimal.BookingSource = ""; // To trigger "UNKNOWN" fallback

            var unknownStatus = (QueueStatus)999;
            var queueMinimal = GetMyQueueListMockData.GetQueueItem(apptMinimal, room: null, queueNumber: 10, status: unknownStatus);

            _context.Set<Appointment>().Add(apptMinimal);
            _context.Set<Queue>().Add(queueMinimal);
            await _context.SaveChangesAsync();

            //Arrange 2
            SetupHttpContext(userIdClaim: GetMyQueueListMockData.DefaultDoctorUserId.ToString());

            //Act
            var result = await _service.Process(testDate);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2001.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(1);

            var item = result.Data.Items[0];
            item.PatientName.Should().Be("Unknown");
            item.PatientGender.Should().Be("FEMALE");
            item.BookingSource.Should().Be("UNKNOWN");
            item.RoomId.Should().BeNull();
            item.RoomName.Should().BeNull();
            item.ServiceName.Should().BeNull();
            item.StatusText.Should().Be("999");
        }

        [Fact]
        public async Task Process_HttpContextExceptionThrown_ReturnsInternalServerError5001()
        {
            //Arrange 1
            var testDate = new DateOnly(2025, 5, 10);

            //Arrange 2
            _httpContextAccessorMock.Setup(h => h.HttpContext)
                .Throws(new InvalidOperationException("Simulated HTTP Context Exception"));

            //Act
            var result = await _service.Process(testDate);

            //Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_5001.ToString());
        }
    }
}
