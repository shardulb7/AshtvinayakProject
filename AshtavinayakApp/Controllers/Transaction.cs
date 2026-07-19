using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AshtavinayakAPP.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace AshtavinayakAPP.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly AshtvinayakTravelContext _context;

        public TransactionController(AshtvinayakTravelContext context)
        {
            _context = context;
        }

        // GET: api/Transactions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetTransactions()
        {
            try
            {
                var transactions = await _context.Transactions.Where(x => !x.IsDeleted)
                    .Include(t => t.Booking)
                    .Include(t => t.User)
                    .Select(t => new
                    {
                        TransactionId = t.TransactionId,
                        BookingCode = t.Booking.BookingCode,
                        UserName = t.User.UserName,
                        Amount = t.Amount, // Assuming the Transaction model has an Amount field
                        TransactionDate = t.TransactionDate,
                        PaymentStatus = t.PaymentStatus,
                        PaymentMethod = t.PaymentMethod
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Message = "Transactions fetched successfully.",
                    Data = transactions
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        // GET: api/Transactions/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetTransaction(int id)
        {
            try
            {
                var transaction = await _context.Transactions.Where(x => !x.IsDeleted)
                    .Include(t => t.Booking)
                    .Include(t => t.User)
                    .Where(t => t.TransactionId == id)
                    .Select(t => new
                    {
                        TransactionId = t.TransactionId,
                        BookingCode = t.Booking.BookingCode,
                        UserName = t.User.UserName,
                        Amount = t.Amount,
                        TransactionDate = t.TransactionDate,
                        PaymentStatus=t.PaymentStatus,
                        PaymentMethod = t.PaymentMethod


                    })
                    .FirstOrDefaultAsync();

                if (transaction == null)
                {
                    return NotFound("Transaction not found.");
                }

                return Ok(new
                {
                    Message = "Transaction fetched successfully.",
                    Data = transaction
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        // POST: api/Transactions
        [HttpPost]
        public async Task<ActionResult<object>> PostTransaction([FromBody] Transaction transaction)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var bookingExists = await _context.Bookings.AnyAsync(b => b.BookingId == transaction.BookingId);
                if (!bookingExists)
                {
                    return BadRequest("Invalid BookingId.");
                }

                var userExists = await _context.Users.AnyAsync(u => u.UserId == transaction.UserId);
                if (!userExists)
                {
                    return BadRequest("Invalid UserId.");
                }

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Transaction created successfully.",
                    Data = transaction
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        // PUT: api/Transactions/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTransaction(int id, [FromBody] Transaction transaction)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (id != transaction.TransactionId)
                {
                    return BadRequest("Transaction ID mismatch.");
                }

                var existingTransaction = await _context.Transactions.FindAsync(id);
                if (existingTransaction == null)
                {
                    return NotFound("Transaction not found.");
                }

                existingTransaction.BookingId = transaction.BookingId;
                existingTransaction.UserId = transaction.UserId;
                existingTransaction.Amount = transaction.Amount;
                existingTransaction.TransactionDate = transaction.TransactionDate;
                existingTransaction.PaymentMethod = transaction.PaymentMethod;
                                existingTransaction.PaymentMethod = transaction.PaymentMethod;



                _context.Entry(existingTransaction).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Transaction updated successfully.",
                    Data = existingTransaction
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TransactionExists(id))
                {
                    return NotFound("Transaction not found.");
                }
                else
                {
                    throw;
                }
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        // DELETE: api/Transactions/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            try
            {
                var transaction = await _context.Transactions.FindAsync(id);
                if (transaction == null)
                {
                    return NotFound("Transaction not found.");
                }

                _context.Transactions.Remove(transaction);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Transaction deleted successfully.",
                    DeletedTransactionId = id
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        private bool TransactionExists(int id)
        {
            return _context.Transactions.Any(e => e.TransactionId == id);
        }
    }
}
