using System;
using System.Collections.Generic;

namespace AshtavinayakAPP.Models;

public partial class History
{
    public int? CityId { get; set; }

    public int? PackageId { get; set; }

    public int? CategoryId { get; set; }

    public int? BookingId { get; set; }

    public int HistoryId { get; set; }

    public bool IsDeleted { get; set; }

    public virtual Booking? Booking { get; set; }

    public virtual Category? Category { get; set; }

    public virtual City? City { get; set; }

    public virtual Package? Package { get; set; }
}
