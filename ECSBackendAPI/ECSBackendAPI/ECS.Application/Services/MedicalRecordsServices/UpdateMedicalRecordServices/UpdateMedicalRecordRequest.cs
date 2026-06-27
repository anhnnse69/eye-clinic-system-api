using ECS.Domain.Enums;

namespace ECS.Application.Services.MedicalRecordsServices.UpdateMedicalRecordServices
{
    /// <summary>
    /// Request object for updating a medical record.
    /// UC41 - Edit Medical Record
    /// Supports 6 standard medical record templates:
    /// - MS21: Chấn thương (Trauma)
    /// - MS22: Bán phần trước (Anterior Segment)
    /// - MS23: Đáy mắt (Fundus)
    /// - MS24: Glôcôm (Glaucoma)
    /// - MS25: Lác, sụp mi (Strabismus/Ptosis)
    /// - MS26: Mắt trẻ em (Pediatric)
    /// </summary>
    public class UpdateMedicalRecordRequest
    {
        /// <summary>
        /// Record type (MS21_TRAUMA, MS22_ANTERIOR, MS23_FUNDUS, MS24_GLAUCOMA, MS25_STRABISMUS_PTOSIS, MS26_PEDIATRIC).
        /// </summary>
        public string? RecordType { get; set; }

        // ==================== E. MS21/MS24/MS25 Specific History Fields ====================

        // MS21 - Trauma History
        /// <summary>
        /// Cause of trauma. Maps to TraumaRecord.InjuryCause.
        /// </summary>
        public string? TraumaCause { get; set; }
        /// <summary>
        /// Time when trauma occurred. Maps to TraumaRecord.InjuryTime.
        /// </summary>
        public DateTime? TraumaTime { get; set; }
        /// <summary>
        /// Prior treatment methods. Maps to TraumaRecord.PriorTreatment.
        /// </summary>
        public string? TraumaPriorTreatment { get; set; }
        /// <summary>
        /// Clinical course after treatment. Maps to TraumaRecord.PostTreatmentCourse.
        /// </summary>
        public string? TraumaPostTreatmentCourse { get; set; }

        // MS24 - Glaucoma History
        /// <summary>
        /// Duration of glaucoma symptoms.
        /// </summary>
        public string? GlaucomaSymptomDuration { get; set; }
        /// <summary>
        /// Previously visited healthcare facilities.
        /// </summary>
        public string? GlaucomaPriorFacility { get; set; }
        /// <summary>
        /// Prior treatment methods.
        /// </summary>
        public string? GlaucomaPriorTreatment { get; set; }
        /// <summary>
        /// Other eye disease history.
        /// </summary>
        public string? GlaucomaHistoryEye { get; set; }
        /// <summary>
        /// Family history of glaucoma.
        /// </summary>
        public string? GlaucomaFamilyHistory { get; set; }

        // MS25 - Strabismus History
        /// <summary>
        /// Time of strabismus onset. Maps to StrabismusPtosisRecord.AcquiredOnset.
        /// </summary>
        public string? StrabismusOnsetTime { get; set; }
        /// <summary>
        /// Main symptom: Esotropia, Exotropia, Hypertropia, Ptosis, or Nystagmus.
        /// </summary>
        public string? StrabismusMainSymptom { get; set; }

        // MS25 - Strabismus Congenital/Acquired
        /// <summary>
        /// Whether strabismus is congenital.
        /// </summary>
        public bool? StrabismusCongenital { get; set; }
        /// <summary>
        /// Whether strabismus is acquired.
        /// </summary>
        public bool? StrabismusAcquired { get; set; }

        // MS26 - Pediatric History
        /// <summary>
        /// Pathological pregnancy history.
        /// </summary>
        public string? PediatricPregnancyHistory { get; set; }
        /// <summary>
        /// Intellectual development status.
        /// </summary>
        public string? PediatricDevelopment { get; set; }

        // ==================== VITAL SIGNS ====================
        /// <summary>
        /// Pulse rate (beats per minute).
        /// </summary>
        public int? VitalPulse { get; set; }
        /// <summary>
        /// Body temperature (Celsius).
        /// </summary>
        public decimal? VitalTemperature { get; set; }
        /// <summary>
        /// Blood pressure (mmHg).
        /// </summary>
        public string? VitalBloodPressure { get; set; }
        /// <summary>
        /// Respiratory rate (breaths per minute).
        /// </summary>
        public int? VitalRespiratoryRate { get; set; }
        /// <summary>
        /// Body weight in kilograms.
        /// </summary>
        public decimal? VitalWeightKg { get; set; }

        // ==================== III. KHÁM BỆNH (Examination) ====================

        // 1. Khám chuyên khoa - Thị lực & Nhãn áp vào viện
        /// <summary>
        /// Right eye basic examination data (visual acuity, intraocular pressure).
        /// </summary>
        public UpdateEyeBasicExamData? RightEyeBasic { get; set; }
        /// <summary>
        /// Left eye basic examination data (visual acuity, intraocular pressure).
        /// </summary>
        public UpdateEyeBasicExamData? LeftEyeBasic { get; set; }

