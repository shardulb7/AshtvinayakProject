using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AshtavinayakAPP.Models;
using OfficeOpenXml;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AshtavinayakAPP.Controllers
{
    public class BookingSeatsController : Controller
    {
        private readonly AshtvinayakTravelContext _context;

        public BookingSeatsController(AshtvinayakTravelContext context)
        {
            _context = context;
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userSession = context.HttpContext.Session.GetString("User");
            if (string.IsNullOrEmpty(userSession))
            {
                // If no session exists, redirect to the login page
                context.Result = new RedirectToActionResult("Login", "Home", null);
            }
            base.OnActionExecuting(context);
        }
        // GET: BookingSeats
        public async Task<IActionResult> Index(int? id, int? bookingId)
        {
            // Step 1: Base query
            var query = _context.BookingSeats
                .Include(b => b.Trip)
                .Include(b => b.User)
                .Where(b => !b.IsDeleted)
                .AsQueryable();

            // Step 2: Apply filters
            if (id.HasValue && id > 0)
                query = query.Where(b => b.TripId == id);
            if (bookingId.HasValue && bookingId > 0)
                query = query.Where(b => b.BookingId == bookingId);

            // Step 3: Execute query and move grouping client-side
            var bookingSeats = await query.ToListAsync();

            // Step 4: Group and project
            var data = bookingSeats
                .GroupBy(b => new
                {
                    b.TripId,
                    b.UserId,
                    b.Trip?.TourName,
                    b.User?.UserName,
                    b.Trip?.TripDate,
                    b.BookingId
                })
                .Select(g => new BookingSeatDto
                {
                    Id = g.Max(x => x.BookingSeatId),
                    TripId = g.Key.TripId ?? 0,
                    TourName = g.Key.TourName,
                    UserId = g.Key.UserId ?? 0,
                    UserName = g.Key.UserName,
                    TripDate = g.Key.TripDate ?? DateTime.MinValue,
                    BookingId = g.Key.BookingId ?? 0,
                    SeatNumbers = string.Join(", ", g.Select(x => x.SeatNumber).Distinct()),
                    Adults = g.Sum(x => (int?)x.Adults) ?? 0,
                    ChildWithSeat = g.Sum(x => (int?)x.Childwithseat) ?? 0,
                    ChildWithoutSeat = g.Sum(x => (int?)x.Childwithoutseat) ?? 0
                })
                .OrderByDescending(x => x.TripDate)
                .ToList();

            return View(data);
        }


        // GET: BookingSeats/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bookingSeat = await _context.BookingSeats.Include(b => b.Trip)
                                  .Include(b => b.User)
                                  .FirstOrDefaultAsync(m => m.BookingSeatId == id);
            if (bookingSeat == null)
            {
                return NotFound();
            }

            return View(bookingSeat);
        }

        // GET: BookingSeats/Create
        public IActionResult Create()
        {
            ViewData["TripId"] = new SelectList(
                _context.Trips.Where(x => !x.IsDeleted), "TripId", "TourName");
            ViewData["UserId"] = new SelectList(
                _context.Users.Where(x => !x.IsDeleted), "UserId", "UserName");
            ViewData["BookingId"] = new SelectList(_context.Bookings.Select(b => new {
                BookingId = b.BookingId,
                BookingDate = b.BookingDate  // Convert DateTime to string
            }),
                                                   "BookingId", "BookingDate");

            return View();
        }

        // POST: BookingSeats/Create
        // To protect from overposting attacks, enable the specific properties you
        // want to bind to. For more details, see
        // http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([
          Bind(
          "BookingSeatId,SeatNumber,Adults,Childwithseat,Childwithoutseat,TripId,UserId,BookingId")
    ] BookingSeat bookingSeat)
        {
            if (ModelState.IsValid)
            {
                _context.Add(bookingSeat);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["TripId"] =
                new SelectList(_context.Trips.Where(x => !x.IsDeleted), "TripId",
                               "TourName", bookingSeat.TripId);
            ViewData["UserId"] =
                new SelectList(_context.Users.Where(x => !x.IsDeleted), "UserId",
                               "UserName", bookingSeat.UserId);
            ViewData["BookingId"] =
                new SelectList(_context.Users.Where(x => !x.IsDeleted), "BookingId",
                               "BookingDate", bookingSeat.BookingId);

            return View(bookingSeat);
        }

        // GET: BookingSeats/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bookingSeat = await _context.BookingSeats.FindAsync(id);
            if (bookingSeat == null)
            {
                return NotFound();
            }
            ViewData["TripId"] =
                new SelectList(_context.Trips.Where(x => !x.IsDeleted), "TripId",
                               "TourName", bookingSeat.TripId);
            ViewData["UserId"] =
                new SelectList(_context.Users.Where(x => !x.IsDeleted), "UserId",
                               "UserName", bookingSeat.UserId);
            ViewData["BookingId"] =
                new SelectList(_context.Users.Where(x => !x.IsDeleted), "BookingId",
                               "BookingDate", bookingSeat.BookingId);

            return View(bookingSeat);
        }

        // POST: BookingSeats/Edit/5
        // To protect from overposting attacks, enable the specific properties you
        // want to bind to. For more details, see
        // http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [
          Bind(
          "BookingSeatId,SeatNumber,Adults,Childwithseat,Childwithoutseat,TripId,UserId,BookingId")
    ] BookingSeat bookingSeat)
        {
            if (id != bookingSeat.BookingSeatId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(bookingSeat);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingSeatExists(bookingSeat.BookingSeatId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["TripId"] =
                new SelectList(_context.Trips.Where(x => !x.IsDeleted), "TripId",
                               "TripName", bookingSeat.TripId);
            ViewData["UserId"] =
                new SelectList(_context.Users.Where(x => !x.IsDeleted), "UserId",
                               "UserName", bookingSeat.UserId);
            ViewData["BookingId"] =
                new SelectList(_context.Users.Where(x => !x.IsDeleted), "BookingId",
                               "BookingDate", bookingSeat.BookingId);

            return View(bookingSeat);
        }

        // GET: BookingSeats/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bookingSeat = await _context.BookingSeats.Include(b => b.Trip)
                                  .Include(b => b.User)
                                  .FirstOrDefaultAsync(m => m.BookingSeatId == id);
            if (bookingSeat == null)
            {
                return NotFound();
            }

            return View(bookingSeat);
        }

        // POST: BookingSeats/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bookingSeat = await _context.BookingSeats.FindAsync(id);
            if (bookingSeat != null)
            {
                bookingSeat.IsDeleted = true;
                _context.BookingSeats.Update(bookingSeat);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ExportToExcel(string trip)
        {
            var bookings = await _context.BookingSeats.Include(b => b.Trip)
                               .Include(b => b.User)
                               .ToListAsync();

            // Apply filter if a trip is selected
            if (!string.IsNullOrEmpty(trip))
            {
                bookings = bookings.Where(b => b.Trip.TourName == trip).ToList();
            }

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("BookingSeats");

                // Add header row
                worksheet.Cells[1, 1].Value = "Serial No";
                worksheet.Cells[1, 2].Value = "Booking ID";
                worksheet.Cells[1, 3].Value = "Trip";
                worksheet.Cells[1, 4].Value = "User";
                worksheet.Cells[1, 5].Value = "Seat Number";
                worksheet.Cells[1, 6].Value = "Adults";
                worksheet.Cells[1, 7].Value = "Child with Seat";
                worksheet.Cells[1, 8].Value = "Child without Seat";

                // Fill data
                int row = 2;
                int serialNumber = 1;

                foreach (var item in bookings)
                {
                    worksheet.Cells[row, 1].Value = serialNumber++;
                    worksheet.Cells[row, 2].Value = item.BookingId;
                    worksheet.Cells[row, 3].Value = item.Trip?.TourName;
                    worksheet.Cells[row, 4].Value = item.User?.UserName;
                    worksheet.Cells[row, 5].Value = item.SeatNumber;
                    worksheet.Cells[row, 6].Value = item.Adults;
                    worksheet.Cells[row, 7].Value = item.Childwithseat;
                    worksheet.Cells[row, 8].Value = item.Childwithoutseat;

                    row++;
                }

                // Save the Excel file to memory stream
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                // Return the Excel file as download
                string fileName = "BookingSeats.xlsx";
                return File(
                    stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
        }

        private bool BookingSeatExists(int id)
        {
            return _context.BookingSeats.Any(e => e.BookingSeatId == id);
        }
    }
}