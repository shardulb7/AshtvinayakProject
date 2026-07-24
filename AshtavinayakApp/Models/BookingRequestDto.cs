using System.ComponentModel.DataAnnotations;

namespace AshtavinayakAPP.Models
{
    public class BookingRequestDto
    {
        // ✅ FamilyBooking Table Fields
        public string? CarType { get; set; } = null;
        public DateOnly Date { get; set; }
        public DateTime Time { get; set; }

        // ✅ Booking Table Fields
        [Required(ErrorMessage = "UserId is required.")]
        public int? UserId { get; set; }

        [Required(ErrorMessage = "TripId is required.")]
        public int? TripId { get; set; }

        public int? PickupPointId { get; set; }
        public string? Droppoint { get; set; }
        public int? DroppointId { get; set; }
        public DateTime? BookingDate { get; set; }
        public string? RoomType { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        public string? Status { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "TotalPayment must be greater than 0.")] // MED-04
        public decimal? TotalPayment { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Advance cannot be negative.")]             // MED-04
        public decimal? Advance { get; set; }

        public string? BookingCode { get; set; }
        public virtual PickupPoint? PickupPoint { get; set; }

        // ✅ BookingSeat Table Fields — at least one seat required
        [Required(ErrorMessage = "SeatNumbers is required.")]
        [MinLength(1, ErrorMessage = "At least one seat number must be provided.")] // MED-04
        public List<string> SeatNumbers { get; set; } = new List<string>();

        [Range(0, 100, ErrorMessage = "Adults must be between 0 and 100.")]
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