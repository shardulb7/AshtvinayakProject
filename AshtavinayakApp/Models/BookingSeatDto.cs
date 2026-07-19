namespace AshtavinayakAPP.Models
{
    public class BookingSeatDto
    {
        public int Id { get; set; }
        public int TripId { get; set; }
        public string TourName { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public DateTime TripDate { get; set; }
        public string SeatNumbers { get; set; }
        public int BookingId { get; set; }

        // Optional: If you want to include counts
        public int Adults { get; set; }
        public int ChildWithSeat { get; set; }
        public int ChildWithoutSeat { get; set; }
    }
}
