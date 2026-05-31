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
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                    record_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
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
                    systemic_endocrine_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    systemic_endocrine_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    systemic_neuro_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    systemic_neuro_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    systemic_cardio_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    systemic_cardio_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    systemic_respiratory_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    systemic_respiratory_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    systemic_digestive_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    systemic_digestive_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    systemic_musculo_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    systemic_musculo_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    systemic_urogenital_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    systemic_urogenital_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    systemic_other_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                name: "eye_examination",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    va_od_uncorrected = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    va_os_uncorrected = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    va_od_corrected = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    va_os_corrected = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    va_od_near = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    va_os_near = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    va_od_pinhole = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    va_os_pinhole = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    iop_od_mmhg = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    iop_os_mmhg = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    iop_method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    visual_field_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    visual_field_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    extraocular_movement_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    extraocular_movement_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    nystagmus = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    nystagmus_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eyeball_od_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eyeball_os_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eyeball_od_proptosis_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    eyeball_os_proptosis_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    orbit_od_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    orbit_od_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    orbit_os_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    orbit_os_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    auto_refraction_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    auto_refraction_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    retinoscopy_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    retinoscopy_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    subjective_refraction_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    subjective_refraction_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pre_atropine = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    post_atropine = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_eye_examination", x => x.id);
                    table.ForeignKey(
                        name: "fk_eye_examination_medical_records_record_id",
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
                    pd_od = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
                    pd_os = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
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
                    symptom_eye_pain = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    symptom_blurred_vision = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    symptom_visual_field_constriction = table.Column<bool>(type: "bit", nullable: false),
                    symptom_halos = table.Column<bool>(type: "bit", nullable: false),
                    symptom_photophobia = table.Column<bool>(type: "bit", nullable: false),
                    symptom_tearing = table.Column<bool>(type: "bit", nullable: false),
                    symptom_red_eye = table.Column<bool>(type: "bit", nullable: false),
                    symptom_headache = table.Column<bool>(type: "bit", nullable: false),
                    symptom_nausea = table.Column<bool>(type: "bit", nullable: false),
                    symptom_vomiting = table.Column<bool>(type: "bit", nullable: false),
                    symptom_other_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    history_myopia = table.Column<bool>(type: "bit", nullable: false),
                    history_hyperopia = table.Column<bool>(type: "bit", nullable: false),
                    history_trauma = table.Column<bool>(type: "bit", nullable: false),
                    history_uveitis = table.Column<bool>(type: "bit", nullable: false),
                    history_anterior_segment_inflammation = table.Column<bool>(type: "bit", nullable: false),
                    history_crvo = table.Column<bool>(type: "bit", nullable: false),
                    history_prior_eye_surgery = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    history_other_eye_disease = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    history_steroid_use = table.Column<bool>(type: "bit", nullable: false),
                    steroid_drug_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    steroid_duration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    steroid_route = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    steroid_self_medicated = table.Column<bool>(type: "bit", nullable: true),
                    systemic_cardiovascular = table.Column<bool>(type: "bit", nullable: false),
                    systemic_hypertension = table.Column<bool>(type: "bit", nullable: false),
                    systemic_diabetes = table.Column<bool>(type: "bit", nullable: false),
                    systemic_carotid_sinus_fistula = table.Column<bool>(type: "bit", nullable: false),
                    systemic_other_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    family_glaucoma_grandparents = table.Column<bool>(type: "bit", nullable: false),
                    family_glaucoma_parents = table.Column<bool>(type: "bit", nullable: false),
                    family_glaucoma_siblings = table.Column<bool>(type: "bit", nullable: false),
                    family_glaucoma_other_relatives = table.Column<bool>(type: "bit", nullable: false),
                    glaucoma_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    iop_target_od = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    iop_target_os = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    stage_od = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    stage_os = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    cup_disc_description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    nerve_rim_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    nerve_rim_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    bleb_od_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    bleb_od_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    bleb_os_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    bleb_os_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ac_depth_od_smith_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ac_depth_od_herick = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ac_depth_os_smith_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ac_depth_os_herick = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    gonioscopy_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    gonioscopy_os = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                name: "oct_result",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    machine_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    scan_pattern = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    rnfl_average_od = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    rnfl_average_os = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    central_macular_thickness_od = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    central_macular_thickness_os = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
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
                    pregnancy_illness = table.Column<bool>(type: "bit", nullable: false),
                    pregnancy_illness_detail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    intellectual_development_normal = table.Column<bool>(type: "bit", nullable: false),
                    intellectual_development_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    chief_blurred_vision = table.Column<bool>(type: "bit", nullable: false),
                    chief_eye_pain = table.Column<bool>(type: "bit", nullable: false),
                    chief_red_eye = table.Column<bool>(type: "bit", nullable: false),
                    chief_photophobia = table.Column<bool>(type: "bit", nullable: false),
                    extraocular_motility_od_normal = table.Column<bool>(type: "bit", nullable: false),
                    extraocular_motility_od_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    extraocular_motility_os_normal = table.Column<bool>(type: "bit", nullable: false),
                    extraocular_motility_os_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    intrinsic_motility_od_normal = table.Column<bool>(type: "bit", nullable: false),
                    intrinsic_motility_od_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    intrinsic_motility_os_normal = table.Column<bool>(type: "bit", nullable: false),
                    intrinsic_motility_os_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    nystagmus = table.Column<bool>(type: "bit", nullable: false),
                    nystagmus_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    entropion_od = table.Column<bool>(type: "bit", nullable: false),
                    entropion_od_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    entropion_os = table.Column<bool>(type: "bit", nullable: false),
                    entropion_os_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    epicanthus_od = table.Column<bool>(type: "bit", nullable: false),
                    epicanthus_os = table.Column<bool>(type: "bit", nullable: false),
                    ptosis_od = table.Column<bool>(type: "bit", nullable: false),
                    ptosis_os = table.Column<bool>(type: "bit", nullable: false),
                    eyelid_tumor_od = table.Column<bool>(type: "bit", nullable: false),
                    eyelid_tumor_os = table.Column<bool>(type: "bit", nullable: false),
                    eyelid_other_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eyelid_other_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lacrimal_od_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lacrimal_os_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    conjunctiva_congestion_od = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_hemorrhage_od = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_exudate_od = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_tumor_od = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_other_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    conjunctiva_congestion_os = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_hemorrhage_os = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_exudate_os = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_tumor_os = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_other_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cornea_clarity_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cornea_edema_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cornea_tumor_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cornea_precipitates_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cornea_ulcer_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cornea_malformation_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cornea_diameter_od_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    cornea_limbus_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cornea_other_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cornea_clarity_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cornea_edema_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cornea_tumor_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cornea_precipitates_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cornea_ulcer_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cornea_malformation_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cornea_diameter_os_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    cornea_limbus_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cornea_other_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sclera_ectasia_od = table.Column<bool>(type: "bit", nullable: false),
                    sclera_scar_od = table.Column<bool>(type: "bit", nullable: false),
                    sclera_congestion_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sclera_inflammation_od = table.Column<bool>(type: "bit", nullable: false),
                    sclera_other_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sclera_ectasia_os = table.Column<bool>(type: "bit", nullable: false),
                    sclera_scar_os = table.Column<bool>(type: "bit", nullable: false),
                    sclera_congestion_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sclera_inflammation_os = table.Column<bool>(type: "bit", nullable: false),
                    sclera_other_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ac_depth_od_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ac_tyndall_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ac_pus_od = table.Column<bool>(type: "bit", nullable: false),
                    ac_blood_od = table.Column<bool>(type: "bit", nullable: false),
                    ac_angle_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ac_depth_os_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ac_tyndall_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ac_pus_os = table.Column<bool>(type: "bit", nullable: false),
                    ac_blood_os = table.Column<bool>(type: "bit", nullable: false),
                    ac_angle_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    iris_color_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    iris_degeneration_od = table.Column<bool>(type: "bit", nullable: false),
                    iris_neovascularization_od = table.Column<bool>(type: "bit", nullable: false),
                    ciliary_sensation_od = table.Column<bool>(type: "bit", nullable: false),
                    iris_tumor_od = table.Column<bool>(type: "bit", nullable: false),
                    iris_malformation_od = table.Column<bool>(type: "bit", nullable: false),
                    iris_other_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    iris_color_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    iris_degeneration_os = table.Column<bool>(type: "bit", nullable: false),
                    iris_neovascularization_os = table.Column<bool>(type: "bit", nullable: false),
                    ciliary_sensation_os = table.Column<bool>(type: "bit", nullable: false),
                    iris_tumor_os = table.Column<bool>(type: "bit", nullable: false),
                    iris_malformation_os = table.Column<bool>(type: "bit", nullable: false),
                    iris_other_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pupil_shape_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pupil_diameter_od_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    pupil_pigment_ruff_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pupil_reflex_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pupil_malformation_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pupil_shape_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pupil_diameter_os_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    pupil_pigment_ruff_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pupil_reflex_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pupil_malformation_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lens_status_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lens_status_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    vitreous_status_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    vitreous_status_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    retina_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    macula_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    vessels_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    optic_disc_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fundus_tumor_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    retina_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    macula_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    vessels_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    optic_disc_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fundus_tumor_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eyeball_od_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eyeball_os_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    amblyopia_status = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    chief_other = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    congenital = table.Column<bool>(type: "bit", nullable: false),
                    acquired = table.Column<bool>(type: "bit", nullable: false),
                    acquired_onset = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    prior_amblyopia_treatment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    prior_surgery_method = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    prior_surgery_result = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    esotropia = table.Column<bool>(type: "bit", nullable: false),
                    exotropia = table.Column<bool>(type: "bit", nullable: false),
                    vertical_strabismus = table.Column<bool>(type: "bit", nullable: false),
                    nystagmus = table.Column<bool>(type: "bit", nullable: false),
                    nystagmus_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    null_point_nystagmus = table.Column<bool>(type: "bit", nullable: false),
                    auto_refraction_pre_atropine_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    auto_refraction_pre_atropine_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    auto_refraction_post_atropine_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    auto_refraction_post_atropine_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    retinoscopy_post_atropine_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    retinoscopy_post_atropine_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    extraocular_motility_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    extraocular_motility_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    intrinsic_motility_od_normal = table.Column<bool>(type: "bit", nullable: false),
                    intrinsic_motility_od_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    intrinsic_motility_os_normal = table.Column<bool>(type: "bit", nullable: false),
                    intrinsic_motility_os_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    near_point_convergence_normal = table.Column<bool>(type: "bit", nullable: false),
                    near_point_convergence_cm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    cover_test_result = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    hirschberg_od_pre = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    hirschberg_od_post = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    hirschberg_os_pre = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    hirschberg_os_post = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    prism_near_od = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    prism_distance_od = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    prism_up_od = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    prism_down_od = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    prism_near_os = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    prism_distance_os = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    prism_up_os = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    prism_down_os = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    strabismus_syndrome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    strabismus_nature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    synoptophore_objective = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    synoptophore_subjective = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    binocular_simultaneous_vision = table.Column<bool>(type: "bit", nullable: false),
                    binocular_fusion = table.Column<bool>(type: "bit", nullable: false),
                    binocular_stereopsis = table.Column<bool>(type: "bit", nullable: false),
                    fusion_amplitude = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    retinal_correspondence_normal = table.Column<bool>(type: "bit", nullable: false),
                    diplopia = table.Column<bool>(type: "bit", nullable: false),
                    diplopia_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    compensatory_head_posture = table.Column<bool>(type: "bit", nullable: false),
                    compensatory_head_posture_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ptosis = table.Column<bool>(type: "bit", nullable: false),
                    ptosis_degree_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ptosis_degree_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    epicanthus_od = table.Column<bool>(type: "bit", nullable: false),
                    epicanthus_os = table.Column<bool>(type: "bit", nullable: false),
                    levator_function_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    levator_function_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    marcus_gunn_od = table.Column<bool>(type: "bit", nullable: false),
                    marcus_gunn_os = table.Column<bool>(type: "bit", nullable: false),
                    bell_phenomenon_od = table.Column<bool>(type: "bit", nullable: false),
                    bell_phenomenon_os = table.Column<bool>(type: "bit", nullable: false),
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
                    od_lid_laceration = table.Column<bool>(type: "bit", nullable: false),
                    od_canaliculus_laceration = table.Column<bool>(type: "bit", nullable: false),
                    od_corneal_rupture = table.Column<bool>(type: "bit", nullable: false),
                    od_scleral_rupture = table.Column<bool>(type: "bit", nullable: false),
                    od_hyphema = table.Column<bool>(type: "bit", nullable: false),
                    od_lens_rupture = table.Column<bool>(type: "bit", nullable: false),
                    od_vitreous_hemorrhage = table.Column<bool>(type: "bit", nullable: false),
                    od_retinal_detachment = table.Column<bool>(type: "bit", nullable: false),
                    od_intraocular_foreign_body = table.Column<bool>(type: "bit", nullable: false),
                    od_orbital_foreign_body = table.Column<bool>(type: "bit", nullable: false),
                    os_lid_laceration = table.Column<bool>(type: "bit", nullable: false),
                    os_canaliculus_laceration = table.Column<bool>(type: "bit", nullable: false),
                    os_corneal_rupture = table.Column<bool>(type: "bit", nullable: false),
                    os_scleral_rupture = table.Column<bool>(type: "bit", nullable: false),
                    os_hyphema = table.Column<bool>(type: "bit", nullable: false),
                    os_lens_rupture = table.Column<bool>(type: "bit", nullable: false),
                    os_vitreous_hemorrhage = table.Column<bool>(type: "bit", nullable: false),
                    os_retinal_detachment = table.Column<bool>(type: "bit", nullable: false),
                    os_intraocular_foreign_body = table.Column<bool>(type: "bit", nullable: false),
                    os_orbital_foreign_body = table.Column<bool>(type: "bit", nullable: false),
                    conclusion = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    ultrasound_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    axial_length_mm = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    anterior_chamber_depth_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    lens_thickness_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    vitreous_length_mm = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    lens_status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    vitreous_status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    machine = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    strategy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
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
                name: "anterior_segment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    exam_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    eyelid_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    eyelid_edema = table.Column<bool>(type: "bit", nullable: false),
                    eyelid_ptosis = table.Column<bool>(type: "bit", nullable: false),
                    ptosis_degree = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eyelid_entropion = table.Column<bool>(type: "bit", nullable: false),
                    entropion_upper_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    entropion_lower_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eyelid_ectropion = table.Column<bool>(type: "bit", nullable: false),
                    eyelid_lagophthalmos = table.Column<bool>(type: "bit", nullable: false),
                    eyelid_coloboma = table.Column<bool>(type: "bit", nullable: false),
                    coloboma_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eyelid_laceration = table.Column<bool>(type: "bit", nullable: false),
                    laceration_depth = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    laceration_sutured = table.Column<bool>(type: "bit", nullable: true),
                    eyelid_scar = table.Column<bool>(type: "bit", nullable: false),
                    eyelid_chalazion = table.Column<bool>(type: "bit", nullable: false),
                    eyelid_hordeolum = table.Column<bool>(type: "bit", nullable: false),
                    eyelid_tumor = table.Column<bool>(type: "bit", nullable: false),
                    tumor_nature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tumor_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tumor_size = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    canaliculus_normal = table.Column<bool>(type: "bit", nullable: false),
                    canaliculus_laceration = table.Column<bool>(type: "bit", nullable: false),
                    canaliculus_laceration_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    meibomian_normal = table.Column<bool>(type: "bit", nullable: false),
                    meibomian_blockage_degree = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    blepharitis = table.Column<bool>(type: "bit", nullable: false),
                    eyelid_other_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    conjunctiva_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    conjunctiva_congestion_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    conjunctiva_edema = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_hemorrhage = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_papilla = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_follicle = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_keratinization = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_scar = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_discharge_purulent = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_discharge_clear = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_pseudomembrane = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_fluorescein_stain = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_laceration = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_ischemia = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_tumor = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_tumor_nature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    conjunctiva_tumor_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    conjunctiva_tumor_size = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fornix_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fornix_symblepharon_height = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fornix_symblepharon_width = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    conjunctiva_other_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cornea_clarity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cornea_size = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cornea_shape = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cornea_diameter_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    epithelium_punctate_lesion = table.Column<bool>(type: "bit", nullable: false),
                    epithelium_bullous_edema = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    epithelium_loss_area = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    epithelium_loss_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    epithelium_loss_edge = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    band_keratopathy = table.Column<bool>(type: "bit", nullable: false),
                    drug_deposit = table.Column<bool>(type: "bit", nullable: false),
                    stroma_edema = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    stroma_infiltrate_depth = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    stroma_infiltrate_distribution = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    stroma_thinning = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    corneal_ulcer = table.Column<bool>(type: "bit", nullable: false),
                    ulcer_size = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ulcer_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ulcer_edge = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    endothelium_folds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pigment_deposit_posterior = table.Column<bool>(type: "bit", nullable: false),
                    pus_posterior = table.Column<bool>(type: "bit", nullable: false),
                    exudate_posterior = table.Column<bool>(type: "bit", nullable: false),
                    guttata = table.Column<bool>(type: "bit", nullable: false),
                    descemet_rupture = table.Column<bool>(type: "bit", nullable: false),
                    descemet_scroll = table.Column<bool>(type: "bit", nullable: false),
                    keratic_precipitates = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cornea_perforation_threatened = table.Column<bool>(type: "bit", nullable: false),
                    cornea_iris_incarceration = table.Column<bool>(type: "bit", nullable: false),
                    cornea_perforation = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    perforation_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    perforation_seidel = table.Column<bool>(type: "bit", nullable: false),
                    perforation_diameter_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    perforation_sealed = table.Column<bool>(type: "bit", nullable: true),
                    cornea_laceration = table.Column<bool>(type: "bit", nullable: false),
                    laceration_size = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    laceration_location_cornea = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    laceration_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    laceration_sutured_cornea = table.Column<bool>(type: "bit", nullable: true),
                    laceration_suture_anatomical = table.Column<bool>(type: "bit", nullable: true),
                    cornea_blood_staining = table.Column<bool>(type: "bit", nullable: false),
                    cornea_abscess = table.Column<bool>(type: "bit", nullable: false),
                    cornea_sensation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    neovascularization_superficial = table.Column<bool>(type: "bit", nullable: false),
                    neovascularization_superficial_direction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    neovascularization_deep = table.Column<bool>(type: "bit", nullable: false),
                    neovascularization_extent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    limbal_stem_cell_deficiency = table.Column<bool>(type: "bit", nullable: false),
                    limbal_age_degeneration = table.Column<bool>(type: "bit", nullable: false),
                    limbal_calcium_deposit = table.Column<bool>(type: "bit", nullable: false),
                    cornea_dv_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cornea_other_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sclera_normal = table.Column<bool>(type: "bit", nullable: false),
                    sclera_ectasia = table.Column<bool>(type: "bit", nullable: false),
                    sclera_thinning = table.Column<bool>(type: "bit", nullable: false),
                    sclera_necrosis = table.Column<bool>(type: "bit", nullable: false),
                    sclera_episcleritis = table.Column<bool>(type: "bit", nullable: false),
                    sclera_scleritis_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sclera_laceration = table.Column<bool>(type: "bit", nullable: false),
                    sclera_laceration_size = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sclera_laceration_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sclera_laceration_sutured = table.Column<bool>(type: "bit", nullable: true),
                    sclera_tissue_incarceration = table.Column<bool>(type: "bit", nullable: false),
                    sclera_old_surgery_scar = table.Column<bool>(type: "bit", nullable: false),
                    sclera_surgery_scar_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sclera_other_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ac_normal = table.Column<bool>(type: "bit", nullable: false),
                    ac_depth_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ac_depth_herick = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ac_flat = table.Column<bool>(type: "bit", nullable: false),
                    ac_lens_material = table.Column<bool>(type: "bit", nullable: false),
                    ac_pus_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ac_exudate = table.Column<bool>(type: "bit", nullable: false),
                    ac_tyndall = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ac_hemorrhage = table.Column<bool>(type: "bit", nullable: false),
                    ac_hemorrhage_degree = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ac_foreign_body = table.Column<bool>(type: "bit", nullable: false),
                    ac_blood_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    angle_synechiae = table.Column<bool>(type: "bit", nullable: false),
                    angle_pigment = table.Column<bool>(type: "bit", nullable: false),
                    angle_neovascularization = table.Column<bool>(type: "bit", nullable: false),
                    angle_other_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ac_other_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    iris_normal = table.Column<bool>(type: "bit", nullable: false),
                    iris_color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    iris_degeneration = table.Column<bool>(type: "bit", nullable: false),
                    iris_neovascularization = table.Column<bool>(type: "bit", nullable: false),
                    iris_koeppe_nodules = table.Column<bool>(type: "bit", nullable: false),
                    iris_busacca_nodules = table.Column<bool>(type: "bit", nullable: false),
                    iris_prolapse = table.Column<bool>(type: "bit", nullable: false),
                    iris_incarceration = table.Column<bool>(type: "bit", nullable: false),
                    iris_root_tear = table.Column<bool>(type: "bit", nullable: false),
                    iris_root_tear_degree = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    iris_loss = table.Column<bool>(type: "bit", nullable: false),
                    iris_perforation = table.Column<bool>(type: "bit", nullable: false),
                    iris_atrophy_od = table.Column<bool>(type: "bit", nullable: false),
                    iris_diameter_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    iris_other_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pupil_round = table.Column<bool>(type: "bit", nullable: false),
                    pupil_irregular = table.Column<bool>(type: "bit", nullable: false),
                    pupil_diameter_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    pupil_synechiae = table.Column<bool>(type: "bit", nullable: false),
                    pupil_synechiae_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pupil_reflex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pupil_mydriasis_paralysis = table.Column<bool>(type: "bit", nullable: false),
                    pupil_pigment_ruff = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pupil_light_reflex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fundus_reflex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pupil_other_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lens_clear = table.Column<bool>(type: "bit", nullable: false),
                    lens_opacity_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lens_rupture = table.Column<bool>(type: "bit", nullable: false),
                    lens_subluxation = table.Column<bool>(type: "bit", nullable: false),
                    lens_into_ac = table.Column<bool>(type: "bit", nullable: false),
                    lens_into_vitreous = table.Column<bool>(type: "bit", nullable: false),
                    lens_endophthalmitis = table.Column<bool>(type: "bit", nullable: false),
                    lens_foreign_body = table.Column<bool>(type: "bit", nullable: false),
                    lens_pigment_adhesion = table.Column<bool>(type: "bit", nullable: false),
                    iol_present = table.Column<bool>(type: "bit", nullable: false),
                    iol_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    iol_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lens_other_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    vitreous_clear = table.Column<bool>(type: "bit", nullable: false),
                    vitreous_opacity = table.Column<bool>(type: "bit", nullable: false),
                    vitreous_endophthalmitis = table.Column<bool>(type: "bit", nullable: false),
                    vitreous_hemorrhage = table.Column<bool>(type: "bit", nullable: false),
                    vitreous_organized = table.Column<bool>(type: "bit", nullable: false),
                    vitreous_pvd = table.Column<bool>(type: "bit", nullable: false),
                    vitreous_foreign_body = table.Column<bool>(type: "bit", nullable: false),
                    vitreous_tyndall = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    vitreous_other_note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_anterior_segment", x => x.id);
                    table.ForeignKey(
                        name: "fk_anterior_segment_eye_examinations_exam_id",
                        column: x => x.exam_id,
                        principalTable: "eye_examination",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lacrimal_system",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    exam_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    irrigation_free = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    irrigation_regurgitation_same = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    irrigation_regurgitation_opposite = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    irrigation_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    other_note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lacrimal_system", x => x.id);
                    table.ForeignKey(
                        name: "fk_lacrimal_system_eye_examination_exam_id",
                        column: x => x.exam_id,
                        principalTable: "eye_examination",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "posterior_segment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    exam_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    optic_disc_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    optic_disc_edema = table.Column<bool>(type: "bit", nullable: false),
                    optic_disc_atrophy = table.Column<bool>(type: "bit", nullable: false),
                    optic_disc_pallor = table.Column<bool>(type: "bit", nullable: false),
                    optic_disc_cup_disc_ratio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    optic_disc_rim_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    optic_disc_vessel_change = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    optic_disc_hemorrhage = table.Column<bool>(type: "bit", nullable: false),
                    optic_disc_peripapillary_atrophy = table.Column<bool>(type: "bit", nullable: false),
                    optic_disc_neovascularization = table.Column<bool>(type: "bit", nullable: false),
                    optic_disc_nv_size = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    optic_disc_not_visible = table.Column<bool>(type: "bit", nullable: false),
                    optic_disc_other_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    macula_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    macula_reflex_absent = table.Column<bool>(type: "bit", nullable: false),
                    macula_edema_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    macula_hole_degree = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    macula_hole_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    macula_scar = table.Column<bool>(type: "bit", nullable: false),
                    macula_serous_detachment = table.Column<bool>(type: "bit", nullable: false),
                    macula_rpe_detachment = table.Column<bool>(type: "bit", nullable: false),
                    macula_other_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    vessel_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    artery_occlusion_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    vein_occlusion_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    occlusion_edema = table.Column<bool>(type: "bit", nullable: false),
                    occlusion_ischemia = table.Column<bool>(type: "bit", nullable: false),
                    occlusion_mixed = table.Column<bool>(type: "bit", nullable: false),
                    capillary_phlebitis = table.Column<bool>(type: "bit", nullable: false),
                    retinal_neovascularization = table.Column<bool>(type: "bit", nullable: false),
                    choroidal_neovascularization_subfoveal = table.Column<bool>(type: "bit", nullable: false),
                    choroidal_neovascularization_extrafoveal = table.Column<bool>(type: "bit", nullable: false),
                    vessel_other_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    retina_hemorrhage_superficial = table.Column<bool>(type: "bit", nullable: false),
                    retina_hemorrhage_deep = table.Column<bool>(type: "bit", nullable: false),
                    retina_hemorrhage_choroidal = table.Column<bool>(type: "bit", nullable: false),
                    retina_exudate_hard = table.Column<bool>(type: "bit", nullable: false),
                    retina_exudate_cotton_wool = table.Column<bool>(type: "bit", nullable: false),
                    retina_edema = table.Column<bool>(type: "bit", nullable: false),
                    retina_degeneration_peripheral = table.Column<bool>(type: "bit", nullable: false),
                    retina_degeneration_central = table.Column<bool>(type: "bit", nullable: false),
                    retina_degeneration_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    chorioretinitis_active = table.Column<bool>(type: "bit", nullable: false),
                    chorioretinitis_scar = table.Column<bool>(type: "bit", nullable: false),
                    chorioretinitis_count = table.Column<int>(type: "int", nullable: true),
                    chorioretinitis_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    retinal_detachment = table.Column<bool>(type: "bit", nullable: false),
                    retinal_detachment_degree = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    retinal_tear = table.Column<bool>(type: "bit", nullable: false),
                    retinal_tear_count = table.Column<int>(type: "int", nullable: true),
                    retinal_tear_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    retinal_tear_shape = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    intraocular_foreign_body = table.Column<bool>(type: "bit", nullable: false),
                    foreign_body_size = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    foreign_body_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    retina_other_note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_posterior_segment", x => x.id);
                    table.ForeignKey(
                        name: "fk_posterior_segment_eye_examination_exam_id",
                        column: x => x.exam_id,
                        principalTable: "eye_examination",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refraction_record",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    exam_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sph_od = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    cyl_od = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    axis_od = table.Column<int>(type: "int", nullable: true),
                    add_od = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    sph_os = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    cyl_os = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    axis_os = table.Column<int>(type: "int", nullable: true),
                    add_os = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    pd_binocular = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
                    pd_od = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
                    pd_os = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
                    method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    pre_atropine = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    post_atropine = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    lens_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refraction_record", x => x.id);
                    table.ForeignKey(
                        name: "fk_refraction_record_eye_examination_exam_id",
                        column: x => x.exam_id,
                        principalTable: "eye_examination",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "glaucoma_drug_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    glaucoma_record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    drug_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    dosage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    duration = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    route = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    drug_count = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    change_reason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glaucoma_drug_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_glaucoma_drug_history_glaucoma_records_glaucoma_record_id",
                        column: x => x.glaucoma_record_id,
                        principalTable: "glaucoma_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "glaucoma_surgery_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    glaucoma_record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    attempt_number = table.Column<int>(type: "int", nullable: false),
                    procedure_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    procedure_date = table.Column<DateTime>(type: "date", nullable: true),
                    facility_level = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glaucoma_surgery_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_glaucoma_surgery_history_glaucoma_record_glaucoma_record_id",
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
                name: "ix_anterior_segment_exam_id",
                table: "anterior_segment",
                column: "exam_id");

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
                name: "ix_eye_examination_record_id",
                table: "eye_examination",
                column: "record_id",
                unique: true);

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
                name: "ix_glaucoma_drug_history_glaucoma_record_id",
                table: "glaucoma_drug_history",
                column: "glaucoma_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_glaucoma_record_record_id",
                table: "glaucoma_record",
                column: "record_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_glaucoma_surgery_history_glaucoma_record_id",
                table: "glaucoma_surgery_history",
                column: "glaucoma_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_lacrimal_system_exam_id",
                table: "lacrimal_system",
                column: "exam_id");

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
                name: "ix_posterior_segment_exam_id",
                table: "posterior_segment",
                column: "exam_id");

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
                name: "ix_refraction_record_exam_id",
                table: "refraction_record",
                column: "exam_id");

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
                name: "anterior_segment");

            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "clinic_registration_request");

            migrationBuilder.DropTable(
                name: "document_access_permission");

            migrationBuilder.DropTable(
                name: "emr_export_log");

            migrationBuilder.DropTable(
                name: "feedback");

            migrationBuilder.DropTable(
                name: "glasses_prescription");

            migrationBuilder.DropTable(
                name: "glaucoma_drug_history");

            migrationBuilder.DropTable(
                name: "glaucoma_surgery_history");

            migrationBuilder.DropTable(
                name: "lacrimal_system");

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
                name: "posterior_segment");

            migrationBuilder.DropTable(
                name: "prescription_item");

            migrationBuilder.DropTable(
                name: "queue");

            migrationBuilder.DropTable(
                name: "refraction_record");

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
                name: "eye_examination");

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
