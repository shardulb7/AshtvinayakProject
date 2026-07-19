using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AshtavinayakAPP.Models;

public partial class AshtvinayakTravelContext : DbContext
{
    public AshtvinayakTravelContext()
    {
    }

    public AshtvinayakTravelContext(DbContextOptions<AshtvinayakTravelContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<BookingSeat> BookingSeats { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<City> Cities { get; set; }

    public virtual DbSet<DropUp> DropUps { get; set; }

    public virtual DbSet<FamilyBooking> FamilyBookings { get; set; }

    public virtual DbSet<History> Histories { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Package> Packages { get; set; }

    public virtual DbSet<PickupPoint> PickupPoints { get; set; }

    public virtual DbSet<Seat> Seats { get; set; }

    public virtual DbSet<TourDestination> TourDestinations { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<Trip> Trips { get; set; }

    public virtual DbSet<TripRoute> TripRoutes { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Vehicle> Vehicles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=SQL9001.site4now.net;Initial Catalog=db_aafa7e_ashtavinayak;User Id=db_aafa7e_ashtavinayak_admin;Password=Yogesh@45");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PK_Bookings_73951ACD69F68546");

            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.Advance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BookingCode).HasMaxLength(50);
            entity.Property(e => e.BookingDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.BookingSeatId).HasColumnName("BookingSeatID");
            entity.Property(e => e.Droppoint).HasMaxLength(50);
            entity.Property(e => e.DroppointId).HasColumnName("DroppointID");
            entity.Property(e => e.PickUpPointName).HasMaxLength(500);
            entity.Property(e => e.PickupPointId).HasColumnName("PickupPointID");
            entity.Property(e => e.RoomType).HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TotalPayment).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TripId).HasColumnName("TripID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Trip).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.TripId)
                .HasConstraintName("FK_BookingsTripID_4D94879B");

            entity.HasOne(d => d.User).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Bookings_Users");
        });

