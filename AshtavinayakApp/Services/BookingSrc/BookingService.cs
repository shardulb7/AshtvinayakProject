using AshtavinayakAPP.Controllers;
using AshtavinayakAPP.Models;
using AshtavinayakAPP.Services.SmsService;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using SelectPdf;
using System.Text;
using static System.Net.WebRequestMethods;

namespace AshtavinayakAPP.Services.BookingSrc
{
    public class BookingService : IBookingService  // LOW-02: was `record` — incorrect for a stateful DI service
    {
        private readonly AshtvinayakTravelContext _context;
        private readonly ISmsService _smsService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BookingService> _logger; // LOW-08

        public BookingService(AshtvinayakTravelContext ashtvinayakTravelContext, ISmsService smsService,
            IConfiguration configuration, ILogger<BookingService> logger)
        {
            _context       = ashtvinayakTravelContext;
            _smsService    = smsService;
            _configuration = configuration;
            _logger        = logger; // LOW-08
        }

        public async Task<(bool Success, string Message, object Data)> CreateBookingWithSeatsAsync(
            BookingRequestDto request, int? agentId = null, decimal? commissionPercentage = null)
        {
            // Guard: UserId is required — fail fast before opening a DB transaction
            if (request.UserId == null)
                return (false, "UserId is required.", null);

            // MED-15: EnableRetryOnFailure (added for DB resiliency) requires any manually-opened
            // transaction to run inside an execution strategy — retrying a bare BeginTransactionAsync
            // on a transient failure would throw InvalidOperationException instead. The strategy retries
            // this whole delegate (including opening a fresh transaction) on transient failures.
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync<(bool Success, string Message, object Data)>(async () =>
            {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var trip = await _context.Trips.FirstOrDefaultAsync(t => t.TripId == request.TripId);
                if (trip == null)
                    return (false, "Trip not found.", null);

                if (trip.AvailableSeats < request.SeatNumbers.Count)
                    return (false, "No seats available for this trip.", null);

                var bookedSeats = await _context.BookingSeats
                    .Where(bs => bs.TripId == request.TripId && request.SeatNumbers.Contains(bs.SeatNumber))
                    .Select(bs => bs.SeatNumber)
                    .ToListAsync();

                if (bookedSeats.Any())
                    return (false, "Some seats are already booked.", new { UnavailableSeats = bookedSeats });

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId);
                if (user == null)
                    return (false, "User not found.", null);

                // MED-11: recompute the payable amount server-side from the trip's package rates —
                // never trust a client-submitted price. request.TotalPayment is no longer used for
                // the persisted amount; only Advance (how much the customer chooses to pay now) is
                // still client-supplied, and it's validated against the computed total below.
                var package = await _context.Packages.FirstOrDefaultAsync(p => p.PackageId == trip.PackageId && !p.IsDeleted);
                if (package == null)
                    return (false, "Package not found for this trip.", null);

                decimal computedTotalPayment =
                    (request.Adults ?? 0) * (package.AdultPrice ?? 0)
                    + (request.Childwithseat ?? 0) * (package.Child3To8YrswithSeat ?? 0)
                    + (request.Childwithoutseat ?? 0) * (package.Child3To8YrsWithoutSeat ?? 0);

                if (computedTotalPayment <= 0)
                    return (false, "Unable to determine package pricing for the given passenger counts.", null);

                if (request.Advance.HasValue && request.Advance.Value > computedTotalPayment)
                    return (false, "Advance amount cannot exceed the total payment.", null);

                if (request.TotalPayment.HasValue && request.TotalPayment.Value != computedTotalPayment)
                {
                    _logger.LogWarning(
                        "CreateBookingWithSeats: client-submitted TotalPayment {ClientAmount} differs from server-computed {ComputedAmount} for TripId={TripId} UserId={UserId}. Using computed amount.",
                        request.TotalPayment.Value, computedTotalPayment, request.TripId, request.UserId);
                }

                // Agent commission: computed and snapshotted onto the booking at creation time,
                // so a later change to the agent's/default commission rate never retroactively
                // alters an already-placed booking's recorded figures.
                decimal? commissionAmount = null;
                if (agentId.HasValue)
                {
                    commissionAmount = Math.Round(computedTotalPayment * (commissionPercentage ?? 0) / 100, 2);
                }

                var booking = new Booking
                {
                    UserId        = request.UserId,
                    TripId        = request.TripId,
                    PickupPointId = request.PickupPointId,
                    BookingDate   = request.BookingDate,
                    RoomType      = request.RoomType,
                    Status        = request.Status,
                    TotalPayment  = computedTotalPayment,
                    Advance       = request.Advance,
                    Droppoint     = request.Droppoint,
                    BookingCode   = Guid.NewGuid().ToString("N")[..12].ToUpper(), // HIGH-12: generate unique booking code
                    AgentId              = agentId,
                    CommissionPercentage = agentId.HasValue ? commissionPercentage : null,
                    CommissionAmount     = commissionAmount
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                var bookingSeats = request.SeatNumbers.Select(seatNumber => new BookingSeat
                {
                    BookingId = booking.BookingId,
                    SeatNumber = seatNumber,
                    Adults = request.Adults,
                    Childwithseat = request.Childwithseat,
                    Childwithoutseat = request.Childwithoutseat,
                    TripId = request.TripId,
                    UserId = request.UserId,
                }).ToList();

                _context.BookingSeats.AddRange(bookingSeats);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException dbEx) when (IsUniqueSeatConstraintViolation(dbEx))
                {
                    // MED-10: the pre-check above has a race window between two concurrent
                    // requests for the same seat; the filtered unique index on
                    // (TripId, SeatNumber) is the actual guard, and lands here as a
                    // constraint violation rather than a silent double-booking.
                    await transaction.RollbackAsync();
                    return (false, "Some seats are already booked.", new { UnavailableSeats = request.SeatNumbers });
                }

                // HIGH-12: Mark individual seats unavailable (was only done by the deprecated BookSeats endpoint)
                var seatEntities = await _context.Seats
                    .Where(s => s.TripId == request.TripId && request.SeatNumbers.Contains(s.SeatNumber))
                    .ToListAsync();
                foreach (var s in seatEntities) s.IsAvailable = false;

                trip.AvailableSeats = Math.Max(0, trip.AvailableSeats - request.SeatNumbers.Count); // guard against underflow
                _context.Trips.Update(trip);

                // Save Transaction record
                request.Transaction.BookingId = booking.BookingId;
                request.Transaction.UserId    = (int)request.UserId;
                _context.Transactions.Add(request.Transaction);
                await _context.SaveChangesAsync();

                var message = $"Hello {user.UserName} ! Your Booking is confirmed..!! You will receive Vehicle and Driver details one day prior to your Trip - iTas";

                var bookingTemplateId = _configuration["SmsGateway:BookingTemplateId"] ?? string.Empty;
                var isSent = await _smsService.SendAsync(user.PhoneNumber, message, bookingTemplateId);
                await transaction.CommitAsync();

                // Additive response fields only — the normal (non-agent) response shape is unchanged.
                if (agentId.HasValue)
                {
                    return (true, "Booking and seats saved successfully.", new
                    {
                        BookingId = booking.BookingId,
                        TotalPayment = computedTotalPayment,
                        CommissionPercentage = commissionPercentage ?? 0,
                        CommissionAmount = commissionAmount ?? 0,
                        AgentPayable = computedTotalPayment - (commissionAmount ?? 0)
                    });
                }

                return (true, "Booking and seats saved successfully.", new { BookingId = booking.BookingId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "CreateBookingWithSeats failed for UserId={UserId} TripId={TripId}", // LOW-08
                    request.UserId, request.TripId);
                return (false, "An unexpected error occurred. Please try again.", null);
            }
            });
        }

