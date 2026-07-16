using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.Configurations;
using ECS.Domain.Entities.Feedbacks;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Entities.Notifications;
using ECS.Domain.Entities.Paraclinical;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ECS.Infrastructure.Persistence.Configurations
{
    public class EyeSideValueConverter : ValueConverter<EyeSide, string>
    {
        public EyeSideValueConverter() : base(v => v.ToString(), v => FromProvider(v)) { }

        private static EyeSide FromProvider(string value)
        {
            // Handle DB values: OD/OS/RIGHT/LEFT/BOTH
            if (string.Equals(value, "OD", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "RIGHT", StringComparison.OrdinalIgnoreCase))
                return EyeSide.RIGHT;
            if (string.Equals(value, "OS", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "LEFT", StringComparison.OrdinalIgnoreCase))
                return EyeSide.LEFT;
            if (string.Equals(value, "BOTH", StringComparison.OrdinalIgnoreCase))
                return EyeSide.BOTH;
            throw new InvalidOperationException($"Cannot convert '{value}' to EyeSide.");
        }
    }
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
    // MedicalRecord now only stores core metadata + the Cloudinary URL pointing at
    // a JSON payload that holds all form fields (Eye exam, Trauma, Glaucoma, etc.).
    public class MedicalRecordConfiguration : IEntityTypeConfiguration<MedicalRecord>
    {
        public void Configure(EntityTypeBuilder<MedicalRecord> entity)
        {
            entity.ToTable("medical_record");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.AppointmentId).IsUnique();

            entity.Property(e => e.RecordType).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<int>().HasDefaultValue(RecordStatus.DRAFT);

            // ===== METADATA =====
            entity.Property(e => e.ChiefComplaint).HasMaxLength(2000);
            entity.Property(e => e.Summary).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Notes).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IsLocked).HasDefaultValue(false);
            entity.Property(e => e.FinalizedAt).HasColumnType("datetime2");

            // ===== CLOUD STORAGE =====
            entity.Property(e => e.RecordDataUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.RecordDataPublicId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.RecordDataSchemaVersion).IsRequired().HasMaxLength(20).HasDefaultValue("1.0");
            entity.Property(e => e.RecordDataVersion).HasDefaultValue(1);
            entity.Property(e => e.RecordDataSizeBytes).HasDefaultValue(0L);
            entity.Property(e => e.RecordDataChecksum).IsRequired().HasMaxLength(64);

            entity.Property(e => e.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime2");

            // ===== RELATIONSHIPS =====
            entity.HasOne(e => e.Appointment).WithOne(a => a.MedicalRecord).HasForeignKey<MedicalRecord>(e => e.AppointmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Patient).WithMany(p => p.MedicalRecords).HasForeignKey(e => e.PatientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Doctor).WithMany(d => d.MedicalRecords).HasForeignKey(e => e.DoctorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.FinalizedByUser).WithMany().HasForeignKey(e => e.FinalizedBy).OnDelete(DeleteBehavior.SetNull);
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
    // 6. PARACLINICAL — kept separately because results come from instruments / AI
    // and need their own pipeline (not part of the form JSON).
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

            entity.HasOne(e => e.MedicalRecord).WithMany().HasForeignKey(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class VisualFieldTestConfiguration : IEntityTypeConfiguration<VisualFieldTest>
    {
        public void Configure(EntityTypeBuilder<VisualFieldTest> entity)
        {
            entity.ToTable("visual_field_test");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Side).HasConversion(new EyeSideValueConverter()).HasMaxLength(10);
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

            entity.HasOne(e => e.MedicalRecord).WithMany().HasForeignKey(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class UltrasoundEyeConfiguration : IEntityTypeConfiguration<UltrasoundEye>
    {
        public void Configure(EntityTypeBuilder<UltrasoundEye> entity)
        {
            entity.ToTable("ultrasound_eye");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Side).HasConversion(new EyeSideValueConverter()).HasMaxLength(10);
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

            entity.HasOne(e => e.MedicalRecord).WithMany().HasForeignKey(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    // ==============================
    // 7. FEEDBACK
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
    // 8. NOTIFICATIONS & CONFIG
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
