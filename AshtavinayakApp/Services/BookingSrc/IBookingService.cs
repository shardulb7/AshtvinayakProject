using AshtavinayakAPP.Models;

namespace AshtavinayakAPP.Services.BookingSrc
{
    public interface IBookingService
    {
        Task<(bool Success, string Message, object Data)> CreateBookingWithSeatsAsync(
            BookingRequestDto request, int? agentId = null, decimal? commissionPercentage = null);

        Task<(bool Success, string Message, object Data)> BookCarAsync(CarBookingDTOModel request);

        Task<(bool Success, string Message, object Data)> GetFamilyBookingHistoryAsync(int userId);
        Task<(bool Success, string Message, object Data)> GetBookingHistoryByUserAsync(int userId);
        Task<(bool Success, string Message, object? Data)> UpdatePaymentAsync(Transaction dto);
        Task<(bool Success, string Message, object Data)> GetInvoiceAsync(int bookingId);
        Task<List<Booking>> GetBookingByTripIdAsync(int tripId);
    }
}
