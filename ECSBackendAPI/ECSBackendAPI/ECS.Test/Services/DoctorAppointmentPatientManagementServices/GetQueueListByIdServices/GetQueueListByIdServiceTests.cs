using ECS.Application.Common.Response;
using ECS.Application.Services.DoctorAppointmentPatientManagementServices.GetQueueListByIdServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ECS.Test.Services.DoctorAppointmentPatientManagementServices.GetQueueListByIdServices
{
    public class GetQueueListByIdServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly GetQueueListByIdService _service;

        public GetQueueListByIdServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _service = new GetQueueListByIdService(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task Process_DoctorNotFoundOrInactive_ReturnsFail4011()
        {
            //Arrange 1
            var doctorId = Guid.NewGuid();
            var date = new DateOnly(2025, 6, 15);

            //Arrange 2 (Database empty)

            //Act
            var result = await _service.Process(doctorId, date);

            //Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4011.ToString());
        }

        [Fact]
        public async Task Process_DoctorValidNoQueues_ReturnsSuccess2001WithZeroCounts()
        {
            //Arrange 1
            var doctor = GetQueueListByIdMockData.GetDoctorProfile();
            var date = new DateOnly(2025, 6, 15);

            //Arrange 2
            _context.Set<DoctorProfile>().Add(doctor);
            await _context.SaveChangesAsync();

            //Act
            var result = await _service.Process(doctor.Id, date);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2001.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.DoctorId.Should().Be(doctor.Id);
            result.Data.Date.Should().Be(date);
            result.Data.TotalPatients.Should().Be(0);
            result.Data.WaitingCount.Should().Be(0);
            result.Data.InProgressCount.Should().Be(0);
            result.Data.CompletedCount.Should().Be(0);
            result.Data.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Process_DoctorValidWithQueues_ReturnsSuccess2001WithCalculatedCountsAndMappedItems()
        {
            //Arrange 1
            var doctor = GetQueueListByIdMockData.GetDoctorProfile();
            var date = new DateOnly(2025, 6, 15);

            var patient1 = GetQueueListByIdMockData.GetPatientProfile(fullName: "Nguyen Van A", phone: "0900000001");
            var patient2 = GetQueueListByIdMockData.GetPatientProfile(fullName: "Tran Thi B", phone: "0900000002");
            var patient3 = GetQueueListByIdMockData.GetPatientProfile(fullName: "Le Van C", phone: "0900000003");
            var patient4 = GetQueueListByIdMockData.GetPatientProfile(fullName: "Pham Van D", phone: "0900000004");

            var room = GetQueueListByIdMockData.GetRoom(roomName: "Phong 202");
            var service = GetQueueListByIdMockData.GetService(serviceName: "Kham Do Thi Luc");
            var slot = GetQueueListByIdMockData.GetSlot();

            var q1 = GetQueueListByIdMockData.GetQueueItem(doctor.Id, date, 1, QueueStatus.WAITING, patient1, room, service, slot: slot, includeMedicalRecord: false);
            var q2 = GetQueueListByIdMockData.GetQueueItem(doctor.Id, date, 2, QueueStatus.CALLING, patient2, room, service, slot: null, includeMedicalRecord: true);
            var q3 = GetQueueListByIdMockData.GetQueueItem(doctor.Id, date, 3, QueueStatus.COMPLETED, patient3, room, service, slot: null, includeMedicalRecord: false);
            var q4 = GetQueueListByIdMockData.GetQueueItem(doctor.Id, date, 4, (QueueStatus)99, patient4, room, service, slot: null, includeMedicalRecord: false);

            //Arrange 2
            _context.Set<DoctorProfile>().Add(doctor);
            _context.Set<PatientProfile>().AddRange(patient1, patient2, patient3, patient4);
            _context.Set<FacilityRoom>().Add(room);
            _context.Set<Service>().Add(service);
            _context.Set<TimeSlot>().Add(slot);
            _context.Set<Queue>().AddRange(q1, q2, q3, q4);

            await _context.SaveChangesAsync();

            //Act
            var result = await _service.Process(doctor.Id, date);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2001.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.TotalPatients.Should().Be(4);
            result.Data.WaitingCount.Should().Be(1);
            result.Data.InProgressCount.Should().Be(1);
            result.Data.CompletedCount.Should().Be(1);
            result.Data.Items.Should().HaveCount(4);

            var item1 = result.Data.Items.First(i => i.QueueNumber == 1);
            item1.StatusText.Should().Be("Đang chờ");
            item1.HasMedicalRecord.Should().BeFalse();

            var item2 = result.Data.Items.First(i => i.QueueNumber == 2);
            item2.StatusText.Should().Be("Đang khám");
            item2.HasMedicalRecord.Should().BeTrue();

            var item3 = result.Data.Items.First(i => i.QueueNumber == 3);
            item3.StatusText.Should().Be("Đã khám xong");

            var item4 = result.Data.Items.First(i => i.QueueNumber == 4);
            item4.StatusText.Should().Be("99");
        }

        [Fact]
        public async Task Process_NullNavigationProperties_ReturnsDefaults()
        {
            //Arrange 1
            var doctor = GetQueueListByIdMockData.GetDoctorProfile();
            var date = new DateOnly(2025, 6, 15);

            var patient = new PatientProfile
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                FullName = "", // empty to test DefaultString fallback
                PhoneNumber = null,
                Dob = DateTime.MinValue
            };

            var slot = GetQueueListByIdMockData.GetSlot();

            var appt = new Appointment
            {
                Id = Guid.NewGuid(),
                DoctorId = doctor.Id,
                PatientId = patient.Id,
                Patient = patient,
                SlotId = slot.Id,
                Slot = slot,
                AppointmentDate = date.ToDateTime(TimeOnly.MinValue),
                BookingSource = ""
            };

            var queue = new Queue
            {
                Id = Guid.NewGuid(),
                QueueNumber = 1,
                AppointmentId = appt.Id,
                Appointment = appt,
                ClinicId = Guid.NewGuid(),
                Status = QueueStatus.WAITING
            };

            //Arrange 2
            _context.Set<DoctorProfile>().Add(doctor);
            _context.Set<PatientProfile>().Add(patient);
            _context.Set<TimeSlot>().Add(slot);
            _context.Set<Appointment>().Add(appt);
            _context.Set<Queue>().Add(queue);
            await _context.SaveChangesAsync();

            //Act
            var result = await _service.Process(doctor.Id, date);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2001.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(1);

            var item = result.Data.Items[0];
            item.PatientName.Should().Be("Unknown");
            item.PatientGender.Should().Be("MALE");
            item.BookingSource.Should().Be("UNKNOWN");
            item.PatientPhone.Should().BeNull();
            item.PatientDateOfBirth.Should().Be(DateTime.MinValue);
            item.RoomId.Should().BeNull();
            item.RoomName.Should().BeNull();
            item.ServiceName.Should().BeNull();
        }
    }
}
