namespace AshtavinayakAPP.Models
{
    public partial record CarBookingDTOModel
    {
        public int? UserId { get; set; }
        public int? TripId { get; set; }               // optional, used in response
        public int? PickupPointId { get; set; }
        public string? Droppoint { get; set; }
        public string? RoomType { get; set; }
        public string? Status { get; set; }
        public decimal? TotalPayment { get; set; }
        public decimal? Advance { get; set; }

        // FamilyBooking fields
        public string? CarType { get; set; }
        public DateTime Date { get; set; }
        public DateTime Time { get; set; }
        public int PackageId { get; set; }
        public string? PickUpPointName { get; set; }


        public int? Adults { get; set; }
        public int? Childwithseat { get; set; }
        public int? Childwithoutseat { get; set; }
        public DateTime? BookingDate { get; set; }

        public required Transaction Transaction { get; set; }
    }
}
