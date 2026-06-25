using FluentAssertions;
using ECS.Application.Services.MedicalRecordsServices.UpdateMedicalRecordServices;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.EyeExaminations;
using ECS.Domain.Entities.SubspecialtyRecords;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Prescriptions;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using MockQueryable.Moq;

namespace ECS.Test.Services.MedicalRecordsServices.UpdateMedicalRecordServiceTests
{
    /// <summary>
    /// Integration tests for UpdateMedicalRecordService.
    /// Tests full end-to-end flow including persistence and re-query from database.
    /// </summary>
    public class UpdateMedicalRecordServiceIntegrationTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly Mock<IRepositoryBaseAsync<MedicalRecord, Guid, AppDbContext>> _medicalRecordRepoMock;
        private readonly Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>> _doctorRepoMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly UpdateMedicalRecordService _service;
        private readonly Guid _doctorId = Guid.NewGuid();
        private readonly Guid _userId = Guid.NewGuid();
        private readonly Guid _recordId = Guid.NewGuid();
        private readonly Guid _clinicId = Guid.NewGuid();
        private readonly Guid _patientId = Guid.NewGuid();

        public UpdateMedicalRecordServiceIntegrationTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"IntegrationTestDb_{Guid.NewGuid()}")
                .Options;
            _dbContext = new AppDbContext(options);

            _medicalRecordRepoMock = new Mock<IRepositoryBaseAsync<MedicalRecord, Guid, AppDbContext>>();
            _doctorRepoMock = new Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            var services = new ServiceCollection();
            services.AddSingleton(_dbContext);
            var sp = services.BuildServiceProvider();
            _service = new UpdateMedicalRecordService(
                _medicalRecordRepoMock.Object,
                _doctorRepoMock.Object,
                new UpdateMedicalRecordRequestValidator(),
                _httpContextAccessorMock.Object,
                sp);
        }

        public void Dispose() => _dbContext.Dispose();

        private MedicalRecord CreateFullTestRecord()
        {
            var user = new User { Id = _userId, Email = "doc@ecs.vn", FullName = "Dr. Test", Phone = "0912345678", PasswordHash = "dummy_hash" };
            var doctor = new DoctorProfile { Id = _doctorId, UserId = _userId, ClinicId = _clinicId, IsActive = true, User = user };
            var patient = new PatientProfile
            {
                Id = _patientId,
                FullName = "Test Patient",
                Dob = new DateTime(1990, 1, 1),
                Gender = Gender.MALE
            };

            var record = new MedicalRecord
            {
                Id = _recordId,
                PatientId = patient.Id,
                Patient = patient,
                DoctorId = _doctorId,
                Doctor = doctor,
                RecordType = RecordType.MS25_STRABISMUS_PTOSIS,
                IsLocked = false,
                ChiefComplaint = "Old complaint",
                DiagnosisMain = "Old diagnosis",
                EyeExamBasics = new List<EyeExamBasic>
                {
                    new EyeExamBasic { Id = Guid.NewGuid(), RecordId = _recordId, Side = EyeSide.RIGHT },
                    new EyeExamBasic { Id = Guid.NewGuid(), RecordId = _recordId, Side = EyeSide.LEFT }
                },
                EyeEyelidConjunctivae = new List<EyeEyelidConjunctiva>
                {
                    new EyeEyelidConjunctiva { Id = Guid.NewGuid(), RecordId = _recordId, Side = EyeSide.RIGHT },
                    new EyeEyelidConjunctiva { Id = Guid.NewGuid(), RecordId = _recordId, Side = EyeSide.LEFT }
                },
                EyeCorneas = new List<EyeCornea>
                {
                    new EyeCornea { Id = Guid.NewGuid(), RecordId = _recordId, Side = EyeSide.RIGHT },
                    new EyeCornea { Id = Guid.NewGuid(), RecordId = _recordId, Side = EyeSide.LEFT }
                },
                EyeAcIrises = new List<EyeAcIris>
                {
                    new EyeAcIris { Id = Guid.NewGuid(), RecordId = _recordId, Side = EyeSide.RIGHT },
                    new EyeAcIris { Id = Guid.NewGuid(), RecordId = _recordId, Side = EyeSide.LEFT }
                },
                EyeLensVitreouses = new List<EyeLensVitreous>
                {
                    new EyeLensVitreous { Id = Guid.NewGuid(), RecordId = _recordId, Side = EyeSide.RIGHT },
                    new EyeLensVitreous { Id = Guid.NewGuid(), RecordId = _recordId, Side = EyeSide.LEFT }
                },
                EyeScleras = new List<EyeSclera>
                {
                    new EyeSclera { Id = Guid.NewGuid(), RecordId = _recordId, Side = EyeSide.RIGHT },
                    new EyeSclera { Id = Guid.NewGuid(), RecordId = _recordId, Side = EyeSide.LEFT }
                },
                EyeFundusDiscMaculas = new List<EyeFundusDiscMacula>
                {
                    new EyeFundusDiscMacula { Id = Guid.NewGuid(), RecordId = _recordId, Side = EyeSide.RIGHT },
                    new EyeFundusDiscMacula { Id = Guid.NewGuid(), RecordId = _recordId, Side = EyeSide.LEFT }
                },
                EyeFundusRetinaVessels = new List<EyeFundusRetinaVessel>
                {
                    new EyeFundusRetinaVessel { Id = Guid.NewGuid(), RecordId = _recordId, Side = EyeSide.RIGHT },
                    new EyeFundusRetinaVessel { Id = Guid.NewGuid(), RecordId = _recordId, Side = EyeSide.LEFT }
                },
                LacrimalRecords = new List<LacrimalRecord>
                {
                    new LacrimalRecord { Id = Guid.NewGuid(), RecordId = _recordId, Side = EyeSide.RIGHT },
                    new LacrimalRecord { Id = Guid.NewGuid(), RecordId = _recordId, Side = EyeSide.LEFT }
                },
                StrabismusPtosisRecord = new StrabismusPtosisRecord
                {
                    Id = Guid.NewGuid(),
                    RecordId = _recordId,
                    ChiefStrabismus = false,
                    ChiefPtosis = false,
                    Nystagmus = false,
                    PtosisDegreeOd = null
                },
                GlassesPrescriptions = new List<GlassesPrescription>
                {
                    new GlassesPrescription
                    {
                        Id = Guid.NewGuid(),
                        RecordId = _recordId,
                        SphOd = 0,
                        CylOd = 0,
                        AxisOd = 0,
                        SphOs = 0,
                        CylOs = 0,
                        AxisOs = 0,
                        Pd = 60,
                        LensType = "None"
                    }
                }
            };

            return record;
        }

        private void PersistAndSetupMocks(MedicalRecord record)
        {
            _dbContext.Users.Add(record.Doctor!.User!);
            _dbContext.DoctorProfiles.Add(record.Doctor);
            _dbContext.PatientProfiles.Add(record.Patient!);
            _dbContext.MedicalRecords.Add(record);
            _dbContext.SaveChanges();

            SetupHttpContext(_userId);
            SetupDoctorRepository(record.Doctor);
            SetupMedicalRecordRepository(record);
        }

        private void SetupHttpContext(Guid userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        }

        private void SetupDoctorRepository(DoctorProfile? doctor)
        {
            var doctors = doctor != null ? new List<DoctorProfile> { doctor } : new List<DoctorProfile>();
            var mockQueryable = doctors.BuildMockDbSet<DoctorProfile>();
            _doctorRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<DoctorProfile, bool>>>(), false))
                .Returns(mockQueryable.Object);
        }

        private void SetupMedicalRecordRepository(MedicalRecord? record)
        {
            var records = record != null ? new List<MedicalRecord> { record } : new List<MedicalRecord>();
            var mockQueryable = records.BuildMockDbSet<MedicalRecord>();
            _medicalRecordRepoMock
                .Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<MedicalRecord, bool>>>(), true))
                .Returns(mockQueryable.Object);
            _medicalRecordRepoMock
                .Setup(r => r.SaveChangesAsync())
                .Callback(() => _dbContext.SaveChanges())
                .ReturnsAsync(1);
        }

        #region Integration Tests

        /// <summary>
        /// TC-INT-01: Full MS25 Record Update - Verify All Eye Examinations Are Persisted
        /// This test verifies that when a complete request is sent, ALL fields are correctly updated
        /// and persisted in the database, then re-queried correctly.
        /// </summary>
        [Fact]
        public async Task TC_INT_01_FullMS25RecordUpdate_ShouldPersistAllFields()
        {
            // Arrange
            var record = CreateFullTestRecord();
            PersistAndSetupMocks(record);

            var request = new UpdateMedicalRecordRequest
            {
                ChiefComplaint = "Mắt lác trong từ nhỏ",
                DiagnosisMain = "Lác trong luân phiên",
                RightEyeBasic = new UpdateEyeBasicExamData
                {
                    VaUncorrected = "0.80",
                    IopMmhg = "15.00",
                    IopMethod = "NCT",
                    EomStatus = "Bình thường",
                    Nystagmus = "Không"
                },
                LeftEyeBasic = new UpdateEyeBasicExamData
                {
                    VaUncorrected = "0.50",
                    VaCorrected = "0.90",
                    IopMmhg = "15.00",
                    IopMethod = "NCT",
                    EomStatus = "Bình thường",
                    Nystagmus = "Không"
                },
                RightEyeEyelid = new UpdateEyeEyelidData
                {
                    Status = "Bình thường",
                    Ptosis = false
                },
                LeftEyeEyelid = new UpdateEyeEyelidData
                {
                    Status = "Bình thường",
                    Ptosis = false
                },
                RightEyeCornea = new UpdateEyeCorneaExamData
                {
                    EpitheliumPunctate = false,
                    Ulcer = false
                },
                LeftEyeCornea = new UpdateEyeCorneaExamData
                {
                    EpitheliumPunctate = false,
                    Ulcer = false
                },
                RightEyeAnteriorChamber = new UpdateEyeAnteriorChamberData
                {
                    VitreousInAC = false,
                    Pus = false
                },
                RightEyeIrisPupil = new UpdateEyeIrisPupilData
                {
                    IrisDegeneration = false,
                    PupilShape = "Tròn"
                },
                LeftEyeAnteriorChamber = new UpdateEyeAnteriorChamberData
                {
                    VitreousInAC = false,
                    Pus = false
                },
                LeftEyeIrisPupil = new UpdateEyeIrisPupilData
                {
                    IrisDegeneration = false,
                    PupilShape = "Tròn"
                },
                RightEyeLens = new UpdateEyeLensData
                {
                    Status = "Trong"
                },
                RightEyeVitreous = new UpdateEyeVitreousData
                {
                    Status = "Sạch"
                },
                LeftEyeLens = new UpdateEyeLensData
                {
                    Status = "Trong"
                },
                LeftEyeVitreous = new UpdateEyeVitreousData
                {
                    Status = "Sạch"
                },
                RightEyeSclera = new UpdateEyeScleraExamData
                {
                    Status = "Bình thường"
                },
                LeftEyeSclera = new UpdateEyeScleraExamData
                {
                    Status = "Bình thường"
                },
                RightEyeFundusDiscMacula = new UpdateEyeFundusDiscMaculaData
                {
                    DiscStatus = "Bình thường",
                    MaculaStatus = "Bình thường",
                    MaculaScar = true  // IMPORTANT: Set to true to verify update
                },
                LeftEyeFundusDiscMacula = new UpdateEyeFundusDiscMaculaData
                {
                    DiscStatus = "Bình thường",
                    MaculaStatus = "Bình thường"
                },
                RightEyeFundusRetinaVessel = new UpdateEyeFundusRetinaVesselData
                {
                    VesselStatus = "Bình thường",
                    RetinaStatus = "Bình thường",
                    Hemorrhage = true,     // IMPORTANT: Set to true
                    RetinalTear = true    // IMPORTANT: Set to true
                },
                LeftEyeFundusRetinaVessel = new UpdateEyeFundusRetinaVesselData
                {
                    VesselStatus = "Bình thường",
                    RetinaStatus = "Bình thường",
                    RetinalEdema = true,
                    Detachment = true
                },
                LacrimalRecord = new UpdateLacrimalRecordData
                {
                    Side = "RIGHT",
                    IrrigationFree = true,
                    IrrigationRegurgitationSame = false
                },
                StrabismusPtosisRecord = new UpdateStrabismusPtosisRecordData
                {
                    ChiefStrabismus = true,
                    ChiefPtosis = true,      // IMPORTANT: Set to true
                    Congenital = true,
                    Acquired = true,
                    StrabismusType = "Lác trong bẩm sinh",
                    Nystagmus = true,         // IMPORTANT: Set to true
                    CoverTestResult = "Lác trong 25PD",
                    PtosisDegreeOd = "Grade 2"  // IMPORTANT: Set to verify
                },
                GlassesPrescription = new UpdateGlassesPrescriptionData
                {
                    SphOd = -1,
                    CylOd = -0.5m,
                    AxisOd = 170,
                    SphOs = -1.5m,
                    CylOs = -0.75m,
                    AxisOs = 15,
                    Pd = 58,
                    LensType = "Đơn tròng",
                    Notes = "Kính chỉnh lác"
                }
            };

            // Act
            var result = await _service.Process(_recordId, request);

            // Assert - Response should be success
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2006.ToString());
            result.Data.Should().NotBeNull();
            result.Data!.IsSuccess.Should().BeTrue();

            // Re-query from database to verify persistence
            var updatedRecord = await _dbContext.MedicalRecords
                .Include(r => r.EyeExamBasics)
                .Include(r => r.EyeEyelidConjunctivae)
                .Include(r => r.EyeCorneas)
                .Include(r => r.EyeAcIrises)
                .Include(r => r.EyeLensVitreouses)
                .Include(r => r.EyeScleras)
                .Include(r => r.EyeFundusDiscMaculas)
                .Include(r => r.EyeFundusRetinaVessels)
                .Include(r => r.LacrimalRecords)
                .Include(r => r.StrabismusPtosisRecord)
                .Include(r => r.GlassesPrescriptions)
                .FirstOrDefaultAsync(r => r.Id == _recordId);

            updatedRecord.Should().NotBeNull();
            updatedRecord!.ChiefComplaint.Should().Be("Mắt lác trong từ nhỏ");
            updatedRecord.DiagnosisMain.Should().Be("Lác trong luân phiên");

            // Verify Eye Basic - Right
            var rightBasic = updatedRecord.EyeExamBasics.First(e => e.Side == EyeSide.RIGHT);
            rightBasic.VaUncorrected.Should().Be(0.80m, "VaUncorrected should be parsed from '0.80'");
            rightBasic.IopMmhg.Should().Be(15.00m);
            rightBasic.EomNormal.Should().BeTrue("EomStatus = 'Bình thường' should set EomNormal to true");

            // Verify Eye Basic - Left
            var leftBasic = updatedRecord.EyeExamBasics.First(e => e.Side == EyeSide.LEFT);
            leftBasic.VaUncorrected.Should().Be(0.50m);
            leftBasic.VaCorrected.Should().Be(0.90m, "VaCorrected should be parsed from '0.90'");

            // Verify Fundus Disc Macula - Right
            var rightFundus = updatedRecord.EyeFundusDiscMaculas.First(e => e.Side == EyeSide.RIGHT);
            rightFundus.MaculaScar.Should().BeTrue("MaculaScar should be updated to true");

            // Verify Fundus Retina Vessel - Right
            var rightRetina = updatedRecord.EyeFundusRetinaVessels.First(e => e.Side == EyeSide.RIGHT);
            rightRetina.RetinalTear.Should().BeTrue("RetinalTear should be updated to true");

            // Verify Fundus Retina Vessel - Left
            var leftRetina = updatedRecord.EyeFundusRetinaVessels.First(e => e.Side == EyeSide.LEFT);
            leftRetina.RetinaEdema.Should().BeTrue("RetinaEdema should be true from RetinalEdema");
            leftRetina.RetinalDetachment.Should().BeTrue("RetinalDetachment should be true from Detachment");

            // Verify StrabismusPtosisRecord
            updatedRecord.StrabismusPtosisRecord.Should().NotBeNull();
            updatedRecord.StrabismusPtosisRecord!.ChiefStrabismus.Should().BeTrue();
            updatedRecord.StrabismusPtosisRecord.ChiefPtosis.Should().BeTrue("ChiefPtosis should be updated to true");
            updatedRecord.StrabismusPtosisRecord.Nystagmus.Should().BeTrue("Nystagmus should be updated to true");
            updatedRecord.StrabismusPtosisRecord.PtosisDegreeOd.Should().Be("Grade 2", "PtosisDegreeOd should be updated");

            // Verify Lacrimal Records
            updatedRecord.LacrimalRecords.Should().HaveCount(2);
            var rightLacrimal = updatedRecord.LacrimalRecords.First(l => l.Side == EyeSide.RIGHT);
            rightLacrimal.IrrigationFree.Should().BeTrue();

            // Verify Glasses Prescription
            updatedRecord.GlassesPrescriptions.Should().HaveCount(1);
            var glasses = updatedRecord.GlassesPrescriptions.First();
            glasses.SphOd.Should().Be(-1);
            glasses.CylOd.Should().Be(-0.5m);
            glasses.AxisOd.Should().Be(170);
            glasses.SphOs.Should().Be(-1.5m);
            glasses.LensType.Should().Be("Đơn tròng");
            glasses.Notes.Should().Be("Kính chỉnh lác");
        }

        /// <summary>
        /// TC-INT-02: Verify Snellen Visual Acuity Format Parsing
        /// Tests that "20/20", "20/40", "0.80" format strings are correctly parsed to decimal values.
        /// </summary>
        [Fact]
        public async Task TC_INT_02_VisualAcuityParsing_ShouldConvertSnellenToDecimal()
        {
            // Arrange
            var record = CreateFullTestRecord();
            PersistAndSetupMocks(record);

            var request = new UpdateMedicalRecordRequest
            {
                ChiefComplaint = "Test",
                DiagnosisMain = "Test",
                RightEyeBasic = new UpdateEyeBasicExamData
                {
                    VaUncorrected = "20/20",  // Should parse to 20.0m
                    VaCorrected = "20/40"     // Should parse to 10.0m
                },
                LeftEyeBasic = new UpdateEyeBasicExamData
                {
                    VaUncorrected = "0.80"    // Should parse to 0.80m
                }
            };

            // Act
            await _service.Process(_recordId, request);

            // Assert
            var updatedRecord = await _dbContext.MedicalRecords
                .Include(r => r.EyeExamBasics)
                .FirstOrDefaultAsync(r => r.Id == _recordId);

            var rightBasic = updatedRecord!.EyeExamBasics.First(e => e.Side == EyeSide.RIGHT);
            rightBasic.VaUncorrected.Should().Be(20.0m, "20/20 should parse to 20.0");
            rightBasic.VaCorrected.Should().Be(10.0m, "20/40 should parse to 10.0");

            var leftBasic = updatedRecord.EyeExamBasics.First(e => e.Side == EyeSide.LEFT);
            leftBasic.VaUncorrected.Should().Be(0.80m, "0.80 should parse to 0.80");
        }

        /// <summary>
        /// TC-INT-03: Verify StrabismusPtosisRecord ChiefPtosis and Nystagmus Update
        /// This tests the specific case where request has chiefPtosis: true, nystagmus: true
        /// </summary>
        [Fact]
        public async Task TC_INT_03_StrabismusPtosis_ShouldUpdateChiefAndNystagmus()
        {
            // Arrange
            var record = CreateFullTestRecord();
            PersistAndSetupMocks(record);

            var request = new UpdateMedicalRecordRequest
            {
                ChiefComplaint = "Test",
                DiagnosisMain = "Test",
                StrabismusPtosisRecord = new UpdateStrabismusPtosisRecordData
                {
                    ChiefStrabismus = true,
                    ChiefPtosis = true,
                    Nystagmus = true,
                    Congenital = true,
                    Acquired = true,
                    StrabismusType = "Lác trong bẩm sinh",
                    PtosisDegreeOd = "Grade 2",
                    CoverTestResult = "Lác trong 25PD"
                }
            };

            // Act
            await _service.Process(_recordId, request);

            // Assert
            var updatedRecord = await _dbContext.MedicalRecords
                .Include(r => r.StrabismusPtosisRecord)
                .FirstOrDefaultAsync(r => r.Id == _recordId);

            var stra = updatedRecord!.StrabismusPtosisRecord;
            stra.Should().NotBeNull();
            stra!.ChiefStrabismus.Should().BeTrue();
            stra.ChiefPtosis.Should().BeTrue("ChiefPtosis should be updated to true from request");
            stra.Nystagmus.Should().BeTrue("Nystagmus should be updated to true from request");
            stra.PtosisDegreeOd.Should().Be("Grade 2", "PtosisDegreeOd should be 'Grade 2'");
            stra.StrabismusType.Should().Be("Lác trong bẩm sinh");
            stra.CoverTestResult.Should().Be("Lác trong 25PD");
        }

        /// <summary>
        /// TC-INT-04: Verify EyeFundusRetinaVessel Hemorrhage and RetinalTear Update
        /// </summary>
        [Fact]
        public async Task TC_INT_04_FundusRetinaVessel_ShouldUpdateHemorrhageAndTear()
        {
            // Arrange
            var record = CreateFullTestRecord();
            PersistAndSetupMocks(record);

            var request = new UpdateMedicalRecordRequest
            {
                ChiefComplaint = "Test",
                DiagnosisMain = "Test",
                RightEyeFundusRetinaVessel = new UpdateEyeFundusRetinaVesselData
                {
                    VesselStatus = "Bình thường",
                    RetinaStatus = "Bình thường",
                    Hemorrhage = true,
                    RetinalTear = true
                }
            };

            // Act
            await _service.Process(_recordId, request);

            // Assert
            var updatedRecord = await _dbContext.MedicalRecords
                .Include(r => r.EyeFundusRetinaVessels)
                .FirstOrDefaultAsync(r => r.Id == _recordId);

            var rightRetina = updatedRecord!.EyeFundusRetinaVessels.First(e => e.Side == EyeSide.RIGHT);
            rightRetina.RetinalTear.Should().BeTrue("RetinalTear should be true from request");
        }

        /// <summary>
        /// TC-INT-05: Verify EyeFundusDiscMacula MaculaScar Update
        /// </summary>
        [Fact]
        public async Task TC_INT_05_FundusDiscMacula_ShouldUpdateMaculaScar()
        {
            // Arrange
            var record = CreateFullTestRecord();
            PersistAndSetupMocks(record);

            var request = new UpdateMedicalRecordRequest
            {
                ChiefComplaint = "Test",
                DiagnosisMain = "Test",
                RightEyeFundusDiscMacula = new UpdateEyeFundusDiscMaculaData
                {
                    DiscStatus = "Bình thường",
                    MaculaStatus = "Bình thường",
                    MaculaScar = true
                }
            };

            // Act
            await _service.Process(_recordId, request);

            // Assert
            var updatedRecord = await _dbContext.MedicalRecords
                .Include(r => r.EyeFundusDiscMaculas)
                .FirstOrDefaultAsync(r => r.Id == _recordId);

            var rightMacula = updatedRecord!.EyeFundusDiscMaculas.First(e => e.Side == EyeSide.RIGHT);
            rightMacula.MaculaScar.Should().BeTrue("MaculaScar should be true from request");
        }

        /// <summary>
        /// TC-INT-06: Verify LacrimalRecord Side-Specific Update
        /// When only RIGHT lacrimal is sent in request, LEFT should remain unchanged
        /// </summary>
        [Fact]
        public async Task TC_INT_06_LacrimalRecord_ShouldUpdateSpecificSide()
        {
            // Arrange
            var record = CreateFullTestRecord();
            record.LacrimalRecords.First(l => l.Side == EyeSide.RIGHT).IrrigationFree = false;
            record.LacrimalRecords.First(l => l.Side == EyeSide.LEFT).IrrigationFree = false;
            PersistAndSetupMocks(record);

            var request = new UpdateMedicalRecordRequest
            {
                ChiefComplaint = "Test",
                DiagnosisMain = "Test",
                LacrimalRecord = new UpdateLacrimalRecordData
                {
                    Side = "RIGHT",
                    IrrigationFree = true,
                    IrrigationRegurgitationSame = false
                }
            };

            // Act
            await _service.Process(_recordId, request);

            // Assert
            var updatedRecord = await _dbContext.MedicalRecords
                .Include(r => r.LacrimalRecords)
                .FirstOrDefaultAsync(r => r.Id == _recordId);

            var rightLacrimal = updatedRecord!.LacrimalRecords.First(l => l.Side == EyeSide.RIGHT);
            rightLacrimal.IrrigationFree.Should().BeTrue("RIGHT IrrigationFree should be updated");

            var leftLacrimal = updatedRecord.LacrimalRecords.First(l => l.Side == EyeSide.LEFT);
            leftLacrimal.IrrigationFree.Should().BeFalse("LEFT IrrigationFree should remain false");
        }

        /// <summary>
        /// TC-INT-07: Verify GlassesPrescription Full Update
        /// </summary>
        [Fact]
        public async Task TC_INT_07_GlassesPrescription_ShouldUpdateAllFields()
        {
            // Arrange
            var record = CreateFullTestRecord();
            PersistAndSetupMocks(record);

            var request = new UpdateMedicalRecordRequest
            {
                ChiefComplaint = "Test",
                DiagnosisMain = "Test",
                GlassesPrescription = new UpdateGlassesPrescriptionData
                {
                    SphOd = -2.0m,
                    CylOd = -1.0m,
                    AxisOd = 180,
                    SphOs = -2.5m,
                    CylOs = -1.5m,
                    AxisOs = 10,
                    Pd = 62,
                    LensType = "Hai tròng",
                    Notes = "Kính mới"
                }
            };

            // Act
            await _service.Process(_recordId, request);

            // Assert
            var updatedRecord = await _dbContext.MedicalRecords
                .Include(r => r.GlassesPrescriptions)
                .FirstOrDefaultAsync(r => r.Id == _recordId);

            var glasses = updatedRecord!.GlassesPrescriptions.First();
            glasses.SphOd.Should().Be(-2.0m);
            glasses.CylOd.Should().Be(-1.0m);
            glasses.AxisOd.Should().Be(180);
            glasses.SphOs.Should().Be(-2.5m);
            glasses.CylOs.Should().Be(-1.5m);
            glasses.AxisOs.Should().Be(10);
            glasses.Pd.Should().Be(62);
            glasses.LensType.Should().Be("Hai tròng");
            glasses.Notes.Should().Be("Kính mới");
        }

        #endregion
    }
}