        // 2. Mi mắt (Eyelid)
        /// <summary>
        /// Right eye eyelid examination data.
        /// </summary>
        public UpdateEyeEyelidData? RightEyeEyelid { get; set; }
        /// <summary>
        /// Left eye eyelid examination data.
        /// </summary>
        public UpdateEyeEyelidData? LeftEyeEyelid { get; set; }

        // 3. Kết mạc (Conjunctiva)
        /// <summary>
        /// Right eye conjunctiva examination data.
        /// </summary>
        public UpdateEyeConjunctivaData? RightEyeConjunctiva { get; set; }
        /// <summary>
        /// Left eye conjunctiva examination data.
        /// </summary>
        public UpdateEyeConjunctivaData? LeftEyeConjunctiva { get; set; }

        // 4. Giác mạc (Cornea)
        /// <summary>
        /// Right eye cornea examination data.
        /// </summary>
        public UpdateEyeCorneaExamData? RightEyeCornea { get; set; }
        /// <summary>
        /// Left eye cornea examination data.
        /// </summary>
        public UpdateEyeCorneaExamData? LeftEyeCornea { get; set; }

        // 5. Củng mạc (Sclera)
        /// <summary>
        /// Right eye sclera examination data.
        /// </summary>
        public UpdateEyeScleraExamData? RightEyeSclera { get; set; }
        /// <summary>
        /// Left eye sclera examination data.
        /// </summary>
        public UpdateEyeScleraExamData? LeftEyeSclera { get; set; }

        // 6. Tiền phòng (Anterior Chamber)
        /// <summary>
        /// Right eye anterior chamber examination data.
        /// </summary>
        public UpdateEyeAnteriorChamberData? RightEyeAnteriorChamber { get; set; }
        /// <summary>
        /// Left eye anterior chamber examination data.
        /// </summary>
        public UpdateEyeAnteriorChamberData? LeftEyeAnteriorChamber { get; set; }

        // 7. Mống mắt & Đồng tử (Iris & Pupil)
        /// <summary>
        /// Right eye iris and pupil examination data.
        /// </summary>
        public UpdateEyeIrisPupilData? RightEyeIrisPupil { get; set; }
        /// <summary>
        /// Left eye iris and pupil examination data.
        /// </summary>
        public UpdateEyeIrisPupilData? LeftEyeIrisPupil { get; set; }

        // 8. Thể thủy tinh (Lens)
        /// <summary>
        /// Right eye lens examination data.
        /// </summary>
        public UpdateEyeLensData? RightEyeLens { get; set; }
        /// <summary>
        /// Left eye lens examination data.
        /// </summary>
        public UpdateEyeLensData? LeftEyeLens { get; set; }

        // 9. Dịch kính (Vitreous)
        /// <summary>
        /// Right eye vitreous examination data.
        /// </summary>
        public UpdateEyeVitreousData? RightEyeVitreous { get; set; }
        /// <summary>
        /// Left eye vitreous examination data.
        /// </summary>
        public UpdateEyeVitreousData? LeftEyeVitreous { get; set; }

        // 10. Đáy mắt - Đĩa thị & Hoàng điểm (Optic Disc & Macula)
        /// <summary>
        /// Right eye fundus optic disc and macula examination data.
        /// </summary>
        public UpdateEyeFundusDiscMaculaData? RightEyeFundusDiscMacula { get; set; }
        /// <summary>
        /// Left eye fundus optic disc and macula examination data.
        /// </summary>
        public UpdateEyeFundusDiscMaculaData? LeftEyeFundusDiscMacula { get; set; }

        // 11. Đáy mắt - Võng mạc & Mạch máu (Retina & Vessels)
        /// <summary>
        /// Right eye fundus retina and vessels examination data.
        /// </summary>
        public UpdateEyeFundusRetinaVesselData? RightEyeFundusRetinaVessel { get; set; }
        /// <summary>
        /// Left eye fundus retina and vessels examination data.
        /// </summary>
        public UpdateEyeFundusRetinaVesselData? LeftEyeFundusRetinaVessel { get; set; }

        // 12. Khám toàn thân (Systemic Exam)
        /// <summary>
        /// Systemic examination data (blood pressure, temperature, pulse, etc.).
        /// </summary>
        public UpdateSystemicExamData? SystemicExam { get; set; }

        // ==================== VI. CHẨN ĐOÁN ====================
        /// <summary>
        /// Main diagnosis.
        /// </summary>
        public string? DiagnosisMain { get; set; }
        /// <summary>
        /// Comorbid diagnosis.
        /// </summary>
        public string? DiagnosisComorbid { get; set; }
        /// <summary>
        /// Differential diagnosis.
        /// </summary>
        public string? DiagnosisDifferential { get; set; }

