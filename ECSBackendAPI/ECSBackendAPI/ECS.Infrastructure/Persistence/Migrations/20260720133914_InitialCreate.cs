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
                    is_published = table.Column<bool>(type: "bit", nullable: false),
                    is_publication_requested = table.Column<bool>(type: "bit", nullable: false),
                    publication_requested_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    rating_avg = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: true, defaultValue: 0m),
                    review_count = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    open_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    close_time = table.Column<TimeOnly>(type: "time", nullable: false),
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
                    reviewed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    provisioned_clinic_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clinic_registration_request", x => x.id);
                    table.ForeignKey(
                        name: "fk_clinic_registration_request_clinic_provisioned_clinic_id",
                        column: x => x.provisioned_clinic_id,
                        principalTable: "clinic",
                        principalColumn: "id");
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
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    family_history = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lifestyle_factors = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    current_eye_medications = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    previous_eye_surgery = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eye_vision_history = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    has_medical_demographics = table.Column<bool>(type: "bit", nullable: false)
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
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    room_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
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
                    table.ForeignKey(
                        name: "fk_doctor_schedule_facility_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "facility_room",
                        principalColumn: "id");
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
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    note_reason = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    chief_complaint = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    is_locked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    finalized_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    finalized_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    mongo_document_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    record_data_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    record_data_public_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    record_data_schema_version = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "1.0"),
                    record_data_version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    record_data_size_bytes = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    record_data_checksum = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
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
                    table.ForeignKey(
                        name: "fk_medical_record_users_finalized_by",
                        column: x => x.finalized_by,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "preliminary_diagnoses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    appointment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    patient_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    doctor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    urgency_level = table.Column<int>(type: "int", nullable: false),
                    pain_level = table.Column<int>(type: "int", nullable: true),
                    quick_visual_assessment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    has_vision_change = table.Column<bool>(type: "bit", nullable: false),
                    has_eye_redness = table.Column<bool>(type: "bit", nullable: false),
                    has_eye_discharge = table.Column<bool>(type: "bit", nullable: false),
                    has_light_sensitivity = table.Column<bool>(type: "bit", nullable: false),
                    has_eye_pain = table.Column<bool>(type: "bit", nullable: false),
                    has_headache = table.Column<bool>(type: "bit", nullable: false),
                    has_foreign_body = table.Column<bool>(type: "bit", nullable: false),
                    recommended_action = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_referral_needed = table.Column<bool>(type: "bit", nullable: false),
                    referral_to = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    follow_up_instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    check_in_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    triage_completed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_preliminary_diagnoses", x => x.id);
                    table.ForeignKey(
                        name: "fk_preliminary_diagnoses_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "appointment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_preliminary_diagnoses_doctor_profiles_doctor_id",
                        column: x => x.doctor_id,
                        principalTable: "doctor_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_preliminary_diagnoses_patient_profiles_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patient_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "ix_clinic_registration_request_provisioned_clinic_id",
                table: "clinic_registration_request",
                column: "provisioned_clinic_id");

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
                name: "ix_doctor_schedule_room_id",
                table: "doctor_schedule",
                column: "room_id");

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
                name: "ix_medical_record_appointment_id",
                table: "medical_record",
                column: "appointment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_medical_record_doctor_id",
                table: "medical_record",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_medical_record_finalized_by",
                table: "medical_record",
                column: "finalized_by");

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
                name: "ix_platform_config_config_key",
                table: "platform_config",
                column: "config_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_preliminary_diagnoses_appointment_id",
                table: "preliminary_diagnoses",
                column: "appointment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_preliminary_diagnoses_doctor_id",
                table: "preliminary_diagnoses",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_preliminary_diagnoses_patient_id",
                table: "preliminary_diagnoses",
                column: "patient_id");

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
                name: "ix_time_slot_schedule_id",
                table: "time_slot",
                column: "schedule_id");

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
                name: "feedback");

            migrationBuilder.DropTable(
                name: "medicine_catalog");

            migrationBuilder.DropTable(
                name: "notification");

            migrationBuilder.DropTable(
                name: "oct_result");

            migrationBuilder.DropTable(
                name: "platform_config");

            migrationBuilder.DropTable(
                name: "preliminary_diagnoses");

            migrationBuilder.DropTable(
                name: "queue");

            migrationBuilder.DropTable(
                name: "staff_clinic");

            migrationBuilder.DropTable(
                name: "ultrasound_eye");

            migrationBuilder.DropTable(
                name: "user_patient");

            migrationBuilder.DropTable(
                name: "visual_field_test");

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
                name: "facility_room");

            migrationBuilder.DropTable(
                name: "specialty");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropTable(
                name: "clinic");
        }
    }
}
