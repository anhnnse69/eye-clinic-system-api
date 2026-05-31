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

            entity.Property(e => e.UserId);
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

            entity.Property(e => e.RecordType).HasMaxLength(50);
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
            entity.Property(e => e.SystemicEndocrineNormal).HasDefaultValue(true);
            entity.Property(e => e.SystemicNeuroNormal).HasDefaultValue(true);
            entity.Property(e => e.SystemicCardioNormal).HasDefaultValue(true);
            entity.Property(e => e.SystemicRespiratoryNormal).HasDefaultValue(true);
            entity.Property(e => e.SystemicDigestiveNormal).HasDefaultValue(true);
            entity.Property(e => e.SystemicMusculoNormal).HasDefaultValue(true);
            entity.Property(e => e.SystemicUrogenitalNormal).HasDefaultValue(true);
            entity.Property(e => e.Summary).HasColumnType("nvarchar(max)");
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
    // 6. EYE EXAMINATIONS (chỉ ánh xạ các cột đặc biệt, còn lại EF tự map)
    // ==============================
    public class EyeExaminationConfiguration : IEntityTypeConfiguration<EyeExamination>
    {
        public void Configure(EntityTypeBuilder<EyeExamination> entity)
        {
            entity.ToTable("eye_examination");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            // Các cột số thập phân
            entity.Property(e => e.VaOdUncorrected).HasPrecision(5, 2);
            entity.Property(e => e.VaOsUncorrected).HasPrecision(5, 2);
            entity.Property(e => e.VaOdCorrected).HasPrecision(5, 2);
            entity.Property(e => e.VaOsCorrected).HasPrecision(5, 2);
            entity.Property(e => e.VaOdNear).HasPrecision(5, 2);
            entity.Property(e => e.VaOsNear).HasPrecision(5, 2);
            entity.Property(e => e.VaOdPinhole).HasPrecision(5, 2);
            entity.Property(e => e.VaOsPinhole).HasPrecision(5, 2);
            entity.Property(e => e.IopOdMmhg).HasPrecision(5, 2);
            entity.Property(e => e.IopOsMmhg).HasPrecision(5, 2);
            entity.Property(e => e.EyeballOdProptosisMm).HasPrecision(5, 2);
            entity.Property(e => e.EyeballOsProptosisMm).HasPrecision(5, 2);
            entity.Property(e => e.IopMethod).HasMaxLength(50);
            entity.Property(e => e.ExtraocularMovementNormal).HasDefaultValue(true);
            entity.Property(e => e.Nystagmus).HasDefaultValue(false);
            entity.Property(e => e.OrbitOdNormal).HasDefaultValue(true);
            entity.Property(e => e.OrbitOsNormal).HasDefaultValue(true);
            entity.Property(e => e.PreAtropine).HasDefaultValue(false);
            entity.Property(e => e.PostAtropine).HasDefaultValue(false);

            entity.HasOne(e => e.MedicalRecord).WithOne(m => m.EyeExamination).HasForeignKey<EyeExamination>(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class RefractionRecordConfiguration : IEntityTypeConfiguration<RefractionRecord>
    {
        public void Configure(EntityTypeBuilder<RefractionRecord> entity)
        {
            entity.ToTable("refraction_record");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.SphOd).HasPrecision(6, 2);
            entity.Property(e => e.CylOd).HasPrecision(6, 2);
            entity.Property(e => e.AddOd).HasPrecision(6, 2);
            entity.Property(e => e.SphOs).HasPrecision(6, 2);
            entity.Property(e => e.CylOs).HasPrecision(6, 2);
            entity.Property(e => e.AddOs).HasPrecision(6, 2);
            entity.Property(e => e.PdBinocular).HasPrecision(5, 1);
            entity.Property(e => e.PdOd).HasPrecision(5, 1);
            entity.Property(e => e.PdOs).HasPrecision(5, 1);
            entity.Property(e => e.Method).HasMaxLength(50);
            entity.Property(e => e.LensType).HasMaxLength(50);
            entity.Property(e => e.Notes).HasColumnType("nvarchar(max)");
            entity.Property(e => e.PreAtropine).HasDefaultValue(false);
            entity.Property(e => e.PostAtropine).HasDefaultValue(false);

            entity.HasOne(e => e.Examination).WithMany(e => e.RefractionRecords).HasForeignKey(e => e.ExamId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class LacrimalSystemConfiguration : IEntityTypeConfiguration<LacrimalSystem>
    {
        public void Configure(EntityTypeBuilder<LacrimalSystem> entity)
        {
            entity.ToTable("lacrimal_system");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Side).HasMaxLength(10);
            entity.Property(e => e.IrrigationFree).HasDefaultValue(true);
            entity.Property(e => e.IrrigationRegurgitationSame).HasDefaultValue(false);
            entity.Property(e => e.IrrigationRegurgitationOpposite).HasDefaultValue(false);
            entity.HasOne(e => e.Examination).WithMany(e => e.LacrimalSystems).HasForeignKey(e => e.ExamId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    // Lưu ý: AnteriorSegment và PosteriorSegment có rất nhiều trường, nhưng EF Core tự động map nếu tên property trùng tên cột.
    // Để đảm bảo, ta chỉ cần cấu hình khóa và quan hệ.
    public class AnteriorSegmentConfiguration : IEntityTypeConfiguration<AnteriorSegment>
    {
        public void Configure(EntityTypeBuilder<AnteriorSegment> entity)
        {
            entity.ToTable("anterior_segment");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Side).HasMaxLength(10);

            entity.Property(e => e.EyelidNormal).HasDefaultValue(true);
            entity.Property(e => e.ConjunctivaNormal).HasDefaultValue(true);
            entity.Property(e => e.CorneaPerforation).HasDefaultValue(false);

            entity.Property(e => e.AcBloodMm).HasPrecision(5, 2);
            entity.Property(e => e.AcDepthMm).HasPrecision(5, 2);
            entity.Property(e => e.AcPusMm).HasPrecision(5, 2);
            entity.Property(e => e.CorneaDiameterMm).HasPrecision(5, 2);
            entity.Property(e => e.IrisDiameterMm).HasPrecision(5, 2);
            entity.Property(e => e.PerforationDiameterMm).HasPrecision(5, 2);
            entity.Property(e => e.PupilDiameterMm).HasPrecision(5, 2);

            entity.HasOne(e => e.Examination).WithMany(e => e.AnteriorSegments).HasForeignKey(e => e.ExamId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class PosteriorSegmentConfiguration : IEntityTypeConfiguration<PosteriorSegment>
    {
        public void Configure(EntityTypeBuilder<PosteriorSegment> entity)
        {
            entity.ToTable("posterior_segment");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Side).HasMaxLength(10);
            entity.Property(e => e.OpticDiscNormal).HasDefaultValue(true);
            entity.Property(e => e.MaculaNormal).HasDefaultValue(true);
            entity.Property(e => e.VesselNormal).HasDefaultValue(true);
            entity.HasOne(e => e.Examination).WithMany(e => e.PosteriorSegments).HasForeignKey(e => e.ExamId).OnDelete(DeleteBehavior.Cascade);
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
            entity.Property(e => e.CentralMacularThicknessOd).HasPrecision(6, 2);
            entity.Property(e => e.CentralMacularThicknessOs).HasPrecision(6, 2);
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
            entity.Property(e => e.Machine).HasMaxLength(100);
            entity.Property(e => e.Strategy).HasMaxLength(100);
            entity.Property(e => e.Side).HasMaxLength(10);
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
            entity.Property(e => e.UltrasoundType).HasMaxLength(50);
            entity.Property(e => e.Side).HasMaxLength(10);
            entity.Property(e => e.AxialLengthMm).HasPrecision(6, 2);
            entity.Property(e => e.AnteriorChamberDepthMm).HasPrecision(5, 2);
            entity.Property(e => e.LensThicknessMm).HasPrecision(5, 2);
            entity.Property(e => e.VitreousLengthMm).HasPrecision(6, 2);
            entity.Property(e => e.LensStatus).HasMaxLength(100);
            entity.Property(e => e.VitreousStatus).HasMaxLength(100);
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
            entity.Property(e => e.Conclusion).HasColumnType("nvarchar(max)");
            // Các boolean default false
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
            entity.Property(e => e.GlaucomaType).HasMaxLength(50);
            entity.Property(e => e.IopTargetOd).HasPrecision(5, 2);
            entity.Property(e => e.IopTargetOs).HasPrecision(5, 2);
            entity.Property(e => e.StageOd).HasMaxLength(50);
            entity.Property(e => e.StageOs).HasMaxLength(50);

            entity.Property(e => e.AcDepthOdSmithMm).HasPrecision(5, 2);
            entity.Property(e => e.AcDepthOsSmithMm).HasPrecision(5, 2);

            entity.HasOne(e => e.MedicalRecord).WithOne(m => m.GlaucomaRecord).HasForeignKey<GlaucomaRecord>(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class GlaucomaSurgeryHistoryConfiguration : IEntityTypeConfiguration<GlaucomaSurgeryHistory>
    {
        public void Configure(EntityTypeBuilder<GlaucomaSurgeryHistory> entity)
        {
            entity.ToTable("glaucoma_surgery_history");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Side).HasMaxLength(10);
            entity.Property(e => e.ProcedureType).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ProcedureDate).HasColumnType("date");
            entity.Property(e => e.FacilityLevel).HasMaxLength(100);
            entity.HasOne(e => e.GlaucomaRecord).WithMany(g => g.SurgeryHistories).HasForeignKey(e => e.GlaucomaRecordId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class GlaucomaDrugHistoryConfiguration : IEntityTypeConfiguration<GlaucomaDrugHistory>
    {
        public void Configure(EntityTypeBuilder<GlaucomaDrugHistory> entity)
        {
            entity.ToTable("glaucoma_drug_history");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Side).HasMaxLength(10);
            entity.Property(e => e.DrugName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Dosage).HasMaxLength(100);
            entity.Property(e => e.Duration).HasMaxLength(100);
            entity.Property(e => e.Route).HasMaxLength(50);
            entity.Property(e => e.DrugCount).HasMaxLength(20);
            entity.Property(e => e.ChangeReason).HasColumnType("nvarchar(max)");
            entity.HasOne(e => e.GlaucomaRecord).WithMany(g => g.DrugHistories).HasForeignKey(e => e.GlaucomaRecordId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class StrabismusPtosisRecordConfiguration : IEntityTypeConfiguration<StrabismusPtosisRecord>
    {
        public void Configure(EntityTypeBuilder<StrabismusPtosisRecord> entity)
        {
            entity.ToTable("strabismus_ptosis_record");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.HirschbergOdPost).HasPrecision(5, 2);
            entity.Property(e => e.HirschbergOdPre).HasPrecision(5, 2);
            entity.Property(e => e.HirschbergOsPost).HasPrecision(5, 2);
            entity.Property(e => e.HirschbergOsPre).HasPrecision(5, 2);
            entity.Property(e => e.NearPointConvergenceCm).HasPrecision(5, 2);
            entity.Property(e => e.PrismDistanceOd).HasPrecision(5, 2);
            entity.Property(e => e.PrismDistanceOs).HasPrecision(5, 2);
            entity.Property(e => e.PrismDownOd).HasPrecision(5, 2);
            entity.Property(e => e.PrismDownOs).HasPrecision(5, 2);
            entity.Property(e => e.PrismNearOd).HasPrecision(5, 2);
            entity.Property(e => e.PrismNearOs).HasPrecision(5, 2);
            entity.Property(e => e.PrismUpOd).HasPrecision(5, 2);
            entity.Property(e => e.PrismUpOs).HasPrecision(5, 2);
            entity.Property(e => e.SynoptophoreObjective).HasPrecision(5, 2);
            entity.Property(e => e.SynoptophoreSubjective).HasPrecision(5, 2);

            entity.HasOne(e => e.MedicalRecord)
                  .WithOne(m => m.StrabismusPtosisRecord)
                  .HasForeignKey<StrabismusPtosisRecord>(e => e.RecordId)
                  .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class PediatricEyeRecordConfiguration : IEntityTypeConfiguration<PediatricEyeRecord>
    {
        public void Configure(EntityTypeBuilder<PediatricEyeRecord> entity)
        {
            entity.ToTable("pediatric_eye_record");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.AcDepthOdMm).HasPrecision(5, 2);
            entity.Property(e => e.AcDepthOsMm).HasPrecision(5, 2);
            entity.Property(e => e.CorneaDiameterOdMm).HasPrecision(5, 2);
            entity.Property(e => e.CorneaDiameterOsMm).HasPrecision(5, 2);
            entity.Property(e => e.PupilDiameterOdMm).HasPrecision(5, 2);
            entity.Property(e => e.PupilDiameterOsMm).HasPrecision(5, 2);

            entity.HasOne(e => e.MedicalRecord).WithOne(m => m.PediatricEyeRecord).HasForeignKey<PediatricEyeRecord>(e => e.RecordId).OnDelete(DeleteBehavior.Cascade);
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
            entity.Property(e => e.PdOd).HasPrecision(5, 1);
            entity.Property(e => e.PdOs).HasPrecision(5, 1);
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