        modelBuilder.Entity<BookingSeat>(entity =>
        {
            entity.HasKey(e => e.BookingSeatId).HasName("PK_BookingS_FA4B94265F1E2627");

            entity.Property(e => e.BookingSeatId).HasColumnName("BookingSeatID");
            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.SeatNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TripId).HasColumnName("TripID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Trip).WithMany(p => p.BookingSeats)
                .HasForeignKey(d => d.TripId)
                .HasConstraintName("FK_BookingSeats_Trip");

            entity.HasOne(d => d.User).WithMany(p => p.BookingSeats)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_BookingSeats_Bookings");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK_Categori_19093A2BEDFA3BB5");

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(4000);
            entity.Property(e => e.CityId).HasColumnName("CityID");

            entity.HasOne(d => d.City).WithMany(p => p.Categories)
                .HasForeignKey(d => d.CityId)
                .HasConstraintName("FK_Categories_Cities");

            entity.HasOne(d => d.TourDestination).WithMany(p => p.Categories)
                .HasForeignKey(d => d.TourDestinationId)
                .HasConstraintName("FK_Categories_TourDestination");
        });

        modelBuilder.Entity<City>(entity =>
        {
            entity.HasKey(e => e.CityId).HasName("PK__Cities__F2D21A962DFD6CCF");

            entity.Property(e => e.CityId).HasColumnName("CityID");
            entity.Property(e => e.CityName).HasMaxLength(100);
        });

        modelBuilder.Entity<DropUp>(entity =>
        {
            entity.HasKey(e => e.DroppointId);

            entity.ToTable("DropUp");

            entity.Property(e => e.DroppointId)
                .HasColumnName("DroppointId")
                .ValueGeneratedOnAdd();   // ✅ Makes it IDENTITY

            entity.Property(e => e.CityId).HasColumnName("CityID");

            entity.Property(e => e.DropPoint)
                .HasMaxLength(700);

            entity.HasOne(d => d.City)
                .WithMany(p => p.DropUps)
                .HasForeignKey(d => d.CityId)
                .HasConstraintName("FK_DropUp_Cities");
        });


        modelBuilder.Entity<FamilyBooking>(entity =>
        {
            entity.HasKey(e => e.FamilyId);

            entity.ToTable("FamilyBooking");

            entity.Property(e => e.FamilyId).HasColumnName("FamilyID");
            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.CarType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Time).HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.Property(e => e.Adults)
        .HasColumnName("Adults");

            entity.Property(e => e.Childwithseat)
                .HasColumnName("Childwithseat");

            entity.Property(e => e.Childwithoutseat)
                .HasColumnName("Childwithoutseat");

            entity.Property(e => e.BookingDate)
                .HasColumnType("datetime")
                .HasColumnName("BookingDate");

            entity.HasOne(d => d.Package).WithMany(p => p.FamilyBookings)
                .HasForeignKey(d => d.PackageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FamilyBooking_Packages");
        });

        modelBuilder.Entity<History>(entity =>
        {
            entity.ToTable("History");

            entity.Property(e => e.HistoryId).HasColumnName("HistoryID");
            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CityId).HasColumnName("CityID");
            entity.Property(e => e.PackageId).HasColumnName("PackageID");

            entity.HasOne(d => d.Booking).WithMany(p => p.Histories)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK_History_Bookings");

            entity.HasOne(d => d.Category).WithMany(p => p.Histories)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_History_Categories");

            entity.HasOne(d => d.City).WithMany(p => p.Histories)
                .HasForeignKey(d => d.CityId)
                .HasConstraintName("FK_History_Cities");

            entity.HasOne(d => d.Package).WithMany(p => p.Histories)
                .HasForeignKey(d => d.PackageId)
                .HasConstraintName("FK_History_Packages");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK_Notifica_20CF2E124C36D041");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NotificationDate).HasColumnType("datetime");
            entity.Property(e => e.TripId).HasColumnName("TripID");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Notifications_Trips");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.VehicleId)
                .HasConstraintName("FK_Notifications_Vehicles");
        });

        modelBuilder.Entity<Package>(entity =>
        {
            entity.HasKey(e => e.PackageId).HasName("PK_Packages_322035ECADB4B945");

            entity.Property(e => e.PackageId).HasColumnName("PackageID");
            entity.Property(e => e.CarType).HasMaxLength(500);
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CityId).HasColumnName("CityID");
            entity.Property(e => e.Duration).HasMaxLength(50);
            entity.Property(e => e.Exclusions).HasMaxLength(4000);
            entity.Property(e => e.Inclusions).HasMaxLength(4000);
            entity.Property(e => e.Itinerary).HasMaxLength(4000);
            entity.Property(e => e.PackageName).HasMaxLength(100);

            entity.HasOne(d => d.Category).WithMany(p => p.Packages)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_Packages_Categories");

            entity.HasOne(d => d.City).WithMany(p => p.Packages)
                .HasForeignKey(d => d.CityId)
                .HasConstraintName("FK_PackagesCityID_6A30C649");
        });

        modelBuilder.Entity<PickupPoint>(entity =>
        {
            entity.HasKey(e => e.PickupPointId).HasName("PK_PickupPo_195D7E80D3E6565C");

            entity.Property(e => e.PickupPointId).HasColumnName("PickupPointID");
            entity.Property(e => e.CityId).HasColumnName("CityID");
            entity.Property(e => e.PackageId).HasColumnName("PackageID");
            entity.Property(e => e.PickupPoint1)
                .HasMaxLength(200)
                .HasColumnName("PickupPoint");

            entity.HasOne(d => d.City).WithMany(p => p.PickupPoints)
                .HasForeignKey(d => d.CityId)
                .HasConstraintName("FK_PickupPoints_Cities");

            entity.HasOne(d => d.Package).WithMany(p => p.PickupPoints)
                .HasForeignKey(d => d.PackageId)
                .HasConstraintName("FK_PickupPoints_Packages");
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasKey(e => e.SeatId).HasName("PK_Seats_311713D32A6E6BA9");

            entity.Property(e => e.SeatId).HasColumnName("SeatID");
            entity.Property(e => e.PackageId).HasColumnName("PackageID");
            entity.Property(e => e.SeatNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TripId).HasColumnName("TripID");

            entity.HasOne(d => d.Package).WithMany(p => p.Seats)
                .HasForeignKey(d => d.PackageId)
                .HasConstraintName("FK_SeatsPackageID_66603565");

            entity.HasOne(d => d.Trip).WithMany(p => p.Seats)
                .HasForeignKey(d => d.TripId)
                .HasConstraintName("FK_Seats_Trip");
        });

        modelBuilder.Entity<TourDestination>(entity =>
        {
            entity.ToTable("TourDestination");

            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.DestinationName)
                .HasMaxLength(500)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__Transact__55433A4BA084671B");

            entity.Property(e => e.TransactionId).HasColumnName("TransactionID");
            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PaymentStatus).HasMaxLength(50);
            entity.Property(e => e.TransactionDate).HasColumnType("datetime");
            entity.Property(e => e.TransactionReference).HasMaxLength(500);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Booking).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TransactiBooki_5441852A");

            entity.HasOne(d => d.User).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Transactions_Users");
        });

        modelBuilder.Entity<Trip>(entity =>
        {
            entity.HasKey(e => e.TripId).HasName("PK_Trip_51DC711E381FE12F");

            entity.ToTable("Trip");

            entity.Property(e => e.TripId).HasColumnName("TripID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.PackageId).HasColumnName("PackageID");
            entity.Property(e => e.TourName).HasMaxLength(100);
            entity.Property(e => e.TripDate).HasColumnType("datetime");

            entity.HasOne(d => d.Category).WithMany(p => p.Trips)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_Trip_Categories");

            entity.HasOne(d => d.Package).WithMany(p => p.Trips)
                .HasForeignKey(d => d.PackageId)
                .HasConstraintName("FK_TripPackageID_693CA210");
        });

        modelBuilder.Entity<TripRoute>(entity =>
        {
            entity.HasKey(e => e.Trid).HasName("PK_TripRout_82F3BB35F91260E1");

            entity.ToTable("TripRoute");

            entity.Property(e => e.Trid).HasColumnName("TRID");
            entity.Property(e => e.CityId).HasColumnName("CityID");
            entity.Property(e => e.Day).HasMaxLength(50);
            entity.Property(e => e.PackageId).HasColumnName("PackageID");
            entity.Property(e => e.PointName)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.City).WithMany(p => p.TripRoutes)
                .HasForeignKey(d => d.CityId)
                .HasConstraintName("FK_TripRoute_Cities");

            entity.HasOne(d => d.Package).WithMany(p => p.TripRoutes)
                .HasForeignKey(d => d.PackageId)
                .HasConstraintName("FK_TripRoute_Packages");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK_Users_1788CCACA3D5D2B8");

            entity.HasIndex(e => e.Email, "UQ_Users_A9D1053426A34142").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.JwtToken).HasMaxLength(700);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(15);
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasDefaultValue("User");
            entity.Property(e => e.UserName).HasMaxLength(100);
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.VehicleId).HasName("PK_Vehicles_476B549245444C9B");

            entity.HasIndex(e => e.VehicleNumber, "UQ_Vehicles_ABAD8859CE494BD9").IsUnique();

            entity.Property(e => e.DriverContact).HasMaxLength(50);
            entity.Property(e => e.DriverName).HasMaxLength(100);
            entity.Property(e => e.TripId).HasColumnName("TripID");
            entity.Property(e => e.VehicleName).HasMaxLength(100);
            entity.Property(e => e.VehicleNumber).HasMaxLength(50);
            entity.Property(e => e.VehicleType).HasMaxLength(100);

            entity.HasOne(d => d.Trip).WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.TripId)
                .HasConstraintName("FK_Vehicles_Trips");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