        // ==================== VII. TIÊN LƯỢNG ====================
        /// <summary>
        /// Prognosis.
        /// </summary>
        public string? Prognosis { get; set; }

        // ==================== VIII. ĐIỀU TRỊ ====================
        /// <summary>
        /// Treatment plan.
        /// </summary>
        public string? TreatmentPlan { get; set; }

        // ==================== NOTES ====================
        /// <summary>
        /// Additional notes.
        /// </summary>
        public string? Notes { get; set; }

        // ==================== B. TỔNG KẾT BỆNH ÁN (Summary) ====================
        /// <summary>
        /// Summary of examination findings.
        /// </summary>
        public string? Summary { get; set; }
        /// <summary>
        /// Final clinical diagnosis.
        /// </summary>
        public string? FinalDiagnosisClinical { get; set; }
        /// <summary>
        /// Final diagnosis cause.
        /// </summary>
        public string? FinalDiagnosisCause { get; set; }
        /// <summary>
        /// Treatment process summary.
        /// </summary>
        public string? TreatmentProcessSummary { get; set; }
        /// <summary>
        /// Surgery summary.
        /// </summary>
        public string? SurgerySummary { get; set; }
        /// <summary>
        /// Discharge condition summary.
        /// </summary>
        public string? DischargeConditionSummary { get; set; }
        /// <summary>
        /// Discharge visual acuity - right eye.
        /// </summary>
        public string? DischargeVaOd { get; set; }
        /// <summary>
        /// Discharge visual acuity - left eye.
        /// </summary>
        public string? DischargeVaOs { get; set; }
        /// <summary>
        /// Discharge intraocular pressure - right eye.
        /// </summary>
        public string? DischargeIopOd { get; set; }
        /// <summary>
        /// Discharge intraocular pressure - left eye.
        /// </summary>
        public string? DischargeIopOs { get; set; }
        /// <summary>
        /// Follow-up plan.
        /// </summary>
        public string? FollowUpPlan { get; set; }

        // ==================== ADMINISTRATIVE & OTHER FIELDS ====================
        /// <summary>
        /// Factor code (MaYeuTo) for administrative classification.
        /// </summary>
        public string? MaYeuTo { get; set; }
        /// <summary>
        /// Patient's age at time of admission.
        /// </summary>
        public int? Age { get; set; }
        /// <summary>
        /// Required diagnostic tests.
        /// </summary>
        public string? RequiredTests { get; set; }
        /// <summary>
        /// Diet plan.
        /// </summary>
        public string? DietPlan { get; set; }
        /// <summary>
        /// Care plan.
        /// </summary>
        public string? CarePlan { get; set; }

        // ==================== EYE EXAMINATIONS ====================
        // Eye Orbit
        /// <summary>
        /// Right eye orbit examination data.
        /// </summary>
        public EyeOrbitData? RightEyeOrbit { get; set; }
        /// <summary>
        /// Left eye orbit examination data.
        /// </summary>
        public EyeOrbitData? LeftEyeOrbit { get; set; }

        // ==================== SUBSPECIALTY RECORDS ====================

        /// <summary>
        /// Trauma record (MS21_TRAUMA).
        /// </summary>
        public UpdateTraumaRecordData? TraumaRecord { get; set; }

        /// <summary>
        /// Trauma surgeries (MS21_TRAUMA).
        /// </summary>
        public List<UpdateTraumaSurgeryData>? TraumaSurgeries { get; set; }

        /// <summary>
        /// Lacrimal record (MS22_ANTERIOR, MS26_PEDIATRIC).
        /// </summary>
        public UpdateLacrimalRecordData? LacrimalRecord { get; set; }

        /// <summary>
        /// Glaucoma record (MS24_GLAUCOMA).
        /// </summary>
        public UpdateGlaucomaRecordData? GlaucomaRecord { get; set; }

        /// <summary>
        /// Glaucoma history entries for surgery/drug records (MS24_GLAUCOMA).
        /// </summary>
        public List<UpdateGlaucomaHistoryData>? GlaucomaHistories { get; set; }

        /// <summary>
        /// Strabismus & Ptosis record (MS25_STRABISMUS_PTOSIS).
        /// </summary>
        public UpdateStrabismusPtosisRecordData? StrabismusPtosisRecord { get; set; }

        /// <summary>
        /// Pediatric record (MS26_PEDIATRIC).
        /// </summary>
        public UpdatePediatricRecordData? PediatricRecord { get; set; }

        // ==================== PRESCRIPTIONS ====================

        /// <summary>
        /// Prescription header.
        /// </summary>
        public UpdatePrescriptionData? Prescription { get; set; }

        /// <summary>
        /// Prescription items (list of medicines).
        /// </summary>
        public List<UpdatePrescriptionItemData>? PrescriptionItems { get; set; }

