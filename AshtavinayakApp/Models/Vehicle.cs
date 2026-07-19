using System;
using System.Collections.Generic;

namespace AshtavinayakAPP.Models;

public partial class Vehicle
{
    public int VehicleId { get; set; }

    public string VehicleType { get; set; } = null!;

    public string VehicleName { get; set; } = null!;

    public string VehicleNumber { get; set; } = null!;

    public int? TotalSeats { get; set; }

    public string DriverName { get; set; } = null!;

    public string DriverContact { get; set; } = null!;

    public int TripId { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Trip Trip { get; set; } = null!;
}
