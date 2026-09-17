using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AshtavinayakAPP.Migrations
{
    /// <inheritdoc />
    public partial class AddSharingChargesAndDropUpTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DoubleSharingChargePerPerson",
                table: "Packages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SingleSharingChargePerPerson",
                table: "Packages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TripleSharingChargePerPerson",
                table: "Packages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "Time",
                table: "DropUp",
                type: "time",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoubleSharingChargePerPerson",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "SingleSharingChargePerPerson",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "TripleSharingChargePerPerson",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "Time",
                table: "DropUp");
        }
    }
}
