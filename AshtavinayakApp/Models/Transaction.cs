using System;
using System.Collections.Generic;

namespace AshtavinayakAPP.Models;

public partial class Transaction
{
    public int TransactionId { get; set; }

    public int BookingId { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public DateTime TransactionDate { get; set; }

    public decimal Amount { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public int UserId { get; set; }

    public string TransactionReference { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public virtual Booking? Booking { get; set; } = null!;

    public virtual User? User { get; set; } = null!;
}
