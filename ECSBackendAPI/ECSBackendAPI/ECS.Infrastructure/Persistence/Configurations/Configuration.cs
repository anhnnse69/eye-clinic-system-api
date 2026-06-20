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
using ECS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECS.Infrastructure.Persistence.Configurations
{
    // ==============================
    // 1. AUTH
    // ==============================
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> entity)
        {
            entity.ToTable("user");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasIndex(e => e.Phone).IsUnique();
            entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);

            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(200);

            entity.Property(e => e.PasswordHash).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.AvatarUrl).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime2");
        }
    }

    // ==============================
    // 2. PATIENTS
    // ==============================
    public class PatientProfileConfiguration : IEntityTypeConfiguration<PatientProfile>
    {
        public void Configure(EntityTypeBuilder<PatientProfile> entity)
        {
            entity.ToTable("patient_profile");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Gender).HasConversion<string>().HasMaxLength(10);
            entity.Property(e => e.Dob).HasColumnType("date");
            entity.Property(e => e.IdentityNumber).HasMaxLength(50);
            entity.Property(e => e.Address).HasColumnType("nvarchar(max)");
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.BhytNumber).HasMaxLength(50);
            entity.Property(e => e.BloodType).HasMaxLength(10);
            entity.Property(e => e.Allergies).HasColumnType("nvarchar(max)");
            entity.Property(e => e.MedicalHistory).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime2");

            entity.HasOne(e => e.User)
                .WithMany(u => u.PatientProfiles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    public class UserPatientConfiguration : IEntityTypeConfiguration<UserPatient>
    {
        public void Configure(EntityTypeBuilder<UserPatient> entity)
        {
            entity.ToTable("user_patient");
            entity.HasKey(e => new { e.UserId, e.PatientId });
            entity.Property(e => e.Relationship).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User).WithMany(u => u.UserPatients).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Patient).WithMany(p => p.UserPatients).HasForeignKey(e => e.PatientId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    // ==============================
    // 3. CLINICS
    // ==============================
    public class ClinicConfiguration : IEntityTypeConfiguration<Clinic>
    {
        public void Configure(EntityTypeBuilder<Clinic> entity)
        {
            entity.ToTable("clinic");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Address).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.LogoUrl).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Description).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.RatingAvg).HasPrecision(3, 2).HasDefaultValue(0m);
            entity.Property(e => e.ReviewCount).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime2");
        }
    }

    public class ClinicRegistrationRequestConfiguration : IEntityTypeConfiguration<ClinicRegistrationRequest>
    {
        public void Configure(EntityTypeBuilder<ClinicRegistrationRequest> entity)
        {
            entity.ToTable("clinic_registration_request");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ClinicName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ClinicAddress).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.ContactName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ContactPhone).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ContactEmail).IsRequired().HasMaxLength(200);
            entity.Property(e => e.BusinessLicenseUrl).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("PENDING");
            entity.Property(e => e.ReviewNote).HasColumnType("nvarchar(max)");
            entity.Property(e => e.RequestedAt).HasColumnType("datetime2").HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.ReviewedAt).HasColumnType("datetime2");

            entity.HasOne(e => e.Reviewer).WithMany(u => u.ReviewedRequests).HasForeignKey(e => e.ReviewedBy).OnDelete(DeleteBehavior.SetNull);
        }
    }

    public class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
    {
        public void Configure(EntityTypeBuilder<Specialty> entity)
        {
            entity.ToTable("specialty");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        }
    }

    public class DoctorProfileConfiguration : IEntityTypeConfiguration<DoctorProfile>
    {
        public void Configure(EntityTypeBuilder<DoctorProfile> entity)
        {
            entity.ToTable("doctor_profile");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.ExperienceYears).HasDefaultValue(0);
            entity.Property(e => e.Bio).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.RatingAvg).HasPrecision(3, 2).HasDefaultValue(0m);
            entity.Property(e => e.ReviewCount).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime2");

            entity.HasOne(e => e.User).WithMany(u => u.DoctorProfiles).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Clinic).WithMany(c => c.DoctorProfiles).HasForeignKey(e => e.ClinicId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Specialty).WithMany(s => s.DoctorProfiles).HasForeignKey(e => e.SpecialtyId).OnDelete(DeleteBehavior.SetNull);
        }
    }

    public class ServiceConfiguration : IEntityTypeConfiguration<Service>
    {
        public void Configure(EntityTypeBuilder<Service> entity)
        {
            entity.ToTable("service");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ServiceName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.DurationMinutes).HasDefaultValue(15);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(e => e.Clinic).WithMany(c => c.Services).HasForeignKey(e => e.ClinicId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class FacilityRoomConfiguration : IEntityTypeConfiguration<FacilityRoom>
    {
        public void Configure(EntityTypeBuilder<FacilityRoom> entity)
        {
            entity.ToTable("facility_room");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.RoomName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RoomType).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(e => e.Clinic).WithMany(c => c.FacilityRooms).HasForeignKey(e => e.ClinicId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class StaffClinicConfiguration : IEntityTypeConfiguration<StaffClinic>
    {
        public void Configure(EntityTypeBuilder<StaffClinic> entity)
        {
            entity.ToTable("staff_clinic");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime2");

            entity.HasOne(e => e.User).WithMany(u => u.StaffClinics).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Clinic).WithMany(c => c.StaffClinics).HasForeignKey(e => e.ClinicId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class MedicineCatalogConfiguration : IEntityTypeConfiguration<MedicineCatalog>
    {
        public void Configure(EntityTypeBuilder<MedicineCatalog> entity)
        {
            entity.ToTable("medicine_catalog");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.MedicineName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.GenericName).HasMaxLength(200);
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.DosageForm).HasMaxLength(100);
            entity.Property(e => e.Concentration).HasMaxLength(50);
            entity.Property(e => e.Manufacturer).HasMaxLength(200);
            entity.Property(e => e.Notes).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime2");

            entity.HasOne(e => e.Clinic).WithMany(c => c.MedicineCatalogs).HasForeignKey(e => e.ClinicId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    // ==============================
    // 4. SCHEDULING
    // ==============================
    public class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
    {
        public void Configure(EntityTypeBuilder<DoctorSchedule> entity)
        {
            entity.ToTable("doctor_schedule");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.WorkDate).HasColumnType("date");
            entity.Property(e => e.ShiftType).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Note).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime2");

            entity.HasOne(e => e.Doctor).WithMany(d => d.DoctorSchedules).HasForeignKey(e => e.DoctorId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class TimeSlotConfiguration : IEntityTypeConfiguration<TimeSlot>
    {
        public void Configure(EntityTypeBuilder<TimeSlot> entity)
        {
            entity.ToTable("time_slot");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.StartTime).HasColumnType("datetime2");
            entity.Property(e => e.EndTime).HasColumnType("datetime2");
            entity.Property(e => e.MaxPatients).HasDefaultValue(1);
            entity.Property(e => e.CurrentPatients).HasDefaultValue(0);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(SlotStatus.AVAILABLE);

            entity.HasOne(e => e.Schedule).WithMany(s => s.TimeSlots).HasForeignKey(e => e.ScheduleId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
    {
        public void Configure(EntityTypeBuilder<Appointment> entity)
        {
            entity.ToTable("appointment");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.AppointmentDate).HasColumnType("datetime2");
            entity.Property(e => e.Symptoms).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(AppointmentStatus.PENDING);
            entity.Property(e => e.DepositAmount).HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(e => e.DepositPaid).HasDefaultValue(false);
            entity.Property(e => e.BookingSource).HasMaxLength(20).HasDefaultValue("ONLINE");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime2");

            entity.HasOne(e => e.Patient).WithMany(p => p.Appointments).HasForeignKey(e => e.PatientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Doctor).WithMany(d => d.Appointments).HasForeignKey(e => e.DoctorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Slot).WithMany(s => s.Appointments).HasForeignKey(e => e.SlotId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Service).WithMany(s => s.Appointments).HasForeignKey(e => e.ServiceId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.CreatedBy).WithMany(u => u.AppointmentsCreated).HasForeignKey(e => e.CreatedById).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.FollowUpFrom).WithMany(a => a.FollowUpAppointments).HasForeignKey(e => e.FollowUpFromAppointmentId).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class QueueConfiguration : IEntityTypeConfiguration<Queue>
    {
        public void Configure(EntityTypeBuilder<Queue> entity)
        {
            entity.ToTable("queue");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.QueueNumber);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(QueueStatus.WAITING);
            entity.Property(e => e.CalledAt).HasColumnType("datetime2");
            entity.Property(e => e.CompletedAt).HasColumnType("datetime2");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => e.AppointmentId).IsUnique();
            entity.HasOne(e => e.Appointment).WithOne(a => a.Queue).HasForeignKey<Queue>(e => e.AppointmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Clinic).WithMany(c => c.Queues).HasForeignKey(e => e.ClinicId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Room).WithMany(r => r.Queues).HasForeignKey(e => e.RoomId).OnDelete(DeleteBehavior.SetNull);
        }
    }

    // ==============================
    // 5. MEDICAL RECORDS
    // ==============================
    public class MedicalRecordConfiguration : IEntityTypeConfiguration<MedicalRecord>
    {
        public void Configure(EntityTypeBuilder<MedicalRecord> entity)
        {
            entity.ToTable("medical_record");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.AppointmentId).IsUnique();

            entity.Property(e => e.RecordType).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.ChiefComplaint).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IllnessDayNumber);
            entity.Property(e => e.MedicalHistory).HasColumnType("nvarchar(max)");
            entity.Property(e => e.PersonalHistoryEye).HasColumnType("nvarchar(max)");
            entity.Property(e => e.PersonalHistorySystemic).HasColumnType("nvarchar(max)");
            entity.Property(e => e.FamilyHistory).HasColumnType("nvarchar(max)");
            entity.Property(e => e.VitalPulse);
            entity.Property(e => e.VitalTemperature).HasPrecision(5, 2);
            entity.Property(e => e.VitalBloodPressure).HasMaxLength(20);
            entity.Property(e => e.VitalRespiratoryRate);
            entity.Property(e => e.VitalWeightKg).HasPrecision(6, 2);
            entity.Property(e => e.SystemicExam).HasColumnType("nvarchar(max)"); // JSONB
            entity.Property(e => e.DiagnosisMain).HasColumnType("nvarchar(max)");
            entity.Property(e => e.DiagnosisComorbid).HasColumnType("nvarchar(max)");
            entity.Property(e => e.DiagnosisDifferential).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Prognosis).HasColumnType("nvarchar(max)");
            entity.Property(e => e.TreatmentPlan).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Notes).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IsLocked).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime2");

            entity.HasOne(e => e.Appointment).WithOne(a => a.MedicalRecord).HasForeignKey<MedicalRecord>(e => e.AppointmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Patient).WithMany(p => p.MedicalRecords).HasForeignKey(e => e.PatientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Doctor).WithMany(d => d.MedicalRecords).HasForeignKey(e => e.DoctorId).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class MedicalRecordExtrasConfiguration : IEntityTypeConfiguration<MedicalRecordExtras>
    {
        public void Configure(EntityTypeBuilder<MedicalRecordExtras> entity)
        {
            entity.ToTable("medical_record_extras");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.RecordId).IsUnique();

            entity.Property(e => e.TraumaSummary).HasColumnType("nvarchar(max)"); // JSONB
            entity.Property(e => e.GlaucomaSummary).HasColumnType("nvarchar(max)"); // JSONB
            entity.Property(e => e.PediatricSummary).HasColumnType("nvarchar(max)"); // JSONB
            entity.Property(e => e.LabOrders).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ImagingOrders).HasColumnType("nvarchar(max)");
            entity.Property(e => e.DischargeSummary).HasColumnType("nvarchar(max)");
            entity.Property(e => e.TreatmentProcess).HasColumnType("nvarchar(max)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime2");

            entity.HasOne(e => e.MedicalRecord).WithOne(m => m.Extras).HasForeignKey<MedicalRecordExtras>(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.UpdatedByUser).WithMany().HasForeignKey(e => e.UpdatedBy).OnDelete(DeleteBehavior.SetNull);
        }
    }

    public class DocumentAccessPermissionConfiguration : IEntityTypeConfiguration<DocumentAccessPermission>
    {
        public void Configure(EntityTypeBuilder<DocumentAccessPermission> entity)
        {
            entity.ToTable("document_access_permission");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ExpiresAt).HasColumnType("datetime2");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime2");

            entity.HasOne(e => e.MedicalRecord).WithMany(m => m.DocumentAccessPermissions).HasForeignKey(e => e.MedicalRecordId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.GrantedToUser).WithMany(u => u.GrantedPermissions).HasForeignKey(e => e.GrantedToUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.GrantedByUser).WithMany(u => u.GrantedByPermissions).HasForeignKey(e => e.GrantedByUserId).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class EmrExportLogConfiguration : IEntityTypeConfiguration<EmrExportLog>
    {
        public void Configure(EntityTypeBuilder<EmrExportLog> entity)
        {
            entity.ToTable("emr_export_log");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ExportFormat).HasMaxLength(20);
            entity.Property(e => e.FileUrl).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.MedicalRecord).WithMany(m => m.EmrExportLogs).HasForeignKey(e => e.MedicalRecordId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Exporter).WithMany(u => u.EmrExportLogs).HasForeignKey(e => e.ExportedBy).OnDelete(DeleteBehavior.Restrict);
        }
    }

    // ==============================
    // 6. EYE EXAMINATIONS (8 tables)
    // ==============================
    public class EyeExamBasicConfiguration : IEntityTypeConfiguration<EyeExamBasic>
    {
        public void Configure(EntityTypeBuilder<EyeExamBasic> entity)
        {
            entity.ToTable("eye_exam_basic");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Side).HasConversion<string>().HasMaxLength(10);

            entity.Property(e => e.VaUncorrected).HasPrecision(5, 2);
            entity.Property(e => e.VaCorrected).HasPrecision(5, 2);
            entity.Property(e => e.VaNear).HasPrecision(5, 2);
            entity.Property(e => e.VaPinhole).HasPrecision(5, 2);
            entity.Property(e => e.IopMmhg).HasPrecision(5, 2);
            entity.Property(e => e.IopMethod).HasMaxLength(50);
            entity.Property(e => e.RefractionSph).HasPrecision(6, 2);
            entity.Property(e => e.RefractionCyl).HasPrecision(6, 2);
            entity.Property(e => e.RefractionAdd).HasPrecision(4, 2);
            entity.Property(e => e.Pd).HasPrecision(5, 1);
            entity.Property(e => e.ProptosisMm).HasPrecision(5, 2);
            entity.Property(e => e.PreAtropine).HasDefaultValue(false);
            entity.Property(e => e.PostAtropine).HasDefaultValue(false);
            entity.Property(e => e.EomNormal).HasDefaultValue(true);
            entity.Property(e => e.Nystagmus).HasDefaultValue(false);
            entity.Property(e => e.OrbitNormal).HasDefaultValue(true);

            entity.HasOne(e => e.MedicalRecord).WithMany(m => m.EyeExamBasics).HasForeignKey(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class EyeEyelidConjunctivaConfiguration : IEntityTypeConfiguration<EyeEyelidConjunctiva>
    {
        public void Configure(EntityTypeBuilder<EyeEyelidConjunctiva> entity)
        {
            entity.ToTable("eye_eyelid_conjunctiva");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Side).HasConversion<string>().HasMaxLength(10);

            entity.Property(e => e.EyelidOther).HasColumnType("nvarchar(max)"); // JSONB
            entity.Property(e => e.ConjunctivaOther).HasColumnType("nvarchar(max)"); // JSONB
            entity.Property(e => e.ConjunctivaCongestionType).HasMaxLength(50);
            entity.Property(e => e.ConjunctivaDischarge).HasMaxLength(100);

            entity.HasOne(e => e.MedicalRecord).WithMany(m => m.EyeEyelidConjunctivae).HasForeignKey(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class EyeCorneaConfiguration : IEntityTypeConfiguration<EyeCornea>
    {
        public void Configure(EntityTypeBuilder<EyeCornea> entity)
        {
            entity.ToTable("eye_cornea");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Side).HasConversion<string>().HasMaxLength(10);

            entity.Property(e => e.DiameterMm).HasPrecision(5, 2);
            entity.Property(e => e.PerforationDiameterMm).HasPrecision(5, 2);
            entity.Property(e => e.CornealThickness).HasPrecision(5, 2);
            entity.Property(e => e.CorneaExtras).HasColumnType("nvarchar(max)"); // JSONB

            entity.HasOne(e => e.MedicalRecord).WithMany(m => m.EyeCorneas).HasForeignKey(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class EyeAcIrisConfiguration : IEntityTypeConfiguration<EyeAcIris>
    {
        public void Configure(EntityTypeBuilder<EyeAcIris> entity)
        {
            entity.ToTable("eye_ac_iris");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Side).HasConversion<string>().HasMaxLength(10);

            entity.Property(e => e.AcDepthMm).HasPrecision(5, 2);
            entity.Property(e => e.AcDepthHerick).HasMaxLength(20);
            entity.Property(e => e.AcPusMm).HasPrecision(5, 2);
            entity.Property(e => e.PupilDiameterMm).HasPrecision(5, 2);
            entity.Property(e => e.AcIrisExtras).HasColumnType("nvarchar(max)"); // JSONB

            entity.HasOne(e => e.MedicalRecord).WithMany(m => m.EyeAcIrises).HasForeignKey(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class EyeLensVitreousConfiguration : IEntityTypeConfiguration<EyeLensVitreous>
    {
        public void Configure(EntityTypeBuilder<EyeLensVitreous> entity)
        {
            entity.ToTable("eye_lens_vitreous");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Side).HasConversion<string>().HasMaxLength(10);

            entity.Property(e => e.LensVitreousExtras).HasColumnType("nvarchar(max)"); // JSONB

            entity.HasOne(e => e.MedicalRecord).WithMany(m => m.EyeLensVitreouses).HasForeignKey(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class EyeScleraConfiguration : IEntityTypeConfiguration<EyeSclera>
    {
        public void Configure(EntityTypeBuilder<EyeSclera> entity)
        {
            entity.ToTable("eye_sclera");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Side).HasConversion<string>().HasMaxLength(10);

            entity.Property(e => e.ScleraNormal).HasDefaultValue(true);
            entity.Property(e => e.ScleraExtras).HasColumnType("nvarchar(max)"); // JSONB

            entity.HasOne(e => e.MedicalRecord).WithMany(m => m.EyeScleras).HasForeignKey(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class EyeFundusDiscMaculaConfiguration : IEntityTypeConfiguration<EyeFundusDiscMacula>
    {
        public void Configure(EntityTypeBuilder<EyeFundusDiscMacula> entity)
        {
            entity.ToTable("eye_fundus_disc_macula");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Side).HasConversion<string>().HasMaxLength(10);

            entity.Property(e => e.OpticDiscNormal).HasDefaultValue(true);
            entity.Property(e => e.MaculaNormal).HasDefaultValue(true);
            entity.Property(e => e.DiscMaculaExtras).HasColumnType("nvarchar(max)"); // JSONB

            entity.HasOne(e => e.MedicalRecord).WithMany(m => m.EyeFundusDiscMaculas).HasForeignKey(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class EyeFundusRetinaVesselConfiguration : IEntityTypeConfiguration<EyeFundusRetinaVessel>
    {
        public void Configure(EntityTypeBuilder<EyeFundusRetinaVessel> entity)
        {
            entity.ToTable("eye_fundus_retina_vessel");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Side).HasConversion<string>().HasMaxLength(10);

            entity.Property(e => e.VesselNormal).HasDefaultValue(true);
            entity.Property(e => e.RetinaVesselExtras).HasColumnType("nvarchar(max)"); // JSONB

            entity.HasOne(e => e.MedicalRecord).WithMany(m => m.EyeFundusRetinaVessels).HasForeignKey(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class LacrimalRecordConfiguration : IEntityTypeConfiguration<LacrimalRecord>
    {
        public void Configure(EntityTypeBuilder<LacrimalRecord> entity)
        {
            entity.ToTable("lacrimal_record");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Side).HasConversion<string>().HasMaxLength(10);

            entity.Property(e => e.IrrigationFree).HasDefaultValue(true);

            entity.HasOne(e => e.MedicalRecord).WithMany(m => m.LacrimalRecords).HasForeignKey(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    // ==============================
    // 7. PARACLINICAL
    // ==============================
    public class OctResultConfiguration : IEntityTypeConfiguration<OctResult>
    {
        public void Configure(EntityTypeBuilder<OctResult> entity)
        {
            entity.ToTable("oct_result");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.MachineName).HasMaxLength(100);
            entity.Property(e => e.ScanPattern).HasMaxLength(100);
            entity.Property(e => e.RnflAverageOd).HasPrecision(6, 2);
            entity.Property(e => e.RnflAverageOs).HasPrecision(6, 2);
            entity.Property(e => e.CmtOd).HasPrecision(6, 2);
            entity.Property(e => e.CmtOs).HasPrecision(6, 2);
            entity.Property(e => e.CupDiscRatioOd).HasPrecision(4, 2);
            entity.Property(e => e.CupDiscRatioOs).HasPrecision(4, 2);
            entity.Property(e => e.Conclusion).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ImageUrl).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ExamDate).HasColumnType("datetime2");
            entity.Property(e => e.TechnicianName).HasMaxLength(200);

            entity.HasOne(e => e.MedicalRecord).WithMany(m => m.OctResults).HasForeignKey(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class VisualFieldTestConfiguration : IEntityTypeConfiguration<VisualFieldTest>
    {
        public void Configure(EntityTypeBuilder<VisualFieldTest> entity)
        {
            entity.ToTable("visual_field_test");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Side).HasConversion<string>().HasMaxLength(10);
            entity.Property(e => e.Machine).HasMaxLength(100);
            entity.Property(e => e.Strategy).HasMaxLength(100);
            entity.Property(e => e.MdValue).HasPrecision(6, 2);
            entity.Property(e => e.PsdValue).HasPrecision(6, 2);
            entity.Property(e => e.VfiPercent).HasPrecision(5, 2);
            entity.Property(e => e.Reliable).HasDefaultValue(false);
            entity.Property(e => e.ResultSummary).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ImageUrl).HasColumnType("nvarchar(max)");
            entity.Property(e => e.TestDate).HasColumnType("datetime2");
            entity.Property(e => e.TechnicianName).HasMaxLength(200);

            entity.HasOne(e => e.MedicalRecord).WithMany(m => m.VisualFieldTests).HasForeignKey(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class UltrasoundEyeConfiguration : IEntityTypeConfiguration<UltrasoundEye>
    {
        public void Configure(EntityTypeBuilder<UltrasoundEye> entity)
        {
            entity.ToTable("ultrasound_eye");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Side).HasConversion<string>().HasMaxLength(10);
            entity.Property(e => e.UltrasoundType).HasMaxLength(50);
            entity.Property(e => e.AxialLengthMm).HasPrecision(6, 2);
            entity.Property(e => e.AcDepthMm).HasPrecision(5, 2);
            entity.Property(e => e.LensThicknessMm).HasPrecision(5, 2);
            entity.Property(e => e.VitreousLengthMm).HasPrecision(6, 2);
            entity.Property(e => e.LensStatus).HasMaxLength(100);
            entity.Property(e => e.RetinaStatus).HasMaxLength(100);
            entity.Property(e => e.Conclusion).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ImageUrl).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ExamDate).HasColumnType("datetime2");
            entity.Property(e => e.TechnicianName).HasMaxLength(200);

            entity.HasOne(e => e.MedicalRecord).WithMany(m => m.UltrasoundEyes).HasForeignKey(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    // ==============================
    // 8. SUBSPECIALTY RECORDS
    // ==============================
    public class TraumaRecordConfiguration : IEntityTypeConfiguration<TraumaRecord>
    {
        public void Configure(EntityTypeBuilder<TraumaRecord> entity)
        {
            entity.ToTable("trauma_record");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.InjuryCause).HasColumnType("nvarchar(max)");
            entity.Property(e => e.InjuryTime).HasColumnType("datetime2");
            entity.Property(e => e.PriorTreatment).HasColumnType("nvarchar(max)");
            entity.Property(e => e.PostTreatmentCourse).HasColumnType("nvarchar(max)");
            entity.Property(e => e.OdInjuries).HasColumnType("nvarchar(max)"); // JSONB
            entity.Property(e => e.OsInjuries).HasColumnType("nvarchar(max)"); // JSONB
            entity.Property(e => e.InjuryDetails).HasColumnType("nvarchar(max)"); // JSONB
            entity.Property(e => e.TraumaConclusion).HasColumnType("nvarchar(max)");

            entity.HasOne(e => e.MedicalRecord).WithOne(m => m.TraumaRecord).HasForeignKey<TraumaRecord>(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class GlaucomaRecordConfiguration : IEntityTypeConfiguration<GlaucomaRecord>
    {
        public void Configure(EntityTypeBuilder<GlaucomaRecord> entity)
        {
            entity.ToTable("glaucoma_record");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            // Symptoms fields
            entity.Property(e => e.EyePainLevel).HasMaxLength(50);
            entity.Property(e => e.VisionSymptoms).HasMaxLength(100);
            entity.Property(e => e.VisionProgression).HasMaxLength(100);
            entity.Property(e => e.SystemicSymptoms).HasMaxLength(200);

            // Visual acuity & IOP
            entity.Property(e => e.VaWithoutCorrectionOd).HasPrecision(6, 2);
            entity.Property(e => e.VaWithoutCorrectionOs).HasPrecision(6, 2);
            entity.Property(e => e.VaWithCorrectionOd).HasPrecision(6, 2);
            entity.Property(e => e.VaWithCorrectionOs).HasPrecision(6, 2);
            entity.Property(e => e.IopOd).HasPrecision(5, 2);
            entity.Property(e => e.IopOs).HasPrecision(5, 2);
            entity.Property(e => e.IopMethod).HasMaxLength(50);
            entity.Property(e => e.IopTargetOd).HasPrecision(5, 2);
            entity.Property(e => e.IopTargetOs).HasPrecision(5, 2);

            // History
            entity.Property(e => e.HistoryEye).HasMaxLength(100);
            entity.Property(e => e.HistoryEyeSurgery).HasMaxLength(200);
            entity.Property(e => e.PriorEyeSurgeryDetails).HasMaxLength(500);
            entity.Property(e => e.SteroidUse).HasMaxLength(200);
            entity.Property(e => e.SteroidPrescribed).HasMaxLength(200);
            entity.Property(e => e.GlaucomaMedications).HasColumnType("nvarchar(max)");
            entity.Property(e => e.MedicationChangeReason).HasMaxLength(200);
            entity.Property(e => e.OtherMedications).HasColumnType("nvarchar(max)");
            entity.Property(e => e.TreatmentProgress).HasColumnType("nvarchar(max)");

            // Family history
            entity.Property(e => e.FamilyGlaucomaRelation).HasMaxLength(100);

            // Classification
            entity.Property(e => e.GlaucomaType).HasMaxLength(100);
            entity.Property(e => e.StageOd).HasMaxLength(50);
            entity.Property(e => e.StageOs).HasMaxLength(50);

            // Examination
            entity.Property(e => e.AcDepthSmith).HasMaxLength(50);
            entity.Property(e => e.AcDepthHerick).HasMaxLength(50);
            entity.Property(e => e.GonioscopyOd).HasMaxLength(100);
            entity.Property(e => e.GonioscopyOs).HasMaxLength(100);
            entity.Property(e => e.AngleFindings).HasMaxLength(200);
            entity.Property(e => e.BlebLocation).HasMaxLength(100);
            entity.Property(e => e.BlebStatus).HasMaxLength(50);
            entity.Property(e => e.CornealTransparency).HasMaxLength(50);
            entity.Property(e => e.CornealThickness).HasPrecision(5, 2);
            entity.Property(e => e.IrisColor).HasMaxLength(50);
            entity.Property(e => e.IrisCondition).HasMaxLength(50);
            entity.Property(e => e.PupilDiameter).HasMaxLength(20);
            entity.Property(e => e.PupilPigmentBorder).HasMaxLength(50);
            entity.Property(e => e.PupilReflexResponse).HasMaxLength(50);
            entity.Property(e => e.LensStatus).HasMaxLength(50);
            entity.Property(e => e.FundusRetinaFindings).HasColumnType("nvarchar(max)");
            entity.Property(e => e.FundusMaculaFindings).HasColumnType("nvarchar(max)");
            entity.Property(e => e.OpticDiscDescription).HasColumnType("nvarchar(max)");
            entity.Property(e => e.NerveRimOd).HasMaxLength(100);
            entity.Property(e => e.NerveRimOs).HasMaxLength(100);
            entity.Property(e => e.OpticDiscCupRatio).HasMaxLength(20);
            entity.Property(e => e.OpticDiscVesselChange).HasMaxLength(100);
            entity.Property(e => e.EyeAxialLength).HasMaxLength(50);

            // Treatment plan
            entity.Property(e => e.TreatmentPlanSurgery).HasColumnType("nvarchar(max)");
            entity.Property(e => e.TreatmentPlanLaser).HasColumnType("nvarchar(max)");
            entity.Property(e => e.TreatmentPlanMedication).HasColumnType("nvarchar(max)");
            entity.Property(e => e.FollowUpPlan).HasColumnType("nvarchar(max)");

            entity.HasOne(e => e.MedicalRecord).WithOne(m => m.GlaucomaRecord).HasForeignKey<GlaucomaRecord>(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class GlaucomaHistoryConfiguration : IEntityTypeConfiguration<GlaucomaHistory>
    {
        public void Configure(EntityTypeBuilder<GlaucomaHistory> entity)
        {
            entity.ToTable("glaucoma_history");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.HistoryType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Side).HasConversion<string>().HasMaxLength(10);
            entity.Property(e => e.ProcedureType).HasMaxLength(100);
            entity.Property(e => e.ProcedureDate).HasColumnType("date");
            entity.Property(e => e.FacilityLevel).HasMaxLength(100);
            entity.Property(e => e.DrugName).HasMaxLength(200);
            entity.Property(e => e.Dosage).HasMaxLength(100);
            entity.Property(e => e.Duration).HasMaxLength(100);
            entity.Property(e => e.Route).HasMaxLength(50);
            entity.Property(e => e.ChangeReason).HasColumnType("nvarchar(max)");

            entity.HasOne(e => e.GlaucomaRecord).WithMany(g => g.Histories).HasForeignKey(e => e.GlaucomaRecordId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class StrabismusPtosisRecordConfiguration : IEntityTypeConfiguration<StrabismusPtosisRecord>
    {
        public void Configure(EntityTypeBuilder<StrabismusPtosisRecord> entity)
        {
            entity.ToTable("strabismus_ptosis_record");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            // Chief complaint
            entity.Property(e => e.StrabismusType).HasMaxLength(100);
            entity.Property(e => e.NystagmusType).HasMaxLength(100);

            // Treatment history
            entity.Property(e => e.PriorAmblyopiaTreatment).HasColumnType("nvarchar(max)");
            entity.Property(e => e.PriorAmblyopiaResult).HasMaxLength(50);
            entity.Property(e => e.PriorSurgery).HasColumnType("nvarchar(max)");
            entity.Property(e => e.PriorSurgeryResult).HasMaxLength(100);

            // Visual acuity
            entity.Property(e => e.VaBeforeAtropineOd).HasMaxLength(50);
            entity.Property(e => e.VaBeforeAtropineOs).HasMaxLength(50);
            entity.Property(e => e.VaAfterAtropineOd).HasMaxLength(50);
            entity.Property(e => e.VaAfterAtropineOs).HasMaxLength(50);

            // Refraction
            entity.Property(e => e.RefractionPreAtropine).HasMaxLength(100);
            entity.Property(e => e.RefractionPostAtropine).HasMaxLength(100);

            // Tests
            entity.Property(e => e.PupilShadowTestOd).HasMaxLength(100);
            entity.Property(e => e.PupilShadowTestOs).HasMaxLength(100);
            entity.Property(e => e.EomGazeTest).HasMaxLength(100);
            entity.Property(e => e.EomInternalOd).HasMaxLength(50);
            entity.Property(e => e.EomInternalOs).HasMaxLength(50);
            entity.Property(e => e.ConvergencePoint).HasMaxLength(100);
            entity.Property(e => e.CoverTestResult).HasMaxLength(200);
            entity.Property(e => e.HirschbergBeforeAtropine).HasMaxLength(100);
            entity.Property(e => e.HirschbergAfterAtropine).HasMaxLength(100);
            entity.Property(e => e.PrismNear).HasMaxLength(50);
            entity.Property(e => e.PrismDistance).HasMaxLength(50);
            entity.Property(e => e.PrismUp).HasMaxLength(50);
            entity.Property(e => e.PrismDown).HasMaxLength(50);
            entity.Property(e => e.StrabismusSyndrome).HasColumnType("nvarchar(max)");
            entity.Property(e => e.SynoptophoreObjective).HasMaxLength(100);
            entity.Property(e => e.SynoptophoreSubjective).HasMaxLength(100);

            // Binocular vision
            entity.Property(e => e.BinocularStatus).HasMaxLength(50);
            entity.Property(e => e.FusionAmplitude).HasMaxLength(100);
            entity.Property(e => e.RetinalCorrespondence).HasMaxLength(100);
            entity.Property(e => e.Diplopia).HasMaxLength(100);
            entity.Property(e => e.CompensatoryHeadPosture).HasColumnType("nvarchar(max)");

            // Ptosis measurements
            entity.Property(e => e.PtosisDegreeOd).HasMaxLength(50);
            entity.Property(e => e.PtosisDegreeOs).HasMaxLength(50);
            entity.Property(e => e.LevatorFunctionOd).HasMaxLength(50);
            entity.Property(e => e.LevatorFunctionOs).HasMaxLength(50);
            entity.Property(e => e.MarcusGunn).HasMaxLength(50);
            entity.Property(e => e.BellPhenomenon).HasMaxLength(50);
            entity.Property(e => e.FixationOd).HasMaxLength(50);
            entity.Property(e => e.FixationOs).HasMaxLength(50);
            entity.Property(e => e.PalpebralReflexOd).HasMaxLength(50);
            entity.Property(e => e.PalpebralReflexOs).HasMaxLength(50);

            // Defaults
            entity.Property(e => e.Nystagmus).HasDefaultValue(false);
            entity.Property(e => e.Congenital).HasDefaultValue(false);
            entity.Property(e => e.Acquired).HasDefaultValue(false);
            entity.Property(e => e.ChiefStrabismus).HasDefaultValue(false);
            entity.Property(e => e.ChiefPtosis).HasDefaultValue(false);

            entity.HasOne(e => e.MedicalRecord).WithOne(m => m.StrabismusPtosisRecord).HasForeignKey<StrabismusPtosisRecord>(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class PediatricEyeRecordConfiguration : IEntityTypeConfiguration<PediatricEyeRecord>
    {
        public void Configure(EntityTypeBuilder<PediatricEyeRecord> entity)
        {
            entity.ToTable("pediatric_eye_record");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.ChiefSymptoms).HasColumnType("nvarchar(max)"); // JSONB
            entity.Property(e => e.IntellectualDevelopmentNormal).HasDefaultValue(true);
            entity.Property(e => e.PregnancyIllness).HasDefaultValue(false);

            entity.HasOne(e => e.MedicalRecord).WithOne(m => m.PediatricRecord).HasForeignKey<PediatricEyeRecord>(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    // ==============================
    // 9. PRESCRIPTIONS
    // ==============================
    public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
    {
        public void Configure(EntityTypeBuilder<Prescription> entity)
        {
            entity.ToTable("prescription");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Notes).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.MedicalRecord).WithMany(m => m.Prescriptions).HasForeignKey(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Doctor).WithMany(d => d.Prescriptions).HasForeignKey(e => e.DoctorId).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class PrescriptionItemConfiguration : IEntityTypeConfiguration<PrescriptionItem>
    {
        public void Configure(EntityTypeBuilder<PrescriptionItem> entity)
        {
            entity.ToTable("prescription_item");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.MedicineName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Dosage).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Frequency).HasMaxLength(100);
            entity.Property(e => e.Instruction).HasColumnType("nvarchar(max)");

            entity.HasOne(e => e.Prescription).WithMany(p => p.Items).HasForeignKey(e => e.PrescriptionId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class GlassesPrescriptionConfiguration : IEntityTypeConfiguration<GlassesPrescription>
    {
        public void Configure(EntityTypeBuilder<GlassesPrescription> entity)
        {
            entity.ToTable("glasses_prescription");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.SphOd).HasPrecision(6, 2);
            entity.Property(e => e.CylOd).HasPrecision(6, 2);
            entity.Property(e => e.AddOd).HasPrecision(6, 2);
            entity.Property(e => e.SphOs).HasPrecision(6, 2);
            entity.Property(e => e.CylOs).HasPrecision(6, 2);
            entity.Property(e => e.AddOs).HasPrecision(6, 2);
            entity.Property(e => e.Pd).HasPrecision(5, 1);
            entity.Property(e => e.LensType).HasMaxLength(50);
            entity.Property(e => e.Notes).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.MedicalRecord).WithMany(m => m.GlassesPrescriptions).HasForeignKey(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Doctor).WithMany(d => d.GlassesPrescriptions).HasForeignKey(e => e.DoctorId).OnDelete(DeleteBehavior.Restrict);
        }
    }

    // ==============================
    // 10. FEEDBACK
    // ==============================
    public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
    {
        public void Configure(EntityTypeBuilder<Feedback> entity)
        {
            entity.ToTable("feedback");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.AppointmentId).IsUnique();
            entity.Property(e => e.RatingDoctor);
            entity.Property(e => e.RatingClinic);
            entity.Property(e => e.Comment).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IsPublic).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.Appointment).WithOne(a => a.Feedback).HasForeignKey<Feedback>(e => e.AppointmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Patient).WithMany(p => p.Feedbacks).HasForeignKey(e => e.PatientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Doctor).WithMany(d => d.Feedbacks).HasForeignKey(e => e.DoctorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Clinic).WithMany(c => c.Feedbacks).HasForeignKey(e => e.ClinicId).OnDelete(DeleteBehavior.Restrict);
        }
    }

    // ==============================
    // 11. NOTIFICATIONS & CONFIG
    // ==============================
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> entity)
        {
            entity.ToTable("notification");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Content).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.SentAt).HasColumnType("datetime2").HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User).WithMany(u => u.Notifications).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class PlatformConfigConfiguration : IEntityTypeConfiguration<PlatformConfig>
    {
        public void Configure(EntityTypeBuilder<PlatformConfig> entity)
        {
            entity.ToTable("platform_config");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.ConfigKey).IsUnique();
            entity.Property(e => e.ConfigKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ConfigValue).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.Description).HasColumnType("nvarchar(max)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime2");
        }
    }

    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> entity)
        {
            entity.ToTable("audit_log");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TableName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RecordId).HasMaxLength(450);
            entity.Property(e => e.OldValue).HasColumnType("nvarchar(max)");
            entity.Property(e => e.NewValue).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User).WithMany(u => u.AuditLogs).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
        }
    }
}
