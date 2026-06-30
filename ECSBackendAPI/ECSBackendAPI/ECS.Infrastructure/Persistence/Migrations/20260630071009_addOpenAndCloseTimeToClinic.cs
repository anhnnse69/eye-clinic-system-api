using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addOpenAndCloseTimeToClinic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "close_time",
                table: "clinic",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "open_time",
                table: "clinic",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "close_time",
                table: "clinic");

            migrationBuilder.DropColumn(
                name: "open_time",
                table: "clinic");
        }
    }
}
