using System.Linq.Expressions;
using ECS.Application.Services.ReceptionistManagementServices.ReceptionistCreateWalkinAppointmentServices;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.ReceptionistManagementServices.ReceptionistCreateWalkinAppointmentServices
{
    public class ReceptionistCreateWalkinAppointmentServiceTests
    {
        private readonly Mock<IRepositoryBaseAsync<Appointment, Guid, AppDbContext>> _appointmentRepoMock = new();
        private readonly Mock<IRepositoryBaseAsync<Queue, Guid, AppDbContext>> _queueRepoMock = new();
        private readonly Mock<IRepositoryBaseAsync<TimeSlot, Guid, AppDbContext>> _slotRepoMock = new();
        private readonly Mock<IRepositoryBaseAsync<PatientProfile, Guid, AppDbContext>> _patientRepoMock = new();

        private readonly ReceptionistCreateWalkinAppointmentService _sut;

        public ReceptionistCreateWalkinAppointmentServiceTests()
        {
            _sut = new ReceptionistCreateWalkinAppointmentService(
                _appointmentRepoMock.Object,
                _queueRepoMock.Object,
                _slotRepoMock.Object,
                _patientRepoMock.Object);
        }

        private void SetupPatientRepository(PatientProfile? patient)
        {
            _patientRepoMock
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) => id == ReceptionistCreateWalkinAppointmentMockData.ValidPatientId ? patient : null);
        }

        private void SetupTimeSlotRepository(List<TimeSlot> slots)
        {
            var mockDbSet = slots.BuildMockDbSet();

            _slotRepoMock
                .Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<TimeSlot, bool>>>(),
                    It.IsAny<bool>()
                ))
                .Returns((Expression<Func<TimeSlot, bool>> predicate, bool _) =>
                    mockDbSet.Object.Where(predicate));
        }

        private void SetupQueueRepository(List<Queue> queues)
        {
            var mockDbSet = queues.BuildMockDbSet();

            _queueRepoMock
                .Setup(x => x.FindByCondition(
                    It.IsAny<Expression<Func<Queue, bool>>>(),
                    It.IsAny<bool>()
                ))
                .Returns((Expression<Func<Queue, bool>> predicate, bool _) =>
                    mockDbSet.Object.Where(predicate));
        }

        [Fact]
        public async Task Process_PatientNotFound_ReturnsFailResponse()
        {
            SetupPatientRepository(null);
            var request = ReceptionistCreateWalkinAppointmentMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.Should().NotBeNull();
            result.CodeMessage.Should().Be("APP_MESSAGE_PATIENT_NOT_FOUND");
            _appointmentRepoMock.Verify(x => x.RollbackTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Process_NoSlotsExistAtAll_ReturnsFailResponse()
        {
            SetupPatientRepository(ReceptionistCreateWalkinAppointmentMockData.GetPatientProfile());
            SetupTimeSlotRepository(new List<TimeSlot>());
            var request = ReceptionistCreateWalkinAppointmentMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.Should().NotBeNull();
            result.CodeMessage.Should().Be("DATABASE_EMPTY_NO_SLOTS_EXIST");
        }

        [Fact]
        public async Task Process_DoctorRoomNotConfigured_ReturnsFailResponse()
        {
            SetupPatientRepository(ReceptionistCreateWalkinAppointmentMockData.GetPatientProfile());
            var slotWithoutRoom = ReceptionistCreateWalkinAppointmentMockData.GetTimeSlot(withRoom: false);
            SetupTimeSlotRepository(new List<TimeSlot> { slotWithoutRoom });

            var request = ReceptionistCreateWalkinAppointmentMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.Should().NotBeNull();
            result.CodeMessage.Should().Be("DOCTOR_ROOM_NOT_CONFIGURED");
        }

        [Theory]
        [InlineData("Mild stomachache", "Mild stomachache")]
        [InlineData(null, "Khám vãng lai tại quầy (Đăng ký trực tiếp)")]
        public async Task Process_ValidRequest_CreatesAppointmentAndQueue_ReturnsSuccess(
            string? inputSymptoms, string expectedSymptoms)
        {
            SetupPatientRepository(ReceptionistCreateWalkinAppointmentMockData.GetPatientProfile());
            var slot = ReceptionistCreateWalkinAppointmentMockData.GetTimeSlot(withRoom: true);
            SetupTimeSlotRepository(new List<TimeSlot> { slot });

            var existingQueues = new List<Queue>
            {
                new Queue { Id = Guid.NewGuid(), RoomId = ReceptionistCreateWalkinAppointmentMockData.RoomId, QueueNumber = 1, CreatedAt = DateTime.UtcNow },
                new Queue { Id = Guid.NewGuid(), RoomId = ReceptionistCreateWalkinAppointmentMockData.RoomId, QueueNumber = 2, CreatedAt = DateTime.UtcNow }
            };
            SetupQueueRepository(existingQueues);

            var request = ReceptionistCreateWalkinAppointmentMockData.GetValidRequest(inputSymptoms);

            var result = await _sut.Process(request);

            result.Should().NotBeNull();
            result.CodeMessage.Should().Be("APP_MESSAGE_2000");
            result.Data.Should().NotBeNull();
            result.Data!.Status.Should().Be("ARRIVED");
            result.Data.WalkInQueue.Should().NotBeNull();
            result.Data.WalkInQueue.QueueNumber.Should().Be(3);
            result.Data.WalkInQueue.Status.Should().Be("WAITING");

            _appointmentRepoMock.Verify(x => x.CreateAsync(It.Is<Appointment>(a =>
                a.PatientId == request.PatientProfileId &&
                a.DoctorId == ReceptionistCreateWalkinAppointmentMockData.DoctorId &&
                a.Symptoms == expectedSymptoms &&
                a.Status == AppointmentStatus.ARRIVED &&
                a.BookingSource == "WALKIN")), Times.Once);

            _queueRepoMock.Verify(x => x.CreateAsync(It.Is<Queue>(q =>
                q.RoomId == ReceptionistCreateWalkinAppointmentMockData.RoomId &&
                q.QueueNumber == 3 &&
                q.Status == QueueStatus.WAITING)), Times.Once);

            _appointmentRepoMock.Verify(x => x.EndTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task Process_FallbackToAnyAvailableSlot_WhenDoctorSpecificSlotNotFound()
        {
            SetupPatientRepository(ReceptionistCreateWalkinAppointmentMockData.GetPatientProfile());

            var otherDoctorId = Guid.NewGuid();
            var fallbackSlot = ReceptionistCreateWalkinAppointmentMockData.GetTimeSlot(withRoom: true);
            fallbackSlot.Schedule.DoctorId = otherDoctorId;

            SetupTimeSlotRepository(new List<TimeSlot> { fallbackSlot });
            SetupQueueRepository(new List<Queue>());

            var request = ReceptionistCreateWalkinAppointmentMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.Should().NotBeNull();
            result.CodeMessage.Should().Be("APP_MESSAGE_2000");
            result.Data.Should().NotBeNull();

            _appointmentRepoMock.Verify(x => x.CreateAsync(It.Is<Appointment>(a =>
                a.DoctorId == otherDoctorId)), Times.Once);
        }

        [Fact]
        public async Task Process_ExceptionOccurs_TriggersRollbackAndRethrows()
        {
            SetupPatientRepository(ReceptionistCreateWalkinAppointmentMockData.GetPatientProfile());
            var slot = ReceptionistCreateWalkinAppointmentMockData.GetTimeSlot(withRoom: true);
            SetupTimeSlotRepository(new List<TimeSlot> { slot });
            SetupQueueRepository(new List<Queue>());

            _appointmentRepoMock
                .Setup(x => x.CreateAsync(It.IsAny<Appointment>()))
                .ThrowsAsync(new InvalidOperationException("Database constraint error"));

            var request = ReceptionistCreateWalkinAppointmentMockData.GetValidRequest();

            Func<Task> act = async () => await _sut.Process(request);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Database constraint error");

            _appointmentRepoMock.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task Process_SlotMatchesDoctorIdViaScheduleDoctorId_ReturnsSuccess()
        {
            SetupPatientRepository(ReceptionistCreateWalkinAppointmentMockData.GetPatientProfile());

            var slot = ReceptionistCreateWalkinAppointmentMockData.GetTimeSlot(withRoom: true);
            slot.ScheduleId = Guid.NewGuid();

            SetupTimeSlotRepository(new List<TimeSlot> { slot });
            SetupQueueRepository(new List<Queue>());

            var request = ReceptionistCreateWalkinAppointmentMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.Should().NotBeNull();
            result.CodeMessage.Should().Be("APP_MESSAGE_2000");
        }

        [Fact]
        public async Task Process_NoExistingQueuesForToday_DefaultsQueueNumberToOne()
        {
            SetupPatientRepository(ReceptionistCreateWalkinAppointmentMockData.GetPatientProfile());
            var slot = ReceptionistCreateWalkinAppointmentMockData.GetTimeSlot(withRoom: true);
            SetupTimeSlotRepository(new List<TimeSlot> { slot });

            SetupQueueRepository(new List<Queue>());

            var request = ReceptionistCreateWalkinAppointmentMockData.GetValidRequest();

            var result = await _sut.Process(request);

            result.Should().NotBeNull();
            result.CodeMessage.Should().Be("APP_MESSAGE_2000");
            result.Data!.WalkInQueue.QueueNumber.Should().Be(1);

            _queueRepoMock.Verify(x => x.CreateAsync(It.Is<Queue>(q => q.QueueNumber == 1)), Times.Once);
        }
    }
}