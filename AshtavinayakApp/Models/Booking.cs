using System;
using System.Collections.Generic;

namespace AshtavinayakAPP.Models;

public partial class Booking
{
    public int BookingId { get; set; }

    public int? UserId { get; set; }

    public int? TripId { get; set; }

    public string? PickUpPointName { get; set; }

    public int? PickupPointId { get; set; }

    public DateTime? BookingDate { get; set; }

    public string? Status { get; set; }

    public decimal? TotalPayment { get; set; }

    public decimal? Advance { get; set; }

    public int? BookingSeatId { get; set; }

    public int? DroppointId { get; set; }

    public string? BookingCode { get; set; }

    public string? Droppoint { get; set; }

    public string? RoomType { get; set; }

    public bool IsDeleted { get; set; }

    public int? AgentId { get; set; }

    // Snapshotted at booking time — later changes to commission rates must not
    // retroactively alter the recorded figures on past bookings.
    public decimal? CommissionPercentage { get; set; }

    public decimal? CommissionAmount { get; set; }

    public virtual ICollection<History> Histories { get; set; } = new List<History>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public virtual Trip? Trip { get; set; }

    public virtual User? User { get; set; }
    public virtual PickupPoint? PickupPoint { get; set; }
    public virtual Agent? Agent { get; set; }
}