        /// <summary>
        /// Glasses prescription (optional).
        /// </summary>
        public UpdateGlassesPrescriptionData? GlassesPrescription { get; set; }
    }

    #region Eye Exam Data Classes (Update)

    public class UpdateEyeBasicExamData
    {
        public string? VaUncorrected { get; set; }
        public string? VaCorrected { get; set; }
        public string? VaNear { get; set; }
        public string? VaPinhole { get; set; }
        public string? VaWithGlasses { get; set; }
        public string? IopMmhg { get; set; }
        public string? IopMethod { get; set; }
        public string? AutoRefraction { get; set; }
        public string? Retinoscopy { get; set; }
        public string? SubjectiveRefraction { get; set; }
        public string? VisualField { get; set; }
        public string? EomStatus { get; set; }
        public string? EomNote { get; set; }
        public string? Nystagmus { get; set; }
        public string? NystagmusType { get; set; }
    }

    public class UpdateEyeEyelidData
    {
        public string? Status { get; set; }
        public bool? Ptosis { get; set; }
        public string? PtosisDegree { get; set; }
        public bool? Laceration { get; set; }
        public string? LacerationExtent { get; set; }
        public string? LacerationLocation { get; set; }
        public bool? LacerationSutured { get; set; }
        public bool? LacerationUnsutured { get; set; }
        public string? LacrimalDuctStatus { get; set; }
        public string? LacrimalDuctLocation { get; set; }
        public bool? Scar { get; set; }
        public string? ScarDescription { get; set; }
        public string? OtherFindings { get; set; }
        public bool? Entropion { get; set; }
        public bool? Epicanthus { get; set; }
        public string? EpicanthusType { get; set; }
        public bool? HasTumor { get; set; }
        public string? TumorNature { get; set; }
        public string? TumorLocation { get; set; }
        public string? TumorSize { get; set; }
        public bool? Lagophthalmos { get; set; }
        public bool? LowerLidRetraction { get; set; }
        public string? EyelidDefect { get; set; }
        public string? ChalazionHordeolum { get; set; }
    }

    public class UpdateEyeConjunctivaData
    {
        public string? Status { get; set; }
        public string? CongestionType { get; set; }
        public string? CongestionLocation { get; set; }
        public bool? Hemorrhage { get; set; }
        public string? HemorrhageDescription { get; set; }
        public bool? Laceration { get; set; }
        public string? LacerationLocation { get; set; }
        public bool? Ischemia { get; set; }
        public bool? Edema { get; set; }
        public bool? Papilla { get; set; }
        public bool? Follicle { get; set; }
        public bool? Keratinization { get; set; }
        public bool? Scar { get; set; }
        public string? Discharge { get; set; }
        public bool? FluoresceinStain { get; set; }
        public bool? Pterygium { get; set; }
        public string? PterygiumLocation { get; set; }
        public string? PterygiumSize { get; set; }
        public bool? HasTumor { get; set; }
        public string? TumorNature { get; set; }
        public string? TumorLocation { get; set; }
        public string? TumorSize { get; set; }
        public string? FornixStatus { get; set; }
        public string? SymblepharonHeight { get; set; }
        public string? SymblepharonWidth { get; set; }
        public string? OtherFindings { get; set; }
    }

    public class UpdateEyeCorneaExamData
    {
        public string? Clarity { get; set; }
        public string? Scar { get; set; }
        public string? Size { get; set; }
        public string? Shape { get; set; }
        public decimal? DiameterMm { get; set; }
        public string? EpitheliumStatus { get; set; }
        public bool? EpitheliumPunctate { get; set; }
        public string? EpitheliumEdemaLevel { get; set; }
        public string? EpitheliumLoss { get; set; }
        public string? PosteriorDeposit { get; set; }
        public string? PosteriorDepositLocation { get; set; }
        public string? StromaEdemaLevel { get; set; }
        public string? StromaInfiltrate { get; set; }
        public string? StromaThinning { get; set; }
        public bool? Ulcer { get; set; }
        public string? UlcerLocation { get; set; }
        public string? UlcerSize { get; set; }
        public string? UlcerDescription { get; set; }
        public bool? Abscess { get; set; }
        public bool? Descemetocele { get; set; }
        public bool? BloodStaining { get; set; }
        public bool? Laceration { get; set; }
        public string? LacerationSize { get; set; }
        public string? LacerationLocation { get; set; }
        public string? LacerationType { get; set; }
        public bool? LacerationSutured { get; set; }
        public bool? AnatomicalReduction { get; set; }
        public bool? Perforation { get; set; }
        public decimal? PerforationDiameterMm { get; set; }
        public string? PerforationLocation { get; set; }
        public string? SeidelTest { get; set; }
        public bool? Neovascularization { get; set; }
        public string? NeovascularizationDepth { get; set; }
        public string? NeovascularizationExtent { get; set; }
        public string? LimbalStatus { get; set; }
        public string? Sensation { get; set; }
        public string? InflammationType { get; set; }
        public string? InflammationDepth { get; set; }
        public bool? Episcleritis { get; set; }
        public bool? Staphyloma { get; set; }
        public bool? ForeignBody { get; set; }
        public string? ForeignBodyDescription { get; set; }
        public string? OtherFindings { get; set; }
    }

