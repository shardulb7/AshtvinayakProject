using System;
using System.Collections.Generic;

namespace AshtavinayakAPP.Models;

public partial class DropUp
{
    public int DroppointId { get; set; }

    public string? DropPoint { get; set; }

    public int? CityId { get; set; }

    public int? PackageId { get; set; }

    public bool IsDeleted { get; set; }

    public virtual City? City { get; set; }
}
