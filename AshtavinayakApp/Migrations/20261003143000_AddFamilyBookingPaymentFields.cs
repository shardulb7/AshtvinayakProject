using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AshtavinayakAPP.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyBookingPaymentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use conditional SQL since these columns may already exist
            // (they were added manually via ALTER TABLE before this migration was created)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='FamilyBooking' AND COLUMN_NAME='PickupPoint')
                    ALTER TABLE FamilyBooking ADD PickupPoint NVARCHAR(500) NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='FamilyBooking' AND COLUMN_NAME='TotalPayment')
                    ALTER TABLE FamilyBooking ADD TotalPayment DECIMAL(18,2) NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='FamilyBooking' AND COLUMN_NAME='Advance')
                    ALTER TABLE FamilyBooking ADD Advance DECIMAL(18,2) NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PickupPoint",   table: "FamilyBooking");
            migrationBuilder.DropColumn(name: "TotalPayment",  table: "FamilyBooking");
            migrationBuilder.DropColumn(name: "Advance",       table: "FamilyBooking");
        }
    }
}
