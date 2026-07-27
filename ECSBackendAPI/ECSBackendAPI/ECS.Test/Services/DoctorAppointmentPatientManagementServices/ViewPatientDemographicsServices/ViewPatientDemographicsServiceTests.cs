using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewPatientDemographicsServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ECS.Test.Services.DoctorAppointmentPatientManagementServices.ViewPatientDemographicsServices
{
    public class ViewPatientDemographicsServiceTests : IDisposable
    {
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly AppDbContext _context;
        private readonly IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> _patientProfileRepository;
        private readonly IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> _medicalRecordRepository;
        private readonly ViewPatientDemographicsRequestValidator _validator;
        private readonly ViewPatientDemographicsService _service;

        public ViewPatientDemographicsServiceTests()
        {
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _patientProfileRepository = new RepositoryQueryBase<PatientProfile, Guid, AppDbContext>(_context);
            _medicalRecordRepository = new RepositoryQueryBase<MedicalRecord, Guid, AppDbContext>(_context);
            _validator = new ViewPatientDemographicsRequestValidator();

            _service = new ViewPatientDemographicsService(
                _patientProfileRepository,
                _medicalRecordRepository,
                _validator,
                _context,
                _httpContextAccessorMock.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void SetupHttpContext(string? userIdClaim = null, string? roleClaim = null, bool setNullContext = false, bool setNullClaim = false)
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
            if (roleClaim != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, roleClaim));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };

            _httpContextAccessorMock.Setup(h => h.HttpContext).Returns(httpContext);
        }

        // ── Test Cases ────────────────────────────────────────────────────────────

        [Fact]
        public async Task Process_RequestInvalid_ReturnsValidationError4019()
        {
            //Arrange 1
            var request = new ViewPatientDemographicsRequest
            {
                PageNumber = 0, // Invalid < 1
                PageSize = 0    // Invalid < 1 (tests both pageNumber and pageSize normalization true branches)
            };

            //Arrange 2
            SetupHttpContext(userIdClaim: Guid.NewGuid().ToString(), roleClaim: "DOCTOR");

            //Act
            var result = await _service.Process(request);

            //Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4019.ToString());
        }

        [Fact]
        public async Task Process_HttpContextNull_ReturnsDataScopeNotFound4010()
        {
            //Arrange 1
            var request = new ViewPatientDemographicsRequest
            {
                PageNumber = 1,
                PageSize = 10
            };

            //Arrange 2
            SetupHttpContext(setNullContext: true);

            //Act
            var result = await _service.Process(request);

            //Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4010.ToString());
        }

        [Fact]
        public async Task Process_UserIdClaimMissingOrInvalidGuid_ReturnsDataScopeNotFound4010()
        {
            //Arrange 1
            var request = new ViewPatientDemographicsRequest
            {
                PageNumber = 1,
                PageSize = 10
            };

            //Arrange 2
            SetupHttpContext(userIdClaim: "invalid-guid-string", roleClaim: "DOCTOR");

            //Act
            var result = await _service.Process(request);

            //Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4010.ToString());
        }

        [Fact]
        public async Task Process_UserIdClaimNull_ReturnsDataScopeNotFound4010()
        {
            //Arrange 1
            var request = new ViewPatientDemographicsRequest
            {
                PageNumber = 1,
                PageSize = 10
            };

            //Arrange 2
            SetupHttpContext(setNullClaim: true, roleClaim: "DOCTOR");

            //Act
            var result = await _service.Process(request);

            //Assert
            result.Data.Should().BeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4010.ToString());
        }

        [Fact]
        public async Task Process_RoleClaimNull_ReturnsSuccess2000()
        {
            //Arrange 1
            var request = new ViewPatientDemographicsRequest
            {
                PageNumber = 1,
                PageSize = 10
            };
            var userId = Guid.NewGuid();

            //Arrange 2
            SetupHttpContext(userIdClaim: userId.ToString(), roleClaim: null);

            //Act
            var result = await _service.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task Process_DoctorRoleDoctorNotFound_ReturnsSuccess2000WithEmptyList()
        {
            //Arrange 1
            var request = new ViewPatientDemographicsRequest
            {
                PageNumber = 1,
                PageSize = 10
            };
            var doctorUserId = Guid.NewGuid();

            //Arrange 2
            SetupHttpContext(userIdClaim: doctorUserId.ToString(), roleClaim: "DOCTOR");

            //Act
            var result = await _service.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.TotalRecords.Should().Be(0);
            result.Data.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Process_DoctorRoleValidDoctorWithAppointments_ReturnsSuccess2000WithMappedRecords()
        {
            //Arrange 1
            var doctorUserId = ViewPatientDemographicsMockData.DefaultDoctorUserId;
            var doctorProfileId = ViewPatientDemographicsMockData.DefaultDoctorProfileId;

            var doctor = ViewPatientDemographicsMockData.GetDoctorProfile(doctorUserId, doctorProfileId);
            var patient = ViewPatientDemographicsMockData.GetPatientProfile(Guid.NewGuid());
            var record = ViewPatientDemographicsMockData.GetMedicalRecord(Guid.NewGuid(), patient, doctor, RecordType.MS21_TRAUMA);

            _context.Set<DoctorProfile>().Add(doctor);
            _context.Set<PatientProfile>().Add(patient);
            _context.Set<Appointment>().Add(record.Appointment);
            _context.Set<MedicalRecord>().Add(record);
            await _context.SaveChangesAsync();

            var request = new ViewPatientDemographicsRequest
            {
                PageNumber = 1,
                PageSize = 10
            };

            //Arrange 2
            SetupHttpContext(userIdClaim: doctorUserId.ToString(), roleClaim: "DOCTOR");

            //Act
            var result = await _service.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.TotalRecords.Should().Be(1);
            result.Data.Items.Should().HaveCount(1);

            var item = result.Data.Items[0];
            item.FullName.Should().Be("Nguyen Van A");
            item.RecordTypeLabel.Should().Be("Bệnh án mắt (Chấn thương)");
            item.DoctorName.Should().Be("Bac Si A");
        }

        [Fact]
        public async Task Process_PatientRoleDirectAndLinkedPatients_ReturnsSuccess2000WithMergedRecords()
        {
            //Arrange 1
            var patientUserId = ViewPatientDemographicsMockData.DefaultPatientUserId;
            var doctor = ViewPatientDemographicsMockData.GetDoctorProfile(Guid.NewGuid(), Guid.NewGuid());

            var directPatient = ViewPatientDemographicsMockData.GetPatientProfile(Guid.NewGuid(), userId: patientUserId);
            var linkedPatient = ViewPatientDemographicsMockData.GetPatientProfile(Guid.NewGuid(), userId: Guid.NewGuid());

            var userPatient = new UserPatient
            {
                UserId = patientUserId,
                PatientId = linkedPatient.Id
            };

            var record1 = ViewPatientDemographicsMockData.GetMedicalRecord(Guid.NewGuid(), directPatient, doctor, RecordType.MS22_ANTERIOR);
            var record2 = ViewPatientDemographicsMockData.GetMedicalRecord(Guid.NewGuid(), linkedPatient, doctor, RecordType.MS23_FUNDUS);

            _context.Set<DoctorProfile>().Add(doctor);
            _context.Set<PatientProfile>().AddRange(directPatient, linkedPatient);
            _context.Set<UserPatient>().Add(userPatient);
            _context.Set<Appointment>().AddRange(record1.Appointment, record2.Appointment);
            _context.Set<MedicalRecord>().AddRange(record1, record2);
            await _context.SaveChangesAsync();

            var request = new ViewPatientDemographicsRequest
            {
                PageNumber = 1,
                PageSize = 10
            };

            //Arrange 2
            SetupHttpContext(userIdClaim: patientUserId.ToString(), roleClaim: "PATIENT");

            //Act
            var result = await _service.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.TotalRecords.Should().Be(2);
            result.Data.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task Process_FilterByPatientProfileIdRecordTypeAndSearchTerm_ReturnsSuccess2000Filtered()
        {
            //Arrange 1
            var doctorUserId = ViewPatientDemographicsMockData.DefaultDoctorUserId;
            var doctorProfileId = ViewPatientDemographicsMockData.DefaultDoctorProfileId;

            var doctor = ViewPatientDemographicsMockData.GetDoctorProfile(doctorUserId, doctorProfileId);
            var patient1 = ViewPatientDemographicsMockData.GetPatientProfile(Guid.NewGuid());
            var patient2 = ViewPatientDemographicsMockData.GetPatientProfile(Guid.NewGuid());

            var record1 = ViewPatientDemographicsMockData.GetMedicalRecord(Guid.NewGuid(), patient1, doctor, RecordType.MS24_GLAUCOMA, summary: "Dac biet glaucoma", complaint: "Mo mat");
            var record2 = ViewPatientDemographicsMockData.GetMedicalRecord(Guid.NewGuid(), patient1, doctor, RecordType.MS25_STRABISMUS_PTOSIS, summary: "Benh lác", complaint: "Lech mat");
            var record3 = ViewPatientDemographicsMockData.GetMedicalRecord(Guid.NewGuid(), patient1, doctor, RecordType.MS26_PEDIATRIC, summary: "Tre em", complaint: "Tre bi viem");
            var record4 = ViewPatientDemographicsMockData.GetMedicalRecord(Guid.NewGuid(), patient1, doctor, (RecordType)99, summary: "Khac", complaint: "Khac");

            _context.Set<DoctorProfile>().Add(doctor);
            _context.Set<PatientProfile>().AddRange(patient1, patient2);
            _context.Set<Appointment>().AddRange(record1.Appointment, record2.Appointment, record3.Appointment, record4.Appointment);
            _context.Set<MedicalRecord>().AddRange(record1, record2, record3, record4);
            await _context.SaveChangesAsync();

            var request = new ViewPatientDemographicsRequest
            {
                PatientProfileId = patient1.Id.ToString(),
                RecordType = "MS24_GLAUCOMA",
                SearchTerm = "glaucoma",
                PageNumber = 1,
                PageSize = 10
            };

            //Arrange 2
            SetupHttpContext(userIdClaim: doctorUserId.ToString(), roleClaim: "DOCTOR");

            //Act
            var result = await _service.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.TotalRecords.Should().Be(1);
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items[0].RecordTypeLabel.Should().Be("Bệnh án mắt (Glôcôm)");
        }

        [Fact]
        public async Task Process_AllRecordTypeLabels_ReturnsCorrectLabels()
        {
            //Arrange 1
            var doctorUserId = ViewPatientDemographicsMockData.DefaultDoctorUserId;
            var doctorProfileId = ViewPatientDemographicsMockData.DefaultDoctorProfileId;

            var doctor = ViewPatientDemographicsMockData.GetDoctorProfile(doctorUserId, doctorProfileId);
            var patient = ViewPatientDemographicsMockData.GetPatientProfile(Guid.NewGuid());

            var record1 = ViewPatientDemographicsMockData.GetMedicalRecord(Guid.NewGuid(), patient, doctor, RecordType.MS23_FUNDUS);
            var record2 = ViewPatientDemographicsMockData.GetMedicalRecord(Guid.NewGuid(), patient, doctor, RecordType.MS25_STRABISMUS_PTOSIS);
            var record3 = ViewPatientDemographicsMockData.GetMedicalRecord(Guid.NewGuid(), patient, doctor, RecordType.MS26_PEDIATRIC);
            var record4 = ViewPatientDemographicsMockData.GetMedicalRecord(Guid.NewGuid(), patient, doctor, (RecordType)999);

            _context.Set<DoctorProfile>().Add(doctor);
            _context.Set<PatientProfile>().Add(patient);
            _context.Set<Appointment>().AddRange(record1.Appointment, record2.Appointment, record3.Appointment, record4.Appointment);
            _context.Set<MedicalRecord>().AddRange(record1, record2, record3, record4);
            await _context.SaveChangesAsync();

            var request = new ViewPatientDemographicsRequest
            {
                PageNumber = 1,
                PageSize = 10
            };

            //Arrange 2
            SetupHttpContext(userIdClaim: doctorUserId.ToString(), roleClaim: "DOCTOR");

            //Act
            var result = await _service.Process(request);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Items.Should().HaveCount(4);

            result.Data.Items.Select(i => i.RecordTypeLabel).Should().Contain(new[]
            {
                "Bệnh án mắt (Đáy mắt)",
                "Bệnh án mắt (Lác, sụp mi)",
                "Bệnh án mắt (Mắt trẻ em)",
                "999"
            });
        }
    }
}
