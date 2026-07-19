//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using AshtavinayakAPP.Models;

//namespace AshtavinayakAPP.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class FamilyBookingController : ControllerBase
//    {
//        private readonly AshtvinayakTravelAppContext _context;

//        public FamilyBookingController(AshtvinayakTravelAppContext context) => _context = context;

//        // ✅ GET: api/FamilyBooking (Get all bookings)
//        [HttpGet]
//        public async Task<ActionResult<IEnumerable<FamilyBooking>>> GetFamilyBookings()
//        {
//            return await _context.FamilyBookings
//                .Include(f => f.Trip)  // Include Trip details if needed
//                .ToListAsync();
//        }

//        // ✅ GET: api/FamilyBooking/{id} (Get single booking by ID)
//        [HttpGet("{id}")]
//        public async Task<ActionResult<FamilyBooking>> GetFamilyBooking(int id)
//        {
//            var familyBooking = await _context.FamilyBookings
//                .Include(f => f.Trip)
//                .FirstOrDefaultAsync(f => f.FamilyId == id);

//            if (familyBooking == null)
//            {
//                return NotFound();
//            }

//            return familyBooking;
//        }

//        // ✅ POST: api/FamilyBooking (Create a new booking)
//        [HttpPost]
//        public async Task<ActionResult<FamilyBooking>> CreateFamilyBooking([FromBody] FamilyBooking familyBooking)
//        {
//            if (!ModelState.IsValid)
//            {
//                return BadRequest(ModelState);
//            }

//            _context.FamilyBookings.Add(familyBooking);
//            await _context.SaveChangesAsync();

//            return CreatedAtAction(nameof(GetFamilyBooking), new { id = familyBooking.FamilyId }, familyBooking);
//        }

//        // ✅ PUT: api/FamilyBooking/{id} (Update an existing booking)
//        [HttpPut("{id}")]
//        public async Task<IActionResult> UpdateFamilyBooking(int id, [FromBody] FamilyBooking familyBooking)
//        {
//            if (id != familyBooking.FamilyId)
//            {
//                return BadRequest();
//            }

//            var existingBooking = await _context.FamilyBookings.FindAsync(id);
//            if (existingBooking == null)
//            {
//                return NotFound();
//            }

//            // Update only non-null fields (BookingId and TripId can be null)
//            existingBooking.CarType = familyBooking.CarType ?? existingBooking.CarType;
//            existingBooking.Date = familyBooking.Date;
//            existingBooking.Time = familyBooking.Time;
//            existingBooking.BookingId = familyBooking.BookingId;  // Nullable, so it can be updated or set to null
//            existingBooking.TripId = familyBooking.TripId;  // Nullable, so it can be updated or set to null

//            _context.Entry(existingBooking).State = EntityState.Modified;

//            try
//            {
//                await _context.SaveChangesAsync();
//            }
//            catch (DbUpdateConcurrencyException)
//            {
//                if (!FamilyBookingExists(id))
//                {
//                    return NotFound();
//                }
//                else
//                {
//                    throw;
//                }
//            }

//            return NoContent();
//        }

//        // ✅ DELETE: api/FamilyBooking/{id} (Delete a booking)
//        [HttpDelete("{id}")]
//        public async Task<IActionResult> DeleteFamilyBooking(int id)
//        {
//            var familyBooking = await _context.FamilyBookings.FindAsync(id);
//            if (familyBooking == null)
//            {
//                return NotFound();
//            }

//            _context.FamilyBookings.Remove(familyBooking);
//            await _context.SaveChangesAsync();

//            return NoContent();
//        }

//        private bool FamilyBookingExists(int id)
//        {
//            return _context.FamilyBookings.Any(e => e.FamilyId == id);
//        }
//    }
//}
