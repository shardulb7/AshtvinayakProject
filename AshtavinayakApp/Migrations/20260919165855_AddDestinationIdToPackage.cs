using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AshtavinayakAPP.Migrations
{
    /// <inheritdoc />
    public partial class AddDestinationIdToPackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DestinationId",
                table: "Packages",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DestinationId",
                table: "Packages");
        }
    }
}
