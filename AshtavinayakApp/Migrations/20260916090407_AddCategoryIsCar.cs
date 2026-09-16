using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AshtavinayakAPP.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryIsCar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCar",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCar",
                table: "Categories");
        }
    }
}
