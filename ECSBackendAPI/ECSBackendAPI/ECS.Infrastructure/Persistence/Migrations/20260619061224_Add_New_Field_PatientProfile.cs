using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_New_Field_PatientProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "current_eye_medications",
                table: "patient_profile",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "eye_vision_history",
                table: "patient_profile",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "family_history",
                table: "patient_profile",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "has_medical_demographics",
                table: "patient_profile",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "lifestyle_factors",
                table: "patient_profile",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "previous_eye_surgery",
                table: "patient_profile",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "current_eye_medications",
                table: "patient_profile");

            migrationBuilder.DropColumn(
                name: "eye_vision_history",
                table: "patient_profile");

            migrationBuilder.DropColumn(
                name: "family_history",
                table: "patient_profile");

            migrationBuilder.DropColumn(
                name: "has_medical_demographics",
                table: "patient_profile");

            migrationBuilder.DropColumn(
                name: "lifestyle_factors",
                table: "patient_profile");

            migrationBuilder.DropColumn(
                name: "previous_eye_surgery",
                table: "patient_profile");
        }
    }
}
