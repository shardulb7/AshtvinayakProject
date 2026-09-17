using System;
using System.Collections.Generic;

namespace AshtavinayakAPP.Models;

public partial class Package
{
    public int PackageId { get; set; }

    public string PackageName { get; set; } = null!;

    public string Duration { get; set; } = null!;

    public int? CategoryId { get; set; }

    public int? CityId { get; set; }

    public string? Inclusions { get; set; }

    public string? Exclusions { get; set; }

    public int? AdultPrice { get; set; }

    public int? Child3To8YrswithSeat { get; set; }

    public int? Child3To8YrsWithoutSeat { get; set; }

    public bool IsCar { get; set; }

    public int? FamilyRoomChargePerPerson { get; set; }

    // Room-type sharing charges — per person, added on top of base adult price.
    // Shared (default) = no extra charge.
    // FamilyRoomChargePerPerson is kept for backward compat (car/family bookings).
    public int? SingleSharingChargePerPerson { get; set; }

    public int? DoubleSharingChargePerPerson { get; set; }

    public int? TripleSharingChargePerPerson { get; set; }

    public string? CarType { get; set; }

    public string? Itinerary { get; set; }

    public int? CarPackagePrice { get; set; }

    public int? CarTotalSeat { get; set; }

    public bool IsDeleted { get; set; }

    public int? PkgPersonCount { get; set; }

    public virtual Category? Category { get; set; }

    public virtual City? City { get; set; }

    public virtual ICollection<FamilyBooking> FamilyBookings { get; set; } = new List<FamilyBooking>();

    public virtual ICollection<History> Histories { get; set; } = new List<History>();

    public virtual ICollection<PickupPoint> PickupPoints { get; set; } = new List<PickupPoint>();

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();

    public virtual ICollection<TripRoute> TripRoutes { get; set; } = new List<TripRoute>();

    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