    public class UpdateEyeScleraExamData
    {
        public string? Status { get; set; }
        public bool? Laceration { get; set; }
        public string? LacerationSize { get; set; }
        public string? LacerationLocation { get; set; }
        public bool? LacerationSutured { get; set; }
        public bool? LacerationUnsutured { get; set; }
        public bool? TissueEntrapped { get; set; }
        public string? OtherFindings { get; set; }
    }

    public class UpdateEyeAnteriorChamberData
    {
        public string? Depth { get; set; }
        public decimal? DepthMm { get; set; }
        public string? HerickClassification { get; set; }
        public bool? VitreousInAC { get; set; }
        public bool? Pus { get; set; }
        public decimal? PusMm { get; set; }
        public bool? Exudate { get; set; }
        public string? ExudateDescription { get; set; }
        public string? Tyndall { get; set; }
        public bool? Hemorrhage { get; set; }
        public string? HemorrhageLevel { get; set; }
        public bool? ForeignBody { get; set; }
        public string? OtherFindings { get; set; }
    }

    public class UpdateEyeIrisPupilData
    {
        public string? IrisColor { get; set; }
        public string? IrisCondition { get; set; }
        public bool? IrisDegeneration { get; set; }
        public bool? IrisNeovascularization { get; set; }
        public bool? IrisCiliaryProcesses { get; set; }
        public bool? KoeppeNodules { get; set; }
        public bool? BusaccaNodules { get; set; }
        public bool? IrisRootTear { get; set; }
        public string? IrisRootTearDegree { get; set; }
        public bool? IrisLoss { get; set; }
        public bool? IrisPerforation { get; set; }
        public decimal? PupilDiameterMm { get; set; }
        public string? PupilShape { get; set; }
        public string? PupilPosition { get; set; }
        public string? PupilReflex { get; set; }
        public bool? PupilDilated { get; set; }
        public bool? PtdtTest { get; set; }
        public string? FundusReflex { get; set; }
        public string? OtherFindings { get; set; }
    }

    public class UpdateEyeLensData
    {
        public string? Status { get; set; }
        public string? OpacityType { get; set; }
        public string? OpacityLocation { get; set; }
        public bool? Subluxation { get; set; }
        public bool? LensInAnterior { get; set; }
        public bool? LensInVitreous { get; set; }
        public bool? Purulent { get; set; }
        public bool? AnteriorPigmentation { get; set; }
        public bool? IolPresent { get; set; }
        public string? IolStatus { get; set; }
        public string? IolPosition { get; set; }
        public string? OtherFindings { get; set; }
    }

    public class UpdateEyeVitreousData
    {
        public string? Status { get; set; }
        public string? OpacityLevel { get; set; }
        public string? Tyndall { get; set; }
        public bool? Hemorrhage { get; set; }
        public bool? Organized { get; set; }
        public bool? Pvd { get; set; }
        public bool? Purulent { get; set; }
        public bool? ForeignBody { get; set; }
        public string? OtherFindings { get; set; }
    }

    public class UpdateEyeFundusDiscMaculaData
    {
        public string? DiscStatus { get; set; }
        public string? DiscColor { get; set; }
        public string? CdRatio { get; set; }
        public string? RimStatus { get; set; }
        public string? RimLocation { get; set; }
        public string? VesselChange { get; set; }
        public bool? DiscHemorrhage { get; set; }
        public bool? Neovascularization { get; set; }
        public string? NeovascularizationDegree { get; set; }
        public bool? DiscNotVisible { get; set; }
        public string? MaculaStatus { get; set; }
        public bool? MaculaReflexAbsent { get; set; }
        public string? MaculaEdemaType { get; set; }
        public string? MaculaHoleDegree { get; set; }
        public bool? MaculaScar { get; set; }
        public bool? SerousDetachment { get; set; }
        public bool? MaculaHemorrhage { get; set; }
        public string? MaculaCondition { get; set; }
        public string? ChoroidStatus { get; set; }
        public string? ChoroidFindings { get; set; }
        public bool? CNV { get; set; }
        public bool? ChorioretinitisActive { get; set; }
        public bool? ChorioretinitisScar { get; set; }
        public int? ChorioretinitisCount { get; set; }
        public string? ChorioretinitisLocation { get; set; }
        public string? OtherFindings { get; set; }
    }

