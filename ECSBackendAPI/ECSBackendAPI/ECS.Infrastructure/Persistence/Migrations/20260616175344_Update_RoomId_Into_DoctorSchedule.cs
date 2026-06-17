using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Update_RoomId_Into_DoctorSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "room_id",
                table: "doctor_schedule",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_doctor_schedule_room_id",
                table: "doctor_schedule",
                column: "room_id");

            migrationBuilder.AddForeignKey(
                name: "fk_doctor_schedule_facility_rooms_room_id",
                table: "doctor_schedule",
                column: "room_id",
                principalTable: "facility_room",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_doctor_schedule_facility_rooms_room_id",
                table: "doctor_schedule");

            migrationBuilder.DropIndex(
                name: "ix_doctor_schedule_room_id",
                table: "doctor_schedule");

            migrationBuilder.DropColumn(
                name: "room_id",
                table: "doctor_schedule");
        }
    }
}
