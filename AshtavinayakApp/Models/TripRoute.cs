using System;
using System.Collections.Generic;

namespace AshtavinayakAPP.Models;

public partial class TripRoute
{
    public int Trid { get; set; }

    public int? PackageId { get; set; }

    public string PointName { get; set; } = null!;

    public string? Day { get; set; }

    public int? CityId { get; set; }

    public bool IsDeleted { get; set; }

    public virtual City? City { get; set; }

    public virtual Package? Package { get; set; }
}
