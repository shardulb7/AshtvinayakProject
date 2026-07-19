using System;
using System.Collections.Generic;

namespace AshtavinayakAPP.Models;

public partial class PickupPoint
{
    public int PickupPointId { get; set; }

    public int? CityId { get; set; }

    public string? PickupPoint1 { get; set; }

    public TimeOnly? Time { get; set; }

    public int? PackageId { get; set; }

    public bool IsDeleted { get; set; }

    public virtual City? City { get; set; }

    public virtual Package? Package { get; set; }
}
