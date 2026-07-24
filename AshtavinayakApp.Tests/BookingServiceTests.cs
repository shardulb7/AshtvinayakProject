using AshtavinayakAPP.Models;
using AshtavinayakAPP.Services.BookingSrc;
using AshtavinayakApp.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AshtavinayakApp.Tests;

public class BookingServiceTests
{
    private static AshtvinayakTravelContext MakeContext()
    {
        // The InMemory provider doesn't support real transactions; BookingService always opens
        // one, so suppress the warning it'd otherwise throw on — it becomes a harmless no-op.
        var options = new DbContextOptionsBuilder<AshtvinayakTravelContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AshtvinayakTravelContext(options);
    }

    private static (AshtvinayakTravelContext Context, BookingService Service, Package Package, Trip Trip, User User) SeedAndBuildService()
    {
        var context = MakeContext();

        var package = new Package
        {
            PackageName = "Test Package",
            Duration = "2D",
            AdultPrice = 1000,
            Child3To8YrswithSeat = 800,
            Child3To8YrsWithoutSeat = 500,
            IsCar = false,
            IsDeleted = false
        };
        context.Packages.Add(package);
        context.SaveChanges();

        var trip = new Trip
        {
            PackageId = package.PackageId,
            TripDate = DateTime.UtcNow.AddDays(10),
            TotalSeats = 10,
            AvailableSeats = 10,
            TourName = "Test Trip",
            IsDeleted = false
        };
        context.Trips.Add(trip);
        context.SaveChanges();

        var user = new User
        {
            UserName = "Test User",
            Email = "testuser@example.com",
            PhoneNumber = "9000000000",
            PasswordHash = "irrelevant",
            Role = "User",
            IsDeleted = false
        };
        context.Users.Add(user);
        context.SaveChanges();

        context.Seats.AddRange(
            new Seat { TripId = trip.TripId, PackageId = package.PackageId, SeatNumber = "A1", IsAvailable = true, IsDeleted = false },
            new Seat { TripId = trip.TripId, PackageId = package.PackageId, SeatNumber = "A2", IsAvailable = true, IsDeleted = false }
        );
        context.SaveChanges();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["SmsGateway:BookingTemplateId"] = "template" })
            .Build();

        var service = new BookingService(context, new FakeSmsService(), config, NullLogger<BookingService>.Instance);
        return (context, service, package, trip, user);
    }

    private static BookingRequestDto MakeRequest(Trip trip, User user, int adults, decimal tamperedTotalPayment, string seatNumber = "A1")
        => new()
        {
            UserId = user.UserId,
            TripId = trip.TripId,
            Status = "Confirmed",
            TotalPayment = tamperedTotalPayment,
            Advance = 0,
            SeatNumbers = new List<string> { seatNumber },
            Adults = adults,
            Childwithseat = 0,
            Childwithoutseat = 0,
            CategoryId = 0,
            CityId = 0,
            PackageId = trip.PackageId ?? 0,
            Transaction = new Transaction
            {
                PaymentMethod = "Cash",
                TransactionDate = DateTime.UtcNow,
                Amount = 0,
                PaymentStatus = "Pending",
                TransactionReference = "TESTREF"
            }
        };

    [Fact]
    public async Task CreateBookingWithSeatsAsync_ComputesTotalPaymentServerSide_IgnoringTamperedClientValue()
    {
        var (context, service, _, trip, user) = SeedAndBuildService();
        var request = MakeRequest(trip, user, adults: 2, tamperedTotalPayment: 1m); // client claims ₹1

        var (success, _, data) = await service.CreateBookingWithSeatsAsync(request);

        Assert.True(success);
        var bookingId = (int)data!.GetType().GetProperty("BookingId")!.GetValue(data)!;
        var booking = await context.Bookings.FindAsync(bookingId);

        Assert.NotNull(booking);
        Assert.Equal(2000m, booking!.TotalPayment); // 2 adults * 1000/adult — not the tampered ₹1
    }

    [Fact]
    public async Task CreateBookingWithSeatsAsync_RejectsWhenComputedTotalIsZero()
    {
        var (_, service, _, trip, user) = SeedAndBuildService();
        var request = MakeRequest(trip, user, adults: 0, tamperedTotalPayment: 500m);

        var (success, message, _) = await service.CreateBookingWithSeatsAsync(request);

        Assert.False(success);
        Assert.Contains("pricing", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateBookingWithSeatsAsync_WithAgent_ComputesCommissionAndSnapshotsIt()
    {
        var (context, service, _, trip, user) = SeedAndBuildService();
        var request = MakeRequest(trip, user, adults: 2, tamperedTotalPayment: 1m);

        var (success, _, data) = await service.CreateBookingWithSeatsAsync(request, agentId: 1, commissionPercentage: 10m);

        Assert.True(success);
        var bookingIdProp = data!.GetType().GetProperty("BookingId")!;
        var bookingId = (int)bookingIdProp.GetValue(data)!;

        var booking = await context.Bookings.FindAsync(bookingId);
        Assert.NotNull(booking);
        Assert.Equal(1, booking!.AgentId);
        Assert.Equal(2000m, booking.TotalPayment); // 2 adults * 1000
        Assert.Equal(10m, booking.CommissionPercentage);
        Assert.Equal(200m, booking.CommissionAmount); // 10% of 2000
    }

    [Fact]
    public async Task CreateBookingWithSeatsAsync_WithoutAgent_LeavesCommissionFieldsNull()
    {
        var (context, service, _, trip, user) = SeedAndBuildService();
        var request = MakeRequest(trip, user, adults: 1, tamperedTotalPayment: 1m, seatNumber: "A2");

        var (success, _, data) = await service.CreateBookingWithSeatsAsync(request);
        Assert.True(success);

        var bookingId = (int)data!.GetType().GetProperty("BookingId")!.GetValue(data)!;
        var booking = await context.Bookings.FindAsync(bookingId);

        Assert.Null(booking!.AgentId);
        Assert.Null(booking.CommissionPercentage);
        Assert.Null(booking.CommissionAmount);
    }

    [Fact]
    public async Task CreateBookingWithSeatsAsync_RejectsAlreadyBookedSeat()
    {
        var (context, service, _, trip, user) = SeedAndBuildService();

        // Pre-existing booking for seat A1 on this trip.
        context.BookingSeats.Add(new BookingSeat { TripId = trip.TripId, SeatNumber = "A1", UserId = user.UserId, IsDeleted = false });
        await context.SaveChangesAsync();

        var request = MakeRequest(trip, user, adults: 1, tamperedTotalPayment: 1m, seatNumber: "A1");
        var (success, message, _) = await service.CreateBookingWithSeatsAsync(request);

        Assert.False(success);
        Assert.Contains("already booked", message, StringComparison.OrdinalIgnoreCase);
    }
}
