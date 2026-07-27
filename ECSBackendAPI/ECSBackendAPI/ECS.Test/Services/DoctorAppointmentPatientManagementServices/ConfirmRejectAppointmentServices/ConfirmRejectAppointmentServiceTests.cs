using System.Linq.Expressions;
using ECS.Application.Services.DoctorAppointmentPatientManagementServices.ConfirmRejectAppointmentServices;
using ECS.Application.Services.PatientAppointmentManagementServices.ViewNotificationListServices;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.DoctorAppointmentPatientManagementServices.ConfirmRejectAppointmentServices;

public class ConfirmRejectAppointmentServiceTests : IDisposable
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DoctorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid AppointmentId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid PatientId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepo = new();
    private readonly Mock<IRepositoryBaseAsync<Appointment, Guid, AppDbContext>> _appointmentCommandRepo = new();
    private readonly Mock<IRepositoryQueryBase<Appointment, Guid, AppDbContext>> _appointmentQueryRepo = new();
    private readonly Mock<IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext>> _slotRepo = new();
    private readonly Mock<INotificationCreationService> _notification = new();
    private readonly AppDbContext _context;
    private readonly ConfirmRejectAppointmentService _sut;

    public ConfirmRejectAppointmentServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _context = new AppDbContext(options);
        _sut = new ConfirmRejectAppointmentService(_doctorRepo.Object, _appointmentCommandRepo.Object,
            _appointmentQueryRepo.Object, _slotRepo.Object, _notification.Object, _context);
        _appointmentCommandRepo.Setup(x => x.UpdateAsync(It.IsAny<Appointment>())).Returns(Task.CompletedTask);
        _appointmentCommandRepo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _slotRepo.Setup(x => x.UpdateAsync(It.IsAny<TimeSlot>())).Returns(Task.CompletedTask);
        _notification.Setup(x => x.Process(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(ECS.Application.Common.Response.ApiResponse<string>.Success("OK", ""));
    }

    public void Dispose() => _context.Dispose();

    private void SetupDoctor(DoctorProfile? doctor)
    {
        var rows = doctor is null ? Array.Empty<DoctorProfile>() : new[] { doctor };
        _doctorRepo.Setup(x => x.FindByCondition(It.IsAny<Expression<Func<DoctorProfile, bool>>>(), It.IsAny<bool>()))
            .Returns(rows.BuildMockDbSet<DoctorProfile>().Object);
    }

    private void SetupAppointment(Appointment? appointment)
    {
        var rows = appointment is null ? Array.Empty<Appointment>() : new[] { appointment };
        _appointmentQueryRepo.Setup(x => x.FindByCondition(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<bool>()))
            .Returns(rows.BuildMockDbSet<Appointment>().Object);
    }

    private static DoctorProfile Doctor(bool withUser = true) => new()
    {
        Id = DoctorId, UserId = UserId, IsActive = true,
        User = withUser ? new User { Id = UserId, FullName = "Doctor" } : null!
    };

    private static Appointment Appointment(TimeSlot? slot, User? patientUser = null) => new()
    {
        Id = AppointmentId, DoctorId = DoctorId, PatientId = PatientId, Slot = slot,
        Patient = new PatientProfile { Id = PatientId, FullName = "Patient", User = patientUser },
        Doctor = Doctor(), AppointmentDate = DateTime.UtcNow, Status = AppointmentStatus.PENDING
    };

    [Fact]
    public async Task Process_MissingDoctor_ThrowsDoctorNotFound()
    {
        //Arrange 1
        SetupDoctor(null);
        //Arrange 2
        //Act
        var act = () => _sut.Process(UserId, AppointmentId, new ConfirmRejectAppointmentRequest());
        //Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Process_MissingAppointment_ThrowsAppointmentNotFound()
    {
        //Arrange 1
        SetupDoctor(Doctor()); SetupAppointment(null);
        //Arrange 2
        //Act
        var act = () => _sut.Process(UserId, AppointmentId, new ConfirmRejectAppointmentRequest());
        //Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Process_ConfirmWithReason_UpdatesBookedAndNotifies()
    {
        //Arrange 1
        var appointment = Appointment(null, new User { Id = UserId });
        SetupDoctor(Doctor()); SetupAppointment(appointment);
        //Arrange 2
        //Act
        var result = await _sut.Process(UserId, AppointmentId, new ConfirmRejectAppointmentRequest { Decision = AppointmentDecision.CONFIRM, RejectReason = "  accepted  " });
        //Assert
        result.Data!.Status.Should().Be("BOOKED"); appointment.NoteReason.Should().Be("accepted");
        _notification.Verify(x => x.Process(UserId, "APPOINTMENT_CONFIRMED", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Process_RejectWithReasonAndBookedSlot_ReleasesAndNotifies()
    {
        //Arrange 1
        var slot = new TimeSlot { CurrentPatients = 2, MaxPatients = 3, Status = SlotStatus.BOOKED };
        var appointment = Appointment(slot, new User { Id = UserId });
        SetupDoctor(Doctor()); SetupAppointment(appointment);
        //Arrange 2
        //Act
        var result = await _sut.Process(UserId, AppointmentId, new ConfirmRejectAppointmentRequest { Decision = AppointmentDecision.REJECT, RejectReason = "  no  " });
        //Assert
        result.Data!.Status.Should().Be("CANCELLED"); slot.CurrentPatients.Should().Be(1); slot.Status.Should().Be(SlotStatus.AVAILABLE);
        _slotRepo.Verify(x => x.UpdateAsync(slot), Times.Once);
        _notification.Verify(x => x.Process(UserId, "APPOINTMENT_REJECTED", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Process_RejectWithZeroSlotAndNoPatientUser_ReturnsSuccessWithoutNotification()
    {
        //Arrange 1
        var appointment = Appointment(new TimeSlot { CurrentPatients = 0 }, null);
        SetupDoctor(Doctor()); SetupAppointment(appointment);
        //Arrange 2
        //Act
        var result = await _sut.Process(UserId, AppointmentId, new ConfirmRejectAppointmentRequest { Decision = AppointmentDecision.REJECT });
        //Assert
        result.Data!.Status.Should().Be("CANCELLED");
        _slotRepo.Verify(x => x.UpdateAsync(It.IsAny<TimeSlot>()), Times.Never);
        _notification.Verify(x => x.Process(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Process_RejectWithNullSlot_ReturnsSuccess()
    {
        //Arrange 1
        var appointment = Appointment(null, new User { Id = UserId }); SetupDoctor(Doctor()); SetupAppointment(appointment);
        //Arrange 2
        //Act
        var result = await _sut.Process(UserId, AppointmentId, new ConfirmRejectAppointmentRequest { Decision = AppointmentDecision.REJECT, RejectReason = " " });
        //Assert
        result.Data!.Status.Should().Be("CANCELLED"); _notification.Verify(x => x.Process(UserId, "APPOINTMENT_REJECTED", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Process_ConfirmWithoutReason_LeavesNoteNullAndNotifies()
    {
        //Arrange 1
        var appointment = Appointment(null, new User { Id = UserId });
        SetupDoctor(Doctor()); SetupAppointment(appointment);
        //Arrange 2
        //Act
        var result = await _sut.Process(UserId, AppointmentId, new ConfirmRejectAppointmentRequest { Decision = AppointmentDecision.CONFIRM });
        //Assert
        result.Data!.Status.Should().Be("BOOKED");
        appointment.NoteReason.Should().BeNull();
        _notification.Verify(x => x.Process(UserId, "APPOINTMENT_CONFIRMED", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Process_RejectWithoutReason_ReleasesSlotAndLeavesNoteNull()
    {
        //Arrange 1
        var slot = new TimeSlot { CurrentPatients = 1, MaxPatients = 3, Status = SlotStatus.BOOKED };
        var appointment = Appointment(slot, new User { Id = UserId });
        SetupDoctor(Doctor()); SetupAppointment(appointment);
        //Arrange 2
        //Act
        var result = await _sut.Process(UserId, AppointmentId, new ConfirmRejectAppointmentRequest { Decision = AppointmentDecision.REJECT });
        //Assert
        result.Data!.Status.Should().Be("CANCELLED");
        appointment.NoteReason.Should().BeNull();
        slot.CurrentPatients.Should().Be(0);
        slot.Status.Should().Be(SlotStatus.AVAILABLE);
    }

    [Fact]
    public async Task Process_RejectWithFullSlotAfterDecrement_LeavesStatusBooked()
    {
        //Arrange 1
        var slot = new TimeSlot { CurrentPatients = 3, MaxPatients = 2, Status = SlotStatus.BOOKED };
        var appointment = Appointment(slot, new User { Id = UserId });
        SetupDoctor(Doctor()); SetupAppointment(appointment);
        //Arrange 2
        //Act
        await _sut.Process(UserId, AppointmentId, new ConfirmRejectAppointmentRequest { Decision = AppointmentDecision.REJECT, RejectReason = "x" });
        //Assert
        slot.CurrentPatients.Should().Be(2);
        slot.Status.Should().Be(SlotStatus.BOOKED);
    }

    [Fact]
    public async Task Process_RejectWithNonBookedSlot_LeavesStatusUnchanged()
    {
        //Arrange 1
        var slot = new TimeSlot { CurrentPatients = 1, MaxPatients = 5, Status = SlotStatus.AVAILABLE };
        var appointment = Appointment(slot, new User { Id = UserId });
        SetupDoctor(Doctor()); SetupAppointment(appointment);
        //Arrange 2
        //Act
        await _sut.Process(UserId, AppointmentId, new ConfirmRejectAppointmentRequest { Decision = AppointmentDecision.REJECT, RejectReason = "x" });
        //Assert
        slot.CurrentPatients.Should().Be(0);
        slot.Status.Should().Be(SlotStatus.AVAILABLE);
    }

    [Fact]
    public async Task Process_NoLinkedUserButFallbackUserExists_NotifiesFallbackUser()
    {
        //Arrange 1
        var fallbackUserId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        _context.UserPatients.Add(new UserPatient { UserId = fallbackUserId, PatientId = PatientId, Relationship = "self" });
        await _context.SaveChangesAsync();
        var appointment = Appointment(null, patientUser: null);
        SetupDoctor(Doctor()); SetupAppointment(appointment);
        //Arrange 2
        //Act
        await _sut.Process(UserId, AppointmentId, new ConfirmRejectAppointmentRequest { Decision = AppointmentDecision.CONFIRM, RejectReason = "ok" });
        //Assert
        _notification.Verify(x => x.Process(fallbackUserId, "APPOINTMENT_CONFIRMED", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Process_LinkedUserIdEmptyAndFallbackUserExists_NotifiesFallbackUser()
    {
        //Arrange 1
        // The Patient navigation is non-null but its User has Id=Guid.Empty.
        // After the refactor, the first guard `patient?.User is { } linkedUser && linkedUser.Id != Guid.Empty`
        // is false, so we fall through to the fallback query. The fallback query
        // returns the seeded fallback user, which sends the notification.
        var fallbackUserId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        _context.UserPatients.Add(new UserPatient { UserId = fallbackUserId, PatientId = PatientId, Relationship = "self" });
        await _context.SaveChangesAsync();
        var appointment = new Appointment
        {
            Id = AppointmentId, DoctorId = DoctorId, PatientId = PatientId, Slot = null,
            Patient = new PatientProfile { Id = PatientId, FullName = "LinkedPatient", User = new User { Id = Guid.Empty, FullName = "LinkedUser" } },
            Doctor = Doctor(), AppointmentDate = DateTime.UtcNow, Status = AppointmentStatus.PENDING
        };
        SetupDoctor(Doctor()); SetupAppointment(appointment);
        //Arrange 2
        //Act
        await _sut.Process(UserId, AppointmentId, new ConfirmRejectAppointmentRequest { Decision = AppointmentDecision.CONFIRM, RejectReason = "ok" });
        //Assert
        _notification.Verify(x => x.Process(fallbackUserId, "APPOINTMENT_CONFIRMED", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Process_ConfirmWhenDecisionIsUnknown_DefaultsToRejectionTemplate()
    {
        //Arrange 1
        // Cast the enum to a value outside the defined range so the switch falls
        // through and NotifyPatientAsync takes the false branch of the ternary,
        // i.e. templateKey = "APPOINTMENT_REJECTED". This covers the second
        // ternary branch with decision != CONFIRM that was not exercised by
        // any other test.
        var appointment = Appointment(null, new User { Id = UserId });
        SetupDoctor(Doctor()); SetupAppointment(appointment);
        //Arrange 2
        //Act
        var result = await _sut.Process(UserId, AppointmentId, new ConfirmRejectAppointmentRequest { Decision = (AppointmentDecision)999, RejectReason = "x" });
        //Assert
        result.Data.Should().NotBeNull();
        _notification.Verify(x => x.Process(UserId, "APPOINTMENT_REJECTED", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Process_RejectionWithNullAppointmentPatient_FallsBackWithoutNotification()
    {
        //Arrange 1
        // Patient is fully null so patientUserId becomes null on the first read,
        // then the DbSet is queried but returns null (no row seeded), and finally
        // the second guard returns without sending any notification. This
        // exercises the case where the appointment has no Patient at all.
        var appointment = new Appointment
        {
            Id = AppointmentId, DoctorId = DoctorId, PatientId = PatientId, Slot = null,
            Patient = null!,
            Doctor = Doctor(), AppointmentDate = DateTime.UtcNow, Status = AppointmentStatus.PENDING
        };
        SetupDoctor(Doctor()); SetupAppointment(appointment);
        //Arrange 2
        //Act
        var result = await _sut.Process(UserId, AppointmentId, new ConfirmRejectAppointmentRequest { Decision = AppointmentDecision.REJECT, RejectReason = "x" });
        //Assert
        result.Data!.Status.Should().Be("CANCELLED");
        _notification.Verify(x => x.Process(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Process_ConfirmWhenDoctorUserIsNull_UsesEmptyDoctorNameInPayload()
    {
        //Arrange 1
        // doctor.User == null forces the `doctor.User?.FullName ?? ""` branch
        // to take the "null coalesce" path; combined with the
        // appointment.Patient?.FullName ?? "" branch this gives 100% on the
        // dictionary initializer's null-coalescing expressions.
        var appointment = Appointment(null, new User { Id = UserId });
        var doctor = new DoctorProfile { Id = DoctorId, UserId = UserId, IsActive = true, User = null! };
        SetupDoctor(doctor); SetupAppointment(appointment);
        //Arrange 2
        //Act
        await _sut.Process(UserId, AppointmentId, new ConfirmRejectAppointmentRequest { Decision = AppointmentDecision.CONFIRM, RejectReason = "ok" });
        //Assert
        _notification.Verify(x => x.Process(UserId, "APPOINTMENT_CONFIRMED", It.Is<string>(s => s.Contains("\"DoctorName\":\"\"") && s.Contains("\"PatientName\":\"Patient\""))), Times.Once);
    }

    [Fact]
    public async Task Process_RejectWhenPatientUserIsNull_AndFallbackUserExists_NotifiesFallback()
    {
        //Arrange 1
        // Cover the remaining uncovered condition by exercising the
        // patientUserId is null + valid fallback case while running through the
        // rejection template branch (i.e. decision != CONFIRM).
        var fallbackUserId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        _context.UserPatients.Add(new UserPatient { UserId = fallbackUserId, PatientId = PatientId, Relationship = "self" });
        await _context.SaveChangesAsync();
        var appointment = Appointment(null, patientUser: null);
        SetupDoctor(Doctor()); SetupAppointment(appointment);
        //Arrange 2
        //Act
        await _sut.Process(UserId, AppointmentId, new ConfirmRejectAppointmentRequest { Decision = AppointmentDecision.REJECT, RejectReason = "x" });
        //Assert
        _notification.Verify(x => x.Process(fallbackUserId, "APPOINTMENT_REJECTED", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Process_ConfirmWithNullNoteReason_FallbackPayloadHasEmptyRejectReason()
    {
        //Arrange 1
        // No RejectReason supplied, so appointment.NoteReason is null on the
        // appointment object. This exercises the `appointment.NoteReason ?? string.Empty`
        // null-coalescing expression's "null" branch.
        var fallbackUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        _context.UserPatients.Add(new UserPatient { UserId = fallbackUserId, PatientId = PatientId, Relationship = "self" });
        await _context.SaveChangesAsync();
        var appointment = Appointment(null, patientUser: null);
        SetupDoctor(Doctor()); SetupAppointment(appointment);
        //Arrange 2
        //Act
        await _sut.Process(UserId, AppointmentId, new ConfirmRejectAppointmentRequest { Decision = AppointmentDecision.CONFIRM });
        //Assert
        _notification.Verify(x => x.Process(fallbackUserId, "APPOINTMENT_CONFIRMED", It.Is<string>(s => s.Contains("\"RejectReason\":\"\"") && !s.Contains("\"RejectReason\":\"ok\"") && !s.Contains("\"RejectReason\":null"))), Times.Once);
    }

    [Fact]
    public async Task Process_ConfirmWithNullPatientProfile_UsesEmptyPatientNameInFallbackPayload()
    {
        //Arrange 1
        // Patient navigation is null, so the linked guard fails and we go to the
        // fallback path. In SendNotificationAsync, appointment.Patient?.FullName
        // is null, so the null-coalescing expression returns "".
        var fallbackUserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        _context.UserPatients.Add(new UserPatient { UserId = fallbackUserId, PatientId = PatientId, Relationship = "self" });
        await _context.SaveChangesAsync();
        var appointment = new Appointment
        {
            Id = AppointmentId, DoctorId = DoctorId, PatientId = PatientId, Slot = null,
            Patient = null!,
            Doctor = Doctor(), AppointmentDate = DateTime.UtcNow, Status = AppointmentStatus.PENDING
        };
        SetupDoctor(Doctor()); SetupAppointment(appointment);
        //Arrange 2
        //Act
        await _sut.Process(UserId, AppointmentId, new ConfirmRejectAppointmentRequest { Decision = AppointmentDecision.CONFIRM, RejectReason = "ok" });
        //Assert
        _notification.Verify(x => x.Process(fallbackUserId, "APPOINTMENT_CONFIRMED", It.Is<string>(s => s.Contains("\"PatientName\":\"\"") && s.Contains("\"DoctorName\":\"Doctor\""))), Times.Once);
    }

    [Fact]
    public async Task Process_ConfirmWithNullPatientUser_AndFallbackUser_NotifiesFallback()
    {
        //Arrange 1
        var fallbackUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        _context.UserPatients.Add(new UserPatient { UserId = fallbackUserId, PatientId = PatientId, Relationship = "self" });
        await _context.SaveChangesAsync();
        var appointment = Appointment(null, patientUser: null);
        SetupDoctor(Doctor()); SetupAppointment(appointment);
        //Arrange 2
        //Act
        await _sut.Process(UserId, AppointmentId, new ConfirmRejectAppointmentRequest { Decision = AppointmentDecision.CONFIRM });
        //Assert
        _notification.Verify(x => x.Process(fallbackUserId, "APPOINTMENT_CONFIRMED", It.Is<string>(s => s.Contains("\"RejectReason\":\"\""))), Times.Once);
    }

    [Fact]
    public async Task Process_FallbackUserIsEmpty_ReturnsWithoutNotification()
    {
        //Arrange 1
        // Seed a UserPatient row in the in-memory context whose projected UserId
        // is Guid.Empty by attaching it without going through SaveChangesAsync.
        var placeholderUser = new User { Id = Guid.Empty, FullName = "Placeholder" };
        _context.Users.Attach(placeholderUser);
        var link = new UserPatient { UserId = Guid.Empty, PatientId = PatientId, Relationship = "self", User = placeholderUser };
        _context.UserPatients.Attach(link);
        _context.ChangeTracker.DetectChanges();
        var appointment = Appointment(null, patientUser: null);
        SetupDoctor(Doctor()); SetupAppointment(appointment);
        //Arrange 2
        //Act
        var result = await _sut.Process(UserId, AppointmentId, new ConfirmRejectAppointmentRequest { Decision = AppointmentDecision.REJECT, RejectReason = "x" });
        //Assert
        result.Data!.Status.Should().Be("CANCELLED");
        _notification.Verify(x => x.Process(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Process_UnknownDecision_PersistsAppointmentAndSendsRejectionTemplate()
    {
        //Arrange 1
        var appointment = Appointment(null, new User { Id = UserId });
        SetupDoctor(Doctor()); SetupAppointment(appointment);
        //Arrange 2
        //Act
        var result = await _sut.Process(UserId, AppointmentId, new ConfirmRejectAppointmentRequest { Decision = (AppointmentDecision)999 });
        //Assert
        result.Data!.Status.Should().Be(appointment.Status.ToString());
        _appointmentCommandRepo.Verify(x => x.UpdateAsync(appointment), Times.Once);
        _slotRepo.Verify(x => x.UpdateAsync(It.IsAny<TimeSlot>()), Times.Never);
    }
}
