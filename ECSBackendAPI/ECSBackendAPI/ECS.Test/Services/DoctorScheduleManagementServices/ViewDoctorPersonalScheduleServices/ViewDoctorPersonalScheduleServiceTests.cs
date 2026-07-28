using System.Linq.Expressions;
using ECS.Application.Common.Response;
using ECS.Application.Services.DoctorScheduleManagementServices.ViewDoctorPersonalScheduleServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.DoctorScheduleManagementServices.ViewDoctorPersonalScheduleServices
{
    /// <summary>
    /// Unit tests for <see cref="ViewDoctorPersonalScheduleService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line AND branch coverage on <c>ViewDoctorPersonalScheduleService.cs</c>.
    /// </summary>
    public class ViewDoctorPersonalScheduleServiceTests
    {
        private static readonly Guid DoctorUserId = ViewDoctorPersonalScheduleMockData.DoctorUserId;
        private static readonly Guid DoctorProfileId = ViewDoctorPersonalScheduleMockData.DoctorProfileId;
        private static readonly DateOnly WorkDate = DateOnly.FromDateTime(DateTime.UtcNow);

        private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<DoctorSchedule, Guid, AppDbContext>> _scheduleRepoMock = new();
        private readonly ViewDoctorPersonalScheduleService _sut;

        public ViewDoctorPersonalScheduleServiceTests()
        {
            _sut = new ViewDoctorPersonalScheduleService(
                _doctorRepoMock.Object,
                _scheduleRepoMock.Object);
        }

        // ─────────────────────────────────────────────────────────────────
        // Repository helpers
        // ─────────────────────────────────────────────────────────────────

        private void SetupDoctorRepo(IEnumerable<DoctorProfile> doctors)
        {
            var list = doctors.ToList();
            _doctorRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<DoctorProfile, bool>>>(), It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<DoctorProfile>().Object);
        }

        private void SetupScheduleRepo(IEnumerable<DoctorSchedule> schedules)
        {
            var list = schedules.ToList();
            _scheduleRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<Expression<Func<DoctorSchedule, bool>>>(), It.IsAny<bool>()))
                .Returns(list.BuildMockDbSet<DoctorSchedule>().Object);
        }

        // ==================================================================
        // ==================== ERROR PATH TESTS ===========================
        // ==================================================================

        /// <summary>
        /// TC-01: Doctor not found (no DoctorProfile with matching userId and IsActive) → throws KeyNotFoundException(APP_MESSAGE_4008).
        /// </summary>
        [Fact]
        public async Task Process_DoctorNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var userId = DoctorUserId;
            var request = ViewDoctorPersonalScheduleMockData.GetRequest(workDate: WorkDate);

            //Arrange 2
            SetupDoctorRepo(Array.Empty<DoctorProfile>());

            //Act
            var act = () => _sut.Process(userId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4008.ToString());
            _scheduleRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<DoctorSchedule, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>
        /// TC-02: Doctor inactive (DoctorProfile exists but IsActive = false) → throws KeyNotFoundException(APP_MESSAGE_4008).
        /// Note: Simple mock cannot apply IsActive filter, so inactive = not found scenario.
        /// </summary>
        [Fact]
        public async Task Process_DoctorInactive_ThrowsKeyNotFoundException()
        {
            //Arrange 1
            var userId = DoctorUserId;
            var request = ViewDoctorPersonalScheduleMockData.GetRequest(workDate: WorkDate);

            //Arrange 2
            SetupDoctorRepo(Array.Empty<DoctorProfile>());

            //Act
            var act = () => _sut.Process(userId, request);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4008.ToString());
        }

        // ==================================================================
        // ==================== SUCCESS PATH TESTS =========================
        // ==================================================================

        /// <summary>
        /// TC-03: Happy path - no shift type filter, multiple schedules with rooms and appointments → returns all shifts.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_NoShiftTypeFilter_ReturnsAllShifts()
        {
            //Arrange 1
            var userId = DoctorUserId;
            var request = ViewDoctorPersonalScheduleMockData.GetRequest(workDate: WorkDate, shiftType: null);
            var workDateTime = WorkDate.ToDateTime(TimeOnly.MinValue);

            var appointment = ViewDoctorPersonalScheduleMockData.GetAppointment(
                status: AppointmentStatus.CONFIRMED);
            var timeSlot = ViewDoctorPersonalScheduleMockData.GetTimeSlot(
                scheduleId: ViewDoctorPersonalScheduleMockData.ScheduleId,
                startTime: workDateTime.AddHours(8),
                endTime: workDateTime.AddHours(9),
                appointments: new List<Appointment> { appointment });
            var room = ViewDoctorPersonalScheduleMockData.GetActiveRoom();
            var schedule = ViewDoctorPersonalScheduleMockData.GetSchedule(
                id: ViewDoctorPersonalScheduleMockData.ScheduleId,
                doctorId: DoctorProfileId,
                workDate: workDateTime,
                shiftType: ShiftType.MORNING,
                room: room,
                timeSlots: new List<TimeSlot> { timeSlot });

            //Arrange 2
            SetupDoctorRepo(new[] { ViewDoctorPersonalScheduleMockData.GetActiveDoctorProfile(id: DoctorProfileId, userId: userId) });
            SetupScheduleRepo(new[] { schedule });

            //Act
            var result = await _sut.Process(userId, request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.WorkDate.Should().Be(WorkDate);
            result.Data.Shifts.Should().HaveCount(1);
            result.Data.Shifts[0].ShiftType.Should().Be(ShiftType.MORNING);
            result.Data.Shifts[0].RoomId.Should().Be(room.Id);
            result.Data.Shifts[0].RoomName.Should().Be(room.RoomName);
            result.Data.Shifts[0].Slots.Should().HaveCount(1);
            result.Data.Shifts[0].Slots[0].Status.Should().Be(SlotStatus.AVAILABLE);
        }

        /// <summary>
        /// TC-04: With shift type filter → returns filtered shifts only.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_WithShiftTypeFilter_ReturnsFilteredShifts()
        {
            //Arrange 1
            var userId = DoctorUserId;
            var request = ViewDoctorPersonalScheduleMockData.GetRequest(workDate: WorkDate, shiftType: ShiftType.MORNING);
            var workDateTime = WorkDate.ToDateTime(TimeOnly.MinValue);

            var schedule = ViewDoctorPersonalScheduleMockData.GetSchedule(
                doctorId: DoctorProfileId,
                workDate: workDateTime,
                shiftType: ShiftType.MORNING,
                room: ViewDoctorPersonalScheduleMockData.GetActiveRoom(),
                timeSlots: new List<TimeSlot>
                {
                    ViewDoctorPersonalScheduleMockData.GetTimeSlot(scheduleId: ViewDoctorPersonalScheduleMockData.ScheduleId)
                });

            //Arrange 2
            SetupDoctorRepo(new[] { ViewDoctorPersonalScheduleMockData.GetActiveDoctorProfile(id: DoctorProfileId, userId: userId) });
            SetupScheduleRepo(new[] { schedule });

            //Act
            var result = await _sut.Process(userId, request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Shifts.Should().HaveCount(1);
            result.Data.Shifts[0].ShiftType.Should().Be(ShiftType.MORNING);
        }

        /// <summary>
        /// TC-05: No schedules found for date → returns empty shifts list.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_EmptySchedules_ReturnsEmptyShifts()
        {
            //Arrange 1
            var userId = DoctorUserId;
            var request = ViewDoctorPersonalScheduleMockData.GetRequest(workDate: WorkDate);

            //Arrange 2
            SetupDoctorRepo(new[] { ViewDoctorPersonalScheduleMockData.GetActiveDoctorProfile(id: DoctorProfileId, userId: userId) });
            SetupScheduleRepo(Array.Empty<DoctorSchedule>());

            //Act
            var result = await _sut.Process(userId, request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.WorkDate.Should().Be(WorkDate);
            result.Data.Shifts.Should().BeEmpty();
        }

        /// <summary>
        /// TC-06: Schedule with null TimeSlots → returns null-safe empty slots list.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_ScheduleWithNullTimeSlots_ReturnsNullSafeSlots()
        {
            //Arrange 1
            var userId = DoctorUserId;
            var request = ViewDoctorPersonalScheduleMockData.GetRequest(workDate: WorkDate);
            var workDateTime = WorkDate.ToDateTime(TimeOnly.MinValue);

            var schedule = ViewDoctorPersonalScheduleMockData.GetScheduleWithNullTimeSlots(
                id: ViewDoctorPersonalScheduleMockData.ScheduleId,
                doctorId: DoctorProfileId);
            schedule.WorkDate = workDateTime;

            //Arrange 2
            SetupDoctorRepo(new[] { ViewDoctorPersonalScheduleMockData.GetActiveDoctorProfile(id: DoctorProfileId, userId: userId) });
            SetupScheduleRepo(new[] { schedule });

            //Act
            var result = await _sut.Process(userId, request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Shifts.Should().HaveCount(1);
            result.Data.Shifts[0].Slots.Should().NotBeNull();
            result.Data.Shifts[0].Slots.Should().BeEmpty();
        }

        /// <summary>
        /// TC-07: Slot with null Appointments → returns null-safe empty appointments list.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_SlotWithNullAppointments_ReturnsNullSafeAppointments()
        {
            //Arrange 1
            var userId = DoctorUserId;
            var request = ViewDoctorPersonalScheduleMockData.GetRequest(workDate: WorkDate);
            var workDateTime = WorkDate.ToDateTime(TimeOnly.MinValue);

            var timeSlot = ViewDoctorPersonalScheduleMockData.GetTimeSlotWithNullAppointments(
                scheduleId: ViewDoctorPersonalScheduleMockData.ScheduleId);
            timeSlot.StartTime = workDateTime.AddHours(8);
            timeSlot.EndTime = workDateTime.AddHours(9);

            var schedule = ViewDoctorPersonalScheduleMockData.GetSchedule(
                id: ViewDoctorPersonalScheduleMockData.ScheduleId,
                doctorId: DoctorProfileId,
                workDate: workDateTime,
                room: ViewDoctorPersonalScheduleMockData.GetActiveRoom(),
                timeSlots: new List<TimeSlot> { timeSlot });

            //Arrange 2
            SetupDoctorRepo(new[] { ViewDoctorPersonalScheduleMockData.GetActiveDoctorProfile(id: DoctorProfileId, userId: userId) });
            SetupScheduleRepo(new[] { schedule });

            //Act
            var result = await _sut.Process(userId, request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Shifts[0].Slots[0].Appointments.Should().NotBeNull();
            result.Data.Shifts[0].Slots[0].Appointments.Should().BeEmpty();
        }

        /// <summary>
        /// TC-08: Slot with all cancelled appointments → filters out all cancelled appointments.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_SlotWithAllCancelledAppointments_FiltersOutCancelled()
        {
            //Arrange 1
            var userId = DoctorUserId;
            var request = ViewDoctorPersonalScheduleMockData.GetRequest(workDate: WorkDate);
            var workDateTime = WorkDate.ToDateTime(TimeOnly.MinValue);

            var cancelledAppointments = ViewDoctorPersonalScheduleMockData.GetAllCancelledAppointments();
            var timeSlot = ViewDoctorPersonalScheduleMockData.GetTimeSlot(
                scheduleId: ViewDoctorPersonalScheduleMockData.ScheduleId,
                startTime: workDateTime.AddHours(8),
                endTime: workDateTime.AddHours(9),
                appointments: cancelledAppointments);

            var schedule = ViewDoctorPersonalScheduleMockData.GetSchedule(
                id: ViewDoctorPersonalScheduleMockData.ScheduleId,
                doctorId: DoctorProfileId,
                workDate: workDateTime,
                room: ViewDoctorPersonalScheduleMockData.GetActiveRoom(),
                timeSlots: new List<TimeSlot> { timeSlot });

            //Arrange 2
            SetupDoctorRepo(new[] { ViewDoctorPersonalScheduleMockData.GetActiveDoctorProfile(id: DoctorProfileId, userId: userId) });
            SetupScheduleRepo(new[] { schedule });

            //Act
            var result = await _sut.Process(userId, request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Shifts[0].Slots[0].Appointments.Should().BeEmpty();
        }

        /// <summary>
        /// TC-09: Slot with mixed appointments (confirmed, cancelled, completed) → returns only active appointments.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_SlotWithMixedAppointments_ReturnsOnlyActive()
        {
            //Arrange 1
            var userId = DoctorUserId;
            var request = ViewDoctorPersonalScheduleMockData.GetRequest(workDate: WorkDate);
            var workDateTime = WorkDate.ToDateTime(TimeOnly.MinValue);

            var mixedAppointments = ViewDoctorPersonalScheduleMockData.GetMixedAppointments();
            var timeSlot = ViewDoctorPersonalScheduleMockData.GetTimeSlot(
                scheduleId: ViewDoctorPersonalScheduleMockData.ScheduleId,
                startTime: workDateTime.AddHours(8),
                endTime: workDateTime.AddHours(9),
                appointments: mixedAppointments);

            var schedule = ViewDoctorPersonalScheduleMockData.GetSchedule(
                id: ViewDoctorPersonalScheduleMockData.ScheduleId,
                doctorId: DoctorProfileId,
                workDate: workDateTime,
                room: ViewDoctorPersonalScheduleMockData.GetActiveRoom(),
                timeSlots: new List<TimeSlot> { timeSlot });

            //Arrange 2
            SetupDoctorRepo(new[] { ViewDoctorPersonalScheduleMockData.GetActiveDoctorProfile(id: DoctorProfileId, userId: userId) });
            SetupScheduleRepo(new[] { schedule });

            //Act
            var result = await _sut.Process(userId, request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            var appointments = result.Data!.Shifts[0].Slots[0].Appointments;
            appointments.Should().HaveCount(2);
            appointments.All(a => a.Status != AppointmentStatus.CANCELLED.ToString()).Should().BeTrue();
            appointments.Should().Contain(a => a.Status == AppointmentStatus.CONFIRMED.ToString());
            appointments.Should().Contain(a => a.Status == AppointmentStatus.COMPLETED.ToString());
        }

        /// <summary>
        /// TC-10: Schedule with Room → includes Room info (Id, RoomName).
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_ScheduleWithRoom_IncludesRoomInfo()
        {
            //Arrange 1
            var userId = DoctorUserId;
            var request = ViewDoctorPersonalScheduleMockData.GetRequest(workDate: WorkDate);
            var workDateTime = WorkDate.ToDateTime(TimeOnly.MinValue);
            var room = ViewDoctorPersonalScheduleMockData.GetActiveRoom(
                id: ViewDoctorPersonalScheduleMockData.RoomId,
                roomName: "Phong Kham A");

            var schedule = ViewDoctorPersonalScheduleMockData.GetSchedule(
                id: ViewDoctorPersonalScheduleMockData.ScheduleId,
                doctorId: DoctorProfileId,
                workDate: workDateTime,
                room: room,
                timeSlots: new List<TimeSlot>
                {
                    ViewDoctorPersonalScheduleMockData.GetTimeSlot(scheduleId: ViewDoctorPersonalScheduleMockData.ScheduleId)
                });

            //Arrange 2
            SetupDoctorRepo(new[] { ViewDoctorPersonalScheduleMockData.GetActiveDoctorProfile(id: DoctorProfileId, userId: userId) });
            SetupScheduleRepo(new[] { schedule });

            //Act
            var result = await _sut.Process(userId, request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Shifts[0].RoomId.Should().Be(room.Id);
            result.Data.Shifts[0].RoomName.Should().Be("Phong Kham A");
        }

        /// <summary>
        /// TC-11: Schedule without Room (null) → includes null Room info.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_ScheduleWithoutRoom_IncludesNullRoomInfo()
        {
            //Arrange 1
            var userId = DoctorUserId;
            var request = ViewDoctorPersonalScheduleMockData.GetRequest(workDate: WorkDate);
            var workDateTime = WorkDate.ToDateTime(TimeOnly.MinValue);

            var schedule = ViewDoctorPersonalScheduleMockData.GetScheduleWithoutRoom(
                id: ViewDoctorPersonalScheduleMockData.ScheduleId,
                doctorId: DoctorProfileId);
            schedule.WorkDate = workDateTime;

            //Arrange 2
            SetupDoctorRepo(new[] { ViewDoctorPersonalScheduleMockData.GetActiveDoctorProfile(id: DoctorProfileId, userId: userId) });
            SetupScheduleRepo(new[] { schedule });

            //Act
            var result = await _sut.Process(userId, request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Shifts[0].RoomId.Should().BeNull();
            result.Data.Shifts[0].RoomName.Should().BeNull();
        }

        /// <summary>
        /// TC-12: Multiple shifts (MORNING, AFTERNOON) sorted by ShiftType → returns ordered shifts.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_MultipleShifts_ReturnsSortedByShiftType()
        {
            //Arrange 1
            var userId = DoctorUserId;
            var request = ViewDoctorPersonalScheduleMockData.GetRequest(workDate: WorkDate, shiftType: null);
            var workDateTime = WorkDate.ToDateTime(TimeOnly.MinValue);

            var morningSchedule = ViewDoctorPersonalScheduleMockData.GetSchedule(
                id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                doctorId: DoctorProfileId,
                workDate: workDateTime,
                shiftType: ShiftType.MORNING,
                room: ViewDoctorPersonalScheduleMockData.GetActiveRoom(),
                timeSlots: new List<TimeSlot>
                {
                    ViewDoctorPersonalScheduleMockData.GetTimeSlot(scheduleId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"))
                });

            var afternoonSchedule = ViewDoctorPersonalScheduleMockData.GetSchedule(
                id: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                doctorId: DoctorProfileId,
                workDate: workDateTime,
                shiftType: ShiftType.AFTERNOON,
                room: ViewDoctorPersonalScheduleMockData.GetActiveRoom(),
                timeSlots: new List<TimeSlot>
                {
                    ViewDoctorPersonalScheduleMockData.GetTimeSlot(scheduleId: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"))
                });

            //Arrange 2
            SetupDoctorRepo(new[] { ViewDoctorPersonalScheduleMockData.GetActiveDoctorProfile(id: DoctorProfileId, userId: userId) });
            SetupScheduleRepo(new[] { afternoonSchedule, morningSchedule }); // Intentionally unsorted input

            //Act
            var result = await _sut.Process(userId, request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Shifts.Should().HaveCount(2);
            result.Data.Shifts[0].ShiftType.Should().Be(ShiftType.MORNING);
            result.Data.Shifts[1].ShiftType.Should().Be(ShiftType.AFTERNOON);
        }

        /// <summary>
        /// TC-13: Slot mapping includes correct fields (StartTime, EndTime, MaxPatients, CurrentPatients, Status).
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_SlotMapping_IncludesCorrectFields()
        {
            //Arrange 1
            var userId = DoctorUserId;
            var request = ViewDoctorPersonalScheduleMockData.GetRequest(workDate: WorkDate);
            var workDateTime = WorkDate.ToDateTime(TimeOnly.MinValue);
            var startTime = workDateTime.AddHours(8);
            var endTime = workDateTime.AddHours(9);

            var timeSlot = ViewDoctorPersonalScheduleMockData.GetTimeSlot(
                scheduleId: ViewDoctorPersonalScheduleMockData.ScheduleId,
                startTime: startTime,
                endTime: endTime,
                status: SlotStatus.BOOKED,
                maxPatients: 5,
                currentPatients: 3,
                appointments: new List<Appointment>
                {
                    ViewDoctorPersonalScheduleMockData.GetAppointment(status: AppointmentStatus.CONFIRMED)
                });

            var schedule = ViewDoctorPersonalScheduleMockData.GetSchedule(
                id: ViewDoctorPersonalScheduleMockData.ScheduleId,
                doctorId: DoctorProfileId,
                workDate: workDateTime,
                room: ViewDoctorPersonalScheduleMockData.GetActiveRoom(),
                timeSlots: new List<TimeSlot> { timeSlot });

            //Arrange 2
            SetupDoctorRepo(new[] { ViewDoctorPersonalScheduleMockData.GetActiveDoctorProfile(id: DoctorProfileId, userId: userId) });
            SetupScheduleRepo(new[] { schedule });

            //Act
            var result = await _sut.Process(userId, request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();

            var slot = result.Data!.Shifts[0].Slots[0];
            slot.StartTime.Should().Be(startTime);
            slot.EndTime.Should().Be(endTime);
            slot.MaxPatients.Should().Be(5);
            slot.CurrentPatients.Should().Be(3);
            slot.Status.Should().Be(SlotStatus.BOOKED);
        }

        /// <summary>
        /// TC-14: Appointment mapping includes correct fields (AppointmentId, PatientId, PatientName, PatientPhone, Status, Symptoms).
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_AppointmentMapping_IncludesCorrectFields()
        {
            //Arrange 1
            var userId = DoctorUserId;
            var request = ViewDoctorPersonalScheduleMockData.GetRequest(workDate: WorkDate);
            var workDateTime = WorkDate.ToDateTime(TimeOnly.MinValue);

            var appointment = ViewDoctorPersonalScheduleMockData.GetAppointment(
                id: ViewDoctorPersonalScheduleMockData.AppointmentId,
                patientId: ViewDoctorPersonalScheduleMockData.PatientId,
                status: AppointmentStatus.CONFIRMED,
                symptoms: "Dau mat kinh");

            var timeSlot = ViewDoctorPersonalScheduleMockData.GetTimeSlot(
                scheduleId: ViewDoctorPersonalScheduleMockData.ScheduleId,
                startTime: workDateTime.AddHours(8),
                endTime: workDateTime.AddHours(9),
                appointments: new List<Appointment> { appointment });

            var schedule = ViewDoctorPersonalScheduleMockData.GetSchedule(
                id: ViewDoctorPersonalScheduleMockData.ScheduleId,
                doctorId: DoctorProfileId,
                workDate: workDateTime,
                room: ViewDoctorPersonalScheduleMockData.GetActiveRoom(),
                timeSlots: new List<TimeSlot> { timeSlot });

            //Arrange 2
            SetupDoctorRepo(new[] { ViewDoctorPersonalScheduleMockData.GetActiveDoctorProfile(id: DoctorProfileId, userId: userId) });
            SetupScheduleRepo(new[] { schedule });

            //Act
            var result = await _sut.Process(userId, request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();

            var mappedAppointment = result.Data!.Shifts[0].Slots[0].Appointments[0];
            mappedAppointment.AppointmentId.Should().Be(ViewDoctorPersonalScheduleMockData.AppointmentId);
            mappedAppointment.PatientId.Should().Be(ViewDoctorPersonalScheduleMockData.PatientId);
            mappedAppointment.PatientName.Should().Be("Nguyen Van B");
            mappedAppointment.PatientPhone.Should().Be("0909123456");
            mappedAppointment.Status.Should().Be(AppointmentStatus.CONFIRMED.ToString());
            mappedAppointment.Symptoms.Should().Be("Dau mat kinh");
        }

        /// <summary>
        /// TC-15: Slot note is correctly mapped from schedule.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_NoteMapping_IncludesCorrectNote()
        {
            //Arrange 1
            var userId = DoctorUserId;
            var request = ViewDoctorPersonalScheduleMockData.GetRequest(workDate: WorkDate);
            var workDateTime = WorkDate.ToDateTime(TimeOnly.MinValue);

            var schedule = ViewDoctorPersonalScheduleMockData.GetSchedule(
                id: ViewDoctorPersonalScheduleMockData.ScheduleId,
                doctorId: DoctorProfileId,
                workDate: workDateTime,
                shiftType: ShiftType.MORNING,
                room: ViewDoctorPersonalScheduleMockData.GetActiveRoom(),
                note: "Hen kham lai",
                timeSlots: new List<TimeSlot>
                {
                    ViewDoctorPersonalScheduleMockData.GetTimeSlot(scheduleId: ViewDoctorPersonalScheduleMockData.ScheduleId)
                });

            //Arrange 2
            SetupDoctorRepo(new[] { ViewDoctorPersonalScheduleMockData.GetActiveDoctorProfile(id: DoctorProfileId, userId: userId) });
            SetupScheduleRepo(new[] { schedule });

            //Act
            var result = await _sut.Process(userId, request);

            //Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Shifts[0].Note.Should().Be("Hen kham lai");
        }
    }
}
