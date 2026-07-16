using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAndUpdateMedicalRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "eye_orbits");

            migrationBuilder.DropTable(
                name: "eye_sclera");

            migrationBuilder.DropTable(
                name: "glasses_prescription");

            migrationBuilder.DropTable(
                name: "glaucoma_history");

            migrationBuilder.DropTable(
                name: "lacrimal_record");

            migrationBuilder.DropTable(
                name: "medical_record_extras");

            migrationBuilder.DropTable(
                name: "pediatric_eye_record");

            migrationBuilder.DropTable(
                name: "prescription_item");

            migrationBuilder.DropTable(
                name: "strabismus_ptosis_record");

            migrationBuilder.DropTable(
                name: "trauma_surgeries");

            migrationBuilder.DropTable(
                name: "glaucoma_record");

            migrationBuilder.DropTable(
                name: "prescription");

            migrationBuilder.DropTable(
                name: "trauma_record");

            migrationBuilder.DropColumn(
                name: "diagnosis_comorbid",
                table: "medical_record");

            migrationBuilder.DropColumn(
                name: "diagnosis_differential",
                table: "medical_record");

            migrationBuilder.DropColumn(
                name: "diagnosis_main",
                table: "medical_record");

            migrationBuilder.DropColumn(
                name: "family_history",
                table: "medical_record");

            migrationBuilder.DropColumn(
                name: "illness_day_number",
                table: "medical_record");

            migrationBuilder.DropColumn(
                name: "medical_history",
                table: "medical_record");

            migrationBuilder.DropColumn(
                name: "personal_history_eye",
                table: "medical_record");

            migrationBuilder.DropColumn(
                name: "personal_history_systemic",
                table: "medical_record");

            migrationBuilder.DropColumn(
                name: "prognosis",
                table: "medical_record");

            migrationBuilder.DropColumn(
                name: "systemic_exam",
                table: "medical_record");

            migrationBuilder.DropColumn(
                name: "vital_blood_pressure",
                table: "medical_record");

            migrationBuilder.DropColumn(
                name: "vital_pulse",
                table: "medical_record");

            migrationBuilder.DropColumn(
                name: "vital_respiratory_rate",
                table: "medical_record");

            migrationBuilder.DropColumn(
                name: "vital_temperature",
                table: "medical_record");

            migrationBuilder.DropColumn(
                name: "vital_weight_kg",
                table: "medical_record");

            migrationBuilder.RenameColumn(
                name: "treatment_plan",
                table: "medical_record",
                newName: "summary");

            migrationBuilder.AlterColumn<string>(
                name: "chief_complaint",
                table: "medical_record",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "finalized_at",
                table: "medical_record",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "finalized_by",
                table: "medical_record",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "record_data_checksum",
                table: "medical_record",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "record_data_public_id",
                table: "medical_record",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "record_data_schema_version",
                table: "medical_record",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "1.0");

            migrationBuilder.AddColumn<long>(
                name: "record_data_size_bytes",
                table: "medical_record",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "record_data_url",
                table: "medical_record",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "record_data_version",
                table: "medical_record",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "medical_record",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "ix_medical_record_finalized_by",
                table: "medical_record",
                column: "finalized_by");

            migrationBuilder.AddForeignKey(
                name: "fk_medical_record_users_finalized_by",
                table: "medical_record",
                column: "finalized_by",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_medical_record_users_finalized_by",
                table: "medical_record");

            migrationBuilder.DropIndex(
                name: "ix_medical_record_finalized_by",
                table: "medical_record");

            migrationBuilder.DropColumn(
                name: "finalized_at",
                table: "medical_record");

            migrationBuilder.DropColumn(
                name: "finalized_by",
                table: "medical_record");

            migrationBuilder.DropColumn(
                name: "record_data_checksum",
                table: "medical_record");

            migrationBuilder.DropColumn(
                name: "record_data_public_id",
                table: "medical_record");

            migrationBuilder.DropColumn(
                name: "record_data_schema_version",
                table: "medical_record");

            migrationBuilder.DropColumn(
                name: "record_data_size_bytes",
                table: "medical_record");

            migrationBuilder.DropColumn(
                name: "record_data_url",
                table: "medical_record");

            migrationBuilder.DropColumn(
                name: "record_data_version",
                table: "medical_record");

            migrationBuilder.DropColumn(
                name: "status",
                table: "medical_record");

            migrationBuilder.RenameColumn(
                name: "summary",
                table: "medical_record",
                newName: "treatment_plan");

            migrationBuilder.AlterColumn<string>(
                name: "chief_complaint",
                table: "medical_record",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "diagnosis_comorbid",
                table: "medical_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "diagnosis_differential",
                table: "medical_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "diagnosis_main",
                table: "medical_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "family_history",
                table: "medical_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "illness_day_number",
                table: "medical_record",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "medical_history",
                table: "medical_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "personal_history_eye",
                table: "medical_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "personal_history_systemic",
                table: "medical_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "prognosis",
                table: "medical_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "systemic_exam",
                table: "medical_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vital_blood_pressure",
                table: "medical_record",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "vital_pulse",
                table: "medical_record",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "vital_respiratory_rate",
                table: "medical_record",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "vital_temperature",
                table: "medical_record",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "vital_weight_kg",
                table: "medical_record",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "eye_ac_iris",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ac_depth_herick = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ac_depth_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ac_exudate = table.Column<bool>(type: "bit", nullable: false),
                    ac_exudate_description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ac_flat = table.Column<bool>(type: "bit", nullable: false),
                    ac_foreign_body = table.Column<bool>(type: "bit", nullable: false),
                    ac_hemorrhage = table.Column<bool>(type: "bit", nullable: false),
                    ac_hemorrhage_level = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ac_iris_extras = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ac_lens_material = table.Column<bool>(type: "bit", nullable: false),
                    ac_other_findings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ac_pus = table.Column<bool>(type: "bit", nullable: false),
                    ac_pus_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ac_tyndall = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    angle_neovascularization = table.Column<bool>(type: "bit", nullable: false),
                    angle_other_findings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    angle_pigment = table.Column<bool>(type: "bit", nullable: false),
                    angle_synechiae = table.Column<bool>(type: "bit", nullable: false),
                    fundus_reflex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    iris_busacca_nodules = table.Column<bool>(type: "bit", nullable: false),
                    iris_ciliary_processes = table.Column<bool>(type: "bit", nullable: false),
                    iris_color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    iris_condition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    iris_degeneration = table.Column<bool>(type: "bit", nullable: false),
                    iris_koeppe_nodules = table.Column<bool>(type: "bit", nullable: false),
                    iris_loss = table.Column<bool>(type: "bit", nullable: false),
                    iris_neovascularization = table.Column<bool>(type: "bit", nullable: false),
                    iris_perforation = table.Column<bool>(type: "bit", nullable: false),
                    iris_prolapse = table.Column<bool>(type: "bit", nullable: false),
                    iris_root_tear = table.Column<bool>(type: "bit", nullable: false),
                    iris_root_tear_degree = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    iris_tumor_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pupil_diameter_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    pupil_dilated = table.Column<bool>(type: "bit", nullable: false),
                    pupil_irregular = table.Column<bool>(type: "bit", nullable: false),
                    pupil_light_reflex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pupil_paralyzed = table.Column<bool>(type: "bit", nullable: false),
                    pupil_ptdt_test = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pupil_reflex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pupil_round = table.Column<bool>(type: "bit", nullable: false),
                    pupil_sychiae = table.Column<bool>(type: "bit", nullable: false),
                    pupil_synechiae_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
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
                    band_keratopathy = table.Column<bool>(type: "bit", nullable: false),
                    clarity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cornea_extras = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    corneal_thickness = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    descemet_rupture = table.Column<bool>(type: "bit", nullable: false),
                    diameter_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    drug_deposit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    endothelium_folds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    epithelium_bullous = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    epithelium_edema_level = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    epithelium_loss = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    epithelium_punctate = table.Column<bool>(type: "bit", nullable: false),
                    guttata = table.Column<bool>(type: "bit", nullable: false),
                    keratic_precipitates = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    laceration = table.Column<bool>(type: "bit", nullable: false),
                    laceration_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    laceration_size = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    laceration_sutured = table.Column<bool>(type: "bit", nullable: true),
                    laceration_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    limbal_stem_deficiency = table.Column<bool>(type: "bit", nullable: false),
                    neovascularization = table.Column<bool>(type: "bit", nullable: false),
                    neovascularization_extent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    neovascularization_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    perforation = table.Column<bool>(type: "bit", nullable: false),
                    perforation_diameter_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    perforation_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    perforation_threatened = table.Column<bool>(type: "bit", nullable: false),
                    posterior_deposit_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    posterior_surface_deposit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    seidel_test = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sensation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    shape = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    size = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    stroma_edema = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    stroma_infiltrate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    stroma_thinning = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tissue_entrapped = table.Column<bool>(type: "bit", nullable: false),
                    ulcer = table.Column<bool>(type: "bit", nullable: false),
                    ulcer_description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ulcer_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ulcer_size = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    auto_refraction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cover_test_result = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eom_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    eom_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eyeball_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eyeball_texture = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    hirschberg_test = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    iop_method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    iop_mmhg = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    nystagmus = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    nystagmus_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    orbit_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    orbit_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pd = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
                    post_atropine = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    pre_atropine = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    prism_measurement = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    proptosis_mm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    pupil_accommodation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pupil_exam_result = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pupil_reflex_light = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pupil_relative_afferent_defect = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    refraction_add = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    refraction_axis = table.Column<int>(type: "int", nullable: true),
                    refraction_cyl = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    refraction_sph = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    retinoscopy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    strabismus_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    subjective_refraction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    va_corrected = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    va_near = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    va_pinhole = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    va_uncorrected = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    visual_field = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    chalazion = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_congestion_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    conjunctiva_discharge = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    conjunctiva_edema = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_follicle = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_hemorrhage = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_hemorrhage_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    conjunctiva_keratinization = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_laceration = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_laceration_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    conjunctiva_normal = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_other = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    conjunctiva_papilla = table.Column<bool>(type: "bit", nullable: false),
                    conjunctiva_scar = table.Column<bool>(type: "bit", nullable: false),
                    ectropion = table.Column<bool>(type: "bit", nullable: false),
                    entropion = table.Column<bool>(type: "bit", nullable: false),
                    entropion_pediatric = table.Column<bool>(type: "bit", nullable: false),
                    epicanthus = table.Column<bool>(type: "bit", nullable: false),
                    epicanthus_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eyelid_edema = table.Column<bool>(type: "bit", nullable: false),
                    eyelid_hemorrhage = table.Column<bool>(type: "bit", nullable: false),
                    eyelid_normal = table.Column<bool>(type: "bit", nullable: false),
                    eyelid_other = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fluorescein_stain = table.Column<bool>(type: "bit", nullable: false),
                    fornix_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    has_tumor = table.Column<bool>(type: "bit", nullable: false),
                    hordeolum = table.Column<bool>(type: "bit", nullable: false),
                    laceration = table.Column<bool>(type: "bit", nullable: false),
                    laceration_depth = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    laceration_extent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    laceration_sutured = table.Column<bool>(type: "bit", nullable: false),
                    laceration_unsutured = table.Column<bool>(type: "bit", nullable: false),
                    lacrimal_duct_cut = table.Column<bool>(type: "bit", nullable: false),
                    lacrimal_duct_cut_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lacrimal_duct_normal = table.Column<bool>(type: "bit", nullable: false),
                    lagophthalmos = table.Column<bool>(type: "bit", nullable: false),
                    pterygium = table.Column<bool>(type: "bit", nullable: false),
                    pterygium_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pterygium_size = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ptosis = table.Column<bool>(type: "bit", nullable: false),
                    ptosis_degree = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    scar = table.Column<bool>(type: "bit", nullable: false),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    symblepharon_height = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    symblepharon_width = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tumor_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tumor_nature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tumor_size = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    chorioretinitis_active = table.Column<bool>(type: "bit", nullable: false),
                    chorioretinitis_count = table.Column<int>(type: "int", nullable: true),
                    chorioretinitis_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    chorioretinitis_scar = table.Column<bool>(type: "bit", nullable: false),
                    choroidal_findings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    choroidal_neovascularization = table.Column<bool>(type: "bit", nullable: false),
                    choroidal_normal = table.Column<bool>(type: "bit", nullable: false),
                    disc_macula_extras = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    macula_condition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    macula_edema_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    macula_hemorrhage = table.Column<bool>(type: "bit", nullable: false),
                    macula_hole_degree = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    macula_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    macula_reflex_absent = table.Column<bool>(type: "bit", nullable: false),
                    macula_scar = table.Column<bool>(type: "bit", nullable: false),
                    macula_serous_detachment = table.Column<bool>(type: "bit", nullable: false),
                    optic_disc_atrophy = table.Column<bool>(type: "bit", nullable: false),
                    optic_disc_color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    optic_disc_cup_ratio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    optic_disc_edema = table.Column<bool>(type: "bit", nullable: false),
                    optic_disc_hemorrhage = table.Column<bool>(type: "bit", nullable: false),
                    optic_disc_neovascularization = table.Column<bool>(type: "bit", nullable: false),
                    optic_disc_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    optic_disc_not_visible = table.Column<bool>(type: "bit", nullable: false),
                    optic_disc_pallor = table.Column<bool>(type: "bit", nullable: false),
                    optic_disc_rim_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    optic_disc_rim_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    optic_disc_vessel_change = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
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
                    artery_occlusion_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    chorioretinitis_active = table.Column<bool>(type: "bit", nullable: false),
                    chorioretinitis_count = table.Column<int>(type: "int", nullable: true),
                    chorioretinitis_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    chorioretinitis_scar = table.Column<bool>(type: "bit", nullable: false),
                    choroidal_neovascularization = table.Column<bool>(type: "bit", nullable: false),
                    choroidal_neovessels_subretinal = table.Column<bool>(type: "bit", nullable: false),
                    degenerative_description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    degenerative_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    exudate_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    hemorrhage_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    intraocular_foreign_body = table.Column<bool>(type: "bit", nullable: false),
                    iofb_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    iofb_size = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    occlusion_edema = table.Column<bool>(type: "bit", nullable: false),
                    occlusion_ischemia = table.Column<bool>(type: "bit", nullable: false),
                    occlusion_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    retina_degeneration_central = table.Column<bool>(type: "bit", nullable: false),
                    retina_degeneration_peripheral = table.Column<bool>(type: "bit", nullable: false),
                    retina_edema = table.Column<bool>(type: "bit", nullable: false),
                    retina_exudate_cotton_wool = table.Column<bool>(type: "bit", nullable: false),
                    retina_exudate_hard = table.Column<bool>(type: "bit", nullable: false),
                    retina_hemorrhage_deep = table.Column<bool>(type: "bit", nullable: false),
                    retina_hemorrhage_superficial = table.Column<bool>(type: "bit", nullable: false),
                    retina_normal = table.Column<bool>(type: "bit", nullable: false),
                    retina_vessel_extras = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    retinal_condition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    retinal_detachment = table.Column<bool>(type: "bit", nullable: false),
                    retinal_detachment_level = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    retinal_tear = table.Column<bool>(type: "bit", nullable: false),
                    retinal_tear_count = table.Column<int>(type: "int", nullable: true),
                    retinal_tear_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    retinal_tear_morphology = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    vein_occlusion_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    vessel_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    vessel_status = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    lens_anterior_pigmentation = table.Column<bool>(type: "bit", nullable: false),
                    lens_clear = table.Column<bool>(type: "bit", nullable: false),
                    lens_into_anterior = table.Column<bool>(type: "bit", nullable: false),
                    lens_into_vitreous = table.Column<bool>(type: "bit", nullable: false),
                    lens_iol_position = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lens_iol_present = table.Column<bool>(type: "bit", nullable: false),
                    lens_iol_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lens_opacity_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lens_opacity_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lens_purulent = table.Column<bool>(type: "bit", nullable: false),
                    lens_rupture = table.Column<bool>(type: "bit", nullable: false),
                    lens_subluxation = table.Column<bool>(type: "bit", nullable: false),
                    lens_vitreous_extras = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    vitreous_clear = table.Column<bool>(type: "bit", nullable: false),
                    vitreous_foreign_body = table.Column<bool>(type: "bit", nullable: false),
                    vitreous_hemorrhage = table.Column<bool>(type: "bit", nullable: false),
                    vitreous_opacity = table.Column<bool>(type: "bit", nullable: false),
                    vitreous_organized = table.Column<bool>(type: "bit", nullable: false),
                    vitreous_purulent = table.Column<bool>(type: "bit", nullable: false),
                    vitreous_pvd = table.Column<bool>(type: "bit", nullable: false),
                    vitreous_tyndall = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                name: "eye_orbits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    medical_record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    eom_findings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eom_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eyeball_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eyeball_texture = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    orbital_foreign_body_description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    orbital_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    side = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_eye_orbits", x => x.id);
                    table.ForeignKey(
                        name: "fk_eye_orbits_medical_records_medical_record_id",
                        column: x => x.medical_record_id,
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
                    episcleritis = table.Column<bool>(type: "bit", nullable: false),
                    old_surgery_scar = table.Column<bool>(type: "bit", nullable: false),
                    sclera_ectasia = table.Column<bool>(type: "bit", nullable: false),
                    sclera_extras = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sclera_laceration = table.Column<bool>(type: "bit", nullable: false),
                    sclera_laceration_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sclera_laceration_size = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sclera_laceration_sutured = table.Column<bool>(type: "bit", nullable: true),
                    sclera_necrosis = table.Column<bool>(type: "bit", nullable: false),
                    sclera_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    sclera_thinning = table.Column<bool>(type: "bit", nullable: false),
                    sclera_tissue_entrapped = table.Column<bool>(type: "bit", nullable: false),
                    scleritis_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
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
                    doctor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    add_od = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    add_os = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    axis_od = table.Column<int>(type: "int", nullable: true),
                    axis_os = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    cyl_od = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    cyl_os = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    lens_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pd = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
                    sph_od = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    sph_os = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true)
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
                    ac_depth_herick = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ac_depth_smith = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    angle_findings = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    bleb_location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    bleb_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    corneal_thickness = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    corneal_transparency = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    eye_axial_length = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    eye_pain_level = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    family_glaucoma_relation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    family_has_glaucoma = table.Column<bool>(type: "bit", nullable: false),
                    follow_up_plan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fundus_macula_findings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fundus_retina_findings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    glaucoma_family_history = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    glaucoma_history_eye = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    glaucoma_medications = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    glaucoma_prior_facility = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    glaucoma_prior_treatment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    glaucoma_symptom_duration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    glaucoma_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    gonioscopy_od = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    gonioscopy_os = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    has_cnv = table.Column<bool>(type: "bit", nullable: false),
                    has_cardiovascular_disease = table.Column<bool>(type: "bit", nullable: false),
                    has_carotid_fistula = table.Column<bool>(type: "bit", nullable: false),
                    has_conjunctival_injection = table.Column<bool>(type: "bit", nullable: false),
                    has_diabetes = table.Column<bool>(type: "bit", nullable: false),
                    has_eyelid_swelling = table.Column<bool>(type: "bit", nullable: false),
                    has_filtering_bleb = table.Column<bool>(type: "bit", nullable: false),
                    has_hypertension = table.Column<bool>(type: "bit", nullable: false),
                    has_iris_neovascularization = table.Column<bool>(type: "bit", nullable: false),
                    has_optic_disc_hemorrhage = table.Column<bool>(type: "bit", nullable: false),
                    has_photophobia = table.Column<bool>(type: "bit", nullable: false),
                    has_redness = table.Column<bool>(type: "bit", nullable: false),
                    has_retinal_hemorrhage = table.Column<bool>(type: "bit", nullable: false),
                    has_rim_atrophy = table.Column<bool>(type: "bit", nullable: false),
                    has_scleral_thinning = table.Column<bool>(type: "bit", nullable: false),
                    has_tearing = table.Column<bool>(type: "bit", nullable: false),
                    history_eye = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    history_eye_surgery = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    iop_method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    iop_od = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    iop_os = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    iop_target_od = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    iop_target_os = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    iris_color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    iris_condition = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    lens_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    medication_change_reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    nerve_rim_od = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    nerve_rim_os = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    optic_disc_cup_ratio = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    optic_disc_description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    optic_disc_vessel_change = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    other_medications = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    other_systemic_disease = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    prior_eye_surgery_details = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    pupil_diameter = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    pupil_pigment_border = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    pupil_reflex_response = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    scleral_scar_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    stage_od = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    stage_os = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    steroid_prescribed = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    steroid_use = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    systemic_symptoms = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    treatment_plan_laser = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    treatment_plan_medication = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    treatment_plan_surgery = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    treatment_progress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    va_with_correction_od = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    va_with_correction_os = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    va_without_correction_od = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    va_without_correction_os = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    vision_progression = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    vision_symptoms = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
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
                    irrigation_free = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    irrigation_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    irrigation_regurgitation_opposite = table.Column<bool>(type: "bit", nullable: false),
                    irrigation_regurgitation_same = table.Column<bool>(type: "bit", nullable: false),
                    lacrimal_discharge = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lacrimal_other = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    nasolacrimal_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
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
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    age = table.Column<int>(type: "int", nullable: true),
                    care_plan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    diet_plan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    discharge_iop_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    discharge_iop_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    discharge_summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    discharge_va_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    discharge_va_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    final_diagnosis_cause = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    final_diagnosis_clinical = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    follow_up_plan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    glaucoma_summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    imaging_orders = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lab_orders = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ma_yeu_to = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pediatric_summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    required_tests = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    surgery_summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    trauma_summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    treatment_process = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                name: "pediatric_eye_record",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    acquired = table.Column<bool>(type: "bit", nullable: false),
                    acquired_onset = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    amblyopia_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    chief_symptoms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    congenital = table.Column<bool>(type: "bit", nullable: false),
                    entropion_od = table.Column<bool>(type: "bit", nullable: false),
                    epicanthus_od = table.Column<bool>(type: "bit", nullable: false),
                    eyeball_od_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eyeball_os_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eyeball_texture = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eyelid_tumor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eyelid_tumor_location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eyelid_tumor_size = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fixation_preference_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fixation_preference_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fundus_summary_od = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fundus_summary_os = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    general_health_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    intellectual_development_normal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    intellectual_development_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pediatric_development = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pediatric_pregnancy_history = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pregnancy_illness = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    pregnancy_illness_detail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    prior_treatment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ptosis_od = table.Column<bool>(type: "bit", nullable: false)
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
                    doctor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    acquired = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    acquired_onset = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    bell_phenomenon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    binocular_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    chief_ptosis = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    chief_strabismus = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    compensatory_head_posture = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    congenital = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    convergence_point = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    cover_test_result = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    diplopia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    eom_gaze_test = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    eom_internal_od = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    eom_internal_os = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    fixation_od = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    fixation_os = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    fusion_amplitude = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    hirschberg_after_atropine = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    hirschberg_before_atropine = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    levator_function_od = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    levator_function_os = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    marcus_gunn = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    nystagmus = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    nystagmus_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    palpebral_reflex_od = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    palpebral_reflex_os = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    prior_amblyopia_result = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    prior_amblyopia_treatment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    prior_surgery = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    prior_surgery_result = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    prism_distance = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    prism_down = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    prism_near = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    prism_up = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ptosis_degree_od = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ptosis_degree_os = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    pupil_shadow_test_od = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    pupil_shadow_test_os = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    refraction_post_atropine = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    refraction_pre_atropine = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    retinal_correspondence = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    strabismus_syndrome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    strabismus_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    synoptophore_objective = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    synoptophore_subjective = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    va_after_atropine_od = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    va_after_atropine_os = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    va_before_atropine_od = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    va_before_atropine_os = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
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
                    diagnosis_cause = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    diagnosis_clinical = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    injury_cause = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    injury_details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    injury_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    od_injuries = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    os_injuries = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    post_treatment_course = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    prior_treatment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    trauma_conclusion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    treatment_plan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    treatment_process = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                name: "glaucoma_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    glaucoma_record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    attempt_number = table.Column<int>(type: "int", nullable: true),
                    change_reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    dosage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    drug_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    duration = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    facility_level = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    history_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    procedure_date = table.Column<DateTime>(type: "date", nullable: true),
                    procedure_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    route = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true)
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
                    dosage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    duration_days = table.Column<int>(type: "int", nullable: true),
                    frequency = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    instruction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    medicine_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "trauma_surgeries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    trauma_record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    anesthesia_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    medical_record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    post_surgery_condition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    surgeon_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    surgery_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    surgery_description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    surgery_type = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trauma_surgeries", x => x.id);
                    table.ForeignKey(
                        name: "fk_trauma_surgeries_medical_records_medical_record_id",
                        column: x => x.medical_record_id,
                        principalTable: "medical_record",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_trauma_surgeries_trauma_records_trauma_record_id",
                        column: x => x.trauma_record_id,
                        principalTable: "trauma_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "ix_eye_orbits_medical_record_id",
                table: "eye_orbits",
                column: "medical_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_eye_sclera_record_id",
                table: "eye_sclera",
                column: "record_id");

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
                name: "ix_medical_record_extras_record_id",
                table: "medical_record_extras",
                column: "record_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_medical_record_extras_updated_by",
                table: "medical_record_extras",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "ix_pediatric_eye_record_record_id",
                table: "pediatric_eye_record",
                column: "record_id",
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
                name: "ix_strabismus_ptosis_record_record_id",
                table: "strabismus_ptosis_record",
                column: "record_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trauma_record_record_id",
                table: "trauma_record",
                column: "record_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trauma_surgeries_medical_record_id",
                table: "trauma_surgeries",
                column: "medical_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_trauma_surgeries_trauma_record_id",
                table: "trauma_surgeries",
                column: "trauma_record_id");
        }
    }
}
