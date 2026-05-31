using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Configurations;
using ECS.Domain.Entities.EyeExaminations;
using ECS.Domain.Entities.Feedbacks;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Notifications;
using ECS.Domain.Entities.Paraclinical;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Prescriptions;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Entities.SubspecialtyRecords;
using Microsoft.EntityFrameworkCore;

namespace ECS.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Auth
        public DbSet<User> Users => Set<User>();

        // Patients
        public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();
        public DbSet<UserPatient> UserPatients => Set<UserPatient>();

        // Clinics
        public DbSet<Clinic> Clinics => Set<Clinic>();
        public DbSet<ClinicRegistrationRequest> ClinicRegistrationRequests => Set<ClinicRegistrationRequest>();
        public DbSet<Specialty> Specialties => Set<Specialty>();
        public DbSet<DoctorProfile> DoctorProfiles => Set<DoctorProfile>();
        public DbSet<Service> Services => Set<Service>();
        public DbSet<FacilityRoom> FacilityRooms => Set<FacilityRoom>();
        public DbSet<StaffClinic> StaffClinics => Set<StaffClinic>();
        public DbSet<MedicineCatalog> MedicineCatalogs => Set<MedicineCatalog>();

        // Scheduling
        public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
        public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<Queue> Queues => Set<Queue>();

        // Medical Records
        public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
        public DbSet<DocumentAccessPermission> DocumentAccessPermissions => Set<DocumentAccessPermission>();
        public DbSet<EmrExportLog> EmrExportLogs => Set<EmrExportLog>();

        // Eye Examinations
        public DbSet<EyeExamination> EyeExaminations => Set<EyeExamination>();
        public DbSet<RefractionRecord> RefractionRecords => Set<RefractionRecord>();
        public DbSet<LacrimalSystem> LacrimalSystems => Set<LacrimalSystem>();
        public DbSet<AnteriorSegment> AnteriorSegments => Set<AnteriorSegment>();
        public DbSet<PosteriorSegment> PosteriorSegments => Set<PosteriorSegment>();

        // Paraclinical
        public DbSet<OctResult> OctResults => Set<OctResult>();
        public DbSet<VisualFieldTest> VisualFieldTests => Set<VisualFieldTest>();
        public DbSet<UltrasoundEye> UltrasoundEyes => Set<UltrasoundEye>();

        // Subspecialty Records
        public DbSet<TraumaRecord> TraumaRecords => Set<TraumaRecord>();
        public DbSet<GlaucomaRecord> GlaucomaRecords => Set<GlaucomaRecord>();
        public DbSet<GlaucomaSurgeryHistory> GlaucomaSurgeryHistories => Set<GlaucomaSurgeryHistory>();
        public DbSet<GlaucomaDrugHistory> GlaucomaDrugHistories => Set<GlaucomaDrugHistory>();
        public DbSet<StrabismusPtosisRecord> StrabismusPtosisRecords => Set<StrabismusPtosisRecord>();
        public DbSet<PediatricEyeRecord> PediatricEyeRecords => Set<PediatricEyeRecord>();

        // Prescriptions
        public DbSet<Prescription> Prescriptions => Set<Prescription>();
        public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
        public DbSet<GlassesPrescription> GlassesPrescriptions => Set<GlassesPrescription>();

        // Feedback
        public DbSet<Feedback> Feedbacks => Set<Feedback>();

        // Notifications & Config
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<PlatformConfig> PlatformConfigs => Set<PlatformConfig>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}