        // MED-10: SQL Server unique-constraint-violation error numbers (2601: duplicate key row
        // in an object with a unique index; 2627: violation of PRIMARY KEY/UNIQUE constraint).
        private static bool IsUniqueSeatConstraintViolation(DbUpdateException ex)
            => ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627);

        public async Task<(bool Success, string Message, object Data)> BookCarAsync(CarBookingDTOModel request)
        {
            // MED-15: see the matching comment in CreateBookingWithSeatsAsync — EnableRetryOnFailure
            // requires manually-opened transactions to run inside an execution strategy.
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync<(bool Success, string Message, object Data)>(async () =>
            {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // ✅ Validate Required Fields
                if (request.UserId == null || string.IsNullOrEmpty(request.CarType))
                {
                    return (false, "UserId and CarType are required fields.", null);
                }

                // MED-11: recompute the payable amount server-side from the package's car rate —
                // never trust a client-submitted price (mirrors the fix in CreateBookingWithSeatsAsync).
                var package = await _context.Packages.FirstOrDefaultAsync(p => p.PackageId == request.PackageId && !p.IsDeleted);
                if (package == null)
                    return (false, "Package not found.", null);
                if (!package.IsCar)
                    return (false, "The selected package is not a car package.", null);

                decimal computedTotalPayment = package.CarPackagePrice ?? 0;
                if (computedTotalPayment <= 0)
                    return (false, "Unable to determine package pricing.", null);

                if (request.Advance.HasValue && request.Advance.Value > computedTotalPayment)
                    return (false, "Advance amount cannot exceed the total payment.", null);

                if (request.TotalPayment.HasValue && request.TotalPayment.Value != computedTotalPayment)
                {
                    _logger.LogWarning(
                        "BookCarAsync: client-submitted TotalPayment {ClientAmount} differs from server-computed {ComputedAmount} for PackageId={PackageId} UserId={UserId}. Using computed amount.",
                        request.TotalPayment.Value, computedTotalPayment, request.PackageId, request.UserId);
                }

                // ✅ 1️⃣ Create Booking
                var booking = new Booking
                {
                    UserId = request.UserId.Value,
                    PickupPointId = request.PickupPointId,
                    Droppoint = request.Droppoint,
                    BookingDate = DateTime.UtcNow,
                    RoomType = request.RoomType,
                    Status = request.Status ?? "Confirmed",
                    TotalPayment = computedTotalPayment,
                    Advance = request.Advance ?? 0,
                    PickUpPointName = request.PickUpPointName
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // ✅ 2️⃣ Create FamilyBooking
                var familyBooking = new FamilyBooking
                {
                    CarType = request.CarType,
                    Date = request.Date,
                    Time = request.Time,
                    BookingId = booking.BookingId,
                    UserId = request.UserId.Value,
                    PackageId = request.PackageId,
                    BookingDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")),
                    Adults =request.Adults,
                    Childwithoutseat=request.Childwithoutseat,
                    Childwithseat=request.Childwithseat
                };



                _context.FamilyBookings.Add(familyBooking);
                await _context.SaveChangesAsync();


                request.Transaction.BookingId = booking.BookingId;
                request.Transaction.UserId = (int)request.UserId;
                _context.Transactions.Add(request.Transaction);
                await _context.SaveChangesAsync();


                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId);
                if (user == null)
                {
                    await transaction.RollbackAsync();
                    return (false, "User not found.", null);
                }
                var message = $"Hello {user.UserName} ! Your Booking is confirmed..!! You will receive Vehicle and Driver details one day prior to your Trip - iTas";
                var bookingTemplateId = _configuration["SmsGateway:BookingTemplateId"] ?? string.Empty;
                var isSent = await _smsService.SendAsync(user.PhoneNumber, message, bookingTemplateId);
                // ✅ 3️⃣ Commit Transaction
                await transaction.CommitAsync();
                
                return (true, "Car booking successful!", new
                {
                    BookingId = booking.BookingId,
                    FamilyBookingId = familyBooking.FamilyId,
                    CarType = request.CarType,
                    Status = booking.Status,
                    UserId = request.UserId.Value,
                    TripId = request.TripId,
                    PickupPointId = request.PickupPointId,
                    Droppoint = request.Droppoint,
                    BookingDate = DateTime.UtcNow,
                    RoomType = booking.RoomType,
                    TotalPayment = booking.TotalPayment,
                    Advance = booking.Advance
                });

                
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "BookCarAsync failed for UserId={UserId}", request.UserId);
                return (false, "An unexpected error occurred. Please try again.", null);
            }
            });
        }

        public async Task<(bool Success, string Message, object Data)> GetFamilyBookingHistoryAsync(int userId)
        {
            try
            {
                // 1️⃣ Fetch bookings + family booking + pickup point
                var bookings = await (
                    from b in _context.Bookings
                    join f in _context.FamilyBookings on b.BookingId equals f.BookingId
                    where b.UserId == userId && !b.IsDeleted
                    join p in _context.PickupPoints on b.PickupPointId equals p.PickupPointId into pp
                    from pickup in pp.DefaultIfEmpty()
                    select new
                    {
                        b.BookingId,
                        b.BookingDate,
                        b.Status,
                        b.TotalPayment,
                        b.Advance,
                        b.Droppoint,
                        b.PickupPointId,
                        b.PickUpPointName,
                        PickupPoint = pickup != null ? new
                        {
                            pickup.PickupPointId,
                            pickup.PickupPoint1
                        } : null,
                        CarType = f.CarType,
                        Date = f.Date,
                        Time = f.Time
                    }
                ).ToListAsync();

                if (!bookings.Any())
                    return (false, "No family bookings found for this user.", null);

                var bookingIds = bookings.Select(b => b.BookingId).ToList();

                var transactionsGrouped = await _context.Transactions
    .Where(t => bookingIds.Contains(t.BookingId))
    .GroupBy(t => t.BookingId)
    .ToDictionaryAsync(
        g => g.Key,
        g => g.Select(t => new Transaction
        {
            TransactionId = t.TransactionId,
            BookingId = t.BookingId,
            PaymentMethod = t.PaymentMethod,
            Amount = t.Amount,
            TransactionDate = t.TransactionDate,
            PaymentStatus = t.PaymentStatus,
            TransactionReference = t.TransactionReference
        }).ToList()
    );

                var history = bookings.Select(b =>
                {
                    var transactions = transactionsGrouped.ContainsKey(b.BookingId)
                        ? transactionsGrouped[b.BookingId]
                        : new List<Transaction>();

                    decimal totalPaid = transactions.Sum(t => t.Amount);
                    decimal pendingAmount = (decimal)((b.TotalPayment ?? 0) - (totalPaid));

                    return new
                    {
                        b.BookingId,
                        b.BookingDate,
                        b.Status,
                        b.TotalPayment,
                        b.Advance,
                        b.PickUpPointName,
                        PaidAmount = totalPaid,
                        PendingAmount = pendingAmount,
                        b.Droppoint,
                        b.PickupPointId,
                        b.PickupPoint,
                        b.CarType,
                        b.Date,
                        b.Time,
                        Transactions = transactions
                    };
                }).ToList();


                return (true, "Family booking history fetched successfully.", history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetFamilyBookingHistoryAsync failed for UserId={UserId}", userId);
                return (false, "An unexpected error occurred. Please try again.", null);
            }
        }


        public async Task<(bool Success, string Message, object Data)> GetBookingHistoryByUserAsync(int userId)
        {
            try
            {
                // 1️⃣ Fetch user bookings with related Trip & PickupPoint
                var bookings = await _context.Bookings
                    .Where(b => b.UserId == userId && b.TripId != null && !b.IsDeleted) // Exclude bookings without TripId
                    .Include(b => b.PickupPoint)
                    .Include(b => b.Trip)
                    .ToListAsync();

                if (!bookings.Any())
                    return (false, "No booking history found for this user.", null);

                var bookingIds = bookings.Select(b => b.BookingId).ToList();

                // 2️⃣ Fetch transactions for these bookings, grouped by BookingId
                var transactionsGrouped = await _context.Transactions
                    .Where(t => bookingIds.Contains(t.BookingId))
                    .GroupBy(t => t.BookingId)
                    .ToDictionaryAsync(
                        g => g.Key,
                        g => g.Select(t => new Transaction
                        {
                            TransactionId = t.TransactionId,
                            BookingId = t.BookingId,
                            PaymentMethod = t.PaymentMethod,
                            Amount = t.Amount,
                            TransactionDate = t.TransactionDate,
                            PaymentStatus = t.PaymentStatus,
                            TransactionReference = t.TransactionReference
                        }).ToList()
                    );

                // 3️⃣ Build the booking history with transactions & computed amounts
                var histories = bookings.Select(b =>
                {
                    var transactions = transactionsGrouped.ContainsKey(b.BookingId)
                        ? transactionsGrouped[b.BookingId]
                        : new List<Transaction>();

                    decimal totalPaid = transactions.Sum(t => t.Amount);
                    decimal pendingAmount = (b.TotalPayment ?? 0) - (totalPaid);

                    return new
                    {
                        bookingId = b.BookingId,
                        UserId = b.UserId,
                        TripId = b.TripId,
                        TripName = b.Trip?.TourName,
                        BookingDate = b.BookingDate,
                        Status = b.Status,
                        TotalPayment = b.TotalPayment,
                        Advance = b.Advance,
                        PaidAmount = totalPaid,
                        PendingAmount = pendingAmount,
                        Droppoint = b.Droppoint,
                        PickupPointName = b.PickupPoint?.PickupPoint1 ?? "N/A",
                        SeatNumbers = _context.BookingSeats
                            .Where(bs => bs.BookingId == b.BookingId)
                            .Select(bs => new
                            {
                                bs.SeatNumber,
                                bs.Adults,
                                bs.Childwithseat,
                                bs.Childwithoutseat
                            }).ToList(),
                        Transactions = transactions
                    };
                }).ToList();

                return (true, "User history fetched successfully.", histories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetBookingHistoryByUserAsync failed for UserId={UserId}", userId);
                return (false, "An unexpected error occurred. Please try again.", null);
            }
        }
        public async Task<(bool Success, string Message, object? Data)> UpdatePaymentAsync(Transaction dto)
        {
            try
            {
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.BookingId == dto.BookingId);

                if (booking == null)
                    return (false, "Booking not found.", null);

                // ✅ Add new transaction with required UserId
                var newTransaction = new Transaction
                {
                    BookingId = booking.BookingId,
                    UserId = dto.UserId,
                    Amount = dto.Amount,
                    PaymentMethod = dto.PaymentMethod,
                    TransactionReference = dto.TransactionReference,
                    TransactionDate = DateTime.Now,
                    PaymentStatus = "Success"
                };

                _context.Transactions.Add(newTransaction);

                // Calculate total paid so far (including this transaction)
                var existingPaid = await _context.Transactions
                    .Where(t => t.BookingId == booking.BookingId)
                    .SumAsync(t => t.Amount);

                var totalPaid = existingPaid + dto.Amount;
                var totalPayment = booking.TotalPayment ?? 0;

                // If fully paid, mark booking status
                if (totalPaid >= totalPayment)
                {
                    booking.Status = "Paid";
                }

                await _context.SaveChangesAsync();

                return (true, "Payment updated successfully.", new
                {
                    BookingId = booking.BookingId,
                    TotalPaid = totalPaid,
                    PendingAmount = totalPayment - totalPaid < 0 ? 0 : totalPayment - totalPaid
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdatePaymentAsync failed for BookingId={BookingId}", dto.BookingId);
                return (false, "An unexpected error occurred. Please try again.", null);
            }
        }
        /// <summary>
        /// Get Invoice
        /// </summary>
        /// <param name="bookingId"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<(bool Success, string Message, object Data)> GetInvoiceAsync(int bookingId)
        {
            try
            {
                if (bookingId <= 0)
                {
                    return (false, "Invalid booking ID.", null);
                }

                var booking = await _context.Bookings
                    .Include(b => b.User)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId && !b.IsDeleted);
                if (booking == null)
                {
                    return (false, "Booking Not Found", null);
                }


                StringBuilder builder = new StringBuilder("<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n    <meta charset=\"utf-8\">\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\r\n    <title>Invoice - iTAS Tourism</title>\r\n    <style>\r\n        * {\r\n            margin: 0;\r\n            padding: 0;\r\n            box-sizing: border-box;\r\n        }\r\n        \r\n        body {\r\n            font-family: Arial, sans-serif;\r\n            background: #f6f6f6;\r\n            padding: 10px;\r\n            color: #333;\r\n            font-size: 12px;\r\n            line-height: 1.2;\r\n        }\r\n        \r\n        .invoice-wrap {\r\n            max-width: 900px;\r\n            margin: 0 auto;\r\n            background: #fff;\r\n            border-radius: 4px;\r\n            padding: 15px;\r\n            box-shadow: 0 0 5px rgba(0,0,0,0.1);\r\n        }\r\n        \r\n        .header {\r\n            display: flex;\r\n            justify-content: space-between;\r\n            align-items: flex-start;\r\n            border-bottom: 1px solid #e74c3c;\r\n            padding-bottom: 10px;\r\n            margin-bottom: 10px;\r\n        }\r\n        \r\n        .logo {\r\n            flex: 0 0 120px;\r\n        }\r\n        \r\n        .logo-placeholder {\r\n            width: 120px;\r\n            height: 60px;\r\n            background: #f0f0f0;\r\n            border: 1px dashed #ccc;\r\n            display: flex;\r\n            align-items: center;\r\n            justify-content: center;\r\n            color: #777;\r\n            font-size: 10px;\r\n        }\r\n        \r\n        .company {\r\n            text-align: center;\r\n            flex: 1;\r\n            padding: 0 10px;\r\n        }\r\n        \r\n        .company h1 {\r\n            margin: 0 0 3px 0;\r\n            font-size: 20px;\r\n            color: #2c3e50;\r\n        }\r\n        \r\n        .company p {\r\n            margin: 1px 0;\r\n            font-size: 10px;\r\n            color: #555;\r\n        }\r\n        \r\n        .customer-section {\r\n            display: flex;\r\n            justify-content: space-between;\r\n            margin: 10px 0;\r\n        }\r\n        \r\n        .customer-details {\r\n            width: 65%;\r\n        }\r\n        \r\n        .invoice-details {\r\n            border: 1px solid #000;\r\n            padding: 8px;\r\n            width: 32%;\r\n            font-size: 11px;\r\n        }\r\n        \r\n        .invoice-details table {\r\n            width: 100%;\r\n        }\r\n        \r\n        .invoice-details td {\r\n            padding: 1px 0;\r\n        }\r\n        \r\n        .info-table {\r\n            width: 100%;\r\n            border-collapse: collapse;\r\n            font-size: 11px;\r\n        }\r\n        \r\n        .info-table td {\r\n            padding: 3px;\r\n            vertical-align: top;\r\n        }\r\n        \r\n        .info-table .label {\r\n            font-weight: 700;\r\n            width: 120px;\r\n        }\r\n        \r\n        .items-table {\r\n            width: 100%;\r\n            border-collapse: collapse;\r\n            margin: 10px 0;\r\n            font-size: 11px;\r\n        }\r\n        \r\n        .items-table th, .items-table td {\r\n            border: 1px solid #000;\r\n            padding: 6px;\r\n            text-align: left;\r\n        }\r\n        \r\n        .items-table th {\r\n            background: #eee;\r\n            font-weight: 700;\r\n        }\r\n        \r\n        .combined-section {\r\n            display: flex;\r\n            justify-content: space-between;\r\n            margin-top: 10px;\r\n            border: 1px solid #ddd;\r\n            padding: 8px;\r\n            background: #f9f9f9;\r\n        }\r\n        \r\n        .bank-details {\r\n            width: 40%;\r\n            font-size: 10px;\r\n        }\r\n        \r\n        .amount-words {\r\n            width: 25%;\r\n            font-size: 10px;\r\n            font-weight: 700;\r\n        }\r\n        \r\n        .totals {\r\n            width: 30%;\r\n        }\r\n        \r\n        .totals table {\r\n            width: 100%;\r\n            border-collapse: collapse;\r\n            font-size: 11px;\r\n        }\r\n        \r\n        .totals td {\r\n            padding: 4px;\r\n            border: 1px solid #000;\r\n        }\r\n        \r\n        .totals td.label {\r\n            background: #f3f3f3;\r\n            width: 60%;\r\n            font-weight: 700;\r\n        }\r\n        \r\n        .footer {\r\n            display: flex;\r\n            justify-content: space-between;\r\n            align-items: flex-end;\r\n            margin-top: 15px;\r\n            padding-top: 10px;\r\n            border-top: 1px solid #ddd;\r\n        }\r\n        \r\n        .stamp {\r\n            text-align: center;\r\n        }\r\n        \r\n        .stamp-placeholder {\r\n            width: 80px;\r\n            height: 60px;\r\n            background: #f0f0f0;\r\n            border: 1px dashed #ccc;\r\n            display: flex;\r\n            align-items: center;\r\n            justify-content: center;\r\n            color: #777;\r\n            font-size: 9px;\r\n            margin: 0 auto;\r\n        }\r\n        \r\n        .stamp div {\r\n            font-size: 9px;\r\n            margin-top: 3px;\r\n        }\r\n        \r\n        .highlight {\r\n            color: #e74c3c;\r\n            font-weight: 700;\r\n        }\r\n        \r\n        .text-right {\r\n            text-align: right;\r\n        }\r\n        \r\n        @media print {\r\n            body {\r\n                background: #fff;\r\n                padding: 0;\r\n                margin: 0;\r\n            }\r\n            \r\n            .invoice-wrap {\r\n                box-shadow: none;\r\n                padding: 10px;\r\n                margin: 0;\r\n            }\r\n        }\r\n    </style>\r\n</head>\r\n<body>\r\n<div class=\"invoice-wrap\">\r\n\r\n    <div class=\"header\">\r\n        <div class=\"logo\">\r\n            <img class=\"logo-placeholder\" src=\"https://ashtavinayak.itastourism.com/images/ITAS%20LOGO.jpeg\" alt=\"Ashtavinayak\">\r\n        </div>\r\n        <div class=\"company\">\r\n            <h1>iTAS TOURISM</h1>\r\n            <p>Sahyog Nagar, Gajanan Hsg. Society, Akshay Collection, Po-Rupeenagar, Tal-Haveli, Dist-Pune</p>\r\n            <p>Contact: +91 90496 87995 | Email: itastourism@gmail.com</p>\r\n            <p>GSTN: 27CELPS3061L1ZY | STATE: MAHARASHTRA | CODE: 27</p>\r\n        </div>\r\n    </div>\r\n\r\n    <div class=\"customer-section\">\r\n        <div class=\"customer-details\">\r\n            <table class=\"info-table\">\r\n                <tr><td class=\"label\">Customer Name:</td><td><span class=\"highlight\">%Customer Name%</span></td></tr>\r\n                <tr><td class=\"label\">Address:</td><td><span class=\"highlight\">%Address%</span></td></tr>\r\n                <tr><td class=\"label\">GSTN:</td><td><span class=\"highlight\">%GSTN%</span></td></tr>\r\n                <tr><td class=\"label\">State Code:</td><td><span class=\"highlight\">27</span></td></tr>\r\n                <tr><td class=\"label\">Consignee Name:</td><td><span class=\"highlight\">%Consignee Name%</span></td></tr>\r\n            </table>\r\n        </div>\r\n        <div class=\"invoice-details\">\r\n            <table>\r\n                <tr><td><strong>Invoice No.</strong></td><td>: <span class=\"highlight\">%Invoice No%</span></td></tr>\r\n                <tr><td><strong>Date</strong></td><td>: <span class=\"highlight\">%Date%</span></td></tr>\r\n                <tr><td><strong>Vehicle No</strong></td><td>: <span class=\"highlight\">%Vehicle No%</span></td></tr>\r\n                <tr><td><strong>Driver</strong></td><td>: <span class=\"highlight\">%Driver%</span></td></tr>\r\n            </table>\r\n        </div>\r\n    </div>\r\n\r\n    <table class=\"items-table\">\r\n        <thead>\r\n            <tr>\r\n                <th>Sr.No</th>\r\n                <th>Description</th>\r\n                <th>Qty</th>\r\n                <th>Unit</th>\r\n                <th>Rate</th>\r\n                <th>Net Amount</th>\r\n            </tr>\r\n        </thead>\r\n        <tbody>\r\n            <tr>\r\n                <td>1</td>\r\n                <td><span class=\"highlight\">%Desc%</span></td>\r\n                <td>1</td>\r\n                <td>No(s)</td>\r\n                <td class=\"text-right\"><span class=\"highlight\">%Rate%</span></td>\r\n                <td class=\"text-right\"><span class=\"highlight\">%Net Amount%</span></td>\r\n            </tr>\r\n        </tbody>\r\n    </table>\r\n\r\n    <div class=\"combined-section\">\r\n        <div class=\"bank-details\">\r\n            <strong>Beneficiary Name:</strong> ITAS TOURISM<br>\r\n            <strong>Bank Name:</strong> Bank of Maharashtra<br>\r\n            <strong>A/C No.:</strong> 60309085503<br>\r\n            <strong>IFSC:</strong> MAHB0001132<br><br>\r\n            <strong>Amount in Words: </strong><span class=\"highlight\">%Rupees in words</span>\r\n        </div>\r\n        \r\n        <div class=\"totals\">\r\n            <table>\r\n                <tr><td class=\"label\">Sub Total:</td><td class=\"text-right\"><span class=\"highlight\">%Sub Total%</span></td></tr>\r\n                <tr><td class=\"label\">CGST:</td><td class=\"text-right\"><span class=\"highlight\">%CGST%</span></td></tr>\r\n                <tr><td class=\"label\">SGST:</td><td class=\"text-right\"><span class=\"highlight\">%SGST%</span></td></tr>\r\n                <tr><td class=\"label\">Total GST:</td><td class=\"text-right\"><span class=\"highlight\">%Total GST%</span></td></tr>\r\n                <tr><td class=\"label\">Total Amount:</td><td class=\"text-right\"><span class=\"highlight\">%Total Amount%</span></td></tr>\r\n            </table>\r\n        </div>\r\n    </div>\r\n\r\n    <div class=\"footer\">\r\n        <div>Receiver's Signature & Stamp</div>\r\n        <div class=\"stamp\">\r\n            <img class=\"stamp-placeholder\" src=\"https://ashtavinayak.itastourism.com/images/ITAS%20STAMP.jpeg\" alt=\"Ashtavinayak\">\r\n            <div>Authorized Signatory</div>\r\n        </div>\r\n    </div>\r\n\r\n</div>\r\n\r\n\r\n</body>\r\n</html>");


                //Customer Info
                builder.Replace("%Customer Name%", booking.User?.UserName ?? "N/A");
                builder.Replace("%Consignee Name%", booking.User?.UserName ?? "N/A");
                builder.Replace("%Address%", "N/A");
                builder.Replace("%GSTN%", "N/A");

                builder.Replace("%Date%", booking.BookingDate?.ToString("dd-MMM-yyyy") ?? "N/A");
                builder.Replace("%Invoice No%", GenerateInvoiceNumber(bookingId, DateTime.Now));
                if (booking.TripId != null)
                {
                    var vehicale = await _context.Vehicles.Where(x => x.TripId == booking.TripId && x.IsDeleted == false).FirstOrDefaultAsync();
                    if (vehicale != null)
                    {
                        builder.Replace("%Vehicle No%", vehicale.VehicleNumber);
                        builder.Replace("%Driver%", vehicale.DriverName);
                    }
                    else
                    {
                        builder.Replace("%Vehicle No%", "N/A");
                        builder.Replace("%Driver%", "N/A");
                    }
                }
                else
                {
                    builder.Replace("%Vehicle No%", "N/A");
                    builder.Replace("%Driver%", "N/A");
                }

                //Particular

                var familyBooking = await _context.FamilyBookings.Include(x => x.Package).Where(x => x.BookingId == booking.BookingId).FirstOrDefaultAsync();
                if (familyBooking != null)
                {
                    builder.Replace("%Desc%", familyBooking.Package.PackageName);
                }
                else
                {
                    builder.Replace("%Desc%", "Ashtavinayak Booking");
                }
                decimal subTotal = (decimal)booking.TotalPayment / 1.05m;
                decimal totalGST = (decimal)booking.TotalPayment - subTotal;
                decimal cgst = totalGST / 2;
                decimal sgst = totalGST / 2;
                builder.Replace("%Rate%", subTotal.ToString("0.00"));
                builder.Replace("%Net Amount%", subTotal.ToString("0.00"));
                builder.Replace("%Sub Total%", subTotal.ToString("0.00"));
                builder.Replace("%CGST%", cgst.ToString("0.00"));
                builder.Replace("%SGST%", sgst.ToString("0.00"));
                builder.Replace("%Total GST%", totalGST.ToString("0.00"));
                builder.Replace("%Total Amount%", booking.TotalPayment?.ToString("0.00") ?? "0.00");

                builder.Replace("%Rupees in words", AmountInWords(booking.TotalPayment ?? 0));

                HtmlToPdf converter = new HtmlToPdf();
                PdfDocument doc = converter.ConvertHtmlString(builder.ToString());
                byte[] receiptImgBytes = doc.Save();
                doc.Close();
                string billInvoice = System.Convert.ToBase64String(receiptImgBytes);

                return (true, "Invoice Generated Succssefuly.", billInvoice);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "GetInvoiceAsync failed for BookingId={BookingId}", bookingId);
                return (false, "An unexpected error occurred. Please try again.", null);
            }
        }

        #region Utility
        public static string AmountInWords(decimal amount)
        {
            string[] units = { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
                       "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
                       "Seventeen", "Eighteen", "Nineteen" };

            string[] tens = { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

            Func<long, string> convert = null;
            convert = (num) =>
            {
                if (num == 0) return "Zero";

                string words = "";

                if ((num / 10000000) > 0)
                {
                    words += convert(num / 10000000) + " Crore ";
                    num %= 10000000;
                }

                if ((num / 100000) > 0)
                {
                    words += convert(num / 100000) + " Lakh ";
                    num %= 100000;
                }

                if ((num / 1000) > 0)
                {
                    words += convert(num / 1000) + " Thousand ";
                    num %= 1000;
                }

                if ((num / 100) > 0)
                {
                    words += convert(num / 100) + " Hundred ";
                    num %= 100;
                }

                if (num > 0)
                {
                    if (words != "") words += "and ";

                    if (num < 20) words += units[num];
                    else
                    {
                        words += tens[num / 10];
                        if ((num % 10) > 0)
                            words += " " + units[num % 10];
                    }
                }

                return words.Trim();
            };

            long rupees = (long)Math.Floor(amount);
            int paise = (int)((amount - rupees) * 100);

            string result = convert(rupees) + " Rupees";
            if (paise > 0)
                result += " and " + convert(paise) + " Paise";

            return result;
        }
        public static string GetFinancialYear(DateTime date)
        {
            int year = date.Year;
            int nextYear = year + 1;

            if (date.Month < 4) // Jan-Mar belongs to previous financial year
            {
                year -= 1;
                nextYear -= 1;
            }

            string fy = (year % 100).ToString("D2") + "-" + (nextYear % 100).ToString("D2");
            return fy;
        }
        public static string GenerateInvoiceNumber(long invoiceId, DateTime date)
        {
            string financialYear = GetFinancialYear(date);
            return $"INV/{financialYear}/{invoiceId}";
        }

        #endregion


        #region Utility
public async Task<List<Booking>> GetBookingByTripIdAsync(int tripId)
        {
            return await _context.Bookings.Include(x=>x.User)
                .Where(b => b.TripId == tripId && !b.IsDeleted)
                .GroupBy(x=>x.User.PhoneNumber)
                .Select(g=>g.First())
                .ToListAsync();
        }
        #endregion
    }
}
