using System.Linq.Expressions;
using ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewPatientDetailServices;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using ECS.Test.MockData;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

namespace ECS.Test.Services.DoctorAppointmentPatientManagementServices.ViewPatientDetailServices
{
    /// <summary>
    /// Unit tests for <see cref="ViewPatientDetailService"/>.
    /// Pattern: [Method]_[State]_[ExpectedResult].
    /// Goal: 100% line coverage on <c>ViewPatientDetailService.cs</c>.
    /// </summary>
    /// <remarks>
    /// MockQueryable's in-memory provider does NOT honour <c>.Include()</c>; the
    /// <c>.Include()</c> calls in <see cref="ViewPatientDetailService"/> are treated
    /// as no-ops. Projections still work because we pre-populate the navigation
    /// properties (User, Service, MedicalRecord) on each seed entity.
    /// </remarks>
    public class ViewPatientDetailServiceTests
    {
        private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<PatientProfile, Guid, AppDbContext>> _patientRepoMock = new();
        private readonly Mock<IRepositoryQueryBase<Appointment, Guid, AppDbContext>> _appointmentRepoMock = new();
        private readonly ViewPatientDetailService _sut;

        public ViewPatientDetailServiceTests()
        {
            _sut = new ViewPatientDetailService(
                _doctorRepoMock.Object,
                _patientRepoMock.Object,
                _appointmentRepoMock.Object);
        }

        // ─────────────────────────────────────────────────────────────────
        // Repository helpers
        // ─────────────────────────────────────────────────────────────────

        private void SetupDoctorRepo(IEnumerable<DoctorProfile> doctors)
        {
            var list = doctors.ToList();
            var queryable = list.BuildMockDbSet<DoctorProfile>();
            _doctorRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<DoctorProfile, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(queryable.Object);
        }

        private void SetupPatientRepo(IEnumerable<PatientProfile> patients)
        {
            var list = patients.ToList();
            var queryable = list.BuildMockDbSet<PatientProfile>();
            _patientRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<PatientProfile, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(queryable.Object);
        }

        private void SetupAppointmentRepo(IEnumerable<Appointment> appointments)
        {
            var list = appointments.ToList();
            var queryable = list.BuildMockDbSet<Appointment>();
            _appointmentRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Appointment, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(queryable.Object);
        }

        // ==================================================================
        // ====================== Process(...) tests ========================
        // ==================================================================

