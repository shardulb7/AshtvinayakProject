using AshtavinayakAPP.Models;
using Microsoft.EntityFrameworkCore;

namespace AshtavinayakAPP.Data;

/// <summary>
/// Provides idempotent seed data for the AshtavinayakTravelApp database.
///
/// Seeds the minimum reference data required for the booking flow to function end-to-end:
///   3 Cities, 2 TourDestinations, 4 Categories, 4 Packages, 16 TripRoutes,
///   6 DropUps, 6 PickupPoints, 2 Trips, 60 Seats.
///
/// Seed order strictly respects all FK constraints:
///   Cities → TourDestinations → Categories → Packages
///   → TripRoutes → DropUps → PickupPoints → Trips → Seats
///
/// Every section is guarded by an <c>Any()</c> check — safe to call on every
/// application startup without creating duplicate records.
///
/// Exceptions are caught and logged; the application will still start even if
/// seeding fails (e.g., database unavailable during development).
/// </summary>
internal static class DatabaseSeeder
{
    /// <summary>
    /// Entry point called from <c>Program.cs</c> after <c>builder.Build()</c>.
    /// Creates an async DI scope, resolves the <see cref="AshtvinayakTravelContext"/>,
    /// and delegates to each entity seed method in FK-safe order.
    /// </summary>
    /// <param name="serviceProvider">The root <see cref="IServiceProvider"/> from the built WebApplication.</param>
    /// <param name="logger">Logger for structured seeder output.</param>
    public static async Task SeedAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AshtvinayakTravelContext>();

