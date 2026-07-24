using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using AshtavinayakAPP.Models;

namespace AshtavinayakAPP.Controllers
{
    // Read-only reporting screen — no create/edit/delete, this is a view over Bookings
    // that were placed by an Agent.
    public class AgentBookingsController : Controller
    {
        private readonly AshtvinayakTravelContext _context;

        public AgentBookingsController(AshtvinayakTravelContext context)
        {
            _context = context;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userSession = context.HttpContext.Session.GetString("User");
            if (string.IsNullOrEmpty(userSession))
            {
                context.Result = new RedirectToActionResult("Login", "Home", null);
            }
            base.OnActionExecuting(context);
        }

        public async Task<IActionResult> Index(
            DateTime? fromDate, DateTime? toDate, string? agentName,
            string? bookingStatus, string? paymentStatus, int page = 1)
        {
            const int pageSize = 15;

            var query = _context.Bookings
                .Where(b => b.AgentId != null && !b.IsDeleted)
                .Include(b => b.Agent)
                .Include(b => b.User)
                .Include(b => b.Trip).ThenInclude(t => t!.Package)
                .Include(b => b.Transactions)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(b => b.BookingDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(b => b.BookingDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

            if (!string.IsNullOrWhiteSpace(agentName))
                query = query.Where(b => b.Agent != null && b.Agent.FullName.Contains(agentName));

            if (!string.IsNullOrWhiteSpace(bookingStatus))
                query = query.Where(b => b.Status == bookingStatus);

            if (!string.IsNullOrWhiteSpace(paymentStatus))
                query = query.Where(b => b.Transactions.Any(t => t.PaymentStatus == paymentStatus));

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var bookings = await query
                .OrderByDescending(b => b.BookingId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var rows = bookings.Select(b => new AgentBookingReportRow
            {
                BookingId = b.BookingId,
                BookingDate = b.BookingDate,
                AgentName = b.Agent?.FullName ?? "-",
                CustomerName = b.User?.UserName ?? "-",
                PackageOrService = b.Trip?.Package?.PackageName ?? b.Trip?.TourName ?? "-",
                TotalBookingAmount = b.TotalPayment ?? 0,
                CommissionPercentage = b.CommissionPercentage ?? 0,
                CommissionAmount = b.CommissionAmount ?? 0,
                NetAmountPaidByAgent = (b.TotalPayment ?? 0) - (b.CommissionAmount ?? 0),
                BookingStatus = b.Status,
                PaymentStatus = b.Transactions.OrderByDescending(t => t.TransactionDate).FirstOrDefault()?.PaymentStatus
            }).ToList();

            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;
            ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
            ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");
            ViewData["AgentName"] = agentName;
            ViewData["BookingStatus"] = bookingStatus;
            ViewData["PaymentStatus"] = paymentStatus;

            return View(rows);
        }
    }
}
