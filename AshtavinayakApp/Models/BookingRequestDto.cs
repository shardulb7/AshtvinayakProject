namespace AshtavinayakAPP.Models
{
    public class BookingRequestDto
    {
        // ✅ FamilyBooking Table Fields
        public string? CarType { get; set; } = null;
        public DateOnly Date { get; set; }
        public DateTime Time { get; set; }

        // ✅ Booking Table Fields
        public int? UserId { get; set; }
        public int? TripId { get; set; }
        public int? PickupPointId { get; set; }
        public string? Droppoint { get; set; }
        public int? DroppointId { get; set; }
        public DateTime? BookingDate { get; set; }
        public string? RoomType { get; set; }
        public string? Status { get; set; }
        public decimal? TotalPayment { get; set; }
        public decimal? Advance { get; set; }
        public string? BookingCode { get; set; }
        public virtual PickupPoint? PickupPoint { get; set; }

        // ✅ BookingSeat Table Fields
        public List<string> SeatNumbers { get; set; } = new List<string>();
        public int? Adults { get; set; }
        public int? Childwithseat { get; set; }
        public int? Childwithoutseat { get; set; }

        // ✅ Additional Fields
        public int CategoryId { get; set; }
        public int CityId { get; set; }
        public int PackageId { get; set; }
        public required Transaction Transaction { get; set; }
    }
} 