        /// <summary>
        /// TC-VPD-01: Doctor profile missing or inactive → ResolveActiveDoctorProfileAsync
        /// throws <see cref="KeyNotFoundException"/> with <c>APP_MESSAGE_4008</c>.
        /// Patient + appointment repos must NOT be consulted.
        /// Covers: <c>ResolveActiveDoctorProfileAsync</c> null branch.
        /// </summary>
        [Fact]
        public async Task Process_DoctorProfileMissingOrInactive_ThrowsDoctorNotFound()
        {
            //Arrange 1
            var userId = ViewPatientDetailMockData.DefaultDoctorUserId;
            var patientId = ViewPatientDetailMockData.DefaultPatientId;

            //Arrange 2
            SetupDoctorRepo(Array.Empty<DoctorProfile>());

            //Act
            var act = () => _sut.Process(userId, patientId);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4008.ToString());
            _doctorRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<DoctorProfile, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _patientRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()),
                Times.Never);
            _appointmentRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>
        /// TC-VPD-02: Doctor exists but no appointment links them to the patient
        /// → EnsureDoctorPatientRelationshipAsync throws
        /// <see cref="KeyNotFoundException"/> with <c>APP_MESSAGE_4004</c>.
        /// Patient repo must NOT be consulted.
        /// Covers: <c>EnsureDoctorPatientRelationshipAsync</c> <c>!hasRelation</c> branch.
        /// </summary>
        [Fact]
        public async Task Process_NoDoctorPatientRelationship_ThrowsRelationNotFound()
        {
            //Arrange 1
            var doctor = ViewPatientDetailMockData.GetDoctorProfile();
            var userId = doctor.UserId;
            var patientId = ViewPatientDetailMockData.DefaultPatientId;

            //Arrange 2
            SetupDoctorRepo(new[] { doctor });
            SetupAppointmentRepo(Array.Empty<Appointment>());

            //Act
            var act = () => _sut.Process(userId, patientId);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4004.ToString());
            _doctorRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<DoctorProfile, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _appointmentRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _patientRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()),
                Times.Never);
        }

        /// <summary>
        /// TC-VPD-03: Doctor + relation exist but patient profile is missing
        /// → FetchPatientProfileAsync throws <see cref="KeyNotFoundException"/>
        /// with <c>APP_MESSAGE_4004</c>.
        /// Covers: <c>FetchPatientProfileAsync</c> null branch.
        /// </summary>
        [Fact]
        public async Task Process_PatientProfileNotFound_ThrowsPatientNotFound()
        {
            //Arrange 1
            var doctor = ViewPatientDetailMockData.GetDoctorProfile();
            var patientId = ViewPatientDetailMockData.DefaultPatientId;
            var appointment = ViewPatientDetailMockData.GetAppointment(doctorId: doctor.Id, patientId: patientId);

            //Arrange 2
            SetupDoctorRepo(new[] { doctor });
            SetupAppointmentRepo(new[] { appointment });
            SetupPatientRepo(Array.Empty<PatientProfile>());

            //Act
            var act = () => _sut.Process(doctor.UserId, patientId);

            //Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage(GeneralCode.APP_MESSAGE_4004.ToString());
            _patientRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()),
                Times.Once);
        }

        /// <summary>
        /// TC-VPD-04: Happy path — doctor, patient, relation, and appointment with
        /// service + medical record all present → success response with mapped fields.
        /// Covers: <c>Process</c> happy path, <c>BuildResponse</c> field mapping,
        /// <c>CreateSuccessResponse</c>.
        /// </summary>
        [Fact]
        public async Task Process_ValidDoctorPatientAndRelation_ReturnsSuccessWithResponse()
        {
            //Arrange 1
            var doctor = ViewPatientDetailMockData.GetDoctorProfile();
            var patient = ViewPatientDetailMockData.GetPatientProfile();
            var appointment = ViewPatientDetailMockData.GetAppointment(
                doctorId: doctor.Id, patientId: patient.Id);

            //Arrange 2
            SetupDoctorRepo(new[] { doctor });
            SetupAppointmentRepo(new[] { appointment });
            SetupPatientRepo(new[] { patient });

            //Act
            var result = await _sut.Process(doctor.UserId, patient.Id);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.PatientId.Should().Be(patient.Id);
            result.Data.FullName.Should().Be(patient.FullName);
            result.Data.Gender.Should().Be(patient.Gender);
            result.Data.Dob.Should().Be(patient.Dob);
            result.Data.IdentityNumber.Should().Be(patient.IdentityNumber);
            result.Data.Address.Should().Be(patient.Address);
            result.Data.PhoneNumber.Should().Be(patient.PhoneNumber);
            result.Data.BhytNumber.Should().Be(patient.BhytNumber);
            result.Data.BloodType.Should().Be(patient.BloodType);
            result.Data.Allergies.Should().Be(patient.Allergies);
            result.Data.MedicalHistory.Should().Be(patient.MedicalHistory);
            result.Data.AvatarUrl.Should().Be(patient.User!.AvatarUrl);
            result.Data.Appointments.Should().HaveCount(1);
        }

        /// <summary>
        /// TC-VPD-05: Doctor + patient + relation exist but the appointment history list
        /// is empty → success response with empty <c>Appointments</c>.
        /// Covers: <c>FetchAppointmentHistoryAsync</c> empty-list branch.
        /// </summary>
        [Fact]
        public async Task Process_NoAppointments_ReturnsSuccessWithEmptyAppointmentList()
        {
            //Arrange 1
            var doctor = ViewPatientDetailMockData.GetDoctorProfile();
            var patient = ViewPatientDetailMockData.GetPatientProfile();

            //Arrange 2
            SetupDoctorRepo(new[] { doctor });
            // First call (relation check) returns non-empty so it passes;
            // MockQueryable cannot differentiate two queries on the same predicate,
            // so we return a single appointment to also satisfy the relation check.
            SetupAppointmentRepo(new[] { ViewPatientDetailMockData.GetAppointment(doctorId: doctor.Id, patientId: patient.Id) });
            SetupPatientRepo(new[] { patient });

            //Arrange 2 override: appointment history returns empty list
            // To exercise the empty-list branch we need a setup where the repo returns
            // a non-empty list for AnyAsync but an empty list for ToListAsync. Since
            // MockQueryable cannot branch on operator, we instead use a dedicated
            // setup pattern that returns an empty IQueryable for the second call.

            //Arrange 2 (rewire)
            _appointmentRepoMock.Reset();
            var anyQueryable = new[] { ViewPatientDetailMockData.GetAppointment(doctorId: doctor.Id, patientId: patient.Id) }
                .BuildMockDbSet<Appointment>().Object;
            var emptyQueryable = Array.Empty<Appointment>().BuildMockDbSet<Appointment>().Object;
            int callCount = 0;
            _appointmentRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<Expression<Func<Appointment, bool>>>(),
                    It.IsAny<bool>()))
                .Returns(() => (callCount++ == 0) ? anyQueryable : emptyQueryable);

            //Act
            var result = await _sut.Process(doctor.UserId, patient.Id);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.Appointments.Should().BeEmpty();
        }

        /// <summary>
        /// TC-VPD-06: Appointment without <c>MedicalRecord</c> → <c>MapMedicalRecord</c>
        /// returns null → <c>AppointmentHistoryItem.MedicalRecord == null</c>.
        /// Covers: <c>MapMedicalRecord</c> null branch.
        /// </summary>
        [Fact]
        public async Task Process_AppointmentWithoutMedicalRecord_MapMedicalRecordAsNull()
        {
            //Arrange 1
            var doctor = ViewPatientDetailMockData.GetDoctorProfile();
            var patient = ViewPatientDetailMockData.GetPatientProfile();
            var appointment = ViewPatientDetailMockData.GetAppointment(
                doctorId: doctor.Id, patientId: patient.Id, medicalRecord: null);

            //Arrange 2
            SetupDoctorRepo(new[] { doctor });
            SetupAppointmentRepo(new[] { appointment });
            SetupPatientRepo(new[] { patient });

            //Act
            var result = await _sut.Process(doctor.UserId, patient.Id);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Appointments.Should().HaveCount(1);
            result.Data.Appointments[0].MedicalRecord.Should().BeNull();
        }

        /// <summary>
        /// TC-VPD-07: Appointment with <c>MedicalRecord</c> populated → every mapped
        /// field is asserted, including <c>Prescriptions = []</c>.
        /// Covers: <c>MapMedicalRecord</c> non-null branch (every assignment).
        /// </summary>
        [Fact]
        public async Task Process_AppointmentWithMedicalRecord_MapMedicalRecordFieldsCorrectly()
        {
            //Arrange 1
            var doctor = ViewPatientDetailMockData.GetDoctorProfile();
            var patient = ViewPatientDetailMockData.GetPatientProfile();
            var medicalRecord = ViewPatientDetailMockData.GetMedicalRecord();
            var appointment = ViewPatientDetailMockData.GetAppointment(
                doctorId: doctor.Id, patientId: patient.Id, medicalRecord: medicalRecord);

            //Arrange 2
            SetupDoctorRepo(new[] { doctor });
            SetupAppointmentRepo(new[] { appointment });
            SetupPatientRepo(new[] { patient });

            //Act
            var result = await _sut.Process(doctor.UserId, patient.Id);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Appointments.Should().HaveCount(1);
            var item = result.Data.Appointments[0];
            item.MedicalRecord.Should().NotBeNull();
            item.MedicalRecord!.Id.Should().Be(medicalRecord.Id);
            item.MedicalRecord.RecordType.Should().Be(medicalRecord.RecordType);
            item.MedicalRecord.ChiefComplaint.Should().Be(medicalRecord.ChiefComplaint);
            item.MedicalRecord.DiagnosisMain.Should().Be(medicalRecord.Summary);
            item.MedicalRecord.DiagnosisComorbid.Should().BeNull();
            item.MedicalRecord.TreatmentPlan.Should().Be(medicalRecord.Notes);
            item.MedicalRecord.Notes.Should().Be(medicalRecord.Notes);
            item.MedicalRecord.IsLocked.Should().Be(medicalRecord.IsLocked);
            item.MedicalRecord.CreatedAt.Should().Be(medicalRecord.CreatedAt);
            item.MedicalRecord.Prescriptions.Should().BeEmpty();
        }

        /// <summary>
        /// TC-VPD-08: Patient with <c>User == null</c> → <c>BuildResponse</c> maps
        /// <c>AvatarUrl = null</c> via the null-conditional operator.
        /// Covers: <c>patient.User?.AvatarUrl</c> null branch.
        /// </summary>
        [Fact]
        public async Task Process_PatientWithoutUser_MapAvatarUrlAsNull()
        {
            //Arrange 1
            var doctor = ViewPatientDetailMockData.GetDoctorProfile();
            var patientWithoutUser = new PatientProfile
            {
                Id = ViewPatientDetailMockData.DefaultPatientId,
                UserId = null,
                User = null,
                FullName = "Anonymous Patient",
                Gender = Gender.FEMALE,
                Dob = new DateTime(1985, 5, 20)
            };
            var appointment = ViewPatientDetailMockData.GetAppointment(
                doctorId: doctor.Id, patientId: patientWithoutUser.Id);

            //Arrange 2
            SetupDoctorRepo(new[] { doctor });
            SetupAppointmentRepo(new[] { appointment });
            SetupPatientRepo(new[] { patientWithoutUser });

            //Act
            var result = await _sut.Process(doctor.UserId, patientWithoutUser.Id);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.AvatarUrl.Should().BeNull();
        }

        /// <summary>
        /// TC-VPD-09: Patient with User and AvatarUrl → <c>BuildResponse</c> maps the
        /// URL correctly.
        /// Covers: <c>patient.User?.AvatarUrl</c> non-null branch.
        /// </summary>
        [Fact]
        public async Task Process_PatientWithUserAndAvatarUrl_MapAvatarUrlCorrectly()
        {
            //Arrange 1
            var doctor = ViewPatientDetailMockData.GetDoctorProfile();
            var user = ViewPatientDetailMockData.GetUser(avatarUrl: "https://example.com/p.jpg");
            var patient = ViewPatientDetailMockData.GetPatientProfile(user: user, avatarUrl: "https://example.com/p.jpg");
            var appointment = ViewPatientDetailMockData.GetAppointment(
                doctorId: doctor.Id, patientId: patient.Id);

            //Arrange 2
            SetupDoctorRepo(new[] { doctor });
            SetupAppointmentRepo(new[] { appointment });
            SetupPatientRepo(new[] { patient });

            //Act
            var result = await _sut.Process(doctor.UserId, patient.Id);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.AvatarUrl.Should().Be("https://example.com/p.jpg");
        }

        /// <summary>
        /// TC-VPD-10: Appointment without <c>Service</c> → <c>ServiceName == null</c>
        /// via the null-conditional operator.
        /// Covers: <c>appointment.Service?.ServiceName</c> null branch.
        /// </summary>
        [Fact]
        public async Task Process_AppointmentWithoutService_MapServiceNameAsNull()
        {
            //Arrange 1
            var doctor = ViewPatientDetailMockData.GetDoctorProfile();
            var patient = ViewPatientDetailMockData.GetPatientProfile();
            var appointmentWithoutService = new Appointment
            {
                Id = ViewPatientDetailMockData.DefaultAppointmentId,
                DoctorId = doctor.Id,
                PatientId = patient.Id,
                SlotId = Guid.NewGuid(),
                AppointmentDate = new DateTime(2026, 1, 15, 9, 0, 0),
                Status = AppointmentStatus.PENDING,
                Symptoms = "Headache",
                ServiceId = null,
                Service = null,
                Patient = patient,
                Doctor = doctor
            };

            //Arrange 2
            SetupDoctorRepo(new[] { doctor });
            SetupAppointmentRepo(new[] { appointmentWithoutService });
            SetupPatientRepo(new[] { patient });

            //Act
            var result = await _sut.Process(doctor.UserId, patient.Id);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Appointments.Should().HaveCount(1);
            result.Data.Appointments[0].ServiceName.Should().BeNull();
        }

        /// <summary>
        /// TC-VPD-11: Appointment <c>Status</c> is serialized as a string via
        /// <c>Status.ToString()</c>.
        /// Covers: <c>appointment.Status.ToString()</c> mapping.
        /// </summary>
        [Fact]
        public async Task Process_AppointmentStatus_IsSerializedAsString()
        {
            //Arrange 1
            var doctor = ViewPatientDetailMockData.GetDoctorProfile();
            var patient = ViewPatientDetailMockData.GetPatientProfile();
            var appointment = ViewPatientDetailMockData.GetAppointment(
                doctorId: doctor.Id, patientId: patient.Id,
                status: AppointmentStatus.COMPLETED);

            //Arrange 2
            SetupDoctorRepo(new[] { doctor });
            SetupAppointmentRepo(new[] { appointment });
            SetupPatientRepo(new[] { patient });

            //Act
            var result = await _sut.Process(doctor.UserId, patient.Id);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Appointments.Should().HaveCount(1);
            result.Data.Appointments[0].Status.Should().Be(AppointmentStatus.COMPLETED.ToString());
        }

        /// <summary>
        /// TC-VPD-12: Two appointments seeded OUT-OF-ORDER (later date first, earlier date second)
        /// → response items returned in descending date order.
        /// Covers: <c>OrderByDescending(a => a.AppointmentDate)</c>.
        /// </summary>
        [Fact]
        public async Task Process_AppointmentsOrderedByDescendingDate_ReturnsInSortedOrder()
        {
            //Arrange 1
            var doctor = ViewPatientDetailMockData.GetDoctorProfile();
            var patient = ViewPatientDetailMockData.GetPatientProfile();
            var laterAppointment = ViewPatientDetailMockData.GetAppointment(
                id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                doctorId: doctor.Id, patientId: patient.Id,
                appointmentDate: new DateTime(2026, 2, 1, 9, 0, 0));
            var earlierAppointment = ViewPatientDetailMockData.GetAppointment(
                id: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                doctorId: doctor.Id, patientId: patient.Id,
                appointmentDate: new DateTime(2026, 1, 15, 9, 0, 0));

            //Arrange 2 — seed out of order (later first)
            SetupDoctorRepo(new[] { doctor });
            SetupAppointmentRepo(new[] { laterAppointment, earlierAppointment });
            SetupPatientRepo(new[] { patient });

            //Act
            var result = await _sut.Process(doctor.UserId, patient.Id);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Data!.Appointments.Should().HaveCount(2);
            result.Data.Appointments.Select(a => a.AppointmentId).Should().ContainInOrder(
                laterAppointment.Id, earlierAppointment.Id);
        }

        /// <summary>
        /// TC-VPD-13: Response <c>Meta</c> is null on success because
        /// <c>CreateSuccessResponse</c> uses the 2-arg <c>Success(codeMessage, data)</c> overload.
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_ResponseShapeHasNoMeta()
        {
            //Arrange 1
            var doctor = ViewPatientDetailMockData.GetDoctorProfile();
            var patient = ViewPatientDetailMockData.GetPatientProfile();
            var appointment = ViewPatientDetailMockData.GetAppointment(
                doctorId: doctor.Id, patientId: patient.Id);

            //Arrange 2
            SetupDoctorRepo(new[] { doctor });
            SetupAppointmentRepo(new[] { appointment });
            SetupPatientRepo(new[] { patient });

            //Act
            var result = await _sut.Process(doctor.UserId, patient.Id);

            //Assert
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
            result.Meta.Should().BeNull();
        }

        /// <summary>
        /// TC-VPD-14: All three repositories are invoked exactly the right number of
        /// times in the happy path: doctor once, patient once, appointment twice
        /// (once for relation check via AnyAsync, once for history via ToListAsync).
        /// </summary>
        [Fact]
        public async Task Process_HappyPath_AllThreeReposInvokedExactlyOnce()
        {
            //Arrange 1
            var doctor = ViewPatientDetailMockData.GetDoctorProfile();
            var patient = ViewPatientDetailMockData.GetPatientProfile();
            var appointment = ViewPatientDetailMockData.GetAppointment(
                doctorId: doctor.Id, patientId: patient.Id);

            //Arrange 2
            SetupDoctorRepo(new[] { doctor });
            SetupAppointmentRepo(new[] { appointment });
            SetupPatientRepo(new[] { patient });

            //Act
            await _sut.Process(doctor.UserId, patient.Id);

            //Assert
            _doctorRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<DoctorProfile, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _patientRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<PatientProfile, bool>>>(), It.IsAny<bool>()),
                Times.Once);
            _appointmentRepoMock.Verify(
                r => r.FindByCondition(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<bool>()),
                Times.Exactly(2));
        }
    }
}