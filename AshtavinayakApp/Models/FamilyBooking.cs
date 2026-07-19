using System;
using System.Collections.Generic;

namespace AshtavinayakAPP.Models;

public partial class FamilyBooking
{
    public int FamilyId { get; set; }

    public string CarType { get; set; } = null!;

    public DateTime Date { get; set; }

    public DateTime Time { get; set; }

    public int BookingId { get; set; }

    public int UserId { get; set; }

    public int PackageId { get; set; }

    public bool IsDeleted { get; set; }

    public int? Adults { get; set; }
    public int? Childwithseat { get; set; }
    public int? Childwithoutseat { get; set; }
    public DateTime? BookingDate { get; set; }

    public virtual Package Package { get; set; } = null!;
    public virtual User User { get; set; }
}
