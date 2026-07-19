using System;
using System.Collections.Generic;

namespace AshtavinayakAPP.Models;

public partial class Trip
{
    public int TripId { get; set; }

    public int? PackageId { get; set; }

    public DateTime TripDate { get; set; }

    public int TotalSeats { get; set; }

    public int AvailableSeats { get; set; }

    public string? TourName { get; set; }

    public int? CategoryId { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Category? Category { get; set; }

    public virtual Package? Package { get; set; }

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
