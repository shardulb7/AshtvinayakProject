using System;
using System.Collections.Generic;

namespace AshtavinayakAPP.Models;

public partial class Seat
{
    public int SeatId { get; set; }

    public int? PackageId { get; set; }

    public string? SeatNumber { get; set; }

    public bool IsAvailable { get; set; }

    public int? TripId { get; set; }

    public bool IsDeleted { get; set; }

    public virtual Package? Package { get; set; }

    public virtual Trip? Trip { get; set; }
}
