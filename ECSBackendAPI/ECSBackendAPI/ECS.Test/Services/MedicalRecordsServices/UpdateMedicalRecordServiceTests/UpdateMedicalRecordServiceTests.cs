using FluentAssertions;
using ECS.Application.Services.MedicalRecordsServices.UpdateMedicalRecordServices;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.EyeExaminations;
using ECS.Domain.Entities.SubspecialtyRecords;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Auth;
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
    /// Unit tests for UpdateMedicalRecordService.
    /// TC-UMR-01 through TC-UMR-08
    /// Uses in-memory database for testing with real AppDbContext.
    /// </summary>
    public class UpdateMedicalRecordServiceTests : IDisposable
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

        public UpdateMedicalRecordServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
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

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        #region Helper Methods

        private MedicalRecord CreateBasicTestRecord()
        {
            var user = new User
            {
                Id = _userId,
                Email = "doctor@ecs.vn",
                FullName = "Dr. Test",
                Phone = "0912345678",
                PasswordHash = "dummy_hash_for_test"
            };

            var doctor = new DoctorProfile
            {
                Id = _doctorId,
                UserId = _userId,
                ClinicId = _clinicId,
                User = user,
                IsActive = true
            };

            var patient = new PatientProfile
            {
                Id = Guid.NewGuid(),
                FullName = "Nguyen Van A",
                Dob = new DateTime(1990, 1, 1),
                Gender = Gender.MALE,
                PhoneNumber = "0912345678",
                Address = "Test Address"
            };

            var record = new MedicalRecord
            {
                Id = _recordId,
                PatientId = patient.Id,
                Patient = patient,
                DoctorId = _doctorId,
                Doctor = doctor,
                RecordType = RecordType.MS23_FUNDUS,
                IsLocked = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                EyeExamBasics = new List<EyeExamBasic>(),
                EyeEyelidConjunctivae = new List<EyeEyelidConjunctiva>(),
                EyeCorneas = new List<EyeCornea>(),
                EyeAcIrises = new List<EyeAcIris>(),
                EyeLensVitreouses = new List<EyeLensVitreous>(),
                EyeScleras = new List<EyeSclera>(),
                EyeFundusDiscMaculas = new List<EyeFundusDiscMacula>(),
                EyeFundusRetinaVessels = new List<EyeFundusRetinaVessel>()
            };

            // Add eye examination entities for both eyes
            record.EyeExamBasics.Add(new EyeExamBasic { Id = Guid.NewGuid(), RecordId = record.Id, Side = EyeSide.RIGHT });
            record.EyeExamBasics.Add(new EyeExamBasic { Id = Guid.NewGuid(), RecordId = record.Id, Side = EyeSide.LEFT });
            record.EyeEyelidConjunctivae.Add(new EyeEyelidConjunctiva { Id = Guid.NewGuid(), RecordId = record.Id, Side = EyeSide.RIGHT });
            record.EyeEyelidConjunctivae.Add(new EyeEyelidConjunctiva { Id = Guid.NewGuid(), RecordId = record.Id, Side = EyeSide.LEFT });
            record.EyeCorneas.Add(new EyeCornea { Id = Guid.NewGuid(), RecordId = record.Id, Side = EyeSide.RIGHT });
            record.EyeCorneas.Add(new EyeCornea { Id = Guid.NewGuid(), RecordId = record.Id, Side = EyeSide.LEFT });
            record.EyeAcIrises.Add(new EyeAcIris { Id = Guid.NewGuid(), RecordId = record.Id, Side = EyeSide.RIGHT });
            record.EyeAcIrises.Add(new EyeAcIris { Id = Guid.NewGuid(), RecordId = record.Id, Side = EyeSide.LEFT });
            record.EyeLensVitreouses.Add(new EyeLensVitreous { Id = Guid.NewGuid(), RecordId = record.Id, Side = EyeSide.RIGHT });
            record.EyeLensVitreouses.Add(new EyeLensVitreous { Id = Guid.NewGuid(), RecordId = record.Id, Side = EyeSide.LEFT });
            record.EyeScleras.Add(new EyeSclera { Id = Guid.NewGuid(), RecordId = record.Id, Side = EyeSide.RIGHT });
            record.EyeScleras.Add(new EyeSclera { Id = Guid.NewGuid(), RecordId = record.Id, Side = EyeSide.LEFT });
            record.EyeFundusDiscMaculas.Add(new EyeFundusDiscMacula { Id = Guid.NewGuid(), RecordId = record.Id, Side = EyeSide.RIGHT });
            record.EyeFundusDiscMaculas.Add(new EyeFundusDiscMacula { Id = Guid.NewGuid(), RecordId = record.Id, Side = EyeSide.LEFT });
            record.EyeFundusRetinaVessels.Add(new EyeFundusRetinaVessel { Id = Guid.NewGuid(), RecordId = record.Id, Side = EyeSide.RIGHT });
            record.EyeFundusRetinaVessels.Add(new EyeFundusRetinaVessel { Id = Guid.NewGuid(), RecordId = record.Id, Side = EyeSide.LEFT });

            return record;
        }

        /// <summary>
        /// Creates and persists test record into DbContext.
        /// CRITICAL: Service uses GetDbContext().Set<T>().First() to re-fetch entities,
        /// so entities MUST be persisted before calling the service.
        /// </summary>
        private MedicalRecord CreateAndPersistTestMedicalRecord()
        {
            var record = CreateBasicTestRecord();
            _dbContext.Users.Add(record.Doctor!.User!);
            _dbContext.DoctorProfiles.Add(record.Doctor);
            _dbContext.PatientProfiles.Add(record.Patient!);
            _dbContext.MedicalRecords.Add(record);
            _dbContext.SaveChanges();
            return record;
        }

        private MedicalRecord CreateAndPersistTestMedicalRecordWithStrabismus()
        {
            var record = CreateBasicTestRecord();
            record.StrabismusPtosisRecord = new StrabismusPtosisRecord
            {
                Id = Guid.NewGuid(),
                RecordId = record.Id
            };
            _dbContext.StrabismusPtosisRecords.Add(record.StrabismusPtosisRecord);
            _dbContext.SaveChanges();
            return record;
        }

        private void SetupHttpContext(Guid userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            };

            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        }

        private void SetupDoctorRepository(DoctorProfile? doctor)
        {
            var doctors = doctor != null ? new List<DoctorProfile> { doctor } : new List<DoctorProfile>();
            var mockQueryable = doctors.BuildMockDbSet<DoctorProfile>();

            _doctorRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<System.Linq.Expressions.Expression<Func<DoctorProfile, bool>>>(),
                    false))
                .Returns(mockQueryable.Object);
        }

        private void SetupMedicalRecordRepository(MedicalRecord? record)
        {
            var records = record != null ? new List<MedicalRecord> { record } : new List<MedicalRecord>();
            var mockQueryable = records.BuildMockDbSet<MedicalRecord>();

            _medicalRecordRepoMock
                .Setup(r => r.FindByCondition(
                    It.IsAny<System.Linq.Expressions.Expression<Func<MedicalRecord, bool>>>(),
                    true))
                .Returns(mockQueryable.Object);

            _medicalRecordRepoMock
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);
        }

        private void InvokeUpdateEyeExaminations(UpdateMedicalRecordService service, UpdateMedicalRecordRequest request, MedicalRecord record)
        {
            var method = typeof(UpdateMedicalRecordService)
                .GetMethod("UpdateEyeExaminations", BindingFlags.NonPublic | BindingFlags.Instance);
            method!.Invoke(service, new object[] { record, request });
        }

        private UpdateMedicalRecordRequest CreateValidRequest()
        {
            return new UpdateMedicalRecordRequest
            {
                ChiefComplaint = "Test complaint",
                DiagnosisMain = "Test diagnosis"
            };
        }

        #endregion

        #region TC-UMR-01: UpdateEyeFundusRetinaVessel_ShouldMapAllFields

        /// <summary>
        /// TC-UMR-01: UpdateEyeFundusRetinaVessel_ShouldMapAllFields
        /// Test that when RightEyeFundusRetinaVessel data with retinalEdema: true and detachment: true is passed,
        /// the EyeFundusRetinaVessel entity properties are correctly set BEFORE SaveChanges.
        /// </summary>
        [Fact]
        public void TC_UMR_01_UpdateEyeFundusRetinaVessel_ShouldMapAllFields()
        {
            // Arrange
            var record = CreateAndPersistTestMedicalRecord();
            var request = CreateValidRequest();
            request.RightEyeFundusRetinaVessel = new UpdateEyeFundusRetinaVesselData
            {
                RetinalEdema = true,
                Detachment = true,
                DetachmentLevel = "Toàn bộ",
                RetinaStatus = "Bong võng mạc"
            };

            // Act
            InvokeUpdateEyeExaminations(_service, request, record);

            // Assert - verify entity state AFTER mapping but BEFORE SaveChanges
            var rightRetina = record.EyeFundusRetinaVessels.First(e => e.Side == EyeSide.RIGHT);
            rightRetina.RetinaEdema.Should().BeTrue("RetinaEdema should be true when RetinalEdema is true in request");
            rightRetina.RetinalDetachment.Should().BeTrue("RetinalDetachment should be true when Detachment is true");
            rightRetina.RetinalDetachmentLevel.Should().Be("Toàn bộ");
            rightRetina.RetinalCondition.Should().Be("Bong võng mạc");
        }

        /// <summary>
        /// TC-UMR-01b: Additional assertions for EyeFundusRetinaVessel mapping
        /// Verifies additional fields are correctly mapped
        /// </summary>
        [Fact]
        public void TC_UMR_01b_UpdateEyeFundusRetinaVessel_ShouldMapAllBooleanFields()
        {
            // Arrange
            var record = CreateAndPersistTestMedicalRecord();
            var request = CreateValidRequest();
            request.RightEyeFundusRetinaVessel = new UpdateEyeFundusRetinaVesselData
            {
                RetinalEdema = true,
                Detachment = true,
                RetinalTear = true,
                Iofb = true,
                VesselStatus = "Bình thường"
            };

            // Act
            InvokeUpdateEyeExaminations(_service, request, record);

            // Assert
            var rightRetina = record.EyeFundusRetinaVessels.First(e => e.Side == EyeSide.RIGHT);
            rightRetina.RetinalDetachment.Should().BeTrue();
            rightRetina.RetinalTear.Should().BeTrue();
            rightRetina.IntraocularForeignBody.Should().BeTrue();
            rightRetina.VesselNormal.Should().BeTrue();
        }

        #endregion

        #region TC-UMR-02: UpdateEyeFundusDiscMacula_ShouldMapAllFields

        /// <summary>
        /// TC-UMR-02: UpdateEyeFundusDiscMacula_ShouldMapAllFields
        /// Test that RightEyeFundusDiscMacula with neovascularization: true, maculaScar: true,
        /// maculaHoleDegree: "Có" is correctly mapped to the entity.
        /// </summary>
        [Fact]
        public void TC_UMR_02_UpdateEyeFundusDiscMacula_ShouldMapAllFields()
        {
            // Arrange
            var record = CreateAndPersistTestMedicalRecord();
            var request = CreateValidRequest();
            request.RightEyeFundusDiscMacula = new UpdateEyeFundusDiscMaculaData
            {
                Neovascularization = true,
                MaculaScar = true,
                MaculaHoleDegree = "Có",
                DiscStatus = "Bình thường",
                MaculaStatus = "Sẹo"
            };

            // Act
            InvokeUpdateEyeExaminations(_service, request, record);

            // Assert
            var rightDiscMacula = record.EyeFundusDiscMaculas.First(e => e.Side == EyeSide.RIGHT);
            rightDiscMacula.OpticDiscNeovascularization.Should().BeTrue("Neovascularization should be true");
            rightDiscMacula.MaculaScar.Should().BeTrue("MaculaScar should be true");
            rightDiscMacula.MaculaHoleDegree.Should().Be("Có");
            rightDiscMacula.OpticDiscNormal.Should().BeTrue("DiscStatus = Bình thường should set Normal to true");
            rightDiscMacula.MaculaCondition.Should().Be("Sẹo");
        }

        /// <summary>
        /// TC-UMR-02b: Additional assertions for EyeFundusDiscMacula mapping
        /// Verifies disc status and edema mapping
        /// </summary>
        [Fact]
        public void TC_UMR_02b_UpdateEyeFundusDiscMacula_ShouldMapDiscStatusCorrectly()
        {
            // Arrange
            var record = CreateAndPersistTestMedicalRecord();
            var request = CreateValidRequest();
            request.RightEyeFundusDiscMacula = new UpdateEyeFundusDiscMaculaData
            {
                DiscStatus = "Phù",
                DiscColor = "Đỏ",
                CdRatio = "0.6",
                DiscHemorrhage = true
            };

            // Act
            InvokeUpdateEyeExaminations(_service, request, record);

            // Assert
            var rightDiscMacula = record.EyeFundusDiscMaculas.First(e => e.Side == EyeSide.RIGHT);
            rightDiscMacula.OpticDiscNormal.Should().BeFalse("DiscStatus = Phù should set Normal to false");
            rightDiscMacula.OpticDiscEdema.Should().BeTrue("DiscStatus = Phù should set Edema to true");
            rightDiscMacula.OpticDiscColor.Should().Be("Đỏ");
            rightDiscMacula.OpticDiscCupRatio.Should().Be("0.6");
            rightDiscMacula.OpticDiscHemorrhage.Should().BeTrue();
        }

        #endregion

        #region TC-UMR-03: UpdateStrabismusPtosis_ShouldMapAllFields

        /// <summary>
        /// TC-UMR-03: UpdateStrabismusPtosis_ShouldMapAllFields
        /// Test that StrabismusPtosisRecord with nystagmus: true, strabismusType: "Lác chéo",
        /// chiefPtosis: true is correctly mapped.
        /// </summary>
        [Fact]
        public void TC_UMR_03_UpdateStrabismusPtosis_ShouldMapAllFields()
        {
            // Arrange
            var record = CreateAndPersistTestMedicalRecordWithStrabismus();
            var request = CreateValidRequest();
            request.StrabismusPtosisRecord = new UpdateStrabismusPtosisRecordData
            {
                Nystagmus = true,
                StrabismusType = "Lác chéo",
                ChiefPtosis = true,
                Congenital = true
            };

            // Act
            var method = typeof(UpdateMedicalRecordService)
                .GetMethod("UpdateSubspecialtyRecords", BindingFlags.NonPublic | BindingFlags.Instance);
            method!.Invoke(_service, new object[] { record, request });

            // Assert
            var straRecord = record.StrabismusPtosisRecord;
            straRecord.Should().NotBeNull("StrabismusPtosisRecord should exist");
            straRecord!.Nystagmus.Should().BeTrue("Nystagmus should be true");
            straRecord.StrabismusType.Should().Be("Lác chéo");
            straRecord.ChiefPtosis.Should().BeTrue("ChiefPtosis should be true");
            straRecord.Congenital.Should().BeTrue();
        }

        /// <summary>
        /// TC-UMR-03b: Additional assertions for StrabismusPtosis mapping
        /// Verifies cover test, prism, and other measurements
        /// </summary>
        [Fact]
        public void TC_UMR_03b_UpdateStrabismusPtosis_ShouldMapAdditionalFields()
        {
            // Arrange
            var record = CreateAndPersistTestMedicalRecordWithStrabismus();
            var request = CreateValidRequest();
            request.StrabismusPtosisRecord = new UpdateStrabismusPtosisRecordData
            {
                ChiefStrabismus = true,
                CoverTestResult = "Trả trong ra",
                PrismNear = "20 PD",
                PrismDistance = "25 PD",
                BinocularStatus = "Hợp thị",
                Diplopia = "Có",
                CompensatoryHeadPosture = "Nghiêng đầu"
            };

            // Act
            var method = typeof(UpdateMedicalRecordService)
                .GetMethod("UpdateSubspecialtyRecords", BindingFlags.NonPublic | BindingFlags.Instance);
            method!.Invoke(_service, new object[] { record, request });

            // Assert
            var straRecord = record.StrabismusPtosisRecord!;
            straRecord.ChiefStrabismus.Should().BeTrue();
            straRecord.CoverTestResult.Should().Be("Trả trong ra");
            straRecord.PrismNear.Should().Be("20 PD");
            straRecord.PrismDistance.Should().Be("25 PD");
            straRecord.BinocularStatus.Should().Be("Hợp thị");
            straRecord.Diplopia.Should().Be("Có");
            straRecord.CompensatoryHeadPosture.Should().Be("Nghiêng đầu");
        }

        #endregion

        #region TC-UMR-04: UpdateEyeLens_ShouldMapStatusStringToBoolean

        /// <summary>
        /// TC-UMR-04: UpdateEyeLens_ShouldMapStatusStringToBoolean
        /// Test that when status: "Trong" is passed, LensClear is set to true.
        /// When status: "Đục" is passed, LensClear is set to false.
        /// </summary>
        [Theory]
        [InlineData("Trong", true)]
        [InlineData("Đục", false)]
        [InlineData("", true)]
        public void TC_UMR_04_UpdateEyeLens_ShouldMapStatusStringToBoolean(string status, bool expectedLensClear)
        {
            // Arrange
            var record = CreateAndPersistTestMedicalRecord();
            var request = CreateValidRequest();
            request.RightEyeLens = new UpdateEyeLensData
            {
                Status = status
            };

            // Act
            InvokeUpdateEyeExaminations(_service, request, record);

            // Assert
            var rightLens = record.EyeLensVitreouses.First(e => e.Side == EyeSide.RIGHT);
            rightLens.LensClear.Should().Be(expectedLensClear,
                $"LensClear should be {expectedLensClear} when Status is '{status}'");
        }

        /// <summary>
        /// TC-UMR-04b: Verify Lens with IOL and subluxation
        /// </summary>
        [Fact]
        public void TC_UMR_04b_UpdateEyeLens_WithIolAndSubluxation_ShouldMapCorrectly()
        {
            // Arrange
            var record = CreateAndPersistTestMedicalRecord();
            var request = CreateValidRequest();
            request.RightEyeLens = new UpdateEyeLensData
            {
                Status = "Đục",
                IolPresent = true,
                IolStatus = "Cân",
                IolPosition = "Trong bao",
                Subluxation = true,
                OpacityType = "Nhân"
            };

            // Act
            InvokeUpdateEyeExaminations(_service, request, record);

            // Assert
            var rightLens = record.EyeLensVitreouses.First(e => e.Side == EyeSide.RIGHT);
            rightLens.LensClear.Should().BeFalse();
            rightLens.LensIolPresent.Should().BeTrue();
            rightLens.LensIolStatus.Should().Be("Cân");
            rightLens.LensIolPosition.Should().Be("Trong bao");
            rightLens.LensSubluxation.Should().BeTrue();
            rightLens.LensOpacityType.Should().Be("Nhân");
        }

        #endregion

        #region TC-UMR-05: UpdateEyeSclera_ShouldMapStatusStringToBoolean

        /// <summary>
        /// TC-UMR-05: UpdateEyeSclera_ShouldMapStatusStringToBoolean
        /// Test that status: "Bình thường" sets ScleraNormal to true.
        /// </summary>
        [Fact]
        public void TC_UMR_05_UpdateEyeSclera_ShouldMapStatusStringToBoolean()
        {
            // Arrange
            var record = CreateAndPersistTestMedicalRecord();
            var request = CreateValidRequest();
            request.RightEyeSclera = new UpdateEyeScleraExamData
            {
                Status = "Bình thường"
            };

            // Act
            InvokeUpdateEyeExaminations(_service, request, record);

            // Assert
            var rightSclera = record.EyeScleras.First(e => e.Side == EyeSide.RIGHT);
            rightSclera.ScleraNormal.Should().BeTrue("ScleraNormal should be true when Status is 'Bình thường'");
            rightSclera.ScleraEctasia.Should().BeFalse("ScleraEctasia should be false");
            rightSclera.OldSurgeryScar.Should().BeFalse("OldSurgeryScar should be false");
        }

        /// <summary>
        /// TC-UMR-05b: Verify other sclera status values map correctly
        /// </summary>
        [Theory]
        [InlineData("Giãn lồi", false, true, false)]
        [InlineData("Sẹo", false, false, true)]
        public void TC_UMR_05b_UpdateEyeSclera_ShouldMapOtherStatusCorrectly(
            string status, bool expectedNormal, bool expectedEctasia, bool expectedScar)
        {
            // Arrange
            var record = CreateAndPersistTestMedicalRecord();
            var request = CreateValidRequest();
            request.RightEyeSclera = new UpdateEyeScleraExamData
            {
                Status = status
            };

            // Act
            InvokeUpdateEyeExaminations(_service, request, record);

            // Assert
            var rightSclera = record.EyeScleras.First(e => e.Side == EyeSide.RIGHT);
            rightSclera.ScleraNormal.Should().Be(expectedNormal);
            rightSclera.ScleraEctasia.Should().Be(expectedEctasia);
            rightSclera.OldSurgeryScar.Should().Be(expectedScar);
        }

        #endregion

        #region TC-UMR-06: UpdateEyeCornea_ShouldMapAllBooleanFields

        /// <summary>
        /// TC-UMR-06: UpdateEyeCornea_ShouldMapAllBooleanFields
        /// Test that cornea with ulcer: true, laceration: false, neovascularization: false
        /// correctly sets entity properties.
        /// </summary>
        [Fact]
        public void TC_UMR_06_UpdateEyeCornea_ShouldMapAllBooleanFields()
        {
            // Arrange
            var record = CreateAndPersistTestMedicalRecord();
            var request = CreateValidRequest();
            request.RightEyeCornea = new UpdateEyeCorneaExamData
            {
                Ulcer = true,
                Laceration = false,
                Neovascularization = false,
                Clarity = "Trong"
            };

            // Act
            InvokeUpdateEyeExaminations(_service, request, record);

            // Assert
            var rightCornea = record.EyeCorneas.First(e => e.Side == EyeSide.RIGHT);
            rightCornea.Ulcer.Should().BeTrue("Ulcer should be true");
            rightCornea.Laceration.Should().BeFalse("Laceration should be false");
            rightCornea.Neovascularization.Should().BeFalse("Neovascularization should be false");
            rightCornea.Clarity.Should().Be("Trong");
        }

        /// <summary>
        /// TC-UMR-06b: Verify cornea perforation and other properties
        /// </summary>
        [Fact]
        public void TC_UMR_06b_UpdateEyeCornea_WithPerforation_ShouldMapCorrectly()
        {
            // Arrange
            var record = CreateAndPersistTestMedicalRecord();
            var request = CreateValidRequest();
            request.RightEyeCornea = new UpdateEyeCorneaExamData
            {
                Perforation = true,
                PerforationDiameterMm = 3.5m,
                PerforationLocation = "Trung tâm",
                SeidelTest = "Thủng bít",
                EpitheliumStatus = "Băng keo",
                EpitheliumPunctate = true
            };

            // Act
            InvokeUpdateEyeExaminations(_service, request, record);

            // Assert
            var rightCornea = record.EyeCorneas.First(e => e.Side == EyeSide.RIGHT);
            rightCornea.Perforation.Should().BeTrue();
            rightCornea.PerforationDiameterMm.Should().Be(3.5m);
            rightCornea.PerforationLocation.Should().Be("Trung tâm");
            rightCornea.SeidelTest.Should().Be("Thủng bít");
            rightCornea.BandKeratopathy.Should().BeTrue("EpitheliumStatus='Băng keo' should set BandKeratopathy=true");
            rightCornea.EpitheliumPunctate.Should().BeTrue();
        }

        #endregion

        #region TC-UMR-07: UpdateEyeAcIris_ShouldMapAllBooleanFields

        /// <summary>
        /// TC-UMR-07: UpdateEyeAcIris_ShouldMapAllBooleanFields
        /// Test that AC/Iris with vitreousInAC: true, pus: false correctly sets entity properties.
        /// </summary>
        [Fact]
        public void TC_UMR_07_UpdateEyeAcIris_ShouldMapAllBooleanFields()
        {
            // Arrange
            var record = CreateAndPersistTestMedicalRecord();
            var request = CreateValidRequest();
            request.RightEyeAnteriorChamber = new UpdateEyeAnteriorChamberData
            {
                VitreousInAC = true,
                Pus = false,
                Depth = "Bình thường"
            };

            // Act
            InvokeUpdateEyeExaminations(_service, request, record);

            // Assert
            var rightAcIris = record.EyeAcIrises.First(e => e.Side == EyeSide.RIGHT);
            rightAcIris.AcLensMaterial.Should().BeTrue("AcLensMaterial (vitreousInAC) should be true");
            rightAcIris.AcPus.Should().BeFalse("AcPus should be false when Pus is false");
        }

        /// <summary>
        /// TC-UMR-07b: Verify AC depth and Iris/Pupil mapping
        /// </summary>
        [Fact]
        public void TC_UMR_07b_UpdateEyeAcIris_WithIrisPupil_ShouldMapCorrectly()
        {
            // Arrange
            var record = CreateAndPersistTestMedicalRecord();
            var request = CreateValidRequest();
            request.RightEyeAnteriorChamber = new UpdateEyeAnteriorChamberData
            {
                Depth = "Sâu",
                DepthMm = 4.5m,
                HerickClassification = "≥1/2 GM"
            };
            request.RightEyeIrisPupil = new UpdateEyeIrisPupilData
            {
                IrisColor = "Nâu",
                IrisCondition = "Thoái hóa",
                PupilShape = "Tròn",
                PupilReflex = "Tốt",
                PupilDiameterMm = 3.0m,
                IrisNeovascularization = true
            };

            // Act
            InvokeUpdateEyeExaminations(_service, request, record);

            // Assert
            var rightAcIris = record.EyeAcIrises.First(e => e.Side == EyeSide.RIGHT);
            rightAcIris.AcFlat.Should().BeFalse("AcFlat should be false when Depth is Sâu");
            rightAcIris.AcDepthMm.Should().Be(4.5m, "AcDepthMm should be 4.5 when Depth is Sâu");
            rightAcIris.AcDepthHerick.Should().Be("≥1/2 GM");
            rightAcIris.IrisColor.Should().Be("Nâu");
            rightAcIris.IrisCondition.Should().Be("Thoái hóa");
            rightAcIris.IrisDegeneration.Should().BeTrue("IrisCondition = Thoái hóa should set Degeneration to true");
            rightAcIris.PupilRound.Should().BeTrue("PupilShape = Tròn should set Round to true");
            rightAcIris.PupilReflex.Should().Be("Tốt");
            rightAcIris.PupilDiameterMm.Should().Be(3.0m);
            rightAcIris.IrisNeovascularization.Should().BeTrue();
        }

        #endregion

        #region TC-UMR-08: Process_ShouldReturnSuccess_WhenAllDataIsValid

        /// <summary>
        /// TC-UMR-08: Process_ShouldReturnSuccess_WhenAllDataIsValid
        /// Integration test: Call Process() with valid request and verify response code is APP_MESSAGE_2006.
        /// </summary>
        [Fact]
        public async Task TC_UMR_08_Process_ShouldReturnSuccess_WhenAllDataIsValid()
        {
            // Arrange
            var record = CreateAndPersistTestMedicalRecordWithStrabismus();
            SetupHttpContext(_userId);
            SetupDoctorRepository(record.Doctor);
            SetupMedicalRecordRepository(record);

            var request = CreateValidRequest();
            request.RightEyeFundusRetinaVessel = new UpdateEyeFundusRetinaVesselData
            {
                RetinalEdema = true,
                Detachment = true
            };

            // Act
            var result = await _service.Process(_recordId, request);

            // Assert
            result.Should().NotBeNull("Response should not be null");
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2006.ToString(),
                "Success response should have APP_MESSAGE_2006 code");
            result.Data.Should().NotBeNull("Data should not be null on success");
            result.Data!.IsSuccess.Should().BeTrue("Response.IsSuccess should be true");
        }

        #endregion

        #region Additional Comprehensive Tests

        /// <summary>
        /// TC-UMR-09: UpdateEyeVitreous_WithStatus_ShouldMapCorrectly
        /// Test that vitreous status "Sạch" sets VitreousClear to true.
        /// </summary>
        [Theory]
        [InlineData("Sạch", true, false)]
        [InlineData("Đục", false, true)]
        public void TC_UMR_09_UpdateEyeVitreous_ShouldMapStatusCorrectly(string status, bool expectedClear, bool expectedOpacity)
        {
            // Arrange
            var record = CreateAndPersistTestMedicalRecord();
            var request = CreateValidRequest();
            request.RightEyeVitreous = new UpdateEyeVitreousData
            {
                Status = status
            };

            // Act
            InvokeUpdateEyeExaminations(_service, request, record);

            // Assert
            var rightLens = record.EyeLensVitreouses.First(e => e.Side == EyeSide.RIGHT);
            rightLens.VitreousClear.Should().Be(expectedClear);
            rightLens.VitreousOpacity.Should().Be(expectedOpacity);
        }

        /// <summary>
        /// TC-UMR-10: UpdateMultipleEyeExaminations_ShouldMapAllCorrectly
        /// Test that multiple eye examinations in one request all map correctly.
        /// </summary>
        [Fact]
        public void TC_UMR_10_UpdateMultipleEyeExaminations_ShouldMapAllCorrectly()
        {
            // Arrange
            var record = CreateAndPersistTestMedicalRecord();
            var request = CreateValidRequest();

            // Right Eye
            request.RightEyeBasic = new UpdateEyeBasicExamData { VaUncorrected = "20/20" };
            request.RightEyeCornea = new UpdateEyeCorneaExamData { Clarity = "Trong", Ulcer = true };
            request.RightEyeLens = new UpdateEyeLensData { Status = "Trong" };
            request.RightEyeSclera = new UpdateEyeScleraExamData { Status = "Bình thường" };
            request.RightEyeFundusRetinaVessel = new UpdateEyeFundusRetinaVesselData { RetinalEdema = true };
            request.RightEyeFundusDiscMacula = new UpdateEyeFundusDiscMaculaData { DiscStatus = "Bình thường" };

            // Left Eye
            request.LeftEyeBasic = new UpdateEyeBasicExamData { VaUncorrected = "20/40" };
            request.LeftEyeCornea = new UpdateEyeCorneaExamData { Clarity = "Phù", Ulcer = false };
            request.LeftEyeLens = new UpdateEyeLensData { Status = "Đục" };
            request.LeftEyeSclera = new UpdateEyeScleraExamData { Status = "Bình thường" };
            request.LeftEyeFundusRetinaVessel = new UpdateEyeFundusRetinaVesselData { RetinalEdema = false };
            request.LeftEyeFundusDiscMacula = new UpdateEyeFundusDiscMaculaData { DiscStatus = "Phù" };

            // Act
            InvokeUpdateEyeExaminations(_service, request, record);

            // Assert - Right Eye
            record.EyeCorneas.First(e => e.Side == EyeSide.RIGHT).Ulcer.Should().BeTrue();
            record.EyeLensVitreouses.First(e => e.Side == EyeSide.RIGHT).LensClear.Should().BeTrue();
            record.EyeScleras.First(e => e.Side == EyeSide.RIGHT).ScleraNormal.Should().BeTrue();
            record.EyeFundusRetinaVessels.First(e => e.Side == EyeSide.RIGHT).RetinaEdema.Should().BeTrue();
            record.EyeFundusDiscMaculas.First(e => e.Side == EyeSide.RIGHT).OpticDiscNormal.Should().BeTrue();

            // Assert - Left Eye
            record.EyeCorneas.First(e => e.Side == EyeSide.LEFT).Ulcer.Should().BeFalse();
            record.EyeLensVitreouses.First(e => e.Side == EyeSide.LEFT).LensClear.Should().BeFalse();
            record.EyeScleras.First(e => e.Side == EyeSide.LEFT).ScleraNormal.Should().BeTrue();
            record.EyeFundusRetinaVessels.First(e => e.Side == EyeSide.LEFT).RetinaEdema.Should().BeFalse();
            record.EyeFundusDiscMaculas.First(e => e.Side == EyeSide.LEFT).OpticDiscEdema.Should().BeTrue();
        }

        /// <summary>
        /// TC-UMR-11: Process_ShouldReturnError_WhenRecordNotFound
        /// Test that Process returns error when medical record is not found.
        /// </summary>
        [Fact]
        public async Task TC_UMR_11_Process_ShouldReturnError_WhenRecordNotFound()
        {
            // Arrange
            SetupHttpContext(_userId);
            SetupDoctorRepository(new DoctorProfile { Id = _doctorId, UserId = _userId, IsActive = true });
            SetupMedicalRecordRepository(null!);

            var request = CreateValidRequest();

            // Act
            var result = await _service.Process(Guid.NewGuid(), request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4028.ToString(),
                "Should return APP_MESSAGE_4028 when record not found");
        }

        /// <summary>
        /// TC-UMR-12: Process_ShouldReturnError_WhenDoctorNotAuthorized
        /// Test that Process returns error when doctor is not the creator of the record.
        /// </summary>
        [Fact]
        public async Task TC_UMR_12_Process_ShouldReturnError_WhenDoctorNotAuthorized()
        {
            // Arrange
            var record = CreateAndPersistTestMedicalRecordWithStrabismus();
            record.DoctorId = Guid.NewGuid(); // Different doctor ID

            SetupHttpContext(_userId);
            SetupDoctorRepository(record.Doctor);
            SetupMedicalRecordRepository(record);

            var request = CreateValidRequest();

            // Act
            var result = await _service.Process(_recordId, request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4014.ToString(),
                "Should return APP_MESSAGE_4014 when doctor is not authorized");
        }

        /// <summary>
        /// TC-UMR-13: Process_ShouldReturnError_WhenRecordIsLocked
        /// Test that Process returns error when medical record is locked.
        /// </summary>
        [Fact]
        public async Task TC_UMR_13_Process_ShouldReturnError_WhenRecordIsLocked()
        {
            // Arrange
            var record = CreateAndPersistTestMedicalRecordWithStrabismus();
            record.IsLocked = true; // Locked record

            SetupHttpContext(_userId);
            SetupDoctorRepository(record.Doctor);
            SetupMedicalRecordRepository(record);

            var request = CreateValidRequest();

            // Act
            var result = await _service.Process(_recordId, request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4014.ToString(),
                "Should return APP_MESSAGE_4014 when record is locked");
        }

        /// <summary>
        /// TC-UMR-14: Process_ShouldReturnError_WhenHttpContextInvalid
        /// Test that Process returns error when user ID cannot be extracted from context.
        /// </summary>
        [Fact]
        public async Task TC_UMR_14_Process_ShouldReturnError_WhenHttpContextInvalid()
        {
            // Arrange
            var record = CreateAndPersistTestMedicalRecordWithStrabismus();
            SetupHttpContext(Guid.Empty); // Invalid user ID
            SetupDoctorRepository(record.Doctor);
            SetupMedicalRecordRepository(record);

            var request = CreateValidRequest();

            // Act
            var result = await _service.Process(_recordId, request);

            // Assert
            result.Should().NotBeNull();
            result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_4033.ToString(),
                "Should return APP_MESSAGE_4033 when user ID is invalid");
        }

        #endregion
    }
}
