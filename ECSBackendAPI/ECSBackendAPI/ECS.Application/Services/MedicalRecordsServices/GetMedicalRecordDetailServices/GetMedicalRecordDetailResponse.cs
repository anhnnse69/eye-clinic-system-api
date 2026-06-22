namespace ECS.Application.Services.MedicalRecordsServices.GetMedicalRecordDetailServices
{
    /// <summary>
    /// Response object for medical record detail - includes all related data.
    /// UC39 - View Medical Record Detail
    /// Designed to support both viewing and editing by doctors.
    /// </summary>
    public class GetMedicalRecordDetailResponse
    {
        public Guid Id { get; set; }
        public Guid AppointmentId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public string RecordType { get; set; } = string.Empty;
        public string? ChiefComplaint { get; set; }
        public int? IllnessDayNumber { get; set; }
        public string? MedicalHistory { get; set; }
        public string? PersonalHistoryEye { get; set; }
        public string? PersonalHistorySystemic { get; set; }
        public string? FamilyHistory { get; set; }
        public int? VitalPulse { get; set; }
        public decimal? VitalTemperature { get; set; }
        public string? VitalBloodPressure { get; set; }
        public int? VitalRespiratoryRate { get; set; }
        public decimal? VitalWeightKg { get; set; }
        public string? SystemicExam { get; set; }
        public string? DiagnosisMain { get; set; }
        public string? DiagnosisComorbid { get; set; }
        public string? DiagnosisDifferential { get; set; }
        public string? Prognosis { get; set; }
        public string? TreatmentPlan { get; set; }
        public string? Notes { get; set; }
        public bool IsLocked { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string PatientFullName { get; set; } = string.Empty;
        public string? PatientDob { get; set; }
        public string? PatientPhone { get; set; }
        public string? PatientEmail { get; set; }
        public string? PatientGender { get; set; }
        public string? PatientAddress { get; set; }
        public string? PatientIdentityNumber { get; set; }

        public string DoctorFullName { get; set; } = string.Empty;
        public string? DoctorTitle { get; set; }
        public string? DoctorSpecialty { get; set; }

        public DateTime AppointmentDate { get; set; }
        public string? AppointmentStatus { get; set; }
        public string? AppointmentNotes { get; set; }

        public EyeExamBasicDetail? RightEyeExamBasic { get; set; }
        public EyeExamBasicDetail? LeftEyeExamBasic { get; set; }

        public EyeEyelidConjunctivaDetail? RightEyeEyelidConjunctiva { get; set; }
        public EyeEyelidConjunctivaDetail? LeftEyeEyelidConjunctiva { get; set; }

        public EyeCorneaDetail? RightEyeCornea { get; set; }
        public EyeCorneaDetail? LeftEyeCornea { get; set; }

        public EyeAnteriorChamberDetail? RightEyeAcIris { get; set; }
        public EyeAnteriorChamberDetail? LeftEyeAcIris { get; set; }

        public EyeLensVitreousDetail? RightEyeLensVitreous { get; set; }
        public EyeLensVitreousDetail? LeftEyeLensVitreous { get; set; }

        public EyeScleraDetail? RightEyeSclera { get; set; }
        public EyeScleraDetail? LeftEyeSclera { get; set; }

        public EyeFundusDiscMaculaDetail? RightEyeFundusDiscMacula { get; set; }
        public EyeFundusDiscMaculaDetail? LeftEyeFundusDiscMacula { get; set; }

        public EyeFundusRetinaVesselDetail? RightEyeFundusRetinaVessel { get; set; }
        public EyeFundusRetinaVesselDetail? LeftEyeFundusRetinaVessel { get; set; }

        public List<LacrimalRecordDetail> LacrimalRecords { get; set; } = new();

        public List<OctResultDetail> OctResults { get; set; } = new();
        public List<VisualFieldTestDetail> VisualFieldTests { get; set; } = new();
        public List<UltrasoundEyeDetail> UltrasoundEyes { get; set; } = new();

        public TraumaRecordDetail? TraumaRecord { get; set; }
        public GlaucomaRecordDetail? GlaucomaRecord { get; set; }
        public StrabismusPtosisRecordDetail? StrabismusPtosisRecord { get; set; }
        public PediatricRecordDetail? PediatricRecord { get; set; }

        public List<PrescriptionDetail> Prescriptions { get; set; } = new();
        public List<GlassesPrescriptionDetail> GlassesPrescriptions { get; set; } = new();

        public MedicalRecordExtrasDetail? Extras { get; set; }
        public List<DocumentAccessPermissionDetail> DocumentAccessPermissions { get; set; } = new();

        public bool CanEdit { get; set; }
        public bool CanViewOnly { get; set; }
        public string? EditRestrictionReason { get; set; }
    }

    public class EyeExamBasicDetail
    {
        public Guid Id { get; set; }
        public string Side { get; set; } = string.Empty;
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
        public string? EomStatus { get; set; }
        public string? EomNote { get; set; }
        public string? Nystagmus { get; set; }
        public string? NystagmusType { get; set; }
        public string? VisualField { get; set; }
        public string? EyeballStatus { get; set; }
        public string? EyeballTexture { get; set; }
        public string? StrabismusType { get; set; }
        public string? CoverTestResult { get; set; }
        public string? HirschbergTest { get; set; }
        public string? PrismMeasurement { get; set; }
        public string? PupilExamResult { get; set; }
        public string? PupilReflexLight { get; set; }
        public string? PupilAccommodation { get; set; }
        public string? PupilRelativeAfferentDefect { get; set; }
        public string? Notes { get; set; }
    }

    public class EyeEyelidConjunctivaDetail
    {
        public Guid Id { get; set; }
        public string Side { get; set; } = string.Empty;
        public string? Status { get; set; }
        public bool Ptosis { get; set; }
        public string? PtosisDegree { get; set; }
        public bool Laceration { get; set; }
        public string? LacerationExtent { get; set; }
        public string? LacerationLocation { get; set; }
        public bool LacerationSutured { get; set; }
        public bool LacerationUnsutured { get; set; }
        public string? LacrimalDuctStatus { get; set; }
        public string? LacrimalDuctLocation { get; set; }
        public bool Scar { get; set; }
        public string? OtherFindings { get; set; }
        public bool Entropion { get; set; }
        public bool Epicanthus { get; set; }
        public bool EntropionPediatric { get; set; }
        public string? FornixStatus { get; set; }
        public string? SymblepharonHeight { get; set; }
        public string? SymblepharonWidth { get; set; }
        public string? ChalazionHordeolum { get; set; }

        public string? ConjunctivaStatus { get; set; }
        public string? ConjunctivaCongestionType { get; set; }
        public bool ConjunctivaEdema { get; set; }
        public bool ConjunctivaHemorrhage { get; set; }
        public string? ConjunctivaHemorrhageLocation { get; set; }
        public bool ConjunctivaLaceration { get; set; }
        public string? ConjunctivaLacerationLocation { get; set; }
        public bool ConjunctivaIschemia { get; set; }
        public bool ConjunctivaPapilla { get; set; }
        public bool ConjunctivaFollicle { get; set; }
        public bool ConjunctivaKeratinization { get; set; }
        public bool ConjunctivaScar { get; set; }
        public bool FluoresceinStain { get; set; }
        public bool Pterygium { get; set; }
        public string? PterygiumLocation { get; set; }
        public string? PterygiumSize { get; set; }
        public bool HasTumor { get; set; }
        public string? TumorNature { get; set; }
        public string? TumorLocation { get; set; }
        public string? TumorSize { get; set; }
        public bool Lagophthalmos { get; set; }
        public string? OtherFindingsConjunctiva { get; set; }
    }

    public class EyeCorneaDetail
    {
        public Guid Id { get; set; }
        public string Side { get; set; } = string.Empty;
        public string? Clarity { get; set; }
        public string? Size { get; set; }
        public string? Shape { get; set; }
        public decimal? DiameterMm { get; set; }
        public string? Sensation { get; set; }
        public string? EpitheliumStatus { get; set; }
        public bool EpitheliumPunctate { get; set; }
        public string? EpitheliumEdemaLevel { get; set; }
        public string? EpitheliumLoss { get; set; }
        public string? StromaEdemaLevel { get; set; }
        public string? StromaInfiltrate { get; set; }
        public string? StromaThinning { get; set; }
        public bool Ulcer { get; set; }
        public string? UlcerLocation { get; set; }
        public string? UlcerSize { get; set; }
        public string? UlcerDescription { get; set; }
        public bool Abscess { get; set; }
        public bool Descemetocele { get; set; }
        public bool BloodStaining { get; set; }
        public bool Laceration { get; set; }
        public string? LacerationSize { get; set; }
        public string? LacerationLocation { get; set; }
        public string? LacerationType { get; set; }
        public bool? LacerationSutured { get; set; }
        public bool? AnatomicalReduction { get; set; }
        public bool Perforation { get; set; }
        public decimal? PerforationDiameterMm { get; set; }
        public string? PerforationLocation { get; set; }
        public string? SeidelTest { get; set; }
        public bool Neovascularization { get; set; }
        public string? NeovascularizationDepth { get; set; }
        public string? NeovascularizationExtent { get; set; }
        public string? LimbalStatus { get; set; }
        public decimal? CornealThickness { get; set; }
        public string? DrugDeposit { get; set; }
        public string? OtherFindings { get; set; }
        public bool ForeignBody { get; set; }
    }

    public class EyeAnteriorChamberDetail
    {
        public Guid Id { get; set; }
        public string Side { get; set; } = string.Empty;
        public string? Depth { get; set; }
        public decimal? DepthMm { get; set; }
        public string? HerickClassification { get; set; }
        public bool VitreousInAC { get; set; }
        public bool Pus { get; set; }
        public decimal? PusMm { get; set; }
        public string? Tyndall { get; set; }
        public bool Exudate { get; set; }
        public string? ExudateDescription { get; set; }
        public bool Hemorrhage { get; set; }
        public string? HemorrhageLevel { get; set; }
        public bool ForeignBody { get; set; }
        public string? OtherFindings { get; set; }
        public string? IrisColor { get; set; }
        public string? IrisCondition { get; set; }
        public bool IrisDegeneration { get; set; }
        public bool IrisNeovascularization { get; set; }
        public bool IrisCiliaryProcesses { get; set; }
        public bool KoeppeNodules { get; set; }
        public bool BusaccaNodules { get; set; }
        public bool IrisRootTear { get; set; }
        public string? IrisRootTearDegree { get; set; }
        public bool IrisLoss { get; set; }
        public bool IrisPerforation { get; set; }
        public string? PupilShape { get; set; }
        public string? PupilPosition { get; set; }
        public string? PupilReflex { get; set; }
        public bool PupilDilated { get; set; }
        public bool PtdtTest { get; set; }
        public string? FundusReflex { get; set; }
        public string? AngleFindings { get; set; }
        public bool AngleSynechiae { get; set; }
        public bool AnglePigment { get; set; }
        public bool AngleNeovascularization { get; set; }
    }

    public class EyeLensVitreousDetail
    {
        public Guid Id { get; set; }
        public string Side { get; set; } = string.Empty;
        public string? LensStatus { get; set; }
        public string? OpacityType { get; set; }
        public string? OpacityLocation { get; set; }
        public bool Subluxation { get; set; }
        public bool LensInAnterior { get; set; }
        public bool LensInVitreous { get; set; }
        public bool Purulent { get; set; }
        public bool AnteriorPigmentation { get; set; }
        public bool IolPresent { get; set; }
        public string? IolStatus { get; set; }
        public string? IolPosition { get; set; }
        public string? Status { get; set; }
        public string? OpacityLevel { get; set; }
        public string? Tyndall { get; set; }
        public bool Hemorrhage { get; set; }
        public bool Organized { get; set; }
        public bool Pvd { get; set; }
        public bool VitreousPurulent { get; set; }
        public bool ForeignBody { get; set; }
        public string? OtherFindings { get; set; }
    }

    public class EyeScleraDetail
    {
        public Guid Id { get; set; }
        public string Side { get; set; } = string.Empty;
        public string? Status { get; set; }
        public bool Laceration { get; set; }
        public string? LacerationSize { get; set; }
        public string? LacerationLocation { get; set; }
        public bool? LacerationSutured { get; set; }
        public bool LacerationUnsutured { get; set; }
        public bool TissueEntrapped { get; set; }
        public string? OtherFindings { get; set; }
    }

    public class EyeFundusDiscMaculaDetail
    {
        public Guid Id { get; set; }
        public string Side { get; set; } = string.Empty;
        public string? DiscStatus { get; set; }
        public string? DiscColor { get; set; }
        public string? CdRatio { get; set; }
        public string? RimStatus { get; set; }
        public string? RimLocation { get; set; }
        public string? VesselChange { get; set; }
        public bool DiscHemorrhage { get; set; }
        public bool Neovascularization { get; set; }
        public bool DiscNotVisible { get; set; }
        public string? MaculaStatus { get; set; }
        public bool MaculaReflexAbsent { get; set; }
        public string? MaculaEdemaType { get; set; }
        public string? MaculaHoleDegree { get; set; }
        public bool MaculaScar { get; set; }
        public bool SerousDetachment { get; set; }
        public bool MaculaHemorrhage { get; set; }
        public string? ChoroidStatus { get; set; }
        public string? ChoroidalFindings { get; set; }
        public bool CNV { get; set; }
        public bool ChorioretinitisActive { get; set; }
        public bool ChorioretinitisScar { get; set; }
        public int? ChorioretinitisCount { get; set; }
        public string? ChorioretinitisLocation { get; set; }
    }

    public class EyeFundusRetinaVesselDetail
    {
        public Guid Id { get; set; }
        public string Side { get; set; } = string.Empty;
        public string? VesselStatus { get; set; }
        public string? ArteryOcclusion { get; set; }
        public string? VeinOcclusion { get; set; }
        public string? OcclusionType { get; set; }
        public bool OcclusionEdema { get; set; }
        public bool OcclusionIschemia { get; set; }
        public bool Vasculitis { get; set; }
        public bool RetinalNeovascularization { get; set; }
        public string? RetinaStatus { get; set; }
        public string? RetinalCondition { get; set; }
        public bool RetinalEdema { get; set; }
        public string? EdemaType { get; set; }
        public bool Hemorrhage { get; set; }
        public string? HemorrhageType { get; set; }
        public bool Degeneration { get; set; }
        public string? DegenerationType { get; set; }
        public string? DegenerationDescription { get; set; }
        public bool Detachment { get; set; }
        public string? DetachmentLevel { get; set; }
        public bool RetinalTear { get; set; }
        public int? TearCount { get; set; }
        public string? TearLocation { get; set; }
        public string? TearMorphology { get; set; }
        public bool BmscDetachment { get; set; }
        public bool Iofb { get; set; }
        public string? IofbLocation { get; set; }
        public string? IofbSize { get; set; }
        public string? CombinedFindings { get; set; }
        public string? OtherFindings { get; set; }
    }

    public class LacrimalRecordDetail
    {
        public Guid Id { get; set; }
        public string Side { get; set; } = string.Empty;
        public string? LacrimalDischarge { get; set; }
        public string? NasolacrimalStatus { get; set; }
        public bool IrrigationFree { get; set; }
        public bool IrrigationRegurgitationSame { get; set; }
        public bool IrrigationRegurgitationOpposite { get; set; }
        public string? IrrigationNote { get; set; }
        public string? LacrimalOther { get; set; }
    }

    public class OctResultDetail
    {
        public Guid Id { get; set; }
        public string? MachineName { get; set; }
        public string? ScanPattern { get; set; }
        public decimal? RnflAverageOd { get; set; }
        public decimal? RnflAverageOs { get; set; }
        public decimal? CmtOd { get; set; }
        public decimal? CmtOs { get; set; }
        public decimal? CupDiscRatioOd { get; set; }
        public decimal? CupDiscRatioOs { get; set; }
        public string? Conclusion { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime ExamDate { get; set; }
        public string? TechnicianName { get; set; }
    }

    public class VisualFieldTestDetail
    {
        public Guid Id { get; set; }
        public string Side { get; set; } = string.Empty;
        public string? Machine { get; set; }
        public string? Strategy { get; set; }
        public decimal? MdValue { get; set; }
        public decimal? PsdValue { get; set; }
        public decimal? VfiPercent { get; set; }
        public bool Reliable { get; set; }
        public string? ResultSummary { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime TestDate { get; set; }
        public string? TechnicianName { get; set; }
    }

    public class UltrasoundEyeDetail
    {
        public Guid Id { get; set; }
        public string Side { get; set; } = string.Empty;
        public string? UltrasoundType { get; set; }
        public decimal? AxialLengthMm { get; set; }
        public decimal? AcDepthMm { get; set; }
        public decimal? LensThicknessMm { get; set; }
        public decimal? VitreousLengthMm { get; set; }
        public string? LensStatus { get; set; }
        public string? RetinaStatus { get; set; }
        public string? Conclusion { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime ExamDate { get; set; }
        public string? TechnicianName { get; set; }
    }

    public class TraumaRecordDetail
    {
        public Guid Id { get; set; }
        public string? InjuryCause { get; set; }
        public DateTime? InjuryTime { get; set; }
        public string? PriorTreatment { get; set; }
        public string? PostTreatmentCourse { get; set; }
        public string? OdInjuries { get; set; }
        public string? OsInjuries { get; set; }
        public string? InjuryDetails { get; set; }
        public string? TraumaConclusion { get; set; }
        public string? DiagnosisClinical { get; set; }
        public string? DiagnosisCause { get; set; }
        public string? TreatmentProcess { get; set; }
        public string? TreatmentPlan { get; set; }
        public List<TraumaSurgeryDetail> Surgeries { get; set; } = new();
    }

    public class TraumaSurgeryDetail
    {
        public Guid Id { get; set; }
        public DateTime? SurgeryDate { get; set; }
        public string? SurgeryType { get; set; }
        public string? SurgeryDescription { get; set; }
        public string? SurgeonName { get; set; }
        public string? AnesthesiaType { get; set; }
        public string? PostSurgeryCondition { get; set; }
        public string? Notes { get; set; }
    }

    public class GlaucomaRecordDetail
    {
        public Guid Id { get; set; }
        public string? EyePainLevel { get; set; }
        public string? VisionSymptoms { get; set; }
        public string? VisionProgression { get; set; }
        public bool HasPhotophobia { get; set; }
        public bool HasTearing { get; set; }
        public bool HasRedness { get; set; }
        public string? SystemicSymptoms { get; set; }
        public string? VaWithoutCorrectionOd { get; set; }
        public string? VaWithoutCorrectionOs { get; set; }
        public string? VaWithCorrectionOd { get; set; }
        public string? VaWithCorrectionOs { get; set; }
        public string? IopOd { get; set; }
        public string? IopOs { get; set; }
        public string? IopMethod { get; set; }
        public string? IopTargetOd { get; set; }
        public string? IopTargetOs { get; set; }
        public string? HistoryEye { get; set; }
        public string? HistoryEyeSurgery { get; set; }
        public string? PriorEyeSurgeryDetails { get; set; }
        public string? SteroidUse { get; set; }
        public string? SteroidPrescribed { get; set; }
        public string? MedicationDuration { get; set; }
        public string? MedicationRoute { get; set; }
        public bool HasCardiovascularDisease { get; set; }
        public bool HasHypertension { get; set; }
        public bool HasDiabetes { get; set; }
        public bool HasCarotidFistula { get; set; }
        public string? OtherSystemicDisease { get; set; }
        public bool FamilyHasGlaucoma { get; set; }
        public string? FamilyGlaucomaRelation { get; set; }
        public string? GlaucomaMedications { get; set; }
        public string? MedicationChangeReason { get; set; }
        public string? OtherMedications { get; set; }
        public string? TreatmentProgress { get; set; }
        public string? GlaucomaType { get; set; }
        public string? StageOd { get; set; }
        public string? StageOs { get; set; }
        public bool HasEyelidSwelling { get; set; }
        public bool HasConjunctivalInjection { get; set; }
        public bool HasFilteringBleb { get; set; }
        public string? BlebLocation { get; set; }
        public string? BlebStatus { get; set; }
        public string? ConjunctivalScarLocation { get; set; }
        public string? CornealTransparency { get; set; }
        public string? CornealEdemaLevel { get; set; }
        public string? CornealThickness { get; set; }
        public bool HasScleralThinning { get; set; }
        public string? ScleralScarLocation { get; set; }
        public string? AcDepthSmith { get; set; }
        public string? AcDepthHerick { get; set; }
        public string? GonioscopyOd { get; set; }
        public string? GonioscopyOs { get; set; }
        public string? AngleFindings { get; set; }
        public string? IrisColor { get; set; }
        public string? IrisCondition { get; set; }
        public bool HasIrisNeovascularization { get; set; }
        public string? PupilDiameter { get; set; }
        public string? PupilPigmentBorder { get; set; }
        public string? PupilReflexResponse { get; set; }
        public string? LensStatus { get; set; }
        public string? FundusRetinaFindings { get; set; }
        public string? FundusMaculaFindings { get; set; }
        public bool HasCNV { get; set; }
        public bool HasRetinalHemorrhage { get; set; }
        public string? OpticDiscDescription { get; set; }
        public string? NerveRimOd { get; set; }
        public string? NerveRimOs { get; set; }
        public string? OpticDiscCupRatio { get; set; }
        public string? OpticDiscVesselChange { get; set; }
        public bool HasOpticDiscHemorrhage { get; set; }
        public bool HasRimAtrophy { get; set; }
        public string? EyeAxialLength { get; set; }
        public string? TreatmentPlanSurgery { get; set; }
        public string? TreatmentPlanLaser { get; set; }
        public string? TreatmentPlanMedication { get; set; }
        public string? FollowUpPlan { get; set; }
        public List<GlaucomaHistoryDetail> Histories { get; set; } = new();
    }

    public class GlaucomaHistoryDetail
    {
        public Guid Id { get; set; }
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

    public class StrabismusPtosisRecordDetail
    {
        public Guid Id { get; set; }
        public bool ChiefStrabismus { get; set; }
        public bool ChiefPtosis { get; set; }
        public bool Congenital { get; set; }
        public bool Acquired { get; set; }
        public string? AcquiredOnset { get; set; }
        public string? StrabismusType { get; set; }
        public bool Nystagmus { get; set; }
        public string? NystagmusType { get; set; }
        public string? PriorAmblyopiaTreatment { get; set; }
        public string? PriorAmblyopiaResult { get; set; }
        public string? PriorSurgery { get; set; }
        public string? PriorSurgeryResult { get; set; }
        public string? VaBeforeAtropineOd { get; set; }
        public string? VaBeforeAtropineOs { get; set; }
        public string? VaAfterAtropineOd { get; set; }
        public string? VaAfterAtropineOs { get; set; }
        public string? RefractionPreAtropine { get; set; }
        public string? RefractionPostAtropine { get; set; }
        public string? PupilShadowTestOd { get; set; }
        public string? PupilShadowTestOs { get; set; }
        public string? EomGazeTest { get; set; }
        public string? EomGazeIncreaseOd { get; set; }
        public string? EomGazeIncreaseOs { get; set; }
        public string? EomGazeLimitOd { get; set; }
        public string? EomGazeLimitOs { get; set; }
        public string? EomInternalOd { get; set; }
        public string? EomInternalOs { get; set; }
        public string? ConvergencePoint { get; set; }
        public string? CoverTestResult { get; set; }
        public string? HirschbergBeforeAtropine { get; set; }
        public string? HirschbergAfterAtropine { get; set; }
        public string? PrismNear { get; set; }
        public string? PrismDistance { get; set; }
        public string? PrismUp { get; set; }
        public string? PrismDown { get; set; }
        public string? StrabismusSyndrome { get; set; }
        public string? SynoptophoreObjective { get; set; }
        public string? SynoptophoreSubjective { get; set; }
        public string? BinocularStatus { get; set; }
        public string? FusionAmplitude { get; set; }
        public string? RetinalCorrespondence { get; set; }
        public string? Diplopia { get; set; }
        public string? CompensatoryHeadPosture { get; set; }
        public string? PtosisDegreeOd { get; set; }
        public string? PtosisDegreeOs { get; set; }
        public string? LevatorFunctionOd { get; set; }
        public string? LevatorFunctionOs { get; set; }
        public string? MarcusGunn { get; set; }
        public string? BellPhenomenon { get; set; }
        public string? FixationOd { get; set; }
        public string? FixationOs { get; set; }
        public string? PalpebralReflexOd { get; set; }
        public string? PalpebralReflexOs { get; set; }
    }

    public class PediatricRecordDetail
    {
        public Guid Id { get; set; }
        public bool Congenital { get; set; }
        public bool Acquired { get; set; }
        public string? AcquiredOnset { get; set; }
        public string? PriorTreatment { get; set; }
        public bool PregnancyIllness { get; set; }
        public string? PregnancyIllnessDetail { get; set; }
        public bool IntellectualDevelopmentNormal { get; set; }
        public string? ChiefSymptoms { get; set; }
        public bool EntropionOd { get; set; }
        public bool EpicanthusOd { get; set; }
        public bool PtosisOd { get; set; }
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
        public string? IntellectualDevelopmentStatus { get; set; }
        public string? GeneralHealthStatus { get; set; }
    }

    public class PrescriptionDetail
    {
        public Guid Id { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public List<PrescriptionItemDetail> Items { get; set; } = new();
    }

    public class PrescriptionItemDetail
    {
        public Guid Id { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string? Frequency { get; set; }
        public int? DurationDays { get; set; }
        public int Quantity { get; set; }
        public string? Instruction { get; set; }
    }

    public class GlassesPrescriptionDetail
    {
        public Guid Id { get; set; }
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
        public DateTime CreatedAt { get; set; }
    }

    public class MedicalRecordExtrasDetail
    {
        public Guid Id { get; set; }
        public string? TraumaSummary { get; set; }
        public string? GlaucomaSummary { get; set; }
        public string? PediatricSummary { get; set; }
        public string? LabOrders { get; set; }
        public string? ImagingOrders { get; set; }
        public string? DischargeSummary { get; set; }
        public string? TreatmentProcess { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class DocumentAccessPermissionDetail
    {
        public Guid Id { get; set; }
        public string GrantedToUserName { get; set; } = string.Empty;
        public string GrantedByUserName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
