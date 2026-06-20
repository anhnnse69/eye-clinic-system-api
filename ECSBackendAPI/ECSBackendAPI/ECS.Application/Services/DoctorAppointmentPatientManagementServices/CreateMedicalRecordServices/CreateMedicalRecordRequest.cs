using ECS.Domain.Enums;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.CreateMedicalRecordServices
{
    /// <summary>
    /// Request object for creating a medical record.
    /// UC40 - Create Medical Record
    /// Supports 6 standard medical record templates:
    /// - MS21: Chấn thương (Trauma)
    /// - MS22: Bán phần trước (Anterior Segment)
    /// - MS23: Đáy mắt (Fundus)
    /// - MS24: Glôcôm (Glaucoma)
    /// - MS25: Lác, sụp mi (Strabismus/Ptosis)
    /// - MS26: Mắt trẻ em (Pediatric)
    /// </summary>
    public class CreateMedicalRecordRequest
    {
        /// <summary>
        /// The appointment ID to create medical record for.
        /// </summary>
        public string AppointmentId { get; set; } = string.Empty;

        /// <summary>
        /// Record type (MS21_TRAUMA, MS22_ANTERIOR, MS23_FUNDUS, MS24_GLAUCOMA, MS25_STRABISMUS_PTOSIS, MS26_PEDIATRIC).
        /// </summary>
        public string RecordType { get; set; } = string.Empty;

        // ==================== I. HÀNH CHÍNH (Administrative) ====================
        /// <summary>
        /// Factor code (MaYeuTo) for administrative classification.
        /// </summary>
        public string? MaYeuTo { get; set; }
        /// <summary>
        /// Patient's age at time of admission.
        /// </summary>
        public int? Age { get; set; }

        // ==================== II. QUẢN LÝ NGƯỜI BỆNH (Patient Management) ====================
        /// <summary>
        /// Date when the patient was admitted to the hospital.
        /// </summary>
        public DateTime? AdmissionDate { get; set; }
        /// <summary>
        /// Type of admission: Emergency (Cấp cứu), Outpatient (KKB), or Inpatient (Khoa điều trị).
        /// </summary>
        public string? AdmissionType { get; set; }
        /// <summary>
        /// Source of referral: Healthcare facility, Self-referral, or Other.
        /// </summary>
        public string? ReferralSource { get; set; }
        /// <summary>
        /// Admission number indicating which time this specific condition has required hospitalization.
        /// </summary>
        public int? AdmissionNumber { get; set; }
        /// <summary>
        /// Date when the patient was admitted to a specific department.
        /// </summary>
        public DateTime? DepartmentAdmissionDate { get; set; }
        /// <summary>
        /// Name of the department where the patient is admitted.
        /// </summary>
        public string? DepartmentName { get; set; }
        /// <summary>
        /// Bed number assigned to the patient.
        /// </summary>
        public string? BedNumber { get; set; }
        /// <summary>
        /// Date when the patient was transferred to another department/facility.
        /// </summary>
        public DateTime? TransferDate { get; set; }
        /// <summary>
        /// Destination department for patient transfer.
        /// </summary>
        public string? TransferToDepartment { get; set; }
        /// <summary>
        /// Reason for the transfer.
        /// </summary>
        public string? TransferReason { get; set; }
        /// <summary>
        /// Date when the patient was discharged.
        /// </summary>
        public DateTime? DischargeDate { get; set; }
        /// <summary>
        /// Type of discharge: Discharge (Ra viện), Request to leave (Xin về), Left without permission (Bỏ về), or Transferred (Đưa về).
        /// </summary>
        public string? DischargeType { get; set; }
        /// <summary>
        /// Facility to which the patient is transferred: Higher level, Lower level, or Subspecialty.
        /// </summary>
        public string? TransferToFacility { get; set; }
        /// <summary>
        /// Total number of days the patient received treatment.
        /// </summary>
        public int? TotalTreatmentDays { get; set; }

        // ==================== III. CHẨN ĐOÁN MÃ MÃ (Diagnosis Codes) ====================
        /// <summary>
        /// Diagnosis at referral facility.
        /// </summary>
        public string? DiagnosisAtReferral { get; set; }
        /// <summary>
        /// Diagnosis at emergency room or outpatient clinic.
        /// </summary>
        public string? DiagnosisAtER { get; set; }
        /// <summary>
        /// Diagnosis at admission to treatment department.
        /// </summary>
        public string? DiagnosisAtAdmission { get; set; }
        /// <summary>
        /// Diagnosis of complications (Tai biến, Biến chứng).
        /// </summary>
        public string? DiagnosisComplication { get; set; }
        /// <summary>
        /// Type of complication: Surgical, Anesthetic-induced, Infectious, or Other.
        /// </summary>
        public string? DiagnosisComplicationType { get; set; }
        /// <summary>
        /// Number of days of treatment after surgery.
        /// </summary>
        public int? PostSurgeryTreatmentDays { get; set; }
        /// <summary>
        /// Total number of surgeries performed.
        /// </summary>
        public int? TotalSurgeryCount { get; set; }
        /// <summary>
        /// Diagnosis at discharge - main condition.
        /// </summary>
        public string? DiagnosisAtDischarge { get; set; }
        /// <summary>
        /// Cause of the condition.
        /// </summary>
        public string? DiagnosisCause { get; set; }
        /// <summary>
        /// Comorbid conditions.
        /// </summary>
        public string? DiagnosisComorbidities { get; set; }
        /// <summary>
        /// Pre-surgery diagnosis.
        /// </summary>
        public string? DiagnosisPreSurgery { get; set; }
        /// <summary>
        /// Post-surgery diagnosis.
        /// </summary>
        public string? DiagnosisPostSurgery { get; set; }

        // ==================== IV. TÌNH TRẠNG RA VIỆN (Discharge Status) ====================
        /// <summary>
        /// Treatment outcome: Recovered (Khỏi), Improved (Đỡ giảm), Unchanged (Không thay đổi), Worse (Nặng hơn), Deceased (Tử vong).
        /// </summary>
        public string? TreatmentResult { get; set; }
        /// <summary>
        /// Pathology result: Benign (Lành tính), Suspicious (Nghi ngờ), Malignant (Ác tính).
        /// </summary>
        public string? PathologyResult { get; set; }
        /// <summary>
        /// Date and time of death.
        /// </summary>
        public DateTime? DeathTime { get; set; }
        /// <summary>
        /// Time of death within hours after admission: 24h, 48h, or 72h.
        /// </summary>
        public string? DeathWithinHours { get; set; }
        /// <summary>
        /// Primary cause of death.
        /// </summary>
        public string? DeathCause { get; set; }
        /// <summary>
        /// Type of death cause: Due to illness, Due to treatment complications, or Other.
        /// </summary>
        public string? DeathCauseType { get; set; }
        /// <summary>
        /// Whether an autopsy was performed.
        /// </summary>
        public bool? AutopsyPerformed { get; set; }
        /// <summary>
        /// Diagnosis from autopsy findings.
        /// </summary>
        public string? AutopsyDiagnosis { get; set; }

        // ==================== A. BỆNH ÁN - I. LÝ DO VÀO VIỆN ====================
        /// <summary>
        /// Chief complaint - reason for hospital visit.
        /// </summary>
        public string? ChiefComplaint { get; set; }
        /// <summary>
        /// Number of days the patient has been ill.
        /// </summary>
        public int? IllnessDayNumber { get; set; }

        // ==================== A. BỆNH ÁN - II. HỎI BỆNH (History) ====================
        /// <summary>
        /// Medical history - disease progression.
        /// </summary>
        public string? MedicalHistory { get; set; }
        /// <summary>
        /// Personal eye disease history.
        /// </summary>
        public string? PersonalHistoryEye { get; set; }
        /// <summary>
        /// Personal systemic medical history.
        /// </summary>
        public string? PersonalHistorySystemic { get; set; }
        /// <summary>
        /// Family medical history.
        /// </summary>
        public string? FamilyHistory { get; set; }

        // MS21 Specific - Trauma History
        /// <summary>
        /// Cause of trauma.
        /// </summary>
        public string? TraumaCause { get; set; }
        /// <summary>
        /// Time when trauma occurred.
        /// </summary>
        public DateTime? TraumaTime { get; set; }
        /// <summary>
        /// Prior treatment methods.
        /// </summary>
        public string? TraumaPriorTreatment { get; set; }
        /// <summary>
        /// Clinical course after treatment.
        /// </summary>
        public string? TraumaPostTreatmentCourse { get; set; }

        // MS24 Specific - Glaucoma History
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
        /// History of corticosteroid use.
        /// </summary>
        public string? GlaucomaSteroidUse { get; set; }
        /// <summary>
        /// Family history of glaucoma.
        /// </summary>
        public string? GlaucomaFamilyHistory { get; set; }

        // MS25 Specific - Strabismus History
        /// <summary>
        /// Whether strabismus is congenital.
        /// </summary>
        public bool? StrabismusCongenital { get; set; }
        /// <summary>
        /// Whether strabismus is acquired.
        /// </summary>
        public bool? StrabismusAcquired { get; set; }
        /// <summary>
        /// Time of strabismus onset.
        /// </summary>
        public string? StrabismusOnsetTime { get; set; }
        /// <summary>
        /// Main symptom: Esotropia (Lác trong), Exotropia (Lác ngoài), Hypertropia (Lác chéo), Ptosis (Sụp mi), or Nystagmus (Rung giật nhãn cầu).
        /// </summary>
        public string? StrabismusMainSymptom { get; set; }

        // MS26 Specific - Pediatric History
        /// <summary>
        /// Pathological pregnancy history.
        /// </summary>
        public string? PediatricPregnancyHistory { get; set; }
        /// <summary>
        /// Intellectual development status.
        /// </summary>
        public string? PediatricDevelopment { get; set; }

        // ==================== III. KHÁM BỆNH (Examination) ====================

        // 1. Khám chuyên khoa - Thị lực & Nhãn áp vào viện
        /// <summary>
        /// Right eye basic examination data (visual acuity, intraocular pressure).
        /// </summary>
        public EyeBasicExamData? RightEyeBasic { get; set; }
        /// <summary>
        /// Left eye basic examination data (visual acuity, intraocular pressure).
        /// </summary>
        public EyeBasicExamData? LeftEyeBasic { get; set; }

        // 2. Mi mắt (Eyelid)
        /// <summary>
        /// Right eye eyelid examination data.
        /// </summary>
        public EyeEyelidData? RightEyeEyelid { get; set; }
        /// <summary>
        /// Left eye eyelid examination data.
        /// </summary>
        public EyeEyelidData? LeftEyeEyelid { get; set; }

        // 3. Kết mạc (Conjunctiva)
        /// <summary>
        /// Right eye conjunctiva examination data.
        /// </summary>
        public EyeConjunctivaData? RightEyeConjunctiva { get; set; }
        /// <summary>
        /// Left eye conjunctiva examination data.
        /// </summary>
        public EyeConjunctivaData? LeftEyeConjunctiva { get; set; }

        // 4. Giác mạc (Cornea)
        /// <summary>
        /// Right eye cornea examination data.
        /// </summary>
        public EyeCorneaExamData? RightEyeCornea { get; set; }
        /// <summary>
        /// Left eye cornea examination data.
        /// </summary>
        public EyeCorneaExamData? LeftEyeCornea { get; set; }

        // 5. Củng mạc (Sclera)
        /// <summary>
        /// Right eye sclera examination data.
        /// </summary>
        public EyeScleraExamData? RightEyeSclera { get; set; }
        /// <summary>
        /// Left eye sclera examination data.
        /// </summary>
        public EyeScleraExamData? LeftEyeSclera { get; set; }

        // 6. Tiền phòng (Anterior Chamber)
        /// <summary>
        /// Right eye anterior chamber examination data.
        /// </summary>
        public EyeAnteriorChamberData? RightEyeAnteriorChamber { get; set; }
        /// <summary>
        /// Left eye anterior chamber examination data.
        /// </summary>
        public EyeAnteriorChamberData? LeftEyeAnteriorChamber { get; set; }

        // 7. Mống mắt & Đồng tử (Iris & Pupil)
        /// <summary>
        /// Right eye iris and pupil examination data.
        /// </summary>
        public EyeIrisPupilData? RightEyeIrisPupil { get; set; }
        /// <summary>
        /// Left eye iris and pupil examination data.
        /// </summary>
        public EyeIrisPupilData? LeftEyeIrisPupil { get; set; }

        // 8. Thể thủy tinh (Lens)
        /// <summary>
        /// Right eye lens examination data.
        /// </summary>
        public EyeLensData? RightEyeLens { get; set; }
        /// <summary>
        /// Left eye lens examination data.
        /// </summary>
        public EyeLensData? LeftEyeLens { get; set; }

        // 9. Dịch kính (Vitreous)
        /// <summary>
        /// Right eye vitreous examination data.
        /// </summary>
        public EyeVitreousData? RightEyeVitreous { get; set; }
        /// <summary>
        /// Left eye vitreous examination data.
        /// </summary>
        public EyeVitreousData? LeftEyeVitreous { get; set; }

        // 10. Đáy mắt - Đĩa thị & Hoàng điểm (Optic Disc & Macula)
        /// <summary>
        /// Right eye fundus optic disc and macula examination data.
        /// </summary>
        public EyeFundusDiscMaculaData? RightEyeFundusDiscMacula { get; set; }
        /// <summary>
        /// Left eye fundus optic disc and macula examination data.
        /// </summary>
        public EyeFundusDiscMaculaData? LeftEyeFundusDiscMacula { get; set; }

        // 11. Đáy mắt - Võng mạc & Mạch máu (Retina & Vessels)
        /// <summary>
        /// Right eye fundus retina and vessels examination data.
        /// </summary>
        public EyeFundusRetinaVesselData? RightEyeFundusRetinaVessel { get; set; }
        /// <summary>
        /// Left eye fundus retina and vessels examination data.
        /// </summary>
        public EyeFundusRetinaVesselData? LeftEyeFundusRetinaVessel { get; set; }

        // 12. Hốc mắt (Orbit)
        /// <summary>
        /// Right eye orbit examination data.
        /// </summary>
        public EyeOrbitData? RightEyeOrbit { get; set; }
        /// <summary>
        /// Left eye orbit examination data.
        /// </summary>
        public EyeOrbitData? LeftEyeOrbit { get; set; }

        // 2. Khám toàn thân
        /// <summary>
        /// Systemic examination data (blood pressure, temperature, pulse, etc.).
        /// </summary>
        public SystemicExamData? SystemicExam { get; set; }

        // ==================== IV. CÁC XÉT NGHIỆM CẦN LÀM ====================
        /// <summary>
        /// Required diagnostic tests.
        /// </summary>
        public string? RequiredTests { get; set; }

        // ==================== V. TÓM TẮT ====================
        /// <summary>
        /// Summary of examination findings.
        /// </summary>
        public string? Summary { get; set; }

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
        /// <summary>
        /// Diet plan.
        /// </summary>
        public string? DietPlan { get; set; }
        /// <summary>
        /// Care plan.
        /// </summary>
        public string? CarePlan { get; set; }

        // ==================== VITAL SIGNS (when creating record) ====================
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

        // ==================== NOTES ====================
        /// <summary>
        /// Additional notes.
        /// </summary>
        public string? Notes { get; set; }

        // ==================== B. TỔNG KẾT BỆNH ÁN (Summary) ====================
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

        // ==================== SUBSPECIALTY RECORDS ====================

        /// <summary>
        /// Trauma record (MS21_TRAUMA).
        /// </summary>
        public TraumaRecordData? TraumaRecord { get; set; }

        /// <summary>
        /// Trauma surgeries (MS21_TRAUMA).
        /// </summary>
        public List<TraumaSurgeryData>? TraumaSurgeries { get; set; }

        /// <summary>
        /// Lacrimal record (MS22_ANTERIOR, MS26_PEDIATRIC).
        /// </summary>
        public LacrimalRecordData? LacrimalRecord { get; set; }

        /// <summary>
        /// Glaucoma record (MS24_GLAUCOMA).
        /// </summary>
        public GlaucomaRecordData? GlaucomaRecord { get; set; }

        /// <summary>
        /// Glaucoma history entries for surgery/drug records (MS24_GLAUCOMA).
        /// </summary>
        public List<GlaucomaHistoryData>? GlaucomaHistories { get; set; }

        /// <summary>
        /// Strabismus & Ptosis record (MS25_STRABISMUS_PTOSIS).
        /// </summary>
        public StrabismusPtosisRecordData? StrabismusPtosisRecord { get; set; }

        /// <summary>
        /// Pediatric record (MS26_PEDIATRIC).
        /// </summary>
        public PediatricRecordData? PediatricRecord { get; set; }

        // ==================== PRESCRIPTIONS ====================

        /// <summary>
        /// Prescription header.
        /// </summary>
        public PrescriptionData? Prescription { get; set; }

        /// <summary>
        /// Prescription items (list of medicines).
        /// </summary>
        public List<PrescriptionItemData>? PrescriptionItems { get; set; }

        /// <summary>
        /// Glasses prescription (optional).
        /// </summary>
        public GlassesPrescriptionData? GlassesPrescription { get; set; }
    }

    #region Eye Basic Exam Data

    /// <summary>
    /// Basic eye examination data including visual acuity and intraocular pressure.
    /// </summary>
    public class EyeBasicExamData
    {
        // Thị lực (Visual Acuity)
        /// <summary>
        /// Uncorrected visual acuity.
        /// </summary>
        public string? VaUncorrected { get; set; }
        /// <summary>
        /// Best corrected visual acuity.
        /// </summary>
        public string? VaCorrected { get; set; }
        /// <summary>
        /// Near visual acuity.
        /// </summary>
        public string? VaNear { get; set; }
        /// <summary>
        /// Pinhole visual acuity.
        /// </summary>
        public string? VaPinhole { get; set; }
        /// <summary>
        /// Visual acuity with glasses.
        /// </summary>
        public string? VaWithGlasses { get; set; }

        // Nhãn áp (Intraocular Pressure)
        /// <summary>
        /// Intraocular pressure measured in mmHg.
        /// </summary>
        public string? IopMmhg { get; set; }
        /// <summary>
        /// Method used to measure intraocular pressure.
        /// </summary>
        public string? IopMethod { get; set; }

        // Khúc xạ máy (Refraction)
        /// <summary>
        /// Auto-refraction measurement result.
        /// </summary>
        public string? AutoRefraction { get; set; }
        /// <summary>
        /// Retinoscopy result.
        /// </summary>
        public string? Retinoscopy { get; set; }
        /// <summary>
        /// Subjective refraction result.
        /// </summary>
        public string? SubjectiveRefraction { get; set; }

        // Vận nhãn (Extraocular Movement)
        /// <summary>
        /// Extraocular movement status.
        /// </summary>
        public string? EomStatus { get; set; }
        /// <summary>
        /// Extraocular movement notes.
        /// </summary>
        public string? EomNote { get; set; }
        /// <summary>
        /// Presence of nystagmus.
        /// </summary>
        public string? Nystagmus { get; set; }
        /// <summary>
        /// Type of nystagmus.
        /// </summary>
        public string? NystagmusType { get; set; }

        // Thị trường (Visual Field)
        /// <summary>
        /// Visual field examination result.
        /// </summary>
        public string? VisualField { get; set; }
    }

    #endregion

    #region Eye Eyelid Data

    /// <summary>
    /// Eyelid examination data.
    /// </summary>
    public class EyeEyelidData
    {
        // Tình trạng chung (General Condition)
        /// <summary>
        /// Overall eyelid status: Normal, Edema, or Hematoma.
        /// </summary>
        public string? Status { get; set; }

        // Sụp mi (Ptosis)
        /// <summary>
        /// Whether ptosis (drooping eyelid) is present.
        /// </summary>
        public bool? Ptosis { get; set; }
        /// <summary>
        /// Degree of ptosis (Grade 1, 2, 3).
        /// </summary>
        public string? PtosisDegree { get; set; }

        // Rách mi (Eyelid Laceration)
        /// <summary>
        /// Whether eyelid laceration is present.
        /// </summary>
        public bool? Laceration { get; set; }
        /// <summary>
        /// Extent of laceration: Partial layer, Full thickness, Lid margin, or Tissue loss.
        /// </summary>
        public string? LacerationExtent { get; set; }
        /// <summary>
        /// Location of the laceration.
        /// </summary>
        public string? LacerationLocation { get; set; }
        /// <summary>
        /// Whether laceration has been sutured.
        /// </summary>
        public bool? LacerationSutured { get; set; }
        /// <summary>
        /// Whether laceration is unsutured.
        /// </summary>
        public bool? LacerationUnsutured { get; set; }

        // Lệ quản (Lacrimal Duct)
        /// <summary>
        /// Lacrimal duct status: Normal or Transected.
        /// </summary>
        public string? LacrimalDuctStatus { get; set; }
        /// <summary>
        /// Lacrimal duct injury location: Outer 1/3, Middle 1/3, Inner 1/3, or Both ducts transected.
        /// </summary>
        public string? LacrimalDuctLocation { get; set; }

        // Sẹo mi (Eyelid Scar)
        /// <summary>
        /// Whether scar tissue is present.
        /// </summary>
        public bool? Scar { get; set; }
        /// <summary>
        /// Description of scar.
        /// </summary>
        public string? ScarDescription { get; set; }

        // Tổn thương khác (Other Findings)
        /// <summary>
        /// Other examination findings.
        /// </summary>
        public string? OtherFindings { get; set; }

        // Quặm (MS26) - Entropion
        /// <summary>
        /// Whether entropion (inward turning of eyelid) is present.
        /// </summary>
        public bool? Entropion { get; set; }
        /// <summary>
        /// Whether epicanthus is present.
        /// </summary>
        public bool? Epicanthus { get; set; }
        /// <summary>
        /// Type of epicanthus.
        /// </summary>
        public string? EpicanthusType { get; set; }

        // U mi (Eyelid Tumor)
        /// <summary>
        /// Whether a tumor is present.
        /// </summary>
        public bool? HasTumor { get; set; }
        /// <summary>
        /// Nature of the tumor (benign/malignant).
        /// </summary>
        public string? TumorNature { get; set; }
        /// <summary>
        /// Location of the tumor.
        /// </summary>
        public string? TumorLocation { get; set; }
        /// <summary>
        /// Size of the tumor.
        /// </summary>
        public string? TumorSize { get; set; }

        // Hở mi, Trễ mi (Lagophthalmos, Lower Lid Retraction)
        /// <summary>
        /// Whether lagophthalmos (incomplete eyelid closure) is present.
        /// </summary>
        public bool? Lagophthalmos { get; set; }
        /// <summary>
        /// Whether lower lid retraction is present.
        /// </summary>
        public bool? LowerLidRetraction { get; set; }

        // Khuyết mi (Eyelid Defect)
        /// <summary>
        /// Description of eyelid defect if present.
        /// </summary>
        public string? EyelidDefect { get; set; }

        // Chắp, Lẹo (Chalazion, Hordeolum)
        /// <summary>
        /// Presence of chalazion or hordeolum.
        /// </summary>
        public string? ChalazionHordeolum { get; set; }
    }

    #endregion

    #region Eye Conjunctiva Data

    /// <summary>
    /// Conjunctiva examination data.
    /// </summary>
    public class EyeConjunctivaData
    {
        // Tình trạng chung (General Condition)
        /// <summary>
        /// Overall conjunctival status: Normal, Congestion, or Hemorrhage.
        /// </summary>
        public string? Status { get; set; }

        // Cương tụ (Congestion)
        /// <summary>
        /// Type of congestion: Diffuse, Bulbar, Limbal, or Total limbal.
        /// </summary>
        public string? CongestionType { get; set; }
        /// <summary>
        /// Location of congestion.
        /// </summary>
        public string? CongestionLocation { get; set; }

        // Xuất huyết (Hemorrhage)
        /// <summary>
        /// Whether subconjunctival hemorrhage is present.
        /// </summary>
        public bool? Hemorrhage { get; set; }
        /// <summary>
        /// Description of hemorrhage.
        /// </summary>
        public string? HemorrhageDescription { get; set; }

        // Rách kết mạc (Conjunctival Laceration)
        /// <summary>
        /// Whether conjunctival laceration is present.
        /// </summary>
        public bool? Laceration { get; set; }
        /// <summary>
        /// Location of laceration.
        /// </summary>
        public string? LacerationLocation { get; set; }

        // Thiếu máu (Ischemia)
        /// <summary>
        /// Whether conjunctival ischemia is present.
        /// </summary>
        public bool? Ischemia { get; set; }

        // Phù nề (Edema)
        /// <summary>
        /// Whether conjunctival edema (chemosis) is present.
        /// </summary>
        public bool? Edema { get; set; }

        // Nhú, Hột (Papillae, Follicles)
        /// <summary>
        /// Whether papillae are present.
        /// </summary>
        public bool? Papilla { get; set; }
        /// <summary>
        /// Whether follicles are present.
        /// </summary>
        public bool? Follicle { get; set; }

        // Sừng hóa (Keratinization)
        /// <summary>
        /// Whether keratinization is present.
        /// </summary>
        public bool? Keratinization { get; set; }

        // Sẹo kết mạc (Conjunctival Scar)
        /// <summary>
        /// Whether conjunctival scar is present.
        /// </summary>
        public bool? Scar { get; set; }

        // Tiết tố (Discharge)
        /// <summary>
        /// Type of discharge.
        /// </summary>
        public string? Discharge { get; set; }
        /// <summary>
        /// Whether fluorescein staining is positive.
        /// </summary>
        public bool? FluoresceinStain { get; set; }

        // Mắt ngả (Pterygium)
        /// <summary>
        /// Whether pterygium is present.
        /// </summary>
        public bool? Pterygium { get; set; }
        /// <summary>
        /// Location of pterygium.
        /// </summary>
        public string? PterygiumLocation { get; set; }
        /// <summary>
        /// Size of pterygium.
        /// </summary>
        public string? PterygiumSize { get; set; }

        // U kết mạc (Conjunctival Tumor)
        /// <summary>
        /// Whether a tumor is present.
        /// </summary>
        public bool? HasTumor { get; set; }
        /// <summary>
        /// Nature of the tumor.
        /// </summary>
        public string? TumorNature { get; set; }
        /// <summary>
        /// Location of the tumor.
        /// </summary>
        public string? TumorLocation { get; set; }
        /// <summary>
        /// Size of the tumor.
        /// </summary>
        public string? TumorSize { get; set; }

        // Cùng đồ (Fornix) - MS22, MS26
        /// <summary>
        /// Fornix status: Normal, Shallow, or Adherent.
        /// </summary>
        public string? FornixStatus { get; set; }
        /// <summary>
        /// Symblepharon height.
        /// </summary>
        public string? SymblepharonHeight { get; set; }
        /// <summary>
        /// Symblepharon width.
        /// </summary>
        public string? SymblepharonWidth { get; set; }

        // Tổn thương khác (Other Findings)
        /// <summary>
        /// Other examination findings.
        /// </summary>
        public string? OtherFindings { get; set; }
    }

    #endregion

    #region Eye Cornea Exam Data

    /// <summary>
    /// Cornea examination data.
    /// </summary>
    public class EyeCorneaExamData
    {
        // Tình trạng trong suốt (Transparency)
        /// <summary>
        /// Corneal clarity: Clear, Edema, or Scar.
        /// </summary>
        public string? Clarity { get; set; }
        /// <summary>
        /// Description of corneal scar.
        /// </summary>
        public string? Scar { get; set; }

        // Kích thước, hình dạng (Size, Shape)
        /// <summary>
        /// Corneal size: Normal, Large, or Small.
        /// </summary>
        public string? Size { get; set; }
        /// <summary>
        /// Corneal shape: Normal, Conical (Keratoconus), or Spherical.
        /// </summary>
        public string? Shape { get; set; }
        /// <summary>
        /// Corneal diameter in millimeters.
        /// </summary>
        public decimal? DiameterMm { get; set; }

        // Biểu mô (Epithelium)
        /// <summary>
        /// Epithelium status.
        /// </summary>
        public string? EpitheliumStatus { get; set; }
        /// <summary>
        /// Whether punctate epitheliopathy is present.
        /// </summary>
        public bool? EpitheliumPunctate { get; set; }
        /// <summary>
        /// Level of epithelium edema: Mild, Moderate, or Severe.
        /// </summary>
        public string? EpitheliumEdemaLevel { get; set; }
        /// <summary>
        /// Description of epithelium loss.
        /// </summary>
        public string? EpitheliumLoss { get; set; }

        // Tủa mặt sau (Posterior Deposits)
        /// <summary>
        /// Type of posterior deposit: Fresh, Old, or Pigmented.
        /// </summary>
        public string? PosteriorDeposit { get; set; }
        /// <summary>
        /// Location of posterior deposit.
        /// </summary>
        public string? PosteriorDepositLocation { get; set; }

        // Nhu mô (Stroma)
        /// <summary>
        /// Level of stromal edema: Mild, Moderate, or Severe.
        /// </summary>
        public string? StromaEdemaLevel { get; set; }
        /// <summary>
        /// Description of stromal infiltrate.
        /// </summary>
        public string? StromaInfiltrate { get; set; }
        /// <summary>
        /// Description of stromal thinning.
        /// </summary>
        public string? StromaThinning { get; set; }

        // Loét (Ulcer)
        /// <summary>
        /// Whether corneal ulcer is present.
        /// </summary>
        public bool? Ulcer { get; set; }
        /// <summary>
        /// Location of ulcer.
        /// </summary>
        public string? UlcerLocation { get; set; }
        /// <summary>
        /// Size of ulcer.
        /// </summary>
        public string? UlcerSize { get; set; }
        /// <summary>
        /// Description of ulcer.
        /// </summary>
        public string? UlcerDescription { get; set; }

        // Abces, Trợt (Abscess, Descmetocele)
        /// <summary>
        /// Whether corneal abscess is present.
        /// </summary>
        public bool? Abscess { get; set; }
        /// <summary>
        /// Whether descemetocele is present.
        /// </summary>
        public bool? Descemetocele { get; set; }

        // Ngấm máu (Blood Staining)
        /// <summary>
        /// Whether corneal blood staining is present.
        /// </summary>
        public bool? BloodStaining { get; set; }

        // Rách giác mạc (MS21) - Corneal Laceration
        /// <summary>
        /// Whether corneal laceration is present.
        /// </summary>
        public bool? Laceration { get; set; }
        /// <summary>
        /// Size of laceration.
        /// </summary>
        public string? LacerationSize { get; set; }
        /// <summary>
        /// Location of laceration.
        /// </summary>
        public string? LacerationLocation { get; set; }
        /// <summary>
        /// Type of laceration: Clean, Irregular, Tissue loss, or Intraocular tissue entrapment.
        /// </summary>
        public string? LacerationType { get; set; }
        /// <summary>
        /// Whether laceration has been sutured.
        /// </summary>
        public bool? LacerationSutured { get; set; }
        /// <summary>
        /// Whether anatomical reduction is achieved.
        /// </summary>
        public bool? AnatomicalReduction { get; set; }

        // Thủng (Perforation)
        /// <summary>
        /// Whether corneal perforation is present.
        /// </summary>
        public bool? Perforation { get; set; }
        /// <summary>
        /// Diameter of perforation in millimeters.
        /// </summary>
        public decimal? PerforationDiameterMm { get; set; }
        /// <summary>
        /// Location of perforation.
        /// </summary>
        public string? PerforationLocation { get; set; }
        /// <summary>
        /// Seidel test result: Sealed or Not sealed.
        /// </summary>
        public string? SeidelTest { get; set; }

        // Tân mạch (Neovascularization)
        /// <summary>
        /// Whether corneal neovascularization is present.
        /// </summary>
        public bool? Neovascularization { get; set; }
        /// <summary>
        /// Depth of neovascularization: Superficial or Deep.
        /// </summary>
        public string? NeovascularizationDepth { get; set; }
        /// <summary>
        /// Extent of neovascularization: ≤1/3, 1/3-2/3, or ≥2/3 of circumference.
        /// </summary>
        public string? NeovascularizationExtent { get; set; }

        // Vùng rìa (Limbal Zone)
        /// <summary>
        /// Limbal status: Limbal stem cell deficiency, Degenerative pterygium, or Calcific deposits.
        /// </summary>
        public string? LimbalStatus { get; set; }

        // Cảm giác giác mạc (Corneal Sensation)
        /// <summary>
        /// Corneal sensation: Absent, Reduced, or Normal.
        /// </summary>
        public string? Sensation { get; set; }

        // Viêm (Inflammation)
        /// <summary>
        /// Type of inflammation: Nodular, Diffuse, or Abscess.
        /// </summary>
        public string? InflammationType { get; set; }
        /// <summary>
        /// Depth of inflammation: Superficial or Deep.
        /// </summary>
        public string? InflammationDepth { get; set; }

        // Viêm thượng củng mạc (Episcleritis)
        /// <summary>
        /// Whether episcleritis is present.
        /// </summary>
        public bool? Episcleritis { get; set; }

        // Giãn lối (Staphyloma)
        /// <summary>
        /// Whether corneal staphyloma is present.
        /// </summary>
        public bool? Staphyloma { get; set; }

        // Dị vật (Foreign Body)
        /// <summary>
        /// Whether foreign body is present.
        /// </summary>
        public bool? ForeignBody { get; set; }
        /// <summary>
        /// Description of foreign body.
        /// </summary>
        public string? ForeignBodyDescription { get; set; }

        // Tổn thương khác (Other Findings)
        /// <summary>
        /// Other examination findings.
        /// </summary>
        public string? OtherFindings { get; set; }
    }

    #endregion

    #region Eye Sclera Exam Data

    /// <summary>
    /// Sclera examination data.
    /// </summary>
    public class EyeScleraExamData
    {
        /// <summary>
        /// Overall scleral status: Normal, Staphyloma, or Scar.
        /// </summary>
        public string? Status { get; set; }

        // Rách củng mạc (Scleral Laceration)
        /// <summary>
        /// Whether scleral laceration is present.
        /// </summary>
        public bool? Laceration { get; set; }
        /// <summary>
        /// Size of laceration.
        /// </summary>
        public string? LacerationSize { get; set; }
        /// <summary>
        /// Location of laceration.
        /// </summary>
        public string? LacerationLocation { get; set; }
        /// <summary>
        /// Whether laceration has been sutured.
        /// </summary>
        public bool? LacerationSutured { get; set; }
        /// <summary>
        /// Whether laceration is unsutured.
        /// </summary>
        public bool? LacerationUnsutured { get; set; }
        /// <summary>
        /// Whether tissue is entrapped in the laceration.
        /// </summary>
        public bool? TissueEntrapped { get; set; }

        // Tổn thương khác (Other Findings)
        /// <summary>
        /// Other examination findings.
        /// </summary>
        public string? OtherFindings { get; set; }
    }

    #endregion

    #region Eye Anterior Chamber Data

    /// <summary>
    /// Anterior chamber examination data.
    /// </summary>
    public class EyeAnteriorChamberData
    {
        // Độ sâu (Depth)
        /// <summary>
        /// Anterior chamber depth: Deep, Shallow, or Flat.
        /// </summary>
        public string? Depth { get; set; }
        /// <summary>
        /// Anterior chamber depth in millimeters.
        /// </summary>
        public decimal? DepthMm { get; set; }
        /// <summary>
        /// Herick classification: <1/4, 1/4-1/2, or ≥1/2 of corneal thickness.
        /// </summary>
        public string? HerickClassification { get; set; }

        // Chất thể thủy tinh (Vitreous in AC)
        /// <summary>
        /// Whether vitreous is present in the anterior chamber.
        /// </summary>
        public bool? VitreousInAC { get; set; }

        // Mủ (Pus)
        /// <summary>
        /// Whether hypopyon (pus) is present.
        /// </summary>
        public bool? Pus { get; set; }
        /// <summary>
        /// Height of hypopyon in millimeters.
        /// </summary>
        public decimal? PusMm { get; set; }

        // Xuất tiết (Exudate)
        /// <summary>
        /// Whether exudate is present.
        /// </summary>
        public bool? Exudate { get; set; }
        /// <summary>
        /// Description of exudate.
        /// </summary>
        public string? ExudateDescription { get; set; }
        /// <summary>
        /// Tyndall effect result.
        /// </summary>
        public string? Tyndall { get; set; }

        // Xuất huyết (Hemorrhage)
        /// <summary>
        /// Whether hyphema (blood in anterior chamber) is present.
        /// </summary>
        public bool? Hemorrhage { get; set; }
        /// <summary>
        /// Level of hyphema.
        /// </summary>
        public string? HemorrhageLevel { get; set; }

        // Dị vật (Foreign Body)
        /// <summary>
        /// Whether foreign body is present.
        /// </summary>
        public bool? ForeignBody { get; set; }

        // Tổn thương khác (Other Findings)
        /// <summary>
        /// Other examination findings.
        /// </summary>
        public string? OtherFindings { get; set; }
    }

    #endregion

    #region Eye Iris Pupil Data

    /// <summary>
    /// Iris and pupil examination data.
    /// </summary>
    public class EyeIrisPupilData
    {
        // Mống mắt (Iris)
        /// <summary>
        /// Iris color.
        /// </summary>
        public string? IrisColor { get; set; }
        /// <summary>
        /// Iris condition: Normal, Degeneration, Brown atrophic, or Fibrotic.
        /// </summary>
        public string? IrisCondition { get; set; }
        /// <summary>
        /// Whether iris degeneration is present.
        /// </summary>
        public bool? IrisDegeneration { get; set; }
        /// <summary>
        /// Whether iris neovascularization (rubeosis) is present.
        /// </summary>
        public bool? IrisNeovascularization { get; set; }
        /// <summary>
        /// Whether ciliary processes are visible (seclusio pupillae).
        /// </summary>
        public bool? IrisCiliaryProcesses { get; set; }

        // Nút Koeppe, Busacca
        /// <summary>
        /// Whether Koeppe nodules are present.
        /// </summary>
        public bool? KoeppeNodules { get; set; }
        /// <summary>
        /// Whether Busacca nodules are present.
        /// </summary>
        public bool? BusaccaNodules { get; set; }

        // Đứt chân mống, Mất mống (Iris Root Tear, Iris Loss)
        /// <summary>
        /// Whether iris root tear (iridodialysis) is present.
        /// </summary>
        public bool? IrisRootTear { get; set; }
        /// <summary>
        /// Degree of iris root tear.
        /// </summary>
        public string? IrisRootTearDegree { get; set; }
        /// <summary>
        /// Whether iris tissue loss is present.
        /// </summary>
        public bool? IrisLoss { get; set; }

        // Thủng mống mắt (Iris Perforation)
        /// <summary>
        /// Whether iris perforation is present.
        /// </summary>
        public bool? IrisPerforation { get; set; }

        // Đồng tử (Pupil)
        /// <summary>
        /// Pupil diameter in millimeters.
        /// </summary>
        public decimal? PupilDiameterMm { get; set; }
        /// <summary>
        /// Pupil shape: Round, Irregular (poikylocoria), or Occluded.
        /// </summary>
        public string? PupilShape { get; set; }
        /// <summary>
        /// Position of occlusion if present.
        /// </summary>
        public string? PupilPosition { get; set; }
        /// <summary>
        /// Pupillary reflex: Normal, Reduced, or Absent.
        /// </summary>
        public string? PupilReflex { get; set; }
        /// <summary>
        /// Whether pupil is dilated and fixed (amaurotic).
        /// </summary>
        public bool? PupilDilated { get; set; }
        /// <summary>
        /// Whether relative afferent pupillary defect (RAPD) test was performed.
        /// </summary>
        public bool? PtdtTest { get; set; }

        // Ánh đồng tử (Fundus Reflex)
        /// <summary>
        /// Fundus reflex: Pink (normal), Gray (cataract), or Not visible.
        /// </summary>
        public string? FundusReflex { get; set; }

        // Tổn thương khác (Other Findings)
        /// <summary>
        /// Other examination findings.
        /// </summary>
        public string? OtherFindings { get; set; }
    }

    #endregion

    #region Eye Lens Data

    /// <summary>
    /// Lens examination data.
    /// </summary>
    public class EyeLensData
    {
        /// <summary>
        /// Overall lens status: Clear, Opacified (cataract), Dislocated, or Foreign body.
        /// </summary>
        public string? Status { get; set; }

        // Đục thể thủy tinh (Cataract)
        /// <summary>
        /// Type of opacity: Nuclear, Cortical, Subcapsular, or Total.
        /// </summary>
        public string? OpacityType { get; set; }
        /// <summary>
        /// Location of opacity.
        /// </summary>
        public string? OpacityLocation { get; set; }

        // Sa lệch (Subluxation)
        /// <summary>
        /// Whether lens subluxation is present.
        /// </summary>
        public bool? Subluxation { get; set; }

        // Ra tiền phòng, Vào buồng dịch kính (Lens Displacement)
        /// <summary>
        /// Whether lens is in the anterior chamber.
        /// </summary>
        public bool? LensInAnterior { get; set; }
        /// <summary>
        /// Whether lens is in the vitreous cavity.
        /// </summary>
        public bool? LensInVitreous { get; set; }

        // Viêm mủ (Purulent)
        /// <summary>
        /// Whether purulent infection of lens is present.
        /// </summary>
        public bool? Purulent { get; set; }

        // Dính sắc tố mặt trước (Anterior Pigmentation)
        /// <summary>
        /// Whether anterior pigmentation on lens is present.
        /// </summary>
        public bool? AnteriorPigmentation { get; set; }

        // IOL (Intraocular Lens)
        /// <summary>
        /// Whether intraocular lens is present.
        /// </summary>
        public bool? IolPresent { get; set; }
        /// <summary>
        /// IOL status: Well-positioned, Decentered, or Posterior capsular opacification.
        /// </summary>
        public string? IolStatus { get; set; }
        /// <summary>
        /// IOL position: In anterior chamber or In bag.
        /// </summary>
        public string? IolPosition { get; set; }

        // Tổn thương khác (Other Findings)
        /// <summary>
        /// Other examination findings.
        /// </summary>
        public string? OtherFindings { get; set; }
    }

    #endregion

    #region Eye Vitreous Data

    public class EyeVitreousData
    {
        public string? Status { get; set; } // Sạch, Đục, Xuất huyết, Viêm mủ

        // Đục dịch kính
        public string? OpacityLevel { get; set; }
        public string? Tyndall { get; set; }

        // Xuất huyết
        public bool? Hemorrhage { get; set; }

        // Tổ chức hóa
        public bool? Organized { get; set; }

        // Bong dịch kính sau
        public bool? Pvd { get; set; }

        // Viêm mủ
        public bool? Purulent { get; set; }

        // Dị vật
        public bool? ForeignBody { get; set; }

        // Tổn thương khác
        public string? OtherFindings { get; set; }
    }

    #endregion

    #region Eye Fundus Disc Macula Data

    /// <summary>
    /// Fundus examination data - optic disc and macula.
    /// </summary>
    public class EyeFundusDiscMaculaData
    {
        // Đĩa thị (Optic Disc)
        /// <summary>
        /// Optic disc status: Normal, Edema, Atrophy, or Pallor.
        /// </summary>
        public string? DiscStatus { get; set; }
        /// <summary>
        /// Color of the optic disc.
        /// </summary>
        public string? DiscColor { get; set; }
        /// <summary>
        /// Cup-to-disc ratio.
        /// </summary>
        public string? CdRatio { get; set; }
        /// <summary>
        /// Neuroretinal rim status: Normal or Abnormal.
        /// </summary>
        public string? RimStatus { get; set; }
        /// <summary>
        /// Location of rim abnormality: Inferior, Superior, Nasal, or Temporal.
        /// </summary>
        public string? RimLocation { get; set; }
        /// <summary>
        /// Vessel change at disc: Normal, Bayoneting, or Kinking.
        /// </summary>
        public string? VesselChange { get; set; }
        /// <summary>
        /// Whether disc hemorrhage is present.
        /// </summary>
        public bool? DiscHemorrhage { get; set; }
        /// <summary>
        /// Whether optic disc neovascularization (NVD) is present.
        /// </summary>
        public bool? Neovascularization { get; set; }
        /// <summary>
        /// Degree of neovascularization.
        /// </summary>
        public string? NeovascularizationDegree { get; set; }
        /// <summary>
        /// Whether optic disc is not visible.
        /// </summary>
        public bool? DiscNotVisible { get; set; }

        // Hoàng điểm (Macula)
        /// <summary>
        /// Macular status: Normal, Edema, or Scarred/Atrophic.
        /// </summary>
        public string? MaculaStatus { get; set; }
        /// <summary>
        /// Whether foveal reflex is absent.
        /// </summary>
        public bool? MaculaReflexAbsent { get; set; }
        /// <summary>
        /// Type of macular edema.
        /// </summary>
        public string? MaculaEdemaType { get; set; }
        /// <summary>
        /// Degree of macular hole.
        /// </summary>
        public string? MaculaHoleDegree { get; set; }
        /// <summary>
        /// Whether macular scar is present.
        /// </summary>
        public bool? MaculaScar { get; set; }
        /// <summary>
        /// Whether serous retinal detachment is present.
        /// </summary>
        public bool? SerousDetachment { get; set; }
        /// <summary>
        /// Whether macular hemorrhage is present.
        /// </summary>
        public bool? MaculaHemorrhage { get; set; }
        /// <summary>
        /// Macular condition description.
        /// </summary>
        public string? MaculaCondition { get; set; }

        // Hắc mạc (Choroid)
        /// <summary>
        /// Choroidal status.
        /// </summary>
        public string? ChoroidStatus { get; set; }
        /// <summary>
        /// Choroidal findings.
        /// </summary>
        public string? ChoroidFindings { get; set; }
        /// <summary>
        /// Whether choroidal neovascularization (CNV) is present.
        /// </summary>
        public bool? CNV { get; set; }

        // Ổ viêm hắc mạc (Chorioretinitis)
        /// <summary>
        /// Whether active chorioretinitis is present.
        /// </summary>
        public bool? ChorioretinitisActive { get; set; }
        /// <summary>
        /// Whether chorioretinitis scar is present.
        /// </summary>
        public bool? ChorioretinitisScar { get; set; }
        /// <summary>
        /// Number of chorioretinitis lesions.
        /// </summary>
        public int? ChorioretinitisCount { get; set; }
        /// <summary>
        /// Location of chorioretinitis.
        /// </summary>
        public string? ChorioretinitisLocation { get; set; }

        // Tổn thương khác (Other Findings)
        /// <summary>
        /// Other examination findings.
        /// </summary>
        public string? OtherFindings { get; set; }
    }

    #endregion

    #region Eye Fundus Retina Vessel Data

    /// <summary>
    /// Fundus examination data - retina and blood vessels.
    /// </summary>
    public class EyeFundusRetinaVesselData
    {
        // Hệ mạch (Vessels)
        /// <summary>
        /// Overall vessel status: Normal.
        /// </summary>
        public string? VesselStatus { get; set; }

        // Tắc động mạch (Artery Occlusion)
        /// <summary>
        /// Type of artery occlusion: Central, Branch, or Branch retinal artery occlusion (BRAO).
        /// </summary>
        public string? ArteryOcclusion { get; set; }

        // Tắc tĩnh mạch (Vein Occlusion)
        /// <summary>
        /// Type of vein occlusion: Central (CRVO) or Branch (BRVO).
        /// </summary>
        public string? VeinOcclusion { get; set; }
        /// <summary>
        /// Type of occlusion: Edematous, Ischemic, or Mixed.
        /// </summary>
        public string? OcclusionType { get; set; }

        // Viêm mao mạch (Vasculitis)
        /// <summary>
        /// Whether vasculitis is present.
        /// </summary>
        public bool? Vasculitis { get; set; }

        // Tân mạch võng mạc (Retinal Neovascularization)
        /// <summary>
        /// Whether retinal neovascularization is present.
        /// </summary>
        public bool? RetinalNeovascularization { get; set; }

        // Võng mạc (Retina)
        /// <summary>
        /// Overall retinal status: Normal, Vasculitis, or Neovascularization.
        /// </summary>
        public string? RetinaStatus { get; set; }
        /// <summary>
        /// Additional retinal condition description.
        /// </summary>
        public string? RetinalCondition { get; set; }

        // Phù (Edema)
        /// <summary>
        /// Whether retinal edema is present.
        /// </summary>
        public bool? RetinalEdema { get; set; }
        /// <summary>
        /// Type of edema: Localized or Diffuse.
        /// </summary>
        public string? EdemaType { get; set; }

        // Xuất huyết (Hemorrhage)
        /// <summary>
        /// Whether retinal hemorrhage is present.
        /// </summary>
        public bool? Hemorrhage { get; set; }
        /// <summary>
        /// Type of hemorrhage: Superficial (flame), Deep (dot/blot), or Choroidal.
        /// </summary>
        public string? HemorrhageType { get; set; }

        // Xuất tiết (Exudate)
        /// <summary>
        /// Type of exudate: Hard or Cotton-wool.
        /// </summary>
        public string? ExudateType { get; set; }

        // Thoái hóa (Degeneration)
        /// <summary>
        /// Whether retinal degeneration is present.
        /// </summary>
        public bool? Degeneration { get; set; }
        /// <summary>
        /// Type of degeneration: Peripheral or Macular.
        /// </summary>
        public string? DegenerationType { get; set; }
        /// <summary>
        /// Description of degeneration.
        /// </summary>
        public string? DegenerationDescription { get; set; }

        // Bong võng mạc (Retinal Detachment)
        /// <summary>
        /// Whether retinal detachment is present.
        /// </summary>
        public bool? Detachment { get; set; }
        /// <summary>
        /// Level of retinal detachment.
        /// </summary>
        public string? DetachmentLevel { get; set; }

        // Rách võng mạc (Retinal Tear)
        /// <summary>
        /// Whether retinal tear is present.
        /// </summary>
        public bool? RetinalTear { get; set; }
        /// <summary>
        /// Number of retinal tears.
        /// </summary>
        public int? TearCount { get; set; }
        /// <summary>
        /// Location of retinal tear.
        /// </summary>
        public string? TearLocation { get; set; }
        /// <summary>
        /// Morphology of retinal tear.
        /// </summary>
        public string? TearMorphology { get; set; }

        // Bong BMST (BMSC Detachment)
        /// <summary>
        /// Whether Bruch's membrane/retinal pigment epithelium complex detachment is present.
        /// </summary>
        public bool? BmscDetachment { get; set; }

        // Dị vật nội nhãn (IOFB)
        /// <summary>
        /// Whether intraocular foreign body (IOFB) is present.
        /// </summary>
        public bool? Iofb { get; set; }
        /// <summary>
        /// Location of intraocular foreign body.
        /// </summary>
        public string? IofbLocation { get; set; }
        /// <summary>
        /// Size of intraocular foreign body.
        /// </summary>
        public string? IofbSize { get; set; }

        // Tổn thương phối hợp (Combined Findings)
        /// <summary>
        /// Description of combined findings.
        /// </summary>
        public string? CombinedFindings { get; set; }

        // Tổn thương khác (Other Findings)
        /// <summary>
        /// Other examination findings.
        /// </summary>
        public string? OtherFindings { get; set; }
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

    #region Systemic Exam Data

    /// <summary>
    /// Systemic examination data for general health assessment.
    /// </summary>
    public class SystemicExamData
    {
        // Huyết áp, Nhiệt độ, Mạch, Nhịp thở (Vital Signs)
        /// <summary>
        /// Blood pressure reading.
        /// </summary>
        public string? BloodPressure { get; set; }
        /// <summary>
        /// Body temperature.
        /// </summary>
        public string? Temperature { get; set; }
        /// <summary>
        /// Pulse rate.
        /// </summary>
        public string? Pulse { get; set; }
        /// <summary>
        /// Respiratory rate.
        /// </summary>
        public string? RespiratoryRate { get; set; }

        // Nội tiết (Endocrine)
        /// <summary>
        /// Endocrine status: Normal or Pathological.
        /// </summary>
        public string? EndocrineStatus { get; set; }
        /// <summary>
        /// Endocrine findings if pathological.
        /// </summary>
        public string? EndocrineFindings { get; set; }

        // Tâm thần, thần kinh (Neuropsychiatric)
        /// <summary>
        /// Neuropsychiatric status.
        /// </summary>
        public string? NeuroStatus { get; set; }
        /// <summary>
        /// Neuropsychiatric findings.
        /// </summary>
        public string? NeuroFindings { get; set; }

        // Tuần hoàn (Cardiovascular)
        /// <summary>
        /// Cardiovascular status.
        /// </summary>
        public string? CardiovascularStatus { get; set; }
        /// <summary>
        /// Cardiovascular findings.
        /// </summary>
        public string? CardiovascularFindings { get; set; }

        // Hô hấp (Respiratory)
        /// <summary>
        /// Respiratory status.
        /// </summary>
        public string? RespiratoryStatus { get; set; }
        /// <summary>
        /// Respiratory findings.
        /// </summary>
        public string? RespiratoryFindings { get; set; }

        // Tiêu hóa (Digestive)
        /// <summary>
        /// Digestive status.
        /// </summary>
        public string? DigestiveStatus { get; set; }
        /// <summary>
        /// Digestive findings.
        /// </summary>
        public string? DigestiveFindings { get; set; }

        // Cơ xương khớp (Musculoskeletal)
        /// <summary>
        /// Musculoskeletal status.
        /// </summary>
        public string? MusculoskeletalStatus { get; set; }
        /// <summary>
        /// Musculoskeletal findings.
        /// </summary>
        public string? MusculoskeletalFindings { get; set; }

        // Tiết niệu, sinh dục (Urogenital)
        /// <summary>
        /// Urogenital status.
        /// </summary>
        public string? UrogenitalStatus { get; set; }
        /// <summary>
        /// Urogenital findings.
        /// </summary>
        public string? UrogenitalFindings { get; set; }

        // Khác (Other)
        /// <summary>
        /// Other findings.
        /// </summary>
        public string? OtherFindings { get; set; }
    }

    #endregion

    #region Subspecialty Record Data Classes

    /// <summary>
    /// Trauma record data for MS21 (Trauma) medical records.
    /// </summary>
    public class TraumaRecordData
    {
        /// <summary>
        /// Cause of the injury.
        /// </summary>
        public string? InjuryCause { get; set; }
        /// <summary>
        /// Time when the injury occurred.
        /// </summary>
        public DateTime? InjuryTime { get; set; }
        /// <summary>
        /// Prior treatment received.
        /// </summary>
        public string? PriorTreatment { get; set; }
        /// <summary>
        /// Clinical course after treatment.
        /// </summary>
        public string? PostTreatmentCourse { get; set; }
        /// <summary>
        /// Injuries to right eye (OD).
        /// </summary>
        public string? OdInjuries { get; set; }
        /// <summary>
        /// Injuries to left eye (OS).
        /// </summary>
        public string? OsInjuries { get; set; }
        /// <summary>
        /// Detailed description of injuries.
        /// </summary>
        public string? InjuryDetails { get; set; }
        /// <summary>
        /// Conclusion of trauma assessment.
        /// </summary>
        public string? TraumaConclusion { get; set; }
    }

    /// <summary>
    /// Trauma surgery record for MS21 (Trauma) medical records.
    /// </summary>
    public class TraumaSurgeryData
    {
        /// <summary>
        /// Date of surgery.
        /// </summary>
        public DateTime? SurgeryDate { get; set; }
        /// <summary>
        /// Type of surgery performed.
        /// </summary>
        public string? SurgeryType { get; set; }
        /// <summary>
        /// Description of the surgical procedure.
        /// </summary>
        public string? SurgeryDescription { get; set; }
        /// <summary>
        /// Name of the surgeon.
        /// </summary>
        public string? SurgeonName { get; set; }
        /// <summary>
        /// Type of anesthesia used.
        /// </summary>
        public string? AnesthesiaType { get; set; }
        /// <summary>
        /// Patient's condition after surgery.
        /// </summary>
        public string? PostSurgeryCondition { get; set; }
        /// <summary>
        /// Additional notes.
        /// </summary>
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Lacrimal record data for MS22 (Anterior) and MS26 (Pediatric) medical records.
    /// </summary>
    public class LacrimalRecordData
    {
        /// <summary>
        /// Eye side: OD (right), OS (left), or OU (both).
        /// </summary>
        public string Side { get; set; } = string.Empty;
        /// <summary>
        /// Whether lacrimal irrigation flows freely.
        /// </summary>
        public bool IrrigationFree { get; set; } = true;
        /// <summary>
        /// Whether regurgitation occurs from the same puncture point.
        /// </summary>
        public bool IrrigationRegurgitationSame { get; set; } = false;
        /// <summary>
        /// Whether regurgitation occurs from the opposite puncture point.
        /// </summary>
        public bool IrrigationRegurgitationOpposite { get; set; } = false;
        /// <summary>
        /// Notes on lacrimal irrigation.
        /// </summary>
        public string? IrrigationNote { get; set; }
        /// <summary>
        /// Other lacrimal findings.
        /// </summary>
        public string? LacrimalOther { get; set; }
    }

    /// <summary>
    /// Glaucoma record data for MS24 (Glaucoma) medical records.
    /// </summary>
    public class GlaucomaRecordData
    {
        // ===== SYMPTOMS (Triệu chứng) =====
        /// <summary>
        /// Level of eye pain: Severe, Moderate, Mild, or None.
        /// </summary>
        public string? EyePainLevel { get; set; }
        /// <summary>
        /// Vision symptoms: Sudden blur, Intermittent blur, Foggy vision, or None.
        /// </summary>
        public string? VisionSymptoms { get; set; }
        /// <summary>
        /// Vision progression: Progressive blur, Tunnel vision, or Halos.
        /// </summary>
        public string? VisionProgression { get; set; }
        /// <summary>
        /// Whether photophobia (light sensitivity) is present.
        /// </summary>
        public bool HasPhotophobia { get; set; } = false;
        /// <summary>
        /// Whether excessive tearing is present.
        /// </summary>
        public bool HasTearing { get; set; } = false;
        /// <summary>
        /// Whether eye redness is present.
        /// </summary>
        public bool HasRedness { get; set; } = false;
        /// <summary>
        /// Systemic symptoms: Headache, Vomiting, or Nausea.
        /// </summary>
        public string? SystemicSymptoms { get; set; }

        // ===== VISUAL ACUITY & IOP =====
        /// <summary>
        /// Visual acuity without correction - right eye.
        /// </summary>
        public string? VaWithoutCorrectionOd { get; set; }
        /// <summary>
        /// Visual acuity without correction - left eye.
        /// </summary>
        public string? VaWithoutCorrectionOs { get; set; }
        /// <summary>
        /// Visual acuity with correction - right eye.
        /// </summary>
        public string? VaWithCorrectionOd { get; set; }
        /// <summary>
        /// Visual acuity with correction - left eye.
        /// </summary>
        public string? VaWithCorrectionOs { get; set; }
        /// <summary>
        /// Intraocular pressure - right eye.
        /// </summary>
        public string? IopOd { get; set; }
        /// <summary>
        /// Intraocular pressure - left eye.
        /// </summary>
        public string? IopOs { get; set; }
        /// <summary>
        /// IOP measurement method: Mackay-Marg or Goldmann.
        /// </summary>
        public string? IopMethod { get; set; }
        /// <summary>
        /// Target intraocular pressure - right eye.
        /// </summary>
        public string? IopTargetOd { get; set; }
        /// <summary>
        /// Target intraocular pressure - left eye.
        /// </summary>
        public string? IopTargetOs { get; set; }

        // ===== HISTORY (Tiền sử) =====
        /// <summary>
        /// Eye disease history: Myopia, Hyperopia, Trauma, Uveitis, or CRVO.
        /// </summary>
        public string? HistoryEye { get; set; }
        /// <summary>
        /// History of eye surgery.
        /// </summary>
        public string? HistoryEyeSurgery { get; set; }
        /// <summary>
        /// Details of prior eye surgery.
        /// </summary>
        public string? PriorEyeSurgeryDetails { get; set; }
        /// <summary>
        /// Steroid use: Name, duration, and route of administration.
        /// </summary>
        public string? SteroidUse { get; set; }
        /// <summary>
        /// Whether steroid was prescribed by doctor or self-administered.
        /// </summary>
        public string? SteroidPrescribed { get; set; }
        /// <summary>
        /// Duration of medication use.
        /// </summary>
        public string? MedicationDuration { get; set; }
        /// <summary>
        /// Route of medication administration.
        /// </summary>
        public string? MedicationRoute { get; set; }

        // Systemic history
        /// <summary>
        /// Whether cardiovascular disease is present.
        /// </summary>
        public bool HasCardiovascularDisease { get; set; } = false;
        /// <summary>
        /// Whether hypertension is present.
        /// </summary>
        public bool HasHypertension { get; set; } = false;
        /// <summary>
        /// Whether diabetes is present.
        /// </summary>
        public bool HasDiabetes { get; set; } = false;
        /// <summary>
        /// Whether carotid fistula is present.
        /// </summary>
        public bool HasCarotidFistula { get; set; } = false;
        /// <summary>
        /// Other systemic diseases.
        /// </summary>
        public string? OtherSystemicDisease { get; set; }

        // Family history
        /// <summary>
        /// Whether family has glaucoma history.
        /// </summary>
        public bool FamilyHasGlaucoma { get; set; } = false;
        /// <summary>
        /// Family relation with glaucoma: Grandparents, Parents, Siblings, or Extended family.
        /// </summary>
        public string? FamilyGlaucomaRelation { get; set; }

        // ===== TREATMENT HISTORY =====
        /// <summary>
        /// Current glaucoma medications.
        /// </summary>
        public string? GlaucomaMedications { get; set; }
        /// <summary>
        /// Other medications being taken.
        /// </summary>
        public string? OtherMedications { get; set; }
        /// <summary>
        /// Progress of treatment.
        /// </summary>
        public string? TreatmentProgress { get; set; }

        // ===== CLASSIFICATION =====
        /// <summary>
        /// Type of glaucoma.
        /// </summary>
        public string? GlaucomaType { get; set; }
        /// <summary>
        /// Stage of glaucoma - right eye.
        /// </summary>
        public string? StageOd { get; set; }
        /// <summary>
        /// Stage of glaucoma - left eye.
        /// </summary>
        public string? StageOs { get; set; }

        // ===== EXAMINATION =====
        // Eyelid
        /// <summary>
        /// Whether eyelid swelling is present.
        /// </summary>
        public bool HasEyelidSwelling { get; set; } = false;

        // Conjunctiva
        /// <summary>
        /// Whether conjunctival injection is present.
        /// </summary>
        public bool HasConjunctivalInjection { get; set; } = false;
        /// <summary>
        /// Whether filtering bleb is present.
        /// </summary>
        public bool HasFilteringBleb { get; set; } = false;
        /// <summary>
        /// Location of filtering bleb.
        /// </summary>
        public string? BlebLocation { get; set; }
        /// <summary>
        /// Status of filtering bleb: Functional, Flat, Fibrotic, Thin, or Overhanging.
        /// </summary>
        public string? BlebStatus { get; set; }
        /// <summary>
        /// Location of conjunctival scar.
        /// </summary>
        public string? ConjunctivalScarLocation { get; set; }

        // Cornea
        /// <summary>
        /// Corneal transparency: Clear, Scar, or Edema.
        /// </summary>
        public string? CornealTransparency { get; set; }
        /// <summary>
        /// Level of corneal edema.
        /// </summary>
        public string? CornealEdemaLevel { get; set; }
        /// <summary>
        /// Corneal thickness measurement.
        /// </summary>
        public string? CornealThickness { get; set; }

        // Sclera
        /// <summary>
        /// Whether scleral thinning is present.
        /// </summary>
        public bool HasScleralThinning { get; set; } = false;
        /// <summary>
        /// Location of scleral scar.
        /// </summary>
        public string? ScleralScarLocation { get; set; }

        // Anterior Chamber
        /// <summary>
        /// Anterior chamber depth by Smith classification.
        /// </summary>
        public string? AcDepthSmith { get; set; }
        /// <summary>
        /// Anterior chamber depth by Herick classification.
        /// </summary>
        public string? AcDepthHerick { get; set; }

        // Gonioscopy
        /// <summary>
        /// Gonioscopy findings - right eye.
        /// </summary>
        public string? GonioscopyOd { get; set; }
        /// <summary>
        /// Gonioscopy findings - left eye.
        /// </summary>
        public string? GonioscopyOs { get; set; }
        /// <summary>
        /// Angle findings description.
        /// </summary>
        public string? AngleFindings { get; set; }

        // Iris
        /// <summary>
        /// Iris color.
        /// </summary>
        public string? IrisColor { get; set; }
        /// <summary>
        /// Iris condition: Degeneration.
        /// </summary>
        public string? IrisCondition { get; set; }
        /// <summary>
        /// Whether iris neovascularization (rubeosis) is present.
        /// </summary>
        public bool HasIrisNeovascularization { get; set; } = false;

        // Pupil
        /// <summary>
        /// Pupil diameter.
        /// </summary>
        public string? PupilDiameter { get; set; }
        /// <summary>
        /// Pupil pigment border.
        /// </summary>
        public string? PupilPigmentBorder { get; set; }
        /// <summary>
        /// Pupillary reflex response: Normal, Reduced, or Absent.
        /// </summary>
        public string? PupilReflexResponse { get; set; }

        // Lens
        /// <summary>
        /// Lens status: Clear or Cataract.
        /// </summary>
        public string? LensStatus { get; set; }

        // Fundus findings
        /// <summary>
        /// Fundus retina findings.
        /// </summary>
        public string? FundusRetinaFindings { get; set; }
        /// <summary>
        /// Fundus macula findings.
        /// </summary>
        public string? FundusMaculaFindings { get; set; }
        /// <summary>
        /// Whether choroidal neovascularization (CNV) is present.
        /// </summary>
        public bool HasCNV { get; set; } = false;
        /// <summary>
        /// Whether retinal hemorrhage is present.
        /// </summary>
        public bool HasRetinalHemorrhage { get; set; } = false;

        // Optic disc
        /// <summary>
        /// Optic disc description.
        /// </summary>
        public string? OpticDiscDescription { get; set; }
        /// <summary>
        /// Nerve rim status - right eye.
        /// </summary>
        public string? NerveRimOd { get; set; }
        /// <summary>
        /// Nerve rim status - left eye.
        /// </summary>
        public string? NerveRimOs { get; set; }
        /// <summary>
        /// Optic disc cup ratio.
        /// </summary>
        public string? OpticDiscCupRatio { get; set; }
        /// <summary>
        /// Optic disc vessel change.
        /// </summary>
        public string? OpticDiscVesselChange { get; set; }
        /// <summary>
        /// Whether optic disc hemorrhage is present.
        /// </summary>
        public bool HasOpticDiscHemorrhage { get; set; } = false;
        /// <summary>
        /// Whether rim atrophy is present.
        /// </summary>
        public bool HasRimAtrophy { get; set; } = false;

        // ===== EYE MEASUREMENTS =====
        /// <summary>
        /// Eye axial length measurement.
        /// </summary>
        public string? EyeAxialLength { get; set; }

        // ===== TREATMENT PLAN =====
        /// <summary>
        /// Surgical treatment plan.
        /// </summary>
        public string? TreatmentPlanSurgery { get; set; }
        /// <summary>
        /// Laser treatment plan.
        /// </summary>
        public string? TreatmentPlanLaser { get; set; }
        /// <summary>
        /// Medication treatment plan.
        /// </summary>
        public string? TreatmentPlanMedication { get; set; }
        /// <summary>
        /// Follow-up plan.
        /// </summary>
        public string? FollowUpPlan { get; set; }
    }

    /// <summary>
    /// Glaucoma surgery/drug history record for MS24 (Glaucoma) medical records.
    /// </summary>
    public class GlaucomaHistoryData
    {
        /// <summary>
        /// Type of history: Surgery or Medication.
        /// </summary>
        public string HistoryType { get; set; } = string.Empty;
        /// <summary>
        /// Eye side: OD, OS, or OU.
        /// </summary>
        public string? EyeSide { get; set; }
        /// <summary>
        /// Attempt number.
        /// </summary>
        public int? AttemptNumber { get; set; }
        /// <summary>
        /// Type of procedure.
        /// </summary>
        public string? ProcedureType { get; set; }
        /// <summary>
        /// Date of procedure.
        /// </summary>
        public DateTime? ProcedureDate { get; set; }
        /// <summary>
        /// Facility level: District, Provincial, Central, or Other.
        /// </summary>
        public string? FacilityLevel { get; set; }
        /// <summary>
        /// Name of drug.
        /// </summary>
        public string? DrugName { get; set; }
        /// <summary>
        /// Dosage of medication.
        /// </summary>
        public string? Dosage { get; set; }
        /// <summary>
        /// Duration of treatment.
        /// </summary>
        public string? Duration { get; set; }
        /// <summary>
        /// Route of administration: Eye drops, Eye injection, or Systemic.
        /// </summary>
        public string? Route { get; set; }
        /// <summary>
        /// Reason for change.
        /// </summary>
        public string? ChangeReason { get; set; }
    }

    /// <summary>
    /// Strabismus and ptosis record data for MS25 (Strabismus/Ptosis) medical records.
    /// </summary>
    public class StrabismusPtosisRecordData
    {
        // ===== CHIEF COMPLAINT & CAUSE =====
        /// <summary>
        /// Whether chief complaint is strabismus.
        /// </summary>
        public bool ChiefStrabismus { get; set; } = false;
        /// <summary>
        /// Whether chief complaint is ptosis.
        /// </summary>
        public bool ChiefPtosis { get; set; } = false;
        /// <summary>
        /// Whether condition is congenital.
        /// </summary>
        public bool Congenital { get; set; } = false;
        /// <summary>
        /// Whether condition is acquired.
        /// </summary>
        public bool Acquired { get; set; } = false;
        /// <summary>
        /// Onset time for acquired condition.
        /// </summary>
        public string? AcquiredOnset { get; set; }

        // ===== STRABISMUS TYPE =====
        /// <summary>
        /// Type of strabismus: Esotropia, Exotropia, or Hypertropia.
        /// </summary>
        public string? StrabismusType { get; set; }

        // ===== NYSTAGMUS =====
        /// <summary>
        /// Whether nystagmus is present.
        /// </summary>
        public bool Nystagmus { get; set; } = false;
        /// <summary>
        /// Type of nystagmus.
        /// </summary>
        public string? NystagmusType { get; set; }

        // ===== TREATMENT HISTORY =====
        /// <summary>
        /// Prior amblyopia treatment received.
        /// </summary>
        public string? PriorAmblyopiaTreatment { get; set; }
        /// <summary>
        /// Result of prior amblyopia treatment: Good, Average, or Poor.
        /// </summary>
        public string? PriorAmblyopiaResult { get; set; }
        /// <summary>
        /// Prior surgery performed.
        /// </summary>
        public string? PriorSurgery { get; set; }
        /// <summary>
        /// Result of prior surgery: Good, Under-corrected, or Over-corrected.
        /// </summary>
        public string? PriorSurgeryResult { get; set; }

        // ===== VISUAL ACUITY BEFORE/AFTER ATROPINE =====
        /// <summary>
        /// Visual acuity before atropine - right eye.
        /// </summary>
        public string? VaBeforeAtropineOd { get; set; }
        /// <summary>
        /// Visual acuity before atropine - left eye.
        /// </summary>
        public string? VaBeforeAtropineOs { get; set; }
        /// <summary>
        /// Visual acuity after atropine - right eye.
        /// </summary>
        public string? VaAfterAtropineOd { get; set; }
        /// <summary>
        /// Visual acuity after atropine - left eye.
        /// </summary>
        public string? VaAfterAtropineOs { get; set; }

        // ===== REFRACTION =====
        /// <summary>
        /// Refraction result before atropine.
        /// </summary>
        public string? RefractionPreAtropine { get; set; }
        /// <summary>
        /// Refraction result after atropine.
        /// </summary>
        public string? RefractionPostAtropine { get; set; }

        // ===== SOI BÓNG ĐỒNG TỬ (Pupil Shadow Test) =====
        /// <summary>
        /// Pupil shadow test result - right eye.
        /// </summary>
        public string? PupilShadowTestOd { get; set; }
        /// <summary>
        /// Pupil shadow test result - left eye.
        /// </summary>
        public string? PupilShadowTestOs { get; set; }

        // ===== VẬN NHÃN NGOẠI LAI (Extraocular Movement) =====
        /// <summary>
        /// Extraocular movement gaze test result.
        /// </summary>
        public string? EomGazeTest { get; set; }
        /// <summary>
        /// Extraocular movement increase - right eye: (+), (++), or (+++).
        /// </summary>
        public string? EomGazeIncreaseOd { get; set; }
        /// <summary>
        /// Extraocular movement increase - left eye: (+), (++), or (+++).
        /// </summary>
        public string? EomGazeIncreaseOs { get; set; }
        /// <summary>
        /// Extraocular movement limitation - right eye: (-), (--), or (---).
        /// </summary>
        public string? EomGazeLimitOd { get; set; }
        /// <summary>
        /// Extraocular movement limitation - left eye: (-), (--), or (---).
        /// </summary>
        public string? EomGazeLimitOs { get; set; }

        // ===== VẬN NHÃN NỘI TẠI (Internal EOM) =====
        /// <summary>
        /// Internal extraocular movement - right eye.
        /// </summary>
        public string? EomInternalOd { get; set; }
        /// <summary>
        /// Internal extraocular movement - left eye.
        /// </summary>
        public string? EomInternalOs { get; set; }

        // ===== CONVERGENCE POINT (Near Point of Convergence) =====
        /// <summary>
        /// Near point of convergence. Normal: 6-8cm.
        /// </summary>
        public string? ConvergencePoint { get; set; }

        // ===== COVER TEST (Thử nghiệm che mắt) =====
        /// <summary>
        /// Cover test result: Esotropia, Exotropia, or Hypertropia.
        /// </summary>
        public string? CoverTestResult { get; set; }

        // ===== HIRSCHBERG TEST & PRISM =====
        /// <summary>
        /// Hirschberg test result before atropine.
        /// </summary>
        public string? HirschbergBeforeAtropine { get; set; }
        /// <summary>
        /// Hirschberg test result after atropine.
        /// </summary>
        public string? HirschbergAfterAtropine { get; set; }
        /// <summary>
        /// Prism measurement at near.
        /// </summary>
        public string? PrismNear { get; set; }
        /// <summary>
        /// Prism measurement at distance.
        /// </summary>
        public string? PrismDistance { get; set; }
        /// <summary>
        /// Prism measurement in upgaze.
        /// </summary>
        public string? PrismUp { get; set; }
        /// <summary>
        /// Prism measurement in downgaze.
        /// </summary>
        public string? PrismDown { get; set; }

        // ===== HỘI CHỨNG (Syndrome) =====
        /// <summary>
        /// Associated syndrome if present.
        /// </summary>
        public string? StrabismusSyndrome { get; set; }

        // ===== SYOPTOPHORE TEST =====
        /// <summary>
        /// Synoptophore objective angle.
        /// </summary>
        public string? SynoptophoreObjective { get; set; }
        /// <summary>
        /// Synoptophore subjective angle.
        /// </summary>
        public string? SynoptophoreSubjective { get; set; }

        // ===== THỊ GIÁC HAI MẮT (Binocular Vision) =====
        /// <summary>
        /// Binocular vision status: Simultaneous perception, Fusion, or Stereopsis.
        /// </summary>
        public string? BinocularStatus { get; set; }
        /// <summary>
        /// Fusion amplitude.
        /// </summary>
        public string? FusionAmplitude { get; set; }
        /// <summary>
        /// Retinal correspondence: Normal or Abnormal.
        /// </summary>
        public string? RetinalCorrespondence { get; set; }
        /// <summary>
        /// Presence of diplopia (double vision).
        /// </summary>
        public string? Diplopia { get; set; }
        /// <summary>
        /// Compensatory head posture if present.
        /// </summary>
        public string? CompensatoryHeadPosture { get; set; }

        // ===== SỤP MI (Ptosis) =====
        /// <summary>
        /// Degree of ptosis - right eye: Grade 1, 2, or 3.
        /// </summary>
        public string? PtosisDegreeOd { get; set; }
        /// <summary>
        /// Degree of ptosis - left eye: Grade 1, 2, or 3.
        /// </summary>
        public string? PtosisDegreeOs { get; set; }
        /// <summary>
        /// Levator function - right eye: Good, Average, or Poor.
        /// </summary>
        public string? LevatorFunctionOd { get; set; }
        /// <summary>
        /// Levator function - left eye: Good, Average, or Poor.
        /// </summary>
        public string? LevatorFunctionOs { get; set; }
        /// <summary>
        /// Marcus Gunn phenomenon: Present or Absent.
        /// </summary>
        public string? MarcusGunn { get; set; }
        /// <summary>
        /// Bell phenomenon: Present or Absent.
        /// </summary>
        public string? BellPhenomenon { get; set; }

        // ===== FIXATION =====
        /// <summary>
        /// Fixation type - right eye: Central, Paracentral, or Eccentric.
        /// </summary>
        public string? FixationOd { get; set; }
        /// <summary>
        /// Fixation type - left eye: Central, Paracentral, or Eccentric.
        /// </summary>
        public string? FixationOs { get; set; }

        // ===== PHẢN XẠ THỂ MI (Palpebral Reflex) =====
        /// <summary>
        /// Palpebral reflex - right eye.
        /// </summary>
        public string? PalpebralReflexOd { get; set; }
        /// <summary>
        /// Palpebral reflex - left eye.
        /// </summary>
        public string? PalpebralReflexOs { get; set; }

        // ===== EPICANTHUS =====
        /// <summary>
        /// Epicanthus: Present or Absent.
        /// </summary>
        public string? Epicanthus { get; set; }

        // ===== GÓC HÃM (Hering's Law / Hemming Angle) =====
        /// <summary>
        /// Hemming angle: None or Present.
        /// </summary>
        public string? HemmingAngle { get; set; }
    }

    /// <summary>
    /// Pediatric record data for MS26 (Pediatric) medical records.
    /// </summary>
    public class PediatricRecordData
    {
        // ===== HISTORY =====
        /// <summary>
        /// Whether condition is congenital.
        /// </summary>
        public bool Congenital { get; set; } = false;
        /// <summary>
        /// Whether condition is acquired.
        /// </summary>
        public bool Acquired { get; set; } = false;
        /// <summary>
        /// Onset time for acquired condition.
        /// </summary>
        public string? AcquiredOnset { get; set; }
        /// <summary>
        /// Prior treatment received.
        /// </summary>
        public string? PriorTreatment { get; set; }

        // Pregnancy & development
        /// <summary>
        /// Whether there was pathological pregnancy illness.
        /// </summary>
        public bool PregnancyIllness { get; set; } = false;
        /// <summary>
        /// Details of pathological pregnancy illness.
        /// </summary>
        public string? PregnancyIllnessDetail { get; set; }
        /// <summary>
        /// Whether intellectual development is normal.
        /// </summary>
        public bool IntellectualDevelopmentNormal { get; set; } = true;

        // Chief symptoms
        /// <summary>
        /// Chief symptoms: Blurred vision, Pain, Redness, or Photophobia.
        /// </summary>
        public string? ChiefSymptoms { get; set; }

        // ===== EYELID CONDITIONS =====
        /// <summary>
        /// Whether entropion is present - right eye.
        /// </summary>
        public bool EntropionOd { get; set; } = false;
        /// <summary>
        /// Whether epicanthus is present - right eye.
        /// </summary>
        public bool EpicanthusOd { get; set; } = false;
        /// <summary>
        /// Whether ptosis is present - right eye.
        /// </summary>
        public bool PtosisOd { get; set; } = false;
        /// <summary>
        /// Description of eyelid tumor.
        /// </summary>
        public string? EyelidTumor { get; set; }
        /// <summary>
        /// Location of eyelid tumor.
        /// </summary>
        public string? EyelidTumorLocation { get; set; }
        /// <summary>
        /// Size of eyelid tumor.
        /// </summary>
        public string? EyelidTumorSize { get; set; }

        // ===== EYEBALL STATUS =====
        /// <summary>
        /// Eyeball status - right eye.
        /// </summary>
        public string? EyeballOdStatus { get; set; }
        /// <summary>
        /// Eyeball status - left eye.
        /// </summary>
        public string? EyeballOsStatus { get; set; }
        /// <summary>
        /// Eyeball texture: Soft, Tense, Large, Small, or Atrophic.
        /// </summary>
        public string? EyeballTexture { get; set; }

        // ===== AMBLYOPIA (Lazy Eye) =====
        /// <summary>
        /// Status of amblyopia.
        /// </summary>
        public string? AmblyopiaStatus { get; set; }
        /// <summary>
        /// Fixation preference - right eye: Central, Paracentral, or Eccentric.
        /// </summary>
        public string? FixationPreferenceOd { get; set; }
        /// <summary>
        /// Fixation preference - left eye: Central, Paracentral, or Eccentric.
        /// </summary>
        public string? FixationPreferenceOs { get; set; }

        // ===== FUNDUS SUMMARY =====
        /// <summary>
        /// Fundus examination summary - right eye.
        /// </summary>
        public string? FundusSummaryOd { get; set; }
        /// <summary>
        /// Fundus examination summary - left eye.
        /// </summary>
        public string? FundusSummaryOs { get; set; }

        // ===== DEVELOPMENTAL STATUS =====
        /// <summary>
        /// Intellectual development status.
        /// </summary>
        public string? IntellectualDevelopmentStatus { get; set; }
        /// <summary>
        /// General health status.
        /// </summary>
        public string? GeneralHealthStatus { get; set; }
    }

    #endregion

    #region Prescription Data Classes

    /// <summary>
    /// Prescription header data.
    /// </summary>
    public class PrescriptionData
    {
        /// <summary>
        /// Additional notes for the prescription.
        /// </summary>
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Prescription item data for individual medicine entries.
    /// </summary>
    public class PrescriptionItemData
    {
        /// <summary>
        /// Name of the medicine.
        /// </summary>
        public string MedicineName { get; set; } = string.Empty;
        /// <summary>
        /// Dosage amount.
        /// </summary>
        public string Dosage { get; set; } = string.Empty;
        /// <summary>
        /// Frequency of administration.
        /// </summary>
        public string? Frequency { get; set; }
        /// <summary>
        /// Duration in days.
        /// </summary>
        public int? DurationDays { get; set; }
        /// <summary>
        /// Quantity to dispense.
        /// </summary>
        public int Quantity { get; set; }
        /// <summary>
        /// Special instructions for the patient.
        /// </summary>
        public string? Instruction { get; set; }
    }

    /// <summary>
    /// Glasses prescription data including sphere, cylinder, axis, and addition values.
    /// </summary>
    public class GlassesPrescriptionData
    {
        /// <summary>
        /// Spherical power for right eye.
        /// </summary>
        public decimal? SphOd { get; set; }
        /// <summary>
        /// Cylindrical power for right eye.
        /// </summary>
        public decimal? CylOd { get; set; }
        /// <summary>
        /// Axis for right eye (1-180 degrees).
        /// </summary>
        public int? AxisOd { get; set; }
        /// <summary>
        /// Add power for right eye (for bifocals/progressives).
        /// </summary>
        public decimal? AddOd { get; set; }
        /// <summary>
        /// Spherical power for left eye.
        /// </summary>
        public decimal? SphOs { get; set; }
        /// <summary>
        /// Cylindrical power for left eye.
        /// </summary>
        public decimal? CylOs { get; set; }
        /// <summary>
        /// Axis for left eye (1-180 degrees).
        /// </summary>
        public int? AxisOs { get; set; }
        /// <summary>
        /// Add power for left eye (for bifocals/progressives).
        /// </summary>
        public decimal? AddOs { get; set; }
        /// <summary>
        /// Pupillary distance.
        /// </summary>
        public decimal? Pd { get; set; }
        /// <summary>
        /// Type of lens prescribed.
        /// </summary>
        public string? LensType { get; set; }
        /// <summary>
        /// Additional notes.
        /// </summary>
        public string? Notes { get; set; }
    }

    #endregion
}
