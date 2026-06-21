using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.EyeExaminations;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Prescriptions;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Entities.SubspecialtyRecords;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECS.Application.Services.MedicalRecordsServices.CreateMedicalRecordServices
{
    /// <summary>
    /// Service implementation for creating medical records.
    /// UC40 - Create Medical Record
    /// Handles creation of medical record with all related eye examinations and prescriptions.
    /// Supports 6 standard medical record templates:
    /// - MS21: Chấn thương (Trauma)
    /// - MS22: Bán phần trước (Anterior Segment)
    /// - MS23: Đáy mắt (Fundus)
    /// - MS24: Glôcôm (Glaucoma)
    /// - MS25: Lác, sụp mi (Strabismus/Ptosis)
    /// - MS26: Mắt trẻ em (Pediatric)
    /// Uses ExecutionState pattern for clean flow management.
    /// </summary>
    public class CreateMedicalRecordService : ICreateMedicalRecordService
    {
        private readonly IRepositoryQueryBase<Appointment, Guid, AppDbContext> _appointmentRepository;
        private readonly IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> _patientRepository;
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> _doctorRepository;
        private readonly IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> _medicalRecordRepository;
        private readonly IRepositoryBaseAsync<MedicalRecord, Guid, AppDbContext> _medicalRecordRepositoryAsync;
        private readonly IRepositoryBaseAsync<Queue, Guid, AppDbContext> _queueRepository;
        private readonly IValidator<CreateMedicalRecordRequest> _validator;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of <see cref="CreateMedicalRecordService"/> with all required repositories and services.
        /// </summary>
        /// <param name="appointmentRepository">Repository for appointment entities.</param>
        /// <param name="patientRepository">Repository for patient profile entities.</param>
        /// <param name="doctorRepository">Repository for doctor profile entities.</param>
        /// <param name="medicalRecordRepository">Query repository for medical record entities.</param>
        /// <param name="medicalRecordRepositoryAsync">Async repository for medical record operations.</param>
        /// <param name="queueRepository">Async repository for queue entities.</param>
        /// <param name="validator">Request validator using FluentValidation.</param>
        /// <param name="context">Application database context.</param>
        /// <param name="httpContextAccessor">HTTP context accessor for user claims.</param>
        public CreateMedicalRecordService(
            IRepositoryQueryBase<Appointment, Guid, AppDbContext> appointmentRepository,
            IRepositoryQueryBase<PatientProfile, Guid, AppDbContext> patientRepository,
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepository,
            IRepositoryQueryBase<MedicalRecord, Guid, AppDbContext> medicalRecordRepository,
            IRepositoryBaseAsync<MedicalRecord, Guid, AppDbContext> medicalRecordRepositoryAsync,
            IRepositoryBaseAsync<Queue, Guid, AppDbContext> queueRepository,
            IValidator<CreateMedicalRecordRequest> validator,
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _appointmentRepository = appointmentRepository;
            _patientRepository = patientRepository;
            _doctorRepository = doctorRepository;
            _medicalRecordRepository = medicalRecordRepository;
            _medicalRecordRepositoryAsync = medicalRecordRepositoryAsync;
            _queueRepository = queueRepository;
            _validator = validator;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Main orchestration method for medical record creation.
        /// </summary>
        /// <param name="request">The create medical record request containing all record data.</param>
        /// <returns>API response with created medical record info or error details.</returns>
        public async Task<ApiResponse<CreateMedicalRecordResponse>> Process(CreateMedicalRecordRequest request)
        {
            var state = new ExecutionState();
            // Step 1: Validate incoming request data
            ValidateRequest(request, state);
            // Step 2: Extract authenticated user ID from JWT token
            RetrieveAuthenticatedUserId(state);
            // Step 3: Parse appointment ID
            ParseAppointmentId(request.AppointmentId, state);
            // Step 4: Verify appointment exists and is valid
            await GetAppointmentAsync(state);
            // Step 5: Verify doctor exists and is active
            await GetDoctorProfileAsync(state);
            // Step 6: Verify patient exists
            await GetPatientProfileAsync(state);
            // Step 7: Check if medical record already exists for this appointment
            CheckMedicalRecordExists(state);
            // Step 8: Create medical record and all related entities
            await CreateMedicalRecordAsync(request, state);
            // Step 9: Update appointment and queue status
            await UpdateStatusesAsync(state);
            // Step 10: Build and return the response
            return CreateResponse(state);
        }

        /// <summary>
        /// ExecutionState holds all mutable state for the process flow.
        /// </summary>
        private class ExecutionState
        {
            public bool IsValidationPassed { get; set; } = true;
            public bool IsUserValid { get; set; } = true;
            public bool IsAppointmentValid { get; set; } = true;
            public bool IsDoctorExists { get; set; } = true;
            public bool IsPatientExists { get; set; } = true;
            public bool IsMedicalRecordExists { get; set; } = false;
            public bool IsExecutionSuccess { get; set; } = true;
            public bool HasError { get; set; } = false;
            public Guid ActiveUserId { get; set; }
            public Guid AppointmentId { get; set; }
            public Appointment? Appointment { get; set; }
            public DoctorProfile? DoctorProfile { get; set; }
            public PatientProfile? PatientProfile { get; set; }
            public MedicalRecord? CreatedMedicalRecord { get; set; }
            public string? ErrorCode { get; set; }
            public string? PatientName { get; set; }
            public string? DoctorName { get; set; }
            public string? RecordTypeLabel { get; set; }
        }

        /// <summary>
        /// Validates the incoming request using FluentValidation rules.
        /// </summary>
        /// <param name="request">The medical record creation request to validate.</param>
        /// <param name="state">Execution state to store validation result.</param>
        private void ValidateRequest(CreateMedicalRecordRequest request, ExecutionState state)
        {
            var result = _validator.Validate(request);
            state.IsValidationPassed = result.IsValid;
            state.HasError = !result.IsValid;
            state.ErrorCode = result.IsValid ? null : GeneralCode.APP_MESSAGE_4019.ToString();
        }

        /// <summary>
        /// Extracts the authenticated user ID from JWT token in HTTP context.
        /// </summary>
        /// <param name="state">Execution state to store user ID.</param>
        private void RetrieveAuthenticatedUserId(ExecutionState state)
        {
            var principalIdValue = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var parseResult = Guid.TryParse(principalIdValue, out var parsedUserId);
            state.IsUserValid = parseResult;
            state.ActiveUserId = parseResult ? parsedUserId : Guid.Empty;
            state.HasError = !parseResult;
            state.ErrorCode = parseResult ? null : GeneralCode.APP_MESSAGE_4033.ToString();
        }

        /// <summary>
        /// Parses the appointment ID from string to Guid.
        /// </summary>
        /// <param name="appointmentId">Appointment ID as string.</param>
        /// <param name="state">Execution state to store parsed appointment ID.</param>
        private void ParseAppointmentId(string appointmentId, ExecutionState state)
        {
            var parseResult = Guid.TryParse(appointmentId, out var parsedId);
            state.AppointmentId = parseResult ? parsedId : Guid.Empty;
            state.HasError = state.HasError || !parseResult;
            state.ErrorCode = parseResult ? state.ErrorCode : GeneralCode.APP_MESSAGE_4019.ToString();
        }

        /// <summary>
        /// Retrieves and validates the appointment from database.
        /// </summary>
        /// <param name="state">Execution state containing appointment ID.</param>
        private async Task GetAppointmentAsync(ExecutionState state)
        {
            if (state.HasError) return;
            var appointment = await _appointmentRepository.FindByCondition(a => a.Id == state.AppointmentId, trackChanges: false).Include(a => a.Patient).Include(a => a.Doctor).ThenInclude(d => d.User).FirstOrDefaultAsync();
            state.Appointment = appointment;
            state.IsAppointmentValid = appointment != null;
            state.HasError = state.HasError || !state.IsAppointmentValid;
            state.ErrorCode = state.IsAppointmentValid ? state.ErrorCode : GeneralCode.APP_MESSAGE_4012.ToString();
        }

        /// <summary>
        /// Retrieves the doctor profile for the authenticated user.
        /// </summary>
        /// <param name="state">Execution state containing active user ID.</param>
        private async Task GetDoctorProfileAsync(ExecutionState state)
        {
            if (state.HasError || state.Appointment == null) return;
            var doctorProfile = await _doctorRepository.FindByCondition(d => d.UserId == state.ActiveUserId && d.IsActive, trackChanges: false).Include(d => d.User).FirstOrDefaultAsync();
            state.DoctorProfile = doctorProfile;
            state.IsDoctorExists = doctorProfile != null;
            state.DoctorName = doctorProfile?.User?.FullName;
            state.HasError = state.HasError || !state.IsDoctorExists;
            state.ErrorCode = state.IsDoctorExists ? state.ErrorCode : GeneralCode.APP_MESSAGE_4011.ToString();
        }

        /// <summary>
        /// Retrieves the patient profile associated with the appointment.
        /// </summary>
        /// <param name="state">Execution state containing appointment with patient ID.</param>
        private async Task GetPatientProfileAsync(ExecutionState state)
        {
            if (state.HasError || state.Appointment == null) return;
            var patientProfile = await _patientRepository.FindByCondition(p => p.Id == state.Appointment.PatientId, trackChanges: false).FirstOrDefaultAsync();
            state.PatientProfile = patientProfile;
            state.IsPatientExists = patientProfile != null;
            state.PatientName = patientProfile?.FullName;
            state.HasError = state.HasError || !state.IsPatientExists;
            state.ErrorCode = state.IsPatientExists ? state.ErrorCode : GeneralCode.APP_MESSAGE_4010.ToString();
        }

        /// <summary>
        /// Checks if a medical record already exists for the appointment.
        /// </summary>
        /// <param name="state">Execution state containing appointment ID.</param>
        private void CheckMedicalRecordExists(ExecutionState state)
        {
            if (!state.IsAppointmentValid || state.HasError) return;
            var existingRecord = _medicalRecordRepository.FindByCondition(r => r.AppointmentId == state.AppointmentId, trackChanges: false).FirstOrDefaultAsync().GetAwaiter().GetResult();
            state.IsMedicalRecordExists = existingRecord != null;
            if (state.IsMedicalRecordExists) { state.HasError = true; state.ErrorCode = GeneralCode.APP_MESSAGE_4027.ToString(); }
        }

        /// <summary>
        /// Creates the medical record and all related entities in a transaction.
        /// </summary>
        /// <param name="request">The medical record creation request.</param>
        /// <param name="state">Execution state containing validated data.</param>
        private async Task CreateMedicalRecordAsync(CreateMedicalRecordRequest request, ExecutionState state)
        {
            if (state.HasError || state.Appointment == null || state.DoctorProfile == null || state.PatientProfile == null) return;
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                Enum.TryParse<RecordType>(request.RecordType, true, out var recordType);
                var medicalRecord = new MedicalRecord { Id = Guid.NewGuid(), AppointmentId = state.AppointmentId, PatientId = state.Appointment.PatientId, DoctorId = state.DoctorProfile.Id, RecordType = recordType, ChiefComplaint = request.ChiefComplaint, IllnessDayNumber = request.IllnessDayNumber, MedicalHistory = request.MedicalHistory, PersonalHistoryEye = request.PersonalHistoryEye, PersonalHistorySystemic = request.PersonalHistorySystemic, FamilyHistory = request.FamilyHistory, VitalPulse = request.VitalPulse, VitalTemperature = request.VitalTemperature, VitalBloodPressure = request.VitalBloodPressure, VitalRespiratoryRate = request.VitalRespiratoryRate, VitalWeightKg = request.VitalWeightKg, SystemicExam = request.SystemicExam != null ? System.Text.Json.JsonSerializer.Serialize(request.SystemicExam) : null, DiagnosisMain = request.DiagnosisMain, DiagnosisComorbid = request.DiagnosisComorbid, DiagnosisDifferential = request.DiagnosisDifferential, Prognosis = request.Prognosis, TreatmentPlan = request.TreatmentPlan, Notes = request.Notes, IsLocked = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                _context.MedicalRecords.Add(medicalRecord);
                await CreateEyeExaminationsAsync(_context, medicalRecord.Id, request);
                await CreateSubspecialtyRecordsAsync(_context, medicalRecord.Id, request, recordType);
                await CreatePrescriptionAsync(_context, medicalRecord.Id, state.DoctorProfile.Id, request);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                state.CreatedMedicalRecord = medicalRecord;
                state.RecordTypeLabel = GetRecordTypeLabel(recordType);
            }
            catch (Exception) { await transaction.RollbackAsync(); state.IsExecutionSuccess = false; state.HasError = true; state.ErrorCode = GeneralCode.APP_MESSAGE_5001.ToString(); }
        }

        /// <summary>
        /// Creates eye examination records for both eyes based on provided exam data.
        /// </summary>
        /// <param name="context">Database context for adding entities.</param>
        /// <param name="recordId">The parent medical record ID.</param>
        /// <param name="request">The medical record creation request with exam data.</param>
        private async Task CreateEyeExaminationsAsync(AppDbContext context, Guid recordId, CreateMedicalRecordRequest request)
        {
            // Eye Exam Basic - Right
            if (request.RightEyeBasic != null)
            {
                context.EyeExamBasics.Add(MapEyeExamBasic(request.RightEyeBasic, recordId, EyeSide.RIGHT));
            }
            else
            {
                context.EyeExamBasics.Add(new EyeExamBasic { Id = Guid.NewGuid(), RecordId = recordId, Side = EyeSide.RIGHT });
            }
            // Eye Exam Basic - Left
            if (request.LeftEyeBasic != null)
            {
                context.EyeExamBasics.Add(MapEyeExamBasic(request.LeftEyeBasic, recordId, EyeSide.LEFT));
            }
            else
            {
                context.EyeExamBasics.Add(new EyeExamBasic { Id = Guid.NewGuid(), RecordId = recordId, Side = EyeSide.LEFT });
            }
            // Eyelid & Conjunctiva - Right
            if (request.RightEyeEyelid != null)
            {
                context.EyeEyelidConjunctivae.Add(MapEyeEyelidConjunctiva(request.RightEyeEyelid, recordId, EyeSide.RIGHT));
            }
            else
            {
                context.EyeEyelidConjunctivae.Add(new EyeEyelidConjunctiva { Id = Guid.NewGuid(), RecordId = recordId, Side = EyeSide.RIGHT });
            }
            // Eyelid & Conjunctiva - Left
            if (request.LeftEyeEyelid != null)
            {
                context.EyeEyelidConjunctivae.Add(MapEyeEyelidConjunctiva(request.LeftEyeEyelid, recordId, EyeSide.LEFT));
            }
            else
            {
                context.EyeEyelidConjunctivae.Add(new EyeEyelidConjunctiva { Id = Guid.NewGuid(), RecordId = recordId, Side = EyeSide.LEFT });
            }
            // Cornea - Right
            if (request.RightEyeCornea != null)
            {
                context.EyeCorneas.Add(MapEyeCornea(request.RightEyeCornea, recordId, EyeSide.RIGHT));
            }
            else
            {
                context.EyeCorneas.Add(new EyeCornea { Id = Guid.NewGuid(), RecordId = recordId, Side = EyeSide.RIGHT });
            }
            // Cornea - Left
            if (request.LeftEyeCornea != null)
            {
                context.EyeCorneas.Add(MapEyeCornea(request.LeftEyeCornea, recordId, EyeSide.LEFT));
            }
            else
            {
                context.EyeCorneas.Add(new EyeCornea { Id = Guid.NewGuid(), RecordId = recordId, Side = EyeSide.LEFT });
            }
            // AC & Iris - Right (Anterior Chamber + Iris Pupil)
            if (request.RightEyeAnteriorChamber != null || request.RightEyeIrisPupil != null)
            {
                context.EyeAcIrises.Add(MapEyeAcIris(request.RightEyeAnteriorChamber, request.RightEyeIrisPupil, recordId, EyeSide.RIGHT));
            }
            else
            {
                context.EyeAcIrises.Add(new EyeAcIris { Id = Guid.NewGuid(), RecordId = recordId, Side = EyeSide.RIGHT });
            }
            // AC & Iris - Left
            if (request.LeftEyeAnteriorChamber != null || request.LeftEyeIrisPupil != null)
            {
                context.EyeAcIrises.Add(MapEyeAcIris(request.LeftEyeAnteriorChamber, request.LeftEyeIrisPupil, recordId, EyeSide.LEFT));
            }
            else
            {
                context.EyeAcIrises.Add(new EyeAcIris { Id = Guid.NewGuid(), RecordId = recordId, Side = EyeSide.LEFT });
            }
            // Lens & Vitreous - Right
            if (request.RightEyeLens != null || request.RightEyeVitreous != null)
            {
                context.EyeLensVitreouses.Add(MapEyeLensVitreous(request.RightEyeLens, request.RightEyeVitreous, recordId, EyeSide.RIGHT));
            }
            else
            {
                context.EyeLensVitreouses.Add(new EyeLensVitreous { Id = Guid.NewGuid(), RecordId = recordId, Side = EyeSide.RIGHT });
            }
            // Lens & Vitreous - Left
            if (request.LeftEyeLens != null || request.LeftEyeVitreous != null)
            {
                context.EyeLensVitreouses.Add(MapEyeLensVitreous(request.LeftEyeLens, request.LeftEyeVitreous, recordId, EyeSide.LEFT));
            }
            else
            {
                context.EyeLensVitreouses.Add(new EyeLensVitreous { Id = Guid.NewGuid(), RecordId = recordId, Side = EyeSide.LEFT });
            }
            // Sclera - Right
            if (request.RightEyeSclera != null)
            {
                context.EyeScleras.Add(MapEyeSclera(request.RightEyeSclera, recordId, EyeSide.RIGHT));
            }
            else
            {
                context.EyeScleras.Add(new EyeSclera { Id = Guid.NewGuid(), RecordId = recordId, Side = EyeSide.RIGHT });
            }
            // Sclera - Left
            if (request.LeftEyeSclera != null)
            {
                context.EyeScleras.Add(MapEyeSclera(request.LeftEyeSclera, recordId, EyeSide.LEFT));
            }
            else
            {
                context.EyeScleras.Add(new EyeSclera { Id = Guid.NewGuid(), RecordId = recordId, Side = EyeSide.LEFT });
            }
            // Fundus Disc & Macula - Right
            if (request.RightEyeFundusDiscMacula != null)
            {
                context.EyeFundusDiscMaculas.Add(MapEyeFundusDiscMacula(request.RightEyeFundusDiscMacula, recordId, EyeSide.RIGHT));
            }
            else
            {
                context.EyeFundusDiscMaculas.Add(new EyeFundusDiscMacula { Id = Guid.NewGuid(), RecordId = recordId, Side = EyeSide.RIGHT });
            }
            // Fundus Disc & Macula - Left
            if (request.LeftEyeFundusDiscMacula != null)
            {
                context.EyeFundusDiscMaculas.Add(MapEyeFundusDiscMacula(request.LeftEyeFundusDiscMacula, recordId, EyeSide.LEFT));
            }
            else
            {
                context.EyeFundusDiscMaculas.Add(new EyeFundusDiscMacula { Id = Guid.NewGuid(), RecordId = recordId, Side = EyeSide.LEFT });
            }
            // Fundus Retina & Vessel - Right
            if (request.RightEyeFundusRetinaVessel != null)
            {
                context.EyeFundusRetinaVessels.Add(MapEyeFundusRetinaVessel(request.RightEyeFundusRetinaVessel, recordId, EyeSide.RIGHT));
            }
            else
            {
                context.EyeFundusRetinaVessels.Add(new EyeFundusRetinaVessel { Id = Guid.NewGuid(), RecordId = recordId, Side = EyeSide.RIGHT });
            }
            // Fundus Retina & Vessel - Left
            if (request.LeftEyeFundusRetinaVessel != null)
            {
                context.EyeFundusRetinaVessels.Add(MapEyeFundusRetinaVessel(request.LeftEyeFundusRetinaVessel, recordId, EyeSide.LEFT));
            }
            else
            {
                context.EyeFundusRetinaVessels.Add(new EyeFundusRetinaVessel { Id = Guid.NewGuid(), RecordId = recordId, Side = EyeSide.LEFT });
            }
        }

        /// <summary>
        /// Creates subspecialty records based on the medical record type (MS21-MS26).
        /// </summary>
        /// <param name="context">Database context for adding entities.</param>
        /// <param name="recordId">The parent medical record ID.</param>
        /// <param name="request">The medical record creation request with subspecialty data.</param>
        /// <param name="recordType">The type of medical record to determine which subspecialty to create.</param>
        private async Task CreateSubspecialtyRecordsAsync(AppDbContext context, Guid recordId, CreateMedicalRecordRequest request, RecordType recordType)
        {
            switch (recordType)
            {
                case RecordType.MS21_TRAUMA:
                    // Trauma Record
                    if (request.TraumaRecord != null)
                    {
                        var traumaRecord = new TraumaRecord
                        {
                            Id = Guid.NewGuid(),
                            RecordId = recordId,
                            InjuryCause = request.TraumaRecord.InjuryCause,
                            InjuryTime = request.TraumaRecord.InjuryTime,
                            PriorTreatment = request.TraumaRecord.PriorTreatment,
                            PostTreatmentCourse = request.TraumaRecord.PostTreatmentCourse,
                            OdInjuries = request.TraumaRecord.OdInjuries,
                            OsInjuries = request.TraumaRecord.OsInjuries,
                            InjuryDetails = request.TraumaRecord.InjuryDetails,
                            TraumaConclusion = request.TraumaRecord.TraumaConclusion
                        };
                        context.TraumaRecords.Add(traumaRecord);

                        // Trauma Surgeries
                        if (request.TraumaSurgeries != null && request.TraumaSurgeries.Count > 0)
                        {
                            foreach (var surgery in request.TraumaSurgeries)
                            {
                                context.TraumaSurgeries.Add(new TraumaSurgery
                                {
                                    Id = Guid.NewGuid(),
                                    TraumaRecordId = traumaRecord.Id,
                                    SurgeryDate = surgery.SurgeryDate,
                                    SurgeryType = surgery.SurgeryType,
                                    SurgeryDescription = surgery.SurgeryDescription,
                                    SurgeonName = surgery.SurgeonName,
                                    AnesthesiaType = surgery.AnesthesiaType,
                                    PostSurgeryCondition = surgery.PostSurgeryCondition,
                                    Notes = surgery.Notes
                                });
                            }
                        }
                    }
                    break;

                case RecordType.MS22_ANTERIOR:
                    // Lacrimal record
                    if (request.LacrimalRecord != null)
                    {
                        Enum.TryParse<EyeSide>(request.LacrimalRecord.Side, true, out var lacrimalSide);
                        context.LacrimalRecords.Add(new LacrimalRecord
                        {
                            Id = Guid.NewGuid(),
                            RecordId = recordId,
                            Side = lacrimalSide,
                            IrrigationFree = request.LacrimalRecord.IrrigationFree,
                            IrrigationRegurgitationSame = request.LacrimalRecord.IrrigationRegurgitationSame,
                            IrrigationRegurgitationOpposite = request.LacrimalRecord.IrrigationRegurgitationOpposite,
                            IrrigationNote = request.LacrimalRecord.IrrigationNote,
                            LacrimalOther = request.LacrimalRecord.LacrimalOther
                        });
                    }
                    break;

                case RecordType.MS23_FUNDUS:
                    // Fundus record - primarily handled by eye examinations
                    break;

                case RecordType.MS24_GLAUCOMA:
                    if (request.GlaucomaRecord != null)
                    {
                        var glaucomaRecord = new GlaucomaRecord
                        {
                            Id = Guid.NewGuid(),
                            RecordId = recordId,

                            // Symptoms
                            EyePainLevel = request.GlaucomaRecord.EyePainLevel,
                            VisionSymptoms = request.GlaucomaRecord.VisionSymptoms,
                            VisionProgression = request.GlaucomaRecord.VisionProgression,
                            HasPhotophobia = request.GlaucomaRecord.HasPhotophobia,
                            HasTearing = request.GlaucomaRecord.HasTearing,
                            HasRedness = request.GlaucomaRecord.HasRedness,
                            SystemicSymptoms = request.GlaucomaRecord.SystemicSymptoms,

                            // Visual Acuity & IOP
                            VaWithoutCorrectionOd = ParseDecimalNullable(request.GlaucomaRecord.VaWithoutCorrectionOd),
                            VaWithoutCorrectionOs = ParseDecimalNullable(request.GlaucomaRecord.VaWithoutCorrectionOs),
                            VaWithCorrectionOd = ParseDecimalNullable(request.GlaucomaRecord.VaWithCorrectionOd),
                            VaWithCorrectionOs = ParseDecimalNullable(request.GlaucomaRecord.VaWithCorrectionOs),
                            IopOd = ParseDecimalNullable(request.GlaucomaRecord.IopOd),
                            IopOs = ParseDecimalNullable(request.GlaucomaRecord.IopOs),
                            IopMethod = request.GlaucomaRecord.IopMethod,
                            IopTargetOd = ParseDecimalNullable(request.GlaucomaRecord.IopTargetOd),
                            IopTargetOs = ParseDecimalNullable(request.GlaucomaRecord.IopTargetOs),

                            // History
                            HistoryEye = request.GlaucomaRecord.HistoryEye,
                            HistoryEyeSurgery = request.GlaucomaRecord.HistoryEyeSurgery,
                            PriorEyeSurgeryDetails = request.GlaucomaRecord.PriorEyeSurgeryDetails,
                            SteroidUse = request.GlaucomaRecord.SteroidUse,
                            SteroidPrescribed = request.GlaucomaRecord.SteroidPrescribed,

                            // Systemic history
                            HasCardiovascularDisease = request.GlaucomaRecord.HasCardiovascularDisease,
                            HasHypertension = request.GlaucomaRecord.HasHypertension,
                            HasDiabetes = request.GlaucomaRecord.HasDiabetes,
                            HasCarotidFistula = request.GlaucomaRecord.HasCarotidFistula,
                            OtherSystemicDisease = request.GlaucomaRecord.OtherSystemicDisease,

                            // Family history
                            FamilyHasGlaucoma = request.GlaucomaRecord.FamilyHasGlaucoma,
                            FamilyGlaucomaRelation = request.GlaucomaRecord.FamilyGlaucomaRelation,

                            // Treatment History
                            GlaucomaMedications = request.GlaucomaRecord.GlaucomaMedications,
                            OtherMedications = request.GlaucomaRecord.OtherMedications,
                            TreatmentProgress = request.GlaucomaRecord.TreatmentProgress,

                            // Classification
                            GlaucomaType = request.GlaucomaRecord.GlaucomaType,
                            StageOd = request.GlaucomaRecord.StageOd,
                            StageOs = request.GlaucomaRecord.StageOs,

                            // Examination
                            HasEyelidSwelling = request.GlaucomaRecord.HasEyelidSwelling,
                            HasConjunctivalInjection = request.GlaucomaRecord.HasConjunctivalInjection,
                            HasFilteringBleb = request.GlaucomaRecord.HasFilteringBleb,
                            BlebLocation = request.GlaucomaRecord.BlebLocation,
                            BlebStatus = request.GlaucomaRecord.BlebStatus,
                            CornealTransparency = request.GlaucomaRecord.CornealTransparency,
                            CornealThickness = ParseDecimalNullable(request.GlaucomaRecord.CornealThickness),
                            AcDepthSmith = request.GlaucomaRecord.AcDepthSmith,
                            AcDepthHerick = request.GlaucomaRecord.AcDepthHerick,
                            GonioscopyOd = request.GlaucomaRecord.GonioscopyOd,
                            GonioscopyOs = request.GlaucomaRecord.GonioscopyOs,
                            AngleFindings = request.GlaucomaRecord.AngleFindings,
                            IrisColor = request.GlaucomaRecord.IrisColor,
                            IrisCondition = request.GlaucomaRecord.IrisCondition,
                            HasIrisNeovascularization = request.GlaucomaRecord.HasIrisNeovascularization,
                            PupilDiameter = request.GlaucomaRecord.PupilDiameter,
                            PupilPigmentBorder = request.GlaucomaRecord.PupilPigmentBorder,
                            PupilReflexResponse = request.GlaucomaRecord.PupilReflexResponse,
                            LensStatus = request.GlaucomaRecord.LensStatus,
                            FundusRetinaFindings = request.GlaucomaRecord.FundusRetinaFindings,
                            FundusMaculaFindings = request.GlaucomaRecord.FundusMaculaFindings,
                            HasCNV = request.GlaucomaRecord.HasCNV,
                            HasRetinalHemorrhage = request.GlaucomaRecord.HasRetinalHemorrhage,
                            OpticDiscDescription = request.GlaucomaRecord.OpticDiscDescription,
                            NerveRimOd = request.GlaucomaRecord.NerveRimOd,
                            NerveRimOs = request.GlaucomaRecord.NerveRimOs,
                            OpticDiscCupRatio = request.GlaucomaRecord.OpticDiscCupRatio,
                            OpticDiscVesselChange = request.GlaucomaRecord.OpticDiscVesselChange,
                            HasOpticDiscHemorrhage = request.GlaucomaRecord.HasOpticDiscHemorrhage,
                            HasRimAtrophy = request.GlaucomaRecord.HasRimAtrophy,
                            EyeAxialLength = request.GlaucomaRecord.EyeAxialLength,

                            // Treatment Plan
                            TreatmentPlanSurgery = request.GlaucomaRecord.TreatmentPlanSurgery,
                            TreatmentPlanLaser = request.GlaucomaRecord.TreatmentPlanLaser,
                            TreatmentPlanMedication = request.GlaucomaRecord.TreatmentPlanMedication,
                            FollowUpPlan = request.GlaucomaRecord.FollowUpPlan
                        };
                        context.GlaucomaRecords.Add(glaucomaRecord);

                        // Add glaucoma histories
                        if (request.GlaucomaHistories != null && request.GlaucomaHistories.Count > 0)
                        {
                            foreach (var history in request.GlaucomaHistories)
                            {
                                Enum.TryParse<EyeSide>(history.EyeSide ?? "", true, out var historySide);
                                context.GlaucomaHistories.Add(new GlaucomaHistory
                                {
                                    Id = Guid.NewGuid(),
                                    GlaucomaRecordId = glaucomaRecord.Id,
                                    HistoryType = history.HistoryType,
                                    Side = historySide,
                                    AttemptNumber = history.AttemptNumber,
                                    ProcedureType = history.ProcedureType,
                                    ProcedureDate = history.ProcedureDate,
                                    FacilityLevel = history.FacilityLevel,
                                    DrugName = history.DrugName,
                                    Dosage = history.Dosage,
                                    Duration = history.Duration,
                                    Route = history.Route,
                                    ChangeReason = history.ChangeReason
                                });
                            }
                        }
                    }
                    break;

                case RecordType.MS25_STRABISMUS_PTOSIS:
                    if (request.StrabismusPtosisRecord != null)
                    {
                        context.StrabismusPtosisRecords.Add(new StrabismusPtosisRecord
                        {
                            Id = Guid.NewGuid(),
                            RecordId = recordId,

                            // Chief complaint & cause
                            ChiefStrabismus = request.StrabismusPtosisRecord.ChiefStrabismus,
                            ChiefPtosis = request.StrabismusPtosisRecord.ChiefPtosis,
                            Congenital = request.StrabismusPtosisRecord.Congenital,
                            Acquired = request.StrabismusPtosisRecord.Acquired,
                            AcquiredOnset = request.StrabismusPtosisRecord.AcquiredOnset,

                            // Strabismus type
                            StrabismusType = request.StrabismusPtosisRecord.StrabismusType,

                            // Nystagmus
                            Nystagmus = request.StrabismusPtosisRecord.Nystagmus,
                            NystagmusType = request.StrabismusPtosisRecord.NystagmusType,

                            // Treatment history
                            PriorAmblyopiaTreatment = request.StrabismusPtosisRecord.PriorAmblyopiaTreatment,
                            PriorAmblyopiaResult = request.StrabismusPtosisRecord.PriorAmblyopiaResult,
                            PriorSurgery = request.StrabismusPtosisRecord.PriorSurgery,
                            PriorSurgeryResult = request.StrabismusPtosisRecord.PriorSurgeryResult,

                            // Visual acuity before/after atropine
                            VaBeforeAtropineOd = request.StrabismusPtosisRecord.VaBeforeAtropineOd,
                            VaBeforeAtropineOs = request.StrabismusPtosisRecord.VaBeforeAtropineOs,
                            VaAfterAtropineOd = request.StrabismusPtosisRecord.VaAfterAtropineOd,
                            VaAfterAtropineOs = request.StrabismusPtosisRecord.VaAfterAtropineOs,

                            // Refraction
                            RefractionPreAtropine = request.StrabismusPtosisRecord.RefractionPreAtropine,
                            RefractionPostAtropine = request.StrabismusPtosisRecord.RefractionPostAtropine,

                            // Pupil shadow test
                            PupilShadowTestOd = request.StrabismusPtosisRecord.PupilShadowTestOd,
                            PupilShadowTestOs = request.StrabismusPtosisRecord.PupilShadowTestOs,

                            // Extraocular motility
                            EomGazeTest = request.StrabismusPtosisRecord.EomGazeTest,
                            EomInternalOd = request.StrabismusPtosisRecord.EomInternalOd,
                            EomInternalOs = request.StrabismusPtosisRecord.EomInternalOs,
                            ConvergencePoint = request.StrabismusPtosisRecord.ConvergencePoint,

                            // Cover test
                            CoverTestResult = request.StrabismusPtosisRecord.CoverTestResult,

                            // Hirschberg & Prism
                            HirschbergBeforeAtropine = request.StrabismusPtosisRecord.HirschbergBeforeAtropine,
                            HirschbergAfterAtropine = request.StrabismusPtosisRecord.HirschbergAfterAtropine,
                            PrismNear = request.StrabismusPtosisRecord.PrismNear,
                            PrismDistance = request.StrabismusPtosisRecord.PrismDistance,
                            PrismUp = request.StrabismusPtosisRecord.PrismUp,
                            PrismDown = request.StrabismusPtosisRecord.PrismDown,

                            // Syndrome & synoptophore
                            StrabismusSyndrome = request.StrabismusPtosisRecord.StrabismusSyndrome,
                            SynoptophoreObjective = request.StrabismusPtosisRecord.SynoptophoreObjective,
                            SynoptophoreSubjective = request.StrabismusPtosisRecord.SynoptophoreSubjective,

                            // Binocular vision
                            BinocularStatus = request.StrabismusPtosisRecord.BinocularStatus,
                            FusionAmplitude = request.StrabismusPtosisRecord.FusionAmplitude,
                            RetinalCorrespondence = request.StrabismusPtosisRecord.RetinalCorrespondence,
                            Diplopia = request.StrabismusPtosisRecord.Diplopia,
                            CompensatoryHeadPosture = request.StrabismusPtosisRecord.CompensatoryHeadPosture,

                            // Ptosis measurements
                            PtosisDegreeOd = request.StrabismusPtosisRecord.PtosisDegreeOd,
                            PtosisDegreeOs = request.StrabismusPtosisRecord.PtosisDegreeOs,
                            LevatorFunctionOd = request.StrabismusPtosisRecord.LevatorFunctionOd,
                            LevatorFunctionOs = request.StrabismusPtosisRecord.LevatorFunctionOs,
                            MarcusGunn = request.StrabismusPtosisRecord.MarcusGunn,
                            BellPhenomenon = request.StrabismusPtosisRecord.BellPhenomenon,
                            FixationOd = request.StrabismusPtosisRecord.FixationOd,
                            FixationOs = request.StrabismusPtosisRecord.FixationOs,
                            PalpebralReflexOd = request.StrabismusPtosisRecord.PalpebralReflexOd,
                            PalpebralReflexOs = request.StrabismusPtosisRecord.PalpebralReflexOs
                        });
                    }
                    break;

                case RecordType.MS26_PEDIATRIC:
                    // Lacrimal record for MS26
                    if (request.LacrimalRecord != null)
                    {
                        Enum.TryParse<EyeSide>(request.LacrimalRecord.Side, true, out var lacrimalSideMs26);
                        context.LacrimalRecords.Add(new LacrimalRecord
                        {
                            Id = Guid.NewGuid(),
                            RecordId = recordId,
                            Side = lacrimalSideMs26,
                            IrrigationFree = request.LacrimalRecord.IrrigationFree,
                            IrrigationRegurgitationSame = request.LacrimalRecord.IrrigationRegurgitationSame,
                            IrrigationRegurgitationOpposite = request.LacrimalRecord.IrrigationRegurgitationOpposite,
                            IrrigationNote = request.LacrimalRecord.IrrigationNote,
                            LacrimalOther = request.LacrimalRecord.LacrimalOther
                        });
                    }

                    // Pediatric record for MS26
                    if (request.PediatricRecord != null)
                    {
                        context.PediatricEyeRecords.Add(new PediatricEyeRecord
                        {
                            Id = Guid.NewGuid(),
                            RecordId = recordId,

                            // History
                            Congenital = request.PediatricRecord.Congenital,
                            Acquired = request.PediatricRecord.Acquired,
                            AcquiredOnset = request.PediatricRecord.AcquiredOnset,
                            PriorTreatment = request.PediatricRecord.PriorTreatment,
                            PregnancyIllness = request.PediatricRecord.PregnancyIllness,
                            PregnancyIllnessDetail = request.PediatricRecord.PregnancyIllnessDetail,
                            IntellectualDevelopmentNormal = request.PediatricRecord.IntellectualDevelopmentNormal,
                            ChiefSymptoms = request.PediatricRecord.ChiefSymptoms,

                            // Eyelid conditions
                            EntropionOd = request.PediatricRecord.EntropionOd,
                            EpicanthusOd = request.PediatricRecord.EpicanthusOd,
                            PtosisOd = request.PediatricRecord.PtosisOd,
                            EyelidTumor = request.PediatricRecord.EyelidTumor,
                            EyelidTumorLocation = request.PediatricRecord.EyelidTumorLocation,
                            EyelidTumorSize = request.PediatricRecord.EyelidTumorSize,

                            // Eyeball status
                            EyeballOdStatus = request.PediatricRecord.EyeballOdStatus,
                            EyeballOsStatus = request.PediatricRecord.EyeballOsStatus,
                            EyeballTexture = request.PediatricRecord.EyeballTexture,

                            // Amblyopia
                            AmblyopiaStatus = request.PediatricRecord.AmblyopiaStatus,
                            FixationPreferenceOd = request.PediatricRecord.FixationPreferenceOd,
                            FixationPreferenceOs = request.PediatricRecord.FixationPreferenceOs,

                            // Fundus summary
                            FundusSummaryOd = request.PediatricRecord.FundusSummaryOd,
                            FundusSummaryOs = request.PediatricRecord.FundusSummaryOs,

                            // Developmental status
                            IntellectualDevelopmentStatus = request.PediatricRecord.IntellectualDevelopmentStatus,
                            GeneralHealthStatus = request.PediatricRecord.GeneralHealthStatus
                        });
                    }
                    break;
            }
        }

        /// <summary>
        /// Creates prescription and optional glasses prescription for the medical record.
        /// </summary>
        /// <param name="context">Database context for adding entities.</param>
        /// <param name="recordId">The parent medical record ID.</param>
        /// <param name="doctorId">The prescribing doctor ID.</param>
        /// <param name="request">The medical record creation request with prescription data.</param>
        private async Task CreatePrescriptionAsync(AppDbContext context, Guid recordId, Guid doctorId, CreateMedicalRecordRequest request)
        {
            if (request.Prescription == null || request.PrescriptionItems == null || request.PrescriptionItems.Count == 0) return;
            var prescription = new Prescription { Id = Guid.NewGuid(), RecordId = recordId, DoctorId = doctorId, Notes = request.Prescription.Notes, CreatedAt = DateTime.UtcNow };
            context.Prescriptions.Add(prescription);
            foreach (var item in request.PrescriptionItems) { context.PrescriptionItems.Add(new PrescriptionItem { Id = Guid.NewGuid(), PrescriptionId = prescription.Id, MedicineName = item.MedicineName, Dosage = item.Dosage, Frequency = item.Frequency, DurationDays = item.DurationDays, Quantity = item.Quantity, Instruction = item.Instruction }); }
            if (request.GlassesPrescription != null)
            {
                context.GlassesPrescriptions.Add(new GlassesPrescription
                {
                    Id = Guid.NewGuid(),
                    RecordId = recordId,
                    DoctorId = doctorId,
                    SphOd = request.GlassesPrescription.SphOd,
                    CylOd = request.GlassesPrescription.CylOd,
                    AxisOd = request.GlassesPrescription.AxisOd,
                    AddOd = request.GlassesPrescription.AddOd,
                    SphOs = request.GlassesPrescription.SphOs,
                    CylOs = request.GlassesPrescription.CylOs,
                    AxisOs = request.GlassesPrescription.AxisOs,
                    AddOs = request.GlassesPrescription.AddOs,
                    Pd = request.GlassesPrescription.Pd,
                    LensType = request.GlassesPrescription.LensType,
                    Notes = request.GlassesPrescription.Notes,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Updates appointment and queue statuses after successful medical record creation.
        /// </summary>
        /// <param name="state">Execution state containing the appointment to update.</param>
        private async Task UpdateStatusesAsync(ExecutionState state)
        {
            if (state.HasError || state.Appointment == null) return;
            try
            {
                state.Appointment.Status = AppointmentStatus.IN_PROGRESS;
                state.Appointment.UpdatedAt = DateTime.UtcNow;
                _context.Appointments.Update(state.Appointment);
                var queue = await _context.Queues.FirstOrDefaultAsync(q => q.AppointmentId == state.AppointmentId);
                if (queue != null)
                {
                    queue.Status = QueueStatus.COMPLETED;
                    queue.CompletedAt = DateTime.UtcNow;
                    _context.Queues.Update(queue);
                }
                await _context.SaveChangesAsync();
            }
            catch { /* Non-critical error, just log */ }
        }

        /// <summary>
        /// Creates the API response based on execution state.
        /// </summary>
        /// <param name="state">Execution state containing result or error information.</param>
        /// <returns>Success or failure API response.</returns>
        private ApiResponse<CreateMedicalRecordResponse> CreateResponse(ExecutionState state)
        {
            if (state.HasError)
            {
                return ApiResponse<CreateMedicalRecordResponse>.Fail(state.ErrorCode ?? GeneralCode.APP_MESSAGE_4001.ToString());
            }
            var response = new CreateMedicalRecordResponse
            {
                MedicalRecordId = state.CreatedMedicalRecord?.Id.ToString() ?? string.Empty,
                PatientName = state.PatientName,
                RecordTypeLabel = state.RecordTypeLabel,
                AppointmentDate = state.Appointment?.AppointmentDate.ToString("dd/MM/yyyy"),
                DoctorName = state.DoctorName,
                CreatedAt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm"),
                IsSuccess = true
            };
            return ApiResponse<CreateMedicalRecordResponse>.Success(GeneralCode.APP_MESSAGE_2005.ToString(), response);
        }

        #region Mapping Methods

        /// <summary>
        /// Maps eye basic exam data to EyeExamBasic entity.
        /// </summary>
        /// <param name="data">Eye basic exam data from request.</param>
        /// <param name="recordId">Parent medical record ID.</param>
        /// <param name="side">Eye side (RIGHT or LEFT).</param>
        /// <returns>Mapped EyeExamBasic entity.</returns>
        private static EyeExamBasic MapEyeExamBasic(EyeBasicExamData data, Guid recordId, EyeSide side)
        {
            return new EyeExamBasic
            {
                Id = Guid.NewGuid(),
                RecordId = recordId,
                Side = side,
                VaUncorrected = ParseDecimal(data.VaUncorrected),
                VaCorrected = ParseDecimal(data.VaCorrected),
                VaNear = ParseDecimal(data.VaNear),
                VaPinhole = ParseDecimal(data.VaPinhole),
                IopMmhg = ParseDecimal(data.IopMmhg),
                IopMethod = data.IopMethod,
                AutoRefraction = data.AutoRefraction,
                Retinoscopy = data.Retinoscopy,
                SubjectiveRefraction = data.SubjectiveRefraction,
                VisualField = data.VisualField,
                EomNormal = string.IsNullOrEmpty(data.EomStatus) || data.EomStatus == "Bình thường",
                EomNote = data.EomNote,
                Nystagmus = !string.IsNullOrEmpty(data.Nystagmus) && data.Nystagmus != "Không",
                NystagmusType = data.NystagmusType
            };
        }

        /// <summary>
        /// Maps eyelid and conjunctiva exam data to EyeEyelidConjunctiva entity.
        /// </summary>
        /// <param name="data">Eyelid and conjunctiva exam data from request.</param>
        /// <param name="recordId">Parent medical record ID.</param>
        /// <param name="side">Eye side (RIGHT or LEFT).</param>
        /// <returns>Mapped EyeEyelidConjunctiva entity.</returns>
        private static EyeEyelidConjunctiva MapEyeEyelidConjunctiva(EyeEyelidData data, Guid recordId, EyeSide side)
        {
            return new EyeEyelidConjunctiva
            {
                Id = Guid.NewGuid(),
                RecordId = recordId,
                Side = side,
                EyelidNormal = string.IsNullOrEmpty(data.Status) || data.Status == "Bình thường",
                EyelidEdema = data.Status?.Contains("Phù") == true || data.Status?.Contains("Sưng") == true,
                EyelidHemorrhage = data.Status?.Contains("Tụ máu") == true,
                Ptosis = data.Ptosis == true,
                PtosisDegree = data.PtosisDegree,
                Laceration = data.Laceration == true,
                LacerationExtent = data.LacerationExtent,
                LacerationDepth = data.LacerationLocation,
                LacerationSutured = data.LacerationSutured == true,
                LacerationUnsutured = data.LacerationUnsutured == true,
                Entropion = data.Entropion == true,
                Epicanthus = data.Epicanthus == true,
                EntropionPediatric = data.Entropion == true,
                Lagophthalmos = data.Lagophthalmos == true,
                Scar = data.Scar == true,
                Chalazion = !string.IsNullOrEmpty(data.ChalazionHordeolum),
                Hordeolum = !string.IsNullOrEmpty(data.ChalazionHordeolum),
                HasTumor = data.HasTumor == true,
                TumorNature = data.TumorNature,
                TumorLocation = data.TumorLocation,
                TumorSize = data.TumorSize,
                LacrimalDuctNormal = data.LacrimalDuctStatus == "Bình thường",
                LacrimalDuctCut = data.LacrimalDuctStatus == "Đứt lệ quản",
                LacrimalDuctCutLocation = data.LacrimalDuctLocation,
                EyelidOther = data.OtherFindings
            };
        }

        /// <summary>
        /// Maps cornea exam data to EyeCornea entity.
        /// </summary>
        /// <param name="data">Cornea exam data from request.</param>
        /// <param name="recordId">Parent medical record ID.</param>
        /// <param name="side">Eye side (RIGHT or LEFT).</param>
        /// <returns>Mapped EyeCornea entity.</returns>
        private static EyeCornea MapEyeCornea(EyeCorneaExamData data, Guid recordId, EyeSide side)
        {
            return new EyeCornea
            {
                Id = Guid.NewGuid(),
                RecordId = recordId,
                Side = side,
                Clarity = data.Clarity,
                Size = data.Size,
                Shape = data.Shape,
                DiameterMm = data.DiameterMm,
                Sensation = data.Sensation,
                EpitheliumPunctate = data.EpitheliumPunctate == true,
                EpitheliumBullous = data.EpitheliumEdemaLevel,
                EpitheliumLoss = data.EpitheliumLoss,
                BandKeratopathy = !string.IsNullOrEmpty(data.EpitheliumStatus),
                StromaEdema = data.StromaEdemaLevel,
                StromaInfiltrate = data.StromaInfiltrate,
                StromaThinning = data.StromaThinning,
                Ulcer = data.Ulcer == true,
                UlcerLocation = data.UlcerLocation,
                UlcerSize = data.UlcerSize,
                EndotheliumFolds = data.UlcerDescription,
                PosteriorSurfaceDeposit = data.PosteriorDeposit,
                PosteriorDepositLocation = data.PosteriorDepositLocation,
                Perforation = data.Perforation == true,
                PerforationDiameterMm = data.PerforationDiameterMm,
                PerforationLocation = data.PerforationLocation,
                SeidelTest = data.SeidelTest,
                Laceration = data.Laceration == true,
                LacerationSutured = data.LacerationSutured,
                LacerationLocation = data.LacerationLocation,
                LacerationSize = data.LacerationSize,
                LacerationType = data.LacerationType,
                TissueEntrapped = data.LacerationType?.Contains("Kẹt") == true,
                Neovascularization = data.Neovascularization == true,
                NeovascularizationLocation = data.NeovascularizationDepth,
                NeovascularizationExtent = data.NeovascularizationExtent,
                LimbalStemDeficiency = !string.IsNullOrEmpty(data.LimbalStatus)
            };
        }

        /// <summary>
        /// Maps anterior chamber and iris/pupil exam data to EyeAcIris entity.
        /// </summary>
        /// <param name="acData">Anterior chamber exam data from request.</param>
        /// <param name="irisData">Iris/pupil exam data from request.</param>
        /// <param name="recordId">Parent medical record ID.</param>
        /// <param name="side">Eye side (RIGHT or LEFT).</param>
        /// <returns>Mapped EyeAcIris entity.</returns>
        private static EyeAcIris MapEyeAcIris(EyeAnteriorChamberData? acData, EyeIrisPupilData? irisData, Guid recordId, EyeSide side)
        {
            return new EyeAcIris
            {
                Id = Guid.NewGuid(),
                RecordId = recordId,
                Side = side,

                // Anterior Chamber
                AcDepthMm = acData?.DepthMm,
                AcDepthHerick = acData?.HerickClassification,
                AcFlat = acData?.Depth == "Xẹp tiền phòng",
                AcLensMaterial = acData?.VitreousInAC == true,
                AcPusMm = acData?.Pus == true ? acData.PusMm : null,
                AcTyndall = acData?.Tyndall,
                AcHemorrhage = acData?.Hemorrhage == true,
                AcOtherFindings = acData?.OtherFindings,

                // Iris
                IrisColor = irisData?.IrisColor,
                IrisCondition = irisData?.IrisCondition,
                IrisDegeneration = irisData?.IrisDegeneration == true || irisData?.IrisCondition == "Thoái hóa",
                IrisNeovascularization = irisData?.IrisNeovascularization == true,
                IrisCiliaryProcesses = irisData?.IrisCiliaryProcesses == true,
                IrisKoeppeNodules = irisData?.KoeppeNodules == true,
                IrisBusaccaNodules = irisData?.BusaccaNodules == true,
                IrisRootTear = irisData?.IrisRootTear == true,
                IrisRootTearDegree = irisData?.IrisRootTearDegree,
                IrisLoss = irisData?.IrisLoss == true,
                IrisPerforation = irisData?.IrisPerforation == true,

                // Pupil
                PupilRound = irisData?.PupilShape == "Tròn" || string.IsNullOrEmpty(irisData?.PupilShape),
                PupilIrregular = irisData?.PupilShape == "Méo",
                PupilDiameterMm = irisData?.PupilDiameterMm,
                PupilSychiae = irisData?.PupilShape == "Dính",
                PupilSynechiaeLocation = irisData?.PupilPosition,
                PupilReflex = irisData?.PupilReflex,
                PupilLightReflex = irisData?.PupilReflex,
                PupilPtdtTest = irisData?.PtdtTest == true ? "Có" : "Không",
                PupilDilated = irisData?.PupilDilated == true,
                PupilParalyzed = irisData?.PupilReflex == "Mất",
                FundusReflex = irisData?.FundusReflex
            };
        }

        /// <summary>
        /// Maps lens and vitreous exam data to EyeLensVitreous entity.
        /// </summary>
        /// <param name="lensData">Lens exam data from request.</param>
        /// <param name="vitreousData">Vitreous exam data from request.</param>
        /// <param name="recordId">Parent medical record ID.</param>
        /// <param name="side">Eye side (RIGHT or LEFT).</param>
        /// <returns>Mapped EyeLensVitreous entity.</returns>
        private static EyeLensVitreous MapEyeLensVitreous(EyeLensData? lensData, EyeVitreousData? vitreousData, Guid recordId, EyeSide side)
        {
            return new EyeLensVitreous
            {
                Id = Guid.NewGuid(),
                RecordId = recordId,
                Side = side,

                // Lens
                LensClear = string.IsNullOrEmpty(lensData?.Status) || lensData?.Status == "Trong",
                LensOpacityType = lensData?.OpacityType,
                LensOpacityLocation = lensData?.OpacityLocation,
                LensRupture = lensData?.Status == "Vỡ",
                LensSubluxation = lensData?.Subluxation == true,
                LensIntoAnterior = lensData?.LensInAnterior == true,
                LensIntoVitreous = lensData?.LensInVitreous == true,
                LensPurulent = lensData?.Purulent == true,
                LensAnteriorPigmentation = lensData?.AnteriorPigmentation == true,
                LensIolPresent = lensData?.IolPresent == true,
                LensIolStatus = lensData?.IolStatus,
                LensIolPosition = lensData?.IolPosition,

                // Vitreous
                VitreousClear = string.IsNullOrEmpty(vitreousData?.Status) || vitreousData?.Status == "Sạch",
                VitreousOpacity = vitreousData?.Status == "Đục",
                VitreousHemorrhage = vitreousData?.Hemorrhage == true || vitreousData?.Status == "Xuất huyết",
                VitreousPvd = vitreousData?.Pvd == true,
                VitreousTyndall = vitreousData?.Tyndall,
                VitreousOrganized = vitreousData?.Organized == true,
                VitreousPurulent = vitreousData?.Purulent == true || vitreousData?.Status == "Viêm mủ",
                VitreousForeignBody = vitreousData?.ForeignBody == true
            };
        }

        /// <summary>
        /// Maps sclera exam data to EyeSclera entity.
        /// </summary>
        /// <param name="data">Sclera exam data from request.</param>
        /// <param name="recordId">Parent medical record ID.</param>
        /// <param name="side">Eye side (RIGHT or LEFT).</param>
        /// <returns>Mapped EyeSclera entity.</returns>
        private static EyeSclera MapEyeSclera(EyeScleraExamData data, Guid recordId, EyeSide side)
        {
            return new EyeSclera
            {
                Id = Guid.NewGuid(),
                RecordId = recordId,
                Side = side,
                ScleraNormal = string.IsNullOrEmpty(data.Status) || data.Status == "Bình thường",
                ScleraEctasia = data.Status == "Giãn lồi",
                ScleraLaceration = data.Laceration == true,
                ScleraLacerationSutured = data.LacerationSutured,
                ScleraLacerationLocation = data.LacerationLocation,
                ScleraLacerationSize = data.LacerationSize,
                ScleraTissueEntrapped = data.TissueEntrapped == true,
                OldSurgeryScar = data.Status == "Sẹo"
            };
        }

        /// <summary>
        /// Maps fundus disc and macula exam data to EyeFundusDiscMacula entity.
        /// </summary>
        /// <param name="data">Fundus disc and macula exam data from request.</param>
        /// <param name="recordId">Parent medical record ID.</param>
        /// <param name="side">Eye side (RIGHT or LEFT).</param>
        /// <returns>Mapped EyeFundusDiscMacula entity.</returns>
        private static EyeFundusDiscMacula MapEyeFundusDiscMacula(EyeFundusDiscMaculaData data, Guid recordId, EyeSide side)
        {
            return new EyeFundusDiscMacula
            {
                Id = Guid.NewGuid(),
                RecordId = recordId,
                Side = side,

                // Optic Disc
                OpticDiscNormal = string.IsNullOrEmpty(data.DiscStatus) || data.DiscStatus == "Bình thường",
                OpticDiscColor = data.DiscColor,
                OpticDiscEdema = data.DiscStatus == "Phù",
                OpticDiscAtrophy = data.DiscStatus == "Teo",
                OpticDiscPallor = data.DiscStatus == "Bạc màu",
                OpticDiscCupRatio = data.CdRatio,
                OpticDiscRimStatus = data.RimStatus,
                OpticDiscRimLocation = data.RimLocation,
                OpticDiscVesselChange = data.VesselChange,
                OpticDiscHemorrhage = data.DiscHemorrhage == true,
                OpticDiscNeovascularization = data.Neovascularization == true,
                OpticDiscNotVisible = data.DiscNotVisible == true,

                // Macula
                MaculaNormal = string.IsNullOrEmpty(data.MaculaStatus) || data.MaculaStatus == "Bình thường",
                MaculaCondition = data.MaculaStatus,
                MaculaReflexAbsent = data.MaculaReflexAbsent == true || data.MaculaCondition == "Mất ánh HĐ",
                MaculaEdemaType = data.MaculaEdemaType,
                MaculaHoleDegree = data.MaculaHoleDegree,
                MaculaScar = data.MaculaScar == true,
                MaculaSerousDetachment = data.SerousDetachment == true,

                // Choroid
                ChoroidalNormal = string.IsNullOrEmpty(data.ChoroidStatus) || data.ChoroidStatus == "Bình thường",
                ChoroidalFindings = data.ChoroidFindings
            };
        }

        /// <summary>
        /// Maps fundus retina and vessel exam data to EyeFundusRetinaVessel entity.
        /// </summary>
        /// <param name="data">Fundus retina and vessel exam data from request.</param>
        /// <param name="recordId">Parent medical record ID.</param>
        /// <param name="side">Eye side (RIGHT or LEFT).</param>
        /// <returns>Mapped EyeFundusRetinaVessel entity.</returns>
        private static EyeFundusRetinaVessel MapEyeFundusRetinaVessel(EyeFundusRetinaVesselData data, Guid recordId, EyeSide side)
        {
            return new EyeFundusRetinaVessel
            {
                Id = Guid.NewGuid(),
                RecordId = recordId,
                Side = side,

                // Blood Vessels
                VesselNormal = string.IsNullOrEmpty(data.VesselStatus) || data.VesselStatus == "Bình thường",
                ArteryOcclusionType = data.ArteryOcclusion,
                VeinOcclusionType = data.VeinOcclusion,
                OcclusionType = data.OcclusionType,
                OcclusionEdema = data.RetinalEdema == true,
                OcclusionIschemia = data.OcclusionType?.Contains("thiếu máu") == true,

                // Retina
                RetinaNormal = string.IsNullOrEmpty(data.RetinaStatus) || data.RetinaStatus == "Bình thường",
                RetinalCondition = data.RetinaStatus,
                RetinaHemorrhageSuperficial = data.HemorrhageType == "VM nông",
                RetinaHemorrhageDeep = data.HemorrhageType == "VM sâu",
                RetinaExudateHard = data.ExudateType == "Cứng",
                RetinaExudateCottonWool = data.ExudateType == "Dạng bông",
                RetinaEdema = data.RetinalEdema == true,
                RetinaDegenerationPeripheral = data.Degeneration == true && data.DegenerationType == "chu biên",
                RetinaDegenerationCentral = data.Degeneration == true && data.DegenerationType == "trung tâm",
                DegenerativeType = data.DegenerationType,
                DegenerativeDescription = data.DegenerationDescription,
                RetinalDetachment = data.Detachment == true,
                RetinalDetachmentLevel = data.DetachmentLevel,
                RetinalTear = data.RetinalTear == true,
                RetinalTearCount = data.TearCount,
                RetinalTearLocation = data.TearLocation,
                RetinalTearMorphology = data.TearMorphology,

                // IOFB
                IntraocularForeignBody = data.Iofb == true,
                IofbLocation = data.IofbLocation,
                IofbSize = data.IofbSize,

                // Hemorrhage location
                HemorrhageLocation = data.HemorrhageType,

                // CNV
                ChoroidalNeovascularization = !string.IsNullOrEmpty(data.HemorrhageType) && data.HemorrhageType.Contains("Hắc mạc")
            };
        }

        /// <summary>
        /// Parses a decimal value from string.
        /// </summary>
        /// <param name="value">String value to parse.</param>
        /// <returns>Parsed decimal or null if invalid.</returns>
        private static decimal? ParseDecimal(string? value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            if (decimal.TryParse(value, out var result)) return result;
            return null;
        }

        /// <summary>
        /// Parses a nullable decimal value from string.
        /// </summary>
        /// <param name="value">String value to parse.</param>
        /// <returns>Parsed decimal or null if invalid.</returns>
        private static decimal? ParseDecimalNullable(string? value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            if (decimal.TryParse(value, out var result)) return result;
            return null;
        }

        #endregion

        /// <summary>
        /// Gets the localized label for a record type enum value.
        /// </summary>
        /// <param name="recordType">The record type enum.</param>
        /// <returns>Vietnamese label for the record type.</returns>
        private static string GetRecordTypeLabel(RecordType recordType)
        {
            return recordType switch
            {
                RecordType.MS21_TRAUMA => "Bệnh án mắt (Chấn thương)",
                RecordType.MS22_ANTERIOR => "Bệnh án mắt (Bán phần trước)",
                RecordType.MS23_FUNDUS => "Bệnh án mắt (Đáy mắt)",
                RecordType.MS24_GLAUCOMA => "Bệnh án mắt (Glôcôm)",
                RecordType.MS25_STRABISMUS_PTOSIS => "Bệnh án mắt (Lác, sụp mi)",
                RecordType.MS26_PEDIATRIC => "Bệnh án mắt (Mắt trẻ em)",
                _ => recordType.ToString()
            };
        }
    }
}
