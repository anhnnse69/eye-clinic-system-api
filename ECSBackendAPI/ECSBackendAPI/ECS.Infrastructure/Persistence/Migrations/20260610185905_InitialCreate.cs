using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    logo_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    rating_avg = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: true, defaultValue: 0m),
                    review_count = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clinic", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "platform_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    config_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    config_value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_config", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "specialty",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_specialty", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    password_hash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    avatar_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "facility_room",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    clinic_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    room_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    room_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_facility_room", x => x.id);
                    table.ForeignKey(
                        name: "fk_facility_room_clinic_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinic",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "medicine_catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    clinic_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    medicine_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    generic_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    dosage_form = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    concentration = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    manufacturer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_medicine_catalog", x => x.id);
                    table.ForeignKey(
                        name: "fk_medicine_catalog_clinic_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinic",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    clinic_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    service_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    duration_minutes = table.Column<int>(type: "int", nullable: false, defaultValue: 15),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_clinic_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinic",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    table_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    record_id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    old_value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    new_value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ip_address = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_log", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_log_users_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "clinic_registration_request",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    clinic_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    clinic_address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    contact_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    contact_phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    contact_email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    business_license_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    reviewed_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    review_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    requested_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    reviewed_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clinic_registration_request", x => x.id);
                    table.ForeignKey(
                        name: "fk_clinic_registration_request_users_reviewed_by",
                        column: x => x.reviewed_by,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "doctor_profile",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    clinic_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    specialty_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    experience_years = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    bio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    rating_avg = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: true, defaultValue: 0m),
                    review_count = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_doctor_profile", x => x.id);
                    table.ForeignKey(
                        name: "fk_doctor_profile_clinic_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinic",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_doctor_profile_specialties_specialty_id",
                        column: x => x.specialty_id,
                        principalTable: "specialty",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_doctor_profile_users_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_read = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    sent_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_users_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "patient_profile",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    full_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    dob = table.Column<DateTime>(type: "date", nullable: false),
                    identity_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phone_number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    bhyt_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    blood_type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    allergies = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    medical_history = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patient_profile", x => x.id);
                    table.ForeignKey(
                        name: "fk_patient_profile_users_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "staff_clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    clinic_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_clinic", x => x.id);
                    table.ForeignKey(
                        name: "fk_staff_clinic_clinic_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinic",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_staff_clinic_users_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "doctor_schedule",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    doctor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    work_date = table.Column<DateTime>(type: "date", nullable: false),
                    shift_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_doctor_schedule", x => x.id);
                    table.ForeignKey(
                        name: "fk_doctor_schedule_doctor_profile_doctor_id",
                        column: x => x.doctor_id,
                        principalTable: "doctor_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_patient",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    patient_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    relationship = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_patient", x => new { x.user_id, x.patient_id });
                    table.ForeignKey(
                        name: "fk_user_patient_patient_profile_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patient_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_patient_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "time_slot",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    schedule_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    start_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    max_patients = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    current_patients = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "AVAILABLE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_time_slot", x => x.id);
                    table.ForeignKey(
                        name: "fk_time_slot_doctor_schedule_schedule_id",
                        column: x => x.schedule_id,
                        principalTable: "doctor_schedule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "appointment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    patient_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    doctor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    slot_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    service_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    appointment_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    symptoms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    deposit_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    deposit_paid = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    booking_source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "ONLINE"),
                    created_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    follow_up_from_appointment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_appointment", x => x.id);
                    table.ForeignKey(
                        name: "fk_appointment_appointment_follow_up_from_appointment_id",
                        column: x => x.follow_up_from_appointment_id,
                        principalTable: "appointment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_appointment_doctor_profiles_doctor_id",
                        column: x => x.doctor_id,
                        principalTable: "doctor_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_appointment_patient_profiles_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patient_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_appointment_services_service_id",
                        column: x => x.service_id,
                        principalTable: "service",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_appointment_time_slots_slot_id",
                        column: x => x.slot_id,
                        principalTable: "time_slot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_appointment_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "feedback",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    appointment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    patient_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    doctor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    clinic_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rating_doctor = table.Column<int>(type: "int", nullable: false),
                    rating_clinic = table.Column<int>(type: "int", nullable: false),
                    comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_public = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feedback", x => x.id);
                    table.ForeignKey(
                        name: "fk_feedback_appointment_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "appointment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_feedback_clinic_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinic",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_feedback_doctor_profile_doctor_id",
                        column: x => x.doctor_id,
                        principalTable: "doctor_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_feedback_patient_profiles_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patient_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "medical_record",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    appointment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    patient_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    doctor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    chief_complaint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    illness_day_number = table.Column<int>(type: "int", nullable: true),
                    medical_history = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    personal_history_eye = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    personal_history_systemic = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    family_history = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    vital_pulse = table.Column<int>(type: "int", nullable: true),
                    vital_temperature = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    vital_blood_pressure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    vital_respiratory_rate = table.Column<int>(type: "int", nullable: true),
                    vital_weight_kg = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    systemic_exam = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    diagnosis_main = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    diagnosis_comorbid = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    diagnosis_differential = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    prognosis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    treatment_plan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_locked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_medical_record", x => x.id);
                    table.ForeignKey(
                        name: "fk_medical_record_appointment_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "appointment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_medical_record_doctor_profile_doctor_id",
                        column: x => x.doctor_id,
                        principalTable: "doctor_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_medical_record_patient_profiles_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patient_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "queue",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    appointment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    clinic_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    room_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    queue_number = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "WAITING"),
                    called_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    completed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_queue", x => x.id);
                    table.ForeignKey(
                        name: "fk_queue_appointment_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "appointment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_queue_clinic_clinic_id",
                        column: x => x.clinic_id,
                        principalTable: "clinic",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_queue_facility_room_room_id",
                        column: x => x.room_id,
                        principalTable: "facility_room",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "document_access_permission",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    medical_record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    granted_to_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    granted_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_access_permission", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_access_permission_medical_records_medical_record_id",
                        column: x => x.medical_record_id,
                        principalTable: "medical_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_document_access_permission_users_granted_by_user_id",
                        column: x => x.granted_by_user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_document_access_permission_users_granted_to_user_id",
                        column: x => x.granted_to_user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "emr_export_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    medical_record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    exported_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    export_format = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    file_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_emr_export_log", x => x.id);
                    table.ForeignKey(
                        name: "fk_emr_export_log_medical_records_medical_record_id",
                        column: x => x.medical_record_id,
                        principalTable: "medical_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_emr_export_log_users_exported_by",
                        column: x => x.exported_by,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "eye_ac_iris",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ac_depth_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ac_depth_herick = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ac_flat = table.Column<bool>(type: "bit", nullable: false),
                    ac_lens_material = table.Column<bool>(type: "bit", nullable: false),
                    ac_pus_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ac_tyndall = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ac_hemorrhage = table.Column<bool>(type: "bit", nullable: false),
                    angle_synechiae = table.Column<bool>(type: "bit", nullable: false),
                    angle_pigment = table.Column<bool>(type: "bit", nullable: false),
                    angle_neovascularization = table.Column<bool>(type: "bit", nullable: false),
                    iris_color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    iris_degeneration = table.Column<bool>(type: "bit", nullable: false),
                    iris_neovascularization = table.Column<bool>(type: "bit", nullable: false),
                    iris_koeppe_nodules = table.Column<bool>(type: "bit", nullable: false),
                    iris_busacca_nodules = table.Column<bool>(type: "bit", nullable: false),
                    iris_prolapse = table.Column<bool>(type: "bit", nullable: false),
                    iris_root_tear = table.Column<bool>(type: "bit", nullable: false),
                    iris_root_tear_degree = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    iris_loss = table.Column<bool>(type: "bit", nullable: false),
                    iris_perforation = table.Column<bool>(type: "bit", nullable: false),
                    pupil_round = table.Column<bool>(type: "bit", nullable: false),
                    pupil_irregular = table.Column<bool>(type: "bit", nullable: false),
                    pupil_diameter_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    pupil_sychiae = table.Column<bool>(type: "bit", nullable: false),
                    pupil_reflex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pupil_light_reflex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fundus_reflex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ac_iris_extras = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_eye_ac_iris", x => x.id);
                    table.ForeignKey(
                        name: "fk_eye_ac_iris_medical_records_record_id",
                        column: x => x.record_id,
                        principalTable: "medical_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "eye_cornea",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    clarity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    size = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    shape = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    diameter_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    sensation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    epithelium_punctate = table.Column<bool>(type: "bit", nullable: false),
                    epithelium_bullous = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    epithelium_loss = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    band_keratopathy = table.Column<bool>(type: "bit", nullable: false),
                    stroma_edema = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    stroma_infiltrate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    stroma_thinning = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ulcer = table.Column<bool>(type: "bit", nullable: false),
                    endothelium_folds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    keratic_precipitates = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    guttata = table.Column<bool>(type: "bit", nullable: false),
                    descemet_rupture = table.Column<bool>(type: "bit", nullable: false),
                    perforation_threatened = table.Column<bool>(type: "bit", nullable: false),
                    perforation = table.Column<bool>(type: "bit", nullable: false),
                    perforation_diameter_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    laceration = table.Column<bool>(type: "bit", nullable: false),
                    laceration_sutured = table.Column<bool>(type: "bit", nullable: true),
                    neovascularization = table.Column<bool>(type: "bit", nullable: false),
                    limbal_stem_deficiency = table.Column<bool>(type: "bit", nullable: false),
                    cornea_extras = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_eye_cornea", x => x.id);
                    table.ForeignKey(
                        name: "fk_eye_cornea_medical_records_record_id",
                        column: x => x.record_id,
                        principalTable: "medical_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "eye_exam_basic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    va_uncorrected = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    va_corrected = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    va_near = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    va_pinhole = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    iop_mmhg = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    iop_method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    refraction_sph = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    refraction_cyl = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    refraction_axis = table.Column<int>(type: "int", nullable: true),
                    refraction_add = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    pd = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
                    auto_refraction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    retinoscopy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    subjective_refraction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pre_atropine = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    post_atropine = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    eom_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    eom_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    nystagmus = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    nystagmus_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eyeball_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    proptosis_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    orbit_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    orbit_note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_eye_exam_basic", x => x.id);
                    table.ForeignKey(
                        name: "fk_eye_exam_basic_medical_records_record_id",
                        column: x => x.record_id,
                        principalTable: "medical_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "eye_eyelid_conjunctiva",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    eyelid_edema = table.Column<bool>(type: "bit", nullable: false),
                    ptosis = table.Column<bool>(type: "bit", nullable: false),
                    ptosis_degree = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    entropion = table.Column<bool>(type: "bit", nullable: false),
                    ectropion = table.Column<bool>(type: "bit", nullable: false),
                    lagophthalmos = table.Column<bool>(type: "bit", nullable: false),
                    laceration = table.Column<bool>(type: "bit", nullable: false),
                    laceration_depth = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    scar = table.Column<bool>(type: "bit", nullable: false),
                    chalazion = table.Column<bool>(type: "bit", nullable: false),
                    hordeolum = table.Column<bool>(type: "bit", nullable: false),
                    eyelid_other = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    conjunctiva_congestion_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    conjunctiva_edema = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_hemorrhage = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_papilla = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_follicle = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_keratinization = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_scar = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_discharge = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    fluorescein_stain = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_other = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fornix_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    symblepharon_height = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    symblepharon_width = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_eye_eyelid_conjunctiva", x => x.id);
                    table.ForeignKey(
                        name: "fk_eye_eyelid_conjunctiva_medical_records_record_id",
                        column: x => x.record_id,
                        principalTable: "medical_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "eye_fundus_disc_macula",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    optic_disc_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    optic_disc_edema = table.Column<bool>(type: "bit", nullable: false),
                    optic_disc_atrophy = table.Column<bool>(type: "bit", nullable: false),
                    optic_disc_pallor = table.Column<bool>(type: "bit", nullable: false),
                    optic_disc_cup_ratio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    optic_disc_rim_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    optic_disc_vessel_change = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    optic_disc_hemorrhage = table.Column<bool>(type: "bit", nullable: false),
                    optic_disc_neovascularization = table.Column<bool>(type: "bit", nullable: false),
                    optic_disc_not_visible = table.Column<bool>(type: "bit", nullable: false),
                    macula_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    macula_reflex_absent = table.Column<bool>(type: "bit", nullable: false),
                    macula_edema_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    macula_hole_degree = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    macula_scar = table.Column<bool>(type: "bit", nullable: false),
                    macula_serous_detachment = table.Column<bool>(type: "bit", nullable: false),
                    disc_macula_extras = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_eye_fundus_disc_macula", x => x.id);
                    table.ForeignKey(
                        name: "fk_eye_fundus_disc_macula_medical_records_record_id",
                        column: x => x.record_id,
                        principalTable: "medical_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "eye_fundus_retina_vessel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    vessel_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    artery_occlusion_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    vein_occlusion_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    occlusion_edema = table.Column<bool>(type: "bit", nullable: false),
                    occlusion_ischemia = table.Column<bool>(type: "bit", nullable: false),
                    choroidal_neovascularization = table.Column<bool>(type: "bit", nullable: false),
                    retina_hemorrhage_superficial = table.Column<bool>(type: "bit", nullable: false),
                    retina_hemorrhage_deep = table.Column<bool>(type: "bit", nullable: false),
                    retina_exudate_hard = table.Column<bool>(type: "bit", nullable: false),
                    retina_exudate_cotton_wool = table.Column<bool>(type: "bit", nullable: false),
                    retina_edema = table.Column<bool>(type: "bit", nullable: false),
                    retina_degeneration_peripheral = table.Column<bool>(type: "bit", nullable: false),
                    retina_degeneration_central = table.Column<bool>(type: "bit", nullable: false),
                    retinal_detachment = table.Column<bool>(type: "bit", nullable: false),
                    retinal_tear = table.Column<bool>(type: "bit", nullable: false),
                    retinal_tear_count = table.Column<int>(type: "int", nullable: true),
                    chorioretinitis_active = table.Column<bool>(type: "bit", nullable: false),
                    chorioretinitis_scar = table.Column<bool>(type: "bit", nullable: false),
                    chorioretinitis_count = table.Column<int>(type: "int", nullable: true),
                    intraocular_foreign_body = table.Column<bool>(type: "bit", nullable: false),
                    retina_vessel_extras = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_eye_fundus_retina_vessel", x => x.id);
                    table.ForeignKey(
                        name: "fk_eye_fundus_retina_vessel_medical_records_record_id",
                        column: x => x.record_id,
                        principalTable: "medical_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "eye_lens_vitreous",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    lens_clear = table.Column<bool>(type: "bit", nullable: false),
                    lens_opacity_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lens_rupture = table.Column<bool>(type: "bit", nullable: false),
                    lens_subluxation = table.Column<bool>(type: "bit", nullable: false),
                    lens_into_anterior = table.Column<bool>(type: "bit", nullable: false),
                    lens_iol_present = table.Column<bool>(type: "bit", nullable: false),
                    lens_iol_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    vitreous_clear = table.Column<bool>(type: "bit", nullable: false),
                    vitreous_opacity = table.Column<bool>(type: "bit", nullable: false),
                    vitreous_hemorrhage = table.Column<bool>(type: "bit", nullable: false),
                    vitreous_pvd = table.Column<bool>(type: "bit", nullable: false),
                    vitreous_tyndall = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lens_vitreous_extras = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_eye_lens_vitreous", x => x.id);
                    table.ForeignKey(
                        name: "fk_eye_lens_vitreous_medical_records_record_id",
                        column: x => x.record_id,
                        principalTable: "medical_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "eye_sclera",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    sclera_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    sclera_ectasia = table.Column<bool>(type: "bit", nullable: false),
                    sclera_thinning = table.Column<bool>(type: "bit", nullable: false),
                    sclera_necrosis = table.Column<bool>(type: "bit", nullable: false),
                    episcleritis = table.Column<bool>(type: "bit", nullable: false),
                    scleritis_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sclera_laceration = table.Column<bool>(type: "bit", nullable: false),
                    sclera_laceration_sutured = table.Column<bool>(type: "bit", nullable: true),
                    old_surgery_scar = table.Column<bool>(type: "bit", nullable: false),
                    sclera_extras = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_eye_sclera", x => x.id);
                    table.ForeignKey(
                        name: "fk_eye_sclera_medical_records_record_id",
                        column: x => x.record_id,
                        principalTable: "medical_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "glasses_prescription",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    doctor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sph_od = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    cyl_od = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    axis_od = table.Column<int>(type: "int", nullable: true),
                    add_od = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    sph_os = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    cyl_os = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    axis_os = table.Column<int>(type: "int", nullable: true),
                    add_os = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    pd = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
                    lens_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glasses_prescription", x => x.id);
                    table.ForeignKey(
                        name: "fk_glasses_prescription_doctor_profile_doctor_id",
                        column: x => x.doctor_id,
                        principalTable: "doctor_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_glasses_prescription_medical_records_record_id",
                        column: x => x.record_id,
                        principalTable: "medical_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "glaucoma_record",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    symptoms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    history_eye = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    history_steroid = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    history_systemic = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    family_glaucoma = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    glaucoma_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    iop_target_od = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    iop_target_os = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    stage_od = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    stage_os = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    gonioscopy_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    gonioscopy_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    bleb_od_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    bleb_os_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    optic_disc_description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    nerve_rim_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    nerve_rim_os = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glaucoma_record", x => x.id);
                    table.ForeignKey(
                        name: "fk_glaucoma_record_medical_records_record_id",
                        column: x => x.record_id,
                        principalTable: "medical_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lacrimal_record",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    irrigation_free = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    irrigation_regurgitation_same = table.Column<bool>(type: "bit", nullable: false),
                    irrigation_regurgitation_opposite = table.Column<bool>(type: "bit", nullable: false),
                    irrigation_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lacrimal_other = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lacrimal_record", x => x.id);
                    table.ForeignKey(
                        name: "fk_lacrimal_record_medical_records_record_id",
                        column: x => x.record_id,
                        principalTable: "medical_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "medical_record_extras",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    trauma_summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    glaucoma_summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pediatric_summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lab_orders = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    imaging_orders = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    discharge_summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    treatment_process = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_medical_record_extras", x => x.id);
                    table.ForeignKey(
                        name: "fk_medical_record_extras_medical_record_record_id",
                        column: x => x.record_id,
                        principalTable: "medical_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_medical_record_extras_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "oct_result",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    machine_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    scan_pattern = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    rnfl_average_od = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    rnfl_average_os = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    cmt_od = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    cmt_os = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    cup_disc_ratio_od = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    cup_disc_ratio_os = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    conclusion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    image_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    exam_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    technician_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_oct_result", x => x.id);
                    table.ForeignKey(
                        name: "fk_oct_result_medical_record_record_id",
                        column: x => x.record_id,
                        principalTable: "medical_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pediatric_eye_record",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    congenital = table.Column<bool>(type: "bit", nullable: false),
                    acquired = table.Column<bool>(type: "bit", nullable: false),
                    acquired_onset = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    prior_treatment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pregnancy_illness = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    pregnancy_illness_detail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    intellectual_development_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    chief_symptoms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    entropion_od = table.Column<bool>(type: "bit", nullable: false),
                    epicanthus_od = table.Column<bool>(type: "bit", nullable: false),
                    ptosis_od = table.Column<bool>(type: "bit", nullable: false),
                    eyeball_od_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    amblyopia_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fundus_summary_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fundus_summary_os = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pediatric_eye_record", x => x.id);
                    table.ForeignKey(
                        name: "fk_pediatric_eye_record_medical_record_record_id",
                        column: x => x.record_id,
                        principalTable: "medical_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "prescription",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    doctor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prescription", x => x.id);
                    table.ForeignKey(
                        name: "fk_prescription_doctor_profile_doctor_id",
                        column: x => x.doctor_id,
                        principalTable: "doctor_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_prescription_medical_record_record_id",
                        column: x => x.record_id,
                        principalTable: "medical_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "strabismus_ptosis_record",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    chief_strabismus = table.Column<bool>(type: "bit", nullable: false),
                    chief_ptosis = table.Column<bool>(type: "bit", nullable: false),
                    congenital = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    acquired = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    acquired_onset = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    prior_amblyopia_treatment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    prior_surgery = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    strabismus_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    nystagmus = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    nystagmus_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    refraction_pre_atropine = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    refraction_post_atropine = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cover_test_result = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    prism_measurements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    strabismus_syndrome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    binocular_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ptosis_od_degree = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ptosis_os_degree = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    levator_function_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    levator_function_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    marcus_gunn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fixation_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fixation_os = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_strabismus_ptosis_record", x => x.id);
                    table.ForeignKey(
                        name: "fk_strabismus_ptosis_record_medical_record_record_id",
                        column: x => x.record_id,
                        principalTable: "medical_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trauma_record",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    injury_cause = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    injury_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    prior_treatment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    post_treatment_course = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    od_injuries = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    os_injuries = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    injury_details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    trauma_conclusion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trauma_record", x => x.id);
                    table.ForeignKey(
                        name: "fk_trauma_record_medical_record_record_id",
                        column: x => x.record_id,
                        principalTable: "medical_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ultrasound_eye",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ultrasound_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    axial_length_mm = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    ac_depth_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    lens_thickness_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    vitreous_length_mm = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    lens_status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    retina_status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    conclusion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    image_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    exam_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    technician_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ultrasound_eye", x => x.id);
                    table.ForeignKey(
                        name: "fk_ultrasound_eye_medical_record_record_id",
                        column: x => x.record_id,
                        principalTable: "medical_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "visual_field_test",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    machine = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    strategy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    md_value = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    psd_value = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    vfi_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    reliable = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    result_summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    image_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    test_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    technician_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_visual_field_test", x => x.id);
                    table.ForeignKey(
                        name: "fk_visual_field_test_medical_record_record_id",
                        column: x => x.record_id,
                        principalTable: "medical_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "glaucoma_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    glaucoma_record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    history_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    attempt_number = table.Column<int>(type: "int", nullable: true),
                    procedure_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    procedure_date = table.Column<DateTime>(type: "date", nullable: true),
                    facility_level = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    drug_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    dosage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    duration = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    route = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    change_reason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glaucoma_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_glaucoma_history_glaucoma_records_glaucoma_record_id",
                        column: x => x.glaucoma_record_id,
                        principalTable: "glaucoma_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "prescription_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    prescription_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    medicine_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    dosage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    frequency = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    duration_days = table.Column<int>(type: "int", nullable: true),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    instruction = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prescription_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_prescription_item_prescription_prescription_id",
                        column: x => x.prescription_id,
                        principalTable: "prescription",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_appointment_created_by_id",
                table: "appointment",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_appointment_doctor_id",
                table: "appointment",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_appointment_follow_up_from_appointment_id",
                table: "appointment",
                column: "follow_up_from_appointment_id");

            migrationBuilder.CreateIndex(
                name: "ix_appointment_patient_id",
                table: "appointment",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_appointment_service_id",
                table: "appointment",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_appointment_slot_id",
                table: "appointment",
                column: "slot_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_user_id",
                table: "audit_log",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_clinic_registration_request_reviewed_by",
                table: "clinic_registration_request",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_profile_clinic_id",
                table: "doctor_profile",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_profile_specialty_id",
                table: "doctor_profile",
                column: "specialty_id");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_profile_user_id",
                table: "doctor_profile",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_schedule_doctor_id",
                table: "doctor_schedule",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_access_permission_granted_by_user_id",
                table: "document_access_permission",
                column: "granted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_access_permission_granted_to_user_id",
                table: "document_access_permission",
                column: "granted_to_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_access_permission_medical_record_id",
                table: "document_access_permission",
                column: "medical_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_emr_export_log_exported_by",
                table: "emr_export_log",
                column: "exported_by");

            migrationBuilder.CreateIndex(
                name: "ix_emr_export_log_medical_record_id",
                table: "emr_export_log",
                column: "medical_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_eye_ac_iris_record_id",
                table: "eye_ac_iris",
                column: "record_id");

            migrationBuilder.CreateIndex(
                name: "ix_eye_cornea_record_id",
                table: "eye_cornea",
                column: "record_id");

            migrationBuilder.CreateIndex(
                name: "ix_eye_exam_basic_record_id",
                table: "eye_exam_basic",
                column: "record_id");

            migrationBuilder.CreateIndex(
                name: "ix_eye_eyelid_conjunctiva_record_id",
                table: "eye_eyelid_conjunctiva",
                column: "record_id");

            migrationBuilder.CreateIndex(
                name: "ix_eye_fundus_disc_macula_record_id",
                table: "eye_fundus_disc_macula",
                column: "record_id");

            migrationBuilder.CreateIndex(
                name: "ix_eye_fundus_retina_vessel_record_id",
                table: "eye_fundus_retina_vessel",
                column: "record_id");

            migrationBuilder.CreateIndex(
                name: "ix_eye_lens_vitreous_record_id",
                table: "eye_lens_vitreous",
                column: "record_id");

            migrationBuilder.CreateIndex(
                name: "ix_eye_sclera_record_id",
                table: "eye_sclera",
                column: "record_id");

            migrationBuilder.CreateIndex(
                name: "ix_facility_room_clinic_id",
                table: "facility_room",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "ix_feedback_appointment_id",
                table: "feedback",
                column: "appointment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_feedback_clinic_id",
                table: "feedback",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "ix_feedback_doctor_id",
                table: "feedback",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_feedback_patient_id",
                table: "feedback",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_glasses_prescription_doctor_id",
                table: "glasses_prescription",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_glasses_prescription_record_id",
                table: "glasses_prescription",
                column: "record_id");

            migrationBuilder.CreateIndex(
                name: "ix_glaucoma_history_glaucoma_record_id",
                table: "glaucoma_history",
                column: "glaucoma_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_glaucoma_record_record_id",
                table: "glaucoma_record",
                column: "record_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lacrimal_record_record_id",
                table: "lacrimal_record",
                column: "record_id");

            migrationBuilder.CreateIndex(
                name: "ix_medical_record_appointment_id",
                table: "medical_record",
                column: "appointment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_medical_record_doctor_id",
                table: "medical_record",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_medical_record_patient_id",
                table: "medical_record",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_medical_record_extras_record_id",
                table: "medical_record_extras",
                column: "record_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_medical_record_extras_updated_by",
                table: "medical_record_extras",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "ix_medicine_catalog_clinic_id",
                table: "medicine_catalog",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_user_id",
                table: "notification",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_oct_result_record_id",
                table: "oct_result",
                column: "record_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_profile_user_id",
                table: "patient_profile",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_pediatric_eye_record_record_id",
                table: "pediatric_eye_record",
                column: "record_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_platform_config_config_key",
                table: "platform_config",
                column: "config_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_prescription_doctor_id",
                table: "prescription",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_prescription_record_id",
                table: "prescription",
                column: "record_id");

            migrationBuilder.CreateIndex(
                name: "ix_prescription_item_prescription_id",
                table: "prescription_item",
                column: "prescription_id");

            migrationBuilder.CreateIndex(
                name: "ix_queue_appointment_id",
                table: "queue",
                column: "appointment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_queue_clinic_id",
                table: "queue",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "ix_queue_room_id",
                table: "queue",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_clinic_id",
                table: "service",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "ix_staff_clinic_clinic_id",
                table: "staff_clinic",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "ix_staff_clinic_user_id",
                table: "staff_clinic",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_strabismus_ptosis_record_record_id",
                table: "strabismus_ptosis_record",
                column: "record_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_time_slot_schedule_id",
                table: "time_slot",
                column: "schedule_id");

            migrationBuilder.CreateIndex(
                name: "ix_trauma_record_record_id",
                table: "trauma_record",
                column: "record_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ultrasound_eye_record_id",
                table: "ultrasound_eye",
                column: "record_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_email",
                table: "user",
                column: "email",
                unique: true,
                filter: "[email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_phone",
                table: "user",
                column: "phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_patient_patient_id",
                table: "user_patient",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_visual_field_test_record_id",
                table: "visual_field_test",
                column: "record_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "clinic_registration_request");

            migrationBuilder.DropTable(
                name: "document_access_permission");

            migrationBuilder.DropTable(
                name: "emr_export_log");

            migrationBuilder.DropTable(
                name: "eye_ac_iris");

            migrationBuilder.DropTable(
                name: "eye_cornea");

            migrationBuilder.DropTable(
                name: "eye_exam_basic");

            migrationBuilder.DropTable(
                name: "eye_eyelid_conjunctiva");

            migrationBuilder.DropTable(
                name: "eye_fundus_disc_macula");

            migrationBuilder.DropTable(
                name: "eye_fundus_retina_vessel");

            migrationBuilder.DropTable(
                name: "eye_lens_vitreous");

            migrationBuilder.DropTable(
                name: "eye_sclera");

            migrationBuilder.DropTable(
                name: "feedback");

            migrationBuilder.DropTable(
                name: "glasses_prescription");

            migrationBuilder.DropTable(
                name: "glaucoma_history");

            migrationBuilder.DropTable(
                name: "lacrimal_record");

            migrationBuilder.DropTable(
                name: "medical_record_extras");

            migrationBuilder.DropTable(
                name: "medicine_catalog");

            migrationBuilder.DropTable(
                name: "notification");

            migrationBuilder.DropTable(
                name: "oct_result");

            migrationBuilder.DropTable(
                name: "pediatric_eye_record");

            migrationBuilder.DropTable(
                name: "platform_config");

            migrationBuilder.DropTable(
                name: "prescription_item");

            migrationBuilder.DropTable(
                name: "queue");

            migrationBuilder.DropTable(
                name: "staff_clinic");

            migrationBuilder.DropTable(
                name: "strabismus_ptosis_record");

            migrationBuilder.DropTable(
                name: "trauma_record");

            migrationBuilder.DropTable(
                name: "ultrasound_eye");

            migrationBuilder.DropTable(
                name: "user_patient");

            migrationBuilder.DropTable(
                name: "visual_field_test");

            migrationBuilder.DropTable(
                name: "glaucoma_record");

            migrationBuilder.DropTable(
                name: "prescription");

            migrationBuilder.DropTable(
                name: "facility_room");

            migrationBuilder.DropTable(
                name: "medical_record");

            migrationBuilder.DropTable(
                name: "appointment");

            migrationBuilder.DropTable(
                name: "patient_profile");

            migrationBuilder.DropTable(
                name: "service");

            migrationBuilder.DropTable(
                name: "time_slot");

            migrationBuilder.DropTable(
                name: "doctor_schedule");

            migrationBuilder.DropTable(
                name: "doctor_profile");

            migrationBuilder.DropTable(
                name: "clinic");

            migrationBuilder.DropTable(
                name: "specialty");

            migrationBuilder.DropTable(
                name: "user");
        }
    }
}
