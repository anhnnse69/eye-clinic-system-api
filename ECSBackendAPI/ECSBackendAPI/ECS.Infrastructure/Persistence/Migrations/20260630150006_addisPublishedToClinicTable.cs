using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addisPublishedToClinicTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "provisioned_clinic_id",
                table: "clinic_registration_request",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_published",
                table: "clinic",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_clinic_registration_request_provisioned_clinic_id",
                table: "clinic_registration_request",
                column: "provisioned_clinic_id");

            migrationBuilder.AddForeignKey(
                name: "fk_clinic_registration_request_clinic_provisioned_clinic_id",
                table: "clinic_registration_request",
                column: "provisioned_clinic_id",
                principalTable: "clinic",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_clinic_registration_request_clinic_provisioned_clinic_id",
                table: "clinic_registration_request");

            migrationBuilder.DropIndex(
                name: "ix_clinic_registration_request_provisioned_clinic_id",
                table: "clinic_registration_request");

            migrationBuilder.DropColumn(
                name: "provisioned_clinic_id",
                table: "clinic_registration_request");

            migrationBuilder.DropColumn(
                name: "is_published",
                table: "clinic");
        }
    }
}
