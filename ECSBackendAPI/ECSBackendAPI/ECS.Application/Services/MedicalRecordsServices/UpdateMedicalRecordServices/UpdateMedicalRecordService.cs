using System.Security.Claims;
using ECS.Application.Common.Response;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.EyeExaminations;
using ECS.Domain.Entities.Prescriptions;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.SubspecialtyRecords;
using ECS.Domain.Enums;
using ECS.Infrastructure.Persistence;
using ECS.Infrastructure.Repositories.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECS.Application.Services.MedicalRecordsServices.UpdateMedicalRecordServices
{
    /// <summary>
    /// Service implementation for updating medical records.
    /// UC41 - Edit Medical Record
    /// Handles updates to medical record with all related eye examinations and prescriptions.
    /// Supports concurrency conflict handling with DbUpdateConcurrencyException.
    /// </summary>
    public class UpdateMedicalRecordService : IUpdateMedicalRecordService
    {
        private readonly IRepositoryBaseAsync<MedicalRecord, Guid, AppDbContext> _medicalRecordRepository;
        private readonly IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> _doctorRepository;
        private readonly IValidator<UpdateMedicalRecordRequest> _validator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;

        public UpdateMedicalRecordService(
            IRepositoryBaseAsync<MedicalRecord, Guid, AppDbContext> medicalRecordRepository,
            IRepositoryQueryBase<DoctorProfile, Guid, AppDbContext> doctorRepository,
            IValidator<UpdateMedicalRecordRequest> validator,
            IHttpContextAccessor httpContextAccessor,
            IServiceProvider serviceProvider)
        {
            _medicalRecordRepository = medicalRecordRepository;
            _doctorRepository = doctorRepository;
            _validator = validator;
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
        }

        private AppDbContext GetDbContext() =>
            _serviceProvider.GetRequiredService<AppDbContext>();

        /// <summary>
        /// Main orchestration method for medical record update.
        /// </summary>
        /// <param name="recordId">The medical record ID to update.</param>
        /// <param name="request">The update medical record request containing all record data.</param>
        /// <returns>API response with updated medical record info or error details.</returns>
        public async Task<ApiResponse<UpdateMedicalRecordResponse>> Process(Guid recordId, UpdateMedicalRecordRequest request)
        {
            var state = new ExecutionState();
            // Step 1: Validate incoming request data
            ValidateRequest(request, state);
            // Step 2: Extract authenticated user ID from JWT token
            RetrieveAuthenticatedUserId(state);
            // Step 3: Get doctor profile
            await GetDoctorProfileAsync(state);
            // Step 4: Fetch existing medical record with all related entities
            await FetchMedicalRecordAsync(recordId, state);
            // Step 5: Verify doctor has permission to edit this record
            VerifyDoctorPermission(state);
            // Step 6: Check if record is locked
            CheckRecordLock(state);
            // Step 7: Apply updates to the medical record
            ApplyUpdates(request, state);
            // Step 8: Persist changes and handle concurrency
            await PersistChangesAsync(state);
            // Step 9: Build and return the response
            return CreateResponse(state);
        }

        /// <summary>
        /// Holds all mutable execution state for the update process flow.
        /// Uses ExecutionState pattern for clean state management across pipeline steps.
        /// </summary>
        private class ExecutionState
        {
            public bool IsValidationPassed { get; set; } = true;
            public bool IsUserValid { get; set; } = true;
            public bool IsDoctorExists { get; set; } = true;
            public bool IsAuthorized { get; set; } = true;
            public bool IsRecordLocked { get; set; } = false;
            public bool IsRecordFound { get; set; } = true;
            public bool HasConcurrencyError { get; set; } = false;
            public bool HasError { get; set; } = false;
            public bool IsExecutionSuccess { get; set; } = true;
            public Guid ActiveUserId { get; set; }
            public DoctorProfile? DoctorProfile { get; set; }
            public MedicalRecord? MedicalRecord { get; set; }
            public string? ErrorCode { get; set; }
            public string? PatientName { get; set; }
            public string? DoctorName { get; set; }
            public string? RecordTypeLabel { get; set; }
        }

        /// <summary>
        /// Validates the incoming request using FluentValidation rules.
        /// </summary>
        /// <param name="request">The medical record update request to validate.</param>
        /// <param name="state">Execution state to store validation result.</param>
        private void ValidateRequest(UpdateMedicalRecordRequest request, ExecutionState state)
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
            var isValidUserId = parseResult && parsedUserId != Guid.Empty;
            state.IsUserValid = isValidUserId;
            state.ActiveUserId = isValidUserId ? parsedUserId : Guid.Empty;
            state.HasError = !isValidUserId;
            state.ErrorCode = isValidUserId ? null : GeneralCode.APP_MESSAGE_4033.ToString();
        }

        /// <summary>
        /// Retrieves the doctor profile for the authenticated user.
        /// </summary>
        /// <param name="state">Execution state containing active user ID.</param>
        private async Task GetDoctorProfileAsync(ExecutionState state)
        {
            if (state.HasError) return;
            var doctorProfile = await _doctorRepository
                .FindByCondition(d => d.UserId == state.ActiveUserId && d.IsActive, trackChanges: false)
                .Include(d => d.User)
                .FirstOrDefaultAsync();
            state.DoctorProfile = doctorProfile;
            state.IsDoctorExists = doctorProfile != null;
            state.DoctorName = doctorProfile?.User?.FullName;
            state.HasError = state.HasError || !state.IsDoctorExists;
            state.ErrorCode = state.IsDoctorExists ? state.ErrorCode : GeneralCode.APP_MESSAGE_4011.ToString();
        }

        /// <summary>
        /// Retrieves and validates the medical record with all related entities from database.
        /// Includes: Patient, Doctor, EyeExaminations, SubspecialtyRecords, Prescriptions.
        /// </summary>
        /// <param name="recordId">The medical record ID to fetch.</param>
        /// <param name="state">Execution state to store fetched record.</param>
        private async Task FetchMedicalRecordAsync(Guid recordId, ExecutionState state)
        {
            if (state.HasError) return;

            var medicalRecord = await _medicalRecordRepository
                .FindByCondition(r => r.Id == recordId, trackChanges: true)
                .AsSplitQuery()
                .Include(r => r.Patient)
                .Include(r => r.Doctor)
                    .ThenInclude(d => d.User)
                .Include(r => r.Appointment)
                .Include(r => r.Extras)
                .Include(r => r.EyeExamBasics!)
                .Include(r => r.EyeEyelidConjunctivae!)
                .Include(r => r.EyeCorneas!)
                .Include(r => r.EyeAcIrises!)
                .Include(r => r.EyeLensVitreouses!)
                .Include(r => r.EyeScleras!)
                .Include(r => r.EyeFundusDiscMaculas!)
                .Include(r => r.EyeFundusRetinaVessels!)
                .Include(r => r.TraumaRecord!)
                    .ThenInclude(t => t.Surgeries)
                .Include(r => r.GlaucomaRecord!)
                    .ThenInclude(g => g.Histories)
                .Include(r => r.StrabismusPtosisRecord)
                .Include(r => r.PediatricRecord)
                .Include(r => r.LacrimalRecords)
                .Include(r => r.OctResults)
                .Include(r => r.VisualFieldTests)
                .Include(r => r.UltrasoundEyes)
                .Include(r => r.Prescriptions!)
                    .ThenInclude(p => p.Items)
                .Include(r => r.GlassesPrescriptions)
                .Include(r => r.DocumentAccessPermissions!)
                    .ThenInclude(d => d.GrantedToUser)
                .Include(r => r.DocumentAccessPermissions!)
                    .ThenInclude(d => d.GrantedByUser)
                .FirstOrDefaultAsync();

            state.MedicalRecord = medicalRecord;
            state.IsRecordFound = medicalRecord != null;
            state.PatientName = medicalRecord?.Patient?.FullName;
            state.HasError = state.HasError || !state.IsRecordFound;
            state.ErrorCode = state.IsRecordFound ? state.ErrorCode : GeneralCode.APP_MESSAGE_4028.ToString();
        }

        /// <summary>
        /// Verifies if the doctor has permission to edit the medical record.
        /// Currently only the original creator can edit the record.
        /// </summary>
        /// <param name="state">Execution state containing medical record and doctor profile.</param>
        private void VerifyDoctorPermission(ExecutionState state)
        {
            if (!state.IsRecordFound || state.MedicalRecord == null || state.HasError) return;

            // Check if the doctor is the creator of the record or is authorized
            var isCreator = state.MedicalRecord.DoctorId == state.DoctorProfile?.Id;

            // For now, only the creator can edit the record
            // This can be extended to include other authorization rules
            state.IsAuthorized = isCreator;
            state.HasError = state.HasError || !state.IsAuthorized;
            state.ErrorCode = state.IsAuthorized ? state.ErrorCode : GeneralCode.APP_MESSAGE_4014.ToString();
        }

        /// <summary>
        /// Checks if the medical record is locked and cannot be edited.
        /// </summary>
        /// <param name="state">Execution state containing medical record.</param>
        private void CheckRecordLock(ExecutionState state)
        {
            if (!state.IsRecordFound || state.MedicalRecord == null || state.HasError) return;
            state.IsRecordLocked = state.MedicalRecord.IsLocked;
            if (state.IsRecordLocked)
            {
                state.HasError = true;
                state.ErrorCode = GeneralCode.APP_MESSAGE_4014.ToString();
            }
        }

        /// <summary>
        /// Applies the update request values to the medical record entity.
        /// Updates main record fields, diagnosis, vital signs, eye examinations, and extras (JSONB).
        /// </summary>
        /// <param name="request">The update request containing new values.</param>
        /// <param name="state">Execution state containing the medical record to update.</param>
        private void ApplyUpdates(UpdateMedicalRecordRequest request, ExecutionState state)
        {
            if (state.HasError || state.MedicalRecord == null) return;

            var record = state.MedicalRecord;

            // Update record type if provided
            if (!string.IsNullOrEmpty(request.RecordType) && Enum.TryParse<RecordType>(request.RecordType, true, out var recordType))
            {
                record.RecordType = recordType;
                state.RecordTypeLabel = GetRecordTypeLabel(recordType);
            }

            // Update chief complaint and history
            if (request.ChiefComplaint != null) record.ChiefComplaint = request.ChiefComplaint;
            if (request.IllnessDayNumber.HasValue) record.IllnessDayNumber = request.IllnessDayNumber;
            if (request.MedicalHistory != null) record.MedicalHistory = request.MedicalHistory;
            if (request.PersonalHistoryEye != null) record.PersonalHistoryEye = request.PersonalHistoryEye;
            if (request.PersonalHistorySystemic != null) record.PersonalHistorySystemic = request.PersonalHistorySystemic;
            if (request.FamilyHistory != null) record.FamilyHistory = request.FamilyHistory;

            // Update vital signs
            if (request.VitalPulse.HasValue) record.VitalPulse = request.VitalPulse;
            if (request.VitalTemperature.HasValue) record.VitalTemperature = request.VitalTemperature;
            if (request.VitalBloodPressure != null) record.VitalBloodPressure = request.VitalBloodPressure;
            if (request.VitalRespiratoryRate.HasValue) record.VitalRespiratoryRate = request.VitalRespiratoryRate;
            if (request.VitalWeightKg.HasValue) record.VitalWeightKg = request.VitalWeightKg;

            // Update systemic exam
            if (request.SystemicExam != null)
            {
                record.SystemicExam = System.Text.Json.JsonSerializer.Serialize(request.SystemicExam);
            }

            // Update diagnosis
            if (request.DiagnosisMain != null) record.DiagnosisMain = request.DiagnosisMain;
            if (request.DiagnosisComorbid != null) record.DiagnosisComorbid = request.DiagnosisComorbid;
            if (request.DiagnosisDifferential != null) record.DiagnosisDifferential = request.DiagnosisDifferential;
            if (request.Prognosis != null) record.Prognosis = request.Prognosis;
            if (request.TreatmentPlan != null) record.TreatmentPlan = request.TreatmentPlan;
            if (request.Notes != null) record.Notes = request.Notes;

            // Update timestamps
            record.UpdatedAt = DateTime.UtcNow;

            // Update MedicalRecordExtras (JSONB fields)
            UpdateMedicalRecordExtras(record, request);

            // Update eye examinations
            UpdateEyeExaminations(record, request);

            // Update subspecialty records
            UpdateSubspecialtyRecords(record, request);

            // Update prescriptions
            UpdatePrescriptions(record, request, state);
        }

        /// <summary>
        /// Updates MedicalRecordExtras (JSONB) for fields not directly in MedicalRecord entity.
        /// </summary>
        private void UpdateMedicalRecordExtras(MedicalRecord record, UpdateMedicalRecordRequest request)
        {
            var extras = record.Extras;
            var needsCreate = extras == null;

            if (needsCreate)
            {
                extras = new MedicalRecordExtras
                {
                    RecordId = record.Id,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = record.DoctorId
                };
                GetDbContext().Set<MedicalRecordExtras>().Add(extras);
                record.Extras = extras;
            }

            // Store summaries in MedicalRecordExtras
            if (request.Summary != null)
            {
                extras.TraumaSummary = request.Summary;
                extras.GlaucomaSummary = request.Summary;
                extras.PediatricSummary = request.Summary;
            }

            // Store administrative fields
            extras.MaYeuTo = request.MaYeuTo ?? extras.MaYeuTo;
            extras.Age = request.Age ?? extras.Age;

            // Store required tests
            extras.RequiredTests = request.RequiredTests ?? extras.RequiredTests;

            // Store treatment plans
            extras.DietPlan = request.DietPlan ?? extras.DietPlan;
            extras.CarePlan = request.CarePlan ?? extras.CarePlan;

            // Store surgery summary
            extras.SurgerySummary = request.SurgerySummary ?? extras.SurgerySummary;

            // Store treatment process
            extras.TreatmentProcess = request.TreatmentProcessSummary ?? extras.TreatmentProcess;

            // Store discharge summary
            extras.DischargeSummary = request.DischargeConditionSummary ?? extras.DischargeSummary;

            // Store discharge VA/IOP
            extras.DischargeVaOd = request.DischargeVaOd ?? extras.DischargeVaOd;
            extras.DischargeVaOs = request.DischargeVaOs ?? extras.DischargeVaOs;
            extras.DischargeIopOd = request.DischargeIopOd ?? extras.DischargeIopOd;
            extras.DischargeIopOs = request.DischargeIopOs ?? extras.DischargeIopOs;

            // Store summary fields
            extras.FinalDiagnosisClinical = request.FinalDiagnosisClinical ?? extras.FinalDiagnosisClinical;
            extras.FinalDiagnosisCause = request.FinalDiagnosisCause ?? extras.FinalDiagnosisCause;

            // Update audit fields
            extras.UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates subspecialty records: Trauma, Lacrimal, Glaucoma, Strabismus/Ptosis, Pediatric.
        /// Includes full CRUD for TraumaSurgeries and GlaucomaHistories.
        /// </summary>
        private void UpdateSubspecialtyRecords(MedicalRecord record, UpdateMedicalRecordRequest request)
        {
            // ===== Trauma Record (MS21) =====
            if (request.TraumaRecord != null)
            {
                var traumaId = record.TraumaRecord?.Id;
                TraumaRecord trauma;
                if (traumaId.HasValue)
                {
                    trauma = GetDbContext().Set<TraumaRecord>().FirstOrDefault(t => t.Id == traumaId)!;
                }
                else
                {
                    trauma = new TraumaRecord { RecordId = record.Id };
                    GetDbContext().Set<TraumaRecord>().Add(trauma);
                    record.TraumaRecord = trauma;
                }

                // Injury History
                if (request.TraumaRecord.InjuryCause != null) trauma.InjuryCause = request.TraumaRecord.InjuryCause;
                if (request.TraumaRecord.InjuryTime.HasValue) trauma.InjuryTime = request.TraumaRecord.InjuryTime;
                if (request.TraumaRecord.PriorTreatment != null) trauma.PriorTreatment = request.TraumaRecord.PriorTreatment;
                if (request.TraumaRecord.PostTreatmentCourse != null) trauma.PostTreatmentCourse = request.TraumaRecord.PostTreatmentCourse;

                // Injury History (from top-level TraumaHistory fields)
                if (request.TraumaCause != null) trauma.InjuryCause = request.TraumaCause;
                if (request.TraumaTime.HasValue) trauma.InjuryTime = request.TraumaTime;
                if (request.TraumaPriorTreatment != null) trauma.PriorTreatment = request.TraumaPriorTreatment;
                if (request.TraumaPostTreatmentCourse != null) trauma.PostTreatmentCourse = request.TraumaPostTreatmentCourse;

                // Injury Summary
                if (request.TraumaRecord.OdInjuries != null) trauma.OdInjuries = request.TraumaRecord.OdInjuries;
                if (request.TraumaRecord.OsInjuries != null) trauma.OsInjuries = request.TraumaRecord.OsInjuries;

                // Injury Details
                if (request.TraumaRecord.InjuryDetails != null) trauma.InjuryDetails = request.TraumaRecord.InjuryDetails;
                if (request.TraumaRecord.TraumaConclusion != null) trauma.TraumaConclusion = request.TraumaRecord.TraumaConclusion;

                // Discharge Summary (from top-level fields)
                if (request.FinalDiagnosisClinical != null) trauma.DiagnosisClinical = request.FinalDiagnosisClinical;
                if (request.TreatmentProcessSummary != null) trauma.TreatmentProcess = request.TreatmentProcessSummary;
                if (request.TreatmentPlan != null) trauma.TreatmentPlan = request.TreatmentPlan;
            }

            // ===== Trauma Surgeries (MS21) - Full CRUD =====
            if (request.TraumaSurgeries != null)
            {
                var traumaRecord = record.TraumaRecord;
                if (traumaRecord != null)
                {
                    UpdateTraumaSurgeries(traumaRecord, request.TraumaSurgeries);
                }
            }

            // ===== Lacrimal Record =====
            if (request.LacrimalRecord != null)
            {
                var side = Enum.Parse<EyeSide>(request.LacrimalRecord.Side, true);
                var existingLacrimalId = record.LacrimalRecords?.FirstOrDefault(l => l.Side == side)?.Id;
                LacrimalRecord lacrimal;
                if (existingLacrimalId.HasValue)
                {
                    lacrimal = GetDbContext().Set<LacrimalRecord>().FirstOrDefault(l => l.Id == existingLacrimalId)!;
                }
                else
                {
                    lacrimal = new LacrimalRecord { Side = side, RecordId = record.Id };
                    GetDbContext().Set<LacrimalRecord>().Add(lacrimal);
                }
                lacrimal.IrrigationFree = request.LacrimalRecord.IrrigationFree;
                lacrimal.IrrigationRegurgitationSame = request.LacrimalRecord.IrrigationRegurgitationSame;
                lacrimal.IrrigationRegurgitationOpposite = request.LacrimalRecord.IrrigationRegurgitationOpposite;
                if (request.LacrimalRecord.IrrigationNote != null) lacrimal.IrrigationNote = request.LacrimalRecord.IrrigationNote;
                if (request.LacrimalRecord.LacrimalOther != null) lacrimal.LacrimalOther = request.LacrimalRecord.LacrimalOther;
            }

            // ===== Glasses Prescription =====
            if (request.GlassesPrescription != null)
            {
                var glassesId = record.GlassesPrescriptions?.FirstOrDefault()?.Id;
                GlassesPrescription glasses;
                if (glassesId.HasValue)
                {
                    glasses = GetDbContext().Set<GlassesPrescription>().FirstOrDefault(g => g.Id == glassesId.Value)!;
                }
                else
                {
                    glasses = new GlassesPrescription
                    {
                        RecordId = record.Id,
                        DoctorId = record.DoctorId,
                        CreatedAt = DateTime.UtcNow
                    };
                    GetDbContext().Set<GlassesPrescription>().Add(glasses);
                }
                var gp = request.GlassesPrescription;
                glasses.SphOd = gp.SphOd;
                glasses.CylOd = gp.CylOd;
                glasses.AxisOd = gp.AxisOd;
                glasses.SphOs = gp.SphOs;
                glasses.CylOs = gp.CylOs;
                glasses.AxisOs = gp.AxisOs;
                glasses.AddOd = gp.AddOd;
                glasses.AddOs = gp.AddOs;
                glasses.Pd = gp.Pd;
                glasses.LensType = gp.LensType;
                glasses.Notes = gp.Notes;
            }

            // ===== Glaucoma Record (MS24) =====
            if (request.GlaucomaRecord != null)
            {
                var glaucomaId = record.GlaucomaRecord?.Id;
                GlaucomaRecord glaucoma;
                if (glaucomaId.HasValue)
                {
                    glaucoma = GetDbContext().Set<GlaucomaRecord>().FirstOrDefault(g => g.Id == glaucomaId)!;
                }
                else
                {
                    glaucoma = new GlaucomaRecord { RecordId = record.Id };
                    GetDbContext().Set<GlaucomaRecord>().Add(glaucoma);
                    record.GlaucomaRecord = glaucoma;
                }

                var g = request.GlaucomaRecord;

                // ===== Symptoms =====
                if (g.EyePainLevel != null) glaucoma.EyePainLevel = g.EyePainLevel;
                if (g.VisionSymptoms != null) glaucoma.VisionSymptoms = g.VisionSymptoms;
                if (g.VisionProgression != null) glaucoma.VisionProgression = g.VisionProgression;
                glaucoma.HasPhotophobia = g.HasPhotophobia;
                glaucoma.HasTearing = g.HasTearing;
                glaucoma.HasRedness = g.HasRedness;
                if (g.SystemicSymptoms != null) glaucoma.SystemicSymptoms = g.SystemicSymptoms;

                // ===== Visual Acuity & IOP =====
                glaucoma.VaWithoutCorrectionOd = ParseDecimal(g.VaWithoutCorrectionOd);
                glaucoma.VaWithoutCorrectionOs = ParseDecimal(g.VaWithoutCorrectionOs);
                glaucoma.VaWithCorrectionOd = ParseDecimal(g.VaWithCorrectionOd);
                glaucoma.VaWithCorrectionOs = ParseDecimal(g.VaWithCorrectionOs);
                glaucoma.IopOd = ParseDecimal(g.IopOd);
                glaucoma.IopOs = ParseDecimal(g.IopOs);
                if (g.IopMethod != null) glaucoma.IopMethod = g.IopMethod;
                glaucoma.IopTargetOd = ParseDecimal(g.IopTargetOd);
                glaucoma.IopTargetOs = ParseDecimal(g.IopTargetOs);

                // ===== History =====
                // Top-level Glaucoma history fields (from request, not from GlaucomaRecord nested object)
                if (request.GlaucomaSymptomDuration != null) glaucoma.HistoryEye = request.GlaucomaSymptomDuration;
                if (request.GlaucomaPriorFacility != null) glaucoma.HistoryEyeSurgery = request.GlaucomaPriorFacility;
                if (request.GlaucomaPriorTreatment != null) glaucoma.PriorEyeSurgeryDetails = request.GlaucomaPriorTreatment;
                if (request.GlaucomaHistoryEye != null) glaucoma.GlaucomaMedications = request.GlaucomaHistoryEye;
                if (request.GlaucomaFamilyHistory != null) glaucoma.FamilyGlaucomaRelation = request.GlaucomaFamilyHistory;

                // Nested GlaucomaRecord history fields
                if (g.HistoryEye != null) glaucoma.HistoryEye = g.HistoryEye;
                if (g.HistoryEyeSurgery != null) glaucoma.HistoryEyeSurgery = g.HistoryEyeSurgery;
                if (g.PriorEyeSurgeryDetails != null) glaucoma.PriorEyeSurgeryDetails = g.PriorEyeSurgeryDetails;
                if (g.SteroidUse != null) glaucoma.SteroidUse = g.SteroidUse;
                if (g.SteroidPrescribed != null) glaucoma.SteroidPrescribed = g.SteroidPrescribed;

                // ===== Systemic History =====
                glaucoma.HasCardiovascularDisease = g.HasCardiovascularDisease;
                glaucoma.HasHypertension = g.HasHypertension;
                glaucoma.HasDiabetes = g.HasDiabetes;
                glaucoma.HasCarotidFistula = g.HasCarotidFistula;
                if (g.OtherSystemicDisease != null) glaucoma.OtherSystemicDisease = g.OtherSystemicDisease;

                // ===== Family History =====
                glaucoma.FamilyHasGlaucoma = g.FamilyHasGlaucoma;
                if (g.FamilyGlaucomaRelation != null) glaucoma.FamilyGlaucomaRelation = g.FamilyGlaucomaRelation;

                // ===== Treatment History =====
                if (g.GlaucomaMedications != null) glaucoma.GlaucomaMedications = g.GlaucomaMedications;
                if (g.OtherMedications != null) glaucoma.OtherMedications = g.OtherMedications;
                if (g.TreatmentProgress != null) glaucoma.TreatmentProgress = g.TreatmentProgress;

                // ===== Classification =====
                if (g.GlaucomaType != null) glaucoma.GlaucomaType = g.GlaucomaType;
                if (g.StageOd != null) glaucoma.StageOd = g.StageOd;
                if (g.StageOs != null) glaucoma.StageOs = g.StageOs;

                // ===== Examination - Eyelid =====
                glaucoma.HasEyelidSwelling = g.HasEyelidSwelling;

                // ===== Examination - Conjunctiva =====
                glaucoma.HasConjunctivalInjection = g.HasConjunctivalInjection;
                glaucoma.HasFilteringBleb = g.HasFilteringBleb;
                if (g.BlebStatus != null) glaucoma.BlebStatus = g.BlebStatus;
                if (g.BlebLocation != null) glaucoma.BlebLocation = g.BlebLocation;

                // ===== Examination - Cornea =====
                if (g.CornealTransparency != null) glaucoma.CornealTransparency = g.CornealTransparency;
                glaucoma.CornealThickness = ParseDecimal(g.CornealThickness);

                // ===== Examination - Sclera =====
                // Note: HasScleralThinning and ScleralScarLocation exist in entity

                // ===== Examination - Anterior Chamber =====
                if (g.AcDepthSmith != null) glaucoma.AcDepthSmith = g.AcDepthSmith;
                if (g.AcDepthHerick != null) glaucoma.AcDepthHerick = g.AcDepthHerick;

                // ===== Examination - Gonioscopy =====
                if (g.GonioscopyOd != null) glaucoma.GonioscopyOd = g.GonioscopyOd;
                if (g.GonioscopyOs != null) glaucoma.GonioscopyOs = g.GonioscopyOs;
                if (g.AngleFindings != null) glaucoma.AngleFindings = g.AngleFindings;

                // ===== Examination - Iris =====
                if (g.IrisColor != null) glaucoma.IrisColor = g.IrisColor;
                if (g.IrisCondition != null) glaucoma.IrisCondition = g.IrisCondition;
                glaucoma.HasIrisNeovascularization = g.HasIrisNeovascularization;

                // ===== Examination - Pupil =====
                if (g.PupilDiameter != null) glaucoma.PupilDiameter = g.PupilDiameter;
                if (g.PupilPigmentBorder != null) glaucoma.PupilPigmentBorder = g.PupilPigmentBorder;
                if (g.PupilReflexResponse != null) glaucoma.PupilReflexResponse = g.PupilReflexResponse;

                // ===== Examination - Lens =====
                if (g.LensStatus != null) glaucoma.LensStatus = g.LensStatus;

                // ===== Examination - Fundus =====
                if (g.FundusRetinaFindings != null) glaucoma.FundusRetinaFindings = g.FundusRetinaFindings;
                if (g.FundusMaculaFindings != null) glaucoma.FundusMaculaFindings = g.FundusMaculaFindings;
                glaucoma.HasCNV = g.HasCNV;
                glaucoma.HasRetinalHemorrhage = g.HasRetinalHemorrhage;

                // ===== Examination - Optic Disc =====
                if (g.OpticDiscDescription != null) glaucoma.OpticDiscDescription = g.OpticDiscDescription;
                if (g.NerveRimOd != null) glaucoma.NerveRimOd = g.NerveRimOd;
                if (g.NerveRimOs != null) glaucoma.NerveRimOs = g.NerveRimOs;
                if (g.OpticDiscCupRatio != null) glaucoma.OpticDiscCupRatio = g.OpticDiscCupRatio;
                if (g.OpticDiscVesselChange != null) glaucoma.OpticDiscVesselChange = g.OpticDiscVesselChange;
                glaucoma.HasOpticDiscHemorrhage = g.HasOpticDiscHemorrhage;
                glaucoma.HasRimAtrophy = g.HasRimAtrophy;

                // ===== Eye Measurements =====
                if (g.EyeAxialLength != null) glaucoma.EyeAxialLength = g.EyeAxialLength;

                // ===== Treatment Plan =====
                if (g.TreatmentPlanSurgery != null) glaucoma.TreatmentPlanSurgery = g.TreatmentPlanSurgery;
                if (g.TreatmentPlanLaser != null) glaucoma.TreatmentPlanLaser = g.TreatmentPlanLaser;
                if (g.TreatmentPlanMedication != null) glaucoma.TreatmentPlanMedication = g.TreatmentPlanMedication;
                if (g.FollowUpPlan != null) glaucoma.FollowUpPlan = g.FollowUpPlan;
            }

            // ===== Glaucoma Histories (MS24) - Full CRUD =====
            if (request.GlaucomaHistories != null)
            {
                var glaucomaRecord = record.GlaucomaRecord;
                if (glaucomaRecord != null)
                {
                    UpdateGlaucomaHistories(glaucomaRecord, request.GlaucomaHistories);
                }
            }

            // ===== Strabismus/Ptosis Record (MS25) =====
            if (request.StrabismusPtosisRecord != null)
            {
                var straId = record.StrabismusPtosisRecord?.Id;
                StrabismusPtosisRecord stra;
                if (straId.HasValue)
                {
                    stra = GetDbContext().Set<StrabismusPtosisRecord>().FirstOrDefault(s => s.Id == straId)!;
                }
                else
                {
                    stra = new StrabismusPtosisRecord { RecordId = record.Id };
                    GetDbContext().Set<StrabismusPtosisRecord>().Add(stra);
                    record.StrabismusPtosisRecord = stra;
                }

                var s = request.StrabismusPtosisRecord;

                // ===== Chief Complaint & Cause (from nested object AND top-level fields) =====
                stra.ChiefStrabismus = s.ChiefStrabismus;
                stra.ChiefPtosis = s.ChiefPtosis;
                stra.Congenital = request.StrabismusCongenital ?? s.Congenital;
                stra.Acquired = request.StrabismusAcquired ?? s.Acquired;
                stra.AcquiredOnset = request.StrabismusOnsetTime ?? s.AcquiredOnset;

                // ===== Strabismus type (from nested object AND top-level field) =====
                stra.StrabismusType = request.StrabismusMainSymptom ?? s.StrabismusType;

                // Top-level Strabismus history fields
                if (request.StrabismusOnsetTime != null) stra.AcquiredOnset = request.StrabismusOnsetTime;
                if (request.StrabismusMainSymptom != null) stra.StrabismusType = request.StrabismusMainSymptom;

                // ===== Strabismus Type =====
                if (s.StrabismusType != null) stra.StrabismusType = s.StrabismusType;

                // ===== Nystagmus =====
                stra.Nystagmus = s.Nystagmus;
                if (s.NystagmusType != null) stra.NystagmusType = s.NystagmusType;

                // ===== Treatment History =====
                if (s.PriorAmblyopiaTreatment != null) stra.PriorAmblyopiaTreatment = s.PriorAmblyopiaTreatment;
                if (s.PriorAmblyopiaResult != null) stra.PriorAmblyopiaResult = s.PriorAmblyopiaResult;
                if (s.PriorSurgery != null) stra.PriorSurgery = s.PriorSurgery;
                if (s.PriorSurgeryResult != null) stra.PriorSurgeryResult = s.PriorSurgeryResult;

                // ===== Visual Acuity Before/After Atropine =====
                if (s.VaBeforeAtropineOd != null) stra.VaBeforeAtropineOd = s.VaBeforeAtropineOd;
                if (s.VaBeforeAtropineOs != null) stra.VaBeforeAtropineOs = s.VaBeforeAtropineOs;
                if (s.VaAfterAtropineOd != null) stra.VaAfterAtropineOd = s.VaAfterAtropineOd;
                if (s.VaAfterAtropineOs != null) stra.VaAfterAtropineOs = s.VaAfterAtropineOs;

                // ===== Refraction =====
                if (s.RefractionPreAtropine != null) stra.RefractionPreAtropine = s.RefractionPreAtropine;
                if (s.RefractionPostAtropine != null) stra.RefractionPostAtropine = s.RefractionPostAtropine;

                // ===== Pupil Shadow Test =====
                if (s.PupilShadowTestOd != null) stra.PupilShadowTestOd = s.PupilShadowTestOd;
                if (s.PupilShadowTestOs != null) stra.PupilShadowTestOs = s.PupilShadowTestOs;

                // ===== Extraocular Motility =====
                if (s.EomGazeTest != null) stra.EomGazeTest = s.EomGazeTest;

                // ===== Internal Extraocular Motility =====
                if (s.EomInternalOd != null) stra.EomInternalOd = s.EomInternalOd;
                if (s.EomInternalOs != null) stra.EomInternalOs = s.EomInternalOs;

                // ===== Convergence Point =====
                if (s.ConvergencePoint != null) stra.ConvergencePoint = s.ConvergencePoint;

                // ===== Cover Test =====
                if (s.CoverTestResult != null) stra.CoverTestResult = s.CoverTestResult;

                // ===== Hirschberg Test =====
                if (s.HirschbergBeforeAtropine != null) stra.HirschbergBeforeAtropine = s.HirschbergBeforeAtropine;
                if (s.HirschbergAfterAtropine != null) stra.HirschbergAfterAtropine = s.HirschbergAfterAtropine;

                // ===== Prism Measurement =====
                if (s.PrismNear != null) stra.PrismNear = s.PrismNear;
                if (s.PrismDistance != null) stra.PrismDistance = s.PrismDistance;
                if (s.PrismUp != null) stra.PrismUp = s.PrismUp;
                if (s.PrismDown != null) stra.PrismDown = s.PrismDown;

                // ===== Syndrome =====
                if (s.StrabismusSyndrome != null) stra.StrabismusSyndrome = s.StrabismusSyndrome;

                // ===== Synoptophore Test =====
                if (s.SynoptophoreObjective != null) stra.SynoptophoreObjective = s.SynoptophoreObjective;
                if (s.SynoptophoreSubjective != null) stra.SynoptophoreSubjective = s.SynoptophoreSubjective;

                // ===== Binocular Vision =====
                if (s.BinocularStatus != null) stra.BinocularStatus = s.BinocularStatus;
                if (s.FusionAmplitude != null) stra.FusionAmplitude = s.FusionAmplitude;
                if (s.RetinalCorrespondence != null) stra.RetinalCorrespondence = s.RetinalCorrespondence;
                if (s.Diplopia != null) stra.Diplopia = s.Diplopia;
                if (s.CompensatoryHeadPosture != null) stra.CompensatoryHeadPosture = s.CompensatoryHeadPosture;

                // ===== Ptosis Measurements =====
                if (s.PtosisDegreeOd != null) stra.PtosisDegreeOd = s.PtosisDegreeOd;
                if (s.PtosisDegreeOs != null) stra.PtosisDegreeOs = s.PtosisDegreeOs;
                if (s.LevatorFunctionOd != null) stra.LevatorFunctionOd = s.LevatorFunctionOd;
                if (s.LevatorFunctionOs != null) stra.LevatorFunctionOs = s.LevatorFunctionOs;
                if (s.MarcusGunn != null) stra.MarcusGunn = s.MarcusGunn;
                if (s.BellPhenomenon != null) stra.BellPhenomenon = s.BellPhenomenon;
                if (s.FixationOd != null) stra.FixationOd = s.FixationOd;
                if (s.FixationOs != null) stra.FixationOs = s.FixationOs;

                // ===== Palpebral Reflex =====
                if (s.PalpebralReflexOd != null) stra.PalpebralReflexOd = s.PalpebralReflexOd;
                if (s.PalpebralReflexOs != null) stra.PalpebralReflexOs = s.PalpebralReflexOs;
            }

            // ===== Pediatric Record (MS26) =====
            if (request.PediatricRecord != null)
            {
                var pedId = record.PediatricRecord?.Id;
                PediatricEyeRecord ped;
                if (pedId.HasValue)
                {
                    ped = GetDbContext().Set<PediatricEyeRecord>().FirstOrDefault(p => p.Id == pedId)!;
                }
                else
                {
                    ped = new PediatricEyeRecord { RecordId = record.Id };
                    GetDbContext().Set<PediatricEyeRecord>().Add(ped);
                    record.PediatricRecord = ped;
                }

                var p = request.PediatricRecord;

                // ===== History =====
                ped.Congenital = p.Congenital;
                ped.Acquired = p.Acquired;
                if (p.AcquiredOnset != null) ped.AcquiredOnset = p.AcquiredOnset;
                if (p.PriorTreatment != null) ped.PriorTreatment = p.PriorTreatment;

                // ===== Pregnancy & Development =====
                ped.PregnancyIllness = p.PregnancyIllness;
                if (p.PregnancyIllnessDetail != null) ped.PregnancyIllnessDetail = p.PregnancyIllnessDetail;
                ped.IntellectualDevelopmentNormal = p.IntellectualDevelopmentNormal;
                ped.PediatricPregnancyHistory = request.PediatricPregnancyHistory ?? p.PregnancyIllnessDetail;
                ped.PediatricDevelopment = request.PediatricDevelopment;

                // ===== Chief Symptoms =====
                if (p.ChiefSymptoms != null) ped.ChiefSymptoms = p.ChiefSymptoms;

                // ===== Eyelid Conditions =====
                ped.EntropionOd = p.EntropionOd;
                ped.EpicanthusOd = p.EpicanthusOd;
                ped.PtosisOd = p.PtosisOd;
                if (p.EyelidTumor != null) ped.EyelidTumor = p.EyelidTumor;
                if (p.EyelidTumorLocation != null) ped.EyelidTumorLocation = p.EyelidTumorLocation;
                if (p.EyelidTumorSize != null) ped.EyelidTumorSize = p.EyelidTumorSize;

                // ===== Eyeball Status =====
                if (p.EyeballOdStatus != null) ped.EyeballOdStatus = p.EyeballOdStatus;
                if (p.EyeballOsStatus != null) ped.EyeballOsStatus = p.EyeballOsStatus;
                if (p.EyeballTexture != null) ped.EyeballTexture = p.EyeballTexture;

                // ===== Amblyopia =====
                if (p.AmblyopiaStatus != null) ped.AmblyopiaStatus = p.AmblyopiaStatus;
                if (p.FixationPreferenceOd != null) ped.FixationPreferenceOd = p.FixationPreferenceOd;
                if (p.FixationPreferenceOs != null) ped.FixationPreferenceOs = p.FixationPreferenceOs;

                // ===== Fundus Summary =====
                if (p.FundusSummaryOd != null) ped.FundusSummaryOd = p.FundusSummaryOd;
                if (p.FundusSummaryOs != null) ped.FundusSummaryOs = p.FundusSummaryOs;

                // ===== Developmental Status =====
                if (p.GeneralHealthStatus != null) ped.GeneralHealthStatus = p.GeneralHealthStatus;
            }
        }

        /// <summary>
        /// Updates trauma surgeries (TraumaSurgeries) with full CRUD operations.
        /// Handles: Add new, Update existing, Delete existing.
        /// </summary>
        private void UpdateTraumaSurgeries(TraumaRecord traumaRecord, List<UpdateTraumaSurgeryData> surgeries)
        {
            var dbSet = GetDbContext().Set<TraumaSurgery>();
            var existingSurgeries = traumaRecord.Surgeries?.ToList() ?? new List<TraumaSurgery>();
            var requestIds = surgeries.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).ToHashSet();
            var existingIds = existingSurgeries.Select(s => s.Id).ToHashSet();

            // Delete surgeries that are not in the request
            var toDelete = existingSurgeries.Where(s => !requestIds.Contains(s.Id)).ToList();
            foreach (var deleteSurgery in toDelete)
            {
                dbSet.Remove(deleteSurgery);
            }

            // Add or update surgeries
            foreach (var surgeryData in surgeries)
            {
                TraumaSurgery surgery;
                if (surgeryData.Id.HasValue && existingIds.Contains(surgeryData.Id.Value))
                {
                    // Update existing
                    surgery = dbSet.FirstOrDefault(s => s.Id == surgeryData.Id.Value)!;
                }
                else
                {
                    // Add new
                    surgery = new TraumaSurgery { TraumaRecordId = traumaRecord.Id };
                    dbSet.Add(surgery);
                }

                if (surgeryData.SurgeryDate.HasValue) surgery.SurgeryDate = surgeryData.SurgeryDate;
                if (surgeryData.SurgeryType != null) surgery.SurgeryType = surgeryData.SurgeryType;
                if (surgeryData.SurgeryDescription != null) surgery.SurgeryDescription = surgeryData.SurgeryDescription;
                if (surgeryData.SurgeonName != null) surgery.SurgeonName = surgeryData.SurgeonName;
                if (surgeryData.AnesthesiaType != null) surgery.AnesthesiaType = surgeryData.AnesthesiaType;
                if (surgeryData.PostSurgeryCondition != null) surgery.PostSurgeryCondition = surgeryData.PostSurgeryCondition;
                if (surgeryData.Notes != null) surgery.Notes = surgeryData.Notes;
            }
        }

        /// <summary>
        /// Updates glaucoma histories (GlaucomaHistories) with full CRUD operations.
        /// Handles: Add new, Update existing, Delete existing.
        /// </summary>
        private void UpdateGlaucomaHistories(GlaucomaRecord glaucomaRecord, List<UpdateGlaucomaHistoryData> histories)
        {
            var dbSet = GetDbContext().Set<GlaucomaHistory>();
            var existingHistories = glaucomaRecord.Histories?.ToList() ?? new List<GlaucomaHistory>();
            var requestIds = histories.Where(h => h.Id.HasValue).Select(h => h.Id!.Value).ToHashSet();
            var existingIds = existingHistories.Select(h => h.Id).ToHashSet();

            // Delete histories that are not in the request
            var toDelete = existingHistories.Where(h => !requestIds.Contains(h.Id)).ToList();
            foreach (var deleteHistory in toDelete)
            {
                dbSet.Remove(deleteHistory);
            }

            // Add or update histories
            foreach (var historyData in histories)
            {
                GlaucomaHistory history;
                if (historyData.Id.HasValue && existingIds.Contains(historyData.Id.Value))
                {
                    // Update existing
                    history = dbSet.FirstOrDefault(h => h.Id == historyData.Id.Value)!;
                }
                else
                {
                    // Add new
                    history = new GlaucomaHistory { GlaucomaRecordId = glaucomaRecord.Id };
                    dbSet.Add(history);
                }

                if (!string.IsNullOrEmpty(historyData.HistoryType)) history.HistoryType = historyData.HistoryType;
                if (historyData.EyeSide != null) history.Side = Enum.Parse<EyeSide>(historyData.EyeSide, true);
                if (historyData.AttemptNumber.HasValue) history.AttemptNumber = historyData.AttemptNumber;

                // For SURGERY
                if (historyData.ProcedureType != null) history.ProcedureType = historyData.ProcedureType;
                if (historyData.ProcedureDate.HasValue) history.ProcedureDate = historyData.ProcedureDate;
                if (historyData.FacilityLevel != null) history.FacilityLevel = historyData.FacilityLevel;

                // For DRUG
                if (historyData.DrugName != null) history.DrugName = historyData.DrugName;
                if (historyData.Dosage != null) history.Dosage = historyData.Dosage;
                if (historyData.Duration != null) history.Duration = historyData.Duration;
                if (historyData.Route != null) history.Route = historyData.Route;
                if (historyData.ChangeReason != null) history.ChangeReason = historyData.ChangeReason;
            }
        }

        /// <summary>
        /// Updates prescriptions with full CRUD operations for prescription items.
        /// Handles: Add new prescription, Update existing, Add/Update/Delete prescription items.
        /// </summary>
        private void UpdatePrescriptions(MedicalRecord record, UpdateMedicalRecordRequest request, ExecutionState state)
        {
            // Only update if Prescription or PrescriptionItems are provided
            if (request.Prescription == null && (request.PrescriptionItems == null || !request.PrescriptionItems.Any()))
                return;

            var dbContext = GetDbContext();
            var prescriptionDbSet = dbContext.Set<Prescription>();
            var prescriptionItemDbSet = dbContext.Set<PrescriptionItem>();

            // Get or create prescription
            Prescription prescription;
            var existingPrescription = record.Prescriptions?.FirstOrDefault();
            if (existingPrescription != null)
            {
                prescription = existingPrescription;
            }
            else
            {
                prescription = new Prescription
                {
                    RecordId = record.Id,
                    DoctorId = record.DoctorId,
                    CreatedAt = DateTime.UtcNow
                };
                prescriptionDbSet.Add(prescription);
            }

            // Update prescription header
            if (request.Prescription != null && request.Prescription.Notes != null)
            {
                prescription.Notes = request.Prescription.Notes;
            }

            // Handle prescription items
            if (request.PrescriptionItems != null && request.PrescriptionItems.Any())
            {
                var existingItems = prescription.Items?.ToList() ?? new List<PrescriptionItem>();
                var requestIds = request.PrescriptionItems.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();

                // Delete items that are not in the request
                var toDelete = existingItems.Where(i => !requestIds.Contains(i.Id)).ToList();
                foreach (var deleteItem in toDelete)
                {
                    prescriptionItemDbSet.Remove(deleteItem);
                }

                // Add or update items
                foreach (var itemData in request.PrescriptionItems)
                {
                    PrescriptionItem item;
                    if (itemData.Id.HasValue && existingItems.Any(i => i.Id == itemData.Id.Value))
                    {
                        // Update existing
                        item = prescriptionItemDbSet.FirstOrDefault(i => i.Id == itemData.Id.Value)!;
                    }
                    else
                    {
                        // Add new
                        item = new PrescriptionItem { PrescriptionId = prescription.Id };
                        prescriptionItemDbSet.Add(item);
                    }

                    if (!string.IsNullOrEmpty(itemData.MedicineName)) item.MedicineName = itemData.MedicineName;
                    if (!string.IsNullOrEmpty(itemData.Dosage)) item.Dosage = itemData.Dosage;
                    if (itemData.Frequency != null) item.Frequency = itemData.Frequency;
                    if (itemData.DurationDays.HasValue) item.DurationDays = itemData.DurationDays;
                    item.Quantity = itemData.Quantity;
                    if (itemData.Instruction != null) item.Instruction = itemData.Instruction;
                }
            }
        }

        /// <summary>
        /// Updates eye examination records for both eyes (RIGHT and LEFT) based on provided exam data.
        /// </summary>
        private void UpdateEyeExaminations(MedicalRecord record, UpdateMedicalRecordRequest request)
        {
            // Right Eye Basic
            if (request.RightEyeBasic != null)
            {
                var rightBasic = record.EyeExamBasics?.FirstOrDefault(e => e.Side == EyeSide.RIGHT);
                if (rightBasic != null)
                {
                    MapEyeExamBasic(rightBasic, request.RightEyeBasic);
                }
            }

            // Left Eye Basic
            if (request.LeftEyeBasic != null)
            {
                var leftBasic = record.EyeExamBasics?.FirstOrDefault(e => e.Side == EyeSide.LEFT);
                if (leftBasic != null)
                {
                    MapEyeExamBasic(leftBasic, request.LeftEyeBasic);
                }
            }

            // Right Eyelid & Conjunctiva
            if (request.RightEyeEyelid != null || request.RightEyeConjunctiva != null)
            {
                var rightEyelid = record.EyeEyelidConjunctivae?.FirstOrDefault(e => e.Side == EyeSide.RIGHT);
                if (rightEyelid == null)
                {
                    rightEyelid = new EyeEyelidConjunctiva { Side = EyeSide.RIGHT, RecordId = record.Id };
                    record.EyeEyelidConjunctivae ??= new List<EyeEyelidConjunctiva>();
                    record.EyeEyelidConjunctivae.Add(rightEyelid);
                }
                MapEyeEyelidConjunctiva(rightEyelid, request.RightEyeEyelid, request.RightEyeConjunctiva);
            }

            // Left Eyelid & Conjunctiva
            if (request.LeftEyeEyelid != null || request.LeftEyeConjunctiva != null)
            {
                var leftEyelid = record.EyeEyelidConjunctivae?.FirstOrDefault(e => e.Side == EyeSide.LEFT);
                if (leftEyelid == null)
                {
                    leftEyelid = new EyeEyelidConjunctiva { Side = EyeSide.LEFT, RecordId = record.Id };
                    record.EyeEyelidConjunctivae ??= new List<EyeEyelidConjunctiva>();
                    record.EyeEyelidConjunctivae.Add(leftEyelid);
                }
                MapEyeEyelidConjunctiva(leftEyelid, request.LeftEyeEyelid, request.LeftEyeConjunctiva);
            }

            // Right Cornea
            if (request.RightEyeCornea != null)
            {
                var rightCornea = record.EyeCorneas?.FirstOrDefault(e => e.Side == EyeSide.RIGHT);
                if (rightCornea == null)
                {
                    rightCornea = new EyeCornea { Side = EyeSide.RIGHT, RecordId = record.Id };
                    record.EyeCorneas ??= new List<EyeCornea>();
                    record.EyeCorneas.Add(rightCornea);
                }
                MapEyeCornea(rightCornea, request.RightEyeCornea);
            }

            // Left Cornea
            if (request.LeftEyeCornea != null)
            {
                var leftCornea = record.EyeCorneas?.FirstOrDefault(e => e.Side == EyeSide.LEFT);
                if (leftCornea == null)
                {
                    leftCornea = new EyeCornea { Side = EyeSide.LEFT, RecordId = record.Id };
                    record.EyeCorneas ??= new List<EyeCornea>();
                    record.EyeCorneas.Add(leftCornea);
                }
                MapEyeCornea(leftCornea, request.LeftEyeCornea);
            }

            // Right AC & Iris
            if (request.RightEyeAnteriorChamber != null || request.RightEyeIrisPupil != null)
            {
                var rightAcIris = record.EyeAcIrises?.FirstOrDefault(e => e.Side == EyeSide.RIGHT);
                if (rightAcIris == null)
                {
                    rightAcIris = new EyeAcIris { Side = EyeSide.RIGHT, RecordId = record.Id };
                    record.EyeAcIrises ??= new List<EyeAcIris>();
                    record.EyeAcIrises.Add(rightAcIris);
                }
                MapEyeAcIris(rightAcIris, request.RightEyeAnteriorChamber, request.RightEyeIrisPupil);
            }

            // Left AC & Iris
            if (request.LeftEyeAnteriorChamber != null || request.LeftEyeIrisPupil != null)
            {
                var leftAcIris = record.EyeAcIrises?.FirstOrDefault(e => e.Side == EyeSide.LEFT);
                if (leftAcIris == null)
                {
                    leftAcIris = new EyeAcIris { Side = EyeSide.LEFT, RecordId = record.Id };
                    record.EyeAcIrises ??= new List<EyeAcIris>();
                    record.EyeAcIrises.Add(leftAcIris);
                }
                MapEyeAcIris(leftAcIris, request.LeftEyeAnteriorChamber, request.LeftEyeIrisPupil);
            }

            // Right Lens & Vitreous
            if (request.RightEyeLens != null || request.RightEyeVitreous != null)
            {
                var rightLensVitreous = record.EyeLensVitreouses?.FirstOrDefault(e => e.Side == EyeSide.RIGHT);
                if (rightLensVitreous == null)
                {
                    rightLensVitreous = new EyeLensVitreous { Side = EyeSide.RIGHT, RecordId = record.Id };
                    record.EyeLensVitreouses ??= new List<EyeLensVitreous>();
                    record.EyeLensVitreouses.Add(rightLensVitreous);
                }
                MapEyeLensVitreous(rightLensVitreous, request.RightEyeLens, request.RightEyeVitreous);
            }

            // Left Lens & Vitreous
            if (request.LeftEyeLens != null || request.LeftEyeVitreous != null)
            {
                var leftLensVitreous = record.EyeLensVitreouses?.FirstOrDefault(e => e.Side == EyeSide.LEFT);
                if (leftLensVitreous == null)
                {
                    leftLensVitreous = new EyeLensVitreous { Side = EyeSide.LEFT, RecordId = record.Id };
                    record.EyeLensVitreouses ??= new List<EyeLensVitreous>();
                    record.EyeLensVitreouses.Add(leftLensVitreous);
                }
                MapEyeLensVitreous(leftLensVitreous, request.LeftEyeLens, request.LeftEyeVitreous);
            }

            // Right Sclera
            if (request.RightEyeSclera != null)
            {
                var rightSclera = record.EyeScleras?.FirstOrDefault(e => e.Side == EyeSide.RIGHT);
                if (rightSclera == null)
                {
                    rightSclera = new EyeSclera { Side = EyeSide.RIGHT, RecordId = record.Id };
                    record.EyeScleras ??= new List<EyeSclera>();
                    record.EyeScleras.Add(rightSclera);
                }
                MapEyeSclera(rightSclera, request.RightEyeSclera);
            }

            // Left Sclera
            if (request.LeftEyeSclera != null)
            {
                var leftSclera = record.EyeScleras?.FirstOrDefault(e => e.Side == EyeSide.LEFT);
                if (leftSclera == null)
                {
                    leftSclera = new EyeSclera { Side = EyeSide.LEFT, RecordId = record.Id };
                    record.EyeScleras ??= new List<EyeSclera>();
                    record.EyeScleras.Add(leftSclera);
                }
                MapEyeSclera(leftSclera, request.LeftEyeSclera);
            }

            // Right Fundus Disc & Macula
            if (request.RightEyeFundusDiscMacula != null)
            {
                var rightFundusDiscMacula = record.EyeFundusDiscMaculas?.FirstOrDefault(e => e.Side == EyeSide.RIGHT);
                if (rightFundusDiscMacula == null)
                {
                    rightFundusDiscMacula = new EyeFundusDiscMacula { Side = EyeSide.RIGHT, RecordId = record.Id };
                    record.EyeFundusDiscMaculas ??= new List<EyeFundusDiscMacula>();
                    record.EyeFundusDiscMaculas.Add(rightFundusDiscMacula);
                }
                MapEyeFundusDiscMacula(rightFundusDiscMacula, request.RightEyeFundusDiscMacula);
            }

            // Left Fundus Disc & Macula
            if (request.LeftEyeFundusDiscMacula != null)
            {
                var leftFundusDiscMacula = record.EyeFundusDiscMaculas?.FirstOrDefault(e => e.Side == EyeSide.LEFT);
                if (leftFundusDiscMacula == null)
                {
                    leftFundusDiscMacula = new EyeFundusDiscMacula { Side = EyeSide.LEFT, RecordId = record.Id };
                    record.EyeFundusDiscMaculas ??= new List<EyeFundusDiscMacula>();
                    record.EyeFundusDiscMaculas.Add(leftFundusDiscMacula);
                }
                MapEyeFundusDiscMacula(leftFundusDiscMacula, request.LeftEyeFundusDiscMacula);
            }

            // Right Fundus Retina & Vessel
            if (request.RightEyeFundusRetinaVessel != null)
            {
                var rightFundusRetinaVessel = record.EyeFundusRetinaVessels?.FirstOrDefault(e => e.Side == EyeSide.RIGHT);
                if (rightFundusRetinaVessel == null)
                {
                    rightFundusRetinaVessel = new EyeFundusRetinaVessel { Side = EyeSide.RIGHT, RecordId = record.Id };
                    record.EyeFundusRetinaVessels ??= new List<EyeFundusRetinaVessel>();
                    record.EyeFundusRetinaVessels.Add(rightFundusRetinaVessel);
                }
                MapEyeFundusRetinaVessel(rightFundusRetinaVessel, request.RightEyeFundusRetinaVessel);
            }

            // Left Fundus Retina & Vessel
            if (request.LeftEyeFundusRetinaVessel != null)
            {
                var leftFundusRetinaVessel = record.EyeFundusRetinaVessels?.FirstOrDefault(e => e.Side == EyeSide.LEFT);
                if (leftFundusRetinaVessel == null)
                {
                    leftFundusRetinaVessel = new EyeFundusRetinaVessel { Side = EyeSide.LEFT, RecordId = record.Id };
                    record.EyeFundusRetinaVessels ??= new List<EyeFundusRetinaVessel>();
                    record.EyeFundusRetinaVessels.Add(leftFundusRetinaVessel);
                }
                MapEyeFundusRetinaVessel(leftFundusRetinaVessel, request.LeftEyeFundusRetinaVessel);
            }

            // Eye Orbit - Right
            if (request.RightEyeOrbit != null)
            {
                var rightOrbit = record.EyeOrbits?.FirstOrDefault(e => e.Side == EyeSide.RIGHT);
                if (rightOrbit == null)
                {
                    rightOrbit = new EyeOrbit { Side = EyeSide.RIGHT, RecordId = record.Id };
                    record.EyeOrbits ??= new List<EyeOrbit>();
                    record.EyeOrbits.Add(rightOrbit);
                }
                MapEyeOrbit(rightOrbit, request.RightEyeOrbit);
            }

            // Eye Orbit - Left
            if (request.LeftEyeOrbit != null)
            {
                var leftOrbit = record.EyeOrbits?.FirstOrDefault(e => e.Side == EyeSide.LEFT);
                if (leftOrbit == null)
                {
                    leftOrbit = new EyeOrbit { Side = EyeSide.LEFT, RecordId = record.Id };
                    record.EyeOrbits ??= new List<EyeOrbit>();
                    record.EyeOrbits.Add(leftOrbit);
                }
                MapEyeOrbit(leftOrbit, request.LeftEyeOrbit);
            }
        }

        /// <summary>
        /// Persists all changes to the database with concurrency conflict handling.
        /// </summary>
        private async Task PersistChangesAsync(ExecutionState state)
        {
            if (state.HasError || state.MedicalRecord == null) return;

            try
            {
                await _medicalRecordRepository.SaveChangesAsync();
                state.IsExecutionSuccess = true;

                if (state.MedicalRecord.Id != Guid.Empty)
                {
                    await GetDbContext().Entry(state.MedicalRecord)
                        .Collection(r => r.EyeExamBasics!)
                        .LoadAsync();
                    await GetDbContext().Entry(state.MedicalRecord)
                        .Collection(r => r.EyeEyelidConjunctivae!)
                        .LoadAsync();
                    await GetDbContext().Entry(state.MedicalRecord)
                        .Collection(r => r.EyeCorneas!)
                        .LoadAsync();
                    await GetDbContext().Entry(state.MedicalRecord)
                        .Collection(r => r.EyeAcIrises!)
                        .LoadAsync();
                    await GetDbContext().Entry(state.MedicalRecord)
                        .Collection(r => r.EyeLensVitreouses!)
                        .LoadAsync();
                    await GetDbContext().Entry(state.MedicalRecord)
                        .Collection(r => r.EyeScleras!)
                        .LoadAsync();
                    await GetDbContext().Entry(state.MedicalRecord)
                        .Collection(r => r.EyeFundusDiscMaculas!)
                        .LoadAsync();
                    await GetDbContext().Entry(state.MedicalRecord)
                        .Collection(r => r.EyeFundusRetinaVessels!)
                        .LoadAsync();
                    await GetDbContext().Entry(state.MedicalRecord)
                        .Reference(r => r.StrabismusPtosisRecord)
                        .LoadAsync();
                    await GetDbContext().Entry(state.MedicalRecord)
                        .Reference(r => r.TraumaRecord)
                        .LoadAsync();
                    await GetDbContext().Entry(state.MedicalRecord)
                        .Reference(r => r.GlaucomaRecord)
                        .LoadAsync();
                    await GetDbContext().Entry(state.MedicalRecord)
                        .Reference(r => r.PediatricRecord)
                        .LoadAsync();
                    await GetDbContext().Entry(state.MedicalRecord)
                        .Collection(r => r.LacrimalRecords!)
                        .LoadAsync();
                    await GetDbContext().Entry(state.MedicalRecord)
                        .Collection(r => r.GlassesPrescriptions!)
                        .LoadAsync();
                    await GetDbContext().Entry(state.MedicalRecord)
                        .Collection(r => r.Prescriptions!)
                        .LoadAsync();
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                state.HasConcurrencyError = true;
                state.HasError = true;
                state.IsExecutionSuccess = false;
                state.ErrorCode = GeneralCode.APP_MESSAGE_5001.ToString();
            }
            catch (Exception)
            {
                state.HasError = true;
                state.IsExecutionSuccess = false;
                state.ErrorCode = GeneralCode.APP_MESSAGE_5001.ToString();
            }
        }

        /// <summary>
        /// Creates the API response based on execution state.
        /// </summary>
        private ApiResponse<UpdateMedicalRecordResponse> CreateResponse(ExecutionState state)
        {
            if (state.HasConcurrencyError)
            {
                return ApiResponse<UpdateMedicalRecordResponse>.Fail(GeneralCode.APP_MESSAGE_5001.ToString());
            }

            if (state.HasError)
            {
                return ApiResponse<UpdateMedicalRecordResponse>.Fail(state.ErrorCode ?? GeneralCode.APP_MESSAGE_5001.ToString());
            }

            var response = new UpdateMedicalRecordResponse
            {
                MedicalRecordId = state.MedicalRecord?.Id.ToString() ?? string.Empty,
                PatientName = state.PatientName,
                RecordTypeLabel = state.RecordTypeLabel ?? GetRecordTypeLabel(state.MedicalRecord?.RecordType ?? RecordType.MS23_FUNDUS),
                AppointmentDate = state.MedicalRecord?.Appointment?.AppointmentDate.ToString("dd/MM/yyyy"),
                DoctorName = state.DoctorName,
                UpdatedAt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm"),
                IsSuccess = true
            };

            return ApiResponse<UpdateMedicalRecordResponse>.Success(GeneralCode.APP_MESSAGE_2006.ToString(), response);
        }

        #region Mapping Methods

        /// <summary>
        /// Maps eye basic exam data (visual acuity, IOP, refraction) to EyeExamBasic entity.
        /// </summary>
        private static void MapEyeExamBasic(EyeExamBasic entity, UpdateEyeBasicExamData data)
        {
            if (data.VaUncorrected != null) entity.VaUncorrected = ParseDecimal(data.VaUncorrected);
            if (data.VaCorrected != null) entity.VaCorrected = ParseDecimal(data.VaCorrected);
            if (data.VaNear != null) entity.VaNear = ParseDecimal(data.VaNear);
            if (data.VaPinhole != null) entity.VaPinhole = ParseDecimal(data.VaPinhole);
            if (data.VaWithGlasses != null) entity.VaCorrected = ParseDecimal(data.VaWithGlasses);
            if (data.IopMmhg != null) entity.IopMmhg = ParseDecimal(data.IopMmhg);
            if (data.IopMethod != null) entity.IopMethod = data.IopMethod;
            if (data.AutoRefraction != null) entity.AutoRefraction = data.AutoRefraction;
            if (data.Retinoscopy != null) entity.Retinoscopy = data.Retinoscopy;
            if (data.SubjectiveRefraction != null) entity.SubjectiveRefraction = data.SubjectiveRefraction;
            if (data.VisualField != null) entity.VisualField = data.VisualField;
            if (data.EomStatus != null)
            {
                entity.EomNormal = string.IsNullOrEmpty(data.EomStatus) || data.EomStatus == "Bình thường";
            }
            if (data.EomNote != null) entity.EomNote = data.EomNote;
            if (data.Nystagmus != null)
            {
                entity.Nystagmus = !string.IsNullOrEmpty(data.Nystagmus) && data.Nystagmus != "Không";
                entity.NystagmusType = data.NystagmusType;
            }
        }

        /// <summary>
        /// Maps eyelid and conjunctiva exam data to EyeEyelidConjunctiva entity.
        /// </summary>
        private static void MapEyeEyelidConjunctiva(EyeEyelidConjunctiva entity, UpdateEyeEyelidData? eyelidData, UpdateEyeConjunctivaData? conjunctivaData)
        {
            // ===== EYELID =====
            if (eyelidData != null)
            {
                if (eyelidData.Status != null)
                {
                    entity.EyelidNormal = string.IsNullOrEmpty(eyelidData.Status) || eyelidData.Status == "Bình thường";
                    entity.EyelidEdema = eyelidData.Status?.Contains("Phù") == true || eyelidData.Status?.Contains("Sưng") == true;
                    entity.EyelidHemorrhage = eyelidData.Status?.Contains("Tụ máu") == true;
                }
                if (eyelidData.Ptosis.HasValue) entity.Ptosis = eyelidData.Ptosis.Value;
                if (eyelidData.PtosisDegree != null) entity.PtosisDegree = eyelidData.PtosisDegree;
                if (eyelidData.Laceration.HasValue) entity.Laceration = eyelidData.Laceration.Value;
                if (eyelidData.LacerationExtent != null) entity.LacerationExtent = eyelidData.LacerationExtent;
                if (eyelidData.LacerationLocation != null) entity.LacerationDepth = eyelidData.LacerationLocation;
                if (eyelidData.LacerationSutured.HasValue) entity.LacerationSutured = eyelidData.LacerationSutured.Value;
                if (eyelidData.LacerationUnsutured.HasValue) entity.LacerationUnsutured = eyelidData.LacerationUnsutured.Value;
                if (eyelidData.Entropion.HasValue) { entity.Entropion = eyelidData.Entropion.Value; entity.EntropionPediatric = eyelidData.Entropion.Value; }
                if (eyelidData.Epicanthus.HasValue) entity.Epicanthus = eyelidData.Epicanthus.Value;
                if (eyelidData.Lagophthalmos.HasValue) entity.Lagophthalmos = eyelidData.Lagophthalmos.Value;
                if (eyelidData.LowerLidRetraction.HasValue) entity.Lagophthalmos = eyelidData.LowerLidRetraction.Value;
                if (eyelidData.Scar.HasValue) entity.Scar = eyelidData.Scar.Value;
                if (eyelidData.EyelidDefect != null) entity.LacerationExtent = eyelidData.EyelidDefect;
                if (!string.IsNullOrEmpty(eyelidData.ChalazionHordeolum)) { entity.Chalazion = true; entity.Hordeolum = true; }
                if (eyelidData.HasTumor.HasValue) entity.HasTumor = eyelidData.HasTumor.Value;
                if (eyelidData.TumorNature != null) entity.TumorNature = eyelidData.TumorNature;
                if (eyelidData.TumorLocation != null) entity.TumorLocation = eyelidData.TumorLocation;
                if (eyelidData.TumorSize != null) entity.TumorSize = eyelidData.TumorSize;
                if (eyelidData.LacrimalDuctStatus != null)
                {
                    entity.LacrimalDuctNormal = eyelidData.LacrimalDuctStatus == "Bình thường";
                    entity.LacrimalDuctCut = eyelidData.LacrimalDuctStatus == "Đứt lệ quản";
                }
                if (eyelidData.LacrimalDuctLocation != null) entity.LacrimalDuctCutLocation = eyelidData.LacrimalDuctLocation;
                if (eyelidData.ScarDescription != null) entity.Scar = true;
                if (eyelidData.OtherFindings != null) entity.EyelidOther = eyelidData.OtherFindings;
            }

            // ===== CONJUNCTIVA =====
            if (conjunctivaData != null)
            {
                if (conjunctivaData.Status != null)
                {
                    entity.ConjunctivaNormal = string.IsNullOrEmpty(conjunctivaData.Status) || conjunctivaData.Status == "Bình thường";
                }
                if (conjunctivaData.CongestionType != null) entity.ConjunctivaCongestionType = conjunctivaData.CongestionType;
                if (conjunctivaData.CongestionLocation != null) entity.ConjunctivaCongestionType = conjunctivaData.CongestionLocation;
                if (conjunctivaData.Hemorrhage.HasValue) entity.ConjunctivaHemorrhage = conjunctivaData.Hemorrhage.Value;
                if (conjunctivaData.HemorrhageDescription != null) entity.ConjunctivaHemorrhageLocation = conjunctivaData.HemorrhageDescription;
                if (conjunctivaData.Laceration.HasValue) entity.ConjunctivaLaceration = conjunctivaData.Laceration.Value;
                if (conjunctivaData.LacerationLocation != null) entity.ConjunctivaLacerationLocation = conjunctivaData.LacerationLocation;
                if (conjunctivaData.Ischemia.HasValue) entity.ConjunctivaEdema = conjunctivaData.Ischemia.Value;
                if (conjunctivaData.Edema.HasValue) entity.ConjunctivaEdema = conjunctivaData.Edema.Value;
                if (conjunctivaData.Papilla.HasValue) entity.ConjunctivaPapilla = conjunctivaData.Papilla.Value;
                if (conjunctivaData.Follicle.HasValue) entity.ConjunctivaFollicle = conjunctivaData.Follicle.Value;
                if (conjunctivaData.Keratinization.HasValue) entity.ConjunctivaKeratinization = conjunctivaData.Keratinization.Value;
                if (conjunctivaData.Scar.HasValue) entity.ConjunctivaScar = conjunctivaData.Scar.Value;
                if (conjunctivaData.Discharge != null) entity.ConjunctivaDischarge = conjunctivaData.Discharge;
                if (conjunctivaData.FluoresceinStain.HasValue) entity.FluoresceinStain = conjunctivaData.FluoresceinStain.Value;
                if (conjunctivaData.Pterygium.HasValue) entity.Pterygium = conjunctivaData.Pterygium.Value;
                if (conjunctivaData.PterygiumLocation != null) entity.PterygiumLocation = conjunctivaData.PterygiumLocation;
                if (conjunctivaData.PterygiumSize != null) entity.PterygiumSize = conjunctivaData.PterygiumSize;
                if (conjunctivaData.HasTumor.HasValue) entity.HasTumor = conjunctivaData.HasTumor.Value;
                if (conjunctivaData.TumorNature != null) entity.TumorNature = conjunctivaData.TumorNature;
                if (conjunctivaData.TumorLocation != null) entity.TumorLocation = conjunctivaData.TumorLocation;
                if (conjunctivaData.TumorSize != null) entity.TumorSize = conjunctivaData.TumorSize;
                if (conjunctivaData.FornixStatus != null) entity.FornixStatus = conjunctivaData.FornixStatus;
                if (conjunctivaData.SymblepharonHeight != null) entity.SymblepharonHeight = conjunctivaData.SymblepharonHeight;
                if (conjunctivaData.SymblepharonWidth != null) entity.SymblepharonWidth = conjunctivaData.SymblepharonWidth;
                if (conjunctivaData.OtherFindings != null) entity.ConjunctivaOther = conjunctivaData.OtherFindings;
            }
        }

        /// <summary>
        /// Maps cornea exam data to EyeCornea entity.
        /// </summary>
        private static void MapEyeCornea(EyeCornea entity, UpdateEyeCorneaExamData data)
        {
            if (data.Clarity != null) entity.Clarity = data.Clarity;
            if (data.Scar != null) entity.BandKeratopathy = true;
            if (data.Size != null) entity.Size = data.Size;
            if (data.Shape != null) entity.Shape = data.Shape;
            if (data.DiameterMm.HasValue) entity.DiameterMm = data.DiameterMm;
            if (data.Sensation != null) entity.Sensation = data.Sensation;
            if (data.EpitheliumStatus != null) entity.BandKeratopathy = true;
            if (data.EpitheliumPunctate.HasValue) entity.EpitheliumPunctate = data.EpitheliumPunctate.Value;
            if (data.EpitheliumEdemaLevel != null) entity.EpitheliumBullous = data.EpitheliumEdemaLevel;
            if (data.EpitheliumLoss != null) entity.EpitheliumLoss = data.EpitheliumLoss;
            if (data.StromaEdemaLevel != null) entity.StromaEdema = data.StromaEdemaLevel;
            if (data.StromaInfiltrate != null) entity.StromaInfiltrate = data.StromaInfiltrate;
            if (data.StromaThinning != null) entity.StromaThinning = data.StromaThinning;
            if (data.Ulcer.HasValue) entity.Ulcer = data.Ulcer.Value;
            if (data.UlcerLocation != null) entity.UlcerLocation = data.UlcerLocation;
            if (data.UlcerSize != null) entity.UlcerSize = data.UlcerSize;
            if (data.UlcerDescription != null) entity.EndotheliumFolds = data.UlcerDescription;
            if (data.PosteriorDeposit != null) entity.PosteriorSurfaceDeposit = data.PosteriorDeposit;
            if (data.PosteriorDepositLocation != null) entity.PosteriorDepositLocation = data.PosteriorDepositLocation;
            if (data.Abscess.HasValue) entity.StromaInfiltrate = "Áp xe";
            if (data.Descemetocele.HasValue) entity.StromaEdema = "Descemetocele";
            if (data.BloodStaining.HasValue) entity.EpitheliumBullous = "Blood staining";
            if (data.Perforation.HasValue) entity.Perforation = data.Perforation.Value;
            if (data.PerforationDiameterMm.HasValue) entity.PerforationDiameterMm = data.PerforationDiameterMm;
            if (data.PerforationLocation != null) entity.PerforationLocation = data.PerforationLocation;
            if (data.SeidelTest != null) entity.SeidelTest = data.SeidelTest;
            if (data.Laceration.HasValue) entity.Laceration = data.Laceration.Value;
            if (data.LacerationSutured.HasValue) entity.LacerationSutured = data.LacerationSutured;
            if (data.LacerationLocation != null) entity.LacerationLocation = data.LacerationLocation;
            if (data.LacerationSize != null) entity.LacerationSize = data.LacerationSize;
            if (data.LacerationType != null)
            {
                entity.LacerationType = data.LacerationType;
                entity.TissueEntrapped = data.LacerationType?.Contains("Kẹt") == true;
            }
            if (data.AnatomicalReduction.HasValue) entity.LacerationSutured = data.AnatomicalReduction.Value;
            if (data.Neovascularization.HasValue) entity.Neovascularization = data.Neovascularization.Value;
            if (data.NeovascularizationDepth != null) entity.NeovascularizationLocation = data.NeovascularizationDepth;
            if (data.NeovascularizationExtent != null) entity.NeovascularizationExtent = data.NeovascularizationExtent;
            if (data.LimbalStatus != null) entity.LimbalStemDeficiency = true;
            if (data.InflammationType != null) entity.StromaInfiltrate = data.InflammationType;
            if (data.InflammationDepth != null) entity.NeovascularizationLocation = data.InflammationDepth;
            if (data.Episcleritis.HasValue) entity.BandKeratopathy = true;
            if (data.Staphyloma.HasValue) entity.BandKeratopathy = true;
            if (data.ForeignBody.HasValue) entity.Laceration = data.ForeignBody.Value;
            if (data.ForeignBodyDescription != null) entity.LacerationType = data.ForeignBodyDescription;
            if (data.OtherFindings != null) entity.NeovascularizationExtent = data.OtherFindings;
        }

        /// <summary>
        /// Maps anterior chamber and iris/pupil exam data to EyeAcIris entity.
        /// </summary>
        private static void MapEyeAcIris(EyeAcIris entity, UpdateEyeAnteriorChamberData? acData, UpdateEyeIrisPupilData? irisData)
        {
            if (acData != null)
            {
                if (acData.Depth == "Xẹp tiền phòng")
                {
                    entity.AcFlat = true;
                    entity.AcDepthMm = null;
                }
                else if (acData.Depth == "Sâu")
                {
                    entity.AcFlat = false;
                    entity.AcDepthMm = acData.DepthMm ?? 999;
                }
                else
                {
                    if (acData.DepthMm.HasValue) entity.AcDepthMm = acData.DepthMm;
                }
                if (acData.HerickClassification != null) entity.AcDepthHerick = acData.HerickClassification;
                if (acData.VitreousInAC.HasValue) entity.AcLensMaterial = acData.VitreousInAC.Value;
                if (acData.Pus.HasValue) entity.AcPus = acData.Pus.Value;
                if (acData.Pus.HasValue && acData.Pus.Value && acData.PusMm.HasValue) entity.AcPusMm = acData.PusMm;
                if (acData.Exudate.HasValue) entity.AcExudate = acData.Exudate.Value;
                if (acData.ExudateDescription != null) entity.AcExudateDescription = acData.ExudateDescription;
                if (acData.Tyndall != null) entity.AcTyndall = acData.Tyndall;
                if (acData.Hemorrhage.HasValue) entity.AcHemorrhage = acData.Hemorrhage.Value;
                if (acData.HemorrhageLevel != null) entity.AcHemorrhageLevel = acData.HemorrhageLevel;
                if (acData.ForeignBody.HasValue) entity.AcForeignBody = acData.ForeignBody.Value;
                if (acData.OtherFindings != null) entity.AcOtherFindings = acData.OtherFindings;
            }

            if (irisData != null)
            {
                if (irisData.IrisColor != null) entity.IrisColor = irisData.IrisColor;
                if (irisData.IrisCondition != null)
                {
                    entity.IrisCondition = irisData.IrisCondition;
                    entity.IrisDegeneration = irisData.IrisCondition == "Thoái hóa";
                }
                if (irisData.IrisDegeneration.HasValue) entity.IrisDegeneration = irisData.IrisDegeneration.Value;
                if (irisData.IrisNeovascularization.HasValue) entity.IrisNeovascularization = irisData.IrisNeovascularization.Value;
                if (irisData.IrisCiliaryProcesses.HasValue) entity.IrisCiliaryProcesses = irisData.IrisCiliaryProcesses.Value;
                if (irisData.KoeppeNodules.HasValue) entity.IrisKoeppeNodules = irisData.KoeppeNodules.Value;
                if (irisData.BusaccaNodules.HasValue) entity.IrisBusaccaNodules = irisData.BusaccaNodules.Value;
                if (irisData.IrisRootTear.HasValue) entity.IrisRootTear = irisData.IrisRootTear.Value;
                if (irisData.IrisRootTearDegree != null) entity.IrisRootTearDegree = irisData.IrisRootTearDegree;
                if (irisData.IrisLoss.HasValue) entity.IrisLoss = irisData.IrisLoss.Value;
                if (irisData.IrisPerforation.HasValue) entity.IrisPerforation = irisData.IrisPerforation.Value;
                if (irisData.PupilDiameterMm.HasValue) entity.PupilDiameterMm = irisData.PupilDiameterMm;
                if (irisData.PupilShape != null)
                {
                    entity.PupilRound = irisData.PupilShape == "Tròn" || string.IsNullOrEmpty(irisData.PupilShape);
                    entity.PupilIrregular = irisData.PupilShape == "Méo";
                    entity.PupilSychiae = irisData.PupilShape == "Dính";
                    if (irisData.PupilShape == "Dính" && irisData.PupilPosition != null) entity.PupilSynechiaeLocation = irisData.PupilPosition;
                }
                if (irisData.PupilPosition != null && entity.PupilSynechiaeLocation == null) entity.PupilSynechiaeLocation = irisData.PupilPosition;
                if (irisData.PupilReflex != null)
                {
                    entity.PupilReflex = irisData.PupilReflex;
                    entity.PupilLightReflex = irisData.PupilReflex;
                    entity.PupilParalyzed = irisData.PupilReflex == "Mất";
                }
                if (irisData.PtdtTest.HasValue) entity.PupilPtdtTest = irisData.PtdtTest.Value ? "Có" : "Không";
                if (irisData.PupilDilated.HasValue) entity.PupilDilated = irisData.PupilDilated.Value;
                if (irisData.FundusReflex != null) entity.FundusReflex = irisData.FundusReflex;
                if (irisData.OtherFindings != null) entity.AcOtherFindings = irisData.OtherFindings;
            }
        }

        /// <summary>
        /// Maps lens and vitreous exam data to EyeLensVitreous entity.
        /// </summary>
        private static void MapEyeLensVitreous(EyeLensVitreous entity, UpdateEyeLensData? lensData, UpdateEyeVitreousData? vitreousData)
        {
            if (lensData != null)
            {
                if (string.IsNullOrEmpty(lensData.Status) || lensData.Status == "Trong")
                    entity.LensClear = true;
                else if (lensData.Status == "Đục")
                    entity.LensClear = false;
                if (lensData.Status != null) entity.LensOpacityType = lensData.OpacityType;
                if (lensData.OpacityLocation != null) entity.LensOpacityLocation = lensData.OpacityLocation;
                if (lensData.Status == "Vỡ") entity.LensRupture = true;
                if (lensData.Subluxation.HasValue) entity.LensSubluxation = lensData.Subluxation.Value;
                if (lensData.LensInAnterior.HasValue) entity.LensIntoAnterior = lensData.LensInAnterior.Value;
                if (lensData.LensInVitreous.HasValue) entity.LensIntoVitreous = lensData.LensInVitreous.Value;
                if (lensData.Purulent.HasValue) entity.LensPurulent = lensData.Purulent.Value;
                if (lensData.AnteriorPigmentation.HasValue) entity.LensAnteriorPigmentation = lensData.AnteriorPigmentation.Value;
                if (lensData.IolPresent.HasValue) entity.LensIolPresent = lensData.IolPresent.Value;
                if (lensData.IolStatus != null) entity.LensIolStatus = lensData.IolStatus;
                if (lensData.IolPosition != null) entity.LensIolPosition = lensData.IolPosition;
                if (lensData.OtherFindings != null) entity.LensOpacityType = lensData.OtherFindings;
            }

            if (vitreousData != null)
            {
                if (string.IsNullOrEmpty(vitreousData.Status) || vitreousData.Status == "Sạch")
                    entity.VitreousClear = true;
                else if (vitreousData.Status == "Đục")
                    entity.VitreousClear = false;
                if (vitreousData.Status == "Đục") entity.VitreousOpacity = true;
                if (vitreousData.OpacityLevel != null && vitreousData.OpacityLevel != "Sạch") entity.VitreousOpacity = true;
                if (vitreousData.Hemorrhage.HasValue) entity.VitreousHemorrhage = vitreousData.Hemorrhage.Value || vitreousData.Status == "Xuất huyết";
                if (vitreousData.Pvd.HasValue) entity.VitreousPvd = vitreousData.Pvd.Value;
                if (vitreousData.Tyndall != null) entity.VitreousTyndall = vitreousData.Tyndall;
                if (vitreousData.Organized.HasValue) entity.VitreousOrganized = vitreousData.Organized.Value;
                if (vitreousData.Purulent.HasValue) entity.VitreousPurulent = vitreousData.Purulent.Value || vitreousData.Status == "Viêm mủ";
                if (vitreousData.ForeignBody.HasValue) entity.VitreousForeignBody = vitreousData.ForeignBody.Value;
                if (vitreousData.OtherFindings != null) entity.VitreousTyndall = vitreousData.OtherFindings;
            }
        }

        /// <summary>
        /// Maps sclera exam data to EyeSclera entity.
        /// </summary>
        private static void MapEyeSclera(EyeSclera entity, UpdateEyeScleraExamData data)
        {
            if (data.Status != null)
            {
                entity.ScleraNormal = string.IsNullOrEmpty(data.Status) || data.Status == "Bình thường";
                entity.ScleraEctasia = data.Status == "Giãn lồi";
                entity.OldSurgeryScar = data.Status == "Sẹo";
            }
            if (data.Laceration.HasValue) entity.ScleraLaceration = data.Laceration.Value;
            if (data.LacerationSutured.HasValue) entity.ScleraLacerationSutured = data.LacerationSutured;
            if (data.LacerationUnsutured.HasValue) entity.ScleraLacerationSutured = !data.LacerationUnsutured.Value;
            if (data.LacerationLocation != null) entity.ScleraLacerationLocation = data.LacerationLocation;
            if (data.LacerationSize != null) entity.ScleraLacerationSize = data.LacerationSize;
            if (data.TissueEntrapped.HasValue) entity.ScleraTissueEntrapped = data.TissueEntrapped.Value;
            if (data.OtherFindings != null) entity.ScleraLacerationLocation = data.OtherFindings;
        }

        /// <summary>
        /// Maps fundus disc and macula exam data to EyeFundusDiscMacula entity.
        /// </summary>
        private static void MapEyeFundusDiscMacula(EyeFundusDiscMacula entity, UpdateEyeFundusDiscMaculaData data)
        {
            if (data.DiscStatus != null)
            {
                entity.OpticDiscNormal = string.IsNullOrEmpty(data.DiscStatus) || data.DiscStatus == "Bình thường";
                entity.OpticDiscEdema = data.DiscStatus == "Phù";
                entity.OpticDiscAtrophy = data.DiscStatus == "Teo";
                entity.OpticDiscPallor = data.DiscStatus == "Bạc màu";
            }
            if (data.DiscColor != null) entity.OpticDiscColor = data.DiscColor;
            if (data.CdRatio != null) entity.OpticDiscCupRatio = data.CdRatio;
            if (data.RimStatus != null) entity.OpticDiscRimStatus = data.RimStatus;
            if (data.RimLocation != null) entity.OpticDiscRimLocation = data.RimLocation;
            if (data.VesselChange != null) entity.OpticDiscVesselChange = data.VesselChange;
            if (data.DiscHemorrhage.HasValue) entity.OpticDiscHemorrhage = data.DiscHemorrhage.Value;
            if (data.Neovascularization.HasValue) entity.OpticDiscNeovascularization = data.Neovascularization.Value;
            if (data.NeovascularizationDegree != null) entity.OpticDiscCupRatio = data.NeovascularizationDegree;
            if (data.DiscNotVisible.HasValue) entity.OpticDiscNotVisible = data.DiscNotVisible.Value;

            if (data.MaculaStatus != null)
            {
                entity.MaculaNormal = string.IsNullOrEmpty(data.MaculaStatus) || data.MaculaStatus == "Bình thường";
                entity.MaculaCondition = data.MaculaStatus;
            }
            if (data.MaculaReflexAbsent.HasValue) entity.MaculaReflexAbsent = data.MaculaReflexAbsent.Value;
            if (data.MaculaEdemaType != null) entity.MaculaEdemaType = data.MaculaEdemaType;
            if (data.MaculaHoleDegree != null) entity.MaculaHoleDegree = data.MaculaHoleDegree;
            if (data.MaculaScar.HasValue) entity.MaculaScar = data.MaculaScar.Value;
            if (data.SerousDetachment.HasValue) entity.MaculaSerousDetachment = data.SerousDetachment.Value;
            if (data.MaculaHemorrhage.HasValue) entity.MaculaHemorrhage = data.MaculaHemorrhage.Value;
            if (data.MaculaCondition != null) entity.MaculaCondition = data.MaculaCondition;

            if (data.ChoroidStatus != null)
            {
                entity.ChoroidalNormal = string.IsNullOrEmpty(data.ChoroidStatus) || data.ChoroidStatus == "Bình thường";
            }
            if (data.ChoroidFindings != null) entity.ChoroidalFindings = data.ChoroidFindings;

            if (data.ChorioretinitisActive.HasValue) entity.ChorioretinitisActive = data.ChorioretinitisActive.Value;
            if (data.ChorioretinitisScar.HasValue) entity.ChorioretinitisScar = data.ChorioretinitisScar.Value;
            if (data.ChorioretinitisCount.HasValue) entity.ChorioretinitisCount = data.ChorioretinitisCount.Value;
            if (data.ChorioretinitisLocation != null) entity.ChorioretinitisLocation = data.ChorioretinitisLocation;

            if (data.CNV.HasValue) entity.ChoroidalNeovascularization = data.CNV.Value;
            if (data.OtherFindings != null) entity.ChoroidalFindings = data.OtherFindings;
        }

        /// <summary>
        /// Maps fundus retina and vessel exam data to EyeFundusRetinaVessel entity.
        /// </summary>
        private static void MapEyeFundusRetinaVessel(EyeFundusRetinaVessel entity, UpdateEyeFundusRetinaVesselData data)
        {
            if (data.VesselStatus != null)
            {
                entity.VesselNormal = string.IsNullOrEmpty(data.VesselStatus) || data.VesselStatus == "Bình thường";
                entity.VesselStatus = data.VesselStatus;
            }
            if (data.ArteryOcclusion != null) entity.ArteryOcclusionType = data.ArteryOcclusion;
            if (data.VeinOcclusion != null) entity.VeinOcclusionType = data.VeinOcclusion;
            if (data.OcclusionType != null)
            {
                entity.OcclusionType = data.OcclusionType;
                entity.OcclusionEdema = data.RetinalEdema == true;
                entity.OcclusionIschemia = data.OcclusionType.Contains("Thiếu máu", StringComparison.OrdinalIgnoreCase);
            }

            if (data.RetinaStatus != null)
            {
                entity.RetinaNormal = string.IsNullOrEmpty(data.RetinaStatus) || data.RetinaStatus == "Bình thường";
                entity.RetinalCondition = data.RetinaStatus;
            }
            if (data.RetinalCondition != null) entity.RetinalCondition = data.RetinalCondition;
            if (data.RetinalEdema.HasValue) entity.RetinaEdema = data.RetinalEdema.Value;

            if (data.Hemorrhage.HasValue)
            {
                entity.RetinaHemorrhageSuperficial = data.Hemorrhage.Value;
                entity.RetinaHemorrhageDeep = data.Hemorrhage.Value;
            }
            if (data.HemorrhageType != null)
            {
                entity.HemorrhageLocation = data.HemorrhageType;
                entity.RetinaHemorrhageSuperficial = data.HemorrhageType.Contains("nông", StringComparison.OrdinalIgnoreCase);
                entity.RetinaHemorrhageDeep = data.HemorrhageType.Contains("sâu", StringComparison.OrdinalIgnoreCase);
            }

            if (data.ExudateType != null)
            {
                entity.ExudateType = data.ExudateType;
                entity.RetinaExudateHard = data.ExudateType.Contains("Cứng", StringComparison.OrdinalIgnoreCase);
                entity.RetinaExudateCottonWool = data.ExudateType.Contains("bông", StringComparison.OrdinalIgnoreCase);
            }

            if (data.Degeneration.HasValue && data.DegenerationType != null)
            {
                entity.RetinaDegenerationPeripheral = data.Degeneration.Value && data.DegenerationType.Contains("biên", StringComparison.OrdinalIgnoreCase);
                entity.RetinaDegenerationCentral = data.Degeneration.Value && data.DegenerationType.Contains("trung tâm", StringComparison.OrdinalIgnoreCase);
            }
            if (data.DegenerationDescription != null) entity.DegenerativeDescription = data.DegenerationDescription;

            if (data.Detachment.HasValue) entity.RetinalDetachment = data.Detachment.Value;
            if (data.DetachmentLevel != null) entity.RetinalDetachmentLevel = data.DetachmentLevel;
            if (data.RetinalTear.HasValue) entity.RetinalTear = data.RetinalTear.Value;
            if (data.TearCount.HasValue) entity.RetinalTearCount = data.TearCount;
            if (data.TearLocation != null) entity.RetinalTearLocation = data.TearLocation;
            if (data.TearMorphology != null) entity.RetinalTearMorphology = data.TearMorphology;

            if (data.Iofb.HasValue) entity.IntraocularForeignBody = data.Iofb.Value;
            if (data.IofbLocation != null) entity.IofbLocation = data.IofbLocation;
            if (data.IofbSize != null) entity.IofbSize = data.IofbSize;

            if (data.Vasculitis.HasValue && data.Vasculitis.Value)
            {
                entity.RetinalCondition = "Viêm mao mạch";
            }
            if (data.RetinalNeovascularization.HasValue)
            {
                entity.ChoroidalNeovascularization = data.RetinalNeovascularization.Value;
                entity.ChoroidalNeovesselsSubretinal = data.RetinalNeovascularization.Value;
            }

            if (data.BmscDetachment.HasValue)
            {
                var extras = new List<string>();
                if (!string.IsNullOrEmpty(entity.RetinaVesselExtras))
                    extras.AddRange(entity.RetinaVesselExtras.Split(',', StringSplitOptions.RemoveEmptyEntries));
                if (data.BmscDetachment.Value && !extras.Contains("bmsc_detachment"))
                    extras.Add("bmsc_detachment");
                else if (!data.BmscDetachment.Value && extras.Contains("bmsc_detachment"))
                    extras.Remove("bmsc_detachment");
                entity.RetinaVesselExtras = string.Join(",", extras);
            }

            if (!string.IsNullOrEmpty(data.CombinedFindings)) entity.RetinaVesselExtras = data.CombinedFindings;
            if (!string.IsNullOrEmpty(data.OtherFindings)) entity.RetinaVesselExtras = data.OtherFindings;
        }

        /// <summary>
        /// Maps orbit exam data to EyeOrbit entity.
        /// </summary>
        private static void MapEyeOrbit(EyeOrbit entity, EyeOrbitData data)
        {
            if (data.Status != null) entity.OrbitalStatus = data.Status;
            if (data.ForeignBodyDescription != null) entity.OrbitalForeignBodyDescription = data.ForeignBodyDescription;
            if (data.EomStatus != null) entity.EomStatus = data.EomStatus;
            if (data.EomFindings != null) entity.EomFindings = data.EomFindings;
            if (data.EyeballStatus != null) entity.EyeballStatus = data.EyeballStatus;
            if (data.EyeballTexture != null) entity.EyeballTexture = data.EyeballTexture;
        }

        /// <summary>
        /// Parses a decimal value from string.
        /// Supports: plain decimals ("15", "3.5"), Snellen format ("20/20", "20/40").
        /// </summary>
        private static decimal? ParseDecimal(string? value)
        {
            if (string.IsNullOrEmpty(value)) return null;

            if (value.Contains('/'))
            {
                var parts = value.Split('/');
                if (parts.Length == 2 &&
                    decimal.TryParse(parts[0], out var numerator) &&
                    decimal.TryParse(parts[1], out var denominator) &&
                    denominator != 0)
                {
                    return (numerator / denominator) * 20;
                }
                return null;
            }

            if (decimal.TryParse(value, out var result)) return result;
            return null;
        }

        /// <summary>
        /// Gets the localized Vietnamese label for a record type enum value.
        /// </summary>
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

        #endregion
    }
}
