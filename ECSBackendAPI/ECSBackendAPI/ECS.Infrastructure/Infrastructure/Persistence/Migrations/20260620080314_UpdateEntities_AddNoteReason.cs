using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECS.Infrastructure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntities_AddNoteReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "prism_measurements",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "ptosis_od_degree",
                table: "strabismus_ptosis_record");

            migrationBuilder.RenameColumn(
                name: "ptosis_os_degree",
                table: "strabismus_ptosis_record",
                newName: "compensatory_head_posture");

            migrationBuilder.RenameColumn(
                name: "symptoms",
                table: "glaucoma_record",
                newName: "treatment_progress");

            migrationBuilder.RenameColumn(
                name: "history_systemic",
                table: "glaucoma_record",
                newName: "treatment_plan_surgery");

            migrationBuilder.RenameColumn(
                name: "history_steroid",
                table: "glaucoma_record",
                newName: "treatment_plan_medication");

            migrationBuilder.RenameColumn(
                name: "family_glaucoma",
                table: "glaucoma_record",
                newName: "treatment_plan_laser");

            migrationBuilder.RenameColumn(
                name: "bleb_os_status",
                table: "glaucoma_record",
                newName: "scleral_scar_location");

            migrationBuilder.RenameColumn(
                name: "bleb_od_status",
                table: "glaucoma_record",
                newName: "other_systemic_disease");

            migrationBuilder.AddColumn<string>(
                name: "diagnosis_cause",
                table: "trauma_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "diagnosis_clinical",
                table: "trauma_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "treatment_plan",
                table: "trauma_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "treatment_process",
                table: "trauma_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "strabismus_type",
                table: "strabismus_ptosis_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "refraction_pre_atropine",
                table: "strabismus_ptosis_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "refraction_post_atropine",
                table: "strabismus_ptosis_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "nystagmus_type",
                table: "strabismus_ptosis_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "marcus_gunn",
                table: "strabismus_ptosis_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "levator_function_os",
                table: "strabismus_ptosis_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "levator_function_od",
                table: "strabismus_ptosis_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "fixation_os",
                table: "strabismus_ptosis_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "fixation_od",
                table: "strabismus_ptosis_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "cover_test_result",
                table: "strabismus_ptosis_record",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "chief_strabismus",
                table: "strabismus_ptosis_record",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "chief_ptosis",
                table: "strabismus_ptosis_record",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "binocular_status",
                table: "strabismus_ptosis_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bell_phenomenon",
                table: "strabismus_ptosis_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "convergence_point",
                table: "strabismus_ptosis_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "diplopia",
                table: "strabismus_ptosis_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "eom_gaze_test",
                table: "strabismus_ptosis_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "eom_internal_od",
                table: "strabismus_ptosis_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "eom_internal_os",
                table: "strabismus_ptosis_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fusion_amplitude",
                table: "strabismus_ptosis_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hirschberg_after_atropine",
                table: "strabismus_ptosis_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hirschberg_before_atropine",
                table: "strabismus_ptosis_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "palpebral_reflex_od",
                table: "strabismus_ptosis_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "palpebral_reflex_os",
                table: "strabismus_ptosis_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "prior_amblyopia_result",
                table: "strabismus_ptosis_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "prior_surgery_result",
                table: "strabismus_ptosis_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "prism_distance",
                table: "strabismus_ptosis_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "prism_down",
                table: "strabismus_ptosis_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "prism_near",
                table: "strabismus_ptosis_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "prism_up",
                table: "strabismus_ptosis_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ptosis_degree_od",
                table: "strabismus_ptosis_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ptosis_degree_os",
                table: "strabismus_ptosis_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pupil_shadow_test_od",
                table: "strabismus_ptosis_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pupil_shadow_test_os",
                table: "strabismus_ptosis_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "retinal_correspondence",
                table: "strabismus_ptosis_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "synoptophore_objective",
                table: "strabismus_ptosis_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "synoptophore_subjective",
                table: "strabismus_ptosis_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "va_after_atropine_od",
                table: "strabismus_ptosis_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "va_after_atropine_os",
                table: "strabismus_ptosis_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "va_before_atropine_od",
                table: "strabismus_ptosis_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "va_before_atropine_os",
                table: "strabismus_ptosis_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "eyeball_os_status",
                table: "pediatric_eye_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "eyeball_texture",
                table: "pediatric_eye_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "eyelid_tumor",
                table: "pediatric_eye_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "eyelid_tumor_location",
                table: "pediatric_eye_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "eyelid_tumor_size",
                table: "pediatric_eye_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fixation_preference_od",
                table: "pediatric_eye_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fixation_preference_os",
                table: "pediatric_eye_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "general_health_status",
                table: "pediatric_eye_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "intellectual_development_status",
                table: "pediatric_eye_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lacrimal_discharge",
                table: "lacrimal_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nasolacrimal_status",
                table: "lacrimal_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "nerve_rim_os",
                table: "glaucoma_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "nerve_rim_od",
                table: "glaucoma_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "history_eye",
                table: "glaucoma_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "gonioscopy_os",
                table: "glaucoma_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "gonioscopy_od",
                table: "glaucoma_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "glaucoma_type",
                table: "glaucoma_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ac_depth_herick",
                table: "glaucoma_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ac_depth_smith",
                table: "glaucoma_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "angle_findings",
                table: "glaucoma_record",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bleb_location",
                table: "glaucoma_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bleb_status",
                table: "glaucoma_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "corneal_thickness",
                table: "glaucoma_record",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "corneal_transparency",
                table: "glaucoma_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "eye_axial_length",
                table: "glaucoma_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "eye_pain_level",
                table: "glaucoma_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "family_glaucoma_relation",
                table: "glaucoma_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "family_has_glaucoma",
                table: "glaucoma_record",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "follow_up_plan",
                table: "glaucoma_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fundus_macula_findings",
                table: "glaucoma_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fundus_retina_findings",
                table: "glaucoma_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "glaucoma_medications",
                table: "glaucoma_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "has_cardiovascular_disease",
                table: "glaucoma_record",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_carotid_fistula",
                table: "glaucoma_record",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_cnv",
                table: "glaucoma_record",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_conjunctival_injection",
                table: "glaucoma_record",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_diabetes",
                table: "glaucoma_record",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_eyelid_swelling",
                table: "glaucoma_record",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_filtering_bleb",
                table: "glaucoma_record",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_hypertension",
                table: "glaucoma_record",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_iris_neovascularization",
                table: "glaucoma_record",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_optic_disc_hemorrhage",
                table: "glaucoma_record",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_photophobia",
                table: "glaucoma_record",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_redness",
                table: "glaucoma_record",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_retinal_hemorrhage",
                table: "glaucoma_record",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_rim_atrophy",
                table: "glaucoma_record",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_scleral_thinning",
                table: "glaucoma_record",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_tearing",
                table: "glaucoma_record",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "history_eye_surgery",
                table: "glaucoma_record",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "iop_method",
                table: "glaucoma_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "iop_od",
                table: "glaucoma_record",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "iop_os",
                table: "glaucoma_record",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "iris_color",
                table: "glaucoma_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "iris_condition",
                table: "glaucoma_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lens_status",
                table: "glaucoma_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "medication_change_reason",
                table: "glaucoma_record",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "optic_disc_cup_ratio",
                table: "glaucoma_record",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "optic_disc_vessel_change",
                table: "glaucoma_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "other_medications",
                table: "glaucoma_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "prior_eye_surgery_details",
                table: "glaucoma_record",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pupil_diameter",
                table: "glaucoma_record",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pupil_pigment_border",
                table: "glaucoma_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pupil_reflex_response",
                table: "glaucoma_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "steroid_prescribed",
                table: "glaucoma_record",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "steroid_use",
                table: "glaucoma_record",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "systemic_symptoms",
                table: "glaucoma_record",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "va_with_correction_od",
                table: "glaucoma_record",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "va_with_correction_os",
                table: "glaucoma_record",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "va_without_correction_od",
                table: "glaucoma_record",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "va_without_correction_os",
                table: "glaucoma_record",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vision_progression",
                table: "glaucoma_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vision_symptoms",
                table: "glaucoma_record",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sclera_laceration_location",
                table: "eye_sclera",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sclera_laceration_size",
                table: "eye_sclera",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "sclera_tissue_entrapped",
                table: "eye_sclera",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "lens_anterior_pigmentation",
                table: "eye_lens_vitreous",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "lens_into_vitreous",
                table: "eye_lens_vitreous",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "lens_iol_position",
                table: "eye_lens_vitreous",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lens_opacity_location",
                table: "eye_lens_vitreous",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "lens_purulent",
                table: "eye_lens_vitreous",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "vitreous_foreign_body",
                table: "eye_lens_vitreous",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "vitreous_organized",
                table: "eye_lens_vitreous",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "vitreous_purulent",
                table: "eye_lens_vitreous",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "chorioretinitis_location",
                table: "eye_fundus_retina_vessel",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "choroidal_neovessels_subretinal",
                table: "eye_fundus_retina_vessel",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "degenerative_description",
                table: "eye_fundus_retina_vessel",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "degenerative_type",
                table: "eye_fundus_retina_vessel",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "exudate_type",
                table: "eye_fundus_retina_vessel",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hemorrhage_location",
                table: "eye_fundus_retina_vessel",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "iofb_location",
                table: "eye_fundus_retina_vessel",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "iofb_size",
                table: "eye_fundus_retina_vessel",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "occlusion_type",
                table: "eye_fundus_retina_vessel",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "retina_normal",
                table: "eye_fundus_retina_vessel",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "retinal_condition",
                table: "eye_fundus_retina_vessel",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "retinal_detachment_level",
                table: "eye_fundus_retina_vessel",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "retinal_tear_location",
                table: "eye_fundus_retina_vessel",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "retinal_tear_morphology",
                table: "eye_fundus_retina_vessel",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "choroidal_findings",
                table: "eye_fundus_disc_macula",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "choroidal_normal",
                table: "eye_fundus_disc_macula",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "macula_condition",
                table: "eye_fundus_disc_macula",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "optic_disc_color",
                table: "eye_fundus_disc_macula",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "optic_disc_rim_location",
                table: "eye_fundus_disc_macula",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "conjunctiva_hemorrhage_location",
                table: "eye_eyelid_conjunctiva",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "conjunctiva_laceration",
                table: "eye_eyelid_conjunctiva",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "conjunctiva_laceration_location",
                table: "eye_eyelid_conjunctiva",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "conjunctiva_normal",
                table: "eye_eyelid_conjunctiva",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "entropion_pediatric",
                table: "eye_eyelid_conjunctiva",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "epicanthus",
                table: "eye_eyelid_conjunctiva",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "epicanthus_type",
                table: "eye_eyelid_conjunctiva",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "eyelid_hemorrhage",
                table: "eye_eyelid_conjunctiva",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "eyelid_normal",
                table: "eye_eyelid_conjunctiva",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_tumor",
                table: "eye_eyelid_conjunctiva",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "laceration_extent",
                table: "eye_eyelid_conjunctiva",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "laceration_sutured",
                table: "eye_eyelid_conjunctiva",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "laceration_unsutured",
                table: "eye_eyelid_conjunctiva",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "lacrimal_duct_cut",
                table: "eye_eyelid_conjunctiva",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "lacrimal_duct_cut_location",
                table: "eye_eyelid_conjunctiva",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "lacrimal_duct_normal",
                table: "eye_eyelid_conjunctiva",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "pterygium",
                table: "eye_eyelid_conjunctiva",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "pterygium_location",
                table: "eye_eyelid_conjunctiva",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pterygium_size",
                table: "eye_eyelid_conjunctiva",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tumor_location",
                table: "eye_eyelid_conjunctiva",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tumor_nature",
                table: "eye_eyelid_conjunctiva",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tumor_size",
                table: "eye_eyelid_conjunctiva",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cover_test_result",
                table: "eye_exam_basic",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "eyeball_texture",
                table: "eye_exam_basic",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hirschberg_test",
                table: "eye_exam_basic",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "prism_measurement",
                table: "eye_exam_basic",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pupil_accommodation",
                table: "eye_exam_basic",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pupil_exam_result",
                table: "eye_exam_basic",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pupil_reflex_light",
                table: "eye_exam_basic",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pupil_relative_afferent_defect",
                table: "eye_exam_basic",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "strabismus_type",
                table: "eye_exam_basic",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "visual_field",
                table: "eye_exam_basic",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "corneal_thickness",
                table: "eye_cornea",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "drug_deposit",
                table: "eye_cornea",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "epithelium_edema_level",
                table: "eye_cornea",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "laceration_location",
                table: "eye_cornea",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "laceration_size",
                table: "eye_cornea",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "laceration_type",
                table: "eye_cornea",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "neovascularization_extent",
                table: "eye_cornea",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "neovascularization_location",
                table: "eye_cornea",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "perforation_location",
                table: "eye_cornea",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "posterior_deposit_location",
                table: "eye_cornea",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "posterior_surface_deposit",
                table: "eye_cornea",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seidel_test",
                table: "eye_cornea",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "tissue_entrapped",
                table: "eye_cornea",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ulcer_description",
                table: "eye_cornea",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ulcer_location",
                table: "eye_cornea",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ulcer_size",
                table: "eye_cornea",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ac_other_findings",
                table: "eye_ac_iris",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "angle_other_findings",
                table: "eye_ac_iris",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "iris_ciliary_processes",
                table: "eye_ac_iris",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "iris_condition",
                table: "eye_ac_iris",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "iris_tumor_location",
                table: "eye_ac_iris",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "pupil_dilated",
                table: "eye_ac_iris",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "pupil_paralyzed",
                table: "eye_ac_iris",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "pupil_ptdt_test",
                table: "eye_ac_iris",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pupil_synechiae_location",
                table: "eye_ac_iris",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "trauma_surgery",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    trauma_record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    surgery_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    surgery_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    surgery_description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    surgeon_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    anesthesia_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    post_surgery_condition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    medical_record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trauma_surgery", x => x.id);
                    table.ForeignKey(
                        name: "fk_trauma_surgery_medical_records_medical_record_id",
                        column: x => x.medical_record_id,
                        principalTable: "medical_record",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_trauma_surgery_trauma_records_trauma_record_id",
                        column: x => x.trauma_record_id,
                        principalTable: "trauma_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_trauma_surgery_medical_record_id",
                table: "trauma_surgery",
                column: "medical_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_trauma_surgery_trauma_record_id",
                table: "trauma_surgery",
                column: "trauma_record_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "trauma_surgery");

            migrationBuilder.DropColumn(
                name: "diagnosis_cause",
                table: "trauma_record");

            migrationBuilder.DropColumn(
                name: "diagnosis_clinical",
                table: "trauma_record");

            migrationBuilder.DropColumn(
                name: "treatment_plan",
                table: "trauma_record");

            migrationBuilder.DropColumn(
                name: "treatment_process",
                table: "trauma_record");

            migrationBuilder.DropColumn(
                name: "bell_phenomenon",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "convergence_point",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "diplopia",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "eom_gaze_test",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "eom_internal_od",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "eom_internal_os",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "fusion_amplitude",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "hirschberg_after_atropine",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "hirschberg_before_atropine",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "palpebral_reflex_od",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "palpebral_reflex_os",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "prior_amblyopia_result",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "prior_surgery_result",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "prism_distance",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "prism_down",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "prism_near",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "prism_up",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "ptosis_degree_od",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "ptosis_degree_os",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "pupil_shadow_test_od",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "pupil_shadow_test_os",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "retinal_correspondence",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "synoptophore_objective",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "synoptophore_subjective",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "va_after_atropine_od",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "va_after_atropine_os",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "va_before_atropine_od",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "va_before_atropine_os",
                table: "strabismus_ptosis_record");

            migrationBuilder.DropColumn(
                name: "eyeball_os_status",
                table: "pediatric_eye_record");

            migrationBuilder.DropColumn(
                name: "eyeball_texture",
                table: "pediatric_eye_record");

            migrationBuilder.DropColumn(
                name: "eyelid_tumor",
                table: "pediatric_eye_record");

            migrationBuilder.DropColumn(
                name: "eyelid_tumor_location",
                table: "pediatric_eye_record");

            migrationBuilder.DropColumn(
                name: "eyelid_tumor_size",
                table: "pediatric_eye_record");

            migrationBuilder.DropColumn(
                name: "fixation_preference_od",
                table: "pediatric_eye_record");

            migrationBuilder.DropColumn(
                name: "fixation_preference_os",
                table: "pediatric_eye_record");

            migrationBuilder.DropColumn(
                name: "general_health_status",
                table: "pediatric_eye_record");

            migrationBuilder.DropColumn(
                name: "intellectual_development_status",
                table: "pediatric_eye_record");

            migrationBuilder.DropColumn(
                name: "lacrimal_discharge",
                table: "lacrimal_record");

            migrationBuilder.DropColumn(
                name: "nasolacrimal_status",
                table: "lacrimal_record");

            migrationBuilder.DropColumn(
                name: "ac_depth_herick",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "ac_depth_smith",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "angle_findings",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "bleb_location",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "bleb_status",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "corneal_thickness",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "corneal_transparency",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "eye_axial_length",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "eye_pain_level",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "family_glaucoma_relation",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "family_has_glaucoma",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "follow_up_plan",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "fundus_macula_findings",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "fundus_retina_findings",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "glaucoma_medications",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "has_cardiovascular_disease",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "has_carotid_fistula",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "has_cnv",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "has_conjunctival_injection",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "has_diabetes",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "has_eyelid_swelling",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "has_filtering_bleb",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "has_hypertension",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "has_iris_neovascularization",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "has_optic_disc_hemorrhage",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "has_photophobia",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "has_redness",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "has_retinal_hemorrhage",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "has_rim_atrophy",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "has_scleral_thinning",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "has_tearing",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "history_eye_surgery",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "iop_method",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "iop_od",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "iop_os",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "iris_color",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "iris_condition",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "lens_status",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "medication_change_reason",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "optic_disc_cup_ratio",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "optic_disc_vessel_change",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "other_medications",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "prior_eye_surgery_details",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "pupil_diameter",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "pupil_pigment_border",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "pupil_reflex_response",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "steroid_prescribed",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "steroid_use",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "systemic_symptoms",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "va_with_correction_od",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "va_with_correction_os",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "va_without_correction_od",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "va_without_correction_os",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "vision_progression",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "vision_symptoms",
                table: "glaucoma_record");

            migrationBuilder.DropColumn(
                name: "sclera_laceration_location",
                table: "eye_sclera");

            migrationBuilder.DropColumn(
                name: "sclera_laceration_size",
                table: "eye_sclera");

            migrationBuilder.DropColumn(
                name: "sclera_tissue_entrapped",
                table: "eye_sclera");

            migrationBuilder.DropColumn(
                name: "lens_anterior_pigmentation",
                table: "eye_lens_vitreous");

            migrationBuilder.DropColumn(
                name: "lens_into_vitreous",
                table: "eye_lens_vitreous");

            migrationBuilder.DropColumn(
                name: "lens_iol_position",
                table: "eye_lens_vitreous");

            migrationBuilder.DropColumn(
                name: "lens_opacity_location",
                table: "eye_lens_vitreous");

            migrationBuilder.DropColumn(
                name: "lens_purulent",
                table: "eye_lens_vitreous");

            migrationBuilder.DropColumn(
                name: "vitreous_foreign_body",
                table: "eye_lens_vitreous");

            migrationBuilder.DropColumn(
                name: "vitreous_organized",
                table: "eye_lens_vitreous");

            migrationBuilder.DropColumn(
                name: "vitreous_purulent",
                table: "eye_lens_vitreous");

            migrationBuilder.DropColumn(
                name: "chorioretinitis_location",
                table: "eye_fundus_retina_vessel");

            migrationBuilder.DropColumn(
                name: "choroidal_neovessels_subretinal",
                table: "eye_fundus_retina_vessel");

            migrationBuilder.DropColumn(
                name: "degenerative_description",
                table: "eye_fundus_retina_vessel");

            migrationBuilder.DropColumn(
                name: "degenerative_type",
                table: "eye_fundus_retina_vessel");

            migrationBuilder.DropColumn(
                name: "exudate_type",
                table: "eye_fundus_retina_vessel");

            migrationBuilder.DropColumn(
                name: "hemorrhage_location",
                table: "eye_fundus_retina_vessel");

            migrationBuilder.DropColumn(
                name: "iofb_location",
                table: "eye_fundus_retina_vessel");

            migrationBuilder.DropColumn(
                name: "iofb_size",
                table: "eye_fundus_retina_vessel");

            migrationBuilder.DropColumn(
                name: "occlusion_type",
                table: "eye_fundus_retina_vessel");

            migrationBuilder.DropColumn(
                name: "retina_normal",
                table: "eye_fundus_retina_vessel");

            migrationBuilder.DropColumn(
                name: "retinal_condition",
                table: "eye_fundus_retina_vessel");

            migrationBuilder.DropColumn(
                name: "retinal_detachment_level",
                table: "eye_fundus_retina_vessel");

            migrationBuilder.DropColumn(
                name: "retinal_tear_location",
                table: "eye_fundus_retina_vessel");

            migrationBuilder.DropColumn(
                name: "retinal_tear_morphology",
                table: "eye_fundus_retina_vessel");

            migrationBuilder.DropColumn(
                name: "choroidal_findings",
                table: "eye_fundus_disc_macula");

            migrationBuilder.DropColumn(
                name: "choroidal_normal",
                table: "eye_fundus_disc_macula");

            migrationBuilder.DropColumn(
                name: "macula_condition",
                table: "eye_fundus_disc_macula");

            migrationBuilder.DropColumn(
                name: "optic_disc_color",
                table: "eye_fundus_disc_macula");

            migrationBuilder.DropColumn(
                name: "optic_disc_rim_location",
                table: "eye_fundus_disc_macula");

            migrationBuilder.DropColumn(
                name: "conjunctiva_hemorrhage_location",
                table: "eye_eyelid_conjunctiva");

            migrationBuilder.DropColumn(
                name: "conjunctiva_laceration",
                table: "eye_eyelid_conjunctiva");

            migrationBuilder.DropColumn(
                name: "conjunctiva_laceration_location",
                table: "eye_eyelid_conjunctiva");

            migrationBuilder.DropColumn(
                name: "conjunctiva_normal",
                table: "eye_eyelid_conjunctiva");

            migrationBuilder.DropColumn(
                name: "entropion_pediatric",
                table: "eye_eyelid_conjunctiva");

            migrationBuilder.DropColumn(
                name: "epicanthus",
                table: "eye_eyelid_conjunctiva");

            migrationBuilder.DropColumn(
                name: "epicanthus_type",
                table: "eye_eyelid_conjunctiva");

            migrationBuilder.DropColumn(
                name: "eyelid_hemorrhage",
                table: "eye_eyelid_conjunctiva");

            migrationBuilder.DropColumn(
                name: "eyelid_normal",
                table: "eye_eyelid_conjunctiva");

            migrationBuilder.DropColumn(
                name: "has_tumor",
                table: "eye_eyelid_conjunctiva");

            migrationBuilder.DropColumn(
                name: "laceration_extent",
                table: "eye_eyelid_conjunctiva");

            migrationBuilder.DropColumn(
                name: "laceration_sutured",
                table: "eye_eyelid_conjunctiva");

            migrationBuilder.DropColumn(
                name: "laceration_unsutured",
                table: "eye_eyelid_conjunctiva");

            migrationBuilder.DropColumn(
                name: "lacrimal_duct_cut",
                table: "eye_eyelid_conjunctiva");

            migrationBuilder.DropColumn(
                name: "lacrimal_duct_cut_location",
                table: "eye_eyelid_conjunctiva");

            migrationBuilder.DropColumn(
                name: "lacrimal_duct_normal",
                table: "eye_eyelid_conjunctiva");

            migrationBuilder.DropColumn(
                name: "pterygium",
                table: "eye_eyelid_conjunctiva");

            migrationBuilder.DropColumn(
                name: "pterygium_location",
                table: "eye_eyelid_conjunctiva");

            migrationBuilder.DropColumn(
                name: "pterygium_size",
                table: "eye_eyelid_conjunctiva");

            migrationBuilder.DropColumn(
                name: "tumor_location",
                table: "eye_eyelid_conjunctiva");

            migrationBuilder.DropColumn(
                name: "tumor_nature",
                table: "eye_eyelid_conjunctiva");

            migrationBuilder.DropColumn(
                name: "tumor_size",
                table: "eye_eyelid_conjunctiva");

            migrationBuilder.DropColumn(
                name: "cover_test_result",
                table: "eye_exam_basic");

            migrationBuilder.DropColumn(
                name: "eyeball_texture",
                table: "eye_exam_basic");

            migrationBuilder.DropColumn(
                name: "hirschberg_test",
                table: "eye_exam_basic");

            migrationBuilder.DropColumn(
                name: "prism_measurement",
                table: "eye_exam_basic");

            migrationBuilder.DropColumn(
                name: "pupil_accommodation",
                table: "eye_exam_basic");

            migrationBuilder.DropColumn(
                name: "pupil_exam_result",
                table: "eye_exam_basic");

            migrationBuilder.DropColumn(
                name: "pupil_reflex_light",
                table: "eye_exam_basic");

            migrationBuilder.DropColumn(
                name: "pupil_relative_afferent_defect",
                table: "eye_exam_basic");

            migrationBuilder.DropColumn(
                name: "strabismus_type",
                table: "eye_exam_basic");

            migrationBuilder.DropColumn(
                name: "visual_field",
                table: "eye_exam_basic");

            migrationBuilder.DropColumn(
                name: "corneal_thickness",
                table: "eye_cornea");

            migrationBuilder.DropColumn(
                name: "drug_deposit",
                table: "eye_cornea");

            migrationBuilder.DropColumn(
                name: "epithelium_edema_level",
                table: "eye_cornea");

            migrationBuilder.DropColumn(
                name: "laceration_location",
                table: "eye_cornea");

            migrationBuilder.DropColumn(
                name: "laceration_size",
                table: "eye_cornea");

            migrationBuilder.DropColumn(
                name: "laceration_type",
                table: "eye_cornea");

            migrationBuilder.DropColumn(
                name: "neovascularization_extent",
                table: "eye_cornea");

            migrationBuilder.DropColumn(
                name: "neovascularization_location",
                table: "eye_cornea");

            migrationBuilder.DropColumn(
                name: "perforation_location",
                table: "eye_cornea");

            migrationBuilder.DropColumn(
                name: "posterior_deposit_location",
                table: "eye_cornea");

            migrationBuilder.DropColumn(
                name: "posterior_surface_deposit",
                table: "eye_cornea");

            migrationBuilder.DropColumn(
                name: "seidel_test",
                table: "eye_cornea");

            migrationBuilder.DropColumn(
                name: "tissue_entrapped",
                table: "eye_cornea");

            migrationBuilder.DropColumn(
                name: "ulcer_description",
                table: "eye_cornea");

            migrationBuilder.DropColumn(
                name: "ulcer_location",
                table: "eye_cornea");

            migrationBuilder.DropColumn(
                name: "ulcer_size",
                table: "eye_cornea");

            migrationBuilder.DropColumn(
                name: "ac_other_findings",
                table: "eye_ac_iris");

            migrationBuilder.DropColumn(
                name: "angle_other_findings",
                table: "eye_ac_iris");

            migrationBuilder.DropColumn(
                name: "iris_ciliary_processes",
                table: "eye_ac_iris");

            migrationBuilder.DropColumn(
                name: "iris_condition",
                table: "eye_ac_iris");

            migrationBuilder.DropColumn(
                name: "iris_tumor_location",
                table: "eye_ac_iris");

            migrationBuilder.DropColumn(
                name: "pupil_dilated",
                table: "eye_ac_iris");

            migrationBuilder.DropColumn(
                name: "pupil_paralyzed",
                table: "eye_ac_iris");

            migrationBuilder.DropColumn(
                name: "pupil_ptdt_test",
                table: "eye_ac_iris");

            migrationBuilder.DropColumn(
                name: "pupil_synechiae_location",
                table: "eye_ac_iris");

            migrationBuilder.RenameColumn(
                name: "compensatory_head_posture",
                table: "strabismus_ptosis_record",
                newName: "ptosis_os_degree");

            migrationBuilder.RenameColumn(
                name: "treatment_progress",
                table: "glaucoma_record",
                newName: "symptoms");

            migrationBuilder.RenameColumn(
                name: "treatment_plan_surgery",
                table: "glaucoma_record",
                newName: "history_systemic");

            migrationBuilder.RenameColumn(
                name: "treatment_plan_medication",
                table: "glaucoma_record",
                newName: "history_steroid");

            migrationBuilder.RenameColumn(
                name: "treatment_plan_laser",
                table: "glaucoma_record",
                newName: "family_glaucoma");

            migrationBuilder.RenameColumn(
                name: "scleral_scar_location",
                table: "glaucoma_record",
                newName: "bleb_os_status");

            migrationBuilder.RenameColumn(
                name: "other_systemic_disease",
                table: "glaucoma_record",
                newName: "bleb_od_status");

            migrationBuilder.AlterColumn<string>(
                name: "strabismus_type",
                table: "strabismus_ptosis_record",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "refraction_pre_atropine",
                table: "strabismus_ptosis_record",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "refraction_post_atropine",
                table: "strabismus_ptosis_record",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "nystagmus_type",
                table: "strabismus_ptosis_record",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "marcus_gunn",
                table: "strabismus_ptosis_record",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "levator_function_os",
                table: "strabismus_ptosis_record",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "levator_function_od",
                table: "strabismus_ptosis_record",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "fixation_os",
                table: "strabismus_ptosis_record",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "fixation_od",
                table: "strabismus_ptosis_record",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "cover_test_result",
                table: "strabismus_ptosis_record",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "chief_strabismus",
                table: "strabismus_ptosis_record",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "chief_ptosis",
                table: "strabismus_ptosis_record",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "binocular_status",
                table: "strabismus_ptosis_record",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "prism_measurements",
                table: "strabismus_ptosis_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ptosis_od_degree",
                table: "strabismus_ptosis_record",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "nerve_rim_os",
                table: "glaucoma_record",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "nerve_rim_od",
                table: "glaucoma_record",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "history_eye",
                table: "glaucoma_record",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "gonioscopy_os",
                table: "glaucoma_record",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "gonioscopy_od",
                table: "glaucoma_record",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "glaucoma_type",
                table: "glaucoma_record",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
