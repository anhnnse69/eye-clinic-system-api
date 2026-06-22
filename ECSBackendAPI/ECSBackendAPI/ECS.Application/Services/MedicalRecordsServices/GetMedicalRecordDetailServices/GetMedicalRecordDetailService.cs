using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.EyeExaminations;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Paraclinical;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Prescriptions;
using ECS.Domain.Entities.SubspecialtyRecords;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.MedicalRecordsServices.GetMedicalRecordDetailServices
{
    /// <summary>
    /// Service implementation for retrieving a single medical record detail.
    /// UC39 - View Medical Record Detail
    /// Orchestrates validation, authentication, authorization, and data mapping to return comprehensive
    /// medical record details including all related entities: diagnoses, prescriptions, clinical notes,
    /// eye examinations, paraclinical results, and subspecialty records.
    /// </summary>
    public class GetMedicalRecordDetailService : IGetMedicalRecordDetailService
    {
        private readonly IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> _medicalRecordRepository;
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> _doctorProfileRepository;
        private readonly IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> _patientProfileRepository;
        private readonly IValidator<GetMedicalRecordDetailRequest> _validator;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of <see cref="GetMedicalRecordDetailService"/> with required dependencies.
        /// </summary>
        /// <param name="medicalRecordRepository">Repository for querying medical record data.</param>
        /// <param name="doctorProfileRepository">Repository for querying doctor profile data.</param>
        /// <param name="patientProfileRepository">Repository for querying patient profile data.</param>
        /// <param name="validator">Validator for medical record detail request data.</param>
        /// <param name="httpContextAccessor">Accessor for HTTP context to retrieve user claims.</param>
        public GetMedicalRecordDetailService(
            IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> medicalRecordRepository,
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorProfileRepository,
            IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> patientProfileRepository,
            IValidator<GetMedicalRecordDetailRequest> validator,
            IHttpContextAccessor httpContextAccessor)
        {
            _medicalRecordRepository = medicalRecordRepository;
            _doctorProfileRepository = doctorProfileRepository;
            _patientProfileRepository = patientProfileRepository;
            _validator = validator;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Processes the request to retrieve medical record detail with validation and authorization.
        /// </summary>
        /// <param name="request">The medical record detail request containing the record ID.</param>
        /// <returns>
        /// An <see cref="ApiResponse{GetMedicalRecordDetailResponse}"/> containing:
        /// - Success response with complete medical record data if validation and authorization pass
        /// - Error response with appropriate error code if validation or authorization fails
        /// </returns>
        public async Task<ApiResponse<GetMedicalRecordDetailResponse>> Process(GetMedicalRecordDetailRequest request)
        {
            bool isValidationPassed = true;
            bool isAuthorized = true;
            string? validationErrorCode = null;
            Guid activeUserId = Guid.Empty;
            string userRole = string.Empty;
            Guid profileId;
            bool isStaff;
            MedicalRecord? record;
            ValidateRequest(request, ref isValidationPassed, ref validationErrorCode);
            RetrieveAuthenticatedUserInfo(ref activeUserId, ref userRole, ref isValidationPassed, ref validationErrorCode);
            (profileId, isStaff, isValidationPassed, validationErrorCode) = await ResolveUserProfileAsync(activeUserId, userRole, isValidationPassed, validationErrorCode);
            (record, isValidationPassed, validationErrorCode) = await FetchMedicalRecordAsync(request.Id, isValidationPassed, validationErrorCode);
            ValidateRecordAccess(record, userRole, profileId, isStaff, ref isAuthorized, ref isValidationPassed, ref validationErrorCode);
            return CreateResponse(record, isValidationPassed, isAuthorized, validationErrorCode);
        }

        /// <summary>
        /// Validates the incoming request payload against defined business rules.
        /// </summary>
        private void ValidateRequest(
            GetMedicalRecordDetailRequest request,
            ref bool isValidationPassed,
            ref string? validationErrorCode)
        {
            var result = _validator.Validate(request);
            if (!result.IsValid)
            {
                isValidationPassed = false;
                validationErrorCode = GeneralCode.APP_MESSAGE_4003.ToString();
            }
        }

        /// <summary>
        /// Retrieves authenticated user information from the HTTP context.
        /// </summary>
        private void RetrieveAuthenticatedUserInfo(
            ref Guid activeUserId,
            ref string userRole,
            ref bool isValidationPassed,
            ref string? validationErrorCode)
        {
            var principalIdValue = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var parseResult = Guid.TryParse(principalIdValue, out var parsedUserId);

            if (!parseResult)
            {
                isValidationPassed = false;
                validationErrorCode = GeneralCode.APP_MESSAGE_4033.ToString();
                return;
            }

            activeUserId = parsedUserId;
            userRole = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }

        /// <summary>
        /// Resolves the user profile based on their role.
        /// </summary>
        private async Task<(Guid ProfileId, bool IsStaff, bool IsValidationPassed, string? ValidationErrorCode)> ResolveUserProfileAsync(
            Guid activeUserId,
            string userRole,
            bool isValidationPassed,
            string? validationErrorCode)
        {
            if (!isValidationPassed)
            {
                return (Guid.Empty, false, false, validationErrorCode);
            }

            Guid profileId = Guid.Empty;
            bool isStaff = false;

            if (userRole == nameof(UserRole.PATIENT))
            {
                var patientProfile = await _patientProfileRepository
                    .FindByCondition(p => p.UserId == activeUserId, trackChanges: false)
                    .FirstOrDefaultAsync();

                if (patientProfile == null)
                {
                    return (Guid.Empty, false, false, GeneralCode.APP_MESSAGE_4033.ToString());
                }
                profileId = patientProfile.Id;
            }
            else if (userRole == nameof(UserRole.DOCTOR))
            {
                var doctorProfile = await _doctorProfileRepository
                    .FindByCondition(d => d.UserId == activeUserId && d.IsActive, trackChanges: false)
                    .Include(d => d.User)
                    .FirstOrDefaultAsync();

                if (doctorProfile == null)
                {
                    return (Guid.Empty, false, false, GeneralCode.APP_MESSAGE_4033.ToString());
                }
                profileId = doctorProfile.Id;
            }
            else if (userRole == nameof(UserRole.CLINIC_ADMIN) ||
                     userRole == nameof(UserRole.RECEPTIONIST) ||
                     userRole == nameof(UserRole.SYSTEM_ADMIN))
            {
                isStaff = true;
            }
            else
            {
                return (Guid.Empty, false, false, GeneralCode.APP_MESSAGE_4033.ToString());
            }

            return (profileId, isStaff, true, null);
        }

        /// <summary>
        /// Fetches the medical record from the database with all related entities.
        /// </summary>
        private async Task<(MedicalRecord? Record, bool IsValidationPassed, string? ValidationErrorCode)> FetchMedicalRecordAsync(
            Guid requestId,
            bool isValidationPassed,
            string? validationErrorCode)
        {
            if (!isValidationPassed)
            {
                return (null, false, validationErrorCode);
            }

            var record = await _medicalRecordRepository
                .FindByCondition(x => x.Id == requestId, trackChanges: false)
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.Appointment)
                .Include(x => x.Patient)
                .Include(x => x.Doctor).ThenInclude(d => d.User)
                .Include(x => x.Doctor.Specialty)
                .Include(x => x.EyeExamBasics)
                .Include(x => x.EyeEyelidConjunctivae)
                .Include(x => x.EyeCorneas)
                .Include(x => x.EyeAcIrises)
                .Include(x => x.EyeLensVitreouses)
                .Include(x => x.EyeScleras)
                .Include(x => x.EyeFundusDiscMaculas)
                .Include(x => x.EyeFundusRetinaVessels)
                .Include(x => x.LacrimalRecords)
                .Include(x => x.OctResults)
                .Include(x => x.VisualFieldTests)
                .Include(x => x.UltrasoundEyes)
                .Include(x => x.TraumaRecord).ThenInclude(t => t!.Surgeries)
                .Include(x => x.GlaucomaRecord).ThenInclude(g => g!.Histories)
                .Include(x => x.StrabismusPtosisRecord)
                .Include(x => x.PediatricRecord)
                .Include(x => x.Prescriptions).ThenInclude(p => p.Doctor).ThenInclude(d => d.User)
                .Include(x => x.Prescriptions).ThenInclude(p => p.Items)
                .Include(x => x.GlassesPrescriptions)
                .Include(x => x.Extras)
                .Include(x => x.DocumentAccessPermissions)
                    .ThenInclude(d => d.GrantedToUser)
                .Include(x => x.DocumentAccessPermissions)
                    .ThenInclude(d => d.GrantedByUser)
                .FirstOrDefaultAsync();

            if (record == null)
            {
                return (null, false, GeneralCode.APP_MESSAGE_4028.ToString());
            }

            return (record, true, null);
        }

        /// <summary>
        /// Validates that the authenticated user has access to the requested medical record.
        /// </summary>
        private void ValidateRecordAccess(
            MedicalRecord? record,
            string userRole,
            Guid profileId,
            bool isStaff,
            ref bool isAuthorized,
            ref bool isValidationPassed,
            ref string? validationErrorCode)
        {
            if (!isValidationPassed || record == null)
            {
                return;
            }

            if (isStaff)
            {
                return;
            }

            if (userRole == nameof(UserRole.PATIENT))
            {
                if (record.PatientId != profileId)
                {
                    isAuthorized = false;
                    isValidationPassed = false;
                    validationErrorCode = GeneralCode.APP_MESSAGE_4014.ToString();
                }
            }
            else if (userRole == nameof(UserRole.DOCTOR))
            {
                if (record.DoctorId != profileId)
                {
                    isAuthorized = false;
                    isValidationPassed = false;
                    validationErrorCode = GeneralCode.APP_MESSAGE_4014.ToString();
                }
            }
        }

        /// <summary>
        /// Creates the API response based on validation and authorization status.
        /// </summary>
        private ApiResponse<GetMedicalRecordDetailResponse> CreateResponse(
            MedicalRecord? record,
            bool isValidationPassed,
            bool isAuthorized,
            string? validationErrorCode)
        {
            var errorResponse = CreateErrorResponse(isValidationPassed, isAuthorized, validationErrorCode);
            if (errorResponse != null)
            {
                return errorResponse;
            }
            return CreateSuccessResponse(record!);
        }

        /// <summary>
        /// Creates an error response based on validation failure flags.
        /// </summary>
        /// <summary>
        /// Creates an error response based on validation and authorization failure flags.
        /// </summary>
        /// <param name="isValidationPassed">Indicates whether request validation succeeded.</param>
        /// <param name="isAuthorized">Indicates whether user is authorized to access the record.</param>
        /// <param name="validationErrorCode">The error code from validation failure, if any.</param>
        /// <returns>
        /// A failed <see cref="ApiResponse{GetMedicalRecordDetailResponse}"/> if validation failed;
        /// otherwise <c>null</c> to indicate no error response needed.
        /// </returns>
        private ApiResponse<GetMedicalRecordDetailResponse>? CreateErrorResponse(
            bool isValidationPassed,
            bool isAuthorized,
            string? validationErrorCode)
        {
            if (!isValidationPassed)
            {
                return ApiResponse<GetMedicalRecordDetailResponse>.Fail(
                    validationErrorCode ?? GeneralCode.APP_MESSAGE_4001.ToString());
            }
            return null;
        }

        /// <summary>
        /// Creates a success response containing the medical record details.
        /// </summary>
        private ApiResponse<GetMedicalRecordDetailResponse> CreateSuccessResponse(MedicalRecord record)
        {
            var response = MapToDetailDto(record);
            return ApiResponse<GetMedicalRecordDetailResponse>.Success(
                GeneralCode.APP_MESSAGE_2000.ToString(),
                response);
        }

        /// <summary>
        /// Maps the medical record entity to a detailed response DTO.
        /// </summary>
        /// <param name="record">The medical record entity to map.</param>
        /// <returns>A <see cref="GetMedicalRecordDetailResponse"/> containing all record details.</returns>
        private GetMedicalRecordDetailResponse MapToDetailDto(MedicalRecord record)
        {
            return new GetMedicalRecordDetailResponse
            {
                Id = record.Id,
                AppointmentId = record.AppointmentId,
                PatientId = record.PatientId,
                DoctorId = record.DoctorId,
                RecordType = record.RecordType.ToString(),
                ChiefComplaint = record.ChiefComplaint,
                IllnessDayNumber = record.IllnessDayNumber,
                MedicalHistory = record.MedicalHistory,
                PersonalHistoryEye = record.PersonalHistoryEye,
                PersonalHistorySystemic = record.PersonalHistorySystemic,
                FamilyHistory = record.FamilyHistory,
                VitalPulse = record.VitalPulse,
                VitalTemperature = record.VitalTemperature,
                VitalBloodPressure = record.VitalBloodPressure,
                VitalRespiratoryRate = record.VitalRespiratoryRate,
                VitalWeightKg = record.VitalWeightKg,
                SystemicExam = record.SystemicExam,
                DiagnosisMain = record.DiagnosisMain,
                DiagnosisComorbid = record.DiagnosisComorbid,
                DiagnosisDifferential = record.DiagnosisDifferential,
                Prognosis = record.Prognosis,
                TreatmentPlan = record.TreatmentPlan,
                Notes = record.Notes,
                IsLocked = record.IsLocked,
                CreatedAt = record.CreatedAt,
                UpdatedAt = record.UpdatedAt,

                PatientFullName = record.Patient.FullName,
                PatientDob = record.Patient.Dob.ToString("dd/MM/yyyy"),
                PatientPhone = record.Patient.PhoneNumber,
                PatientEmail = record.Patient.User?.Email ?? string.Empty,
                PatientGender = record.Patient.Gender.ToString(),
                PatientAddress = record.Patient.Address ?? string.Empty,
                PatientIdentityNumber = record.Patient.IdentityNumber ?? string.Empty,

                DoctorFullName = record.Doctor.User?.FullName ?? "N/A",
                DoctorTitle = record.Doctor.Title,
                DoctorSpecialty = record.Doctor.Specialty?.Name ?? string.Empty,

                AppointmentDate = record.Appointment.AppointmentDate,
                AppointmentStatus = record.Appointment.Status.ToString(),
                AppointmentNotes = record.Appointment.NoteReason,

                RightEyeExamBasic = MapEyeExamBasic(record.EyeExamBasics?.FirstOrDefault(x => x.Side == EyeSide.RIGHT)),
                LeftEyeExamBasic = MapEyeExamBasic(record.EyeExamBasics?.FirstOrDefault(x => x.Side == EyeSide.LEFT)),

                RightEyeEyelidConjunctiva = MapEyeEyelidConjunctiva(record.EyeEyelidConjunctivae?.FirstOrDefault(x => x.Side == EyeSide.RIGHT)),
                LeftEyeEyelidConjunctiva = MapEyeEyelidConjunctiva(record.EyeEyelidConjunctivae?.FirstOrDefault(x => x.Side == EyeSide.LEFT)),

                RightEyeCornea = MapEyeCornea(record.EyeCorneas?.FirstOrDefault(x => x.Side == EyeSide.RIGHT)),
                LeftEyeCornea = MapEyeCornea(record.EyeCorneas?.FirstOrDefault(x => x.Side == EyeSide.LEFT)),

                RightEyeAcIris = MapEyeAcIris(record.EyeAcIrises?.FirstOrDefault(x => x.Side == EyeSide.RIGHT)),
                LeftEyeAcIris = MapEyeAcIris(record.EyeAcIrises?.FirstOrDefault(x => x.Side == EyeSide.LEFT)),

                RightEyeLensVitreous = MapEyeLensVitreous(record.EyeLensVitreouses?.FirstOrDefault(x => x.Side == EyeSide.RIGHT)),
                LeftEyeLensVitreous = MapEyeLensVitreous(record.EyeLensVitreouses?.FirstOrDefault(x => x.Side == EyeSide.LEFT)),

                RightEyeSclera = MapEyeSclera(record.EyeScleras?.FirstOrDefault(x => x.Side == EyeSide.RIGHT)),
                LeftEyeSclera = MapEyeSclera(record.EyeScleras?.FirstOrDefault(x => x.Side == EyeSide.LEFT)),

                RightEyeFundusDiscMacula = MapEyeFundusDiscMacula(record.EyeFundusDiscMaculas?.FirstOrDefault(x => x.Side == EyeSide.RIGHT)),
                LeftEyeFundusDiscMacula = MapEyeFundusDiscMacula(record.EyeFundusDiscMaculas?.FirstOrDefault(x => x.Side == EyeSide.LEFT)),

                RightEyeFundusRetinaVessel = MapEyeFundusRetinaVessel(record.EyeFundusRetinaVessels?.FirstOrDefault(x => x.Side == EyeSide.RIGHT)),
                LeftEyeFundusRetinaVessel = MapEyeFundusRetinaVessel(record.EyeFundusRetinaVessels?.FirstOrDefault(x => x.Side == EyeSide.LEFT)),

                LacrimalRecords = MapLacrimalRecords(record.LacrimalRecords),

                OctResults = MapOctResults(record.OctResults),
                VisualFieldTests = MapVisualFieldTests(record.VisualFieldTests),
                UltrasoundEyes = MapUltrasoundEyes(record.UltrasoundEyes),

                TraumaRecord = MapTraumaRecord(record.TraumaRecord),
                GlaucomaRecord = MapGlaucomaRecord(record.GlaucomaRecord),
                StrabismusPtosisRecord = MapStrabismusPtosisRecord(record.StrabismusPtosisRecord),
                PediatricRecord = MapPediatricRecord(record.PediatricRecord),

                Prescriptions = MapPrescriptions(record.Prescriptions),
                GlassesPrescriptions = MapGlassesPrescriptions(record.GlassesPrescriptions),

                Extras = MapExtras(record.Extras),
                DocumentAccessPermissions = MapDocumentPermissions(record.DocumentAccessPermissions)
            };
        }

        /// <summary>
        /// Maps eye exam basic data to detail DTO.
        /// </summary>
        /// <param name="e">The eye exam basic entity to map.</param>
        /// <returns>A <see cref="EyeExamBasicDetail"/> containing basic eye exam data.</returns>
        private EyeExamBasicDetail? MapEyeExamBasic(EyeExamBasic? e) => e == null ? null : new EyeExamBasicDetail
        {
            Id = e.Id,
            Side = e.Side.ToString(),
            VaUncorrected = e.VaUncorrected?.ToString(),
            VaCorrected = e.VaCorrected?.ToString(),
            VaNear = e.VaNear?.ToString(),
            VaPinhole = e.VaPinhole?.ToString(),
            VaWithGlasses = e.VaCorrected?.ToString(),
            IopMmhg = e.IopMmhg?.ToString(),
            IopMethod = e.IopMethod,
            AutoRefraction = e.AutoRefraction,
            Retinoscopy = e.Retinoscopy,
            SubjectiveRefraction = e.SubjectiveRefraction,
            EomStatus = e.EomNormal ? "Bình thường" : e.EomNote,
            EomNote = e.EomNote,
            Nystagmus = e.Nystagmus ? "Có" : "Không",
            NystagmusType = e.NystagmusType,
            VisualField = e.VisualField,
            EyeballStatus = e.EyeballStatus,
            EyeballTexture = e.EyeballTexture,
            StrabismusType = e.StrabismusType,
            CoverTestResult = e.CoverTestResult,
            HirschbergTest = e.HirschbergTest,
            PrismMeasurement = e.PrismMeasurement,
            PupilExamResult = e.PupilExamResult,
            PupilReflexLight = e.PupilReflexLight,
            PupilAccommodation = e.PupilAccommodation,
            PupilRelativeAfferentDefect = e.PupilRelativeAfferentDefect,
            Notes = null
        };

        /// <summary>
        /// Maps eye eyelid and conjunctiva data to detail DTO.
        /// </summary>
        /// <param name="e">The eye eyelid conjunctiva entity to map.</param>
        /// <returns>A <see cref="EyeEyelidConjunctivaDetail"/> containing eyelid and conjunctiva data.</returns>
        private EyeEyelidConjunctivaDetail? MapEyeEyelidConjunctiva(EyeEyelidConjunctiva? e) => e == null ? null : new EyeEyelidConjunctivaDetail
        {
            Id = e.Id,
            Side = e.Side.ToString(),
            Status = e.EyelidNormal ? "Bình thường" : "Bất thường",
            Ptosis = e.Ptosis,
            PtosisDegree = e.PtosisDegree,
            Laceration = e.Laceration,
            LacerationExtent = e.LacerationExtent,
            LacerationLocation = e.LacerationDepth,
            LacerationSutured = e.LacerationSutured,
            LacerationUnsutured = e.LacerationUnsutured,
            LacrimalDuctStatus = e.LacrimalDuctNormal ? "Bình thường" : (e.LacrimalDuctCut ? "Đứt lệ quản" : null),
            LacrimalDuctLocation = e.LacrimalDuctCutLocation,
            Scar = e.Scar,
            OtherFindings = e.EyelidOther,
            Entropion = e.Entropion,
            Epicanthus = e.Epicanthus,
            EntropionPediatric = e.EntropionPediatric,
            FornixStatus = e.FornixStatus,
            SymblepharonHeight = e.SymblepharonHeight,
            SymblepharonWidth = e.SymblepharonWidth,
            ChalazionHordeolum = e.Chalazion || e.Hordeolum ? "Có" : null,
            ConjunctivaStatus = e.ConjunctivaNormal ? "Bình thường" : "Bất thường",
            ConjunctivaCongestionType = e.ConjunctivaCongestionType,
            ConjunctivaEdema = e.ConjunctivaEdema,
            ConjunctivaHemorrhage = e.ConjunctivaHemorrhage,
            ConjunctivaHemorrhageLocation = e.ConjunctivaHemorrhageLocation,
            ConjunctivaLaceration = e.ConjunctivaLaceration,
            ConjunctivaLacerationLocation = e.ConjunctivaLacerationLocation,
            ConjunctivaIschemia = e.ConjunctivaEdema,
            ConjunctivaPapilla = e.ConjunctivaPapilla,
            ConjunctivaFollicle = e.ConjunctivaFollicle,
            ConjunctivaKeratinization = e.ConjunctivaKeratinization,
            ConjunctivaScar = e.ConjunctivaScar,
            FluoresceinStain = e.FluoresceinStain,
            Pterygium = e.Pterygium,
            PterygiumLocation = e.PterygiumLocation,
            PterygiumSize = e.PterygiumSize,
            HasTumor = e.HasTumor,
            TumorNature = e.TumorNature,
            TumorLocation = e.TumorLocation,
            TumorSize = e.TumorSize,
            Lagophthalmos = e.Lagophthalmos,
            OtherFindingsConjunctiva = e.ConjunctivaOther
        };

        /// <summary>
        /// Maps eye cornea data to detail DTO.
        /// </summary>
        /// <param name="e">The eye cornea entity to map.</param>
        /// <returns>A <see cref="EyeCorneaDetail"/> containing cornea examination data.</returns>
        private EyeCorneaDetail? MapEyeCornea(EyeCornea? e) => e == null ? null : new EyeCorneaDetail
        {
            Id = e.Id,
            Side = e.Side.ToString(),
            Clarity = e.Clarity,
            Size = e.Size,
            Shape = e.Shape,
            DiameterMm = e.DiameterMm,
            Sensation = e.Sensation,
            EpitheliumStatus = e.EpitheliumPunctate ? "Đốm biểu mô" : null,
            EpitheliumPunctate = e.EpitheliumPunctate,
            EpitheliumEdemaLevel = e.EpitheliumEdemaLevel,
            EpitheliumLoss = e.EpitheliumLoss,
            StromaEdemaLevel = e.StromaEdema,
            StromaInfiltrate = e.StromaInfiltrate,
            StromaThinning = e.StromaThinning,
            Ulcer = e.Ulcer,
            UlcerLocation = e.UlcerLocation,
            UlcerSize = e.UlcerSize,
            UlcerDescription = e.UlcerDescription,
            Abscess = e.Ulcer,
            Descemetocele = e.PerforationThreatened,
            BloodStaining = e.CorneaExtras?.Contains("blood_staining") == true,
            Laceration = e.Laceration,
            LacerationSize = e.LacerationSize,
            LacerationLocation = e.LacerationLocation,
            LacerationType = e.LacerationType,
            LacerationSutured = e.LacerationSutured,
            AnatomicalReduction = e.LacerationSutured,
            Perforation = e.Perforation,
            PerforationDiameterMm = e.PerforationDiameterMm,
            PerforationLocation = e.PerforationLocation,
            SeidelTest = e.SeidelTest,
            Neovascularization = e.Neovascularization,
            NeovascularizationDepth = e.NeovascularizationLocation,
            NeovascularizationExtent = e.NeovascularizationExtent,
            LimbalStatus = e.LimbalStemDeficiency ? "Thiếu tế bào gốc vùng rìa" : null,
            CornealThickness = e.CornealThickness,
            DrugDeposit = e.DrugDeposit,
            OtherFindings = e.CorneaExtras,
            ForeignBody = e.CorneaExtras?.Contains("foreign_body") == true
        };

        /// <summary>
        /// Maps eye anterior chamber and iris data to detail DTO.
        /// </summary>
        /// <param name="e">The eye anterior chamber iris entity to map.</param>
        /// <returns>A <see cref="EyeAnteriorChamberDetail"/> containing anterior chamber and iris data.</returns>
        private EyeAnteriorChamberDetail? MapEyeAcIris(EyeAcIris? e) => e == null ? null : new EyeAnteriorChamberDetail
        {
            Id = e.Id,
            Side = e.Side.ToString(),
            Depth = e.AcFlat ? "Xẹp" : (e.AcDepthMm.HasValue ? "Sâu" : null),
            DepthMm = e.AcDepthMm,
            HerickClassification = e.AcDepthHerick,
            VitreousInAC = e.AcLensMaterial,
            Pus = e.AcPusMm.HasValue,
            PusMm = e.AcPusMm,
            Tyndall = e.AcTyndall,
            Exudate = e.AcTyndall != null,
            ExudateDescription = e.AcTyndall,
            Hemorrhage = e.AcHemorrhage,
            HemorrhageLevel = e.AcHemorrhage ? "Có" : null,
            ForeignBody = e.AcIrisExtras?.Contains("foreign_body") == true,
            OtherFindings = e.AcOtherFindings,
            IrisColor = e.IrisColor,
            IrisCondition = e.IrisCondition,
            IrisDegeneration = e.IrisDegeneration,
            IrisNeovascularization = e.IrisNeovascularization,
            IrisCiliaryProcesses = e.IrisCiliaryProcesses,
            KoeppeNodules = e.IrisKoeppeNodules,
            BusaccaNodules = e.IrisBusaccaNodules,
            IrisRootTear = e.IrisRootTear,
            IrisRootTearDegree = e.IrisRootTearDegree,
            IrisLoss = e.IrisLoss,
            IrisPerforation = e.IrisProlapse,
            PupilShape = e.PupilRound ? "Tròn" : (e.PupilIrregular ? "Méo" : (e.PupilSychiae ? "Dính" : null)),
            PupilPosition = e.PupilSynechiaeLocation,
            PupilReflex = e.PupilReflex,
            PupilDilated = e.PupilDilated,
            PtdtTest = e.PupilPtdtTest == "Có",
            FundusReflex = e.FundusReflex,
            AngleFindings = e.AngleOtherFindings,
            AngleSynechiae = e.AngleSynechiae,
            AnglePigment = e.AnglePigment,
            AngleNeovascularization = e.AngleNeovascularization
        };

        /// <summary>
        /// Maps eye lens and vitreous data to detail DTO.
        /// </summary>
        /// <param name="e">The eye lens vitreous entity to map.</param>
        /// <returns>A <see cref="EyeLensVitreousDetail"/> containing lens and vitreous data.</returns>
        private EyeLensVitreousDetail? MapEyeLensVitreous(EyeLensVitreous? e) => e == null ? null : new EyeLensVitreousDetail
        {
            Id = e.Id,
            Side = e.Side.ToString(),
            LensStatus = e.LensClear ? "Trong" : "Đục",
            OpacityType = e.LensOpacityType,
            OpacityLocation = e.LensOpacityLocation,
            Subluxation = e.LensSubluxation,
            LensInAnterior = e.LensIntoAnterior,
            LensInVitreous = e.LensIntoVitreous,
            Purulent = e.LensPurulent,
            AnteriorPigmentation = e.LensAnteriorPigmentation,
            IolPresent = e.LensIolPresent,
            IolStatus = e.LensIolStatus,
            IolPosition = e.LensIolPosition,
            Status = e.VitreousClear ? "Sạch" : (e.VitreousHemorrhage ? "Xuất huyết" : (e.VitreousPurulent ? "Viêm mủ" : "Đục")),
            OpacityLevel = e.VitreousOpacity ? "Đục" : null,
            Tyndall = e.VitreousTyndall,
            Hemorrhage = e.VitreousHemorrhage,
            Organized = e.VitreousOrganized,
            Pvd = e.VitreousPvd,
            VitreousPurulent = e.VitreousPurulent,
            ForeignBody = e.VitreousForeignBody,
            OtherFindings = e.LensVitreousExtras
        };

        /// <summary>
        /// Maps eye sclera data to detail DTO.
        /// </summary>
        /// <param name="e">The eye sclera entity to map.</param>
        /// <returns>A <see cref="EyeScleraDetail"/> containing sclera examination data.</returns>
        private EyeScleraDetail? MapEyeSclera(EyeSclera? e) => e == null ? null : new EyeScleraDetail
        {
            Id = e.Id,
            Side = e.Side.ToString(),
            Status = e.ScleraNormal ? "Bình thường" : (e.ScleraEctasia ? "Giãn lồi" : (e.ScleraLaceration ? "Rách" : "Bất thường")),
            Laceration = e.ScleraLaceration,
            LacerationSize = e.ScleraLacerationSize,
            LacerationLocation = e.ScleraLacerationLocation,
            LacerationSutured = e.ScleraLacerationSutured,
            LacerationUnsutured = e.ScleraLacerationSutured == false && e.ScleraLaceration,
            TissueEntrapped = e.ScleraTissueEntrapped,
            OtherFindings = e.ScleraExtras
        };

        /// <summary>
        /// Maps eye fundus disc and macula data to detail DTO.
        /// </summary>
        /// <param name="e">The eye fundus disc macula entity to map.</param>
        /// <returns>A <see cref="EyeFundusDiscMaculaDetail"/> containing optic disc and macula data.</returns>
        private EyeFundusDiscMaculaDetail? MapEyeFundusDiscMacula(EyeFundusDiscMacula? e) => e == null ? null : new EyeFundusDiscMaculaDetail
        {
            Id = e.Id,
            Side = e.Side.ToString(),
            DiscStatus = e.OpticDiscNormal ? "Bình thường" : (e.OpticDiscEdema ? "Phù" : (e.OpticDiscAtrophy ? "Teo" : (e.OpticDiscPallor ? "Bạc màu" : null))),
            DiscColor = e.OpticDiscColor,
            CdRatio = e.OpticDiscCupRatio,
            RimStatus = e.OpticDiscRimStatus,
            RimLocation = e.OpticDiscRimLocation,
            VesselChange = e.OpticDiscVesselChange,
            DiscHemorrhage = e.OpticDiscHemorrhage,
            Neovascularization = e.OpticDiscNeovascularization,
            DiscNotVisible = e.OpticDiscNotVisible,
            MaculaStatus = e.MaculaNormal ? "Bình thường" : e.MaculaCondition,
            MaculaReflexAbsent = e.MaculaReflexAbsent,
            MaculaEdemaType = e.MaculaEdemaType,
            MaculaHoleDegree = e.MaculaHoleDegree,
            MaculaScar = e.MaculaScar,
            SerousDetachment = e.MaculaSerousDetachment,
            MaculaHemorrhage = e.DiscMaculaExtras?.Contains("hemorrhage") == true,
            ChoroidStatus = e.ChoroidalNormal ? "Bình thường" : e.ChoroidalFindings,
            ChoroidalFindings = e.ChoroidalFindings,
            CNV = e.DiscMaculaExtras?.Contains("cnv") == true,
            ChorioretinitisActive = e.DiscMaculaExtras?.Contains("chorioretinitis_active") == true,
            ChorioretinitisScar = e.DiscMaculaExtras?.Contains("chorioretinitis_scar") == true,
            ChorioretinitisCount = e.DiscMaculaExtras?.Contains("chorioretinitis") == true ? 1 : null,
            ChorioretinitisLocation = e.DiscMaculaExtras?.Contains("chorioretinitis") == true ? "Nhiều vị trí" : null
        };

        /// <summary>
        /// Maps eye fundus retina vessel data to detail DTO.
        /// </summary>
        /// <param name="e">The eye fundus retina vessel entity to map.</param>
        /// <returns>A <see cref="EyeFundusRetinaVesselDetail"/> containing retina vessel data.</returns>
        private EyeFundusRetinaVesselDetail? MapEyeFundusRetinaVessel(EyeFundusRetinaVessel? e) => e == null ? null : new EyeFundusRetinaVesselDetail
        {
            Id = e.Id,
            Side = e.Side.ToString(),
            VesselStatus = e.VesselNormal ? "Bình thường" : "Bất thường",
            ArteryOcclusion = e.ArteryOcclusionType,
            VeinOcclusion = e.VeinOcclusionType,
            OcclusionType = e.OcclusionType,
            OcclusionEdema = e.OcclusionEdema,
            OcclusionIschemia = e.OcclusionIschemia,
            Vasculitis = e.RetinaVesselExtras?.Contains("vasculitis") == true,
            RetinalNeovascularization = e.ChoroidalNeovascularization || e.ChoroidalNeovesselsSubretinal,
            RetinaStatus = e.RetinaNormal ? "Bình thường" : e.RetinalCondition,
            RetinalCondition = e.RetinalCondition,
            RetinalEdema = e.RetinaEdema,
            EdemaType = e.OcclusionEdema ? "Phù nề" : null,
            Hemorrhage = e.HemorrhageLocation != null,
            HemorrhageType = e.HemorrhageLocation,
            Degeneration = e.DegenerativeDescription != null,
            DegenerationType = e.DegenerativeType,
            DegenerationDescription = e.DegenerativeDescription,
            Detachment = e.RetinalDetachment,
            DetachmentLevel = e.RetinalDetachmentLevel,
            RetinalTear = e.RetinalTear,
            TearCount = e.RetinalTearCount,
            TearLocation = e.RetinalTearLocation,
            TearMorphology = e.RetinalTearMorphology,
            BmscDetachment = e.RetinaVesselExtras?.Contains("bmsc_detachment") == true,
            Iofb = e.IntraocularForeignBody,
            IofbLocation = e.IofbLocation,
            IofbSize = e.IofbSize,
            CombinedFindings = e.RetinaVesselExtras,
            OtherFindings = e.RetinaVesselExtras
        };

        /// <summary>
        /// Maps lacrimal record data to detail DTO.
        /// </summary>
        /// <param name="e">The lacrimal record entity to map.</param>
        /// <returns>A <see cref="LacrimalRecordDetail"/> containing lacrimal examination data.</returns>
        private LacrimalRecordDetail? MapLacrimalRecord(LacrimalRecord? e) => e == null ? null : new LacrimalRecordDetail
        {
            Id = e.Id,
            Side = e.Side.ToString(),
            LacrimalDischarge = e.LacrimalDischarge,
            NasolacrimalStatus = e.NasolacrimalStatus,
            IrrigationFree = e.IrrigationFree,
            IrrigationRegurgitationSame = e.IrrigationRegurgitationSame,
            IrrigationRegurgitationOpposite = e.IrrigationRegurgitationOpposite,
            IrrigationNote = e.IrrigationNote,
            LacrimalOther = e.LacrimalOther
        };

        /// <summary>
        /// Maps a collection of lacrimal records to detail DTOs.
        /// </summary>
        /// <param name="list">The collection of lacrimal record entities to map.</param>
        /// <returns>A list of <see cref="LacrimalRecordDetail"/> containing lacrimal examination data.</returns>
        private List<LacrimalRecordDetail> MapLacrimalRecords(ICollection<LacrimalRecord>? list)
        {
            if (list == null) return new List<LacrimalRecordDetail>();
            return list.Select(MapLacrimalRecord).ToList();
        }

        /// <summary>
        /// Maps OCT results to detail DTO.
        /// </summary>
        /// <param name="list">The collection of OCT result entities to map.</param>
        /// <returns>A list of <see cref="OctResultDetail"/> containing OCT examination data.</returns>
        private List<OctResultDetail> MapOctResults(ICollection<OctResult>? list)
        {
            if (list == null) return new List<OctResultDetail>();
            return list.Select(e => new OctResultDetail
            {
                Id = e.Id,
                MachineName = e.MachineName,
                ScanPattern = e.ScanPattern,
                RnflAverageOd = e.RnflAverageOd,
                RnflAverageOs = e.RnflAverageOs,
                CmtOd = e.CmtOd,
                CmtOs = e.CmtOs,
                CupDiscRatioOd = e.CupDiscRatioOd,
                CupDiscRatioOs = e.CupDiscRatioOs,
                Conclusion = e.Conclusion,
                ImageUrl = e.ImageUrl,
                ExamDate = e.ExamDate,
                TechnicianName = e.TechnicianName
            }).ToList();
        }

        /// <summary>
        /// Maps visual field test results to detail DTO.
        /// </summary>
        /// <param name="list">The collection of visual field test entities to map.</param>
        /// <returns>A list of <see cref="VisualFieldTestDetail"/> containing visual field test data.</returns>
        private List<VisualFieldTestDetail> MapVisualFieldTests(ICollection<VisualFieldTest>? list)
        {
            if (list == null) return new List<VisualFieldTestDetail>();
            return list.Select(e => new VisualFieldTestDetail
            {
                Id = e.Id,
                Side = e.Side.ToString(),
                Machine = e.Machine,
                Strategy = e.Strategy,
                MdValue = e.MdValue,
                PsdValue = e.PsdValue,
                VfiPercent = e.VfiPercent,
                Reliable = e.Reliable,
                ResultSummary = e.ResultSummary,
                ImageUrl = e.ImageUrl,
                TestDate = e.TestDate,
                TechnicianName = e.TechnicianName
            }).ToList();
        }

        /// <summary>
        /// Maps ultrasound eye examination results to detail DTO.
        /// </summary>
        /// <param name="list">The collection of ultrasound eye entities to map.</param>
        /// <returns>A list of <see cref="UltrasoundEyeDetail"/> containing ultrasound examination data.</returns>
        private List<UltrasoundEyeDetail> MapUltrasoundEyes(ICollection<UltrasoundEye>? list)
        {
            if (list == null) return new List<UltrasoundEyeDetail>();
            return list.Select(e => new UltrasoundEyeDetail
            {
                Id = e.Id,
                Side = e.Side.ToString(),
                UltrasoundType = e.UltrasoundType,
                AxialLengthMm = e.AxialLengthMm,
                AcDepthMm = e.AcDepthMm,
                LensThicknessMm = e.LensThicknessMm,
                VitreousLengthMm = e.VitreousLengthMm,
                LensStatus = e.LensStatus,
                RetinaStatus = e.RetinaStatus,
                Conclusion = e.Conclusion,
                ImageUrl = e.ImageUrl,
                ExamDate = e.ExamDate,
                TechnicianName = e.TechnicianName
            }).ToList();
        }

        /// <summary>
        /// Maps trauma record data to detail DTO.
        /// </summary>
        /// <param name="e">The trauma record entity to map.</param>
        /// <returns>A <see cref="TraumaRecordDetail"/> containing trauma examination data and surgeries.</returns>
        private TraumaRecordDetail? MapTraumaRecord(TraumaRecord? e) => e == null ? null : new TraumaRecordDetail
        {
            Id = e.Id,
            InjuryCause = e.InjuryCause,
            InjuryTime = e.InjuryTime,
            PriorTreatment = e.PriorTreatment,
            PostTreatmentCourse = e.PostTreatmentCourse,
            OdInjuries = e.OdInjuries,
            OsInjuries = e.OsInjuries,
            InjuryDetails = e.InjuryDetails,
            TraumaConclusion = e.TraumaConclusion,
            DiagnosisClinical = e.DiagnosisClinical,
            DiagnosisCause = e.DiagnosisCause,
            TreatmentProcess = e.TreatmentProcess,
            TreatmentPlan = e.TreatmentPlan,
            Surgeries = MapTraumaSurgeries(e.Surgeries)
        };

        /// <summary>
        /// Maps a collection of trauma surgeries to detail DTOs.
        /// </summary>
        /// <param name="list">The collection of trauma surgery entities to map.</param>
        /// <returns>A list of <see cref="TraumaSurgeryDetail"/> containing surgery data.</returns>
        private List<TraumaSurgeryDetail> MapTraumaSurgeries(ICollection<TraumaSurgery>? list)
        {
            if (list == null) return new List<TraumaSurgeryDetail>();
            return list.Select(e => new TraumaSurgeryDetail
            {
                Id = e.Id,
                SurgeryDate = e.SurgeryDate,
                SurgeryType = e.SurgeryType,
                SurgeryDescription = e.SurgeryDescription,
                SurgeonName = e.SurgeonName,
                AnesthesiaType = e.AnesthesiaType,
                PostSurgeryCondition = e.PostSurgeryCondition,
                Notes = e.Notes
            }).ToList();
        }

        /// <summary>
        /// Maps glaucoma record data to detail DTO.
        /// </summary>
        /// <param name="e">The glaucoma record entity to map.</param>
        /// <returns>A <see cref="GlaucomaRecordDetail"/> containing glaucoma examination data and treatment history.</returns>
        private GlaucomaRecordDetail? MapGlaucomaRecord(GlaucomaRecord? e) => e == null ? null : new GlaucomaRecordDetail
        {
            Id = e.Id,
            EyePainLevel = e.EyePainLevel,
            VisionSymptoms = e.VisionSymptoms,
            VisionProgression = e.VisionProgression,
            HasPhotophobia = e.HasPhotophobia,
            HasTearing = e.HasTearing,
            HasRedness = e.HasRedness,
            SystemicSymptoms = e.SystemicSymptoms,
            VaWithoutCorrectionOd = e.VaWithoutCorrectionOd?.ToString(),
            VaWithoutCorrectionOs = e.VaWithoutCorrectionOs?.ToString(),
            VaWithCorrectionOd = e.VaWithCorrectionOd?.ToString(),
            VaWithCorrectionOs = e.VaWithCorrectionOs?.ToString(),
            IopOd = e.IopOd?.ToString(),
            IopOs = e.IopOs?.ToString(),
            IopMethod = e.IopMethod,
            IopTargetOd = e.IopTargetOd?.ToString(),
            IopTargetOs = e.IopTargetOs?.ToString(),
            HistoryEye = e.HistoryEye,
            HistoryEyeSurgery = e.HistoryEyeSurgery,
            PriorEyeSurgeryDetails = e.PriorEyeSurgeryDetails,
            SteroidUse = e.SteroidUse,
            SteroidPrescribed = e.SteroidPrescribed,
            MedicationDuration = null,
            MedicationRoute = null,
            HasCardiovascularDisease = e.HasCardiovascularDisease,
            HasHypertension = e.HasHypertension,
            HasDiabetes = e.HasDiabetes,
            HasCarotidFistula = e.HasCarotidFistula,
            OtherSystemicDisease = e.OtherSystemicDisease,
            FamilyHasGlaucoma = e.FamilyHasGlaucoma,
            FamilyGlaucomaRelation = e.FamilyGlaucomaRelation,
            GlaucomaMedications = e.GlaucomaMedications,
            MedicationChangeReason = e.MedicationChangeReason,
            OtherMedications = e.OtherMedications,
            TreatmentProgress = e.TreatmentProgress,
            GlaucomaType = e.GlaucomaType,
            StageOd = e.StageOd,
            StageOs = e.StageOs,
            HasEyelidSwelling = e.HasEyelidSwelling,
            HasConjunctivalInjection = e.HasConjunctivalInjection,
            HasFilteringBleb = e.HasFilteringBleb,
            BlebLocation = e.BlebLocation,
            BlebStatus = e.BlebStatus,
            ConjunctivalScarLocation = null,
            CornealTransparency = e.CornealTransparency,
            CornealEdemaLevel = null,
            CornealThickness = e.CornealThickness?.ToString(),
            HasScleralThinning = e.HasScleralThinning,
            ScleralScarLocation = e.ScleralScarLocation,
            AcDepthSmith = e.AcDepthSmith,
            AcDepthHerick = e.AcDepthHerick,
            GonioscopyOd = e.GonioscopyOd,
            GonioscopyOs = e.GonioscopyOs,
            AngleFindings = e.AngleFindings,
            IrisColor = e.IrisColor,
            IrisCondition = e.IrisCondition,
            HasIrisNeovascularization = e.HasIrisNeovascularization,
            PupilDiameter = e.PupilDiameter,
            PupilPigmentBorder = e.PupilPigmentBorder,
            PupilReflexResponse = e.PupilReflexResponse,
            LensStatus = e.LensStatus,
            FundusRetinaFindings = e.FundusRetinaFindings,
            FundusMaculaFindings = e.FundusMaculaFindings,
            HasCNV = e.HasCNV,
            HasRetinalHemorrhage = e.HasRetinalHemorrhage,
            OpticDiscDescription = e.OpticDiscDescription,
            NerveRimOd = e.NerveRimOd,
            NerveRimOs = e.NerveRimOs,
            OpticDiscCupRatio = e.OpticDiscCupRatio,
            OpticDiscVesselChange = e.OpticDiscVesselChange,
            HasOpticDiscHemorrhage = e.HasOpticDiscHemorrhage,
            HasRimAtrophy = e.HasRimAtrophy,
            EyeAxialLength = e.EyeAxialLength,
            TreatmentPlanSurgery = e.TreatmentPlanSurgery,
            TreatmentPlanLaser = e.TreatmentPlanLaser,
            TreatmentPlanMedication = e.TreatmentPlanMedication,
            FollowUpPlan = e.FollowUpPlan,
            Histories = MapGlaucomaHistories(e.Histories)
        };

        /// <summary>
        /// Maps glaucoma history records to detail DTOs.
        /// </summary>
        /// <param name="list">The collection of glaucoma history entities to map.</param>
        /// <returns>A list of <see cref="GlaucomaHistoryDetail"/> containing treatment history data.</returns>
        private List<GlaucomaHistoryDetail> MapGlaucomaHistories(ICollection<GlaucomaHistory>? list)
        {
            if (list == null) return new List<GlaucomaHistoryDetail>();
            return list.Select(e => new GlaucomaHistoryDetail
            {
                Id = e.Id,
                HistoryType = e.HistoryType,
                EyeSide = e.Side?.ToString(),
                AttemptNumber = e.AttemptNumber,
                ProcedureType = e.ProcedureType,
                ProcedureDate = e.ProcedureDate,
                FacilityLevel = e.FacilityLevel,
                DrugName = e.DrugName,
                Dosage = e.Dosage,
                Duration = e.Duration,
                Route = e.Route,
                ChangeReason = e.ChangeReason
            }).ToList();
        }

        /// <summary>
        /// Maps strabismus and ptosis record data to detail DTO.
        /// </summary>
        /// <param name="e">The strabismus ptosis record entity to map.</param>
        /// <returns>A <see cref="StrabismusPtosisRecordDetail"/> containing strabismus and ptosis examination data.</returns>
        private StrabismusPtosisRecordDetail? MapStrabismusPtosisRecord(StrabismusPtosisRecord? e) => e == null ? null : new StrabismusPtosisRecordDetail
        {
            Id = e.Id,
            ChiefStrabismus = e.ChiefStrabismus,
            ChiefPtosis = e.ChiefPtosis,
            Congenital = e.Congenital,
            Acquired = e.Acquired,
            AcquiredOnset = e.AcquiredOnset,
            StrabismusType = e.StrabismusType,
            Nystagmus = e.Nystagmus,
            NystagmusType = e.NystagmusType,
            PriorAmblyopiaTreatment = e.PriorAmblyopiaTreatment,
            PriorAmblyopiaResult = e.PriorAmblyopiaResult,
            PriorSurgery = e.PriorSurgery,
            PriorSurgeryResult = e.PriorSurgeryResult,
            VaBeforeAtropineOd = e.VaBeforeAtropineOd,
            VaBeforeAtropineOs = e.VaBeforeAtropineOs,
            VaAfterAtropineOd = e.VaAfterAtropineOd,
            VaAfterAtropineOs = e.VaAfterAtropineOs,
            RefractionPreAtropine = e.RefractionPreAtropine,
            RefractionPostAtropine = e.RefractionPostAtropine,
            PupilShadowTestOd = e.PupilShadowTestOd,
            PupilShadowTestOs = e.PupilShadowTestOs,
            EomGazeTest = e.EomGazeTest,
            EomInternalOd = e.EomInternalOd,
            EomInternalOs = e.EomInternalOs,
            ConvergencePoint = e.ConvergencePoint,
            CoverTestResult = e.CoverTestResult,
            HirschbergBeforeAtropine = e.HirschbergBeforeAtropine,
            HirschbergAfterAtropine = e.HirschbergAfterAtropine,
            PrismNear = e.PrismNear,
            PrismDistance = e.PrismDistance,
            PrismUp = e.PrismUp,
            PrismDown = e.PrismDown,
            StrabismusSyndrome = e.StrabismusSyndrome,
            SynoptophoreObjective = e.SynoptophoreObjective,
            SynoptophoreSubjective = e.SynoptophoreSubjective,
            BinocularStatus = e.BinocularStatus,
            FusionAmplitude = e.FusionAmplitude,
            RetinalCorrespondence = e.RetinalCorrespondence,
            Diplopia = e.Diplopia,
            CompensatoryHeadPosture = e.CompensatoryHeadPosture,
            PtosisDegreeOd = e.PtosisDegreeOd,
            PtosisDegreeOs = e.PtosisDegreeOs,
            LevatorFunctionOd = e.LevatorFunctionOd,
            LevatorFunctionOs = e.LevatorFunctionOs,
            MarcusGunn = e.MarcusGunn,
            BellPhenomenon = e.BellPhenomenon,
            FixationOd = e.FixationOd,
            FixationOs = e.FixationOs,
            PalpebralReflexOd = e.PalpebralReflexOd,
            PalpebralReflexOs = e.PalpebralReflexOs
        };

        /// <summary>
        /// Maps pediatric eye record data to detail DTO.
        /// </summary>
        /// <param name="e">The pediatric eye record entity to map.</param>
        /// <returns>A <see cref="PediatricRecordDetail"/> containing pediatric eye examination data.</returns>
        private PediatricRecordDetail? MapPediatricRecord(PediatricEyeRecord? e) => e == null ? null : new PediatricRecordDetail
        {
            Id = e.Id,
            Congenital = e.Congenital,
            Acquired = e.Acquired,
            AcquiredOnset = e.AcquiredOnset,
            PriorTreatment = e.PriorTreatment,
            PregnancyIllness = e.PregnancyIllness,
            PregnancyIllnessDetail = e.PregnancyIllnessDetail,
            IntellectualDevelopmentNormal = e.IntellectualDevelopmentNormal,
            ChiefSymptoms = e.ChiefSymptoms,
            EntropionOd = e.EntropionOd,
            EpicanthusOd = e.EpicanthusOd,
            PtosisOd = e.PtosisOd,
            EyelidTumor = e.EyelidTumor,
            EyelidTumorLocation = e.EyelidTumorLocation,
            EyelidTumorSize = e.EyelidTumorSize,
            EyeballOdStatus = e.EyeballOdStatus,
            EyeballOsStatus = e.EyeballOsStatus,
            EyeballTexture = e.EyeballTexture,
            AmblyopiaStatus = e.AmblyopiaStatus,
            FixationPreferenceOd = e.FixationPreferenceOd,
            FixationPreferenceOs = e.FixationPreferenceOs,
            FundusSummaryOd = e.FundusSummaryOd,
            FundusSummaryOs = e.FundusSummaryOs,
            IntellectualDevelopmentStatus = e.IntellectualDevelopmentStatus,
            GeneralHealthStatus = e.GeneralHealthStatus
        };

        /// <summary>
        /// Maps a collection of prescriptions to detail DTOs.
        /// </summary>
        /// <param name="list">The collection of prescription entities to map.</param>
        /// <returns>A list of <see cref="PrescriptionDetail"/> containing prescription data.</returns>
        private List<PrescriptionDetail> MapPrescriptions(ICollection<Prescription>? list)
        {
            if (list == null) return new List<PrescriptionDetail>();
            return list.Select(p => new PrescriptionDetail
            {
                Id = p.Id,
                Notes = p.Notes,
                CreatedAt = p.CreatedAt,
                DoctorName = p.Doctor?.User?.FullName ?? "N/A",
                Items = MapPrescriptionItems(p.Items)
            }).ToList();
        }

        /// <summary>
        /// Maps a collection of prescription items to detail DTOs.
        /// </summary>
        /// <param name="list">The collection of prescription item entities to map.</param>
        /// <returns>A list of <see cref="PrescriptionItemDetail"/> containing medication data.</returns>
        private List<PrescriptionItemDetail> MapPrescriptionItems(ICollection<PrescriptionItem>? list)
        {
            if (list == null) return new List<PrescriptionItemDetail>();
            return list.Select(e => new PrescriptionItemDetail
            {
                Id = e.Id,
                MedicineName = e.MedicineName,
                Dosage = e.Dosage,
                Frequency = e.Frequency,
                DurationDays = e.DurationDays,
                Quantity = e.Quantity,
                Instruction = e.Instruction
            }).ToList();
        }

        /// <summary>
        /// Maps a collection of glasses prescriptions to detail DTOs.
        /// </summary>
        /// <param name="list">The collection of glasses prescription entities to map.</param>
        /// <returns>A list of <see cref="GlassesPrescriptionDetail"/> containing glasses prescription data.</returns>
        private List<GlassesPrescriptionDetail> MapGlassesPrescriptions(ICollection<GlassesPrescription>? list)
        {
            if (list == null) return new List<GlassesPrescriptionDetail>();
            return list.Select(e => new GlassesPrescriptionDetail
            {
                Id = e.Id,
                SphOd = e.SphOd,
                CylOd = e.CylOd,
                AxisOd = e.AxisOd,
                AddOd = e.AddOd,
                SphOs = e.SphOs,
                CylOs = e.CylOs,
                AxisOs = e.AxisOs,
                AddOs = e.AddOs,
                Pd = e.Pd,
                LensType = e.LensType,
                Notes = e.Notes,
                CreatedAt = e.CreatedAt
            }).ToList();
        }

        /// <summary>
        /// Maps medical record extras data to detail DTO.
        /// </summary>
        /// <param name="e">The medical record extras entity to map.</param>
        /// <returns>A <see cref="MedicalRecordExtrasDetail"/> containing additional record data such as summaries and orders.</returns>
        private MedicalRecordExtrasDetail? MapExtras(MedicalRecordExtras? e) => e == null ? null : new MedicalRecordExtrasDetail
        {
            Id = e.Id,
            TraumaSummary = e.TraumaSummary,
            GlaucomaSummary = e.GlaucomaSummary,
            PediatricSummary = e.PediatricSummary,
            LabOrders = e.LabOrders,
            ImagingOrders = e.ImagingOrders,
            DischargeSummary = e.DischargeSummary,
            TreatmentProcess = e.TreatmentProcess,
            UpdatedAt = e.UpdatedAt
        };

        /// <summary>
        /// Maps document access permissions to detail DTOs.
        /// </summary>
        /// <param name="list">The collection of document access permission entities to map.</param>
        /// <returns>A list of <see cref="DocumentAccessPermissionDetail"/> containing permission data.</returns>
        private List<DocumentAccessPermissionDetail> MapDocumentPermissions(ICollection<DocumentAccessPermission>? list)
        {
            if (list == null) return new List<DocumentAccessPermissionDetail>();
            return list.Select(e => new DocumentAccessPermissionDetail
            {
                Id = e.Id,
                GrantedToUserName = e.GrantedToUser?.FullName ?? "N/A",
                GrantedByUserName = e.GrantedByUser?.FullName ?? "N/A",
                IsActive = e.IsActive,
                ExpiresAt = e.ExpiresAt,
                CreatedAt = e.CreatedAt
            }).ToList();
        }

        /// <summary>
        /// Represents permission flags for medical record access and editing.
        /// Used to determine user capabilities and restriction reasons.
        /// </summary>
        private struct PermissionFlags
        {
            public bool CanEdit { get; }
            public bool CanViewOnly { get; }
            public bool IsLocked { get; }
            public bool IsCurrentDoctor { get; }
            public bool IsPast { get; }
            public bool IsFuture { get; }

            public PermissionFlags(bool canEdit, bool canViewOnly, bool isLocked,
                bool isCurrentDoctor, bool isPast, bool isFuture)
            {
                CanEdit = canEdit;
                CanViewOnly = canViewOnly;
                IsLocked = isLocked;
                IsCurrentDoctor = isCurrentDoctor;
                IsPast = isPast;
                IsFuture = isFuture;
            }
        }
    }
}
