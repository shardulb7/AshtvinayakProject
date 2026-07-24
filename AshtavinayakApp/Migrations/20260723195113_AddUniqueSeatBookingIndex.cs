using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AshtavinayakAPP.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueSeatBookingIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BookingSeats_TripID",
                table: "BookingSeats");

            migrationBuilder.CreateIndex(
                name: "UQ_BookingSeats_TripId_SeatNumber_Active",
                table: "BookingSeats",
                columns: new[] { "TripID", "SeatNumber" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [TripID] IS NOT NULL AND [SeatNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_BookingSeats_TripId_SeatNumber_Active",
                table: "BookingSeats");

            migrationBuilder.CreateIndex(
                name: "IX_BookingSeats_TripID",
                table: "BookingSeats",
                column: "TripID");
        }
    }
}