        try
        {
            logger.LogInformation("[Seeder] Starting database seed check...");

            await SeedCitiesAsync(context, logger);
            await SeedTourDestinationsAsync(context, logger);
            await SeedCategoriesAsync(context, logger);
            await SeedPackagesAsync(context, logger);
            await SeedTripRoutesAsync(context, logger);
            await SeedDropUpsAsync(context, logger);
            await SeedPickupPointsAsync(context, logger);
            await SeedTripsAsync(context, logger);
            await SeedSeatsAsync(context, logger);
            await SeedCommissionSettingAsync(context, logger);

            logger.LogInformation("[Seeder] Database seed check completed successfully.");
        }
        catch (Exception ex)
        {
            // Log and swallow — the app continues to run; admin can seed manually.
            logger.LogError(ex, "[Seeder] An error occurred while seeding the database. The application will still start.");
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 1. CITIES  (no FK dependencies)
    // ─────────────────────────────────────────────────────────────

    private static async Task SeedCitiesAsync(AshtvinayakTravelContext context, ILogger logger)
    {
        if (await context.Cities.AnyAsync())
        {
            logger.LogInformation("[Seeder] Cities already seeded — skipping.");
            return;
        }

        var cities = new List<City>
        {
            new() { CityName = "Pune",   IsDeleted = false },
            new() { CityName = "Mumbai", IsDeleted = false },
            new() { CityName = "Nashik", IsDeleted = false }
        };

        context.Cities.AddRange(cities);
        await context.SaveChangesAsync();
        logger.LogInformation("[Seeder] Seeded {Count} cities.", cities.Count);
    }

    // ─────────────────────────────────────────────────────────────
    // 2. TOUR DESTINATIONS  (no FK dependencies)
    // ─────────────────────────────────────────────────────────────

    private static async Task SeedTourDestinationsAsync(AshtvinayakTravelContext context, ILogger logger)
    {
        if (await context.TourDestinations.AnyAsync())
        {
            logger.LogInformation("[Seeder] TourDestinations already seeded — skipping.");
            return;
        }

        var destinations = new List<TourDestination>
        {
            new()
            {
                DestinationName = "Ashtavinayak",
                Description     = "The sacred circuit of the eight Ganesh temples of Maharashtra, " +
                                  "each with a unique self-manifested (swayambhu) idol of Lord Ganesha.",
                IsDeleted       = false
            },
            new()
            {
                DestinationName = "Jyotirlinga",
                Description     = "The twelve divine abodes of Lord Shiva across India, " +
                                  "each considered supremely holy and powerful.",
                IsDeleted       = false
            }
        };

        context.TourDestinations.AddRange(destinations);
        await context.SaveChangesAsync();
        logger.LogInformation("[Seeder] Seeded {Count} tour destinations.", destinations.Count);
    }

    // ─────────────────────────────────────────────────────────────
    // 3. CATEGORIES  (FK: Cities, TourDestinations)
    // ─────────────────────────────────────────────────────────────

    private static async Task SeedCategoriesAsync(AshtvinayakTravelContext context, ILogger logger)
    {
        if (await context.Categories.AnyAsync())
        {
            logger.LogInformation("[Seeder] Categories already seeded — skipping.");
            return;
        }

        var pune         = await context.Cities.FirstAsync(c => c.CityName == "Pune");
        var mumbai       = await context.Cities.FirstAsync(c => c.CityName == "Mumbai");
        var nashik       = await context.Cities.FirstAsync(c => c.CityName == "Nashik");
        var ashtavinayak = await context.TourDestinations.FirstAsync(t => t.DestinationName == "Ashtavinayak");
        var jyotirlinga  = await context.TourDestinations.FirstAsync(t => t.DestinationName == "Jyotirlinga");

        var categories = new List<Category>
        {
            new() { CategoryName = "Ashtavinayak Darshan", CityId = pune.CityId,   TourDestinationId = ashtavinayak.Id, IsDeleted = false },
            new() { CategoryName = "Ashtavinayak Darshan", CityId = mumbai.CityId, TourDestinationId = ashtavinayak.Id, IsDeleted = false },
            new() { CategoryName = "Ashtavinayak Darshan", CityId = nashik.CityId, TourDestinationId = ashtavinayak.Id, IsDeleted = false },
            new() { CategoryName = "Jyotirlinga Darshan",  CityId = pune.CityId,   TourDestinationId = jyotirlinga.Id,  IsDeleted = false }
        };

        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();
        logger.LogInformation("[Seeder] Seeded {Count} categories.", categories.Count);
    }

    // ─────────────────────────────────────────────────────────────
    // 4. PACKAGES  (FK: Categories, Cities)
    // ─────────────────────────────────────────────────────────────

    private static async Task SeedPackagesAsync(AshtvinayakTravelContext context, ILogger logger)
    {
        if (await context.Packages.AnyAsync())
        {
            logger.LogInformation("[Seeder] Packages already seeded — skipping.");
            return;
        }

        var pune   = await context.Cities.FirstAsync(c => c.CityName == "Pune");
        var mumbai = await context.Cities.FirstAsync(c => c.CityName == "Mumbai");

        // Resolve categories by city to avoid ambiguous matches on CategoryName alone
        var puneAshtaCat   = await context.Categories
            .FirstAsync(c => c.CityId == pune.CityId   && c.CategoryName == "Ashtavinayak Darshan");
        var mumbaiAshtaCat = await context.Categories
            .FirstAsync(c => c.CityId == mumbai.CityId && c.CategoryName == "Ashtavinayak Darshan");
        var jyotiCat       = await context.Categories
            .FirstAsync(c => c.CategoryName == "Jyotirlinga Darshan");

        var packages = new List<Package>
        {
            // ── Bus Package: Pune → Ashtavinayak ──────────────────────────────
            new()
            {
                PackageName             = "Ashtavinayak Darshan 2N/3D (Bus)",
                Duration                = "2 Nights / 3 Days",
                CategoryId              = puneAshtaCat.CategoryId,
                CityId                  = pune.CityId,
                AdultPrice              = 4500,
                Child3To8YrswithSeat    = 3500,
                Child3To8YrsWithoutSeat = 2500,
                IsCar                   = false,
                PkgPersonCount          = 30,
                Inclusions              = "AC Sleeper Bus | Breakfast & Dinner | Hotel Stay (AC) | All Temple Entry & Darshan",
                Exclusions              = "Lunch | Personal Expenses | Camera Charges at Temples | Travel Insurance",
                Itinerary               = "Day 1: Pune → Morgaon → Siddhatek | Day 2: Pali → Mahad → Theur | Day 3: Lenyadri → Ozar → Ranjangaon → Pune",
                IsDeleted               = false
            },
            // ── Car Package: Pune → Ashtavinayak ──────────────────────────────
            new()
            {
                PackageName             = "Ashtavinayak Darshan 2N/3D (Car)",
                Duration                = "2 Nights / 3 Days",
                CategoryId              = puneAshtaCat.CategoryId,
                CityId                  = pune.CityId,
                IsCar                   = true,
                CarType                 = "Innova / Ertiga",
                CarPackagePrice         = 12000,
                CarTotalSeat            = 6,
                Inclusions              = "Private AC Car | Breakfast | All Temple Entry & Darshan",
                Exclusions              = "Lunch | Dinner | Hotel Stay | Personal Expenses",
                Itinerary               = "Day 1: Pune → Morgaon → Siddhatek | Day 2: Pali → Mahad → Theur | Day 3: Lenyadri → Ozar → Ranjangaon → Pune",
                IsDeleted               = false
            },
            // ── Bus Package: Mumbai → Ashtavinayak ────────────────────────────
            new()
            {
                PackageName             = "Ashtavinayak Darshan 2N/3D (Bus - Mumbai)",
                Duration                = "2 Nights / 3 Days",
                CategoryId              = mumbaiAshtaCat.CategoryId,
                CityId                  = mumbai.CityId,
                AdultPrice              = 5500,
                Child3To8YrswithSeat    = 4500,
                Child3To8YrsWithoutSeat = 3000,
                IsCar                   = false,
                PkgPersonCount          = 30,
                Inclusions              = "AC Sleeper Bus | Breakfast & Dinner | Hotel Stay (AC) | All Temple Entry & Darshan",
                Exclusions              = "Lunch | Personal Expenses | Camera Charges at Temples | Travel Insurance",
                Itinerary               = "Day 1: Mumbai → Morgaon → Siddhatek | Day 2: Pali → Mahad → Theur | Day 3: Lenyadri → Ozar → Ranjangaon → Mumbai",
                IsDeleted               = false
            },
            // ── Bus Package: Pune → Jyotirlinga ───────────────────────────────
            new()
            {
                PackageName             = "Jyotirlinga Darshan 3N/4D (Bus)",
                Duration                = "3 Nights / 4 Days",
                CategoryId              = jyotiCat.CategoryId,
                CityId                  = pune.CityId,
                AdultPrice              = 6500,
                Child3To8YrswithSeat    = 5500,
                Child3To8YrsWithoutSeat = 4000,
                IsCar                   = false,
                PkgPersonCount          = 30,
                Inclusions              = "AC Sleeper Bus | Breakfast & Dinner | Hotel Stay (AC) | All Temple Entry",
                Exclusions              = "Lunch | Personal Expenses | Prasad | Travel Insurance",
                Itinerary               = "Day 1: Pune → Trimbakeshwar → Bhimashankar | Day 2: Grishneshwar → Shirdi | Day 3: Aundha Nagnath → Parali Vaijnath | Day 4: Return to Pune",
                IsDeleted               = false
            }
        };

        context.Packages.AddRange(packages);
        await context.SaveChangesAsync();
        logger.LogInformation("[Seeder] Seeded {Count} packages.", packages.Count);
    }

    // ─────────────────────────────────────────────────────────────
    // 5. TRIP ROUTES  (FK: Cities, Packages)
    // ─────────────────────────────────────────────────────────────

    private static async Task SeedTripRoutesAsync(AshtvinayakTravelContext context, ILogger logger)
    {
        if (await context.TripRoutes.AnyAsync())
        {
            logger.LogInformation("[Seeder] TripRoutes already seeded — skipping.");
            return;
        }

        var pune         = await context.Cities.FirstAsync(c => c.CityName == "Pune");
        var mumbai       = await context.Cities.FirstAsync(c => c.CityName == "Mumbai");
        var pkgPuneBus   = await context.Packages.FirstAsync(p => p.PackageName == "Ashtavinayak Darshan 2N/3D (Bus)");
        var pkgPuneCar   = await context.Packages.FirstAsync(p => p.PackageName == "Ashtavinayak Darshan 2N/3D (Car)");
        var pkgMumbaiBus = await context.Packages.FirstAsync(p => p.PackageName == "Ashtavinayak Darshan 2N/3D (Bus - Mumbai)");
        var pkgJyoti     = await context.Packages.FirstAsync(p => p.PackageName == "Jyotirlinga Darshan 3N/4D (Bus)");

        var tripRoutes = new List<TripRoute>
        {
            // Ashtavinayak Bus — Pune (4 days)
            new() { PackageId = pkgPuneBus.PackageId, CityId = pune.CityId, Day = "Day 1", PointName = "Pune Departure (05:00 AM) → Morgaon: Shri Mayureshwar Temple → Siddhatek: Shri Siddhivinayak Temple → Hotel Check-in", IsDeleted = false },
            new() { PackageId = pkgPuneBus.PackageId, CityId = pune.CityId, Day = "Day 2", PointName = "Pali: Shri Ballaleshwar Temple → Mahad: Shri Varadvinayak Temple → Theur: Shri Chintamani Temple → Hotel", IsDeleted = false },
            new() { PackageId = pkgPuneBus.PackageId, CityId = pune.CityId, Day = "Day 3", PointName = "Lenyadri: Shri Girijatmaj Temple → Ozar: Shri Vighnahar Temple → Ranjangaon: Shri Mahaganapati Temple", IsDeleted = false },
            new() { PackageId = pkgPuneBus.PackageId, CityId = pune.CityId, Day = "Day 4", PointName = "Return to Pune — Estimated Arrival 02:00 PM", IsDeleted = false },

            // Ashtavinayak Car — Pune (4 days)
            new() { PackageId = pkgPuneCar.PackageId, CityId = pune.CityId, Day = "Day 1", PointName = "Pune Departure (06:00 AM) → Morgaon: Shri Mayureshwar Temple → Siddhatek: Shri Siddhivinayak Temple", IsDeleted = false },
            new() { PackageId = pkgPuneCar.PackageId, CityId = pune.CityId, Day = "Day 2", PointName = "Pali: Shri Ballaleshwar Temple → Mahad: Shri Varadvinayak Temple → Theur: Shri Chintamani Temple", IsDeleted = false },
            new() { PackageId = pkgPuneCar.PackageId, CityId = pune.CityId, Day = "Day 3", PointName = "Lenyadri: Shri Girijatmaj Temple → Ozar: Shri Vighnahar Temple → Ranjangaon: Shri Mahaganapati Temple", IsDeleted = false },
            new() { PackageId = pkgPuneCar.PackageId, CityId = pune.CityId, Day = "Day 4", PointName = "Return to Pune — Estimated Arrival 01:00 PM", IsDeleted = false },

            // Ashtavinayak Bus — Mumbai (4 days)
            new() { PackageId = pkgMumbaiBus.PackageId, CityId = mumbai.CityId, Day = "Day 1", PointName = "Mumbai Departure (05:00 AM) → Morgaon: Shri Mayureshwar Temple → Siddhatek: Shri Siddhivinayak Temple → Hotel", IsDeleted = false },
            new() { PackageId = pkgMumbaiBus.PackageId, CityId = mumbai.CityId, Day = "Day 2", PointName = "Pali: Shri Ballaleshwar Temple → Mahad: Shri Varadvinayak Temple → Theur: Shri Chintamani Temple → Hotel", IsDeleted = false },
            new() { PackageId = pkgMumbaiBus.PackageId, CityId = mumbai.CityId, Day = "Day 3", PointName = "Lenyadri: Shri Girijatmaj Temple → Ozar: Shri Vighnahar Temple → Ranjangaon: Shri Mahaganapati Temple", IsDeleted = false },
            new() { PackageId = pkgMumbaiBus.PackageId, CityId = mumbai.CityId, Day = "Day 4", PointName = "Return to Mumbai — Estimated Arrival 04:00 PM", IsDeleted = false },

            // Jyotirlinga Bus — Pune (4 days)
            new() { PackageId = pkgJyoti.PackageId, CityId = pune.CityId, Day = "Day 1", PointName = "Pune Departure (04:00 AM) → Trimbakeshwar: Shri Trimbakeshwar Temple → Bhimashankar: Shri Bhimashankar Temple → Hotel", IsDeleted = false },
            new() { PackageId = pkgJyoti.PackageId, CityId = pune.CityId, Day = "Day 2", PointName = "Grishneshwar: Shri Grishneshwar Temple (Aurangabad) → Shirdi: Shri Sai Baba Temple → Hotel", IsDeleted = false },
            new() { PackageId = pkgJyoti.PackageId, CityId = pune.CityId, Day = "Day 3", PointName = "Aundha Nagnath: Shri Aundha Nagnath Temple → Parali: Shri Vaijnath Temple → Hotel", IsDeleted = false },
            new() { PackageId = pkgJyoti.PackageId, CityId = pune.CityId, Day = "Day 4", PointName = "Return to Pune — Estimated Arrival 12:00 PM", IsDeleted = false }
        };

        context.TripRoutes.AddRange(tripRoutes);
        await context.SaveChangesAsync();
        logger.LogInformation("[Seeder] Seeded {Count} trip routes.", tripRoutes.Count);
    }

    // ─────────────────────────────────────────────────────────────
    // 6. DROP-OFF POINTS  (FK: Cities)
    // ─────────────────────────────────────────────────────────────

    private static async Task SeedDropUpsAsync(AshtvinayakTravelContext context, ILogger logger)
    {
        if (await context.DropUps.AnyAsync())
        {
            logger.LogInformation("[Seeder] DropUps already seeded — skipping.");
            return;
        }

        var pune   = await context.Cities.FirstAsync(c => c.CityName == "Pune");
        var mumbai = await context.Cities.FirstAsync(c => c.CityName == "Mumbai");
        var nashik = await context.Cities.FirstAsync(c => c.CityName == "Nashik");

        var dropUps = new List<DropUp>
        {
            new() { DropPoint = "Swargate Bus Stand, Pune",     CityId = pune.CityId,   IsDeleted = false },
            new() { DropPoint = "Shivajinagar Bus Stand, Pune", CityId = pune.CityId,   IsDeleted = false },
            new() { DropPoint = "Dadar Bus Terminal, Mumbai",   CityId = mumbai.CityId, IsDeleted = false },
            new() { DropPoint = "Borivali Bus Stop, Mumbai",    CityId = mumbai.CityId, IsDeleted = false },
            new() { DropPoint = "Nashik CBS Bus Stand",         CityId = nashik.CityId, IsDeleted = false },
            new() { DropPoint = "Deolali Camp, Nashik",         CityId = nashik.CityId, IsDeleted = false }
        };

        context.DropUps.AddRange(dropUps);
        await context.SaveChangesAsync();
        logger.LogInformation("[Seeder] Seeded {Count} drop-off points.", dropUps.Count);
    }

    // ─────────────────────────────────────────────────────────────
    // 7. PICKUP POINTS  (FK: Cities, Packages)
    // ─────────────────────────────────────────────────────────────

    private static async Task SeedPickupPointsAsync(AshtvinayakTravelContext context, ILogger logger)
    {
        if (await context.PickupPoints.AnyAsync())
        {
            logger.LogInformation("[Seeder] PickupPoints already seeded — skipping.");
            return;
        }

        var pune         = await context.Cities.FirstAsync(c => c.CityName == "Pune");
        var mumbai       = await context.Cities.FirstAsync(c => c.CityName == "Mumbai");
        var pkgPuneBus   = await context.Packages.FirstAsync(p => p.PackageName == "Ashtavinayak Darshan 2N/3D (Bus)");
        var pkgMumbaiBus = await context.Packages.FirstAsync(p => p.PackageName == "Ashtavinayak Darshan 2N/3D (Bus - Mumbai)");
        var pkgJyoti     = await context.Packages.FirstAsync(p => p.PackageName == "Jyotirlinga Darshan 3N/4D (Bus)");

        var pickupPoints = new List<PickupPoint>
        {
            // Package 1 — Ashtavinayak Bus (Pune)
            new() { PickupPoint1 = "Swargate, Pune",     CityId = pune.CityId,   PackageId = pkgPuneBus.PackageId,   Time = new TimeOnly(5,  0), IsDeleted = false },
            new() { PickupPoint1 = "Shivajinagar, Pune", CityId = pune.CityId,   PackageId = pkgPuneBus.PackageId,   Time = new TimeOnly(5, 30), IsDeleted = false },
            // Package 3 — Ashtavinayak Bus (Mumbai)
            new() { PickupPoint1 = "Dadar, Mumbai",      CityId = mumbai.CityId, PackageId = pkgMumbaiBus.PackageId, Time = new TimeOnly(5,  0), IsDeleted = false },
            new() { PickupPoint1 = "Borivali, Mumbai",   CityId = mumbai.CityId, PackageId = pkgMumbaiBus.PackageId, Time = new TimeOnly(5, 30), IsDeleted = false },
            // Package 4 — Jyotirlinga Bus (Pune) — earlier departure
            new() { PickupPoint1 = "Swargate, Pune",     CityId = pune.CityId,   PackageId = pkgJyoti.PackageId,     Time = new TimeOnly(4,  0), IsDeleted = false },
            new() { PickupPoint1 = "Shivajinagar, Pune", CityId = pune.CityId,   PackageId = pkgJyoti.PackageId,     Time = new TimeOnly(4, 30), IsDeleted = false }
        };

        context.PickupPoints.AddRange(pickupPoints);
        await context.SaveChangesAsync();
        logger.LogInformation("[Seeder] Seeded {Count} pickup points.", pickupPoints.Count);
    }

    // ─────────────────────────────────────────────────────────────
    // 8. TRIPS  (FK: Categories, Packages)
    // ─────────────────────────────────────────────────────────────

    private static async Task SeedTripsAsync(AshtvinayakTravelContext context, ILogger logger)
    {
        if (await context.Trips.AnyAsync())
        {
            logger.LogInformation("[Seeder] Trips already seeded — skipping.");
            return;
        }

        var pkgPuneBus = await context.Packages.FirstAsync(p => p.PackageName == "Ashtavinayak Darshan 2N/3D (Bus)");
        var pkgJyoti   = await context.Packages.FirstAsync(p => p.PackageName == "Jyotirlinga Darshan 3N/4D (Bus)");

        // Load with City navigation to disambiguate Ashtavinayak Darshan by city
        var puneAshtaCat = await context.Categories
            .Include(c => c.City)
            .FirstAsync(c => c.CategoryName == "Ashtavinayak Darshan" && c.City!.CityName == "Pune");

        var jyotiCat = await context.Categories
            .FirstAsync(c => c.CategoryName == "Jyotirlinga Darshan");

        // Seed 2 upcoming trips — 15 and 30 days from today (UTC)
        var today = DateTime.UtcNow.Date;

        var trips = new List<Trip>
        {
            new()
            {
                PackageId      = pkgPuneBus.PackageId,
                CategoryId     = puneAshtaCat.CategoryId,
                TourName       = "Ashtavinayak Darshan — Pune Departure",
                TripDate       = today.AddDays(15),
                TotalSeats     = 30,
                AvailableSeats = 30,
                IsDeleted      = false
            },
            new()
            {
                PackageId      = pkgJyoti.PackageId,
                CategoryId     = jyotiCat.CategoryId,
                TourName       = "Jyotirlinga Darshan — Pune Departure",
                TripDate       = today.AddDays(30),
                TotalSeats     = 30,
                AvailableSeats = 30,
                IsDeleted      = false
            }
        };

        context.Trips.AddRange(trips);
        await context.SaveChangesAsync();
        logger.LogInformation("[Seeder] Seeded {Count} trips.", trips.Count);
    }

    // ─────────────────────────────────────────────────────────────
    // 9. SEATS  (FK: Trips, Packages)
    //    Standard layout: rows A–C, seats 1–10 per row = 30 seats/trip
    // ─────────────────────────────────────────────────────────────

    private static async Task SeedSeatsAsync(AshtvinayakTravelContext context, ILogger logger)
    {
        if (await context.Seats.AnyAsync())
        {
            logger.LogInformation("[Seeder] Seats already seeded — skipping.");
            return;
        }

        var trips = await context.Trips
            .Where(t => !t.IsDeleted)
            .ToListAsync();

        if (!trips.Any())
        {
            logger.LogWarning("[Seeder] No trips found — cannot seed seats. Check that SeedTripsAsync ran first.");
            return;
        }

        var rows  = new[] { "A", "B", "C" };
        var seats = new List<Seat>();

        foreach (var trip in trips)
        {
            foreach (var row in rows)
            {
                for (int num = 1; num <= 10; num++)
                {
                    seats.Add(new Seat
                    {
                        TripId      = trip.TripId,
                        PackageId   = trip.PackageId,
                        SeatNumber  = $"{row}{num}",
                        IsAvailable = true,
                        IsDeleted   = false
                    });
                }
            }
        }

        context.Seats.AddRange(seats);
        await context.SaveChangesAsync();
        logger.LogInformation("[Seeder] Seeded {Count} seats across {TripCount} trips.", seats.Count, trips.Count);
    }

    // ─────────────────────────────────────────────────────────────
    // 10. COMMISSION SETTING  (no FK dependencies — single global row)
    // ─────────────────────────────────────────────────────────────

    private static async Task SeedCommissionSettingAsync(AshtvinayakTravelContext context, ILogger logger)
    {
        if (await context.CommissionSettings.AnyAsync())
        {
            logger.LogInformation("[Seeder] CommissionSetting already seeded — skipping.");
            return;
        }

        context.CommissionSettings.Add(new CommissionSetting
        {
            DefaultCommissionPercentage = 10,
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        logger.LogInformation("[Seeder] Seeded default CommissionSetting (10%).");
    }
}
