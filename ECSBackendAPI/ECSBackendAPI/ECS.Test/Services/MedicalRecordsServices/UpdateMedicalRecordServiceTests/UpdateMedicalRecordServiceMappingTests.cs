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
    /// Comprehensive unit tests for UpdateMedicalRecordService mapping methods.
    /// Each test covers ONE DTO field → verifies it maps to the correct entity property.
    /// 
    /// This test suite follows the principle: if a DTO field exists, it MUST map to an entity field.
    /// Tests that FAIL indicate: (1) field not mapped at all, (2) mapped to wrong entity field,
    /// (3) logic bug in conditional mapping.
    /// 
    /// Vấn đề cần debug: Request gửi lên BE đúng, nhưng response trả về không thấy update
    /// → Nguyên nhân: mapping bị sai tên field HOẶC logic điều kiện bỏ qua field
    /// </summary>
    public class UpdateMedicalRecordServiceMappingTests : IDisposable
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

        public UpdateMedicalRecordServiceMappingTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_Mapping_{Guid.NewGuid()}")
                .Options;
            _dbContext = new AppDbContext(options);

            _medicalRecordRepoMock = new Mock<IRepositoryBaseAsync<MedicalRecord, Guid, AppDbContext>>();
            _doctorRepoMock = new Mock<IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            // Use ServiceCollection instead of complex mocks — clean and correct
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

        #region Helper Methods

        private MedicalRecord CreateTestRecord()
        {
            var user = new User { Id = _userId, Email = "doc@ecs.vn", FullName = "Dr. Test", Phone = "0912345678", PasswordHash = "dummy_hash_for_test" };
            var doctor = new DoctorProfile { Id = _doctorId, UserId = _userId, ClinicId = _clinicId, IsActive = true, User = user };
            var patient = new PatientProfile
            {
                Id = Guid.NewGuid(),
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
                RecordType = RecordType.MS23_FUNDUS,
                IsLocked = false,
                EyeExamBasics = new List<EyeExamBasic>(),
                EyeEyelidConjunctivae = new List<EyeEyelidConjunctiva>(),
                EyeCorneas = new List<EyeCornea>(),
                EyeAcIrises = new List<EyeAcIris>(),
                EyeLensVitreouses = new List<EyeLensVitreous>(),
                EyeScleras = new List<EyeSclera>(),
                EyeFundusDiscMaculas = new List<EyeFundusDiscMacula>(),
                EyeFundusRetinaVessels = new List<EyeFundusRetinaVessel>()
            };

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
        /// Creates and PERSISTS a test record into the in-memory database.
        /// CRITICAL: The service uses GetDbContext().Set<T>().First() to re-fetch entities.
        /// If entities are NOT persisted, GetDbContext() returns empty → "Sequence contains no elements".
        /// </summary>
        private MedicalRecord CreateAndPersistTestRecord()
        {
            var record = CreateTestRecord();
            _dbContext.Users.Add(record.Doctor!.User!);
            _dbContext.DoctorProfiles.Add(record.Doctor);
            _dbContext.PatientProfiles.Add(record.Patient!);
            _dbContext.MedicalRecords.Add(record);
            _dbContext.SaveChanges();
            return record;
        }

        private void InvokeUpdateEyeExaminations(UpdateMedicalRecordService service, UpdateMedicalRecordRequest request, MedicalRecord record)
        {
            var method = typeof(UpdateMedicalRecordService)
                .GetMethod("UpdateEyeExaminations", BindingFlags.NonPublic | BindingFlags.Instance);
            method!.Invoke(service, new object[] { record, request });
        }

        private UpdateMedicalRecordRequest CreateBaseRequest() => new UpdateMedicalRecordRequest();

        #endregion

        #region SECTION 1: EyeBasicExam Mapping Tests

        /// <summary>
        /// TC-EB-01: VaUncorrected (string) → VaUncorrected (decimal?)
        /// DTO field: string?, Entity field: decimal?
        /// </summary>
        [Theory]
        [InlineData("20/20", 20.0)]
        [InlineData("20/40", 10.0)]
        [InlineData("1.0", 1.0)]
        public void TC_EB_01_VaUncorrected_ShouldMapToDecimal(string dtoValue, decimal expected)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeBasic = new UpdateEyeBasicExamData { VaUncorrected = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeExamBasics.First(e => e.Side == EyeSide.RIGHT);
            entity.VaUncorrected.Should().Be(expected, $"VaUncorrected='{dtoValue}' should map to {expected}");
        }

        /// <summary>
        /// TC-EB-02: VaCorrected (string) → VaCorrected (decimal?)
        /// </summary>
        [Theory]
        [InlineData("20/20", 20.0)]
        [InlineData("20/200", 2.0)]
        public void TC_EB_02_VaCorrected_ShouldMapToDecimal(string dtoValue, decimal expected)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeBasic = new UpdateEyeBasicExamData { VaCorrected = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeExamBasics.First(e => e.Side == EyeSide.RIGHT);
            entity.VaCorrected.Should().Be(expected, $"VaCorrected='{dtoValue}'");
        }

        /// <summary>
        /// TC-EB-03: IopMmhg (string) → IopMmhg (decimal?)
        /// </summary>
        [Theory]
        [InlineData("15", 15.0)]
        [InlineData("21", 21.0)]
        public void TC_EB_03_IopMmhg_ShouldMapToDecimal(string dtoValue, decimal expected)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeBasic = new UpdateEyeBasicExamData { IopMmhg = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeExamBasics.First(e => e.Side == EyeSide.RIGHT);
            entity.IopMmhg.Should().Be(expected, $"IopMmhg='{dtoValue}'");
        }

        /// <summary>
        /// TC-EB-04: IopMethod (string) → IopMethod (string?)
        /// </summary>
        [Theory]
        [InlineData("Maclakov")]
        [InlineData("Goldmann")]
        public void TC_EB_04_IopMethod_ShouldMap(string dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeBasic = new UpdateEyeBasicExamData { IopMethod = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeExamBasics.First(e => e.Side == EyeSide.RIGHT);
            entity.IopMethod.Should().Be(dtoValue, $"IopMethod should be '{dtoValue}'");
        }

        /// <summary>
        /// TC-EB-05: EomStatus = "Bình thường" → EomNormal = true
        /// </summary>
        [Theory]
        [InlineData("Bình thường", true)]
        [InlineData("", true)]
        [InlineData("Hạn chế", false)]
        public void TC_EB_05_EomStatus_ShouldMapToEomNormal(string dtoValue, bool expected)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeBasic = new UpdateEyeBasicExamData { EomStatus = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeExamBasics.First(e => e.Side == EyeSide.RIGHT);
            entity.EomNormal.Should().Be(expected, $"EomStatus='{dtoValue}' → EomNormal should be {expected}");
        }

        /// <summary>
        /// TC-EB-06: Nystagmus (string) → Nystagmus (bool)
        /// "Có" → true, "Không" → false
        /// </summary>
        [Theory]
        [InlineData("Có", true)]
        [InlineData("Không", false)]
        [InlineData("", false)]
        public void TC_EB_06_Nystagmus_ShouldMapToBool(string dtoValue, bool expected)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeBasic = new UpdateEyeBasicExamData { Nystagmus = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeExamBasics.First(e => e.Side == EyeSide.RIGHT);
            entity.Nystagmus.Should().Be(expected, $"Nystagmus='{dtoValue}'");
        }

        /// <summary>
        /// TC-EB-07: VisualField (string) → VisualField (string?)
        /// </summary>
        [Theory]
        [InlineData("Bình thường")]
        [InlineData("Khoét thị trường")]
        public void TC_EB_07_VisualField_ShouldMap(string dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeBasic = new UpdateEyeBasicExamData { VisualField = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeExamBasics.First(e => e.Side == EyeSide.RIGHT);
            entity.VisualField.Should().Be(dtoValue);
        }

        #endregion

        #region SECTION 2: Eyelid Mapping Tests

        /// <summary>
        /// TC-EL-01: Status = "Bình thường" → EyelidNormal = true
        /// </summary>
        [Theory]
        [InlineData("Bình thường", true)]
        [InlineData("", true)]
        [InlineData("Phù mi", false)]
        [InlineData("Tụ máu mi", false)]
        public void TC_EL_01_Status_ShouldMapToEyelidNormal(string dtoValue, bool expected)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeEyelid = new UpdateEyeEyelidData { Status = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeEyelidConjunctivae.First(e => e.Side == EyeSide.RIGHT);
            entity.EyelidNormal.Should().Be(expected, $"Status='{dtoValue}'");
        }

        /// <summary>
        /// TC-EL-02: LacrimalDuctStatus = "Bình thường" → LacrimalDuctNormal = true
        /// </summary>
        [Theory]
        [InlineData("Bình thường", true, false)]
        [InlineData("Đứt lệ quản", false, true)]
        public void TC_EL_02_LacrimalDuctStatus_ShouldMap(string dtoValue, bool expectedNormal, bool expectedCut)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeEyelid = new UpdateEyeEyelidData { LacrimalDuctStatus = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeEyelidConjunctivae.First(e => e.Side == EyeSide.RIGHT);
            entity.LacrimalDuctNormal.Should().Be(expectedNormal);
            entity.LacrimalDuctCut.Should().Be(expectedCut);
        }

        /// <summary>
        /// TC-EL-03: Ptosis (bool) → Ptosis (bool)
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TC_EL_03_Ptosis_ShouldMap(bool dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeEyelid = new UpdateEyeEyelidData { Ptosis = dtoValue, PtosisDegree = "2mm" };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeEyelidConjunctivae.First(e => e.Side == EyeSide.RIGHT);
            entity.Ptosis.Should().Be(dtoValue);
            entity.PtosisDegree.Should().Be("2mm");
        }

        /// <summary>
        /// TC-EL-04: LacrimalDuctLocation (string) → LacrimalDuctCutLocation (string?)
        /// </summary>
        [Theory]
        [InlineData("1/3 ngoài")]
        [InlineData("1/3 giữa")]
        public void TC_EL_04_LacrimalDuctLocation_ShouldMap(string dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeEyelid = new UpdateEyeEyelidData
            {
                LacrimalDuctStatus = "Đứt lệ quản",
                LacrimalDuctLocation = dtoValue
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeEyelidConjunctivae.First(e => e.Side == EyeSide.RIGHT);
            entity.LacrimalDuctCutLocation.Should().Be(dtoValue);
        }

        #endregion

        #region SECTION 3: Conjunctiva Mapping Tests

        /// <summary>
        /// TC-CJ-01: Status = "Bình thường" → ConjunctivaNormal = true
        /// </summary>
        [Theory]
        [InlineData("Bình thường", true)]
        [InlineData("", true)]
        [InlineData("Xung huyết", false)]
        public void TC_CJ_01_Status_ShouldMapToConjunctivaNormal(string dtoValue, bool expected)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeConjunctiva = new UpdateEyeConjunctivaData { Status = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeEyelidConjunctivae.First(e => e.Side == EyeSide.RIGHT);
            entity.ConjunctivaNormal.Should().Be(expected);
        }

        /// <summary>
        /// TC-CJ-02: CongestionType (string) → ConjunctivaCongestionType (string?)
        /// </summary>
        [Theory]
        [InlineData("Toả lan")]
        [InlineData("Ở nhãn cầu")]
        public void TC_CJ_02_CongestionType_ShouldMap(string dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeConjunctiva = new UpdateEyeConjunctivaData { CongestionType = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeEyelidConjunctivae.First(e => e.Side == EyeSide.RIGHT);
            entity.ConjunctivaCongestionType.Should().Be(dtoValue);
        }

        /// <summary>
        /// TC-CJ-03: Hemorrhage (bool) → ConjunctivaHemorrhage (bool)
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TC_CJ_03_Hemorrhage_ShouldMap(bool dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeConjunctiva = new UpdateEyeConjunctivaData
            {
                Hemorrhage = dtoValue,
                HemorrhageDescription = "Dưới kết mạc"
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeEyelidConjunctivae.First(e => e.Side == EyeSide.RIGHT);
            entity.ConjunctivaHemorrhage.Should().Be(dtoValue);
            entity.ConjunctivaHemorrhageLocation.Should().Be("Dưới kết mạc");
        }

        /// <summary>
        /// TC-CJ-04: Pterygium (bool) + Location + Size → entity fields
        /// </summary>
        [Fact]
        public void TC_CJ_04_Pterygium_ShouldMapAllFields()
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeConjunctiva = new UpdateEyeConjunctivaData
            {
                Pterygium = true,
                PterygiumLocation = "Mũi",
                PterygiumSize = "≤1/3"
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeEyelidConjunctivae.First(e => e.Side == EyeSide.RIGHT);
            entity.Pterygium.Should().BeTrue();
            entity.PterygiumLocation.Should().Be("Mũi");
            entity.PterygiumSize.Should().Be("≤1/3");
        }

        /// <summary>
        /// TC-CJ-05: FornixStatus (string) → FornixStatus (string?)
        /// </summary>
        [Theory]
        [InlineData("Bình thường")]
        [InlineData("Cạn")]
        [InlineData("Dính")]
        public void TC_CJ_05_FornixStatus_ShouldMap(string dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeConjunctiva = new UpdateEyeConjunctivaData
            {
                FornixStatus = dtoValue,
                SymblepharonHeight = "5mm",
                SymblepharonWidth = "3mm"
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeEyelidConjunctivae.First(e => e.Side == EyeSide.RIGHT);
            entity.FornixStatus.Should().Be(dtoValue);
            entity.SymblepharonHeight.Should().Be("5mm");
            entity.SymblepharonWidth.Should().Be("3mm");
        }

        #endregion

        #region SECTION 4: Cornea Mapping Tests

        /// <summary>
        /// TC-CR-01: Clarity (string) → Clarity (string?)
        /// </summary>
        [Theory]
        [InlineData("Trong")]
        [InlineData("Phù")]
        [InlineData("Sẹo")]
        public void TC_CR_01_Clarity_ShouldMap(string dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeCornea = new UpdateEyeCorneaExamData { Clarity = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeCorneas.First(e => e.Side == EyeSide.RIGHT);
            entity.Clarity.Should().Be(dtoValue);
        }

        /// <summary>
        /// TC-CR-02: Ulcer (bool) → Ulcer (bool)
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TC_CR_02_Ulcer_ShouldMap(bool dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeCornea = new UpdateEyeCorneaExamData
            {
                Ulcer = dtoValue,
                UlcerLocation = "Trung tâm",
                UlcerSize = "3mm"
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeCorneas.First(e => e.Side == EyeSide.RIGHT);
            entity.Ulcer.Should().Be(dtoValue);
            entity.UlcerLocation.Should().Be("Trung tâm");
            entity.UlcerSize.Should().Be("3mm");
        }

        /// <summary>
        /// TC-CR-03: Laceration (bool) + Sutured → ScleraLaceration (bool)
        /// NOTE: Cornea entity has Laceration field, not ScleraLaceration
        /// </summary>
        [Fact]
        public void TC_CR_03_Laceration_ShouldMap()
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeCornea = new UpdateEyeCorneaExamData
            {
                Laceration = true,
                LacerationSutured = true,
                LacerationLocation = "1/3 trên",
                LacerationSize = "10mm",
                LacerationType = "Rách gọn"
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeCorneas.First(e => e.Side == EyeSide.RIGHT);
            entity.Laceration.Should().BeTrue();
            entity.LacerationSutured.Should().BeTrue();
            entity.LacerationLocation.Should().Be("1/3 trên");
            entity.LacerationSize.Should().Be("10mm");
            entity.LacerationType.Should().Be("Rách gọn");
        }

        /// <summary>
        /// TC-CR-04: Perforation (bool) + Diameter + Location + SeidelTest
        /// </summary>
        [Fact]
        public void TC_CR_04_Perforation_ShouldMapAllFields()
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeCornea = new UpdateEyeCorneaExamData
            {
                Perforation = true,
                PerforationDiameterMm = 3.5m,
                PerforationLocation = "Trung tâm",
                SeidelTest = "Không bít"
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeCorneas.First(e => e.Side == EyeSide.RIGHT);
            entity.Perforation.Should().BeTrue();
            entity.PerforationDiameterMm.Should().Be(3.5m);
            entity.PerforationLocation.Should().Be("Trung tâm");
            entity.SeidelTest.Should().Be("Không bít");
        }

        /// <summary>
        /// TC-CR-05: Neovascularization (bool) + Depth + Extent
        /// </summary>
        [Fact]
        public void TC_CR_05_Neovascularization_ShouldMapAllFields()
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeCornea = new UpdateEyeCorneaExamData
            {
                Neovascularization = true,
                NeovascularizationDepth = "Nông",
                NeovascularizationExtent = "≤1/3"
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeCorneas.First(e => e.Side == EyeSide.RIGHT);
            entity.Neovascularization.Should().BeTrue();
            entity.NeovascularizationLocation.Should().Be("Nông");
            entity.NeovascularizationExtent.Should().Be("≤1/3");
        }

        /// <summary>
        /// TC-CR-06: DiameterMm (decimal) → DiameterMm (decimal?)
        /// </summary>
        [Theory]
        [InlineData(11.5)]
        [InlineData(12.0)]
        public void TC_CR_06_DiameterMm_ShouldMap(decimal dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeCornea = new UpdateEyeCorneaExamData { DiameterMm = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeCorneas.First(e => e.Side == EyeSide.RIGHT);
            entity.DiameterMm.Should().Be(dtoValue);
        }

        /// <summary>
        /// TC-CR-07: Shape (string) → Shape (string?)
        /// </summary>
        [Theory]
        [InlineData("Bình thường")]
        [InlineData("Nón")]
        public void TC_CR_07_Shape_ShouldMap(string dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeCornea = new UpdateEyeCorneaExamData { Shape = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeCorneas.First(e => e.Side == EyeSide.RIGHT);
            entity.Shape.Should().Be(dtoValue);
        }

        /// <summary>
        /// TC-CR-08: Sensation (string) → Sensation (string?)
        /// </summary>
        [Theory]
        [InlineData("Mất")]
        [InlineData("Giảm")]
        [InlineData("Bình thường")]
        public void TC_CR_08_Sensation_ShouldMap(string dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeCornea = new UpdateEyeCorneaExamData { Sensation = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeCorneas.First(e => e.Side == EyeSide.RIGHT);
            entity.Sensation.Should().Be(dtoValue);
        }

        #endregion

        #region SECTION 5: AnteriorChamber Mapping Tests

        /// <summary>
        /// TC-AC-01: Depth = "Xẹp tiền phòng" → AcFlat = true
        /// </summary>
        [Fact]
        public void TC_AC_01_DepthXep_ShouldSetAcFlat()
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeAnteriorChamber = new UpdateEyeAnteriorChamberData
            {
                Depth = "Xẹp tiền phòng"
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeAcIrises.First(e => e.Side == EyeSide.RIGHT);
            entity.AcFlat.Should().BeTrue("Depth='Xẹp tiền phòng' should set AcFlat=true");
            entity.AcDepthMm.Should().BeNull("AcDepthMm should be null when Xẹp");
        }

        /// <summary>
        /// TC-AC-02: Depth = "Sâu" → AcFlat = false
        /// </summary>
        [Fact]
        public void TC_AC_02_DepthSau_ShouldSetAcFlatFalse()
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeAnteriorChamber = new UpdateEyeAnteriorChamberData
            {
                Depth = "Sâu",
                DepthMm = 4.5m
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeAcIrises.First(e => e.Side == EyeSide.RIGHT);
            entity.AcFlat.Should().BeFalse();
            entity.AcDepthMm.Should().Be(4.5m);
        }

        /// <summary>
        /// TC-AC-03: VitreousInAC (bool) → AcLensMaterial (bool)
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TC_AC_03_VitreousInAC_ShouldMap(bool dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeAnteriorChamber = new UpdateEyeAnteriorChamberData { VitreousInAC = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeAcIrises.First(e => e.Side == EyeSide.RIGHT);
            entity.AcLensMaterial.Should().Be(dtoValue);
        }

        /// <summary>
        /// TC-AC-04: Pus (bool) + PusMm → AcPus, AcPusMm
        /// </summary>
        [Fact]
        public void TC_AC_04_Pus_ShouldMapAllFields()
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeAnteriorChamber = new UpdateEyeAnteriorChamberData
            {
                Pus = true,
                PusMm = 2.5m
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeAcIrises.First(e => e.Side == EyeSide.RIGHT);
            entity.AcPus.Should().BeTrue();
            entity.AcPusMm.Should().Be(2.5m);
        }

        /// <summary>
        /// TC-AC-05: HerickClassification (string) → AcDepthHerick (string?)
        /// </summary>
        [Theory]
        [InlineData("<1/4 GM")]
        [InlineData("1/4-1/2")]
        [InlineData("≥1/2 GM")]
        public void TC_AC_05_HerickClassification_ShouldMap(string dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeAnteriorChamber = new UpdateEyeAnteriorChamberData { HerickClassification = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeAcIrises.First(e => e.Side == EyeSide.RIGHT);
            entity.AcDepthHerick.Should().Be(dtoValue);
        }

        /// <summary>
        /// TC-AC-06: Tyndall (string) → AcTyndall (string?)
        /// </summary>
        [Theory]
        [InlineData("(-)")]
        [InlineData("(+)")]
        [InlineData("(++)")]
        public void TC_AC_06_Tyndall_ShouldMap(string dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeAnteriorChamber = new UpdateEyeAnteriorChamberData { Tyndall = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeAcIrises.First(e => e.Side == EyeSide.RIGHT);
            entity.AcTyndall.Should().Be(dtoValue);
        }

        /// <summary>
        /// TC-AC-07: Hemorrhage (bool) + HemorrhageLevel (string)
        /// </summary>
        [Fact]
        public void TC_AC_07_Hemorrhage_ShouldMapAllFields()
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeAnteriorChamber = new UpdateEyeAnteriorChamberData
            {
                Hemorrhage = true,
                HemorrhageLevel = "1/3 tiền phòng"
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeAcIrises.First(e => e.Side == EyeSide.RIGHT);
            entity.AcHemorrhage.Should().BeTrue();
            entity.AcHemorrhageLevel.Should().Be("1/3 tiền phòng");
        }

        #endregion

        #region SECTION 6: IrisPupil Mapping Tests

        /// <summary>
        /// TC-IP-01: IrisColor (string) → IrisColor (string?)
        /// </summary>
        [Theory]
        [InlineData("Nâu")]
        [InlineData("Xanh")]
        public void TC_IP_01_IrisColor_ShouldMap(string dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeIrisPupil = new UpdateEyeIrisPupilData { IrisColor = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeAcIrises.First(e => e.Side == EyeSide.RIGHT);
            entity.IrisColor.Should().Be(dtoValue);
        }

        /// <summary>
        /// TC-IP-02: IrisCondition = "Thoái hóa" → IrisDegeneration = true
        /// </summary>
        [Theory]
        [InlineData("Thoái hóa", true)]
        [InlineData("Bình thường", false)]
        public void TC_IP_02_IrisCondition_ShouldSetDegeneration(string dtoValue, bool expected)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeIrisPupil = new UpdateEyeIrisPupilData { IrisCondition = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeAcIrises.First(e => e.Side == EyeSide.RIGHT);
            entity.IrisCondition.Should().Be(dtoValue);
            entity.IrisDegeneration.Should().Be(expected);
        }

        /// <summary>
        /// TC-IP-03: PupilShape = "Tròn" → PupilRound = true, PupilIrregular = false
        /// PupilShape = "Méo" → PupilRound = false, PupilIrregular = true
        /// </summary>
        [Theory]
        [InlineData("Tròn", true, false)]
        [InlineData("Méo", false, true)]
        [InlineData("Dính", false, false)]
        public void TC_IP_03_PupilShape_ShouldMapCorrectly(string dtoValue, bool expectedRound, bool expectedIrregular)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeIrisPupil = new UpdateEyeIrisPupilData
            {
                PupilShape = dtoValue,
                PupilPosition = dtoValue == "Dính" ? "Mũi" : null
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeAcIrises.First(e => e.Side == EyeSide.RIGHT);
            entity.PupilRound.Should().Be(expectedRound, $"PupilShape='{dtoValue}'");
            entity.PupilIrregular.Should().Be(expectedIrregular);
            if (dtoValue == "Dính")
                entity.PupilSychiae.Should().BeTrue();
        }

        /// <summary>
        /// TC-IP-04: PupilReflex = "Mất" → PupilParalyzed = true
        /// </summary>
        [Theory]
        [InlineData("Tốt", false)]
        [InlineData("Kém", false)]
        [InlineData("Mất", true)]
        public void TC_IP_04_PupilReflex_ShouldMap(string dtoValue, bool expectedParalyzed)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeIrisPupil = new UpdateEyeIrisPupilData { PupilReflex = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeAcIrises.First(e => e.Side == EyeSide.RIGHT);
            entity.PupilReflex.Should().Be(dtoValue);
            entity.PupilLightReflex.Should().Be(dtoValue);
            entity.PupilParalyzed.Should().Be(expectedParalyzed);
        }

        /// <summary>
        /// TC-IP-05: PupilDiameterMm (decimal) → PupilDiameterMm (decimal?)
        /// </summary>
        [Theory]
        [InlineData(3.0)]
        [InlineData(5.0)]
        public void TC_IP_05_PupilDiameterMm_ShouldMap(decimal dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeIrisPupil = new UpdateEyeIrisPupilData { PupilDiameterMm = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeAcIrises.First(e => e.Side == EyeSide.RIGHT);
            entity.PupilDiameterMm.Should().Be(dtoValue);
        }

        /// <summary>
        /// TC-IP-06: FundusReflex (string) → FundusReflex (string?)
        /// </summary>
        [Theory]
        [InlineData("Hồng")]
        [InlineData("Xám")]
        [InlineData("Không soi được")]
        public void TC_IP_06_FundusReflex_ShouldMap(string dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeIrisPupil = new UpdateEyeIrisPupilData { FundusReflex = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeAcIrises.First(e => e.Side == EyeSide.RIGHT);
            entity.FundusReflex.Should().Be(dtoValue);
        }

        /// <summary>
        /// TC-IP-07: IrisNeovascularization (bool) → IrisNeovascularization (bool)
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TC_IP_07_IrisNeovascularization_ShouldMap(bool dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeIrisPupil = new UpdateEyeIrisPupilData { IrisNeovascularization = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeAcIrises.First(e => e.Side == EyeSide.RIGHT);
            entity.IrisNeovascularization.Should().Be(dtoValue);
        }

        /// <summary>
        /// TC-IP-08: KoeppeNodules + BusaccaNodules + IrisRootTear
        /// </summary>
        [Fact]
        public void TC_IP_08_NodulesAndTear_ShouldMap()
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeIrisPupil = new UpdateEyeIrisPupilData
            {
                KoeppeNodules = true,
                BusaccaNodules = false,
                IrisRootTear = true,
                IrisRootTearDegree = "180°"
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeAcIrises.First(e => e.Side == EyeSide.RIGHT);
            entity.IrisKoeppeNodules.Should().BeTrue();
            entity.IrisBusaccaNodules.Should().BeFalse();
            entity.IrisRootTear.Should().BeTrue();
            entity.IrisRootTearDegree.Should().Be("180°");
        }

        #endregion

        #region SECTION 7: Lens Mapping Tests

        /// <summary>
        /// TC-LN-01: Status = "Trong" → LensClear = true
        /// Status = "Đục" → LensClear = false
        /// </summary>
        [Theory]
        [InlineData("Trong", true)]
        [InlineData("Đục", false)]
        [InlineData("", true)]
        public void TC_LN_01_Status_ShouldMapToLensClear(string dtoValue, bool expected)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeLens = new UpdateEyeLensData { Status = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeLensVitreouses.First(e => e.Side == EyeSide.RIGHT);
            entity.LensClear.Should().Be(expected, $"Status='{dtoValue}'");
        }

        /// <summary>
        /// TC-LN-02: Status = "Vỡ" → LensRupture = true
        /// </summary>
        [Fact]
        public void TC_LN_02_StatusVo_ShouldSetLensRupture()
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeLens = new UpdateEyeLensData { Status = "Vỡ" };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeLensVitreouses.First(e => e.Side == EyeSide.RIGHT);
            entity.LensRupture.Should().BeTrue();
        }

        /// <summary>
        /// TC-LN-03: IolPresent (bool) + IolStatus + IolPosition
        /// </summary>
        [Fact]
        public void TC_LN_03_Iol_ShouldMapAllFields()
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeLens = new UpdateEyeLensData
            {
                IolPresent = true,
                IolStatus = "Cân",
                IolPosition = "Trong bao"
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeLensVitreouses.First(e => e.Side == EyeSide.RIGHT);
            entity.LensIolPresent.Should().BeTrue();
            entity.LensIolStatus.Should().Be("Cân");
            entity.LensIolPosition.Should().Be("Trong bao");
        }

        /// <summary>
        /// TC-LN-04: Subluxation + LensInAnterior + LensInVitreous
        /// </summary>
        [Fact]
        public void TC_LN_04_LensPosition_ShouldMap()
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeLens = new UpdateEyeLensData
            {
                Status = "Đục",
                Subluxation = true,
                LensInAnterior = false,
                LensInVitreous = true
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeLensVitreouses.First(e => e.Side == EyeSide.RIGHT);
            entity.LensClear.Should().BeFalse();
            entity.LensSubluxation.Should().BeTrue();
            entity.LensIntoAnterior.Should().BeFalse();
            entity.LensIntoVitreous.Should().BeTrue();
        }

        #endregion

        #region SECTION 8: Vitreous Mapping Tests

        /// <summary>
        /// TC-VT-01: Status = "Sạch" → VitreousClear = true
        /// Status = "Đục" → VitreousOpacity = true
        /// </summary>
        [Theory]
        [InlineData("Sạch", true, false)]
        [InlineData("Đục", false, true)]
        [InlineData("", true, false)]
        public void TC_VT_01_Status_ShouldMapToVitreousClear(string dtoValue, bool expectedClear, bool expectedOpacity)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeVitreous = new UpdateEyeVitreousData { Status = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeLensVitreouses.First(e => e.Side == EyeSide.RIGHT);
            entity.VitreousClear.Should().Be(expectedClear, $"Status='{dtoValue}'");
            entity.VitreousOpacity.Should().Be(expectedOpacity);
        }

        /// <summary>
        /// TC-VT-02: Hemorrhage (bool) → VitreousHemorrhage (bool)
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TC_VT_02_Hemorrhage_ShouldMap(bool dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeVitreous = new UpdateEyeVitreousData { Hemorrhage = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeLensVitreouses.First(e => e.Side == EyeSide.RIGHT);
            entity.VitreousHemorrhage.Should().Be(dtoValue);
        }

        /// <summary>
        /// TC-VT-03: Pvd (bool) → VitreousPvd (bool)
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TC_VT_03_Pvd_ShouldMap(bool dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeVitreous = new UpdateEyeVitreousData { Pvd = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeLensVitreouses.First(e => e.Side == EyeSide.RIGHT);
            entity.VitreousPvd.Should().Be(dtoValue);
        }

        /// <summary>
        /// TC-VT-04: Tyndall (string) → VitreousTyndall (string?)
        /// </summary>
        [Theory]
        [InlineData("(-)")]
        [InlineData("(+)")]
        public void TC_VT_04_Tyndall_ShouldMap(string dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeVitreous = new UpdateEyeVitreousData { Tyndall = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeLensVitreouses.First(e => e.Side == EyeSide.RIGHT);
            entity.VitreousTyndall.Should().Be(dtoValue);
        }

        /// <summary>
        /// TC-VT-05: Organized + Purulent + ForeignBody
        /// </summary>
        [Fact]
        public void TC_VT_05_ComplexVitreous_ShouldMap()
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeVitreous = new UpdateEyeVitreousData
            {
                Organized = true,
                Purulent = false,
                ForeignBody = false
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeLensVitreouses.First(e => e.Side == EyeSide.RIGHT);
            entity.VitreousOrganized.Should().BeTrue();
            entity.VitreousPurulent.Should().BeFalse();
            entity.VitreousForeignBody.Should().BeFalse();
        }

        #endregion

        #region SECTION 9: Sclera Mapping Tests

        /// <summary>
        /// TC-SC-01: Status = "Bình thường" → ScleraNormal = true
        /// Status = "Giãn lồi" → ScleraEctasia = true
        /// Status = "Sẹo" → OldSurgeryScar = true
        /// </summary>
        [Theory]
        [InlineData("Bình thường", true, false, false)]
        [InlineData("Giãn lồi", false, true, false)]
        [InlineData("Sẹo", false, false, true)]
        [InlineData("", true, false, false)]
        public void TC_SC_01_Status_ShouldMapCorrectly(string dtoValue, bool expectedNormal, bool expectedEctasia, bool expectedScar)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeSclera = new UpdateEyeScleraExamData { Status = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeScleras.First(e => e.Side == EyeSide.RIGHT);
            entity.ScleraNormal.Should().Be(expectedNormal, $"Status='{dtoValue}'");
            entity.ScleraEctasia.Should().Be(expectedEctasia);
            entity.OldSurgeryScar.Should().Be(expectedScar);
        }

        /// <summary>
        /// TC-SC-02: Laceration + Sutured + Location + Size
        /// </summary>
        [Fact]
        public void TC_SC_02_Laceration_ShouldMapAllFields()
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeSclera = new UpdateEyeScleraExamData
            {
                Laceration = true,
                LacerationSutured = true,
                LacerationLocation = "1/3 trên",
                LacerationSize = "8mm"
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeScleras.First(e => e.Side == EyeSide.RIGHT);
            entity.ScleraLaceration.Should().BeTrue();
            entity.ScleraLacerationSutured.Should().BeTrue();
            entity.ScleraLacerationLocation.Should().Be("1/3 trên");
            entity.ScleraLacerationSize.Should().Be("8mm");
        }

        /// <summary>
        /// TC-SC-03: TissueEntrapped (bool) → ScleraTissueEntrapped (bool)
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TC_SC_03_TissueEntrapped_ShouldMap(bool dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeSclera = new UpdateEyeScleraExamData { TissueEntrapped = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeScleras.First(e => e.Side == EyeSide.RIGHT);
            entity.ScleraTissueEntrapped.Should().Be(dtoValue);
        }

        #endregion

        #region SECTION 10: FundusDiscMacula Mapping Tests

        /// <summary>
        /// TC-FD-01: DiscStatus = "Phù" → OpticDiscNormal=false, OpticDiscEdema=true
        /// DiscStatus = "Teo" → OpticDiscAtrophy=true
        /// DiscStatus = "Bạc màu" → OpticDiscPallor=true
        /// </summary>
        [Theory]
        [InlineData("Phù", false, true, false, false)]
        [InlineData("Teo", false, false, true, false)]
        [InlineData("Bạc màu", false, false, false, true)]
        [InlineData("Bình thường", true, false, false, false)]
        [InlineData("", true, false, false, false)]
        public void TC_FD_01_DiscStatus_ShouldMapCorrectly(string dtoValue, bool expectedNormal, bool expectedEdema, bool expectedAtrophy, bool expectedPallor)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeFundusDiscMacula = new UpdateEyeFundusDiscMaculaData { DiscStatus = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeFundusDiscMaculas.First(e => e.Side == EyeSide.RIGHT);
            entity.OpticDiscNormal.Should().Be(expectedNormal, $"DiscStatus='{dtoValue}'");
            entity.OpticDiscEdema.Should().Be(expectedEdema);
            entity.OpticDiscAtrophy.Should().Be(expectedAtrophy);
            entity.OpticDiscPallor.Should().Be(expectedPallor);
        }

        /// <summary>
        /// TC-FD-02: CdRatio (string) → OpticDiscCupRatio (string?)
        /// </summary>
        [Theory]
        [InlineData("0.3")]
        [InlineData("0.5")]
        [InlineData("0.8")]
        public void TC_FD_02_CdRatio_ShouldMap(string dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeFundusDiscMacula = new UpdateEyeFundusDiscMaculaData { CdRatio = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeFundusDiscMaculas.First(e => e.Side == EyeSide.RIGHT);
            entity.OpticDiscCupRatio.Should().Be(dtoValue);
        }

        /// <summary>
        /// TC-FD-03: Neovascularization (bool) → OpticDiscNeovascularization (bool)
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TC_FD_03_Neovascularization_ShouldMap(bool dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeFundusDiscMacula = new UpdateEyeFundusDiscMaculaData { Neovascularization = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeFundusDiscMaculas.First(e => e.Side == EyeSide.RIGHT);
            entity.OpticDiscNeovascularization.Should().Be(dtoValue);
        }

        /// <summary>
        /// TC-FD-04: DiscHemorrhage (bool) → OpticDiscHemorrhage (bool)
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TC_FD_04_DiscHemorrhage_ShouldMap(bool dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeFundusDiscMacula = new UpdateEyeFundusDiscMaculaData { DiscHemorrhage = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeFundusDiscMaculas.First(e => e.Side == EyeSide.RIGHT);
            entity.OpticDiscHemorrhage.Should().Be(dtoValue);
        }

        /// <summary>
        /// TC-FD-05: MaculaStatus = "Phù" → MaculaNormal=false
        /// MaculaStatus = "Lõm teo" → MaculaCondition="Lõm teo"
        /// </summary>
        [Theory]
        [InlineData("Phù", false, "Phù")]
        [InlineData("Lõm teo", false, "Lõm teo")]
        [InlineData("Bình thường", true, "Bình thường")]
        [InlineData("", true, "")]
        public void TC_FD_05_MaculaStatus_ShouldMap(string dtoValue, bool expectedNormal, string expectedCondition)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeFundusDiscMacula = new UpdateEyeFundusDiscMaculaData { MaculaStatus = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeFundusDiscMaculas.First(e => e.Side == EyeSide.RIGHT);
            entity.MaculaNormal.Should().Be(expectedNormal, $"MaculaStatus='{dtoValue}'");
            entity.MaculaCondition.Should().Be(expectedCondition);
        }

        /// <summary>
        /// TC-FD-06: MaculaScar (bool) → MaculaScar (bool)
        /// SerousDetachment (bool) → MaculaSerousDetachment (bool)
        /// </summary>
        [Fact]
        public void TC_FD_06_MaculaScarAndSerous_ShouldMap()
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeFundusDiscMacula = new UpdateEyeFundusDiscMaculaData
            {
                MaculaScar = true,
                SerousDetachment = true
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeFundusDiscMaculas.First(e => e.Side == EyeSide.RIGHT);
            entity.MaculaScar.Should().BeTrue();
            entity.MaculaSerousDetachment.Should().BeTrue();
        }

        /// <summary>
        /// TC-FD-07: MaculaHoleDegree (string) → MaculaHoleDegree (string?)
        /// </summary>
        [Theory]
        [InlineData("Có")]
        [InlineData("Không")]
        public void TC_FD_07_MaculaHoleDegree_ShouldMap(string dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeFundusDiscMacula = new UpdateEyeFundusDiscMaculaData { MaculaHoleDegree = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeFundusDiscMaculas.First(e => e.Side == EyeSide.RIGHT);
            entity.MaculaHoleDegree.Should().Be(dtoValue);
        }

        /// <summary>
        /// TC-FD-08: ChoroidStatus = "Bình thường" → ChoroidalNormal = true
        /// </summary>
        [Theory]
        [InlineData("Bình thường", true)]
        [InlineData("Viêm", false)]
        [InlineData("Teo", false)]
        public void TC_FD_08_ChoroidStatus_ShouldMap(string dtoValue, bool expected)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeFundusDiscMacula = new UpdateEyeFundusDiscMaculaData
            {
                ChoroidStatus = dtoValue,
                ChoroidFindings = dtoValue == "Viêm" ? "Viêm hắc mạc lan tỏa" : null
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeFundusDiscMaculas.First(e => e.Side == EyeSide.RIGHT);
            entity.ChoroidalNormal.Should().Be(expected);
            if (dtoValue == "Viêm")
                entity.ChoroidalFindings.Should().Be("Viêm hắc mạc lan tỏa");
        }

        /// <summary>
        /// TC-FD-09: Chorioretinitis fields — BUG FOUND!
        /// UpdateEyeFundusDiscMaculaData DTO has: ChorioretinitisActive, ChorioretinitisScar, ChorioretinitisCount, ChorioretinitisLocation
        /// BUT EyeFundusDiscMacula entity does NOT have these fields.
        /// AND MapEyeFundusDiscMacula() does NOT map these fields.
        /// → DTO gửi lên sẽ BỊ MẤT, không lưu được vào DB.
        /// Fix: Add ChorioretinitisActive/Scar/Count/Location fields to EyeFundusDiscMacula entity.
        /// </summary>
        [Fact]
        public void TC_FD_09_Chorioretinitis_BUG_DtoFieldsNotMappedToEntity()
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeFundusDiscMacula = new UpdateEyeFundusDiscMaculaData
            {
                ChorioretinitisActive = true,
                ChorioretinitisScar = false,
                ChorioretinitisCount = 2,
                ChorioretinitisLocation = "Ngang đĩa thị"
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            // The entity does NOT have Chorioretinitis fields - this is a DESIGN BUG
            // The DTO fields exist but are NEVER persisted
            var entity = record.EyeFundusDiscMaculas.First(e => e.Side == EyeSide.RIGHT);
            
            // These assertions document the bug:
            // Chorioretinitis fields exist in DTO but NOT in entity
            // MapEyeFundusDiscMacula() never touches Chorioretinitis*
            // → The data sent in the request is LOST
            var dtoHasChorioretinitis = request.RightEyeFundusDiscMacula != null &&
                (request.RightEyeFundusDiscMacula.ChorioretinitisActive == true ||
                 request.RightEyeFundusDiscMacula.ChorioretinitisScar == false);
            
            // Entity does not have Chorioretinitis* fields
            var entityType = typeof(EyeFundusDiscMacula);
            var entityHasChorioretinitis = entityType.GetProperty("ChorioretinitisActive") != null;

            dtoHasChorioretinitis.Should().BeTrue("DTO has Chorioretinitis fields");
            entityHasChorioretinitis.Should().BeFalse("BUT entity does NOT have Chorioretinitis fields — THIS IS THE BUG");
        }

        #endregion

        #region SECTION 11: FundusRetinaVessel Mapping Tests

        /// <summary>
        /// TC-FR-01: VesselStatus = "Bình thường" → VesselNormal = true
        /// </summary>
        [Theory]
        [InlineData("Bình thường", true)]
        [InlineData("Tắc động mạch", false)]
        [InlineData("", true)]
        public void TC_FR_01_VesselStatus_ShouldMap(string dtoValue, bool expected)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeFundusRetinaVessel = new UpdateEyeFundusRetinaVesselData { VesselStatus = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeFundusRetinaVessels.First(e => e.Side == EyeSide.RIGHT);
            entity.VesselNormal.Should().Be(expected, $"VesselStatus='{dtoValue}'");
        }

        /// <summary>
        /// TC-FR-02: ArteryOcclusion (string) → ArteryOcclusionType (string?)
        /// VeinOcclusion (string) → VeinOcclusionType (string?)
        /// </summary>
        [Theory]
        [InlineData("Trung tâm")]
        [InlineData("Nhánh")]
        public void TC_FR_02_ArteryOcclusion_ShouldMap(string dtoValue)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeFundusRetinaVessel = new UpdateEyeFundusRetinaVesselData
            {
                ArteryOcclusion = dtoValue,
                VeinOcclusion = "Nhánh"
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeFundusRetinaVessels.First(e => e.Side == EyeSide.RIGHT);
            entity.ArteryOcclusionType.Should().Be(dtoValue);
            entity.VeinOcclusionType.Should().Be("Nhánh");
        }

        /// <summary>
        /// TC-FR-03: RetinaStatus = "Bình thường" → RetinaNormal = true
        /// </summary>
        [Theory]
        [InlineData("Bình thường", true)]
        [InlineData("Viêm mao mạch", false)]
        [InlineData("", true)]
        public void TC_FR_03_RetinaStatus_ShouldMap(string dtoValue, bool expected)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeFundusRetinaVessel = new UpdateEyeFundusRetinaVesselData { RetinaStatus = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeFundusRetinaVessels.First(e => e.Side == EyeSide.RIGHT);
            entity.RetinaNormal.Should().Be(expected, $"RetinaStatus='{dtoValue}'");
        }

        /// <summary>
        /// TC-FR-04: Detachment (bool) + DetachmentLevel (string)
        /// </summary>
        [Fact]
        public void TC_FR_04_Detachment_ShouldMapAllFields()
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeFundusRetinaVessel = new UpdateEyeFundusRetinaVesselData
            {
                Detachment = true,
                DetachmentLevel = "Toàn bộ"
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeFundusRetinaVessels.First(e => e.Side == EyeSide.RIGHT);
            entity.RetinalDetachment.Should().BeTrue();
            entity.RetinalDetachmentLevel.Should().Be("Toàn bộ");
        }

        /// <summary>
        /// TC-FR-05: RetinalTear (bool) + TearCount + TearLocation + TearMorphology
        /// </summary>
        [Fact]
        public void TC_FR_05_RetinalTear_ShouldMapAllFields()
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeFundusRetinaVessel = new UpdateEyeFundusRetinaVesselData
            {
                RetinalTear = true,
                TearCount = 2,
                TearLocation = "Rìa trên",
                TearMorphology = "Móng ngựa"
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeFundusRetinaVessels.First(e => e.Side == EyeSide.RIGHT);
            entity.RetinalTear.Should().BeTrue();
            entity.RetinalTearCount.Should().Be(2);
            entity.RetinalTearLocation.Should().Be("Rìa trên");
            entity.RetinalTearMorphology.Should().Be("Móng ngựa");
        }

        /// <summary>
        /// TC-FR-06: Iofb (bool) + IofbLocation + IofbSize
        /// </summary>
        [Fact]
        public void TC_FR_06_Iofb_ShouldMapAllFields()
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeFundusRetinaVessel = new UpdateEyeFundusRetinaVesselData
            {
                Iofb = true,
                IofbLocation = "Vitreous",
                IofbSize = "2mm"
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeFundusRetinaVessels.First(e => e.Side == EyeSide.RIGHT);
            entity.IntraocularForeignBody.Should().BeTrue();
            entity.IofbLocation.Should().Be("Vitreous");
            entity.IofbSize.Should().Be("2mm");
        }

        /// <summary>
        /// TC-FR-07: Degeneration (bool) + Type + Description
        /// </summary>
        [Theory]
        [InlineData(true, "chu biên")]
        [InlineData(true, "trung tâm")]
        public void TC_FR_07_Degeneration_ShouldMap(bool degeneration, string degenerationType)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeFundusRetinaVessel = new UpdateEyeFundusRetinaVesselData
            {
                Degeneration = degeneration,
                DegenerationType = degenerationType,
                DegenerationDescription = "Thoái hóa võng mạc chu biên"
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeFundusRetinaVessels.First(e => e.Side == EyeSide.RIGHT);
            if (degeneration && degenerationType == "chu biên")
                entity.RetinaDegenerationPeripheral.Should().BeTrue();
            if (degeneration && degenerationType == "trung tâm")
                entity.RetinaDegenerationCentral.Should().BeTrue();
            entity.DegenerativeType.Should().Be(degenerationType);
            entity.DegenerativeDescription.Should().Be("Thoái hóa võng mạc chu biên");
        }

        /// <summary>
        /// TC-FR-08: OcclusionType with "thiếu máu" → OcclusionIschemia = true
        /// </summary>
        [Theory]
        [InlineData("Phù", false)]
        [InlineData("Thiếu máu", true)]
        [InlineData("Hỗn hợp", false)]
        public void TC_FR_08_OcclusionType_ShouldMap(string dtoValue, bool expectedIschemia)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeFundusRetinaVessel = new UpdateEyeFundusRetinaVesselData
            {
                OcclusionType = dtoValue,
                RetinalEdema = dtoValue == "Phù"
            };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeFundusRetinaVessels.First(e => e.Side == EyeSide.RIGHT);
            entity.OcclusionType.Should().Be(dtoValue);
            entity.OcclusionIschemia.Should().Be(expectedIschemia);
        }

        /// <summary>
        /// TC-FR-09: ExudateType = "Cứng" → RetinaExudateHard = true
        /// ExudateType = "Dạng bông" → RetinaExudateCottonWool = true
        /// </summary>
        [Theory]
        [InlineData("Cứng", true, false)]
        [InlineData("Dạng bông", false, true)]
        public void TC_FR_09_ExudateType_ShouldMap(string dtoValue, bool expectedHard, bool expectedCotton)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeFundusRetinaVessel = new UpdateEyeFundusRetinaVesselData { ExudateType = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeFundusRetinaVessels.First(e => e.Side == EyeSide.RIGHT);
            entity.RetinaExudateHard.Should().Be(expectedHard);
            entity.RetinaExudateCottonWool.Should().Be(expectedCotton);
        }

        /// <summary>
        /// TC-FR-10: HemorrhageType = "VM nông" → RetinaHemorrhageSuperficial = true
        /// HemorrhageType = "VM sâu" → RetinaHemorrhageDeep = true
        /// </summary>
        [Theory]
        [InlineData("VM nông", true, false)]
        [InlineData("VM sâu", false, true)]
        [InlineData("Hắc mạc", false, false)]
        public void TC_FR_10_HemorrhageType_ShouldMap(string dtoValue, bool expectedSuperficial, bool expectedDeep)
        {
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();
            request.RightEyeFundusRetinaVessel = new UpdateEyeFundusRetinaVesselData { HemorrhageType = dtoValue };

            InvokeUpdateEyeExaminations(_service, request, record);

            var entity = record.EyeFundusRetinaVessels.First(e => e.Side == EyeSide.RIGHT);
            entity.RetinaHemorrhageSuperficial.Should().Be(expectedSuperficial);
            entity.RetinaHemorrhageDeep.Should().Be(expectedDeep);
            entity.HemorrhageLocation.Should().Be(dtoValue);
            if (dtoValue == "Hắc mạc")
                entity.ChoroidalNeovascularization.Should().BeTrue();
        }

        #endregion

        #region SECTION 12: Integration - All Eye Fields in One Request

        /// <summary>
        /// TC-INT-01: Full right eye examination → all fields should persist correctly
        /// This is the ultimate integration test that covers the "request updates but response doesn't show" bug
        /// </summary>
        [Fact]
        public void TC_INT_01_FullRightEye_ShouldMapAllFields()
        {
            // Arrange
            var record = CreateAndPersistTestRecord();
            var request = CreateBaseRequest();

            // Basic
            request.RightEyeBasic = new UpdateEyeBasicExamData
            {
                VaUncorrected = "20/40",
                IopMmhg = "18",
                IopMethod = "Maclakov",
                EomStatus = "Bình thường"
            };

            // Eyelid + Conjunctiva
            request.RightEyeEyelid = new UpdateEyeEyelidData
            {
                Status = "Phù mi",
                Ptosis = true,
                PtosisDegree = "2mm"
            };
            request.RightEyeConjunctiva = new UpdateEyeConjunctivaData
            {
                Status = "Xung huyết",
                Hemorrhage = true,
                Pterygium = false
            };

            // Cornea
            request.RightEyeCornea = new UpdateEyeCorneaExamData
            {
                Clarity = "Trong",
                Ulcer = false,
                Neovascularization = false
            };

            // AC + Iris
            request.RightEyeAnteriorChamber = new UpdateEyeAnteriorChamberData
            {
                Depth = "Bình thường",
                DepthMm = 3.0m,
                Pus = false
            };
            request.RightEyeIrisPupil = new UpdateEyeIrisPupilData
            {
                IrisColor = "Nâu",
                PupilShape = "Tròn",
                PupilReflex = "Tốt"
            };

            // Lens + Vitreous
            request.RightEyeLens = new UpdateEyeLensData
            {
                Status = "Trong",
                Subluxation = false
            };
            request.RightEyeVitreous = new UpdateEyeVitreousData
            {
                Status = "Sạch",
                Pvd = false
            };

            // Sclera
            request.RightEyeSclera = new UpdateEyeScleraExamData { Status = "Bình thường" };

            // Fundus
            request.RightEyeFundusDiscMacula = new UpdateEyeFundusDiscMaculaData
            {
                DiscStatus = "Bình thường",
                MaculaStatus = "Bình thường"
            };
            request.RightEyeFundusRetinaVessel = new UpdateEyeFundusRetinaVesselData
            {
                VesselStatus = "Bình thường",
                RetinaStatus = "Bình thường"
            };

            // Act
            InvokeUpdateEyeExaminations(_service, request, record);

            // Assert - Basic
            var basic = record.EyeExamBasics.First(e => e.Side == EyeSide.RIGHT);
            basic.VaUncorrected.Should().Be(10.0m);
            basic.IopMmhg.Should().Be(18.0m);
            basic.IopMethod.Should().Be("Maclakov");
            basic.EomNormal.Should().BeTrue();

            // Assert - Eyelid
            var eyelid = record.EyeEyelidConjunctivae.First(e => e.Side == EyeSide.RIGHT);
            eyelid.EyelidNormal.Should().BeFalse("Status='Phù mi'");
            eyelid.Ptosis.Should().BeTrue();
            eyelid.PtosisDegree.Should().Be("2mm");
            eyelid.ConjunctivaNormal.Should().BeFalse("Status='Xung huyết'");
            eyelid.ConjunctivaHemorrhage.Should().BeTrue();

            // Assert - Cornea
            var cornea = record.EyeCorneas.First(e => e.Side == EyeSide.RIGHT);
            cornea.Clarity.Should().Be("Trong");
            cornea.Ulcer.Should().BeFalse();

            // Assert - AC/Iris
            var acIris = record.EyeAcIrises.First(e => e.Side == EyeSide.RIGHT);
            acIris.AcDepthMm.Should().Be(3.0m);
            acIris.IrisColor.Should().Be("Nâu");
            acIris.PupilRound.Should().BeTrue();
            acIris.PupilReflex.Should().Be("Tốt");

            // Assert - Lens/Vitreous
            var lens = record.EyeLensVitreouses.First(e => e.Side == EyeSide.RIGHT);
            lens.LensClear.Should().BeTrue("Status='Trong'");
            lens.VitreousClear.Should().BeTrue("Status='Sạch'");

            // Assert - Sclera
            var sclera = record.EyeScleras.First(e => e.Side == EyeSide.RIGHT);
            sclera.ScleraNormal.Should().BeTrue();

            // Assert - Fundus
            var discMacula = record.EyeFundusDiscMaculas.First(e => e.Side == EyeSide.RIGHT);
            discMacula.OpticDiscNormal.Should().BeTrue();
            discMacula.MaculaNormal.Should().BeTrue();

            var retina = record.EyeFundusRetinaVessels.First(e => e.Side == EyeSide.RIGHT);
            retina.VesselNormal.Should().BeTrue();
            retina.RetinaNormal.Should().BeTrue();
        }

        #endregion
    }
}
