using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AshtavinayakAPP.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    CityID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Cities__F2D21A962DFD6CCF", x => x.CityID);
                });

            migrationBuilder.CreateTable(
                name: "TourDestination",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DestinationName = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourDestination", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "User"),
                    JwtToken = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users_1788CCACA3D5D2B8", x => x.UserID);
                });

            migrationBuilder.CreateTable(
                name: "DropUp",
                columns: table => new
                {
                    DroppointId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DropPoint = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: true),
                    CityID = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DropUp", x => x.DroppointId);
                    table.ForeignKey(
                        name: "FK_DropUp_Cities",
                        column: x => x.CityID,
                        principalTable: "Cities",
                        principalColumn: "CityID");
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CityID = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TourDestinationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categori_19093A2BEDFA3BB5", x => x.CategoryID);
                    table.ForeignKey(
                        name: "FK_Categories_Cities",
                        column: x => x.CityID,
                        principalTable: "Cities",
                        principalColumn: "CityID");
                    table.ForeignKey(
                        name: "FK_Categories_TourDestination",
                        column: x => x.TourDestinationId,
                        principalTable: "TourDestination",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Packages",
                columns: table => new
                {
                    PackageID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Duration = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CategoryID = table.Column<int>(type: "int", nullable: true),
                    CityID = table.Column<int>(type: "int", nullable: true),
                    Inclusions = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Exclusions = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AdultPrice = table.Column<int>(type: "int", nullable: true),
                    Child3To8YrswithSeat = table.Column<int>(type: "int", nullable: true),
                    Child3To8YrsWithoutSeat = table.Column<int>(type: "int", nullable: true),
                    IsCar = table.Column<bool>(type: "bit", nullable: false),
                    CarType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Itinerary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CarPackagePrice = table.Column<int>(type: "int", nullable: true),
                    CarTotalSeat = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    PkgPersonCount = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packages_322035ECADB4B945", x => x.PackageID);
                    table.ForeignKey(
                        name: "FK_PackagesCityID_6A30C649",
                        column: x => x.CityID,
                        principalTable: "Cities",
                        principalColumn: "CityID");
                    table.ForeignKey(
                        name: "FK_Packages_Categories",
                        column: x => x.CategoryID,
                        principalTable: "Categories",
                        principalColumn: "CategoryID");
                });

            migrationBuilder.CreateTable(
                name: "FamilyBooking",
                columns: table => new
                {
                    FamilyID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Time = table.Column<DateTime>(type: "datetime", nullable: false),
                    BookingID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    PackageId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Adults = table.Column<int>(type: "int", nullable: true),
                    Childwithseat = table.Column<int>(type: "int", nullable: true),
                    Childwithoutseat = table.Column<int>(type: "int", nullable: true),
                    BookingDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyBooking", x => x.FamilyID);
                    table.ForeignKey(
                        name: "FK_FamilyBooking_Packages",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "PackageID");
                    table.ForeignKey(
                        name: "FK_FamilyBooking_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PickupPoints",
                columns: table => new
                {
                    PickupPointID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CityID = table.Column<int>(type: "int", nullable: true),
                    PickupPoint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Time = table.Column<TimeOnly>(type: "time", nullable: true),
                    PackageID = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickupPo_195D7E80D3E6565C", x => x.PickupPointID);
                    table.ForeignKey(
                        name: "FK_PickupPoints_Cities",
                        column: x => x.CityID,
                        principalTable: "Cities",
                        principalColumn: "CityID");
                    table.ForeignKey(
                        name: "FK_PickupPoints_Packages",
                        column: x => x.PackageID,
                        principalTable: "Packages",
                        principalColumn: "PackageID");
                });

            migrationBuilder.CreateTable(
                name: "Trip",
                columns: table => new
                {
                    TripID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageID = table.Column<int>(type: "int", nullable: true),
                    TripDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    TotalSeats = table.Column<int>(type: "int", nullable: false),
                    AvailableSeats = table.Column<int>(type: "int", nullable: false),
                    TourName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CategoryID = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trip_51DC711E381FE12F", x => x.TripID);
                    table.ForeignKey(
                        name: "FK_TripPackageID_693CA210",
                        column: x => x.PackageID,
                        principalTable: "Packages",
                        principalColumn: "PackageID");
                    table.ForeignKey(
                        name: "FK_Trip_Categories",
                        column: x => x.CategoryID,
                        principalTable: "Categories",
                        principalColumn: "CategoryID");
                });

            migrationBuilder.CreateTable(
                name: "TripRoute",
                columns: table => new
                {
                    TRID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageID = table.Column<int>(type: "int", nullable: true),
                    PointName = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    Day = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CityID = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripRout_82F3BB35F91260E1", x => x.TRID);
                    table.ForeignKey(
                        name: "FK_TripRoute_Cities",
                        column: x => x.CityID,
                        principalTable: "Cities",
                        principalColumn: "CityID");
                    table.ForeignKey(
                        name: "FK_TripRoute_Packages",
                        column: x => x.PackageID,
                        principalTable: "Packages",
                        principalColumn: "PackageID");
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    BookingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    TripID = table.Column<int>(type: "int", nullable: true),
                    PickUpPointName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PickupPointID = table.Column<int>(type: "int", nullable: true),
                    BookingDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "Pending"),
                    TotalPayment = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Advance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BookingSeatID = table.Column<int>(type: "int", nullable: true),
                    DroppointID = table.Column<int>(type: "int", nullable: true),
                    BookingCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Droppoint = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RoomType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings_73951ACD69F68546", x => x.BookingID);
                    table.ForeignKey(
                        name: "FK_BookingsTripID_4D94879B",
                        column: x => x.TripID,
                        principalTable: "Trip",
                        principalColumn: "TripID");
                    table.ForeignKey(
                        name: "FK_Bookings_PickupPoints_PickupPointID",
                        column: x => x.PickupPointID,
                        principalTable: "PickupPoints",
                        principalColumn: "PickupPointID");
                    table.ForeignKey(
                        name: "FK_Bookings_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "BookingSeats",
                columns: table => new
                {
                    BookingSeatID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SeatNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Adults = table.Column<int>(type: "int", nullable: true),
                    Childwithseat = table.Column<int>(type: "int", nullable: true),
                    Childwithoutseat = table.Column<int>(type: "int", nullable: true),
                    TripID = table.Column<int>(type: "int", nullable: true),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    BookingID = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingS_FA4B94265F1E2627", x => x.BookingSeatID);
                    table.ForeignKey(
                        name: "FK_BookingSeats_Bookings",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_BookingSeats_Trip",
                        column: x => x.TripID,
                        principalTable: "Trip",
                        principalColumn: "TripID");
                });

            migrationBuilder.CreateTable(
                name: "Seats",
                columns: table => new
                {
                    SeatID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageID = table.Column<int>(type: "int", nullable: true),
                    SeatNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    TripID = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seats_311713D32A6E6BA9", x => x.SeatID);
                    table.ForeignKey(
                        name: "FK_SeatsPackageID_66603565",
                        column: x => x.PackageID,
                        principalTable: "Packages",
                        principalColumn: "PackageID");
                    table.ForeignKey(
                        name: "FK_Seats_Trip",
                        column: x => x.TripID,
                        principalTable: "Trip",
                        principalColumn: "TripID");
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    VehicleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VehicleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VehicleNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalSeats = table.Column<int>(type: "int", nullable: true),
                    DriverName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DriverContact = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TripID = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles_476B549245444C9B", x => x.VehicleId);
                    table.ForeignKey(
                        name: "FK_Vehicles_Trips",
                        column: x => x.TripID,
                        principalTable: "Trip",
                        principalColumn: "TripID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "History",
                columns: table => new
                {
                    HistoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CityID = table.Column<int>(type: "int", nullable: true),
                    PackageID = table.Column<int>(type: "int", nullable: true),
                    CategoryID = table.Column<int>(type: "int", nullable: true),
                    BookingID = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_History", x => x.HistoryID);
                    table.ForeignKey(
                        name: "FK_History_Bookings",
                        column: x => x.BookingID,
                        principalTable: "Bookings",
                        principalColumn: "BookingID");
                    table.ForeignKey(
                        name: "FK_History_Categories",
                        column: x => x.CategoryID,
                        principalTable: "Categories",
                        principalColumn: "CategoryID");
                    table.ForeignKey(
                        name: "FK_History_Cities",
                        column: x => x.CityID,
                        principalTable: "Cities",
                        principalColumn: "CityID");
                    table.ForeignKey(
                        name: "FK_History_Packages",
                        column: x => x.PackageID,
                        principalTable: "Packages",
                        principalColumn: "PackageID");
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    TransactionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingID = table.Column<int>(type: "int", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    TransactionReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Transact__55433A4BA084671B", x => x.TransactionID);
                    table.ForeignKey(
                        name: "FK_TransactiBooki_5441852A",
                        column: x => x.BookingID,
                        principalTable: "Bookings",
                        principalColumn: "BookingID");
                    table.ForeignKey(
                        name: "FK_Transactions_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TripID = table.Column<int>(type: "int", nullable: true),
                    NotificationMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NotificationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    VehicleId = table.Column<int>(type: "int", nullable: true),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifica_20CF2E124C36D041", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_Notifications_Trip_TripID",
                        column: x => x.TripID,
                        principalTable: "Trip",
                        principalColumn: "TripID");
                    table.ForeignKey(
                        name: "FK_Notifications_Trips",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_Vehicles",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "VehicleId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PickupPointID",
                table: "Bookings",
                column: "PickupPointID");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TripID",
                table: "Bookings",
                column: "TripID");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_UserID",
                table: "Bookings",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_BookingSeats_TripID",
                table: "BookingSeats",
                column: "TripID");

            migrationBuilder.CreateIndex(
                name: "IX_BookingSeats_UserID",
                table: "BookingSeats",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CityID",
                table: "Categories",
                column: "CityID");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_TourDestinationId",
                table: "Categories",
                column: "TourDestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_DropUp_CityID",
                table: "DropUp",
                column: "CityID");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyBooking_PackageId",
                table: "FamilyBooking",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyBooking_UserID",
                table: "FamilyBooking",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_History_BookingID",
                table: "History",
                column: "BookingID");

            migrationBuilder.CreateIndex(
                name: "IX_History_CategoryID",
                table: "History",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_History_CityID",
                table: "History",
                column: "CityID");

            migrationBuilder.CreateIndex(
                name: "IX_History_PackageID",
                table: "History",
                column: "PackageID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TripID",
                table: "Notifications",
                column: "TripID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserID",
                table: "Notifications",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_VehicleId",
                table: "Notifications",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_CategoryID",
                table: "Packages",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_CityID",
                table: "Packages",
                column: "CityID");

            migrationBuilder.CreateIndex(
                name: "IX_PickupPoints_CityID",
                table: "PickupPoints",
                column: "CityID");

            migrationBuilder.CreateIndex(
                name: "IX_PickupPoints_PackageID",
                table: "PickupPoints",
                column: "PackageID");

            migrationBuilder.CreateIndex(
                name: "IX_Seats_PackageID",
                table: "Seats",
                column: "PackageID");

            migrationBuilder.CreateIndex(
                name: "IX_Seats_TripID",
                table: "Seats",
                column: "TripID");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_BookingID",
                table: "Transactions",
                column: "BookingID");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserID",
                table: "Transactions",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Trip_CategoryID",
                table: "Trip",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Trip_PackageID",
                table: "Trip",
                column: "PackageID");

            migrationBuilder.CreateIndex(
                name: "IX_TripRoute_CityID",
                table: "TripRoute",
                column: "CityID");

            migrationBuilder.CreateIndex(
                name: "IX_TripRoute_PackageID",
                table: "TripRoute",
                column: "PackageID");

            migrationBuilder.CreateIndex(
                name: "UQ_Users_A9D1053426A34142",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_TripID",
                table: "Vehicles",
                column: "TripID");

            migrationBuilder.CreateIndex(
                name: "UQ_Vehicles_ABAD8859CE494BD9",
                table: "Vehicles",
                column: "VehicleNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingSeats");

            migrationBuilder.DropTable(
                name: "DropUp");

            migrationBuilder.DropTable(
                name: "FamilyBooking");

            migrationBuilder.DropTable(
                name: "History");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Seats");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "TripRoute");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "Trip");

            migrationBuilder.DropTable(
                name: "PickupPoints");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Packages");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropTable(
                name: "TourDestination");
        }
    }
}