    public class UpdateEyeFundusRetinaVesselData
    {
        public string? VesselStatus { get; set; }
        public string? ArteryOcclusion { get; set; }
        public string? VeinOcclusion { get; set; }
        public string? OcclusionType { get; set; }
        public bool? Vasculitis { get; set; }
        public bool? RetinalNeovascularization { get; set; }
        public string? RetinaStatus { get; set; }
        public string? RetinalCondition { get; set; }
        public bool? RetinalEdema { get; set; }
        public string? EdemaType { get; set; }
        public bool? Hemorrhage { get; set; }
        public string? HemorrhageType { get; set; }
        public string? ExudateType { get; set; }
        public bool? Degeneration { get; set; }
        public string? DegenerationType { get; set; }
        public string? DegenerationDescription { get; set; }
        public bool? Detachment { get; set; }
        public string? DetachmentLevel { get; set; }
        public bool? RetinalTear { get; set; }
        public int? TearCount { get; set; }
        public string? TearLocation { get; set; }
        public string? TearMorphology { get; set; }
        public bool? BmscDetachment { get; set; }
        public bool? Iofb { get; set; }
        public string? IofbLocation { get; set; }
        public string? IofbSize { get; set; }
        public string? CombinedFindings { get; set; }
        public string? OtherFindings { get; set; }
    }

    public class UpdateSystemicExamData
    {
        public string? BloodPressure { get; set; }
        public string? Temperature { get; set; }
        public string? Pulse { get; set; }
        public string? RespiratoryRate { get; set; }
        public string? EndocrineStatus { get; set; }
        public string? EndocrineFindings { get; set; }
        public string? NeuroStatus { get; set; }
        public string? NeuroFindings { get; set; }
        public string? CardiovascularStatus { get; set; }
        public string? CardiovascularFindings { get; set; }
        public string? RespiratoryStatus { get; set; }
        public string? RespiratoryFindings { get; set; }
        public string? DigestiveStatus { get; set; }
        public string? DigestiveFindings { get; set; }
        public string? MusculoskeletalStatus { get; set; }
        public string? MusculoskeletalFindings { get; set; }
        public string? UrogenitalStatus { get; set; }
        public string? UrogenitalFindings { get; set; }
        public string? OtherFindings { get; set; }
    }

    #endregion

    #region Subspecialty Record Data Classes (Update)

    public class UpdateTraumaRecordData
    {
        public string? InjuryCause { get; set; }
        public DateTime? InjuryTime { get; set; }
        public string? PriorTreatment { get; set; }
        public string? PostTreatmentCourse { get; set; }
        public string? OdInjuries { get; set; }
        public string? OsInjuries { get; set; }
        public string? InjuryDetails { get; set; }
        public string? TraumaConclusion { get; set; }
    }

