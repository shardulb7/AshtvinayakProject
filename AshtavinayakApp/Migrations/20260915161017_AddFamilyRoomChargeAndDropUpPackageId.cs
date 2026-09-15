using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AshtavinayakAPP.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyRoomChargeAndDropUpPackageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FamilyRoomChargePerPerson",
                table: "Packages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PackageId",
                table: "DropUp",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FamilyRoomChargePerPerson",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "PackageId",
                table: "DropUp");
        }
    }
}
