using System;
using System.Collections.Generic;

namespace AshtavinayakAPP.Models;

public partial class BookingSeat
{
    public int BookingSeatId { get; set; }

    public string? SeatNumber { get; set; }

    public int? Adults { get; set; }

    public int? Childwithseat { get; set; }

    public int? Childwithoutseat { get; set; }

    public int? TripId { get; set; }

    public int? UserId { get; set; }

    public int? BookingId { get; set; }

    public bool IsDeleted { get; set; }

    public virtual Trip? Trip { get; set; }

    public virtual User? User { get; set; }
}