    public class UpdateTraumaSurgeryData
    {
        public Guid? Id { get; set; }
        public DateTime? SurgeryDate { get; set; }
        public string? SurgeryType { get; set; }
        public string? SurgeryDescription { get; set; }
        public string? SurgeonName { get; set; }
        public string? AnesthesiaType { get; set; }
        public string? PostSurgeryCondition { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateLacrimalRecordData
    {
        public string Side { get; set; } = string.Empty;
        public bool IrrigationFree { get; set; } = true;
        public bool IrrigationRegurgitationSame { get; set; } = false;
        public bool IrrigationRegurgitationOpposite { get; set; } = false;
        public string? IrrigationNote { get; set; }
        public string? LacrimalOther { get; set; }
    }

    public class UpdateGlaucomaRecordData
    {
        // Symptoms
        public string? EyePainLevel { get; set; }
        public string? VisionSymptoms { get; set; }
        public string? VisionProgression { get; set; }
        public bool HasPhotophobia { get; set; } = false;
        public bool HasTearing { get; set; } = false;
        public bool HasRedness { get; set; } = false;
        public string? SystemicSymptoms { get; set; }

        // Visual Acuity & IOP
        public string? VaWithoutCorrectionOd { get; set; }
        public string? VaWithoutCorrectionOs { get; set; }
        public string? VaWithCorrectionOd { get; set; }
        public string? VaWithCorrectionOs { get; set; }
        public string? IopOd { get; set; }
        public string? IopOs { get; set; }
        public string? IopMethod { get; set; }
        public string? IopTargetOd { get; set; }
        public string? IopTargetOs { get; set; }

        // History
        public string? HistoryEye { get; set; }
        public string? HistoryEyeSurgery { get; set; }
        public string? PriorEyeSurgeryDetails { get; set; }
        public string? SteroidUse { get; set; }
        public string? SteroidPrescribed { get; set; }

        // Systemic
        public bool HasCardiovascularDisease { get; set; } = false;
        public bool HasHypertension { get; set; } = false;
        public bool HasDiabetes { get; set; } = false;
        public bool HasCarotidFistula { get; set; } = false;
        public string? OtherSystemicDisease { get; set; }

        // Family
        public bool FamilyHasGlaucoma { get; set; } = false;
        public string? FamilyGlaucomaRelation { get; set; }

        // Treatment History
        public string? GlaucomaMedications { get; set; }
        public string? OtherMedications { get; set; }
        public string? TreatmentProgress { get; set; }

        // Classification
        public string? GlaucomaType { get; set; }
        public string? StageOd { get; set; }
        public string? StageOs { get; set; }

        // Examination - Eyelid
        public bool HasEyelidSwelling { get; set; } = false;

        // Examination - Conjunctiva
        public bool HasConjunctivalInjection { get; set; } = false;
        public bool HasFilteringBleb { get; set; } = false;
        public string? BlebLocation { get; set; }
        public string? BlebStatus { get; set; }
        public string? ConjunctivalScarLocation { get; set; }

        // Examination - Cornea
        public string? CornealTransparency { get; set; }
        public string? CornealEdemaLevel { get; set; }
        public string? CornealThickness { get; set; }

        // Examination - Anterior Chamber
        public string? AcDepthSmith { get; set; }
        public string? AcDepthHerick { get; set; }

        // Examination - Gonioscopy
        public string? GonioscopyOd { get; set; }
        public string? GonioscopyOs { get; set; }
        public string? AngleFindings { get; set; }

        // Examination - Iris
        public string? IrisColor { get; set; }
        public string? IrisCondition { get; set; }
        public bool HasIrisNeovascularization { get; set; } = false;

        // Examination - Pupil
        public string? PupilDiameter { get; set; }
        public string? PupilPigmentBorder { get; set; }
        public string? PupilReflexResponse { get; set; }

        // Examination - Lens
        public string? LensStatus { get; set; }

        // Examination - Fundus
        public string? FundusRetinaFindings { get; set; }
        public string? FundusMaculaFindings { get; set; }
        public bool HasCNV { get; set; } = false;
        public bool HasRetinalHemorrhage { get; set; } = false;

        // Examination - Optic Disc
        public string? OpticDiscDescription { get; set; }
        public string? NerveRimOd { get; set; }
        public string? NerveRimOs { get; set; }
        public string? OpticDiscCupRatio { get; set; }
        public string? OpticDiscVesselChange { get; set; }
        public bool HasOpticDiscHemorrhage { get; set; } = false;
        public bool HasRimAtrophy { get; set; } = false;

        // Eye Measurements
        public string? EyeAxialLength { get; set; }

        // Treatment Plan
        public string? TreatmentPlanSurgery { get; set; }
        public string? TreatmentPlanLaser { get; set; }
        public string? TreatmentPlanMedication { get; set; }
        public string? FollowUpPlan { get; set; }
    }

    public class UpdateGlaucomaHistoryData
    {
        public Guid? Id { get; set; }
        public string HistoryType { get; set; } = string.Empty;
        public string? EyeSide { get; set; }
        public int? AttemptNumber { get; set; }
        public string? ProcedureType { get; set; }
        public DateTime? ProcedureDate { get; set; }
        public string? FacilityLevel { get; set; }
        public string? DrugName { get; set; }
        public string? Dosage { get; set; }
        public string? Duration { get; set; }
        public string? Route { get; set; }
        public string? ChangeReason { get; set; }
    }

    public class UpdateStrabismusPtosisRecordData
    {
        // Chief Complaint & Cause
        public bool ChiefStrabismus { get; set; } = false;
        public bool ChiefPtosis { get; set; } = false;
        public bool Congenital { get; set; } = false;
        public bool Acquired { get; set; } = false;
        public string? AcquiredOnset { get; set; }

        // Strabismus Type
        public string? StrabismusType { get; set; }

        // Nystagmus
        public bool Nystagmus { get; set; } = false;
        public string? NystagmusType { get; set; }

        // Treatment History
        public string? PriorAmblyopiaTreatment { get; set; }
        public string? PriorAmblyopiaResult { get; set; }
        public string? PriorSurgery { get; set; }
        public string? PriorSurgeryResult { get; set; }

        // Visual Acuity Before/After Atropine
        public string? VaBeforeAtropineOd { get; set; }
        public string? VaBeforeAtropineOs { get; set; }
        public string? VaAfterAtropineOd { get; set; }
        public string? VaAfterAtropineOs { get; set; }

        // Refraction
        public string? RefractionPreAtropine { get; set; }
        public string? RefractionPostAtropine { get; set; }

        // Pupil Shadow Test
        public string? PupilShadowTestOd { get; set; }
        public string? PupilShadowTestOs { get; set; }

        // Extraocular Motility
        public string? EomGazeTest { get; set; }
        public string? EomGazeIncreaseOd { get; set; }
        public string? EomGazeIncreaseOs { get; set; }
        public string? EomGazeLimitOd { get; set; }
        public string? EomGazeLimitOs { get; set; }

        // Internal Extraocular Motility
        public string? EomInternalOd { get; set; }
        public string? EomInternalOs { get; set; }

        // Convergence Point
        public string? ConvergencePoint { get; set; }

        // Cover Test
        public string? CoverTestResult { get; set; }

        // Hirschberg Test
        public string? HirschbergBeforeAtropine { get; set; }
        public string? HirschbergAfterAtropine { get; set; }

        // Prism Measurement
        public string? PrismNear { get; set; }
        public string? PrismDistance { get; set; }
        public string? PrismUp { get; set; }
        public string? PrismDown { get; set; }

        // Syndrome
        public string? StrabismusSyndrome { get; set; }

        // Synoptophore Test
        public string? SynoptophoreObjective { get; set; }
        public string? SynoptophoreSubjective { get; set; }

        // Binocular Vision
        public string? BinocularStatus { get; set; }
        public string? FusionAmplitude { get; set; }
        public string? RetinalCorrespondence { get; set; }
        public string? Diplopia { get; set; }
        public string? CompensatoryHeadPosture { get; set; }

        // Ptosis Measurements
        public string? PtosisDegreeOd { get; set; }
        public string? PtosisDegreeOs { get; set; }
        public string? LevatorFunctionOd { get; set; }
        public string? LevatorFunctionOs { get; set; }
        public string? MarcusGunn { get; set; }
        public string? BellPhenomenon { get; set; }
        public string? FixationOd { get; set; }
        public string? FixationOs { get; set; }

        // Palpebral Reflex
        public string? PalpebralReflexOd { get; set; }
        public string? PalpebralReflexOs { get; set; }

        // Additional
        public string? Epicanthus { get; set; }
        public string? HemmingAngle { get; set; }
    }

    public class UpdatePediatricRecordData
    {
        public bool Congenital { get; set; } = false;
        public bool Acquired { get; set; } = false;
        public string? AcquiredOnset { get; set; }
        public string? PriorTreatment { get; set; }
        public bool PregnancyIllness { get; set; } = false;
        public string? PregnancyIllnessDetail { get; set; }
        public bool IntellectualDevelopmentNormal { get; set; } = true;
        public string? IntellectualDevelopmentStatus { get; set; }
        public string? ChiefSymptoms { get; set; }
        public bool EntropionOd { get; set; } = false;
        public bool EpicanthusOd { get; set; } = false;
        public bool PtosisOd { get; set; } = false;
        public string? EyelidTumor { get; set; }
        public string? EyelidTumorLocation { get; set; }
        public string? EyelidTumorSize { get; set; }
        public string? EyeballOdStatus { get; set; }
        public string? EyeballOsStatus { get; set; }
        public string? EyeballTexture { get; set; }
        public string? AmblyopiaStatus { get; set; }
        public string? FixationPreferenceOd { get; set; }
        public string? FixationPreferenceOs { get; set; }
        public string? FundusSummaryOd { get; set; }
        public string? FundusSummaryOs { get; set; }
        public string? GeneralHealthStatus { get; set; }
    }

    #endregion

    #region Eye Orbit Data

    /// <summary>
    /// Orbit examination data.
    /// </summary>
    public class EyeOrbitData
    {
        /// <summary>
        /// Overall orbital status: Normal or Pathological.
        /// </summary>
        public string? Status { get; set; }

        // Dị vật (Foreign Body)
        /// <summary>
        /// Whether foreign body is present in orbit.
        /// </summary>
        public bool? ForeignBody { get; set; }
        /// <summary>
        /// Description of foreign body.
        /// </summary>
        public string? ForeignBodyDescription { get; set; }

        // Vận nhãn (Extraocular Movement)
        /// <summary>
        /// Extraocular movement status: Normal or Pathological.
        /// </summary>
        public string? EomStatus { get; set; }
        /// <summary>
        /// Extraocular movement findings.
        /// </summary>
        public string? EomFindings { get; set; }

        // Nhãn cầu (Eyeball)
        /// <summary>
        /// Eyeball status: Atrophic, Proptosis, or Exophthalmometry reading.
        /// </summary>
        public string? EyeballStatus { get; set; }
        /// <summary>
        /// Eyeball texture: Soft, Tense, Large, or Small.
        /// </summary>
        public string? EyeballTexture { get; set; }
    }

    #endregion

    #region Prescription Data Classes (Update)

    public class UpdatePrescriptionData
    {
        public string? Notes { get; set; }
    }

    public class UpdatePrescriptionItemData
    {
        public Guid? Id { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string? Frequency { get; set; }
        public int? DurationDays { get; set; }
        public int Quantity { get; set; }
        public string? Instruction { get; set; }
    }

    public class UpdateGlassesPrescriptionData
    {
        public decimal? SphOd { get; set; }
        public decimal? CylOd { get; set; }
        public int? AxisOd { get; set; }
        public decimal? AddOd { get; set; }
        public decimal? SphOs { get; set; }
        public decimal? CylOs { get; set; }
        public int? AxisOs { get; set; }
        public decimal? AddOs { get; set; }
        public decimal? Pd { get; set; }
        public string? LensType { get; set; }
        public string? Notes { get; set; }
    }

    #endregion
}
