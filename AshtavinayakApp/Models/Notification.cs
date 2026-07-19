using System;
using System.Collections.Generic;

namespace AshtavinayakAPP.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int? TripId { get; set; }

    public string NotificationMessage { get; set; } = null!;

    public DateTime NotificationDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? VehicleId { get; set; }

    public int? UserId { get; set; }

    public bool IsDeleted { get; set; }

    public virtual User? User { get; set; }

    public virtual Vehicle? Vehicle { get; set; }
    public virtual Trip? Trip { get; set; }
}
