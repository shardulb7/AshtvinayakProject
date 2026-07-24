using System;

namespace AshtavinayakAPP.Models
{
    // View model for the Admin Panel's "Agent Bookings" report — not an EF entity.
    public class AgentBookingReportRow
    {
        public int BookingId { get; set; }
        public DateTime? BookingDate { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string PackageOrService { get; set; } = string.Empty;
        public decimal TotalBookingAmount { get; set; }
        public decimal CommissionPercentage { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal NetAmountPaidByAgent { get; set; }
        public string? BookingStatus { get; set; }
        public string? PaymentStatus { get; set; }
    }
}
